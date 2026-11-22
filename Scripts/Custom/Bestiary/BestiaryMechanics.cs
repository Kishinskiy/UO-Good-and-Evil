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

            // 1. Применяем бонус золота и ресурсов от УЖЕ имеющихся знаний
            if (corpse != null && !corpse.Deleted && knowledge.Level > 0)
            {
                ApplyLootBonuses(killer, victim, corpse);
                TryDropBestiaryBook(killer, victim, corpse);
            }

            // 2. Шанс получить новое очко знаний в бестиарий (чем выше уровень, тем ниже шанс)
            if (knowledge.Level < BestiarySystem.MaxKnowledgeLevel)
            {
                // Базовый шанс 15% на 0 уровне -> 3% на 4 уровне (плюс небольшой бонус от Anatomy / ItemID / ForensicEval)
                double skillBonus = (killer.Skills[SkillName.Forensics].Value + killer.Skills[SkillName.Anatomy].Value) / 2000.0; // до +0.10
                double gainChance = Math.Max(0.02, (0.15 - (knowledge.Level * 0.03)) + skillBonus);

                if (Utility.RandomDouble() < gainChance)
                {
                    knowledge.Level++;
                    string mobTitle = !string.IsNullOrEmpty(victim.Name) ? victim.Name : victim.GetType().Name;

                    killer.PlaySound(0x1F7);
                    killer.SendMessage(0x59, $"[Бестиарий] Вы изучили повадки существа [{mobTitle}]! Уровень знаний: {knowledge.Level} / {BestiarySystem.MaxKnowledgeLevel}.");
                    killer.SendMessage(0x35, $"Бонусы против этого вида: +{knowledge.Level * 4}% урона, +{knowledge.Level * 6}% золота, +{knowledge.Level * 3}% шанс трофеев.");
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

        public BestiaryGump(PlayerMobile pm, int page) : base(100, 100)
        {
            _player = pm;
            _page = page;

            AddPage(0);
            AddBackground(0, 0, 480, 420, 9200);
            AddAlphaRegion(10, 10, 460, 400);

            AddHtml(20, 15, 440, 25, "<CENTER><BASEFONT COLOR=#FFD700>БЕСТИАРИЙ ИССЛЕДОВАТЕЛЯ</BASEFONT></CENTER>", false, false);

            var entries = BestiarySystem.PlayerData.TryGetValue(pm.Serial, out var dict)
                ? dict.OrderByDescending(x => x.Value.Level).ThenByDescending(x => x.Value.Kills).ToList()
                : new List<KeyValuePair<string, MonsterKnowledge>>();

            if (entries.Count == 0)
            {
                AddHtml(30, 60, 420, 40, "<BASEFONT COLOR=#FFFFFF>Вы ещё не изучили ни одного существа. Сражайтесь с монстрами, чтобы пополнить бестиарий!</BASEFONT>", false, false);
                return;
            }

            AddLabel(25, 45, 0x35, "Существо");
            AddLabel(220, 45, 0x35, "Уровень");
            AddLabel(295, 45, 0x35, "Убито");
            AddLabel(360, 45, 0x35, "Бонус урона");

            int perPage = 12;
            int start = page * perPage;
            int y = 75;

            for (int i = start; i < start + perPage && i < entries.Count; i++)
            {
                var kvp = entries[i];
                AddLabel(25, y, 0x481, kvp.Key);
                AddLabel(220, y, 0x59, $"{kvp.Value.Level} / {BestiarySystem.MaxKnowledgeLevel}");
                AddLabel(295, y, 0x3B2, kvp.Value.Kills.ToString());
                AddLabel(360, y, 0x35, $"+{kvp.Value.Level * 4}%");
                y += 24;
            }

            if (page > 0)
                AddButton(25, 380, 4014, 4016, 1, GumpButtonType.Reply, 0);

            if (start + perPage < entries.Count)
                AddButton(425, 380, 4005, 4007, 2, GumpButtonType.Reply, 0);
        }

        public override void OnResponse(Network.NetState sender, RelayInfo info)
        {
            if (info.ButtonID == 1 && _page > 0)
                _player.SendGump(new BestiaryGump(_player, _page - 1));
            else if (info.ButtonID == 2)
                _player.SendGump(new BestiaryGump(_player, _page + 1));
        }
    }
}
