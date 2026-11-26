using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Server;
using Server.Commands;
using Server.Mobiles;

namespace Server.Engines.Quests
{
    public class MonsterRankEntry
    {
        public int Level { get; set; }
        public int MinThreat { get; set; }
        public int Points { get; set; }
    }

    public static class RankSystemHelper
    {
        private static readonly string ConfigPath = Path.Combine(Core.BaseDirectory, "Config", "Rank.cfg");

        public static int MaxRankBelow { get; private set; } = 2;
        public static int MaxRankAbove { get; private set; } = 2;
        public static double WeakMonsterMultiplier { get; private set; } = 0.5;

        public static List<MonsterRankEntry> MonsterRanks { get; private set; } = new List<MonsterRankEntry>();
        public static Dictionary<int, int> PlayerRankRequirements { get; private set; } = new Dictionary<int, int>();

        public static void Configure()
        {
            CommandSystem.Register("ReloadRankConfig", AccessLevel.Administrator, e =>
            {
                LoadConfig();
                e.Mobile.SendMessage(0x35, "Конфигурация Config/Rank.cfg успешно перезагружена.");
            });

            LoadConfig();
        }

        public static void LoadConfig()
        {
            LoadDefaults();

            if (!File.Exists(ConfigPath))
            {
                Console.WriteLine($"[GuildRankSystem] Файл {ConfigPath} не найден! Используются значения по умолчанию.");
                return;
            }

            try
            {
                Dictionary<string, string> values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                foreach (string rawLine in File.ReadAllLines(ConfigPath))
                {
                    string line = rawLine.Trim();
                    if (string.IsNullOrEmpty(line) || line.StartsWith("#") || line.StartsWith("//"))
                        continue;

                    int eqIndex = line.IndexOf('=');
                    if (eqIndex <= 0)
                        continue;

                    string key = line.Substring(0, eqIndex).Trim();
                    string val = line.Substring(eqIndex + 1).Trim();
                    values[key] = val;
                }

                if (values.TryGetValue("MaxRankBelow", out string belowStr) && int.TryParse(belowStr, out int below))
                    MaxRankBelow = below;

                if (values.TryGetValue("MaxRankAbove", out string aboveStr) && int.TryParse(aboveStr, out int above))
                    MaxRankAbove = above;

                if (values.TryGetValue("WeakMonsterMultiplier", out string multStr) &&
                    double.TryParse(multStr, NumberStyles.Any, CultureInfo.InvariantCulture, out double mult))
                    WeakMonsterMultiplier = mult;

                for (int level = 1; level <= 10; level++)
                {
                    var entry = MonsterRanks.FirstOrDefault(r => r.Level == level);
                    if (entry == null)
                    {
                        entry = new MonsterRankEntry { Level = level };
                        MonsterRanks.Add(entry);
                    }

                    if (values.TryGetValue($"MonsterRank.{level}.MinThreat", out string threatStr) && int.TryParse(threatStr, out int threat))
                        entry.MinThreat = threat;

                    if (values.TryGetValue($"MonsterRank.{level}.Points", out string ptsStr) && int.TryParse(ptsStr, out int pts))
                        entry.Points = pts;
                }

                MonsterRanks = MonsterRanks.OrderBy(r => r.Level).ToList();

                for (int fromRank = 2; fromRank <= 11; fromRank++)
                {
                    if (values.TryGetValue($"PlayerRank.{fromRank}.RequiredPoints", out string reqStr) && int.TryParse(reqStr, out int req))
                        PlayerRankRequirements[fromRank] = req;
                }

                Console.WriteLine("[GuildRankSystem] Настройки успешно загружены из Config/Rank.cfg.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GuildRankSystem] Ошибка чтения Config/Rank.cfg: {ex.Message}");
            }
        }

        private static void LoadDefaults()
        {
            MaxRankBelow = 2;
            MaxRankAbove = 2;
            WeakMonsterMultiplier = 0.5;

            MonsterRanks = new List<MonsterRankEntry>
            {
                new MonsterRankEntry { Level = 1, MinThreat = 25000, Points = 650 },
                new MonsterRankEntry { Level = 2, MinThreat = 18000, Points = 400 },
                new MonsterRankEntry { Level = 3, MinThreat = 13000, Points = 250 },
                new MonsterRankEntry { Level = 4, MinThreat = 9000,  Points = 160 },
                new MonsterRankEntry { Level = 5, MinThreat = 6000,  Points = 100 },
                new MonsterRankEntry { Level = 6, MinThreat = 4000,  Points = 65 },
                new MonsterRankEntry { Level = 7, MinThreat = 2500,  Points = 40 },
                new MonsterRankEntry { Level = 8, MinThreat = 1500,  Points = 25 },
                new MonsterRankEntry { Level = 9, MinThreat = 900,   Points = 15 },
                new MonsterRankEntry { Level = 10, MinThreat = 500,  Points = 10 }
            };

            PlayerRankRequirements = new Dictionary<int, int>
            {
                { 11, 5000 }, { 10, 8000 }, { 9, 12000 }, { 8, 18000 }, { 7, 25000 },
                { 6, 35000 }, { 5, 48000 }, { 4, 62000 }, { 3, 80000 }, { 2, 100000 }
            };
        }

        public static int GetMonsterRank(BaseCreature bc)
        {
            if (bc == null || bc.Summoned || bc.Controlled)
                return 0;

            int negativeKarma = bc.Karma < 0 ? Math.Abs(bc.Karma) : 0;
            int totalThreat = bc.Fame + negativeKarma;

            foreach (var rank in MonsterRanks)
            {
                if (totalThreat >= rank.MinThreat)
                    return rank.Level;
            }

            return 0;
        }

        public static int GetBasePointsByMonsterRank(int monsterRank)
        {
            var entry = MonsterRanks.FirstOrDefault(r => r.Level == monsterRank);
            return entry != null ? entry.Points : 0;
        }

        public static int GetRequiredPointsForRank(int currentPlayerRank)
        {
            if (PlayerRankRequirements.TryGetValue(currentPlayerRank, out int points))
                return points;

            return 5000;
        }

        public static int CalculateEarnedPoints(int playerRank, int monsterRank)
        {
            if (monsterRank <= 0)
                return 0;

            int targetRank = Math.Max(1, playerRank - 1);

            if (monsterRank > targetRank + MaxRankBelow)
                return 0;

            if (monsterRank < targetRank - MaxRankAbove)
                return 0;

            int points = GetBasePointsByMonsterRank(monsterRank);

            if (monsterRank > targetRank)
            {
                points = Math.Max(1, (int)(points * WeakMonsterMultiplier));
            }

            return points;
        }
    }
}
