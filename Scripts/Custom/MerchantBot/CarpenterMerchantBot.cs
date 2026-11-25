using System;
using System.Collections;
using Server.Items;

namespace Server.Mobiles
{
	public class CarpenterMerchantBot : BaseMerchantBot
	{
		public override string ProfessionName => "Carpenter";

		private static readonly Type[] StockTypes = new Type[]
		{
			typeof(WoodenChest), typeof(MetalGoldenChest), typeof(Armoire),
			typeof(CherryArmoire), typeof(MapleArmoire), typeof(WoodenBookcase),
			typeof(WoodenThrone), typeof(LargeTable), typeof(WritingTable),
			typeof(QuarterStaff), typeof(GnarledStaff), typeof(Bow), typeof(Crossbow)
		};

		[Constructable]
		public CarpenterMerchantBot() : base() { }

		protected override void DressUp()
		{
			AddItem(new HalfApron(1141));
			AddItem(new FancyShirt(1153));
			AddItem(new ShortPants(1157));
			AddItem(new Shoes(1175));
		}

		public override bool IsItemForProfession(Item item)
		{
			return item is BaseContainer || item is BaseStaff || item is BaseRanged ||
			       Array.IndexOf(StockTypes, item.GetType()) >= 0;
		}

		protected override void GenerateCraftedStock(Hashtable sellItems)
		{
			Item loot = Loot.Construct(StockTypes);
			if (loot is BaseWeapon bw) bw.Quality = ItemQuality.Exceptional;
			if (loot is LockableContainer lc)
			{
				lc.Locked = false;
				lc.MaxLockLevel = 100;
				lc.LockLevel = 0;
			}

			AddItemToSale(sellItems, loot, Utility.RandomMinMax(5000, 30000), "[Работа плотника]");
		}

		public CarpenterMerchantBot(Serial serial) : base(serial) { }
		public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write(0); }
		public override void Deserialize(GenericReader reader) { base.Deserialize(reader); reader.ReadInt(); }
	}
}
