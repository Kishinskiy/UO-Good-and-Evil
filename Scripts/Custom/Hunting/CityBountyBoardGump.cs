using System;
using System.IO;
using System.Xml;
using System.Collections.Generic;
using System.Linq;
using Server;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;
using Server.Misc;
using Server.Network;
using Server.Engines.CityLoyalty;
using Server.Engines.Points;
using Server.Custom.CityLevels;
using Server.Custom.WeaponLeveling;
using LoyaltyRating = Server.Engines.CityLoyalty.LoyaltyRating;


namespace Server.Custom.CityHunting
{
	public class CityBountyBoardGump : Gump
    {
        private readonly PlayerMobile _player;
        private readonly CityBountyBoard _board;
        private readonly City _city;
        private readonly List<BoardPosting> _visiblePostings;

        public CityBountyBoardGump(PlayerMobile pm, CityBountyBoard board, City city) : base(100, 50)
        {
            _player = pm;
            _board = board;
            _city = city;

            int playerMaxTier = CityHuntingSystem.GetAvailableTier(pm, city);

            // Фильтруем только доступные игроку контракты (недоступные не отображаются) и берём ровно 5 штук
            _visiblePostings = board.GetPostings()
                .Where(p => p.Tier <= playerMaxTier)
                .OrderByDescending(p => p.Tier)
                .Take(5)
                .ToList();

            var completed = pm.Backpack?.Items
                .OfType<HuntingContract>()
                .FirstOrDefault(c => c.Owner == pm && c.City == city && c.IsCompleted);

            // Динамическая высота окна под 5 карточек
            int totalHeight = (completed != null ? 135 : 85) + Math.Max(1, _visiblePostings.Count) * 72 + 15;

            AddPage(0);
            AddBackground(0, 0, 520, totalHeight, 9200);
            AddAlphaRegion(10, 10, 500, totalHeight - 20);

            AddHtml(20, 15, 480, 25, $"<CENTER><BASEFONT COLOR=#FFD700>ДОСКА ОБЪЯВЛЕНИЙ ГОРОДА {city.ToString().ToUpper()}</BASEFONT></CENTER>", false, false);
            AddLabel(25, 42, 0x59, $"Ваш доступный ранг контрактов в этом городе: Tier {playerMaxTier}");

            if (completed != null)
            {
                AddImageTiled(20, 68, 480, 45, 9354);
                AddLabel(30, 72, 0x35, $"ВЫПОЛНЕН КОНТРАКТ: {completed.TargetDisplayName} ({completed.CurrentKills}/{completed.RequiredKills})");
                AddButton(360, 76, 4005, 4007, 999, GumpButtonType.Reply, 0);
                AddLabel(395, 78, 0x59, "Получить награду");
            }

            int y = completed != null ? 125 : 75;

            if (_visiblePostings.Count == 0)
            {
                AddHtml(30, y + 15, 460, 40, "<BASEFONT COLOR=#FFFFFF>Все доступные для вашего ранга контракты уже разобраны охотниками. Новые объявления появятся позже!</BASEFONT>", false, false);
                return;
            }

            for (int i = 0; i < _visiblePostings.Count; i++)
            {
                BoardPosting post = _visiblePostings[i];

                AddImageTiled(20, y, 480, 65, 9354);

                string tierLabel = post.Target.IsBoss
	                ? $"<BASEFONT COLOR=#FF4444>[БОСС - Tier {post.Tier}]</BASEFONT>"
	                : $"<BASEFONT COLOR=#FFD700>[Tier {post.Tier}]</BASEFONT>";


                AddHtml(30, y + 6, 320, 20, $"{tierLabel} <BASEFONT COLOR=#FFFFFF>{post.Target.DisplayName}</BASEFONT>", false, false);
                AddLabel(30, y + 26, 0x3B2, $"Требуется убить: {post.RequiredKills} шт. | Награда: {post.GoldReward} золота");
                AddLabel(30, y + 44, 0x59, "Доступно для вас (срок 24 часа)");

                AddButton(375, y + 22, 4005, 4007, 10 + i, GumpButtonType.Reply, 0);
                AddLabel(410, y + 24, 0x35, "Взять заказ");

                y += 72;
            }
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            if (_board == null || _board.Deleted || !_player.InRange(_board.Location, 4))
                return;

            // Кнопка сдачи выполненного контракта (ID 999)
            if (info.ButtonID == 999)
            {
                var completed = _player.Backpack?.Items
                    .OfType<HuntingContract>()
                    .FirstOrDefault(c => c.Owner == _player && c.City == _city && c.IsCompleted);

                if (completed != null)
                {
                    CityHuntingSystem.CompleteContractReward(_player, completed);
                }
                return;
            }

            // Кнопки взятия контракта с доски (ID 10 + index)
            if (info.ButtonID >= 10 && info.ButtonID < 10 + _visiblePostings.Count)
            {
                int idx = info.ButtonID - 10;
                BoardPosting post = _visiblePostings[idx];

                // Проверяем, не забрал ли этот контракт другой игрок, пока гамп был открыт
                if (!_board.GetPostings().Contains(post))
                {
                    _player.SendMessage(0x22, "Этот контракт только что забрал другой охотник!");
                    return;
                }

                var active = _player.Backpack?.Items
                    .OfType<HuntingContract>()
                    .FirstOrDefault(c => c.Owner == _player && !c.IsExpired);

                if (active != null)
                {
                    _player.SendMessage(0x22, $"У вас уже есть активный контракт на [{active.TargetDisplayName}] ({active.CurrentKills}/{active.RequiredKills})!");
                    return;
                }

                int playerMaxTier = CityHuntingSystem.GetAvailableTier(_player, _city);
                if (playerMaxTier < post.Tier)
                {
                    _player.SendMessage(0x22, "Вашей славы, репутации или уровня города недостаточно для этого контракта.");
                    return;
                }

                HuntingContract contract = new HuntingContract(_player, _city, post.Target, post.RequiredKills, post.Tier);
                _player.AddToBackpack(contract);
                _board.RemovePosting(post);

                _player.PlaySound(0x249);
                _player.SendMessage(0x35, $"Вы сорвали с доски контракт города {_city}: {post.Target.DisplayName} ({post.RequiredKills} шт.). У вас есть 24 часа!");
            }
        }
    }

}
