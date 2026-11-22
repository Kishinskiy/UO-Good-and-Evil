using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Server;
using Server.Engines.Craft;
using Server.Items;
using Server.Mobiles;
using Server.Custom.JewelryCrafting;

namespace Server.Custom.ItemSockets
{
    public class SocketedGemRecord
    {
        public string Effect { get; set; }
        public double Quality { get; set; }
        public string GemName { get; set; }
    }

    public static class ItemSocketSystem
    {
        private static readonly string FilePath = Path.Combine("Saves/Custom", "ItemSockets.bin");
        public static Dictionary<Serial, List<SocketedGemRecord>> SocketedItems { get; private set; }
            = new Dictionary<Serial, List<SocketedGemRecord>>();

        public static void Configure()
        {
            EventSink.WorldSave += OnSave;
            EventSink.WorldLoad += OnLoad;
        }

        public static int GetMaxSockets(Item item)
        {
            if (item is BaseWeapon weapon && !(weapon is Fists))
                return weapon.Layer == Layer.TwoHanded ? 3 : 2;

            if (item is BaseShield)
                return 2;

            if (item is BaseArmor)
                return 2;

            if (item is BaseJewel || item is BaseClothing)
                return 1;

            return 0;
        }

        public static List<SocketedGemRecord> GetSockets(Item item)
        {
            if (item == null || item.Deleted)
                return null;

            SocketedItems.TryGetValue(item.Serial, out var list);
            return list;
        }

        public static int GetUsedSocketsCount(Item item)
        {
            var list = GetSockets(item);
            return list?.Count ?? 0;
        }

        // Оцениваем сложность самого предмета (артефактность, магия, уже занятые сокеты)
        public static double GetEquipmentDifficultyPenalty(Item target, out string itemTierName)
        {
            int rarity = 0;
            bool isArtifact = false;

            if (target is BaseWeapon w)
            {
                rarity = w.ArtifactRarity;
                isArtifact = w.IsArtifact;
            }
            else if (target is BaseArmor a)
            {
                rarity = a.ArtifactRarity;
                isArtifact = a.IsArtifact;
            }
            else if (target is BaseJewel j)
            {
                rarity = j.ArtifactRarity;
                isArtifact = j.IsArtifact;
            }
            else if (target is BaseClothing c)
            {
                rarity = c.ArtifactRarity;
                isArtifact = c.IsArtifact;
            }

            int usedSockets = GetUsedSocketsCount(target);
            double socketPenalty = usedSockets * 2.5;

            if (rarity >= 10)
            {
                itemTierName = "Легендарный артефакт";
                return 15.0 + socketPenalty;
            }

            if (rarity > 0 || isArtifact)
            {
                itemTierName = "Артефакт";
                return 10.0 + socketPenalty;
            }

            if (HasStrongMagic(target))
            {
                itemTierName = "Магическая экипировка";
                return 5.0 + socketPenalty;
            }

            itemTierName = "Обычная экипировка";
            return socketPenalty;
        }

        private static bool HasStrongMagic(Item target)
        {
            AosAttributes attrs = GetAosAttributes(target);
            if (attrs != null && !attrs.IsEmpty)
                return true;

            if (target is BaseWeapon w && w.WeaponAttributes != null && !w.WeaponAttributes.IsEmpty)
                return true;

            if (target is BaseArmor a && a.ArmorAttributes != null && !a.ArmorAttributes.IsEmpty)
                return true;

            return false;
        }

        // Расчёт порогов навыка Blacksmithy с учётом И камня, И уровня предмета
        public static void GetSkillThresholds(JewelryGem gem, Item target, out double minSkill, out double maxSkillFor100, out string itemTierName)
        {
            double baseMin;
            double baseMax;

            switch (gem.GemEffect)
            {
                case "sapphire":
                case "emerald":
                case "diamond":
                case "citrine":
                case "amber":
                    baseMin = 60.0;
                    baseMax = 80.0;
                    break;

                case "ruby":
                case "amethyst":
                case "tourmaline":
                case "starsapphire":
                    baseMin = 75.0;
                    baseMax = 95.0;
                    break;

                default:
                    baseMin = 85.0;
                    baseMax = 105.0;
                    break;
            }

            double gemPenalty = 0.0;
            if (gem.GemQuality >= 3.0) gemPenalty = 10.0;
            else if (gem.GemQuality >= 2.0) gemPenalty = 5.0;
            else if (gem.GemQuality >= 1.5) gemPenalty = 2.5;

            double itemPenalty = GetEquipmentDifficultyPenalty(target, out itemTierName);

            minSkill = Math.Min(115.0, baseMin + gemPenalty + itemPenalty);
            maxSkillFor100 = Math.Min(120.0, baseMax + gemPenalty + itemPenalty);
        }

        public static double CalculateSocketSuccessChance(double playerSkill, double minSkill, double maxSkillFor100)
        {
            if (playerSkill < minSkill)
                return 0.0;

            if (playerSkill >= maxSkillFor100 || maxSkillFor100 <= minSkill)
                return 1.0;

            double progress = (playerSkill - minSkill) / (maxSkillFor100 - minSkill);
            return Math.Min(1.0, Math.Max(0.10, 0.10 + (progress * 0.90)));
        }

        public static bool TrySocketGem(Mobile from, JewelryGem gem, Item target)
        {
            if (from == null || gem == null || gem.Deleted || target == null || target.Deleted)
                return false;

            if (!gem.IsChildOf(from.Backpack) || !target.IsChildOf(from.Backpack))
            {
                from.SendMessage(0x22, "И камень, и предмет для инкрустации должны находиться в вашем рюкзаке (снимите вещь перед работой).");
                return false;
            }

            int maxSockets = GetMaxSockets(target);
            if (maxSockets <= 0)
            {
                from.SendMessage(0x22, "В этот предмет нельзя вставить драгоценный камень.");
                return false;
            }

            int used = GetUsedSocketsCount(target);
            if (used >= maxSockets)
            {
                from.SendMessage(0x22, $"В этом предмете больше нет свободных гнезд ({used}/{maxSockets}).");
                return false;
            }

            DefBlacksmithy.CheckAnvilAndForge(from, 3, out bool anvil, out bool forge);
            if (!anvil || !forge)
            {
                from.SendMessage(0x22, "Для инкрустации камней вы должны стоять рядом с наковальней и кузнечным горном!");
                return false;
            }

            GetSkillThresholds(gem, target, out double minSkill, out double maxSkillFor100, out string itemTierName);
            double playerSkill = from.Skills[SkillName.Blacksmith].Value;

            if (playerSkill < minSkill)
            {
                from.SendMessage(0x22, $"Предмет: [{itemTierName}]. Для вставки камня [{gem.Name}] требуется навык Blacksmithy {minSkill:F1} (для 100% успеха: {maxSkillFor100:F1}, ваш: {playerSkill:F1}).");
                return false;
            }

            double successChance = CalculateSocketSuccessChance(playerSkill, minSkill, maxSkillFor100);

            if (Utility.RandomDouble() > successChance)
            {
                gem.Delete();
                from.PlaySound(0x54);
                from.SendMessage(0x22, $"Неудача при обработке [{itemTierName}]! Камень раскололся (ваш шанс был {successChance * 100:F0}%, для 100% нужно {maxSkillFor100:F1} Blacksmithy).");
                return false;
            }

            if (!ApplyGemBonusToItem(target, gem.GemEffect, gem.GemQuality))
            {
                from.SendMessage(0x22, "Этот тип камня несовместим с выбранным типом экипировки.");
                return false;
            }

            if (!SocketedItems.TryGetValue(target.Serial, out var records))
            {
                records = new List<SocketedGemRecord>();
                SocketedItems[target.Serial] = records;
            }

            records.Add(new SocketedGemRecord
            {
                Effect = gem.GemEffect,
                Quality = gem.GemQuality,
                GemName = gem.Name
            });

            UpdateItemSocketTag(target, records.Count, maxSockets);

            gem.Delete();
            target.InvalidateProperties();

            from.PlaySound(0x2A);
            from.FixedParticles(0x373A, 10, 15, 5018, EffectLayer.Waist);
            from.SendMessage(0x35, $"Вы успешно инкрустировали [{gem.Name}] в [{itemTierName}]! Гнезд занято: {records.Count} / {maxSockets}.");
            return true;
        }

        private static void UpdateItemSocketTag(Item item, int used, int max)
        {
            string baseName = !string.IsNullOrEmpty(item.Name) ? item.Name : item.GetType().Name;
            int tagIndex = baseName.IndexOf(" [Сокеты:");
            if (tagIndex >= 0)
                baseName = baseName.Substring(0, tagIndex);

            item.Name = $"{baseName} [Сокеты: {used}/{max}]";
        }

        private static bool ApplyGemBonusToItem(Item item, string effect, double q)
        {
            AosAttributes attrs = GetAosAttributes(item);
            if (attrs == null)
                return false;

            BaseWeapon weapon = item as BaseWeapon;
            BaseArmor armor = item as BaseArmor;
            AosElementAttributes resists = GetAosResistances(item);

            switch (effect)
            {
                case "sapphire":
                    attrs.WeaponSpeed += (int)Math.Max(1, Math.Round(5.0 * q));
                    return true;
                case "emerald":
                    attrs.AttackChance += (int)Math.Max(1, Math.Round(5.0 * q));
                    return true;
                case "diamond":
                    attrs.DefendChance += (int)Math.Max(1, Math.Round(5.0 * q));
                    return true;
                case "citrine":
                    attrs.RegenStam += (int)Math.Max(1, Math.Round(5.0 * q));
                    return true;
                case "amber":
                    attrs.LowerRegCost += (int)Math.Max(1, Math.Round(10.0 * q));
                    return true;
                case "ruby":
                    attrs.WeaponDamage += (int)Math.Max(1, Math.Round(15.0 * q));
                    return true;
                case "amethyst":
                    attrs.LowerManaCost += (int)Math.Max(1, Math.Round(5.0 * q));
                    return true;
                case "tourmaline":
                    attrs.RegenMana += (int)Math.Max(1, Math.Round(3.0 * q));
                    return true;
                case "starsapphire":
                    int energyBonus = (int)Math.Max(1, Math.Round(5.0 * q));
                    if (armor != null) armor.EnergyBonus += energyBonus;
                    else if (weapon != null) weapon.WeaponAttributes.ResistEnergyBonus += energyBonus;
                    else if (resists != null) resists.Energy += energyBonus;
                    return true;
                case "perfectemerald":
                    attrs.BonusHits += (int)Math.Max(1, Math.Round(10.0 * q));
                    return true;
                case "darksapphire":
                    attrs.BonusMana += (int)Math.Max(1, Math.Round(15.0 * q));
                    return true;
                case "bluediamond":
                    attrs.CastSpeed += (int)Math.Max(1, Math.Round(1.0 * q));
                    return true;
                case "ecrucitrine":
                    attrs.EnhancePotions += (int)Math.Max(5, Math.Round(25.0 * q));
                    return true;
                case "firerubyslayer":
                    attrs.WeaponDamage += (int)Math.Max(2, Math.Round(15.0 * q));
                    attrs.AttackChance += (int)Math.Max(1, Math.Round(8.0 * q));
                    return true;
                case "turquoise":
                    if (weapon != null)
                        weapon.WeaponAttributes.HitLightning += (int)Math.Max(5, Math.Round(25.0 * q));
                    else
                        attrs.SpellDamage += (int)Math.Max(2, Math.Round(10.0 * q));
                    return true;
                case "vamp_shard":
                    if (weapon != null)
                        weapon.WeaponAttributes.HitLeechHits += (int)Math.Max(5, Math.Round(15.0 * q));
                    else
                        attrs.RegenHits += (int)Math.Max(1, Math.Round(3.0 * q));
                    return true;
                case "physical_resist":
                    int phys = (int)Math.Max(1, Math.Round(4.0 * q));
                    if (armor != null) armor.PhysicalBonus += phys;
                    else if (weapon != null) weapon.WeaponAttributes.ResistPhysicalBonus += phys;
                    else if (resists != null) resists.Physical += phys;
                    return true;
                case "fire_resist":
                    int fire = (int)Math.Max(1, Math.Round(4.0 * q));
                    if (armor != null) armor.FireBonus += fire;
                    else if (weapon != null) weapon.WeaponAttributes.ResistFireBonus += fire;
                    else if (resists != null) resists.Fire += fire;
                    return true;
                case "cold_resist":
                    int cold = (int)Math.Max(1, Math.Round(4.0 * q));
                    if (armor != null) armor.ColdBonus += cold;
                    else if (weapon != null) weapon.WeaponAttributes.ResistColdBonus += cold;
                    else if (resists != null) resists.Cold += cold;
                    return true;
                case "poison_resist":
                    int pois = (int)Math.Max(1, Math.Round(4.0 * q));
                    if (armor != null) armor.PoisonBonus += pois;
                    else if (weapon != null) weapon.WeaponAttributes.ResistPoisonBonus += pois;
                    else if (resists != null) resists.Poison += pois;
                    return true;
                case "energy_resist":
                    int ener = (int)Math.Max(1, Math.Round(4.0 * q));
                    if (armor != null) armor.EnergyBonus += ener;
                    else if (weapon != null) weapon.WeaponAttributes.ResistEnergyBonus += ener;
                    else if (resists != null) resists.Energy += ener;
                    return true;
                default:
                    return false;
            }
        }
        private static AosAttributes GetAosAttributes(Item item)
        {
            if (item is BaseWeapon w) return w.Attributes;
            if (item is BaseArmor a) return a.Attributes;
            if (item is BaseJewel j) return j.Attributes;
            if (item is BaseClothing c) return c.Attributes;
            return null;
        }
        private static AosElementAttributes GetAosResistances(Item item)
        {
            if (item is BaseJewel j) return j.Resistances;
            if (item is BaseClothing c) return c.Resistances;
            return null;
        }
        private static void OnSave(WorldSaveEventArgs e)
        {
            var dead = SocketedItems.Keys.Where(s => World.FindItem(s) == null).ToList();
            foreach (var s in dead)
                SocketedItems.Remove(s);
            Persistence.Serialize(FilePath, writer =>
            {
                writer.Write(0);
                writer.Write(SocketedItems.Count);
                foreach (var kvp in SocketedItems)
                {
                    writer.Write(kvp.Key);
                    writer.Write(kvp.Value.Count);
                    foreach (var rec in kvp.Value)
                    {
                        writer.Write(rec.Effect);
                        writer.Write(rec.Quality);
                        writer.Write(rec.GemName ?? "");
                    }
                }
            });
        }
        private static void OnLoad()
        {
            Persistence.Deserialize(FilePath, reader =>
            {
                int version = reader.ReadInt();
                int count = reader.ReadInt();
                SocketedItems.Clear();
                for (int i = 0; i < count; i++)
                {
                    Serial serial = reader.ReadSerial();
                    int recCount = reader.ReadInt();
                    List<SocketedGemRecord> list = new List<SocketedGemRecord>();
                    for (int j = 0; j < recCount; j++)
                    {
                        list.Add(new SocketedGemRecord
                        {
                            Effect = reader.ReadString(),
                            Quality = reader.ReadDouble(),
                            GemName = reader.ReadString()
                        });
                    }
                    if (World.FindItem(serial) != null)
                        SocketedItems[serial] = list;
                }
            });
        }
    }
}