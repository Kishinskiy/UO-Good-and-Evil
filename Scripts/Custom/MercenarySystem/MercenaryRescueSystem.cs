using System;
using System.Collections.Generic;
using System.IO;
using Server;
using Server.Items;
using Server.Mobiles;

namespace Server.Custom.MercenarySystem
{
    // Данные павшего наёмника, ожидающего выкупа в банке данных Капитана
    public class FallenMercenaryData
    {
        public string OwnerAccount { get; set; }
        public string MercName { get; set; }
        public MercenaryClass Class { get; set; }
        public int Level { get; set; }
        public int Exp { get; set; }
        public int NextLevelExperience { get; set; }
        public int PurchasePrice { get; set; }
        public int RaceIndex { get; set; }
        public bool Female { get; set; }
        public List<Item> EquippedItems { get; set; } = new List<Item>();

        public void Serialize(GenericWriter writer)
        {
            writer.Write(OwnerAccount);
            writer.Write(MercName);
            writer.Write((int)Class);
            writer.Write(Level);
            writer.Write(Exp);
            writer.Write(NextLevelExperience);
            writer.Write(PurchasePrice);
            writer.Write(RaceIndex);
            writer.Write(Female);
            writer.Write((int)EquippedItems.Count);
            foreach (Item item in EquippedItems)
                writer.Write(item);
        }

        public static FallenMercenaryData Deserialize(GenericReader reader, int version)
        {
            FallenMercenaryData data = new FallenMercenaryData();
            data.OwnerAccount = reader.ReadString();
            data.MercName = reader.ReadString();
            data.Class = MercenaryClassUtility.FromSerializedValue(reader.ReadInt());
            data.Level = reader.ReadInt();
            data.Exp = reader.ReadInt();

            if (version >= 2)
            {
                data.NextLevelExperience = reader.ReadInt();
                data.PurchasePrice = reader.ReadInt();

                if (version >= 3)
                {
                    data.RaceIndex = reader.ReadInt();
                    data.Female = reader.ReadBool();
                }
                else
                {
                    data.RaceIndex = Race.Human.RaceIndex;
                }
            }
            else
            {
                data.NextLevelExperience = (int)(500 * Math.Pow(1.8, Math.Max(0, data.Level - 1)));
                data.PurchasePrice = MercenaryContract.GetPurchasePrice(data.Class, data.Level);
                data.RaceIndex = Race.Human.RaceIndex;
            }

            int itemCount = reader.ReadInt();
            data.EquippedItems = new List<Item>();
            for (int i = 0; i < itemCount; i++)
            {
                Item item = reader.ReadItem();

                if (item != null && !item.Deleted)
                    data.EquippedItems.Add(item);
            }

            return data;
        }
    }

    public static class MercenaryRescueSystem
    {
        private static readonly string _savePath = Path.Combine(Core.BaseDirectory, "Saves", "Custom", "MercenaryRescue.bin");

        // Глобальная база данных погибших наёмников (Имя аккаунта хозяина -> список слуг)
        private static readonly Dictionary<string, List<FallenMercenaryData>> _rescueRoster 
            = new Dictionary<string, List<FallenMercenaryData>>();
        private static bool _initialized;

        public static void Initialize()
        {
            if (_initialized)
                return;

            _initialized = true;
            EventSink.CreatureDeath += OnCreatureDeath;
            EventSink.WorldSave += e => Save();
            Load();
        }

        private static void AwardKillExperience(Mobile creature)
        {
            BaseCreature killed = creature as BaseCreature;

            if (killed == null || killed.DamageEntries == null)
                return;

            foreach (DamageEntry entry in killed.DamageEntries)
            {
                BaseMercenary mercenary = entry.Damager as BaseMercenary;

                if (entry.HasExpired || mercenary == null || mercenary.Deleted || !mercenary.Controlled ||
                    !(mercenary.ControlMaster is PlayerMobile))
                    continue;

                mercenary.AwardExperience(Math.Max(15, killed.HitsMax / 3));
            }
        }

        private static void OnCreatureDeath(CreatureDeathEventArgs e)
        {
            AwardKillExperience(e.Creature);

            BaseMercenary merc = e.Creature as BaseMercenary;
            
            // Если погиб наш кастомный наёмник и у него есть хозяин
            if (merc != null && !merc.Deleted && merc.ControlMaster is PlayerMobile pm)
            {
                if (pm.Account == null) return;
                string accName = pm.Account.Username.ToLowerInvariant().Trim();

                // Формируем пакет сохранения для выкупа
                FallenMercenaryData fallenData = new FallenMercenaryData
                {
                    OwnerAccount = accName,
                    MercName = merc.RawName,
                    Class = merc.Class,
                    Level = merc.MercLevel,
                    Exp = merc.MercExp,
                    NextLevelExperience = merc.NextLevelExperience,
                    PurchasePrice = MercenaryContract.GetPurchasePrice(merc.Class, merc.MercLevel),
                    RaceIndex = merc.Race.RaceIndex,
                    Female = merc.Female
                };

                // Preserve the equipment and backpack; the corpse should not duplicate rescued items.
                List<Item> wornItems = new List<Item>(merc.Items);
                foreach (Item item in wornItems)
                {
                    if (item != null && !item.Deleted)
                    {
                        item.Movable = true;
                        fallenData.EquippedItems.Add(item);
                        item.Internalize();
                    }
                }

                e.ClearCorpse = true;

                if (!_rescueRoster.TryGetValue(accName, out List<FallenMercenaryData> list))
                {
                    list = new List<FallenMercenaryData>();
                    _rescueRoster[accName] = list;
                }

                list.Add(fallenData);

                // Оповещаем хозяина
                pm.PlaySound(0x5C3); // Грустный звук
                pm.SendMessage(0x22, $"[Гильдия Наёмников]: Ваш спутник {merc.Name} пал в бою! Его снаряжение и душа спасены Вербовщиками. Вы можете выкупить его у любого Капитана Наёмников в городах.");
                
                // Удаляем нативного моба, чтобы он не оставлял голый лут в данже
                Timer.DelayCall(TimeSpan.FromMilliseconds(50), () => merc.Delete());
            }
        }

        public static void Save()
        {
            try
            {
                string folder = Path.GetDirectoryName(_savePath);
                if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

                BinaryFileWriter writer = new BinaryFileWriter(_savePath, false);
                try
                {
                    writer.Write((int)3); // Version
                    writer.Write((int)_rescueRoster.Count);

                    foreach (var kvp in _rescueRoster)
                    {
                        writer.Write(kvp.Key);
                        writer.Write((int)kvp.Value.Count);
                        foreach (FallenMercenaryData data in kvp.Value)
                            data.Serialize(writer);
                    }
                }
                finally
                {
                    writer.Close();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MercenaryRescueSystem] Save failed: {ex}");
            }
        }

        public static void Load()
        {
            _rescueRoster.Clear();

            if (!File.Exists(_savePath)) return;

            try
            {
                using (FileStream stream = new FileStream(_savePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    using (BinaryReader binReader = new BinaryReader(stream))
                    {
                        GenericReader reader = new BinaryFileReader(binReader);
                        int version = reader.ReadInt();
                        if (version < 1 || version > 3) return;

                        int accountCount = reader.ReadInt();
                        for (int i = 0; i < accountCount; i++)
                        {
                            string accName = reader.ReadString();
                            int mercCount = reader.ReadInt();
                            List<FallenMercenaryData> list = new List<FallenMercenaryData>();
                            for (int j = 0; j < mercCount; j++)
                            {
                                FallenMercenaryData data = FallenMercenaryData.Deserialize(reader, version);
                                data.PurchasePrice = MercenaryContract.GetPurchasePrice(data.Class, data.Level);
                                list.Add(data);
                            }

                            _rescueRoster[accName] = list;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MercenaryRescueSystem] Load failed: {ex}");
            }
        }

        // Метод проверки наличия слуг на выкуп у конкретного игрока
        public static List<FallenMercenaryData> GetFallenMercenaries(PlayerMobile pm)
        {
            if (pm == null || pm.Account == null) return null;
            string accName = pm.Account.Username.ToLowerInvariant().Trim();

            _rescueRoster.TryGetValue(accName, out List<FallenMercenaryData> list);
            return list;
        }

        public static bool RemoveFromRoster(PlayerMobile pm, FallenMercenaryData data)
        {
            if (pm == null || pm.Account == null || data == null) return false;
            string accName = pm.Account.Username.ToLowerInvariant().Trim();

            if (_rescueRoster.TryGetValue(accName, out List<FallenMercenaryData> list))
            {
                bool removed = list.Remove(data);

                if (list.Count == 0) _rescueRoster.Remove(accName);

                return removed;
            }

            return false;
        }
    }
}
