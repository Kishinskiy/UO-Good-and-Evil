using System;
using Server;
using Server.Engines.Craft;
using Server.Items;


namespace Server.Custom.CarpentryTools
{
    public static class CustomCarpentryMod
    {
        public static void Initialize()
        {
            // Регистрируем рецепты после полной инициализации систем крафта ядра
            Timer.DelayCall(TimeSpan.FromSeconds(1.0), RegisterRecipes);
        }

        private static void RegisterRecipes()
        {
            CraftSystem cs = DefCarpentry.CraftSystem;
            if (cs == null)
                return;

            // Локализация 1044294 — стандартная категория "Containers" (Контейнеры) в меню плотника
            // Локализация 1044041 — бревна/доски (Log / Board)
            // Локализация 1024142 — гвозди (Craft Nails)

            // 1. Малый торговый ящик (50 шт.)
            int idx1 = cs.AddCraft(
                typeof(SmallTradeCrate),
                1044294,
                "Малый торговый ящик (50 шт.)",
                30.0,
                55.0,
                typeof(Log),
                1044041,
                15,
                1044351
            );
            cs.AddRes(idx1, typeof(CraftNails), 1024142, 2, "У вас недостаточно гвоздей для сборки ящика.");
            ForceNonExceptional(cs, idx1);

            // 2. Средний торговый ящик (100 шт.)
            int idx2 = cs.AddCraft(
                typeof(MediumTradeCrate),
                1044294,
                "Средний торговый ящик (100 шт.)",
                55.0,
                80.0,
                typeof(Log),
                1044041,
                25,
                1044351
            );
            cs.AddRes(idx2, typeof(CraftNails), 1024142, 4, "У вас недостаточно гвоздей для сборки ящика.");
            ForceNonExceptional(cs, idx2);

            // 3. Большой торговый ящик (200 шт.)
            int idx3 = cs.AddCraft(
                typeof(LargeTradeCrate),
                1044294,
                "Большой торговый ящик (200 шт.)",
                80.0,
                105.0,
                typeof(Log),
                1044041,
                40,
                1044351
            );
            cs.AddRes(idx3, typeof(CraftNails), 1024142, 6, "У вас недостаточно гвоздей для сборки ящика.");
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
