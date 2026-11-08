using System;
using Server;
using Server.Mobiles;
using Server.Regions;

namespace Server.Custom.DungeonDismount
{
    public static class DungeonDismountSystem
    {
        public static void Initialize()
        {
            EventSink.Movement += OnPlayerMovement;
            Timer.DelayCall(TimeSpan.FromSeconds(1.0), TimeSpan.FromSeconds(1.0), CheckMountedPlayers);
        }

        private static void OnPlayerMovement(MovementEventArgs e)
        {
            TryDismount(e.Mobile as PlayerMobile);
        }

        private static void CheckMountedPlayers()
        {
            foreach (Server.Network.NetState state in Server.Network.NetState.Instances)
            {
                if (state != null)
                    TryDismount(state.Mobile as PlayerMobile);
            }
        }

        private static void TryDismount(PlayerMobile player)
        {
            if (player == null || player.Deleted || !player.Alive || player.AccessLevel > AccessLevel.Player ||
                player.Region == null || !player.Region.IsPartOf(typeof(DungeonRegion)) ||
                !player.Mounted || player.Mount == null)
            {
                return;
            }

            IMount mount = player.Mount;
            BaseMount.Dismount(player);

            player.PlaySound(0x524);
            player.SendMessage(0x22, "[Данж]: Сырость и узкие каменные своды пугают животное! Перемещаться верхом в подземельях запрещено.");

            BaseCreature pet = mount as BaseCreature;

            if (pet != null && pet.ControlMaster == player)
            {
                pet.ControlTarget = player;
                pet.ControlOrder = OrderType.Follow;
            }
        }
    }
}
