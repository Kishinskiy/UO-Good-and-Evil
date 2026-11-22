using System;
using Server;
using Server.Engines.Craft;
using Server.Items;
using Server.Mobiles;
using Server.Engines.CannedEvil;

namespace Server.Custom.ItemSockets
{
    public static class SocketChiselCraftAndLoot
    {
        public const int SocketChiselRecipeID = 2001;
        private const double BossDropChance = 0.30; // 30% шанс выпадения свитка рецепта с боссов

        public static void Initialize()
        {
            // Откладываем регистрацию на 1 тик, чтобы DefBlacksmithy.CraftSystem уже гарантированно инициализировался
            Timer.DelayCall(TimeSpan.Zero, RegisterBlacksmithRecipe);

            // Подписываемся на смерть боссов для дропа свитка рецепта
            EventSink.CreatureDeath += OnCreatureDeath;
        }

        private static void RegisterBlacksmithRecipe()
        {
            CraftSystem cs = DefBlacksmithy.CraftSystem;
            if (cs == null)
                return;

            // Добавляем рецепт в книгу кузнеца (категория "Инструменты" / "Tools" или любая существующая)
            int index = cs.AddCraft(
                typeof(SocketChisel),
                "Инструменты",
                "Ювелирное зубило кузнеца (25 зарядов)",
                85.0,
                115.0,
                typeof(ValoriteIngot),
                "Слитки валорита",
                15,
                "Вам не хватает слитков валорита для закалки резца."
            );

            cs.AddRes(index, typeof(GoldIngot), "Золотые слитки", 5, "Нужны золотые слитки для оправы рукояти.");
            cs.AddRecipe(index, SocketChiselRecipeID);
        }

        private static void OnCreatureDeath(CreatureDeathEventArgs e)
        {
            BaseCreature victim = e.Creature as BaseCreature;
            Container corpse = e.Corpse;

            if (victim == null || corpse == null || corpse.Deleted)
                return;

            if (victim.Summoned || victim.Controlled || victim.NoLootOnDeath)
                return;

            // Проверяем, является ли убитый монстр боссом (Чемпион, Peerless или элитный босс с Fame >= 20000)
            bool isBoss = victim is BaseChampion
                          || victim is BasePeerless
                          || victim.Fame >= 20000
                          || victim.GetType().Name.IndexOf("Boss", StringComparison.OrdinalIgnoreCase) >= 0;

            if (isBoss && Utility.RandomDouble() < BossDropChance)
            {
                corpse.DropItem(new RecipeScroll(SocketChiselRecipeID));

                Mobile topDamager = victim.GetHighestDamager();
                if (topDamager != null && topDamager.Player)
                {
                    topDamager.SendMessage(0x35, "В добыче поверженного босса обнаружен редкий кузнечный рецепт: Ювелирное зубило!");
                }
            }
        }
    }
}
