using System;
using Server;
using Server.Engines.Craft;
using Server.Items;
using Server.Mobiles;
using Server.Custom.ItemSockets;

namespace Server.Custom.BlacksmithTools
{
    public enum CustomSmithRecipes
    {
        SocketChisel = 2001,        // Ювелирное зубило кузнеца
        MasterTemperHammer = 2002,  // Молот глубокой закалки
        RunicHammer = 2003          // Рунный кузнечный молот
    }

    // Обёртка над стандартным RunicHammer с пустым конструктором для системы крафта
    public class CraftableVeriteRunicHammer : RunicHammer
    {
        [Constructable]
        public CraftableVeriteRunicHammer() : base(CraftResource.Verite, 15)
        {
            Name = "Рунный кузнечный молот (Verite)";
        }

        public CraftableVeriteRunicHammer(Serial serial) : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            reader.ReadInt();
        }
    }

    public static class CustomBlacksmithMod
    {
        public static void Initialize()
        {
            Timer.DelayCall(TimeSpan.FromSeconds(1.0), RegisterRecipes);
            // Подписываемся на событие только ОДИН раз (в CustomRecipeLoot убираем Initialize)
            EventSink.CreatureDeath += CustomRecipeLoot.OnCreatureDeath;
        }

        private static void RegisterRecipes()
        {
            CraftSystem cs = DefBlacksmithy.CraftSystem;
            if (cs == null)
                return;

            // =========================================================================
            // 1. БАЗОВЫЕ ИНСТРУМЕНТЫ КУЗНЕЦА (Доступны с самого начала игры без рецептов)
            // =========================================================================

            cs.AddCraft(
                typeof(Tongs),
                1011173,
                1024028,
                0.0,
                50.0,
                typeof(IronIngot),
                1044036,
                2,
                1044037
            );

            cs.AddCraft(
                typeof(SmithyHammer),
                1011173,
                1025091,
                10.0,
                60.0,
                typeof(IronIngot),
                1044036,
                4,
                1044037
            );

            // =========================================================================
            // 2. МАСТЕРСКИЕ ИНСТРУМЕНТЫ (Требуют изучения свитка рецепта с боссов)
            // =========================================================================

            // Ювелирное зубило кузнеца
            int idx1 = cs.AddCraft(
                typeof(SocketChisel),
                1011173,
                "Ювелирное зубило кузнеца",
                85.0,
                115.0,
                typeof(ValoriteIngot),
                1044030,
                15,
                1044037
            );
            cs.AddRes(idx1, typeof(GoldIngot), 1044027, 5, 1044037);
            cs.AddRecipe(idx1, (int)CustomSmithRecipes.SocketChisel);
            ForceNonExceptional(cs, idx1);

            // Молот глубокой закалки (исправлен тип на MasterTemperHammer)
            int idx2 = cs.AddCraft(
                typeof(MasterTemperHammer),
                1011173,
                "Молот глубокой закалки",
                95.0,
                120.0,
                typeof(ValoriteIngot),
                1044030,
                20,
                1044037
            );
            cs.AddRes(idx2, typeof(BlueDiamond), 1032696, 1, 1044240);
            cs.AddRecipe(idx2, (int)CustomSmithRecipes.MasterTemperHammer);
            ForceNonExceptional(cs, idx2);

            // Рунный кузнечный молот (использует класс с пустым конструктором)
            int idx3 = cs.AddCraft(
                typeof(CraftableVeriteRunicHammer),
                1011173,
                "Рунный кузнечный молот",
                90.0,
                115.0,
                typeof(VeriteIngot),
                1044029,
                15,
                1044037
            );
            cs.AddRes(idx3, typeof(FireRuby), 1032695, 1, 1044240);
            cs.AddRecipe(idx3, (int)CustomSmithRecipes.RunicHammer);
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
