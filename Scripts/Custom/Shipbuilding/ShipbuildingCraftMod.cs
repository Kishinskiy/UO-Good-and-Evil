using System;
using Server;
using Server.Engines.Craft;
using Server.Items;

namespace Server.Custom.Shipbuilding
{
    public static class ShipbuildingCraftMod
    {
        public static void Initialize()
        {
            Timer.DelayCall(TimeSpan.FromSeconds(1.5), RegisterRecipes);
        }

        private static void RegisterRecipes()
        {
            CraftSystem cs = DefCarpentry.CraftSystem;
            if (cs == null)
                return;

            // Регистрируем в категорию "Other" (1044294) или создаём новую вкладку "Судостроение"
            string category = "Судостроение";

            // 1. Закладка малого шлюпа
            int idx1 = cs.AddCraft(
                typeof(SmallShipyardDeed),
                category,
                "Закладка верфи: Малый шлюп",
                60.0,
                85.0,
                typeof(Log),
                1044041,
                50,
                1044351
            );
            cs.AddRes(idx1, typeof(CraftNails), "Железные гвозди", 10, "Недостаточно гвоздей для разметки стапеля.");
            cs.AddRes(idx1, typeof(BlankScroll), "Чистый свиток", 1, "Требуется чистый свиток для чертежа.");
            ForceNonExceptional(cs, idx1);

            // 2. Закладка среднего корабля
            int idx2 = cs.AddCraft(
                typeof(MediumShipyardDeed),
                category,
                "Закладка верфи: Средний корабль",
                75.0,
                100.0,
                typeof(Log),
                1044041,
                100,
                1044351
            );
            cs.AddRes(idx2, typeof(CraftNails), "Железные гвозди", 20, "Недостаточно гвоздей для разметки стапеля.");
            cs.AddRes(idx2, typeof(BlankScroll), "Чистый свиток", 2, "Требуются чистые свитки для чертежа.");
            ForceNonExceptional(cs, idx2);

            // 3. Закладка большого дракара
            int idx3 = cs.AddCraft(
                typeof(LargeDragonShipyardDeed),
                category,
                "Закладка верфи: Большой дракар",
                90.0,
                115.0,
                typeof(Log),
                1044041,
                200,
                1044351
            );
            cs.AddRes(idx3, typeof(CraftNails), "Железные гвозди", 40, "Недостаточно гвоздей для разметки стапеля.");
            cs.AddRes(idx3, typeof(BlankScroll), "Чистый свиток", 5, "Требуются чистые свитки для чертежа.");
            ForceNonExceptional(cs, idx3);
        }

        private static void ForceNonExceptional(CraftSystem cs, int index)
        {
            CraftItem item = cs.CraftItems.GetAt(index);
            if (item != null)
                item.ForceNonExceptional = true;
        }
    }
}