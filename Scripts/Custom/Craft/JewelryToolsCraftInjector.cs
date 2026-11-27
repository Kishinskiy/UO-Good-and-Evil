using System;
using Server;
using Server.Items;
using Server.Engines.Craft;

namespace Server.Custom.JewelryCrafting
{
    public static class JewelryToolsCraftInjector
    {
        public static void Initialize()
        {
            // Откладываем регистрацию на 1 секунду, чтобы DefTinkering гарантированно инициализировался
            Timer.DelayCall(TimeSpan.FromSeconds(1.0), RegisterRecipes);
        }

        private static void RegisterRecipes()
        {
            CraftSystem tinkering = DefTinkering.CraftSystem;
            if (tinkering == null)
                return;

            // Защита от повторного добавления
            if (tinkering.CraftItems.SearchFor(typeof(JewelryTool)) != null)
                return;

            // Добавляем Инструмент Ювелира (Лупа) во вкладку Tools (1011173)
            int idx = tinkering.AddCraft(
                typeof(JewelryTool),
                1011173, // Вкладка "Tools" (как и в DefBlacksmithy)
                "Инструмент Ювелира (Лупа)",
                65.0,
                95.0,
                typeof(IronIngot),
                1044036,
                6,
                1044037
            );
            tinkering.AddRes(idx, typeof(Sapphire), "Неограненный сапфир", 1, "Требуется сапфир для шлифовки линзы.");

            CraftItem item = tinkering.CraftItems.GetAt(idx);
            if (item != null)
                item.ForceNonExceptional = true;
        }
    }
}
