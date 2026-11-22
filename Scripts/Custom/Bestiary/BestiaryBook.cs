using System;
using Server;
using Server.Items;
using Server.Mobiles;

namespace Server.Custom.Bestiary
{
    public class BestiaryBook : Item
    {
        [Constructable]
        public BestiaryBook() : base(0x2252) // ID красивой книги в кожаном переплёте
        {
            Name = "Энциклопедия чудовищ Британии (Бестиарий)";
            Hue = 0x59B; // Глубокий тёмно-синий/фиолетовый оттенок
            Weight = 1.0;
            LootType = LootType.Blessed; // Книга не выпадает при смерти игрока
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);
            list.Add(1060658, "Предназначение\tХранит знания о поверженных врагах");
            list.Add(1060659, "Использование\tДвойной клик для чтения");
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (!IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
                return;
            }

            if (from is PlayerMobile pm)
            {
                pm.CloseGump(typeof(BestiaryGump));
                pm.SendGump(new BestiaryGump(pm, 0));
                pm.PlaySound(0x55); // Звук перелистывания страниц
            }
        }

        public BestiaryBook(Serial serial) : base(serial)
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
