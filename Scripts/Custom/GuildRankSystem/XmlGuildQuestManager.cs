using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;
using Server;
using Server.Commands;
using Server.Mobiles;

namespace Server.Engines.Quests
{
    public class XmlGuildQuestConfig
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public bool OnlyYoung { get; set; }
        public int RequiredRank { get; set; }
        public int ExactRank { get; set; } // 0 = не проверять, иначе требуется точный ранг
        public string GuildType { get; set; }
        public bool RequireHarrowerKill { get; set; }
        public NpcGuild RequireNpcGuild { get; set; }

        // Требования к статам и навыкам
        public int MinStr { get; set; }
        public int MinDex { get; set; }
        public int MinInt { get; set; }
        public Dictionary<SkillName, double> RequiredSkills { get; set; } = new Dictionary<SkillName, double>();

        // Цель квеста
        public string ObjectiveType { get; set; } // "Slay", "Obtain", "SlayRank" или "HuntPoints"
        public Type TargetType { get; set; }
        public string TargetName { get; set; }
        public int TargetRank { get; set; }
        public int Amount { get; set; }

        // Награды
        public int SetRank { get; set; } // 0 = не менять ранг, иначе установить указанный ранг
        public bool RemoveYoung { get; set; }
        public int RewardGold { get; set; }
        public Type RewardItem { get; set; }
        public int RewardItemAmount { get; set; }
    }

    public static class XmlGuildQuestManager
    {
        private static readonly string FilePath = Path.Combine(Core.BaseDirectory, "Data", "GuildQuests.xml");
        public static List<XmlGuildQuestConfig> Quests { get; private set; } = new List<XmlGuildQuestConfig>();

        public static void Configure()
        {
            CommandSystem.Register("ReloadGuildQuests", AccessLevel.Administrator, e =>
            {
                LoadQuests();
                e.Mobile.SendMessage(0x35, $"Загружено гильдейских квестов из XML: {Quests.Count}");
            });

            LoadQuests();
        }

        public static void LoadQuests()
        {
            Quests.Clear();

            if (!File.Exists(FilePath))
            {
                Console.WriteLine($"[GuildRankSystem] Файл {FilePath} не найден.");
                return;
            }

            try
            {
                XDocument doc = XDocument.Load(FilePath);
                foreach (XElement el in doc.Root.Elements("Quest"))
                {
                    string objType = (string)el.Element("ObjectiveType") ?? "Slay";
                    string targetTypeName = (string)el.Element("TargetType");
                    Type targetType = !string.IsNullOrEmpty(targetTypeName) ? ScriptCompiler.FindTypeByName(targetTypeName) : null;

                    // Для SlayRank и HuntPoints конкретный TargetType не требуется
                    if (!objType.Equals("SlayRank", StringComparison.OrdinalIgnoreCase) &&
                        !objType.Equals("HuntPoints", StringComparison.OrdinalIgnoreCase) &&
                        targetType == null)
                    {
                        Console.WriteLine($"[GuildRankSystem] Ошибка в квесте {(string)el.Attribute("id")}: тип '{targetTypeName}' не найден.");
                        continue;
                    }

                    string rewardItemName = (string)el.Element("RewardItem");
                    Type rewardItemType = !string.IsNullOrEmpty(rewardItemName) ? ScriptCompiler.FindTypeByName(rewardItemName) : null;
                    string npcGuildStr = (string)el.Element("RequireNpcGuild");
                    NpcGuild reqNpcGuild = NpcGuild.None;
                    if (!string.IsNullOrEmpty(npcGuildStr))
                    {
	                    Enum.TryParse(npcGuildStr, true, out reqNpcGuild);
                    }
                    
                    var cfg = new XmlGuildQuestConfig
                    {
                        Id = (string)el.Attribute("id") ?? Guid.NewGuid().ToString(),
                        Title = (string)el.Element("Title") ?? "Задание гильдии",
                        Description = (string)el.Element("Description") ?? "Выполните поручение гильдии.",
                        OnlyYoung = (bool?)el.Element("OnlyYoung") ?? false,
                        RequiredRank = (int?)el.Element("RequiredRank") ?? 11,
                        ExactRank = (int?)el.Element("ExactRank") ?? 0,
                        GuildType = (string)el.Element("GuildType") ?? "Any",
                        RequireHarrowerKill = (bool?)el.Element("RequireHarrowerKill") ?? false,
                        RequireNpcGuild = reqNpcGuild,

                        MinStr = (int?)el.Element("MinStr") ?? 0,
                        MinDex = (int?)el.Element("MinDex") ?? 0,
                        MinInt = (int?)el.Element("MinInt") ?? 0,

                        ObjectiveType = objType,
                        TargetType = targetType,
                        TargetName = (string)el.Element("TargetName") ?? targetTypeName,
                        TargetRank = (int?)el.Element("TargetRank") ?? 10,
                        Amount = (int?)el.Element("Amount") ?? 1,

                        SetRank = (int?)el.Element("SetRank") ?? 0,
                        RemoveYoung = (bool?)el.Element("RemoveYoung") ?? false,
                        RewardGold = (int?)el.Element("RewardGold") ?? 0,
                        RewardItem = rewardItemType,
                        RewardItemAmount = (int?)el.Element("RewardItemAmount") ?? 1
                    };

                    foreach (XElement skillEl in el.Elements("SkillReq"))
                    {
                        string skillNameStr = (string)skillEl.Attribute("name");
                        double minVal = (double?)skillEl.Attribute("min") ?? 0.0;

                        if (Enum.TryParse(skillNameStr, true, out SkillName skillName))
                        {
                            cfg.RequiredSkills[skillName] = minVal;
                        }
                    }

                    Quests.Add(cfg);
                }

                Console.WriteLine($"[GuildRankSystem] Успешно загружено квестов из XML: {Quests.Count}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GuildRankSystem] Ошибка чтения GuildQuests.xml: {ex.Message}");
            }
        }
    }
}
