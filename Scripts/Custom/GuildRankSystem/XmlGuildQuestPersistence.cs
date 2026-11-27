using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;
using Server;
using Server.Mobiles;

namespace Server.Engines.Quests
{
    public static class XmlGuildQuestPersistence
    {
        private static readonly string SaveDir = Path.Combine(Core.BaseDirectory, "Saves", "GuildQuests");
        private static readonly string SavePath = Path.Combine(SaveDir, "ActiveGuildQuests.xml");

        public static void Configure()
        {
            EventSink.WorldSave += OnWorldSave;
        }

        public static void Initialize()
        {
            Timer.DelayCall(TimeSpan.Zero, LoadActiveQuests);
        }

        private static void OnWorldSave(WorldSaveEventArgs e)
        {
            SaveActiveQuests();
        }

        public static void SaveActiveQuests()
        {
            try
            {
                if (!Directory.Exists(SaveDir))
                    Directory.CreateDirectory(SaveDir);

                XElement root = new XElement("ActiveGuildQuests");

                foreach (Mobile m in World.Mobiles.Values)
                {
                    if (m is PlayerMobile pm && pm.Quests != null && pm.Quests.Count > 0)
                    {
                        foreach (BaseQuest q in pm.Quests)
                        {
                            if (q is DynamicXmlGuildQuest dq && dq.Config != null)
                            {
                                XElement questEl = new XElement("PlayerQuest",
                                    new XAttribute("serial", pm.Serial.Value),
                                    new XAttribute("questId", dq.Config.Id)
                                );

                                for (int i = 0; i < dq.Objectives.Count; i++)
                                {
                                    BaseObjective obj = dq.Objectives[i];
                                    questEl.Add(new XElement("Objective",
                                        new XAttribute("index", i),
                                        new XAttribute("progress", obj.CurProgress),
                                        new XAttribute("completed", obj.Completed)
                                    ));
                                }

                                root.Add(questEl);
                            }
                        }
                    }
                }

                XDocument doc = new XDocument(new XDeclaration("1.0", "utf-8", "yes"), root);
                doc.Save(SavePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GuildRankSystem] Ошибка сохранения ActiveGuildQuests.xml: {ex.Message}");
            }
        }

        public static void LoadActiveQuests()
        {
            XmlGuildQuestManager.EnsureLoaded();

            // 1. Удаляем все "пустые/сломанные" экземпляры DynamicXmlGuildQuest
            foreach (Mobile m in World.Mobiles.Values)
            {
                if (m is PlayerMobile pm && pm.Quests != null)
                {
                    for (int i = pm.Quests.Count - 1; i >= 0; i--)
                    {
                        if (pm.Quests[i] is DynamicXmlGuildQuest dq && dq.Config == null)
                        {
                            pm.Quests.RemoveAt(i);
                        }
                    }
                }
            }

            if (!File.Exists(SavePath))
                return;

            try
            {
                XDocument doc = XDocument.Load(SavePath);
                if (doc.Root == null)
                    return;

                int restoredCount = 0;

                foreach (XElement questEl in doc.Root.Elements("PlayerQuest"))
                {
                    int serialVal = (int?)questEl.Attribute("serial") ?? 0;
                    string questId = (string)questEl.Attribute("questId");

                    if (serialVal <= 0 || string.IsNullOrEmpty(questId))
                        continue;

                    Mobile m = World.FindMobile(new Serial(serialVal));
                    if (!(m is PlayerMobile pm) || pm.Deleted || pm.Quests == null)
                        continue;

                    XmlGuildQuestConfig config = XmlGuildQuestManager.Quests.Find(q => q.Id == questId);
                    if (config == null)
                    {
                        Console.WriteLine($"[GuildRankSystem] Не удалось восстановить квест '{questId}' для {pm.Name}: конфиг не найден.");
                        continue;
                    }

                    // Удаляем дубликат, если он уже есть у игрока
                    for (int i = pm.Quests.Count - 1; i >= 0; i--)
                    {
                        if (pm.Quests[i] is DynamicXmlGuildQuest existing && existing.Config != null && existing.Config.Id == questId)
                        {
                            pm.Quests.RemoveAt(i);
                        }
                    }

                    DynamicXmlGuildQuest restoredQuest = new DynamicXmlGuildQuest(config);
                    restoredQuest.Owner = pm;

                    foreach (XElement objEl in questEl.Elements("Objective"))
                    {
                        int idx = (int?)objEl.Attribute("index") ?? -1;
                        int progress = (int?)objEl.Attribute("progress") ?? 0;
                        bool completed = (bool?)objEl.Attribute("completed") ?? false;

                        if (idx >= 0 && idx < restoredQuest.Objectives.Count)
                        {
                            restoredQuest.Objectives[idx].CurProgress = progress;
                            if (completed && !restoredQuest.Objectives[idx].Completed)
                            {
                                restoredQuest.Objectives[idx].Complete();
                            }
                        }
                    }

                    pm.Quests.Add(restoredQuest);
                    restoredCount++;
                }

                Console.WriteLine($"[GuildRankSystem] Восстановлено активных гильдейских квестов из файла: {restoredCount}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GuildRankSystem] Ошибка загрузки ActiveGuildQuests.xml: {ex.Message}");
            }
        }
    }
}
