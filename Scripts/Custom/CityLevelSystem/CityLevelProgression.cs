using System;
using Server;
using Server.Mobiles;
using Server.Regions;
using Server.Engines.CityLoyalty;
using System.Collections.Generic;
using Server.Custom.KarmaVirtueSystem;



namespace Server.Custom.CityLevels
{
    public static class CityLevelProgression
    {
        // Настройки прокачки
        private const double GainStep = 0.1; // На сколько растет скилл города за один прок
        private const int ProtectionRadius = 250; // Радиус клеток вокруг городов


        // словарь с координатами городов для прокачки
        private static readonly Dictionary<City, Point2D> CityCenters = new Dictionary<City, Point2D>
        {
            { City.Britain,     new Point2D(1495, 1629) },
            { City.Minoc,       new Point2D(2466, 544) },
            { City.Yew,         new Point2D(546, 992) },
            { City.Moonglow,    new Point2D(4442, 1172) },
            { City.NewMagincia, new Point2D(3714, 2220) },
            { City.Trinsic,     new Point2D(1867, 2780) },
            { City.SkaraBrae,   new Point2D(632, 2233) },
            { City.Jhelom,      new Point2D(1383, 3815) },
            { City.Vesper,      new Point2D(2899, 676) }
        };
        public static void Initialize()
        {
            // 1. Прокачка за крафт в городе
            EventSink.CraftSuccess += OnCraftSuccess;

            // 2. Прокачка за убийство монстров в регионе города (например, во время ночных осад)
            EventSink.CreatureDeath += OnCreatureDeath;
        }

        // Срабатывает при любом успешном крафте в мире
        private static void OnCraftSuccess(CraftSuccessEventArgs e)
        {
            Mobile crafter = e.Crafter;
            if (crafter == null || !crafter.Player)
                return;

            City? city = GetCityAtLocation(crafter.Location, crafter.Map);
            if (city.HasValue)
            {
                // Базовый шанс прокачки при крафте — 25% (уменьшается с ростом уровня города)
                TryGainCitySkill(city.Value, baseChance: 0.25, crafter);
            }
        }

        // Срабатывает при смерти любого существа в мире
        private static void OnCreatureDeath(CreatureDeathEventArgs e)
        {
            BaseCreature victim = e.Creature as BaseCreature;
            if (victim == null)
                return;

            // 1. Определяем реального игрока: либо сам убийца, либо хозяин питомца/наёмника/саммона
            Mobile killer = e.Killer;
            if (killer is BaseCreature bc)
                killer = bc.GetMaster();

            if (killer == null || !killer.Player)
                return;

            // 2. Игнорируем призванных, прирученных, неуязвимых и неагрессивных существ (мирных NPC, животных, гвардов)
            if (victim.Blessed || victim.Controlled || victim.Summoned || victim.Body.IsAnimal || !victim.IsAggressiveMonster)
                return;

            // 3. Проверяем регион самого монстра, а если он погиб чуть за стенами — регион игрока
            City? city = GetCityAtLocation(victim.Location, victim.Map) ?? GetCityAtLocation(killer.Location, killer.Map);

            if (city.HasValue)
            {
                // Шанс зависит от силы монстра (Fame): от 5% за слабых до 30% за боссов/личей
                double chance = Math.Min(0.30, Math.Max(0.05, victim.Fame / 20000.0));
                TryGainCitySkill(city.Value, chance, killer);
            }
        }


        // Главный метод проверки шанса (по аналогии со скиллами игрока в UO)
        public static void TryGainCitySkill(City city, double baseChance, Mobile contributor = null)
        {
            var data = CityLevelSystem.GetCity(city);
            if (data == null || data.SkillValue >= data.SkillCap)
                return;

            // 1. Чем ближе уровень города к 100.0, тем сложнее получить прирост
            double progressFactor = 1.0 - (data.SkillValue / (data.SkillCap + 10.0));

            // 2. Множитель от состояния алтарей Добродетелей, привязанных к этому городу
            double shrineMultiplier = GetShrineChanceMultiplier(city);

            double finalChance = baseChance * progressFactor * shrineMultiplier;

            if (Utility.RandomDouble() < finalChance)
            {
                CityLevelSystem.GainCitySkill(city, GainStep);

                if (contributor != null && contributor.Player)
                {
                    contributor.SendMessage(0x59, $"Ваш вклад помог развить город {city}! Текущий уровень: {data.SkillValue:F1}.");
                }
            }
        }

        // Получаем множитель шанса прокачки города на основе активных алтарей в KarmaVirtueManager
        private static double GetShrineChanceMultiplier(City city)
        {
            double multiplier = 1.0;

            string targetTownName;
            switch (city)
            {
                case City.NewMagincia: targetTownName = "Magincia"; break;
                case City.SkaraBrae: targetTownName = "Skara Brae"; break;
                default: targetTownName = city.ToString(); break;
            }

            foreach (ShrineData shrine in KarmaVirtueManager.Shrines)
            {
                if (string.Equals(shrine.TownRegionName, targetTownName, StringComparison.OrdinalIgnoreCase))
                {
                    int state = KarmaVirtueManager.GetShrineWorldState(shrine.ID);

                    if (state == 1) // Свет
                        multiplier *= KarmaVirtueManager.TownDevelopmentLightMultiplier;
                    else if (state == 2) // Тьма
                        multiplier *= KarmaVirtueManager.TownDevelopmentDarkMultiplier;
                }
            }

            return multiplier;
        }


        // Определяем город: сначала по точному региону, затем — по радиусу от центра (если allowRadius = true)
        public static City? GetCityAtLocation(Point3D loc, Map map, bool allowRadius = false)
        {
            if (map == null || map == Map.Internal)
                return null;

            Region reg = Region.Find(loc, map);

            // 1. Проверяем точное нахождение в регионе города
            if (reg != null)
            {
                // Если игрок или монстр в подземелье — радиус города не действует
                if (reg.IsPartOf(typeof(DungeonRegion)))
                    return null;

                Region townReg = reg.GetRegion(typeof(TownRegion)) ?? reg;
                string name = townReg.Name;

                if (!string.IsNullOrEmpty(name))
                {
                    foreach (City c in Enum.GetValues(typeof(City)))
                    {
                        string checkName = (c == City.NewMagincia) ? "Magincia" : c.ToString();
                        if (name.IndexOf(checkName, StringComparison.OrdinalIgnoreCase) >= 0)
                            return c;
                    }
                }
            }

            // 2. Если за стенами города (и включен поиск по радиусу) — ищем ближайший город в пределах ProtectionRadius
            if (allowRadius && (map == Map.Trammel || map == Map.Felucca))
            {
                City? nearestCity = null;
                double minDistance = ProtectionRadius;

                foreach (var kvp in CityCenters)
                {
                    double dist = Utility.GetDistanceToSqrt(loc, new Point3D(kvp.Value.X, kvp.Value.Y, loc.Z));
                    if (dist <= minDistance)
                    {
                        minDistance = dist;
                        nearestCity = kvp.Key;
                    }
                }

                return nearestCity;
            }

            return null;
        }

    }
}
