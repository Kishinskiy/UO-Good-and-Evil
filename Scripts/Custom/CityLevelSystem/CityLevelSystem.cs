using System;
using System.Collections.Generic;
using System.IO;
using Server;
using Server.Engines.CityLoyalty; // Подключаем ванильную систему лояльности

namespace Server.Custom.CityLevels
{
    public class CityLevelData
    {
        public City City { get; set; }
        public double SkillValue { get; set; } // Текущий уровень (0.0 - 100.0)
        public double SkillCap { get; set; } = 100.0;

        public CityLevelData(City city)
        {
            City = city;
            SkillValue = 0.0;
        }
    }

    public static class CityLevelSystem
    {
        private static readonly string FilePath = Path.Combine("Saves/Custom", "CityLevels.bin");
        
        // Используем ванильный enum City как ключ словаря
        public static Dictionary<City, CityLevelData> Cities { get; private set; } = new Dictionary<City, CityLevelData>();

        public static void Configure()
        {
            EventSink.WorldSave += OnSave;
            EventSink.WorldLoad += OnLoad;
        }

        // Инициализация всех ванильных городов при первом запуске
        public static void Initialize()
        {
            foreach (City city in Enum.GetValues(typeof(City)))
            {
                if (!Cities.ContainsKey(city))
                {
                    Cities[city] = new CityLevelData(city);
                }
            }
        }

        public static CityLevelData GetCity(City city)
        {
            if (!Cities.TryGetValue(city, out var data))
            {
                data = new CityLevelData(city);
                Cities[city] = data;
            }
            return data;
        }

        // Метод прокачки города с автоматическим обновлением тултипа камня
        public static void GainCitySkill(City city, double amount)
        {
            var data = GetCity(city);
            if (data == null || data.SkillValue >= data.SkillCap)
                return;

            double oldValue = data.SkillValue;
            data.SkillValue = Math.Min(data.SkillCap, Math.Round(data.SkillValue + amount, 1));

            // Если значение изменилось, обновляем тултип ванильного городского камня
            if (data.SkillValue != oldValue)
            {
                UpdateCityStoneTooltip(city);
            }
        }

        // Находим ванильный CityStone и сбрасываем кэш тултипа
        public static void UpdateCityStoneTooltip(City city)
        {
            var loyaltySystem = CityLoyaltySystem.GetCityInstance(city);
            if (loyaltySystem != null && loyaltySystem.Stone != null)
            {
                loyaltySystem.Stone.InvalidateProperties();
            }
        }

        private static void OnSave(WorldSaveEventArgs e)
        {
            Persistence.Serialize(FilePath, writer =>
            {
                writer.Write(0); // Версия
                writer.Write(Cities.Count);

                foreach (var kvp in Cities)
                {
                    writer.Write((int)kvp.Key); // Сохраняем enum как int
                    writer.Write(kvp.Value.SkillValue);
                    writer.Write(kvp.Value.SkillCap);
                }
            });
        }

        private static void OnLoad()
        {
            Persistence.Deserialize(FilePath, reader =>
            {
                int version = reader.ReadInt();
                int count = reader.ReadInt();

                Cities.Clear();
                for (int i = 0; i < count; i++)
                {
                    City city = (City)reader.ReadInt();
                    double val = reader.ReadDouble();
                    double cap = reader.ReadDouble();

                    Cities[city] = new CityLevelData(city)
                    {
                        SkillValue = val,
                        SkillCap = cap
                    };
                }
            });
        }
    }
}
