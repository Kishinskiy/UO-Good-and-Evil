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
        // 👑 КОНФИГУРАЦИЯ СВЕРХСПОСОБНОСТЕЙ АВАТАРА (БАЛАНС МЕНЯТЬ ТУТ)
        // =========================================================================
        public static readonly int StatBonusAmount = 30;       // Буст к силе/ловкости/инте
        public static readonly int MaxIndividualStat = 130;    // Максимальный предел стата
        public static readonly int CustomStatCap = 450;        // Лимит суммы всех характеристик (Stat Cap)
        public static readonly double EpicSkillCap = 130.0;    // Лимит прокачки магии до 130

        public static void MonitorAvatarBoss(BaseCreature boss, PlayerMobile player)
        {
            if (boss != null && player != null)
                new EpicBossMonitorTimer(boss, player).Start();
        }
        
        public static void OnChampionDefeated(BaseCreature boss)
        {
	        if (boss == null)
		        return;

	        List<DamageStore> rights = boss.GetLootingRights();

	        foreach (DamageStore store in rights)
	        {
		        // Проверяем, что у игрока есть право на лут и он участвовал в бою
		        if (store.m_HasRight && store.m_Mobile is PlayerMobile pm)
		        {
			        // Если нужно выдавать награду только светлым персонажам (раскомментируйте 2 строки ниже):
			        if (pm.Karma < 5000)
			        {
				        pm.SendMessage("Твоей силы света не достаточно для прохождения испытания");
				        continue;
			        }

			        // Защита от повторного получения статуса Аватара (проверяем по StatCap)
			        if (pm.IsDemigod)
			        {
				        pm.SendMessage(0x35, "Вы уже обладаете силой Аватара!");
				        continue;
			        }
			        
			        pm.IsDemigod = true;
			        pm.EpicGood = true;

			        World.Broadcast(0x35, true, $"[Триумф Света] Герой {pm.Name} сокрушил Темного Лорда и совершил Восхождение к рангу Аватара!");

			        // Накатываем эволюцию характеристик
			        pm.StatCap = CustomStatCap;
			        pm.RawStr = Math.Min(MaxIndividualStat, pm.RawStr + StatBonusAmount);
			        pm.RawDex = Math.Min(MaxIndividualStat, pm.RawDex + StatBonusAmount);
			        pm.RawInt = Math.Min(MaxIndividualStat, pm.RawInt + StatBonusAmount);

			        // Накатываем эволюцию лимитов магических навыков
			        pm.Skills[SkillName.Magery].Cap = EpicSkillCap;
			        pm.Skills[SkillName.EvalInt].Cap = EpicSkillCap;

			        pm.SendMessage(0x35, $"Поздравляем! Вы получили ранг Аватара! Лимиты Str/Dex/Int повышены, общий Stat Cap поднят до {CustomStatCap}. Навыки Magery и EvalInt теперь могут быть развиты до {EpicSkillCap}!");
			        Effects.PlaySound(pm.Location, pm.Map, 0x5C9);
		        }
	        }
        }

        // Метод для Алтаря Тьмы (вызывается из OnDeath босса SeraphimGuardian)
        public static void OnDarkChampionDefeated(BaseCreature boss)
        {
            if (boss == null)
                return;

            List<DamageStore> rights = boss.GetLootingRights();

            foreach (DamageStore store in rights)
            {
                if (store.m_HasRight && store.m_Mobile is PlayerMobile pm)
                {
                    GrantEvilAvatarStatus(pm);
                }
            }
        }

        // Выдача статуса Владыки Тьмы (EpicEvil)
        public static void GrantEvilAvatarStatus(PlayerMobile pm)
        {
            if (pm == null || pm.Deleted)
                return;

            // Защита от повторного вознесения или смены стороны
            if (pm.IsDemigod || pm.EpicGood || pm.EpicEvil)
            {
                pm.SendMessage(0x22, "Вы уже прошли путь Вознесения и обладаете силой Полубога!");
                return;
            }

            World.Broadcast(0x22, true, $"[Торжество Тьмы] Адепт Хаоса {pm.Name} поверг Верховного Серафима и обрел мощь Владыки Тьмы!");

            // Присваиваем флаги статуса
            pm.IsDemigod = true;
            pm.EpicEvil = true;

            // Накатываем эволюцию характеристик
            pm.StatCap = CustomStatCap;
            pm.RawStr = Math.Min(MaxIndividualStat, pm.RawStr + StatBonusAmount);
            pm.RawDex = Math.Min(MaxIndividualStat, pm.RawDex + StatBonusAmount);
            pm.RawInt = Math.Min(MaxIndividualStat, pm.RawInt + StatBonusAmount);

            // Накатываем эволюцию лимитов магических навыков
            pm.Skills[SkillName.Magery].Cap = EpicSkillCap;
            pm.Skills[SkillName.EvalInt].Cap = EpicSkillCap;

            pm.SendMessage(0x22, $"Поздравляем! Вы получили ранг Владыки Тьмы! Лимиты Str/Dex/Int повышены, общий Stat Cap поднят до {CustomStatCap}. Навыки Magery и EvalInt теперь могут быть развиты до {EpicSkillCap}!");
            Effects.PlaySound(pm.Location, pm.Map, 0x1FB);
            pm.FixedParticles(0x3709, 10, 30, 5052, 0x497, 0, EffectLayer.Waist); // Темное пламя вознесения
        }

        private class EpicBossMonitorTimer : Timer
        {
            private readonly BaseCreature m_Boss;
            private readonly PlayerMobile m_Player;

            public EpicBossMonitorTimer(BaseCreature boss, PlayerMobile player) 
                : base(TimeSpan.FromSeconds(1.0), TimeSpan.FromSeconds(1.0))
            {
                m_Boss = boss;
                m_Player = player;
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

                Stop(); // Босс умер!

                // Проверяем, наносил ли урон сам игрок или его питомцы/наёмники
                if (!HasParticipated(m_Boss, m_Player))
                {
                    m_Player.SendMessage(0x22, "Испытание не засчитано: требуется лично участвовать в уничтожении Хранителя.");
                    return;
                }

                World.Broadcast(0x35, true, $"[Триумф Света] Герой {m_Player.Name} сокрушил Травести и совершил Восхождение к рангу Аватара!");

                // Выдаем артефакты, которые мы вынесли в отдельные C#-классы на Шаге 1 и 2!
                GiveItem(m_Player, new EpicBookOfDestiny());
                GiveItem(m_Player, new EpicSwordOfVirtues());

                // Накатываем эволюцию характеристик
                m_Player.StatCap = CustomStatCap;
                m_Player.RawStr = Math.Min(MaxIndividualStat, m_Player.RawStr + StatBonusAmount);
                m_Player.RawDex = Math.Min(MaxIndividualStat, m_Player.RawDex + StatBonusAmount);
                m_Player.RawInt = Math.Min(MaxIndividualStat, m_Player.RawInt + StatBonusAmount);

                // Накатываем эволюцию лимитов магических навыков
                m_Player.Skills[SkillName.Magery].Cap = EpicSkillCap;
                m_Player.Skills[SkillName.EvalInt].Cap = EpicSkillCap;

                m_Player.SendMessage(0x35, $"Поздравляем! Вы получили ранг Аватара! Лимиты Str/Dex/Int повышены, общий Stat Cap поднят до {CustomStatCap}. Навыки Magery и EvalInt теперь могут быть развиты до {EpicSkillCap}!");
                Effects.PlaySound(m_Player.Location, m_Player.Map, 0x5C9); 
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

            private static void GiveItem(PlayerMobile player, Item item)
            {
                if (player.Backpack == null || !player.AddToBackpack(item))
                {
                    item.MoveToWorld(player.Location, player.Map);
                    player.SendMessage(0x22, $"В рюкзаке нет места. Предмет [{item.Name}] положен у ваших ног.");
                }
            }
        }
    }
}
