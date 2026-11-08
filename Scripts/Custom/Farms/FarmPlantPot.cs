using Server.Engines.Plants;

namespace Server.Custom.Farms
{
    public class FarmPlantPot : PlantItem
    {
        [Constructable]
        public FarmPlantPot() : base(false)
        {
            Name = "farm plant pot";
        }

        public FarmPlantPot(Serial serial) : base(serial)
        {
        }
    }
}
