using System.Collections;
using Server.Items;

namespace Server.Mobiles
{
	public class BlacksmithMerchantBot : BaseMerchantBot
	{
		public override string ProfessionName => "Blacksmith";

		[Constructable]
		public BlacksmithMerchantBot() : base() { }

		protected override void DressUp()
		{
			AddItem(new FullApron(1109));
			AddItem(new Shirt(1150));
			AddItem(new LongPants(1157));
			AddItem(new Boots(1175));
		}

		public override bool IsItemForProfession(Item item)
		{
			return (item is BaseWeapon bw && !(bw is BaseStaff || bw is BaseRanged)) ||
			       (item is BaseArmor ba && ba.MaterialType != ArmorMaterialType.Leather && ba.MaterialType != ArmorMaterialType.Studded);
		}

		protected override void GenerateCraftedStock(Hashtable sellItems)
		{
			Item loot = Utility.RandomBool() ? (Item)Loot.RandomWeapon() : Loot.RandomArmor();
			if (loot is BaseWeapon bw)
			{
				bw.Quality = ItemQuality.Exceptional;
				BaseRunicTool.ApplyAttributesTo(bw, false, 0, Utility.RandomMinMax(1, 3), 10, 50);
			}
			else if (loot is BaseArmor ba)
			{
				ba.Quality = ItemQuality.Exceptional;
				BaseRunicTool.ApplyAttributesTo(ba, false, 0, Utility.RandomMinMax(1, 3), 10, 50);
			}

			AddItemToSale(sellItems, loot, Utility.RandomMinMax(15000, 65000), "[Работа кузнеца]");
		}

		public BlacksmithMerchantBot(Serial serial) : base(serial) { }
		public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write(0); }
		public override void Deserialize(GenericReader reader) { base.Deserialize(reader); reader.ReadInt(); }
	}
}
