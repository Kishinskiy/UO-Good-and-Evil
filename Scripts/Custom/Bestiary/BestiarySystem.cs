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
        public int Level { get; set; } // Уровень изучения от 0 до 5 (или 0..100)
    }

    public static class BestiarySystem
    {
        private static readonly string FilePath = Path.Combine("Saves/Custom", "BestiaryData.bin");

        // Словарь: Serial игрока -> (Имя класса монстра -> Данные изучения)
        public static Dictionary<Serial, Dictionary<string, MonsterKnowledge>> PlayerData { get; private set; }
            = new Dictionary<Serial, Dictionary<string, MonsterKnowledge>>();

        public const int MaxKnowledgeLevel = 5; // Макс. уровень изучения одного монстра (1..5 звёзд/рангов)

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

        // Бонус урона по конкретному монстру: +4% за каждый уровень изучения (до +20% на 5 уровне)
        public static double GetDamageBonusMultiplier(Mobile attacker, BaseCreature defender)
        {
            var k = GetKnowledge(attacker, defender);
            if (k == null || k.Level <= 0)
                return 1.0;

            return 1.0 + (k.Level * 0.04);
        }

        // Бонус золота с монстра: +6% за каждый уровень изучения (до +30% на 5 уровне)
        public static double GetGoldBonusMultiplier(Mobile killer, BaseCreature victim)
        {
            var k = GetKnowledge(killer, victim);
            if (k == null || k.Level <= 0)
                return 1.0;

            return 1.0 + (k.Level * 0.06);
        }

        // Шанс получения редкого трофея/ресурса: +3% за уровень (до +15% на 5 уровне)
        public static double GetRareDropChance(Mobile killer, BaseCreature victim)
        {
            var k = GetKnowledge(killer, victim);
            if (k == null || k.Level <= 0)
                return 0.0;

            return k.Level * 0.03;
        }

        private static void OnSave(WorldSaveEventArgs e)
        {
            Persistence.Serialize(FilePath, writer =>
            {
                writer.Write(0); // Версия
                writer.Write(PlayerData.Count);

                foreach (var playerKvp in PlayerData)
                {
                    writer.Write(playerKvp.Key);
                    writer.Write(playerKvp.Value.Count);

                    foreach (var mobKvp in playerKvp.Value)
                    {
                        writer.Write(mobKvp.Key);
                        writer.Write(mobKvp.Value.Kills);
                        writer.Write(mobKvp.Value.Level);
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
                        int level = reader.ReadInt();

                        dict[mobName] = new MonsterKnowledge { Kills = kills, Level = level };
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
