using System;
using System.Collections.Generic;
using Server;
using Server.Gumps;
using Server.Network;
using Server.Mobiles;
using Server.Items;

namespace Server.Custom.MercenarySystem
{
    public class MercenaryBuybackGump : Gump
    {
        private const int PageSize = 5;
        private readonly MercenaryCaptain m_Captain;
        private readonly PlayerMobile m_Player;
        private readonly List<FallenMercenaryData> m_FallenList;

        public MercenaryBuybackGump(MercenaryCaptain captain, PlayerMobile from, List<FallenMercenaryData> list) : base(150, 100)
        {
            m_Captain = captain;
            m_Player = from;
            m_FallenList = list;

            AddPage(0);
            int pageCount = Math.Max(1, (list.Count + PageSize - 1) / PageSize);

            for (int page = 0; page < pageCount; page++)
            {
                AddPage(page + 1);
                AddBackground(0, 0, 380, 300, 0x13BE);
                AddAlphaRegion(10, 10, 360, 280);

                AddLabel(40, 20, 0x35, "Выкуп павших Наёмников из Эфира");
                AddImageTiled(30, 45, 320, 2, 0x2424);

                int firstIndex = page * PageSize;
                int lastIndex = Math.Min(firstIndex + PageSize, list.Count);
                int y = 60;

                for (int i = firstIndex; i < lastIndex; i++)
                {
                    FallenMercenaryData data = list[i];
                    int buybackCost = MercenaryContract.GetResurrectionPrice(data.Class, data.Level);

                    AddButton(25, y + 2, 0x15E1, 0x15E5, i + 1, GumpButtonType.Reply, 0);
                    AddLabel(60, y, 0x58, $"{data.MercName} ({data.Class})");
                    AddLabel(230, y, 0x3F, $"Ур. {data.Level} [{buybackCost}gp]");

                    y += 35;
                }

                if (page > 0)
                    AddButton(250, 250, 0x15E3, 0x15E7, page, GumpButtonType.Page, 0);

                AddButton(25, 250, 0xFB1, 0xFB2, 0, GumpButtonType.Reply, 0);
                AddLabel(60, 252, 0x22, "Закрыть меню");
                AddButton(110, 250, 0xF7, 0xF8, list.Count + 1, GumpButtonType.Reply, 0);
                AddLabel(145, 252, 0x35, "Купить контракт");

                if (page + 1 < pageCount)
                    AddButton(345, 250, 0x15E1, 0x15E5, page + 2, GumpButtonType.Page, 0);

                if (pageCount > 1)
                    AddLabel(285, 252, 0x35, $"{page + 1}/{pageCount}");
            }
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            if (m_Player == null || m_Player.Deleted || !m_Player.Alive)
                return;

            if (info.ButtonID == m_FallenList.Count + 1)
            {
                if (m_Captain != null && !m_Captain.Deleted && m_Player.InRange(m_Captain.Location, 2))
                    m_Captain.OpenShop(m_Player);
                else
                    m_Player.SendMessage("Подойдите ближе к капитану наёмников, чтобы купить контракт.");

                return;
            }

            if (info.ButtonID == 0)
                return;

            int index = info.ButtonID - 1;
            if (index >= 0 && index < m_FallenList.Count)
            {
                FallenMercenaryData data = m_FallenList[index];
                List<FallenMercenaryData> currentRoster = MercenaryRescueSystem.GetFallenMercenaries(m_Player);

                if (currentRoster == null || !currentRoster.Contains(data))
                {
                    m_Player.SendMessage(0x22, "Этот наёмник уже не ожидает воскрешения.");
                    return;
                }

                if (m_Player.Followers + 2 > m_Player.FollowersMax)
                {
                    m_Player.SendMessage(0x22, "Недостаточно свободных слотов контроля для возвращения наёмника.");
                    return;
                }

                int cost = MercenaryContract.GetResurrectionPrice(data.Class, data.Level);

                if (Server.Mobiles.Banker.Withdraw(m_Player, cost))
                {
                    m_Player.PlaySound(0x2E6);

                    BaseMercenary restoredMerc = new BaseMercenary(data.Class);
                    restoredMerc.RestoreAppearance(data.RaceIndex, data.Female);
                    restoredMerc.Name = data.MercName;

                    restoredMerc.RestoreProgression(data.Level, data.Exp, data.NextLevelExperience);

                    foreach (Item item in data.EquippedItems)
                    {
                        if (item != null && !item.Deleted)
                        {
                            item.Movable = true;
                            restoredMerc.AddItem(item);
                        }
                    }

                    restoredMerc.MoveToWorld(m_Player.Location, m_Player.Map);
                    restoredMerc.SetControlMaster(m_Player);
                    restoredMerc.ControlTarget = m_Player;
                    restoredMerc.ControlOrder = OrderType.Follow;

                    MercenaryRescueSystem.RemoveFromRoster(m_Player, data);

                    m_Player.SendMessage(0x35, $"Вы заплатили {cost} золотых. Наёмник {data.MercName} вернулся в отряд с прежним уровнем и экипировкой.");
                }
                else
                {
                    m_Player.SendMessage(0x22, $"В банке недостаточно золота: для воскрешения нужно {cost} золотых.");
                }
            }
        }
    }
}
