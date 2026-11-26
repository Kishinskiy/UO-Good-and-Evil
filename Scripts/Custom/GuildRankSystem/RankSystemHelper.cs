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
            MonsterRanks = new List<MonsterRankEntry>
            {
                new MonsterRankEntry { Level = 1, MinThreat = 48000, Points = 650 },
                new MonsterRankEntry { Level = 2, MinThreat = 35000, Points = 400 },
                new MonsterRankEntry { Level = 3, MinThreat = 26000, Points = 250 },
                new MonsterRankEntry { Level = 4, MinThreat = 19000, Points = 160 },
                new MonsterRankEntry { Level = 5, MinThreat = 13500, Points = 100 },
                new MonsterRankEntry { Level = 6, MinThreat = 9500,  Points = 65 },
                new MonsterRankEntry { Level = 7, MinThreat = 6500,  Points = 40 },
                new MonsterRankEntry { Level = 8, MinThreat = 4200,  Points = 25 },
                new MonsterRankEntry { Level = 9, MinThreat = 2400,  Points = 15 },
                new MonsterRankEntry { Level = 10, MinThreat = 800,  Points = 10 }
            };

            PlayerRankRequirements = new Dictionary<int, int>
            {
                { 11, 5000 }, { 10, 8000 }, { 9, 12000 }, { 8, 18000 }, { 7, 25000 },
                { 6, 35000 }, { 5, 48000 }, { 4, 62000 }, { 3, 80000 }, { 2, 100000 }
            };
        }

        // Расчет ранга монстра с учетом HP, Урона, Резистов, Навыков, Славы и Кармы
        public static int GetMonsterRank(BaseCreature bc)
        {
            if (bc == null || bc.Summoned || bc.Controlled)
                return 0;

            // Защита от зачета мирных животных (олени, птицы, кролики с нулевой славой и кармой)
            if (bc.Fame == 0 && bc.Karma >= 0 && bc.DamageMax <= 3)
                return 0;

            int negativeKarma = bc.Karma < 0 ? Math.Abs(bc.Karma) / 2 : 0;

            // Сумма всех 5 сопротивлений (Physical, Fire, Cold, Poison, Energy)
            int totalResists = bc.PhysicalResistance + bc.FireResistance + bc.ColdResistance + bc.PoisonResistance + bc.EnergyResistance;

            // Основной боевой или магический навык
            double mainCombatSkill = Math.Max(
                Math.Max(bc.Skills[SkillName.Wrestling].Value, bc.Skills[SkillName.Magery].Value),
                Math.Max(bc.Skills[SkillName.Necromancy].Value, bc.Skills[SkillName.Chivalry].Value)
            );

            // Усиливающий навык (Tactics для воинов, EvalInt для магов)
            double supportSkill = Math.Max(bc.Skills[SkillName.Tactics].Value, bc.Skills[SkillName.EvalInt].Value);

            int skillBonus = (int)((mainCombatSkill * 30) + (supportSkill * 15));
            int resistBonus = Math.Max(0, totalResists) * 15;

            int totalThreat = bc.Fame + negativeKarma + bc.HitsMax + (bc.DamageMax * 150) + resistBonus + skillBonus;

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

        // Начисление очков без ограничений по разнице рангов
        public static int CalculateEarnedPoints(int playerRank, int monsterRank)
        {
            if (monsterRank <= 0)
                return 0;

            return GetBasePointsByMonsterRank(monsterRank);
        }
    }
}
