using Server.Engines.Plants;
using Server.Items;
using System;

namespace Server.Custom.Farms
{
    internal static class PlantHarvestHook
    {
        [CallPriority(Int32.MinValue)]
        public static void Configure()
        {
            Register(CustomPlantTypes.Onion, () => new Onion(), () => new OnionSeed());
            Register(CustomPlantTypes.Potato, () => new Item(0x1814) { Name = "Potato" }, () => new PotatoSeed());
            Register(CustomPlantTypes.Cabbage, () => new Cabbage(), () => new CabbageSeed());
            Register(CustomPlantTypes.Carrot, () => new Carrot(), () => new CarrotSeed());
            Register(CustomPlantTypes.Lettuce, () => new Lettuce(), () => new LettuceSeed());
            Register(CustomPlantTypes.Pumpkin, () => new Pumpkin(), () => new PumpkinSeed());
            Register(CustomPlantTypes.Turnip, () => new Turnip(), () => new TurnipSeed());
            Register(CustomPlantTypes.Grapes, () => new Grapes(), () => new GrapesSeed());
            Register(CustomPlantTypes.Watermelon, () => new Watermelon(), () => new WatermelonSeed());
            Register(CustomPlantTypes.Wheat, () => new WheatSheaf(), () => new WheatSeed());
            Register(CustomPlantTypes.Apple, () => new Apple(), () => new AppleSeed());
            Register(CustomPlantTypes.Peach, () => new Peach(), () => new PeachSeed());
            Register(CustomPlantTypes.Pear, () => new Pear(), () => new PearSeed());
            Register(CustomPlantTypes.Banana, () => new Banana(), () => new BananaSeed());
            Register(CustomPlantTypes.Lemon, () => new Lemon(), () => new LemonSeed());
            Register(CustomPlantTypes.Lime, () => new Lime(), () => new LimeSeed());
            Register(CustomPlantTypes.Coconut, () => new Coconut(), () => new CoconutSeed());
            Register(CustomPlantTypes.Dates, () => new Dates(), () => new DatesSeed());
        }

        private static void Register(PlantType plantType, Func<Item> createResource, Func<Seed> createSeed)
        {
            PlantResourceInfo.Register(plantType, PlantHue.Plain, createResource, (int)PlantStatus.Stage7, createSeed);
        }
    }
}
