using System;
using Server;
using Server.Mobiles;

namespace Server.Custom.VirtueQuests
{
    public static class LoreTomeDropMod
    {
        public static void Initialize()
        {
            EventSink.CreatureDeath += OnCreatureDeath;
        }

        private static void OnCreatureDeath(CreatureDeathEventArgs e)
        {
            if (e.Creature is BaseChampion || e.Creature is BasePeerless)
            {
                // 50% шанс выпадения неизученного фолианта с Чемпиона или Пирлесс-босса
                if (e.Corpse != null && Utility.RandomDouble() < 0.50)
                {
                    e.Corpse.DropItem(new AncientLoreTome());
                }
            }
        }
    }
}
