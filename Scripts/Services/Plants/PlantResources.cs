using Server.Items;
using System;
using System.Collections.Generic;

namespace Server.Engines.Plants
{
    public class PlantResourceInfo
    {
        private static readonly List<PlantResourceInfo> m_ResourceList = new List<PlantResourceInfo>
        {
            new PlantResourceInfo(PlantType.ElephantEarPlant, PlantHue.BrightRed, typeof(RedLeaves)),
            new PlantResourceInfo(PlantType.PonytailPalm, PlantHue.BrightRed, typeof(RedLeaves)),
            new PlantResourceInfo(PlantType.CenturyPlant, PlantHue.BrightRed, typeof(RedLeaves)),
            new PlantResourceInfo(PlantType.Poppies, PlantHue.BrightOrange, typeof(OrangePetals)),
            new PlantResourceInfo(PlantType.Bulrushes, PlantHue.BrightOrange, typeof(OrangePetals)),
            new PlantResourceInfo(PlantType.PampasGrass, PlantHue.BrightOrange, typeof(OrangePetals)),
            new PlantResourceInfo(PlantType.SnakePlant, PlantHue.BrightGreen, typeof(GreenThorns)),
            new PlantResourceInfo(PlantType.BarrelCactus, PlantHue.BrightGreen, typeof(GreenThorns)),
            new PlantResourceInfo(PlantType.CocoaTree, PlantHue.Plain, typeof(CocoaPulp)),
            new PlantResourceInfo(PlantType.SugarCanes, PlantHue.Plain, typeof(SackOfSugar)),
            new PlantResourceInfo(PlantType.FlaxFlowers, PlantHue.Plain, typeof(Flax)),
            new PlantResourceInfo(PlantType.CypressStraight, PlantHue.Plain, typeof(BarkFragment)),
            new PlantResourceInfo(PlantType.CypressTwisted, PlantHue.Plain, typeof(BarkFragment)),
            new PlantResourceInfo(PlantType.Vanilla, PlantHue.Plain, typeof(Vanilla)),
            new PlantResourceInfo(PlantType.PoppyPatch, PlantHue.Plain, typeof(PoppiesDust)),
            new PlantResourceInfo(PlantType.Vanilla, PlantHue.Plain, typeof(Vanilla))
        };

        private PlantResourceInfo(PlantType plantType, PlantHue plantHue, Type resourceType)
        {
            PlantType = plantType;
            PlantHue = plantHue;
            ProductionStartStage = (int)PlantStatus.Stage9;
            ResourceType = resourceType;
            m_CreateResource = () => (Item)Activator.CreateInstance(ResourceType);
        }

        private PlantResourceInfo(PlantType plantType, PlantHue plantHue, Func<Item> createResource,
            int productionStartStage, Func<Seed> createSeed)
        {
            PlantType = plantType;
            PlantHue = plantHue;
            ProductionStartStage = productionStartStage;
            m_CreateResource = createResource;
            m_CreateSeed = createSeed;
        }

        private readonly Func<Item> m_CreateResource;
        private readonly Func<Seed> m_CreateSeed;

        public PlantType PlantType { get; set; }
        public PlantHue PlantHue { get; set; }
        public Type ResourceType { get; set; }
        public int ProductionStartStage { get; }

        public static void Register(PlantType plantType, PlantHue plantHue, Func<Item> createResource)
        {
            Register(plantType, plantHue, createResource, (int)PlantStatus.Stage9, null);
        }

        public static void Register(PlantType plantType, PlantHue plantHue, Func<Item> createResource,
            int productionStartStage, Func<Seed> createSeed)
        {
            if (createResource == null)
                throw new ArgumentNullException(nameof(createResource));

            if (productionStartStage < (int)PlantStatus.BowlOfDirt || productionStartStage > (int)PlantStatus.Stage9)
                throw new ArgumentOutOfRangeException(nameof(productionStartStage));

            var info = new PlantResourceInfo(plantType, plantHue, createResource, productionStartStage, createSeed);

            for (int i = 0; i < m_ResourceList.Count; i++)
            {
                if (m_ResourceList[i].PlantType == plantType && m_ResourceList[i].PlantHue == plantHue)
                {
                    m_ResourceList[i] = info;
                    return;
                }
            }

            m_ResourceList.Add(info);
        }

        public static PlantResourceInfo GetInfo(PlantType plantType, PlantHue plantHue)
        {
            foreach (PlantResourceInfo info in m_ResourceList)
            {
                if (info.PlantType == plantType && info.PlantHue == plantHue)
                    return info;
            }

            return null;
        }

        public Item CreateResource()
        {
            return m_CreateResource();
        }

        public Seed CreateSeed()
        {
            return m_CreateSeed?.Invoke();
        }
    }
}
