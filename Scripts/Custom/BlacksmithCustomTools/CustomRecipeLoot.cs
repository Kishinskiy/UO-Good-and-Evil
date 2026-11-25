using System;
using Server.Engines.CannedEvil;
using Server.Items;
using Server.Mobiles;

namespace Server.Custom.BlacksmithTools
{
	public static class CustomRecipeLoot
	{
		private const double BossDropChance = 0.25; // 25% шанс дропа редкого рецепта с босса

		public static void OnCreatureDeath(CreatureDeathEventArgs e)
		{
			BaseCreature victim = e.Creature as BaseCreature;
			Container corpse = e.Corpse;

			if (victim == null || corpse == null || corpse.Deleted)
				return;

			if (victim.Summoned || victim.Controlled || victim.NoLootOnDeath)
				return;

			bool isBoss = victim is BaseChampion
			              || victim is BasePeerless
			              || victim.Fame >= 20000
			              || victim.GetType().Name.IndexOf("Boss", StringComparison.OrdinalIgnoreCase) >= 0;

			if (isBoss && Utility.RandomDouble() < BossDropChance)
			{
				int[] allRecipes =
				{
					(int)CustomSmithRecipes.SocketChisel,
					(int)CustomSmithRecipes.MasterTemperHammer,
					(int)CustomSmithRecipes.RunicHammer
				};

				int chosenRecipe = allRecipes[Utility.Random(allRecipes.Length)];
				corpse.DropItem(new RecipeScroll(chosenRecipe));

				Mobile killer = victim.GetHighestDamager();
				if (killer is BaseCreature pet && pet.Controlled && pet.ControlMaster != null)
					killer = pet.ControlMaster;

				if (killer != null && killer.Player)
					killer.SendMessage(0x35, "В останках босса вы обнаружили древний кузнечный чертёж!");
			}
		}
	}
}
