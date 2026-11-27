using System;
using Server;
using Server.Items;
using Server.Engines.Craft;
using Server.Custom.ItemSockets;

namespace Server.Custom.JewelryCrafting
{
    public static class JewelryToolsCraftInjector
    {
        public static void Initialize()
        {
            // Получаем стандартную систему крафта механика (Tinkering)
            CraftSystem tinkering = DefTinkering.CraftSystem;

            if (tinkering == null)
                return;

            // Проверяем, не добавлены ли рецепты ранее (защита от дублирования)
            if (tinkering.CraftItems.SearchFor(typeof(JewelryTool)) != null)
                return;

            // Добавляем Инструмент Ювелира (Лупа) в категорию "Tools" (1044049)
            // Требует: 65.0 - 95.0 Tinkering, 6 железных слитков и 1 неограненный сапфир
            int index = tinkering.AddCraft(
                typeof(JewelryTool),
                1044049, // Категория "Tools" (Инструменты) в меню Tinkering
                "Инструмент Ювелира (Лупа)",
                65.0,
                95.0,
                typeof(IronIngot),
                1044036, // "iron ingot"
                6,
                1044037  // "You do not have sufficient metal to make that."
            );
            tinkering.AddRes(index, typeof(Sapphire), "Неограненный сапфир", 1, "Требуется сапфир для шлифовки линзы.");

            // Добавляем Ювелирное зубило кузнеца (SocketChisel) в категорию "Tools" (1044049)
            // Требует: 70.0 - 100.0 Tinkering и 8 железных слитков
            index = tinkering.AddCraft(
                typeof(SocketChisel),
                1044049, // Категория "Tools" (Инструменты)
                "Ювелирное зубило кузнеца",
                70.0,
                100.0,
                typeof(IronIngot),
                1044036,
                8,
                1044037
            );
        }
    }
}
