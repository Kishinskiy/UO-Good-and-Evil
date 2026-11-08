using Server.Items;
using Server.Engines.Plants;
using Server.Targeting;

namespace Server.Custom.Farms
{
    public abstract class CustomSeed : Seed
    {
        protected CustomSeed(PlantType type) : base(type, PlantHue.Plain, true)
        {
        }

        // ОБЯЗАТЕЛЬНЫЙ КОНСТРУКТОР ДЛЯ ДЕСЕРИАЛИЗАЦИИ В СЕРВЕРЕ
        protected CustomSeed(Serial serial) : base(serial)
        {
        }

        public override void AddNameProperty(ObjectPropertyList list)
        {
            if (Amount == 1)
                list.Add(SeedName);
            else
                list.Add("{0} {1}s", Amount, SeedName);
        }

        public override void OnSingleClick(Mobile from)
        {
            if (Amount == 1)
                LabelTo(from, SeedName);
            else
                LabelTo(from, "{0} {1}s", Amount, SeedName);
        }

        protected abstract string SeedName { get; }

        public override void OnDoubleClick(Mobile from)
        {
            if (!IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1042664);
                return;
            }

            from.Target = new FarmSeedTarget(this);
            LabelTo(from, 1061916);
        }

        // Сохранение данных предмета в мир
        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0); // версия скрипта
        }

        // Загрузка данных предмета из мира
        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
        }

        private sealed class FarmSeedTarget : Target
        {
            private readonly CustomSeed _Seed;

            public FarmSeedTarget(CustomSeed seed) : base(-1, false, TargetFlags.None)
            {
                _Seed = seed;
                CheckLOS = false;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (_Seed.Deleted)
                    return;

                if (!_Seed.IsChildOf(from.Backpack))
                {
                    from.SendLocalizedMessage(1042664);
                    return;
                }

                if (targeted is PlantItem plant)
                {
                    plant.PlantSeed(from, _Seed);
                }
                else if (targeted is Item item)
                {
                    item.LabelTo(from, 1061919);
                }
                else
                {
                    from.SendLocalizedMessage(1061919);
                }
            }
        }
    }

    public class OnionSeed : CustomSeed
    {
        [Constructable]
        public OnionSeed() : base(CustomPlantTypes.Onion) { }
        public OnionSeed(Serial serial) : base(serial) { }

        protected override string SeedName => "Onion Seed";

        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write((int)0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); int version = reader.ReadInt(); }
    }

    public class PotatoSeed : CustomSeed
    {
        [Constructable]
        public PotatoSeed() : base(CustomPlantTypes.Potato) { }
        public PotatoSeed(Serial serial) : base(serial) { }

        protected override string SeedName => "Potato Seed";

        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write((int)0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); int version = reader.ReadInt(); }
    }

    public class CabbageSeed : CustomSeed
    {
        [Constructable]
        public CabbageSeed() : base(CustomPlantTypes.Cabbage) { }
        public CabbageSeed(Serial serial) : base(serial) { }

        protected override string SeedName => "Cabbage Seed";

        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write((int)0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); int version = reader.ReadInt(); }
    }

    public class CarrotSeed : CustomSeed
    {
        [Constructable]
        public CarrotSeed() : base(CustomPlantTypes.Carrot) { }
        public CarrotSeed(Serial serial) : base(serial) { }

        protected override string SeedName => "Carrot Seed";

        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write((int)0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); int version = reader.ReadInt(); }
    }

    public class LettuceSeed : CustomSeed
    {
        [Constructable]
        public LettuceSeed() : base(CustomPlantTypes.Lettuce) { }
        public LettuceSeed(Serial serial) : base(serial) { }

        protected override string SeedName => "Lettuce Seed";

        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write((int)0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); int version = reader.ReadInt(); }
    }

    public class PumpkinSeed : CustomSeed
    {
        [Constructable]
        public PumpkinSeed() : base(CustomPlantTypes.Pumpkin) { }
        public PumpkinSeed(Serial serial) : base(serial) { }

        protected override string SeedName => "Pumpkin Seed";

        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write((int)0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); int version = reader.ReadInt(); }
    }

    public class TurnipSeed : CustomSeed
    {
        [Constructable]
        public TurnipSeed() : base(CustomPlantTypes.Turnip) { }
        public TurnipSeed(Serial serial) : base(serial) { }

        protected override string SeedName => "Turnip Seed";

        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write((int)0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); int version = reader.ReadInt(); }
    }

    public class GrapesSeed : CustomSeed
    {
        [Constructable]
        public GrapesSeed() : base(CustomPlantTypes.Grapes) { }
        public GrapesSeed(Serial serial) : base(serial) { }

        protected override string SeedName => "Grapes Seed";

        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write((int)0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); int version = reader.ReadInt(); }
    }

    public class WatermelonSeed : CustomSeed
    {
        [Constructable]
        public WatermelonSeed() : base(CustomPlantTypes.Watermelon) { }
        public WatermelonSeed(Serial serial) : base(serial) { }

        protected override string SeedName => "Watermelon Seed";

        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write((int)0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); int version = reader.ReadInt(); }
    }

    public class WheatSeed : CustomSeed
    {
        [Constructable]
        public WheatSeed() : base(CustomPlantTypes.Wheat) { }
        public WheatSeed(Serial serial) : base(serial) { }

        protected override string SeedName => "Wheat Seed";

        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write((int)0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); int version = reader.ReadInt(); }
    }

    public class AppleSeed : CustomSeed
    {
        [Constructable]
        public AppleSeed() : base(CustomPlantTypes.Apple) { }
        public AppleSeed(Serial serial) : base(serial) { }

        protected override string SeedName => "Apple Seed";

        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write((int)0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); int version = reader.ReadInt(); }
    }

    public class PeachSeed : CustomSeed
    {
        [Constructable]
        public PeachSeed() : base(CustomPlantTypes.Peach) { }
        public PeachSeed(Serial serial) : base(serial) { }

        protected override string SeedName => "Peach Seed";

        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write((int)0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); int version = reader.ReadInt(); }
    }

    public class PearSeed : CustomSeed
    {
        [Constructable]
        public PearSeed() : base(CustomPlantTypes.Pear) { }
        public PearSeed(Serial serial) : base(serial) { }

        protected override string SeedName => "Pear Seed";

        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write((int)0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); int version = reader.ReadInt(); }
    }

    public class BananaSeed : CustomSeed
    {
        [Constructable]
        public BananaSeed() : base(CustomPlantTypes.Banana) { }
        public BananaSeed(Serial serial) : base(serial) { }

        protected override string SeedName => "Banana Seed";

        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write((int)0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); int version = reader.ReadInt(); }
    }

    public class LemonSeed : CustomSeed
    {
        [Constructable]
        public LemonSeed() : base(CustomPlantTypes.Lemon) { }
        public LemonSeed(Serial serial) : base(serial) { }

        protected override string SeedName => "Lemon Seed";

        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write((int)0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); int version = reader.ReadInt(); }
    }

    public class LimeSeed : CustomSeed
    {
        [Constructable]
        public LimeSeed() : base(CustomPlantTypes.Lime) { }
        public LimeSeed(Serial serial) : base(serial) { }

        protected override string SeedName => "Lime Seed";

        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write((int)0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); int version = reader.ReadInt(); }
    }

    public class CoconutSeed : CustomSeed
    {
        [Constructable]
        public CoconutSeed() : base(CustomPlantTypes.Coconut) { }
        public CoconutSeed(Serial serial) : base(serial) { }

        protected override string SeedName => "Coconut Seed";

        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write((int)0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); int version = reader.ReadInt(); }
    }

    public class DatesSeed : CustomSeed
    {
        [Constructable]
        public DatesSeed() : base(CustomPlantTypes.Dates) { }
        public DatesSeed(Serial serial) : base(serial) { }

        protected override string SeedName => "Dates Seed";

        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write((int)0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); int version = reader.ReadInt(); }
    }
}