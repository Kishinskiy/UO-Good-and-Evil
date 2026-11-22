using System;
using System.Collections.Generic;
using System.IO;
using Server;
using Server.Mobiles;

namespace Server.Custom.Bestiary
{
    public class MonsterKnowledge
    {
        public int Kills { get; set; }
        public double SkillValue { get; set; } // Шкала от 0.0 до 100.0, как у скиллов игрока
        public double SkillCap { get; set; } = 100.0;
    }

    public static class BestiarySystem
    {
        private static readonly string FilePath = Path.Combine("Saves/Custom", "BestiaryData.bin");

        public static Dictionary<Serial, Dictionary<string, MonsterKnowledge>> PlayerData { get; private set; }
            = new Dictionary<Serial, Dictionary<string, MonsterKnowledge>>();

        public const double MaxKnowledgeSkill = 100.0;

        public static void Configure()
        {
            EventSink.WorldSave += OnSave;
            EventSink.WorldLoad += OnLoad;
        }

        public static MonsterKnowledge GetKnowledge(Mobile player, BaseCreature creature, bool create = false)
        {
            if (player == null || creature == null || !player.Player)
                return null;

            return GetKnowledge(player, creature.GetType().Name, create);
        }

        public static MonsterKnowledge GetKnowledge(Mobile player, string monsterTypeName, bool create = false)
        {
            if (player == null || string.IsNullOrEmpty(monsterTypeName))
                return null;

            if (!PlayerData.TryGetValue(player.Serial, out var dict))
            {
                if (!create) return null;
                dict = new Dictionary<string, MonsterKnowledge>(StringComparer.OrdinalIgnoreCase);
                PlayerData[player.Serial] = dict;
            }

            if (!dict.TryGetValue(monsterTypeName, out var knowledge))
            {
                if (!create) return null;
                knowledge = new MonsterKnowledge();
                dict[monsterTypeName] = knowledge;
            }

            return knowledge;
        }

        // Бонус урона: до +25% на 100.0 (по 0.25% за каждый 1.0 изучения)
        public static double GetDamageBonusMultiplier(Mobile attacker, BaseCreature defender)
        {
            var k = GetKnowledge(attacker, defender);
            if (k == null || k.SkillValue <= 0.0)
                return 1.0;

            return 1.0 + ((k.SkillValue / 100.0) * 0.25);
        }

        // Бонус золота: до +30% на 100.0 (по 0.30% за каждый 1.0 изучения)
        public static double GetGoldBonusMultiplier(Mobile killer, BaseCreature victim)
        {
            var k = GetKnowledge(killer, victim);
            if (k == null || k.SkillValue <= 0.0)
                return 1.0;

            return 1.0 + ((k.SkillValue / 100.0) * 0.30);
        }

        // Шанс редкого трофея: до +15% на 100.0
        public static double GetRareDropChance(Mobile killer, BaseCreature victim)
        {
            var k = GetKnowledge(killer, victim);
            if (k == null || k.SkillValue <= 0.0)
                return 0.0;

            return (k.SkillValue / 100.0) * 0.15;
        }

        private static void OnSave(WorldSaveEventArgs e)
        {
            Persistence.Serialize(FilePath, writer =>
            {
                writer.Write(1); // Версия 1 (double SkillValue 0.0 - 100.0)
                writer.Write(PlayerData.Count);

                foreach (var playerKvp in PlayerData)
                {
                    writer.Write(playerKvp.Key);
                    writer.Write(playerKvp.Value.Count);

                    foreach (var mobKvp in playerKvp.Value)
                    {
                        writer.Write(mobKvp.Key);
                        writer.Write(mobKvp.Value.Kills);
                        writer.Write(mobKvp.Value.SkillValue);
                        writer.Write(mobKvp.Value.SkillCap);
                    }
                }
            });
        }

        private static void OnLoad()
        {
            Persistence.Deserialize(FilePath, reader =>
            {
                int version = reader.ReadInt();
                int playerCount = reader.ReadInt();

                PlayerData.Clear();
                for (int i = 0; i < playerCount; i++)
                {
                    Serial serial = reader.ReadSerial();
                    int mobCount = reader.ReadInt();

                    var dict = new Dictionary<string, MonsterKnowledge>(StringComparer.OrdinalIgnoreCase);
                    for (int j = 0; j < mobCount; j++)
                    {
                        string mobName = reader.ReadString();
                        int kills = reader.ReadInt();

                        double skillVal;
                        double skillCap = MaxKnowledgeSkill;

                        if (version >= 1)
                        {
                            skillVal = reader.ReadDouble();
                            skillCap = reader.ReadDouble();
                        }
                        else
                        {
                            // Конвертация старого формата 0..5 в 0.0..100.0
                            skillVal = reader.ReadInt() * 20.0;
                        }

                        dict[mobName] = new MonsterKnowledge
                        {
                            Kills = kills,
                            SkillValue = skillVal,
                            SkillCap = skillCap
                        };
                    }

                    if (World.FindMobile(serial) != null)
                    {
                        PlayerData[serial] = dict;
                    }
                }
            });
        }
    }
}
