using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Server;
using Server.Items;
using Server.Mobiles;

namespace Server.Custom.WeaponLeveling
{
    public class WeaponProgressData
    {
        public bool IsAwakened { get; set; } // Активировано ли оружие свитком у кузнеца
        public double SkillValue { get; set; }
        public double SkillCap { get; set; } = 100.0;
        public Dictionary<SlayerName, int> SlayerKills { get; set; } = new Dictionary<SlayerName, int>();
    }


    public static class WeaponLevelSystem
    {
        private static readonly string FilePath = Path.Combine("Saves/Custom", "WeaponLeveling.bin");
        public static Dictionary<Serial, WeaponProgressData> Weapons { get; private set; } = new Dictionary<Serial, WeaponProgressData>();

        // Порог убийств монстров одной группы для пробуждения Slayer на оружии
        public const int KillsRequiredForSlayer = 5000;

        public static void Configure()
        {
            EventSink.WorldSave += OnSave;
            EventSink.WorldLoad += OnLoad;
        }

        public static void Initialize()
        {
            EventSink.CreatureDeath += OnCreatureDeath;
        }

        public static WeaponProgressData GetData(BaseWeapon weapon, bool createIfMissing = false)
        {
            if (weapon == null || weapon.Deleted)
                return null;

            if (!Weapons.TryGetValue(weapon.Serial, out var data) && createIfMissing)
            {
                data = new WeaponProgressData();
                Weapons[weapon.Serial] = data;
            }

            return data;
        }

        private static void OnCreatureDeath(CreatureDeathEventArgs e)
        {
            BaseCreature victim = e.Creature as BaseCreature;
            if (victim == null || victim.Blessed || victim.Controlled || victim.Summoned || victim.Body.IsAnimal || !victim.IsAggressiveMonster)
                return;

            // Определяем игрока-убийцу
            Mobile killer = e.Killer;
            if (killer is BaseCreature bc)
                killer = bc.GetMaster();

            if (killer == null || !killer.Player)
                return;

            // Проверяем, какое оружие сейчас в руках у игрока
            BaseWeapon weapon = killer.Weapon as BaseWeapon;
            if (weapon == null || weapon is Fists)
                return;

            // Оружие качается ТОЛЬКО если оно предварительно пробуждено свитком у кузнеца
            var data = GetData(weapon, createIfMissing: false);
            if (data == null || !data.IsAwakened)
                return;

            ProcessWeaponProgress(killer, weapon, victim);
        }

        private static void ProcessWeaponProgress(Mobile player, BaseWeapon weapon, BaseCreature victim)
        {
            var data = GetData(weapon, createIfMissing: true);

            // 1. Прокачка уровня (скилла) самого оружия
            if (data.SkillValue < data.SkillCap)
            {
                // Шанс роста зависит от силы монстра (Fame) и текущего уровня оружия
                double baseChance = Math.Min(0.45, Math.Max(0.10, victim.Fame / 15000.0));
                double progressFactor = 1.0 - (data.SkillValue / (data.SkillCap + 10.0));

                if (Utility.RandomDouble() < baseChance * progressFactor)
                {
                    data.SkillValue = Math.Min(data.SkillCap, Math.Round(data.SkillValue + 0.1, 1));

                    // Даём оружию реальный бонус к урону по мере прокачки (например, +1% DamageIncrease за каждые 4.0 скилла, до +25%)
                    int targetDamageBonus = (int)(data.SkillValue / 4.0);
                    if (weapon.Attributes.WeaponDamage < targetDamageBonus)
                    {
                        weapon.Attributes.WeaponDamage = targetDamageBonus;
                    }

                    player.SendMessage(0x59, $"Ваше оружие [{GetWeaponName(weapon)}] закалилось в бою! Уровень: {data.SkillValue:F1}");
                }
            }

            // 2. Накопление убийств для получения соответствующего Slayer
            SlayerName matchedSlayer = FindSlayerForCreature(victim);
            if (matchedSlayer != SlayerName.None)
            {
                if (!data.SlayerKills.ContainsKey(matchedSlayer))
                    data.SlayerKills[matchedSlayer] = 0;

                data.SlayerKills[matchedSlayer]++;
                int kills = data.SlayerKills[matchedSlayer];

                // Если у оружия ещё нет Slayer и набито нужное количество монстров этого типа
                if (weapon.Slayer == SlayerName.None && kills >= KillsRequiredForSlayer)
                {
                    weapon.Slayer = matchedSlayer;
                    player.FixedParticles(0x373A, 10, 15, 5018, EffectLayer.Waist);
                    player.PlaySound(0x1F7);
                    player.SendMessage(0x35, $"Оружие впитало кровь врагов и пробудило силу [{matchedSlayer}]!");
                }
                // Опционально: при 100.0 уровне оружия троекратном убийствах другой группы можно открыть второй слот Slayer2
                else if (weapon.Slayer != SlayerName.None && weapon.Slayer != matchedSlayer && weapon.Slayer2 == SlayerName.None && data.SkillValue >= 100.0 && kills >= KillsRequiredForSlayer * 3)
                {
                    weapon.Slayer2 = matchedSlayer;
                    player.FixedParticles(0x373A, 10, 15, 5018, EffectLayer.Waist);
                    player.PlaySound(0x1F7);
                    player.SendMessage(0x35, $"Мастерское оружие обрело вторую силу: [{matchedSlayer}]!");
                }
            }
        }

        // Автоматический подбор Slayer через стандартные группы ServUO (SlayerGroup)
        public static SlayerName FindSlayerForCreature(BaseCreature creature)
        {
            if (creature == null || SlayerGroup.Groups == null)
                return SlayerName.None;

            foreach (SlayerGroup group in SlayerGroup.Groups)
            {
                if (group == null)
                    continue;

                // Сначала проверяем более узкие/специфические слееры (Entries), например OrcSlaying или DragonSlaying
                if (group.Entries != null)
                {
                    foreach (SlayerEntry entry in group.Entries)
                    {
                        if (entry != null && entry.Slays(creature))
                            return entry.Name;
                    }
                }

                // Если узкий не подошёл — проверяем общий SuperSlayer группы (Silver, Repond, Exorcism, Arachnid и т.д.)
                if (group.Super != null && group.Super.Slays(creature))
                {
                    return group.Super.Name;
                }
            }

            return SlayerName.None;
        }

        private static string GetWeaponName(BaseWeapon w)
        {
            return !string.IsNullOrEmpty(w.Name) ? w.Name : w.GetType().Name;
        }

        private static void OnSave(WorldSaveEventArgs e)
        {
            var deadSerials = Weapons.Keys.Where(s => World.FindItem(s) == null).ToList();
            foreach (var s in deadSerials)
                Weapons.Remove(s);

            Persistence.Serialize(FilePath, writer =>
            {
                writer.Write(1); // Версия 1 (с флагом IsAwakened)
                writer.Write(Weapons.Count);

                foreach (var kvp in Weapons)
                {
                    writer.Write(kvp.Key);
                    writer.Write(kvp.Value.IsAwakened);
                    writer.Write(kvp.Value.SkillValue);
                    writer.Write(kvp.Value.SkillCap);

                    writer.Write(kvp.Value.SlayerKills.Count);
                    foreach (var sk in kvp.Value.SlayerKills)
                    {
                        writer.Write((int)sk.Key);
                        writer.Write(sk.Value);
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

                Weapons.Clear();
                for (int i = 0; i < count; i++)
                {
                    Serial serial = reader.ReadSerial();
                    bool isAwakened = version >= 1 ? reader.ReadBool() : true;
                    double val = reader.ReadDouble();
                    double cap = reader.ReadDouble();

                    var data = new WeaponProgressData
                    {
                        IsAwakened = isAwakened,
                        SkillValue = val,
                        SkillCap = cap
                    };

                    int killCount = reader.ReadInt();
                    for (int j = 0; j < killCount; j++)
                    {
                        SlayerName slayer = (SlayerName)reader.ReadInt();
                        int kills = reader.ReadInt();
                        data.SlayerKills[slayer] = kills;
                    }

                    if (World.FindItem(serial) != null)
                    {
                        Weapons[serial] = data;
                    }
                }
            });
        }
    }
}
