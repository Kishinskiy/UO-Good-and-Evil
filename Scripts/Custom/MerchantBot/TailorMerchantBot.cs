using System;
using System.Collections;
using Server.Items;

namespace Server.Mobiles
{
    public class TailorMerchantBot : BaseMerchantBot
    {
        public override string ProfessionName => "Tailor";

        private static readonly Type[] LeatherTypes = new Type[]
        {
            typeof(LeatherChest), typeof(LeatherLegs), typeof(LeatherArms),
            typeof(LeatherGloves), typeof(LeatherGorget), typeof(StuddedChest)
        };

        [Constructable]
        public TailorMerchantBot() : base() { }

        protected override void DressUp()
        {
            AddItem(new FancyShirt(1284));
            AddItem(new LongPants(1150));
            AddItem(new Sandals(1175));
        }

        public override bool IsItemForProfession(Item item)
        {
            return item is BaseClothing ||
                   (item is BaseArmor ba && (ba.MaterialType == ArmorMaterialType.Leather || ba.MaterialType == ArmorMaterialType.Studded));
        }

        protected override void GenerateCraftedStock(Hashtable sellItems)
        {
            Item loot;
            int price;

            if (Utility.RandomBool())
            {
                loot = Loot.RandomClothing();
                loot.Hue = Utility.RandomDyedHue();
                price = Utility.RandomMinMax(3000, 12000);
            }
            else
            {
                loot = Loot.Construct(LeatherTypes);
                if (loot is BaseArmor la)
                {
                    la.Quality = ItemQuality.Exceptional;
                    BaseRunicTool.ApplyAttributesTo(la, false, 0, Utility.RandomMinMax(1, 3), 10, 50);
                }
                price = Utility.RandomMinMax(15000, 50000);
            }

            AddItemToSale(sellItems, loot, price, "[Работа портного]");
        }

        public TailorMerchantBot(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write(0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); reader.ReadInt(); }
    }
}
