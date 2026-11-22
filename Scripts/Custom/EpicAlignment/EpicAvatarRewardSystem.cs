using System;
using System.Collections.Generic;
using Server;
using Server.Items;
using Server.Mobiles;

namespace Server.Custom.EpicAlignment
{
    public static class EpicAvatarRewardSystem
    {
        // =========================================================================
        // 👑 ЧТЕНИЕ КОНФИГУРАЦИИ ИЗ Config/EpicAlignment.cfg
        // =========================================================================
        public static int StatCapBonus => Config.Get("EpicAlignment.StatCapBonus", 105);
        public static int StatInstantBonus => Config.Get("EpicAlignment.StatInstantBonus", 25);
        public static int MinIndividualStatCap => Config.Get("EpicAlignment.MinIndividualStatCap", 150);
        public static int TotalSkillCapBonus => Config.Get("EpicAlignment.TotalSkillCapBonus", 1000);
        public static double EpicSkillCapBonus => Config.Get("EpicAlignment.EpicSkillCapBonus", 30.0);
        public static int RequiredKarma => Config.Get("EpicAlignment.RequiredKarma", 5000);

        public static void OnChampionDefeated(BaseCreature boss, bool isDarkPath = false)
        {
            if (boss == null)
                return;

            List<DamageStore> rights = boss.GetLootingRights();

            foreach (DamageStore store in rights)
            {
                if (store.m_HasRight && store.m_Mobile is PlayerMobile pm)
                {
                    if (isDarkPath)
                        GrantEvilAvatarStatus(pm);
                    else
                        GrantGoodAvatarStatus(pm);
                }
            }
        }

        public static void GrantGoodAvatarStatus(PlayerMobile pm)
        {
            if (pm == null || pm.Deleted)
                return;

            if (pm.Shrines != ActivatedShrines.All)
            {
                pm.SendMessage(0x22, "Вы помогли одолеть Темного Лорда, но без активации всех 8 Алтарей Добродетели вознесение невозможно!");
                return;
            }

            if (pm.Karma < RequiredKarma)
            {
                pm.SendMessage(0x22, $"Твоей силы света недостаточно для прохождения испытания (требуется карма от +{RequiredKarma}).");
                return;
            }

            if (pm.IsDemigod || pm.EpicGood || pm.EpicEvil)
            {
                pm.SendMessage(0x35, "Вы уже прошли путь Вознесения и обладаете силой Полубога!");
                return;
            }

            pm.IsDemigod = true;
            pm.EpicGood = true;
            pm.Shrines = ActivatedShrines.None;

            World.Broadcast(0x35, true, $"[Триумф Света] Герой {pm.Name} сокрушил Темного Лорда и совершил Восхождение к рангу Аватара!");

            GiveItem(pm, new EpicBookOfDestiny());
            ApplyAscensionStats(pm, false);

            pm.SendMessage(0x35, $"Поздравляем! Вы получили ранг Аватара Света! Ваш Stat Cap увеличен на +{StatCapBonus} (текущий: {pm.StatCap}), общий Skill Cap увеличен на +{TotalSkillCapBonus / 10.0:F1}!");
            Effects.PlaySound(pm.Location, pm.Map, 0x5C9);
            pm.FixedParticles(0x373A, 10, 15, 5018, EffectLayer.Waist);
        }

        public static void GrantEvilAvatarStatus(PlayerMobile pm)
        {
            if (pm == null || pm.Deleted)
                return;

            if (pm.Shrines != ActivatedShrines.All)
            {
                pm.SendMessage(0x22, "Вы помогли повергнуть Серафима, но без активации всех 8 Алтарей Силы ритуал вознесения невозможен!");
                return;
            }

            if (pm.Karma > -RequiredKarma)
            {
                pm.SendMessage(0x22, $"В вашей душе недостаточно тьмы для этого ритуала (требуется карма от -{RequiredKarma} и ниже).");
                return;
            }

            if (pm.IsDemigod || pm.EpicGood || pm.EpicEvil)
            {
                pm.SendMessage(0x22, "Вы уже прошли путь Вознесения и обладаете силой Полубога!");
                return;
            }

            pm.IsDemigod = true;
            pm.EpicEvil = true;
            pm.Shrines = ActivatedShrines.None;

            World.Broadcast(0x22, true, $"[Торжество Тьмы] Адепт Хаоса {pm.Name} поверг Верховного Серафима и обрел мощь Владыки Тьмы!");

            GiveItem(pm, new EpicGrimoireOfChaos());
            ApplyAscensionStats(pm, true);

            pm.SendMessage(0x22, $"Поздравляем! Вы получили ранг Владыки Тьмы! Ваш Stat Cap увеличен на +{StatCapBonus} (текущий: {pm.StatCap}), общий Skill Cap увеличен на +{TotalSkillCapBonus / 10.0:F1}!");
            Effects.PlaySound(pm.Location, pm.Map, 0x1FB);
            pm.FixedParticles(0x3709, 10, 30, 5052, 0x497, 0, EffectLayer.Waist);
        }

        private static void ApplyAscensionStats(PlayerMobile pm, bool isDarkPath)
        {
            // 1. Аддитивно увеличиваем общий StatCap (не затирая скроллы и кастомные предметы!)
            pm.StatCap += StatCapBonus;

            // Повышаем индивидуальные капы статов, если они ниже целевого минимума
            if (pm.StrMaxCap < MinIndividualStatCap) pm.StrMaxCap = MinIndividualStatCap;
            if (pm.DexMaxCap < MinIndividualStatCap) pm.DexMaxCap = MinIndividualStatCap;
            if (pm.IntMaxCap < MinIndividualStatCap) pm.IntMaxCap = MinIndividualStatCap;

            // Начисляем мгновенный бонус к характеристикам с учетом актуального индивидуального капа игрока
            pm.RawStr = Math.Min(pm.StrMaxCap, pm.RawStr + StatInstantBonus);
            pm.RawDex = Math.Min(pm.DexMaxCap, pm.RawDex + StatInstantBonus);
            pm.RawInt = Math.Min(pm.IntMaxCap, pm.RawInt + StatInstantBonus);

            // 2. Аддитивно увеличиваем общий лимит всех умений (700.0 + 100.0 = 800.0, а если был бонус - сохранится)
            pm.SkillsCap += TotalSkillCapBonus;

            // 3. Аддитивно повышаем потолок профильных навыков (+30.0 к текущему капе навыка)
            IncreaseSkillCap(pm, SkillName.Magery, EpicSkillCapBonus);
            IncreaseSkillCap(pm, SkillName.EvalInt, EpicSkillCapBonus);
            IncreaseSkillCap(pm, SkillName.Tactics, EpicSkillCapBonus);
            IncreaseSkillCap(pm, SkillName.MagicResist, EpicSkillCapBonus);

            if (isDarkPath)
            {
                IncreaseSkillCap(pm, SkillName.Necromancy, EpicSkillCapBonus);
                IncreaseSkillCap(pm, SkillName.SpiritSpeak, EpicSkillCapBonus);
            }
            else
            {
                IncreaseSkillCap(pm, SkillName.Chivalry, EpicSkillCapBonus);
                IncreaseSkillCap(pm, SkillName.Healing, EpicSkillCapBonus);
            }
        }

        private static void IncreaseSkillCap(PlayerMobile pm, SkillName skillName, double bonus)
        {
            Skill skill = pm.Skills[skillName];
            if (skill != null)
            {
                skill.Cap += bonus;
            }
        }

        public static void GiveItem(PlayerMobile player, Item item)
        {
            if (player.Backpack == null || !player.AddToBackpack(item))
            {
                item.MoveToWorld(player.Location, player.Map);
                player.SendMessage(0x22, $"В рюкзаке нет места. Предмет [{item.Name}] положен у ваших ног.");
            }
        }

        public static void MonitorAvatarBoss(BaseCreature boss, PlayerMobile player, bool isDarkPath = false)
        {
            if (boss != null && player != null)
                new EpicBossMonitorTimer(boss, player, isDarkPath).Start();
        }

        private class EpicBossMonitorTimer : Timer
        {
            private readonly BaseCreature m_Boss;
            private readonly PlayerMobile m_Player;
            private readonly bool m_IsDarkPath;

            public EpicBossMonitorTimer(BaseCreature boss, PlayerMobile player, bool isDarkPath)
                : base(TimeSpan.FromSeconds(1.0), TimeSpan.FromSeconds(1.0))
            {
                m_Boss = boss;
                m_Player = player;
                m_IsDarkPath = isDarkPath;
                Priority = TimerPriority.OneSecond;
            }

            protected override void OnTick()
            {
                if (m_Boss == null || m_Boss.Deleted || m_Player == null || m_Player.Deleted)
                {
                    Stop();
                    return;
                }

                if (m_Boss.Alive)
                    return;

                Stop();

                if (!HasParticipated(m_Boss, m_Player))
                {
                    m_Player.SendMessage(0x22, "Испытание не засчитано: требуется лично участвовать в уничтожении Хранителя.");
                    return;
                }

                if (m_IsDarkPath)
                    GrantEvilAvatarStatus(m_Player);
                else
                    GrantGoodAvatarStatus(m_Player);
            }

            private bool HasParticipated(BaseCreature boss, PlayerMobile pm)
            {
                List<DamageStore> rights = boss.GetLootingRights();
                foreach (DamageStore store in rights)
                {
                    if (store.m_Damage <= 0 || store.m_Mobile == null)
                        continue;

                    if (store.m_Mobile == pm)
                        return true;

                    if (store.m_Mobile is BaseCreature pet && (pet.ControlMaster == pm || pet.SummonMaster == pm))
                        return true;
                }
                return false;
            }
        }
    }
}
