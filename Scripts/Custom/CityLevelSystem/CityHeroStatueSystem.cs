using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Linq;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.Engines.CityLoyalty;

namespace Server.Custom.CityLevels
{
    // Специальный маркер-спавнер (аналог точки XML Spawner), который ГМ ставит в любом месте города
    public class CityStatueSpawner : Item
    {
        private City _city;

        [CommandProperty(AccessLevel.GameMaster)]
        public City City
        {
            get => _city;
            set
            {
                _city = value;
                Name = $"Маркер статуи героя ({_city})";
                InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool HasStatue { get; set; }

        [Constructable]
        public CityStatueSpawner() : this(City.Britain)
        {
        }

        [Constructable]
        public CityStatueSpawner(City city) : base(0x18DB) // Вид стандартного спавнера (невидим для игроков)
        {
            Visible = false; // Игроки не видят сам маркер, видят только ГМы
            Movable = false;
            City = city;
            CityHeroStatueSystem.RegisterSpawner(this);
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);
            list.Add(1060658, "Город\t{0}", _city);
            list.Add(1060659, "Статуя возведена\t{0}", HasStatue ? "Да" : "Нет (ожидает героя)");
        }

        public override void OnDelete()
        {
            CityHeroStatueSystem.UnregisterSpawner(this);
            base.OnDelete();
        }

        public CityStatueSpawner(Serial serial) : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0);
            writer.Write((int)_city);
            writer.Write(HasStatue);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
            _city = (City)reader.ReadInt();
            HasStatue = reader.ReadBool();

            CityHeroStatueSystem.RegisterSpawner(this);
        }
    }

    public static class CityHeroStatueSystem
    {
        private static readonly string XmlConfigPath = Path.Combine(Core.BaseDirectory, "Data/Custom/CityStatueSpawns.xml");

        public static List<CityStatueSpawner> Spawners { get; private set; } = new List<CityStatueSpawner>();

        public static void RegisterSpawner(CityStatueSpawner spawner)
        {
            if (spawner != null && !Spawners.Contains(spawner))
                Spawners.Add(spawner);
        }

        public static void UnregisterSpawner(CityStatueSpawner spawner)
        {
            if (spawner != null)
                Spawners.Remove(spawner);
        }

        public static void Initialize()
        {
            LoadFromXml();
            Timer.DelayCall(TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(30), CheckOnlineDemigods);
        }

        public static void LoadFromXml()
        {
            if (!File.Exists(XmlConfigPath))
            {
                Console.WriteLine($"[CityHeroStatue] XML файл не найден: {XmlConfigPath}");
                return;
            }

            try
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(XmlConfigPath);

                XmlNodeList nodes = doc.GetElementsByTagName("Spawn");
                int created = 0;

                foreach (XmlNode node in nodes)
                {
                    if (node.Attributes == null)
                        continue;

                    string cityStr = node.Attributes["city"]?.Value;
                    string mapStr = node.Attributes["map"]?.Value ?? "Trammel";
                    string dirStr = node.Attributes["direction"]?.Value ?? "South";

                    if (!Enum.TryParse(cityStr, true, out City city) || !Enum.IsDefined(typeof(City), city))
                        continue;

                    Map map = Map.Parse(mapStr);
                    if (map == null || map == Map.Internal)
                        continue;

                    if (!int.TryParse(node.Attributes["x"]?.Value, out int x) ||
                        !int.TryParse(node.Attributes["y"]?.Value, out int y) ||
                        !int.TryParse(node.Attributes["z"]?.Value, out int z))
                    {
                        Console.WriteLine($"[CityHeroStatue] Некорректные координаты для города {city} в XML.");
                        continue;
                    }

                    if (!Enum.TryParse(dirStr, true, out Direction dir) || !Enum.IsDefined(typeof(Direction), dir))
                        dir = Direction.South;

                    var existing = Spawners.FirstOrDefault(s => !s.Deleted && s.City == city && s.Map == map);
                    if (existing != null)
                    {
                        if (!existing.HasStatue)
                        {
                            existing.Direction = dir;
                            existing.MoveToWorld(new Point3D(x, y, z), map);
                        }
                        continue;
                    }

                    CityStatueSpawner spawner = new CityStatueSpawner(city)
                    {
                        Direction = dir
                    };
                    spawner.MoveToWorld(new Point3D(x, y, z), map);
                    created++;
                }

                if (created > 0)
                    Console.WriteLine($"[CityHeroStatue] Загружено из XML новых точек под статуи: {created}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CityHeroStatue] Ошибка чтения XML: {ex.Message}");
            }
        }
        private static void CheckOnlineDemigods()
        {
            if (!CityLoyaltySystem.Enabled || Spawners.Count == 0)
                return;

            foreach (PlayerMobile pm in PlayerMobile.Instances)
            {
                if (pm == null || pm.Deleted || pm.NetState == null)
                    continue;

                // 1. Статус Полубога
                if (!pm.IsDemigod && !pm.EpicGood && !pm.EpicEvil)
                    continue;

                // Определяем город, в котором находится игрок
                City? currentCity = CityLevelProgression.GetCityAtLocation(pm.Location, pm.Map, allowRadius: false);
                if (!currentCity.HasValue)
                    continue;

                TryErectStatue(pm, currentCity.Value);
            }
        }

        public static void TryErectStatue(PlayerMobile pm, City city)
        {
            // Ищем свободный маркер-спавнер для этого города (на той же карте, где находится игрок)
            var spawner = Spawners.FirstOrDefault(s =>
                s != null && !s.Deleted && !s.HasStatue && s.City == city && s.Map == pm.Map);

            if (spawner == null)
                return;

            // 2. Максимальный уровень города (100.0)
            var levelData = CityLevelSystem.GetCity(city);
            if (levelData == null || levelData.SkillValue < levelData.SkillCap)
                return;

            // 3. Максимальная репутация игрока в городе (Adored)
            var loyaltySys = CityLoyaltySystem.GetCityInstance(city);
            if (loyaltySys == null)
                return;

            var entry = loyaltySys.GetPlayerEntry<CityLoyaltyEntry>(pm);
            if (loyaltySys.GetLoyaltyRating(pm, entry) < LoyaltyRating.Adored)
                return;

            // Все условия выполнены — ставим статую ровно на координаты маркер-спавнера!
            ErectStatueAtSpawner(pm, spawner);
        }

        private static void ErectStatueAtSpawner(PlayerMobile pm, CityStatueSpawner spawner)
        {
            Point3D loc = spawner.Location;
            Map map = spawner.Map;

            // Создаём статую персонажа (CharacterStatue в ServUO наследуется от Mobile)
            CharacterStatue statue = new CharacterStatue(pm.Serial);
            CharacterStatuePlinth plinth = new CharacterStatuePlinth(statue);

            statue.Pose = pm.EpicEvil ? StatuePose.Casting : StatuePose.Ready;
            statue.Material = pm.EpicEvil ? StatueMaterial.Dark : StatueMaterial.Light;
            statue.Plinth = plinth;
            statue.Direction = spawner.Direction != Direction.North ? spawner.Direction : Direction.South;
            statue.IsRewardItem = false;
            statue.Frozen = true;
            statue.Blessed = true;

            plinth.Movable = false;

            string title = pm.EpicEvil ? "Владыка Тьмы" : (pm.EpicGood ? "Аватар Света" : "Полубог");
            statue.Name = $"{title} {pm.Name}";
            statue.Title = $"Покровитель города {spawner.City}";

            // Ставим постамент в точку спавнера, а статую — прямо на него (Z + 5)
            plinth.MoveToWorld(loc, map);
            statue.MoveToWorld(new Point3D(loc.X, loc.Y, loc.Z + 5), map);
            statue.InvalidatePose();

            spawner.HasStatue = true;

            Effects.SendLocationEffect(loc, map, 0x373A, 30, 10, pm.EpicEvil ? 0x497 : 0x5D, 0);
            Effects.PlaySound(loc, map, 0x5C9);

            World.Broadcast(
                pm.EpicEvil ? 0x22 : 0x35,
                true,
                $"[Легенда Британии] В центре города {spawner.City} воздвигнута статуя в честь {title} {pm.Name}!"
            );
        }
    }
}
