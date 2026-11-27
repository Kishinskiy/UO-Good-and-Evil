using System;
using Server;
using Server.Engines.Craft;

namespace Server.Items
{
    [Flipable(0x102E, 0x102F)]
    public class CraftNails : Item, ICraftable 
    {
        [Constructable]
        public CraftNails() : this(1)
        {
        }

        [Constructable]
        public CraftNails(int amount) : base(0x102E)
        {
            Name = "Железные гвозди";
            Stackable = true;
            Weight = 0.1;
            Amount = amount;
        }

        // Убрали override, теперь это реализация метода интерфейса ICraftable
        public int OnCraft(int quality, bool makersMark, Mobile from, CraftSystem craftSystem, Type typeRes, ITool tool, CraftItem craftItem, int resHue)
        {
            Amount = 10; // Количество гвоздей за 1 успешный крафт (из 1 слитка)
            return quality;
        }

        public CraftNails(Serial serial) : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            reader.ReadInt();
        }
    }
}
