using System;
using System.Collections.Generic;
using System.Linq;
using Server;
using Server.Commands;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;

namespace Server.Custom.Bestiary
{
    public static class BestiaryMechanics
    {
        public static void Initialize()
        {
            EventSink.CreatureDeath += OnCreatureDeath;
            CommandSystem.Register("bestiary", AccessLevel.Player, e =>
            {
                if (e.Mobile is PlayerMobile pm)
                    pm.SendGump(new BestiaryGump(pm, 0));
            });
        }

                private static void OnCreatureDeath(CreatureDeathEventArgs e)
        {
            BaseCreature victim = e.Creature as BaseCreature;
            Container corpse = e.Corpse;

            if (victim == null || victim.Blessed || victim.Controlled || victim.Summoned || victim.Body.IsAnimal || !victim.IsAggressiveMonster)
                return;

            Mobile killer = e.Killer;
            if (killer is BaseCreature bc)
                killer = bc.GetMaster();

            if (killer == null || !killer.Player)
                return;

            var knowledge = BestiarySystem.GetKnowledge(killer, victim, create: true);
            knowledge.Kills++;

            // 1. Применяем бонусы добычи и шанс выпадения книги
            if (corpse != null && !corpse.Deleted)
            {
                if (knowledge.SkillValue > 0.0)
                    ApplyLootBonuses(killer, victim, corpse);

                TryDropBestiaryBook(killer, victim, corpse);
            }

            // 2. Прокачка знания о монстре (шаг +0.1, с убывающей отдачей как у скиллов UO)
            if (knowledge.SkillValue < knowledge.SkillCap)
            {
                // Базовый шанс 35% на 0.0, плавно снижается по мере приближения к 100.0
                double progressFactor = 1.0 - (knowledge.SkillValue / (knowledge.SkillCap + 10.0));
                double skillBonus = (killer.Skills[SkillName.Forensics].Value + killer.Skills[SkillName.Anatomy].Value) / 2000.0; // до +10%
                double gainChance = Math.Max(0.02, (0.35 * progressFactor) + skillBonus);

                if (Utility.RandomDouble() < gainChance)
                {
                    knowledge.SkillValue = Math.Min(knowledge.SkillCap, Math.Round(knowledge.SkillValue + 0.1, 1));
                    string mobTitle = !string.IsNullOrEmpty(victim.Name) ? victim.Name : victim.GetType().Name;

                    killer.SendMessage(0x59, $"[Бестиарий] Ваши знания о существе [{mobTitle}] выросли до {knowledge.SkillValue:F1}%!");

                    // Звук и уведомление о вехах (50.0 и 100.0)
                    if (knowledge.SkillValue == 50.0)
                    {
                        killer.PlaySound(0x1F7);
                        killer.SendMessage(0x35, $"[Бестиарий] Вы изучили сопротивления существа [{mobTitle}]! Проверьте книгу.");
                    }
                    else if (knowledge.SkillValue == 100.0)
                    {
                        killer.PlaySound(0x1F7);
                        killer.SendMessage(0x35, $"[Бестиарий] Вы в совершенстве постигли анатомию [{mobTitle}] (100.0%)!");
                    }
                }
            }
        }


        private static void ApplyLootBonuses(Mobile killer, BaseCreature victim, Container corpse)
        {
            // Увеличиваем золото в трупе
            double goldMult = BestiarySystem.GetGoldBonusMultiplier(killer, victim);
            if (goldMult > 1.0)
            {
                foreach (Gold gold in corpse.Items.OfType<Gold>().ToList())
                {
                    gold.Amount = (int)Math.Ceiling(gold.Amount * goldMult);
                }
            }

            // Шанс выбить дополнительный ценный ресурс/трофей за счёт знаний анатомии монстра
            double dropChance = BestiarySystem.GetRareDropChance(killer, victim);
            if (dropChance > 0 && Utility.RandomDouble() < dropChance)
            {
                Item bonusResource = CreateBonusResourceFor(victim);
                if (bonusResource != null)
                {
                    corpse.DropItem(bonusResource);
                    killer.SendMessage(0x3B2, "Благодаря знаниям бестиария вы нашли дополнительный трофей в останках!");
                }
            }
        }

        // Подбор ресурса в зависимости от типа/силы монстра
        private static Item CreateBonusResourceFor(BaseCreature victim)
        {
            int amount = Utility.RandomMinMax(2, 5);

            // Если с монстра снимается кожа — даём редкую кожу
            if (victim.Hides > 0)
            {
                switch (victim.HideType)
                {
                    case HideType.Barbed: return new BarbedLeather(amount);
                    case HideType.Horned: return new HornedLeather(amount);
                    case HideType.Spined: return new SpinedLeather(amount);
                    default: return new Leather(amount * 2);
                }
            }

            // Для магических/сильных существ — драгоценные камни или свиток трансцендентности (очень редко)
            if (victim.Fame >= 10000 && Utility.RandomDouble() < 0.05)
                return ScrollOfTranscendence.CreateRandom(1, 5);

            Item gem = Loot.RandomGem();
            if (gem != null)
                gem.Amount = Utility.RandomMinMax(1, 3);

            return gem;
        }

        private static void TryDropBestiaryBook(Mobile killer, BaseCreature victim, Container corpse)
        {
            // Базовый шанс 1% для обычных монстров, до 10% для сильных существ и боссов (зависит от Fame)
            double dropChance = Math.Min(0.10, Math.Max(0.01, victim.Fame / 150000.0));

            if (victim.IsParagon)
                dropChance += 0.03;

            if (Utility.RandomDouble() < dropChance)
            {
                corpse.DropItem(new BestiaryBook());
                killer.SendMessage(0x59, "Среди трофеев поверженного существа вы обнаружили древнюю Энциклопедию чудовищ (Бестиарий)!");
            }
        }

    }

    // Окно бестиария игрока (команда [bestiary)
        public class BestiaryGump : Gump
    {
        private readonly PlayerMobile _player;
        private readonly int _page;
        private readonly string _selectedMonsterType;
        private readonly List<KeyValuePair<string, MonsterKnowledge>> _entries;

        public BestiaryGump(PlayerMobile pm, int page, string selectedMonsterType = null) : base(80, 80)
        {
            _player = pm;
            _page = page;
            _selectedMonsterType = selectedMonsterType;

            _entries = BestiarySystem.PlayerData.TryGetValue(pm.Serial, out var dict)
                ? dict.OrderByDescending(x => x.Value.SkillValue).ThenByDescending(x => x.Value.Kills).ToList()
                : new List<KeyValuePair<string, MonsterKnowledge>>();

            AddPage(0);
            AddBackground(0, 0, 680, 440, 9200);
            AddAlphaRegion(10, 10, 660, 420);

            AddHtml(20, 15, 640, 25, "<CENTER><BASEFONT COLOR=#FFD700>БЕСТИАРИЙ ИССЛЕДОВАТЕЛЯ</BASEFONT></CENTER>", false, false);

            if (_entries.Count == 0)
            {
                AddHtml(30, 70, 620, 40, "<BASEFONT COLOR=#FFFFFF>Страницы книги пока пусты. Сражайтесь с монстрами, чтобы изучать их анатомию и повадки!</BASEFONT>", false, false);
                return;
            }

            AddLabel(25, 45, 0x35, "Существо");
            AddLabel(205, 45, 0x35, "Изучено");
            AddLabel(275, 45, 0x35, "Убито");
            AddLabel(335, 45, 0x35, "Инфо");

            int perPage = 12;
            int start = page * perPage;
            int y = 75;

            for (int i = start; i < start + perPage && i < _entries.Count; i++)
            {
                var kvp = _entries[i];
                bool isSelected = string.Equals(kvp.Key, _selectedMonsterType, StringComparison.OrdinalIgnoreCase);

                AddLabel(25, y, isSelected ? 0x35 : 0x481, kvp.Key);
                AddLabel(205, y, 0x59, $"{kvp.Value.SkillValue:F1}%");
                AddLabel(275, y, 0x3B2, kvp.Value.Kills.ToString());

                AddButton(340, y + 2, 4005, 4007, 100 + i, GumpButtonType.Reply, 0);
                y += 25;
            }

            if (page > 0)
                AddButton(25, 395, 4014, 4016, 1, GumpButtonType.Reply, 0);

            if (start + perPage < _entries.Count)
                AddButton(335, 395, 4005, 4007, 2, GumpButtonType.Reply, 0);

            DrawMonsterDetailsPanel();
        }

        private void DrawMonsterDetailsPanel()
        {
            AddImageTiled(385, 45, 2, 370, 9304);

            if (string.IsNullOrEmpty(_selectedMonsterType))
            {
                AddHtml(400, 120, 255, 100, "<BASEFONT COLOR=#CCCCCC>Нажмите на стрелку рядом с изученным существом, чтобы открыть заметки о его уязвимостях и бонусах.</BASEFONT>", false, false);
                return;
            }

            var entry = _entries.FirstOrDefault(x => string.Equals(x.Key, _selectedMonsterType, StringComparison.OrdinalIgnoreCase));
            if (entry.Value == null)
                return;

            double skill = entry.Value.SkillValue;
            int y = 50;

            AddHtml(400, y, 260, 25, $"<BASEFONT COLOR=#FFD700><B>{entry.Key}</B></BASEFONT>", false, false);
            y += 28;

            AddLabel(400, y, 0x59, $"Изучение вида: {skill:F1}% / {entry.Value.SkillCap:F1}%");
            y += 22;
            AddLabel(400, y, 0x3B2, $"Повержено особей: {entry.Value.Kills}");
            y += 30;

            double dmgBonus = (skill / 100.0) * 25.0;
            double goldBonus = (skill / 100.0) * 30.0;
            double dropBonus = (skill / 100.0) * 15.0;

            AddLabel(400, y, 0x35, "Ваши бонусы против вида:");
            y += 22;
            AddLabel(410, y, 0x481, $"• Урон: +{dmgBonus:F1}%");
            y += 20;
            AddLabel(410, y, 0x481, $"• Золото в добыче: +{goldBonus:F1}%");
            y += 20;
            AddLabel(410, y, 0x481, $"• Шанс редкого трофея: +{dropBonus:F1}%");
            y += 32;

            BaseCreature sample = CreateSampleCreature(_selectedMonsterType);
            if (sample != null)
            {
                if (skill >= 50.0)
                {
                    AddLabel(400, y, 0x35, "Сопротивления (открыто на 50.0%):");
                    y += 22;
                    AddLabel(410, y, 0x481, $"Физ: {sample.PhysicalResistance}% | Огонь: {sample.FireResistance}% | Холод: {sample.ColdResistance}%");
                    y += 20;
                    AddLabel(410, y, 0x481, $"Яд: {sample.PoisonResistance}% | Энергия: {sample.EnergyResistance}%");
                    y += 28;
                }
                else
                {
                    AddHtml(400, y, 255, 40, "<BASEFONT COLOR=#888888>[Сопротивления существа откроются при изучении 50.0%]</BASEFONT>", false, false);
                    y += 45;
                }

                if (skill >= 100.0)
                {
                    AddLabel(400, y, 0x35, "Анатомия (открыто на 100.0%):");
                    y += 22;
                    AddLabel(410, y, 0x481, $"Здоровье: ~{sample.HitsMax} | Урон: {sample.DamageMin}-{sample.DamageMax}");
                }
                else
                {
                    AddHtml(400, y, 255, 40, "<BASEFONT COLOR=#888888>[Характеристики и урон откроются при изучении 100.0%]</BASEFONT>", false, false);
                }

                sample.Delete();
            }
        }

        private static BaseCreature CreateSampleCreature(string typeName)
        {
            Type t = ScriptCompiler.FindTypeByName(typeName);
            if (t != null && typeof(BaseCreature).IsAssignableFrom(t))
            {
                try
                {
                    return Activator.CreateInstance(t) as BaseCreature;
                }
                catch { }
            }
            return null;
        }

        public override void OnResponse(Network.NetState sender, RelayInfo info)
        {
            if (info.ButtonID == 1 && _page > 0)
            {
                _player.PlaySound(0x55);
                _player.SendGump(new BestiaryGump(_player, _page - 1, _selectedMonsterType));
            }
            else if (info.ButtonID == 2)
            {
                _player.PlaySound(0x55);
                _player.SendGump(new BestiaryGump(_player, _page + 1, _selectedMonsterType));
            }
            else if (info.ButtonID >= 100)
            {
                int index = info.ButtonID - 100;
                if (index >= 0 && index < _entries.Count)
                {
                    _player.PlaySound(0x55);
                    _player.SendGump(new BestiaryGump(_player, _page, _entries[index].Key));
                }
            }
        }
    }

}
