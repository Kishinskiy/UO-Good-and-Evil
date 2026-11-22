using System;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.Engines.CannedEvil; // Для BaseChampion

namespace Server.Custom.WeaponLeveling
{
    public static class WeaponScrollLoot
    {
        // Настройки шансов выпадения
        private const double ChampionDropChance = 0.25; // 25% с Чемпионов и Peerless-боссов
        private const double HighBossDropChance = 0.05; // 5% с Парагонов и элитных монстров (Fame >= 15000)

        public static void Initialize()
        {
            EventSink.CreatureDeath += OnCreatureDeath;
        }

        private static void OnCreatureDeath(CreatureDeathEventArgs e)
        {
            BaseCreature victim = e.Creature as BaseCreature;
            Container corpse = e.Corpse;

            if (victim == null || corpse == null || corpse.Deleted)
                return;

            // Игнорируем призванных и прирученных существ
            if (victim.Summoned || victim.Controlled || victim.NoLootOnDeath)
                return;

            double dropChance = 0.0;

            // 1. Чемпионы (BaseChampion) и Пирлесс-боссы (BasePeerless)
            if (victim is BaseChampion || victim is BasePeerless)
            {
                dropChance = ChampionDropChance;
            }
            // 2. Парагоны или очень сильные монстры (Драконы, Балроны, Древние Личи и т.д.)
            else if (victim.IsParagon || victim.Fame >= 15000)
            {
                dropChance = HighBossDropChance;
            }

            if (dropChance > 0 && Utility.RandomDouble() < dropChance)
            {
                corpse.DropItem(new WeaponTemperingScroll());

                // Опционально: уведомляем игрока, нанёсшего больше всего урона
                Mobile topDamager = victim.GetHighestDamager();
                if (topDamager != null && topDamager.Player)
                {
                    topDamager.SendMessage(0x35, "В останках поверженного чудовища блеснул Свиток древней закалки оружия!");
                }
            }
        }
    }
}
