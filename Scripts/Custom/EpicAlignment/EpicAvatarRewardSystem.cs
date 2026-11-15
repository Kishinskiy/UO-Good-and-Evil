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
        public static readonly int StatBonusAmount = 25;       // Мгновенный прирост к Str/Dex/Int (+25 к каждому)
        public static readonly int MaxIndividualStat = 150;    // Предел одного стата (было 125, стало 150 как StrMaxCap)
        public static readonly int CustomStatCap = 330;        // Сумма всех статов (было 225, стало 330)
        public static readonly int CustomTotalSkillCap = 8000; // Общий кап скиллов: 800.0 вместо стандартных 700.0
        public static readonly double EpicSkillCap = 130.0;    // Лимит прокачки профильных навыков до 130.0
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

            if (pm.Shrines != ActivatedShrines.All)
            {
                pm.SendMessage(0x22, "Вы помогли одолеть Темного Лорда, но без активации всех 8 Алтарей Добродетели вознесение невозможно!");
                return;
            }

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

            // Выдаем Гримуар Аватара
            GiveItem(pm, new EpicBookOfDestiny());

            ApplyAscensionStats(pm, false);

            pm.SendMessage(0x35, $"Поздравляем! Вы получили ранг Аватара Света! Лимиты Str/Dex/Int повышены, общий Stat Cap поднят до {CustomStatCap}. Навыки Magery и EvalInt теперь могут быть развиты до {EpicSkillCap}!");
            Effects.PlaySound(pm.Location, pm.Map, 0x5C9);
            pm.FixedParticles(0x373A, 10, 15, 5018, EffectLayer.Waist);
        }

        // Выдача статуса Владыки Тьмы (EpicEvil)
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
                pm.SendMessage(0x22, "В вашей душе недостаточно тьмы для этого ритуала (требуется карма от -5000 и ниже).");
                return;
            }

            if (pm.IsDemigod || pm.EpicGood || pm.EpicEvil)
            {
                pm.SendMessage(0x22, "Вы уже прошли путь Вознесения и обладаете силой Полубога!");
                return;
            }

            pm.IsDemigod = true;
            pm.EpicEvil = true;

            World.Broadcast(0x22, true, $"[Торжество Тьмы] Адепт Хаоса {pm.Name} поверг Верховного Серафима и обрел мощь Владыки Тьмы!");

            // Выдаем Темный Гримуар Мондейна
            GiveItem(pm, new EpicGrimoireOfChaos());

            ApplyAscensionStats(pm, true);

            pm.SendMessage(0x22, $"Поздравляем! Вы получили ранг Владыки Тьмы! Лимиты Str/Dex/Int повышены, общий Stat Cap поднят до {CustomStatCap}. Навыки Magery и EvalInt теперь могут быть развиты до {EpicSkillCap}!");
            Effects.PlaySound(pm.Location, pm.Map, 0x1FB);
            pm.FixedParticles(0x3709, 10, 30, 5052, 0x497, 0, EffectLayer.Waist);
        }

        private static void ApplyAscensionStats(PlayerMobile pm, bool isDarkPath)
        {
	        // 1. Повышаем общий и индивидуальный лимит характеристик (до 330 и 150)
	        pm.StatCap = CustomStatCap;
	        pm.RawStr = Math.Min(MaxIndividualStat, pm.RawStr + StatBonusAmount);
	        pm.RawDex = Math.Min(MaxIndividualStat, pm.RawDex + StatBonusAmount);
	        pm.RawInt = Math.Min(MaxIndividualStat, pm.RawInt + StatBonusAmount);

	        // 2. Увеличиваем общий лимит всех умений персонажа с 700.0 до 800.0
	        if (pm.SkillsCap < CustomTotalSkillCap)
		        pm.SkillsCap = CustomTotalSkillCap;

	        // 3. Открываем прокачку до 130.0 для универсальных и классовых навыков
	        pm.Skills[SkillName.Magery].Cap = EpicSkillCap;
	        pm.Skills[SkillName.EvalInt].Cap = EpicSkillCap;
	        pm.Skills[SkillName.Tactics].Cap = EpicSkillCap;
	        pm.Skills[SkillName.MagicResist].Cap = EpicSkillCap;

	        if (isDarkPath)
	        {
		        pm.Skills[SkillName.Necromancy].Cap = EpicSkillCap;
		        pm.Skills[SkillName.SpiritSpeak].Cap = EpicSkillCap;
	        }
	        else
	        {
		        pm.Skills[SkillName.Chivalry].Cap = EpicSkillCap;
		        pm.Skills[SkillName.Healing].Cap = EpicSkillCap;
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
