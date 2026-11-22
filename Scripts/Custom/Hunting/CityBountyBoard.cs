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
    public class BoardPosting
    {
        public HuntTargetDef Target { get; set; }
        public int RequiredKills { get; set; }
        public int Tier { get; set; }
        public int GoldReward { get; set; }
    }

    public class CityBountyBoard : Item
    {
        private List<BoardPosting> _postings = new List<BoardPosting>();
        private DateTime _nextRefresh = DateTime.MinValue;

        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime NextRefresh
        {
            get => _nextRefresh;
            set => _nextRefresh = value;
        }

        [Constructable]
        public CityBountyBoard() : base(0x1E5E) // ID классической городской доски объявлений
        {
            Name = "Городская доска объявлений и розыска";
            Movable = false;
            AllBoards.Add(this);
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);

            City? city = CityLevelProgression.GetCityAtLocation(Location, Map, allowRadius: false);
            if (city.HasValue)
            {
                list.Add(1060658, "Город\t{0}", city.Value);
            }

            RefreshPostingsIfNeeded();
            list.Add(1060659, "Активных объявлений\t{0}", _postings.Count);
        }

        public void RefreshPostingsIfNeeded()
        {
            if (DateTime.UtcNow < _nextRefresh && _postings.Count > 0)
                return;

            _postings.Clear();
            _nextRefresh = DateTime.UtcNow.AddHours(2.0); // Обновление доски каждые 2 часа

            // Генерируем 4 объявления разных рангов (от 1 до 4), чтобы на доске всегда был выбор
            for (int tier = 1; tier <= 4; tier++)
            {
                var posting = CreateRandomPostingForTier(tier);
                if (posting != null)
                    _postings.Add(posting);
            }

            InvalidateProperties();
        }

        private static BoardPosting CreateRandomPostingForTier(int tier)
        {
            // Используем временного фиктивного игрока или публичный генератор из CityHuntingSystem
            // Для простоты берём пулы напрямую через вспомогательный метод
            return CityHuntingSystem.CreateBoardPosting(tier);
        }

        public List<BoardPosting> GetPostings()
        {
            RefreshPostingsIfNeeded();
            return _postings;
        }

        public void RemovePosting(BoardPosting post)
        {
            _postings.Remove(post);
            InvalidateProperties();
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (!(from is PlayerMobile pm) || !pm.InRange(Location, 4))
            {
                from.SendLocalizedMessage(500446); // That is too far away.
                return;
            }

            City? city = CityLevelProgression.GetCityAtLocation(Location, Map, allowRadius: false);
            if (!city.HasValue)
            {
                pm.SendMessage(0x22, "Эта доска объявлений не привязана ни к одному городу.");
                return;
            }

            RefreshPostingsIfNeeded();
            pm.CloseGump(typeof(CityBountyBoardGump));
            pm.SendGump(new CityBountyBoardGump(pm, this, city.Value));
            pm.PlaySound(0x55);
        }

        public CityBountyBoard(Serial serial) : base(serial)
        {
        }
        private static readonly string XmlConfigPath = Path.Combine(Core.BaseDirectory, "Data/Custom/CityBountyBoards.xml");
        public static List<CityBountyBoard> AllBoards { get; } = new List<CityBountyBoard>();

        public static void Initialize()
        {
            LoadFromXml();
        }

        public static void LoadFromXml()
        {
            if (!File.Exists(XmlConfigPath))
            {
                Console.WriteLine($"[CityBountyBoard] XML файл не найден: {XmlConfigPath}");
                return;
            }

            try
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(XmlConfigPath);

                XmlNodeList nodes = doc.GetElementsByTagName("Board");
                int spawned = 0;

                foreach (XmlNode node in nodes)
                {
                    if (node.Attributes == null)
                        continue;

                    string mapStr = node.Attributes["map"]?.Value ?? "Trammel";
                    Map map = Map.Parse(mapStr);
                    if (map == null || map == Map.Internal)
                        continue;

                    if (!int.TryParse(node.Attributes["x"]?.Value, out int x) ||
                        !int.TryParse(node.Attributes["y"]?.Value, out int y) ||
                        !int.TryParse(node.Attributes["z"]?.Value, out int z))
                    {
                        continue;
                    }

                    string itemIdStr = node.Attributes["itemID"]?.Value ?? "0x1E5E";
                    int itemID = Utility.ToInt32(itemIdStr);

                    Point3D loc = new Point3D(x, y, z);

                    // Проверяем, нет ли уже заспавненной доски в радиусе 5 тайлов от этой точки (чтобы не плодить дубликаты при рестарте)
                    bool exists = AllBoards.Any(b => b != null && !b.Deleted && b.Map == map && Utility.InRange(b.Location, loc, 5));
                    if (exists)
                        continue;

                    CityBountyBoard board = new CityBountyBoard
                    {
                        ItemID = itemID > 0 ? itemID : 0x1E5E
                    };
                    board.MoveToWorld(loc, map);
                    spawned++;
                }

                if (spawned > 0)
                    Console.WriteLine($"[CityBountyBoard] Заспавнено из XML новых досок объявлений (Trammel/Felucca): {spawned}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CityBountyBoard] Ошибка чтения XML: {ex.Message}");
            }
        }
        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            reader.ReadInt();
            AllBoards.Add(this);
        }

        public override void OnDelete()
        {
            AllBoards.Remove(this);
            base.OnDelete();
        }

    }

    public class CityBountyBoardGump : Gump
    {
        private readonly PlayerMobile _player;
        private readonly CityBountyBoard _board;
        private readonly City _city;
        private readonly List<BoardPosting> _postings;

        public CityBountyBoardGump(PlayerMobile pm, CityBountyBoard board, City city) : base(100, 80)
        {
            _player = pm;
            _board = board;
            _city = city;
            _postings = board.GetPostings();

            int playerMaxTier = CityHuntingSystem.GetAvailableTier(pm, city);

            AddPage(0);
            AddBackground(0, 0, 520, 440, 9200);
            AddAlphaRegion(10, 10, 500, 420);

            AddHtml(20, 15, 480, 25, $"<CENTER><BASEFONT COLOR=#FFD700>ДОСКА ОБЪЯВЛЕНИЙ ГОРОДА {city.ToString().ToUpper()}</BASEFONT></CENTER>", false, false);
            AddLabel(25, 42, 0x59, $"Ваш доступный ранг контрактов в этом городе: Tier {playerMaxTier}");

            // Проверяем, есть ли у игрока выполненный контракт этого города для сдачи
            var completed = pm.Backpack?.Items
                .OfType<HuntingContract>()
                .FirstOrDefault(c => c.Owner == pm && c.City == city && c.IsCompleted);

            if (completed != null)
            {
                AddImageTiled(20, 68, 480, 45, 9354);
                AddLabel(30, 72, 0x35, $"ВЫПОЛНЕН КОНТРАКТ: {completed.TargetDisplayName} ({completed.CurrentKills}/{completed.RequiredKills})");
                AddButton(360, 76, 4005, 4007, 999, GumpButtonType.Reply, 0);
                AddLabel(395, 78, 0x59, "Получить награду");
            }

            int y = completed != null ? 125 : 75;

            if (_postings.Count == 0)
            {
                AddHtml(30, y + 20, 460, 40, "<BASEFONT COLOR=#FFFFFF>Все контракты на этой доске уже разобраны охотниками. Новые объявления появятся позже!</BASEFONT>", false, false);
                return;
            }

            for (int i = 0; i < _postings.Count; i++)
            {
                BoardPosting post = _postings[i];
                bool canTake = playerMaxTier >= post.Tier;

                AddImageTiled(20, y, 480, 65, 9354);

                string tierLabel = post.Target.IsBoss ? "<BASEFONT COLOR=#FF4444>[БОСС - Tier 4]</BASEFONT>" : $"<BASEFONT COLOR=#FFD700>[Tier {post.Tier}]</BASEFONT>";
                AddHtml(30, y + 6, 300, 20, $"{tierLabel} <BASEFONT COLOR=#FFFFFF>{post.Target.DisplayName}</BASEFONT>", false, false);
                AddLabel(30, y + 26, 0x3B2, $"Требуется убить: {post.RequiredKills} шт. | Награда: {post.GoldReward} золота");
                AddLabel(30, y + 44, canTake ? 0x59 : 0x22, canTake ? "Доступно для вас (срок 24 часа)" : $"Требуется ранг Tier {post.Tier} (выше слава/лояльность/уровень города)");

                if (canTake)
                {
                    AddButton(375, y + 22, 4005, 4007, 10 + i, GumpButtonType.Reply, 0);
                    AddLabel(410, y + 24, 0x35, "Взять заказ");
                }

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
            if (info.ButtonID >= 10 && info.ButtonID < 10 + _postings.Count)
            {
                int idx = info.ButtonID - 10;
                BoardPosting post = _postings[idx];

                // Проверяем, нет ли уже активного контракта
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

                // Создаём свиток контракта, удаляем объявление с доски и кладём свиток в рюкзак
                HuntingContract contract = new HuntingContract(_player, _city, post.Target, post.RequiredKills, post.Tier);
                _player.AddToBackpack(contract);
                _board.RemovePosting(post);

                _player.PlaySound(0x249);
                _player.SendMessage(0x35, $"Вы сорвали с доски контракт города {_city}: {post.Target.DisplayName} ({post.RequiredKills} шт.). У вас есть 24 часа!");
            }
        }
    }
}