using System;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.Custom.JewelryCrafting;

namespace Server.Engines.Craft
{
    public enum JewelryRecipes
    {
        // Базовые камни (Рецепты огранки)
        SapphireMana = 1001,
        EmeraldHCI = 1002,
        DiamondDCI = 1003,
        CitrineRegen = 1004,
        AmethystLMC = 1005,
        TourmalineSSI = 1006,
        AmberLRC = 1007,

        // Высшие / Эпические камни (Ванильные редкие ресурсные камни)
        RubyDI = 1101,
        StarSapphireHPI = 1102,
        FireRubySlayer = 1103,
        PerfectEmeraldRegen = 1104,
        DarkSapphireManaRegen = 1105,
        BlueDiamondCasting = 1106,
        TurquoiseCrit = 1107,
        EcruCitrinePot = 1108,
        WhitePearlSSI = 1109,        // Новое: Белая жемчужина
        BrilliantAmberFCR = 1110,    // Новое: Сияющий янтарь

        // Сверхредкие артефактные сокеты
        VampiricShard = 1201,

        // Высшая бижутерия
        AmuletOfLife = 1301,
        RingOfAstralPower = 1302,
        BraceletOfStorms = 1303,
        EarringsOfVampire = 1304,
        TalismanOfFortune = 1305,

        // Камни резистов (Ванильные соответствия)
        PhysicalResistGem = 1401, // Из WhitePearl
        FireResistGem = 1402,     // Из FireRuby
        ColdResistGem = 1403,     // Из Sapphire
        PoisonResistGem = 1404,   // Из Emerald
        EnergyResistGem = 1405    // Из Turquoise
    }

    public class DefJewelryCrafting : CraftSystem
    {
        public override SkillName MainSkill => SkillName.Tinkering;
        public override int GumpTitleNumber => 0;

        private static CraftSystem m_CraftSystem;
        public static CraftSystem CraftSystem => m_CraftSystem ?? (m_CraftSystem = new DefJewelryCrafting());

        public override double GetChanceAtMin(CraftItem item) => 0.0;

        private DefJewelryCrafting() : base(1, 1, 1.25)
        {
        }

        public override int CanCraft(Mobile from, ITool tool, Type itemType)
        {
            int num = 0;
            if (tool == null || tool.Deleted || tool.UsesRemaining <= 0) return 1044038;
            if (!tool.CheckAccessible(from, ref num)) return num;

            bool anvil, forge;
            DefBlacksmithy.CheckAnvilAndForge(from, 2, out anvil, out forge);
            if (!forge)
            {
                from.SendMessage(0x22, "Для работы ювелирным инструментом вы должны стоять у кузнечного горна!");
                return 1044628;
            }

            return 0;
        }

        public override void PlayCraftEffect(Mobile from) => from.PlaySound(0x1F7);

        public override int PlayEndingEffect(Mobile from, bool failed, bool lostMaterial, bool toolBroken, int quality, bool makersMark, CraftItem item)
        {
            if (toolBroken) from.SendMessage(0x22, "Ваш ювелирный инструмент пришел в негодность.");
            if (failed)
            {
                from.PlaySound(0x41);
                return lostMaterial ? 1044043 : 1044157;
            }
            from.PlaySound(0x4F);
            return 1044154;
        }

        public override void InitCraftList()
        {
            int index = -1;

            // =========================================================================
            // 🟢 КАТЕГОРИЯ 1: БАЗОВАЯ ОГРАНКА (ВАНИЛЬНЫЙ СТАНДАРТ)
            // =========================================================================

            // Сапфир Разума (+8 Max Mana)
            index = AddCraft(typeof(JewelryGem), "Базовая огранка", "Ограненный Сапфир Разума (+8 Max Mana)", 75.0, 105.0, typeof(Sapphire), "Неограненный сапфир", 3, "Вам не хватает сапфиров.");
            AddRes(index, typeof(IronIngot), "Железные слитки", 5, "Нужно железо для оправы.");
            SetData(index, "sapphire");
            AddCreateItem(index, (from, craftItem, tool) =>
            {
                double tink = from.Skills[SkillName.Tinkering].Value;
                double med = from.Skills[SkillName.Meditation].Value;
                double avg = (tink + med) / 2.0;
                double quality = avg <= 50.0 ? 0.5 : 0.5 + ((avg - 50.0) * 0.035);
                return new JewelryGem("sapphire", quality, 0x1EA7, 1144);
            });

            // Изумруд Точности (+5% HCI)
            index = AddCraft(typeof(JewelryGem), "Базовая огранка", "Ограненный Изумруд Точности (+5% HCI)", 80.0, 110.0, typeof(Emerald), "Неограненный изумруд", 3, "Вам не хватает изумрудов.");
            AddRes(index, typeof(IronIngot), "Железные слитки", 5, "Нужно железо для оправы.");
            SetData(index, "emerald");
            AddCreateItem(index, (from, craftItem, tool) =>
            {
                double tink = from.Skills[SkillName.Tinkering].Value;
                double quality = tink <= 50.0 ? 0.5 : 0.5 + ((tink - 50.0) * 0.035);
                return new JewelryGem("emerald", quality, 0x1EA7, 1168);
            });

            // Алмаз Защиты (+5% DCI)
            index = AddCraft(typeof(JewelryGem), "Базовая огранка", "Ограненный Алмаз Защиты (+5% DCI)", 80.0, 110.0, typeof(Diamond), "Неограненный алмаз", 3, "Вам не хватает алмазов.");
            AddRes(index, typeof(IronIngot), "Железные слитки", 5, "Нужно железо для оправы.");
            SetData(index, "diamond");
            AddCreateItem(index, (from, craftItem, tool) =>
            {
                double tink = from.Skills[SkillName.Tinkering].Value;
                double quality = tink <= 50.0 ? 0.5 : 0.5 + ((tink - 50.0) * 0.035);
                return new JewelryGem("diamond", quality, 0x1EA7, 1150);
            });

            // Цитрин Выносливости (+5 Stamina Regen + 30 Luck)
            index = AddCraft(typeof(JewelryGem), "Базовая огранка", "Ограненный Цитрин Выносливости (+5 Stam Regen / +30 Luck)", 85.0, 115.0, typeof(Citrine), "Неограненный цитрин", 3, "Вам не хватает цитринов.");
            AddRes(index, typeof(IronIngot), "Железные слитки", 5, "Нужно железо для оправы.");
            SetData(index, "citrine");
            AddCreateItem(index, (from, craftItem, tool) =>
            {
                double tink = from.Skills[SkillName.Tinkering].Value;
                double quality = tink <= 50.0 ? 0.5 : 0.5 + ((tink - 50.0) * 0.035);
                return new JewelryGem("citrine", quality, 0x1EA7, 1166);
            });

            // Янтарь Снабжения (+10% LRC)
            index = AddCraft(typeof(JewelryGem), "Базовая огранка", "Ограненный Янтарь Снабжения (+10% LRC)", 85.0, 115.0, typeof(Amber), "Неограненный янтарь", 3, "Вам не хватает янтаря.");
            AddRes(index, typeof(IronIngot), "Железные слитки", 5, "Нужно железо для оправы.");
            SetData(index, "amber");
            AddCreateItem(index, (from, craftItem, tool) =>
            {
                double tink = from.Skills[SkillName.Tinkering].Value;
                double mag = from.Skills[SkillName.Magery].Value;
                double avg = (tink + mag) / 2.0;
                double quality = avg <= 50.0 ? 0.5 : 0.5 + ((avg - 50.0) * 0.035);
                return new JewelryGem("amber", quality, 0x1EA7, 1170);
            });


            // =========================================================================
            // 💎 КАТЕГОРИЯ 2: ПРОДВИНУТАЯ ОГРАНКА (ВАНИЛЬНЫЙ СТАНДАРТ)
            // =========================================================================

            // Рубин Урона (+15% DI)
            index = AddCraft(typeof(JewelryGem), "Продвинутая огранка", "Ограненный Рубин Урона (+15% DI)", 90.0, 115.0, typeof(Ruby), "Неограненный рубин", 5, "Вам не хватает рубинов.");
            AddRes(index, typeof(GoldIngot), "Золотые слитки", 10, "Нужно золото для благородной оправы.");
            AddRecipe(index, (int)JewelryRecipes.RubyDI);
            SetData(index, "ruby");
            AddCreateItem(index, (from, craftItem, tool) =>
            {
                double tink = from.Skills[SkillName.Tinkering].Value;
                double tac = from.Skills[SkillName.Tactics].Value;
                double avg = (tink + tac) / 2.0;
                double quality = avg <= 50.0 ? 0.5 : 0.5 + ((avg - 50.0) * 0.045);
                return new JewelryGem("ruby", quality, 0x1EA7, 1157);
            });

            // Аметист Тайных Сил (-5% Lower Mana Cost)
            index = AddCraft(typeof(JewelryGem), "Продвинутая огранка", "Ограненный Аметист Экономии (-5% LMC)", 90.0, 115.0, typeof(Amethyst), "Неограненный аметист", 5, "Вам не хватает аметистов.");
            AddRes(index, typeof(GoldIngot), "Золотые слитки", 10, "Нужно золото для оправы.");
            AddRecipe(index, (int)JewelryRecipes.AmethystLMC);
            SetData(index, "amethyst");
            AddCreateItem(index, (from, craftItem, tool) =>
            {
                double tink = from.Skills[SkillName.Tinkering].Value;
                double eval = from.Skills[SkillName.EvalInt].Value;
                double avg = (tink + eval) / 2.0;
                double quality = avg <= 50.0 ? 0.5 : 0.5 + ((avg - 50.0) * 0.045);
                return new JewelryGem("amethyst", quality, 0x1EA7, 1162);
            });

            // Турмалин Скорости (+5% SSI)
            index = AddCraft(typeof(JewelryGem), "Продвинутая огранка", "Ограненный Турмалин Скорости (+5% SSI)", 95.0, 120.0, typeof(Tourmaline), "Неограненный турмалин", 5, "Вам не хватает турмалинов.");
            AddRes(index, typeof(GoldIngot), "Золотые слитки", 10, "Нужно золото для оправы.");
            AddRecipe(index, (int)JewelryRecipes.TourmalineSSI);
            SetData(index, "tourmaline");
            AddCreateItem(index, (from, craftItem, tool) =>
            {
                double tink = from.Skills[SkillName.Tinkering].Value;
                double tac = from.Skills[SkillName.Tactics].Value;
                double avg = (tink + tac) / 2.0;
                double quality = avg <= 50.0 ? 0.5 : 0.5 + ((avg - 50.0) * 0.045);
                return new JewelryGem("tourmaline", quality, 0x1EA7, 1190);
            });
            // Звёздный Сапфир Жизни (+8 Max HP)
            index = AddCraft(typeof(JewelryGem), "Продвинутая огранка", "Звёздный Сапфир Жизни (+8 Max HP)", 95.0, 120.0, typeof(StarSapphire), "Неограненный звёздный сапфир", 4, "Вам не хватает звёздных сапфиров.");
            AddRes(index, typeof(GoldIngot), "Золотые слитки", 12, "Нужно золото для оправы.");
            AddRecipe(index, (int)JewelryRecipes.StarSapphireHPI);
            SetData(index, "starsapphire");
            AddCreateItem(index, (from, craftItem, tool) =>
            {
                double tink = from.Skills[SkillName.Tinkering].Value;
                double anat = from.Skills[SkillName.Anatomy].Value;
                double avg = (tink + anat) / 2.0;
                double quality = avg <= 50.0 ? 0.5 : 0.5 + ((avg - 50.0) * 0.045);
                return new JewelryGem("starsapphire", quality, 0x1EA7, 1202);
            });
            // =========================================================================
            // 👑 КАТЕГОРИЯ 3: МИСТИЧЕСКАЯ ЮВЕЛИРКА (РЕДКИЕ РЕСУРСНЫЕ КАМНИ UO)
            // =========================================================================
            // Совершенный Изумруд Регенерации (+3 HP Regen)
            index = AddCraft(typeof(JewelryGem), "Мистическая огранка", "Совершенный Изумруд Регенерации (+3 HP Regen)", 100.0, 120.0, typeof(PerfectEmerald), "Совершенный изумруд", 2, "Требуется редкий совершенный изумруд.");
            AddRes(index, typeof(GoldIngot), "Золотые слитки", 15, "Нужно золото высшей пробы.");
            AddRecipe(index, (int)JewelryRecipes.PerfectEmeraldRegen);
            SetData(index, "perfectemerald");
            AddCreateItem(index, (from, craftItem, tool) =>
            {
                double tink = from.Skills[SkillName.Tinkering].Value;
                double anat = from.Skills[SkillName.Anatomy].Value;
                double avg = (tink + anat) / 2.0;
                double quality = avg <= 50.0 ? 0.5 : 0.5 + ((avg - 50.0) * 0.05);
                return new JewelryGem("perfectemerald", quality, 0x1EA7, 1150);
            });
            // Тёмный Сапфир Потока (+3 Mana Regen)
            index = AddCraft(typeof(JewelryGem), "Мистическая огранка", "Тёмный Сапфир Потока (+3 Mana Regen)", 100.0, 120.0, typeof(DarkSapphire), "Тёмный сапфир", 2, "Требуется редкий тёмный сапфир.");
            AddRes(index, typeof(GoldIngot), "Золотые слитки", 15, "Нужно золото высшей пробы.");
            AddRecipe(index, (int)JewelryRecipes.DarkSapphireManaRegen);
            SetData(index, "darksapphire");
            AddCreateItem(index, (from, craftItem, tool) =>
            {
                double tink = from.Skills[SkillName.Tinkering].Value;
                double med = from.Skills[SkillName.Meditation].Value;
                double avg = (tink + med) / 2.0;
                double quality = avg <= 50.0 ? 0.5 : 0.5 + ((avg - 50.0) * 0.05);
                return new JewelryGem("darksapphire", quality, 0x1EA7, 1240);
            });
            // Голубой Алмаз Времени (+1 Faster Casting)
            index = AddCraft(typeof(JewelryGem), "Мистическая огранка", "Голубой Алмаз Времени (+1 Faster Casting)", 105.0, 125.0, typeof(BlueDiamond), "Голубой алмаз", 2, "Требуется редкий голубой алмаз.");
            AddRes(index, typeof(VeriteIngot), "Слитки верита", 10, "Нужна тугоплавкая оправа из верита.");
            AddRecipe(index, (int)JewelryRecipes.BlueDiamondCasting);
            SetData(index, "bluediamond");
            AddCreateItem(index, (from, craftItem, tool) =>
            {
                double tink = from.Skills[SkillName.Tinkering].Value;
                double mag = from.Skills[SkillName.Magery].Value;
                double avg = (tink + mag) / 2.0;
                double quality = avg <= 50.0 ? 0.5 : 0.5 + ((avg - 50.0) * 0.05);
                return new JewelryGem("bluediamond", quality, 0x1EA7, 1266);
            });
            // Пронзающая Бирюза Грома (35% Hit Lightning)
            index = AddCraft(typeof(JewelryGem), "Мистическая огранка", "Пронзающая Бирюза Грома (35% Hit Lightning)", 105.0, 125.0, typeof(Turquoise), "Драгоценная бирюза", 3, "Требуется бирюза.");
            AddRes(index, typeof(ValoriteIngot), "Слитки валорита", 10, "Нужна валоритовая оправа для проводимости.");
            AddRecipe(index, (int)JewelryRecipes.TurquoiseCrit);
            SetData(index, "turquoise");
            AddCreateItem(index, (from, craftItem, tool) =>
            {
                double tink = from.Skills[SkillName.Tinkering].Value;
                double tactics = from.Skills[SkillName.Tactics].Value;
                double avg = (tink + tactics) / 2.0;
                double quality = avg <= 50.0 ? 0.5 : 0.5 + ((avg - 50.0) * 0.05);
                return new JewelryGem("turquoise", quality, 0x1EA7, 1315);
            });
            // Экру-Цитрин Знахаря (+50% Enhance Potions)
            index = AddCraft(typeof(JewelryGem), "Мистическая огранка", "Экру-Цитрин Знахаря (+50% Enhance Potions)", 105.0, 125.0, typeof(EcruCitrine), "Редкий экру-цитрин", 2, "Требуется экру-цитрин.");
            AddRes(index, typeof(GoldIngot), "Золотые слитки", 20, "Нужно много золота.");
            AddRecipe(index, (int)JewelryRecipes.EcruCitrinePot);
            SetData(index, "ecrucitrine");
            AddCreateItem(index, (from, craftItem, tool) =>
            {
                double tink = from.Skills[SkillName.Tinkering].Value;
                double alch = from.Skills[SkillName.Alchemy].Value;
                double avg = (tink + alch) / 2.0;
                double quality = avg <= 50.0 ? 0.5 : 0.5 + ((avg - 50.0) * 0.05);
                return new JewelryGem("ecrucitrine", quality, 0x1EA7, 1166);
            });
            // НОВОЕ: Белая Жемчужина Стремительности (+10% SSI - добывается Fishing)
            index = AddCraft(typeof(JewelryGem), "Мистическая огранка", "Белая Жемчужина Стремительности (+10% SSI)", 105.0, 125.0, typeof(WhitePearl), "Белая жемчужина", 2, "Требуется редкая белая жемчужина.");
            AddRes(index, typeof(VeriteIngot), "Слитки верита", 10, "Нужна легкая оправа из верита.");
            AddRecipe(index, (int)JewelryRecipes.WhitePearlSSI);
            SetData(index, "whitepearl");
            AddCreateItem(index, (from, craftItem, tool) =>
            {
                double tink = from.Skills[SkillName.Tinkering].Value;
                double fish = from.Skills[SkillName.Fishing].Value;
                double avg = (tink + Math.Max(tink, fish)) / 2.0;
                double quality = avg <= 50.0 ? 0.5 : 0.5 + ((avg - 50.0) * 0.05);
                return new JewelryGem("whitepearl", quality, 0x1EA7, 1153);
            });
            // НОВОЕ: Сияющий Янтарь Чародея (+1 FCR / +10% SDI - добывается Lumberjacking)
            index = AddCraft(typeof(JewelryGem), "Мистическая огранка", "Сияющий Янтарь Чародея (+1 FCR / +10% SDI)", 105.0, 125.0, typeof(BrilliantAmber), "Сияющий янтарь", 2, "Требуется редкий сияющий янтарь.");
            AddRes(index, typeof(GoldIngot), "Золотые слитки", 15, "Нужно золото для усиления магии.");
            AddRecipe(index, (int)JewelryRecipes.BrilliantAmberFCR);
            SetData(index, "brilliantamber");
            AddCreateItem(index, (from, craftItem, tool) =>
            {
                double tink = from.Skills[SkillName.Tinkering].Value;
                double eval = from.Skills[SkillName.EvalInt].Value;
                double avg = (tink + eval) / 2.0;
                double quality = avg <= 50.0 ? 0.5 : 0.5 + ((avg - 50.0) * 0.05);
                return new JewelryGem("brilliantamber", quality, 0x1EA7, 1281);
            });
            // МИСТИЧЕСКИЙ ОСКОЛОК ВАМПИРИЗМА (20% Hit Leech)
            index = AddCraft(typeof(JewelryGem), "Мистическая огранка", "Мистический Осколок Вампиризма (20% Hit Leech)", 115.0, 125.0, typeof(FireRuby), "Основа из огненного рубина", 3, "Требуется огненный рубин для удержания душ.");
            AddRes(index, typeof(DaemonBlood), "Кровь древнего демона", 20, "Нужна кровь демона.");
            Type curseShard = ScriptCompiler.FindTypeByName("CurseShardCraftable");
            if (curseShard != null) AddRes(index, curseShard, "Осколок Скверны", 5, "Вам не хватает осколков скверны.");
            Type arcaneDust = ScriptCompiler.FindTypeByName("ArcaneDust");
            if (arcaneDust != null) AddRes(index, arcaneDust, "Арканическая пыль", 15, "Нужна арканическая пыль для связки.");
            AddRecipe(index, (int)JewelryRecipes.VampiricShard);
            SetData(index, "vamp_shard");
            AddCreateItem(index, (from, craftItem, tool) =>
            {
                double tink = from.Skills[SkillName.Tinkering].Value;
                double necro = from.Skills[SkillName.Necromancy].Value;
                double avg = (tink + necro) / 2.0;
                double quality = avg <= 50.0 ? 0.5 : 0.5 + ((avg - 50.0) * 0.05);
                return new JewelryGem("vamp_shard", quality, 0x1EA7, 1175);
            });
            // Огненный Рубин Истребителя
            index = AddCraft(typeof(JewelryGem), "Мистическая огранка", "Огненный Рубин Истребителя (+20% Weapon Damage / +10% HCI)", 110.0, 125.0, typeof(FireRuby), "Редкий огненный рубин", 3, "Требуется редкий огненный рубин.");
            AddRes(index, typeof(ValoriteIngot), "Слитки валорита", 10, "Нужна оправа из валорита.");
            AddRes(index, typeof(DaemonBlood), "Кровь демона", 10, "Нужна кровь демона для закалки.");
            AddRecipe(index, (int)JewelryRecipes.FireRubySlayer);
            SetData(index, "firerubyslayer");
            AddCreateItem(index, (from, craftItem, tool) =>
            {
                double tink = from.Skills[SkillName.Tinkering].Value;
                double tac = from.Skills[SkillName.Tactics].Value;
                double avg = (tink + tac) / 2.0;
                double quality = avg <= 50.0 ? 0.5 : 0.5 + ((avg - 50.0) * 0.05);
                return new JewelryGem("firerubyslayer", quality, 0x1EA7, 1359);
            });
            // =========================================================================
            // 🛡️ КАМНИ СТОЙКОСТИ (100% СОВПАДЕНИЕ С ВАНИЛЬНЫМИ КАМНЯМИ РЕЗИСТОВ)
            // =========================================================================
            AddResistCapGem("physical_resist", "Жемчужина Физической Стойкости", "физический урон",
            typeof(WhitePearl), "Белая жемчужина", 115.0, 125.0, (int)JewelryRecipes.PhysicalResistGem, 1153);
            AddResistCapGem("fire_resist", "Огненный Рубин Стойкости", "огонь",
            typeof(FireRuby), "Основа из огненного рубина", 115.0, 125.0, (int)JewelryRecipes.FireResistGem, 1175);
            AddResistCapGem("cold_resist", "Сапфир Ледяной Стойкости", "холод",
            typeof(Sapphire), "Неограненный сапфир", 110.0, 125.0, (int)JewelryRecipes.ColdResistGem, 1144);
            AddResistCapGem("poison_resist", "Изумруд Ядовитой Стойкости", "яд",
            typeof(Emerald), "Неограненный изумруд", 110.0, 125.0, (int)JewelryRecipes.PoisonResistGem, 1168);
            AddResistCapGem("energy_resist", "Бирюза Энергетической Стойкости", "энергию",
            typeof(Turquoise), "Драгоценная бирюза", 115.0, 125.0, (int)JewelryRecipes.EnergyResistGem, 1315);
            // =========================================================================
            // 💍 КАТЕГОРИЯ 4: СОЗДАНИЕ ВЫСШИХ УКРАШЕНИЙ (С ИСПРАВЛЕННЫМ СПИСАНИЕМ)
            // =========================================================================
            // 1. Амулет Жизни
            index = AddCraft(typeof(GoldNecklace), "Кастомные украшения", "Амулет Живительного Сияния (Эпический Амулет)", 95.0, 120.0, typeof(GoldIngot), "Золотые слитки", 15, "Вам не хватает золота.");
            AddRes(index, typeof(JewelryGem), "Совершенный Изумруд Регенерации", 1, "Требуется Совершенный Изумруд Регенерации!");
            AddRecipe(index, (int)JewelryRecipes.AmuletOfLife);
            AddCreateItem(index, (from, craftItem, tool) =>
            {
                if (!PrepareSpecificGemForConsume(from, "perfectemerald", "Совершенный Изумруд Регенерации", typeof(GoldIngot), 15, out double q))
                    return null;
                GoldNecklace necklace = new GoldNecklace();
                necklace.Name = "Амулет Живительного Сияния [Ювелирный Артефакт]";
                necklace.Hue = 1150;
                necklace.Attributes.BonusHits = (int)Math.Round(15.0 * q);
                necklace.Attributes.RegenHits = (int)Math.Round(3.0 * q);
                return necklace;
            });
            // 2. Кольцо Астральной Силы
            index = AddCraft(typeof(GoldRing), "Кастомные украшения", "Кольцо Астральной Силы (Эпическое Кольцо)", 95.0, 120.0, typeof(GoldIngot), "Золотые слитки", 12, "Вам не хватает золота.");
            AddRes(index, typeof(JewelryGem), "Тёмный Сапфир Потока", 1, "Требуется Тёмный Сапфир Потока!");
            AddRecipe(index, (int)JewelryRecipes.RingOfAstralPower);
            AddCreateItem(index, (from, craftItem, tool) =>
            {
                if (!PrepareSpecificGemForConsume(from, "darksapphire", "Тёмный Сапфир Потока", typeof(GoldIngot), 12, out double q))
                    return null;
                GoldRing ring = new GoldRing();
                ring.Name = "Кольцо Астральной Силы [Ювелирный Артефакт]";
                ring.Hue = 1240;
                ring.Attributes.BonusMana = (int)Math.Round(20.0 * q);
                ring.Attributes.RegenMana = (int)Math.Round(4.0 * q);
                return ring;
            });
            // 3. Браслет Первобытной Бури
            index = AddCraft(typeof(GoldBracelet), "Кастомные украшения", "Браслет Первобытной Бури (Эпический Браслет)", 100.0, 125.0, typeof(VeriteIngot), "Слитки верита", 15, "Вам не хватает верита.");
            AddRes(index, typeof(JewelryGem), "Пронзающая Бирюза Грома", 1, "Требуется Бирюза Грома!");
            AddRecipe(index, (int)JewelryRecipes.BraceletOfStorms);
            AddCreateItem(index, (from, craftItem, tool) =>
            {
                if (!PrepareSpecificGemForConsume(from, "turquoise", "Бирюза Грома", typeof(VeriteIngot), 15, out double q))
                    return null;
                GoldBracelet bracelet = new GoldBracelet();
                bracelet.Name = "Браслет Первобытной Бури [Ювелирный Артефакт]";
                bracelet.Hue = 1315;
                bracelet.Attributes.WeaponDamage = (int)Math.Round(20.0 * q);
                bracelet.Attributes.SpellDamage = (int)Math.Round(15.0 * q);
                return bracelet;
            });
            // 4. Серьги Кровавого Графа
            index = AddCraft(typeof(GoldEarrings), "Кастомные украшения", "Серьги Кровавого Графа (Эпические Серьги)", 105.0, 125.0, typeof(GoldIngot), "Золотые слитки", 10, "Вам не хватает золота.");
            AddRes(index, typeof(JewelryGem), "Мистический Осколок Вампиризма", 1, "Требуется Осколок Вампиризма!");
            AddRecipe(index, (int)JewelryRecipes.EarringsOfVampire);
            AddCreateItem(index, (from, craftItem, tool) =>
            {
                if (!PrepareSpecificGemForConsume(from, "vamp_shard", "Осколок Вампиризма", typeof(GoldIngot), 10, out double q))
                    return null;
                GoldEarrings earrings = new GoldEarrings();
                earrings.Name = "Серьги Кровавого Графа [Ювелирный Артефакт]";
                earrings.Hue = 1175;
                earrings.Attributes.RegenHits = (int)Math.Round(5.0 * q);
                earrings.Attributes.AttackChance = (int)Math.Round(15.0 * q);
                return earrings;
            });
            // 5. Талисман Дикого Знахаря
            index = AddCraft(typeof(BaseTalisman), "Кастомные украшения", "Талисман Дикого Знахаря (Эпический Талисман)", 105.0, 125.0, typeof(IronIngot), "Железные слитки", 5, "Вам не хватает железа.");
            AddRes(index, typeof(JewelryGem), "Экру-Цитрин Знахаря", 1, "Требуется Экру-Цитрин Знахаря!");
            AddRecipe(index, (int)JewelryRecipes.TalismanOfFortune);
            AddCreateItem(index, (from, craftItem, tool) =>
            {
                if (!PrepareSpecificGemForConsume(from, "ecrucitrine", "Экру-Цитрин Знахаря", typeof(IronIngot), 5, out double q))
                    return null;
                BaseTalisman talisman = new BaseTalisman(0x2F5B);
                talisman.Name = "Талисман Дикого Знахаря [Ювелирный Артефакт]";
                talisman.Hue = 1166;
                talisman.Attributes.EnhancePotions = (int)Math.Round(50.0 * q);
                talisman.Attributes.Luck = (int)Math.Round(300.0 * q);
                return talisman;
            });
            // =========================================================================
            // 💍 БАЗОВЫЕ УКРАШЕНИЯ С ИНКРУСТАЦИЕЙ (ОТКРЫТЫ ИЗНАЧАЛЬНО)
            // =========================================================================
            // 1. Кольцо Точности (Требует Изумруд Точности)
            index = AddCraft(typeof(GoldRing), "Базовая огранка", "Кольцо Точности (Начальное украшение)", 65.0, 95.0, typeof(IronIngot), "Железные слитки", 4, "Вам не хватает железа.");
            AddRes(index, typeof(JewelryGem), "Ограненный Изумруд Точности", 1, "Требуется ограненный Изумруд Точности!");
            AddCreateItem(index, (from, craftItem, tool) =>
            {
                if (!PrepareSpecificGemForConsume(from, "emerald", "Изумруд Точности", typeof(IronIngot), 4, out double q))
                    return null;
                GoldRing ring = new GoldRing();
                ring.Name = "Кольцо Точности [Ремесленное украшение]";
                ring.Hue = 1168;
                ring.Attributes.AttackChance = (int)Math.Max(1, Math.Round(4.0 * q));
                return ring;
            });
            // 2. Браслет Защиты (Требует Алмаз Защиты)
            index = AddCraft(typeof(GoldBracelet), "Базовая огранка", "Браслет Защиты (Начальное украшение)", 65.0, 95.0, typeof(IronIngot), "Железные слитки", 4, "Вам не хватает железа.");
            AddRes(index, typeof(JewelryGem), "Ограненный Алмаз Защиты", 1, "Требуется ограненный Алмаз Защиты!");
            AddCreateItem(index, (from, craftItem, tool) =>
            {
                if (!PrepareSpecificGemForConsume(from, "diamond", "Алмаз Защиты", typeof(IronIngot), 4, out double q))
                    return null;
                GoldBracelet bracelet = new GoldBracelet();
                bracelet.Name = "Браслет Защиты [Ремесленное украшение]";
                bracelet.Hue = 1150;
                bracelet.Attributes.DefendChance = (int)Math.Max(1, Math.Round(4.0 * q));
                return bracelet;
            });
            // 3. Серьги Выносливости (Требует Цитрин Выносливости)
            index = AddCraft(typeof(GoldEarrings), "Базовая огранка", "Серьги Выносливости (Начальное украшение)", 70.0, 100.0, typeof(IronIngot), "Железные слитки", 3, "Вам не хватает железа.");
            AddRes(index, typeof(JewelryGem), "Ограненный Цитрин Выносливости", 1, "Требуется ограненный Цитрин Выносливости!");
            AddCreateItem(index, (from, craftItem, tool) =>
            {
                if (!PrepareSpecificGemForConsume(from, "citrine", "Цитрин Выносливости", typeof(IronIngot), 3, out double q))
                    return null;
                GoldEarrings earrings = new GoldEarrings();
                earrings.Name = "Серьги Выносливости [Ремесленное украшение]";
                earrings.Hue = 1166;
                earrings.Attributes.RegenStam = (int)Math.Max(1, Math.Round(2.0 * q));
                earrings.Attributes.Luck = (int)Math.Max(10, Math.Round(25.0 * q));
                return earrings;
            });
            // 4. Амулет Разума (Требует Сапфир Разума — ванильный аналог)
            index = AddCraft(typeof(GoldNecklace), "Базовая огранка", "Амулет Разума (Начальное украшение)", 70.0, 100.0, typeof(IronIngot), "Железные слитки", 5, "Вам не хватает железа.");
            AddRes(index, typeof(JewelryGem), "Ограненный Сапфир Разума", 1, "Требуется ограненный Сапфир Разума!");
            AddCreateItem(index, (from, craftItem, tool) =>
            {
                if (!PrepareSpecificGemForConsume(from, "sapphire", "Сапфир Разума", typeof(IronIngot), 5, out double q))
                    return null;
                GoldNecklace necklace = new GoldNecklace();
                necklace.Name = "Амулет Разума [Ремесленное украшение]";
                necklace.Hue = 1144;
                necklace.Attributes.BonusMana = (int)Math.Max(2, Math.Round(6.0 * q));
                return necklace;
            });
        }
        private void AddResistCapGem(string effect, string name, string resistName, Type rawGemType, string rawGemName,
        double minSkill, double maxSkill, int recipe, int hue)
        {
            int index = AddCraft(typeof(JewelryGem), "Камни стойкости", $"{name} (резист: {resistName})",
            minSkill, maxSkill, rawGemType, rawGemName, 5, $"Вам не хватает камней для огранки «{name}».");
            AddRes(index, typeof(GoldIngot), "Золотые слитки для оправы", 10, "Нужно золото для оправы.");
            AddRecipe(index, recipe);
            SetData(index, effect);
            AddCreateItem(index, (from, craftItem, tool) =>
            {
                double tinkering = from.Skills[SkillName.Tinkering].Value;
                double quality = tinkering <= 50.0 ? 0.5 : 0.5 + ((tinkering - 50.0) * 0.05);
                return new JewelryGem(effect, quality, 0x1EA7, hue);
            });
        }
        private static bool PrepareSpecificGemForConsume(Mobile from, string requiredEffect, string displayGemName, Type ingotType, int ingotAmount, out double qualityMultiplier)
        {
            qualityMultiplier = 1.0;

            if (from?.Backpack == null)
                return false;

            Item[] allGems = from.Backpack.FindItemsByType(typeof(JewelryGem), true);
            JewelryGem targetGem = null;

            foreach (Item item in allGems)
            {
                if (item is JewelryGem gem && gem.GemEffect == requiredEffect)
                {
                    targetGem = gem;
                    break;
                }
            }

            if (targetGem == null)
            {
                from.SendMessage(0x22, $"В рюкзаке не найден ограненный камень: [{displayGemName}]! Ресурсы возвращены.");

                if (ingotType != null && ingotAmount > 0 && Activator.CreateInstance(ingotType, ingotAmount) is Item ingots)
                    from.AddToBackpack(ingots);

                if (allGems.Length > 0 && allGems[0] is JewelryGem firstGem)
                {
                    from.AddToBackpack(new JewelryGem(firstGem.GemEffect, firstGem.GemQuality, firstGem.ItemID, firstGem.Hue));
                }

                return false;
            }

            qualityMultiplier = targetGem.GemQuality;

            Item firstItem = from.Backpack.FindItemByType(typeof(JewelryGem), true);

            if (firstItem is JewelryGem firstToBeDeleted && firstToBeDeleted != targetGem)
            {
                targetGem.GemEffect = firstToBeDeleted.GemEffect;
                targetGem.GemQuality = firstToBeDeleted.GemQuality;
                targetGem.Hue = firstToBeDeleted.Hue;
                targetGem.Name = firstToBeDeleted.Name;
                targetGem.InvalidateProperties();
            }

            return true;
        }
    }
}