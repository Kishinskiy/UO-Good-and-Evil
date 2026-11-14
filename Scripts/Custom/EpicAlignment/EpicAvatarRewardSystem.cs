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
        // 👑 КОНФИГУРАЦИЯ СВЕРХСПОСОБНОСТЕЙ ПОЛУБОГА (БАЛАНС МЕНЯТЬ ТУТ)
        // =========================================================================
        public static readonly int StatBonusAmount = 30;       // Буст к силе/ловкости/инте
        public static readonly int MaxIndividualStat = 130;    // Максимальный предел стата
        public static readonly int CustomStatCap = 450;        // Лимит суммы всех характеристик (Stat Cap)
        public static readonly double EpicSkillCap = 130.0;    // Лимит прокачки магии до 130
        public static readonly int RequiredKarma = 5000;       // Требуемая карма (+5000 для Света, -5000 для Тьмы)

        // Единый метод для обоих Алтарей Чемпионов!
        // isDarkPath = false -> Светлый путь (для DarkLord)
        // isDarkPath = true  -> Темный путь (для SeraphimGuardian)
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

        // Выдача статуса Аватара Света (EpicGood)
        public static void GrantGoodAvatarStatus(PlayerMobile pm)
        {
            if (pm == null || pm.Deleted)
                return;

            if (pm.Karma < RequiredKarma)
            {
                pm.SendMessage(0x22, "Твоей силы света недостаточно для прохождения испытания (требуется карма от +5000).");
                return;
            }

            if (pm.IsDemigod || pm.EpicGood || pm.EpicEvil)
            {
                pm.SendMessage(0x35, "Вы уже прошли путь Вознесения и обладаете силой Полубога!");
                return;
            }

            pm.IsDemigod = true;
            pm.EpicGood = true;

            World.Broadcast(0x35, true, $"[Триумф Света] Герой {pm.Name} сокрушил Темного Лорда и совершил Восхождение к рангу Аватара!");

            ApplyAscensionStats(pm);

            pm.SendMessage(0x35, $"Поздравляем! Вы получили ранг Аватара Света! Лимиты Str/Dex/Int повышены, общий Stat Cap поднят до {CustomStatCap}. Навыки Magery и EvalInt теперь могут быть развиты до {EpicSkillCap}!");
            Effects.PlaySound(pm.Location, pm.Map, 0x5C9);
            pm.FixedParticles(0x373A, 10, 15, 5018, EffectLayer.Waist);
        }

        // Выдача статуса Владыки Тьмы (EpicEvil)
        public static void GrantEvilAvatarStatus(PlayerMobile pm)
        {
            if (pm == null || pm.Deleted)
                return;

            if (pm.Karma > -RequiredKarma)
            {
                pm.SendMessage(0x22, "В вашей душе недостаточно тьмы для этого ритуала (требуется карма от -5000 и ниже).");
                return;
            }

            if (pm.IsDemigod)
            {
                pm.SendMessage(0x22, "Вы уже прошли путь Вознесения и обладаете силой Полубога!");
                return;
            }

            pm.IsDemigod = true;
            pm.EpicEvil = true;

            World.Broadcast(0x22, true, $"[Торжество Тьмы] Адепт Хаоса {pm.Name} поверг Верховного Серафима и обрел мощь Владыки Тьмы!");

            ApplyAscensionStats(pm);

            pm.SendMessage(0x22, $"Поздравляем! Вы получили ранг Владыки Тьмы! Лимиты Str/Dex/Int повышены, общий Stat Cap поднят до {CustomStatCap}. Навыки Magery и EvalInt теперь могут быть развиты до {EpicSkillCap}!");
            Effects.PlaySound(pm.Location, pm.Map, 0x1FB);
            pm.FixedParticles(0x3709, 10, 30, 5052, 0x497, 0, EffectLayer.Waist);
        }

        private static void ApplyAscensionStats(PlayerMobile pm)
        {
            pm.StatCap = CustomStatCap;
            pm.RawStr = Math.Min(MaxIndividualStat, pm.RawStr + StatBonusAmount);
            pm.RawDex = Math.Min(MaxIndividualStat, pm.RawDex + StatBonusAmount);
            pm.RawInt = Math.Min(MaxIndividualStat, pm.RawInt + StatBonusAmount);

            pm.Skills[SkillName.Magery].Cap = EpicSkillCap;
            pm.Skills[SkillName.EvalInt].Cap = EpicSkillCap;
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
