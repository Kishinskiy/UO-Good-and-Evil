using System;
using System.Collections.Generic;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.Engines.Craft;

namespace Server.Custom.JewelryCrafting
{
    public static class JewelryLootMod
    {
        public static void Initialize()
        {
            EventSink.CreatureDeath += OnCreatureDeath;
            EventSink.ItemCreated += OnItemCreated;
        }

        private static void OnCreatureDeath(CreatureDeathEventArgs e)
        {
            BaseCreature bc = e.Creature as BaseCreature;
            if (bc == null || bc.Deleted || bc.Backpack == null) return;

            bool isBoss = bc is BaseChampion || bc.GetType().Name.ToLower().Contains("boss") || bc.Fame >= 20000;

            if (isBoss)
            {
                int[] epicRecipes = new int[]
                {
                    (int)JewelryRecipes.RubyDI, (int)JewelryRecipes.StarSapphireHPI,
                    (int)JewelryRecipes.PerfectEmeraldRegen, (int)JewelryRecipes.DarkSapphireManaRegen,
                    (int)JewelryRecipes.BlueDiamondCasting, (int)JewelryRecipes.TurquoiseCrit,
                    (int)JewelryRecipes.EcruCitrinePot, (int)JewelryRecipes.WhitePearlSSI,
                    (int)JewelryRecipes.BrilliantAmberFCR, (int)JewelryRecipes.VampiricShard,
                    (int)JewelryRecipes.AmuletOfLife, (int)JewelryRecipes.RingOfAstralPower,
                    (int)JewelryRecipes.BraceletOfStorms, (int)JewelryRecipes.EarringsOfVampire,
                    (int)JewelryRecipes.TalismanOfFortune, (int)JewelryRecipes.FireRubySlayer,
                    (int)JewelryRecipes.PhysicalResistGem, (int)JewelryRecipes.FireResistGem,
                    (int)JewelryRecipes.ColdResistGem, (int)JewelryRecipes.PoisonResistGem,
                    (int)JewelryRecipes.EnergyResistGem
                };

                int randomRecipeID = epicRecipes[Utility.Random(epicRecipes.Length)];
                RecipeScroll scroll = new RecipeScroll(randomRecipeID);
                bc.Backpack.DropItem(scroll);
                return;
            }

            if (bc.Map == Map.Malas || bc.Region?.Name != null && bc.Region.Name.ToLower().Contains("cemetery"))
            {
                if (Utility.RandomDouble() < 0.02)
                {
                    int[] baseRecipes = new int[]
                    {
                        (int)JewelryRecipes.SapphireMana, (int)JewelryRecipes.EmeraldHCI,
                        (int)JewelryRecipes.DiamondDCI, (int)JewelryRecipes.CitrineRegen,
                        (int)JewelryRecipes.AmethystLMC, (int)JewelryRecipes.TourmalineSSI,
                        (int)JewelryRecipes.AmberLRC
                    };

                    int randomRecipeID = baseRecipes[Utility.Random(baseRecipes.Length)];
                    bc.Backpack.DropItem(new RecipeScroll(randomRecipeID));
                }
            }
        }

        private static void OnItemCreated(ItemCreatedEventArgs e)
        {
            Item item = e.Item;
            if (item == null || item.Deleted) return;

            string typeName = item.GetType().Name.ToLower();
            if (typeName.Contains("treasurechest") || typeName.Contains("lockablecontainer"))
            {
                var levelProp = item.GetType().GetProperty("Level");
                int chestLevel = levelProp != null ? (int)levelProp.GetValue(item, null) : 0;

                if (chestLevel >= 4)
                {
                    if (Utility.RandomDouble() < 0.40)
                    {
                        Container chest = item as Container;
                        if (chest != null)
                        {
                            Array values = Enum.GetValues(typeof(JewelryRecipes));
                            JewelryRecipes randomRecipe = (JewelryRecipes)values.GetValue(Utility.Random(values.Length));

                            chest.DropItem(new RecipeScroll((int)randomRecipe));
                        }
                    }
                }
            }
        }
    }
}
