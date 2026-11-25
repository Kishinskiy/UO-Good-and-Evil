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

        // Внутри класса CityBountyBoard:
        public void RefreshPostingsIfNeeded()
        {
	        if (DateTime.UtcNow < _nextRefresh && _postings.Count > 0)
		        return;

	        _postings.Clear();
	        _nextRefresh = DateTime.UtcNow.AddHours(2.0); // Обновление доски каждые 2 часа

	        // Генерируем по 5 объявлений для каждого ранга (1..4), чтобы игроку любого уровня всегда хватало 5 доступных контрактов
	        int maxTier = CityHuntingSystem.MaxTier;
	        for (int tier = 1; tier <= maxTier; tier++)
	        {
		        for (int i = 0; i < 5; i++)
		        {
			        var posting = CreateRandomPostingForTier(tier);
			        if (posting != null)
				        _postings.Add(posting);
		        }
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
}
