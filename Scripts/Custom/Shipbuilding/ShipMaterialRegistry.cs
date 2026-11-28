using System;
using System.Collections.Generic;
using System.IO;
using Server;
using Server.Items;
using Server.Multis;

namespace Server.Custom.Shipbuilding
{
    public class ShipMaterialData
    {
        public CraftResource Wood { get; set; }
        public CraftResource Metal { get; set; }
        public int Hue { get; set; }
    }

    public static class ShipMaterialRegistry
    {
        private static readonly Dictionary<int, ShipMaterialData> m_DeedData = new Dictionary<int, ShipMaterialData>();
        private static readonly Dictionary<int, ShipMaterialData> m_BoatData = new Dictionary<int, ShipMaterialData>();

        private static readonly string SavePath = Path.Combine(Core.BaseDirectory, "Saves", "CustomShipMaterials.bin");

        public static void Configure()
        {
            EventSink.WorldSave += OnWorldSave;
            EventSink.WorldLoad += OnWorldLoad;
        }

        public static void RegisterDeed(Item deed, CraftResource wood, CraftResource metal, int hue)
        {
            if (deed == null) return;
            m_DeedData[deed.Serial.Value] = new ShipMaterialData { Wood = wood, Metal = metal, Hue = hue };
        }

        public static ShipMaterialData GetDeedData(Item deed)
        {
            if (deed != null && m_DeedData.TryGetValue(deed.Serial.Value, out var data))
                return data;
            return null;
        }

        public static void TransferToBoat(BaseBoatDeed deed, BaseBoat boat)
        {
            if (deed == null || boat == null) return;

            if (m_DeedData.TryGetValue(deed.Serial.Value, out var data))
            {
                m_BoatData[boat.Serial.Value] = data;
                m_DeedData.Remove(deed.Serial.Value);
                ApplyBonusesToBoat(boat, data);
            }
        }

        public static void TransferToDockedBoat(BaseBoat boat, BaseDockedBoat dockedBoat)
        {
            if (boat == null || dockedBoat == null) return;

            if (m_BoatData.TryGetValue(boat.Serial.Value, out var data))
            {
                m_DeedData[dockedBoat.Serial.Value] = data;

                dockedBoat.Hue = data.Hue;
                string wName = CraftResources.GetName(data.Wood);
                string mName = CraftResources.GetName(data.Metal);
                dockedBoat.Name = $"Судно в сухом доке [{wName} / {mName}]";
            }
        }

        public static void TransferFromDockedBoat(BaseDockedBoat dockedBoat, BaseBoat boat)
        {
            if (dockedBoat == null || boat == null) return;

            if (m_DeedData.TryGetValue(dockedBoat.Serial.Value, out var data))
            {
                m_BoatData[boat.Serial.Value] = data;
                m_DeedData.Remove(dockedBoat.Serial.Value);
                ApplyBonusesToBoat(boat, data);
            }
        }

        public static ShipMaterialData GetBoatData(BaseBoat boat)
        {
            if (boat != null && m_BoatData.TryGetValue(boat.Serial.Value, out var data))
                return data;
            return null;
        }

        public static void ApplyBonusesToBoat(BaseBoat boat, ShipMaterialData data)
        {
            if (boat == null || data == null) return;

            if (data.Hue > 0)
            {
                boat.Hue = data.Hue;
                if (boat.PPlank != null) boat.PPlank.Hue = data.Hue;
                if (boat.SPlank != null) boat.SPlank.Hue = data.Hue;
                if (boat.Hold != null) boat.Hold.Hue = data.Hue;
                if (boat.TillerMan is Item tmItem) tmItem.Hue = data.Hue;
                else if (boat.TillerMan is Mobile tmMob) tmMob.Hue = data.Hue;
            }

            if (boat.Hold != null)
            {
                double weightMult = 1.0 + GetWoodBonus(data.Wood) + GetMetalBonus(data.Metal);
                boat.Hold.MaxWeight = (int)(boat.Hold.MaxWeight * weightMult);
            }

            boat.Hits = boat.MaxHits;
        }

        public static double GetWoodBonus(CraftResource wood)
        {
            switch (wood)
            {
                case CraftResource.OakWood: return 0.10;
                case CraftResource.AshWood: return 0.15;
                case CraftResource.YewWood: return 0.25;
                case CraftResource.Heartwood: return 0.35;
                case CraftResource.Bloodwood: return 0.45;
                case CraftResource.Frostwood: return 0.60;
                default: return 0.0;
            }
        }

        public static double GetMetalBonus(CraftResource metal)
        {
            switch (metal)
            {
                case CraftResource.DullCopper: return 0.05;
                case CraftResource.ShadowIron: return 0.10;
                case CraftResource.Copper: return 0.15;
                case CraftResource.Bronze: return 0.20;
                case CraftResource.Gold: return 0.25;
                case CraftResource.Agapite: return 0.28;
                case CraftResource.Verite: return 0.32;
                case CraftResource.Valorite: return 0.35;
                default: return 0.0;
            }
        }

        private static void OnWorldSave(WorldSaveEventArgs e)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(SavePath));
                using (FileStream fs = new FileStream(SavePath, FileMode.Create, FileAccess.Write))
                using (BinaryWriter writer = new BinaryWriter(fs))
                {
                    writer.Write(0);

                    writer.Write(m_DeedData.Count);
                    foreach (var kvp in m_DeedData)
                    {
                        writer.Write(kvp.Key);
                        writer.Write((int)kvp.Value.Wood);
                        writer.Write((int)kvp.Value.Metal);
                        writer.Write(kvp.Value.Hue);
                    }

                    writer.Write(m_BoatData.Count);
                    foreach (var kvp in m_BoatData)
                    {
                        writer.Write(kvp.Key);
                        writer.Write((int)kvp.Value.Wood);
                        writer.Write((int)kvp.Value.Metal);
                        writer.Write(kvp.Value.Hue);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ShipMaterialRegistry] Save error: {ex.Message}");
            }
        }

        private static void OnWorldLoad()
        {
            if (!File.Exists(SavePath))
                return;

            try
            {
                using (FileStream fs = new FileStream(SavePath, FileMode.Open, FileAccess.Read))
                using (BinaryReader reader = new BinaryReader(fs))
                {
                    int version = reader.ReadInt32();

                    int deedCount = reader.ReadInt32();
                    m_DeedData.Clear();
                    for (int i = 0; i < deedCount; i++)
                    {
                        int s = reader.ReadInt32();
                        CraftResource w = (CraftResource)reader.ReadInt32();
                        CraftResource m = (CraftResource)reader.ReadInt32();
                        int h = reader.ReadInt32();
                        m_DeedData[s] = new ShipMaterialData { Wood = w, Metal = m, Hue = h };
                    }

                    int boatCount = reader.ReadInt32();
                    m_BoatData.Clear();
                    for (int i = 0; i < boatCount; i++)
                    {
                        int s = reader.ReadInt32();
                        CraftResource w = (CraftResource)reader.ReadInt32();
                        CraftResource m = (CraftResource)reader.ReadInt32();
                        int h = reader.ReadInt32();
                        m_BoatData[s] = new ShipMaterialData { Wood = w, Metal = m, Hue = h };
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ShipMaterialRegistry] Load error: {ex.Message}");
            }
        }
    }
}
