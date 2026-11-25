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
        public int RequiredRank { get; set; }
        public string GuildType { get; set; }
        public string ObjectiveType { get; set; } // "Slay" или "Obtain"
        public Type TargetType { get; set; }
        public string TargetName { get; set; }
        public int Amount { get; set; }
        public int RewardGold { get; set; }
        public Type RewardItem { get; set; }
        public int RewardItemAmount { get; set; }
    }

    public static class XmlGuildQuestManager
    {
        private static readonly string FilePath = Path.Combine(Core.BaseDirectory, "Data", "GuildQuests.xml");
        public static List<XmlGuildQuestConfig> Quests { get; private set; } = new List<XmlGuildQuestConfig>();

        public static void Initialize()
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
                    string targetTypeName = (string)el.Element("TargetType");
                    Type targetType = ScriptCompiler.FindTypeByName(targetTypeName);

                    if (targetType == null)
                    {
                        Console.WriteLine($"[GuildRankSystem] Ошибка в квесте {(string)el.Attribute("id")}: тип '{targetTypeName}' не найден.");
                        continue;
                    }

                    string rewardItemName = (string)el.Element("RewardItem");
                    Type rewardItemType = !string.IsNullOrEmpty(rewardItemName) ? ScriptCompiler.FindTypeByName(rewardItemName) : null;

                    Quests.Add(new XmlGuildQuestConfig
                    {
                        Id = (string)el.Attribute("id") ?? Guid.NewGuid().ToString(),
                        Title = (string)el.Element("Title") ?? "Задание гильдии",
                        Description = (string)el.Element("Description") ?? "Выполните поручение гильдии.",
                        RequiredRank = (int?)el.Element("RequiredRank") ?? 10,
                        GuildType = (string)el.Element("GuildType") ?? "Any",
                        ObjectiveType = (string)el.Element("ObjectiveType") ?? "Slay",
                        TargetType = targetType,
                        TargetName = (string)el.Element("TargetName") ?? targetTypeName,
                        Amount = (int?)el.Element("Amount") ?? 1,
                        RewardGold = (int?)el.Element("RewardGold") ?? 0,
                        RewardItem = rewardItemType,
                        RewardItemAmount = (int?)el.Element("RewardItemAmount") ?? 1
                    });
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
