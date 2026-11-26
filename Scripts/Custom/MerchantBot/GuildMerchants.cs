using System;
using System.Collections;
using Server.Items;

namespace Server.Mobiles
{
    // ================= 1. ПОРТНОЙ (TailorsGuild) =================
    public abstract class BaseTailorMerchant : BaseMerchantBot
    {
        public override string ProfessionName => "Tailor";
        public override NpcGuild RequiredNpcGuild => Tier >= 3 ? NpcGuild.TailorsGuild : NpcGuild.None;
        public override int RequiredGuildRank => Tier == 1 ? 11 : (Tier == 2 ? 8 : (Tier == 3 ? 5 : 2));

        private static readonly Type[] LeatherTypes = { typeof(LeatherChest), typeof(LeatherLegs), typeof(LeatherArms), typeof(LeatherGloves), typeof(LeatherGorget), typeof(StuddedChest) };

        protected override void DressUp() { AddItem(new FancyShirt(1284)); AddItem(new LongPants(1150)); AddItem(new Sandals(1175)); }
        public override bool IsItemForProfession(Item item) => item is BaseClothing || (item is BaseArmor ba && (ba.MaterialType == ArmorMaterialType.Leather || ba.MaterialType == ArmorMaterialType.Studded));

        protected override void GenerateCraftedStock(Hashtable sellItems)
        {
            Item loot = Utility.RandomBool() ? Loot.RandomClothing() : Loot.Construct(LeatherTypes);
            if (loot is BaseClothing) loot.Hue = Utility.RandomDyedHue();
            ApplyTierMagicAttributes(loot);
            AddItemToSale(sellItems, loot, Utility.RandomMinMax(4000, 12000) * GetTierPriceMultiplier(), $"[Портной T{Tier}]");
        }

        public BaseTailorMerchant() : base() { }
        public BaseTailorMerchant(Serial s) : base(s) { }
        public override void Serialize(GenericWriter w) { base.Serialize(w); w.Write(0); }
        public override void Deserialize(GenericReader r) { base.Deserialize(r); r.ReadInt(); }
    }
    public class TailorMerchantT1 : BaseTailorMerchant { public override int Tier => 1; [Constructable] public TailorMerchantT1() {} public TailorMerchantT1(Serial s) : base(s) {} public override void Serialize(GenericWriter w) { base.Serialize(w); w.Write(0); } public override void Deserialize(GenericReader r) { base.Deserialize(r); r.ReadInt(); } }
    public class TailorMerchantT2 : BaseTailorMerchant { public override int Tier => 2; [Constructable] public TailorMerchantT2() {} public TailorMerchantT2(Serial s) : base(s) {} public override void Serialize(GenericWriter w) { base.Serialize(w); w.Write(0); } public override void Deserialize(GenericReader r) { base.Deserialize(r); r.ReadInt(); } }
    public class TailorMerchantT3 : BaseTailorMerchant { public override int Tier => 3; [Constructable] public TailorMerchantT3() {} public TailorMerchantT3(Serial s) : base(s) {} public override void Serialize(GenericWriter w) { base.Serialize(w); w.Write(0); } public override void Deserialize(GenericReader r) { base.Deserialize(r); r.ReadInt(); } }
    public class TailorMerchantT4 : BaseTailorMerchant { public override int Tier => 4; [Constructable] public TailorMerchantT4() {} public TailorMerchantT4(Serial s) : base(s) {} public override void Serialize(GenericWriter w) { base.Serialize(w); w.Write(0); } public override void Deserialize(GenericReader r) { base.Deserialize(r); r.ReadInt(); } }

    // ================= 2. ПЛОТНИК (Carpenters / Tinkers) =================
    public abstract class BaseCarpenterMerchant : BaseMerchantBot
    {
        public override string ProfessionName => "Carpenter";
        public override NpcGuild RequiredNpcGuild => Tier >= 3 ? NpcGuild.TinkersGuild : NpcGuild.None;
        public override int RequiredGuildRank => Tier == 1 ? 11 : (Tier == 2 ? 8 : (Tier == 3 ? 5 : 2));

        private static readonly Type[] StockTypes = { typeof(WoodenChest), typeof(MetalGoldenChest), typeof(Armoire), typeof(WoodenBookcase), typeof(QuarterStaff), typeof(GnarledStaff), typeof(Bow), typeof(Crossbow) };

        protected override void DressUp() { AddItem(new HalfApron(1141)); AddItem(new FancyShirt(1153)); AddItem(new ShortPants(1157)); AddItem(new Shoes(1175)); }
        public override bool IsItemForProfession(Item item) => item is BaseContainer || item is BaseStaff || Array.IndexOf(StockTypes, item.GetType()) >= 0;

        protected override void GenerateCraftedStock(Hashtable sellItems)
        {
            Item loot = Loot.Construct(StockTypes);
            if (loot is LockableContainer lc) { lc.Locked = false; lc.MaxLockLevel = 100; lc.LockLevel = 0; }
            ApplyTierMagicAttributes(loot);
            AddItemToSale(sellItems, loot, Utility.RandomMinMax(4000, 12000) * GetTierPriceMultiplier(), $"[Плотник T{Tier}]");
        }

        public BaseCarpenterMerchant() : base() { }
        public BaseCarpenterMerchant(Serial s) : base(s) { }
        public override void Serialize(GenericWriter w) { base.Serialize(w); w.Write(0); }
        public override void Deserialize(GenericReader r) { base.Deserialize(r); r.ReadInt(); }
    }
    public class CarpenterMerchantT1 : BaseCarpenterMerchant { public override int Tier => 1; [Constructable] public CarpenterMerchantT1() {} public CarpenterMerchantT1(Serial s) : base(s) {} public override void Serialize(GenericWriter w) { base.Serialize(w); w.Write(0); } public override void Deserialize(GenericReader r) { base.Deserialize(r); r.ReadInt(); } }
    public class CarpenterMerchantT2 : BaseCarpenterMerchant { public override int Tier => 2; [Constructable] public CarpenterMerchantT2() {} public CarpenterMerchantT2(Serial s) : base(s) {} public override void Serialize(GenericWriter w) { base.Serialize(w); w.Write(0); } public override void Deserialize(GenericReader r) { base.Deserialize(r); r.ReadInt(); } }
    public class CarpenterMerchantT3 : BaseCarpenterMerchant { public override int Tier => 3; [Constructable] public CarpenterMerchantT3() {} public CarpenterMerchantT3(Serial s) : base(s) {} public override void Serialize(GenericWriter w) { base.Serialize(w); w.Write(0); } public override void Deserialize(GenericReader r) { base.Deserialize(r); r.ReadInt(); } }
    public class CarpenterMerchantT4 : BaseCarpenterMerchant { public override int Tier => 4; [Constructable] public CarpenterMerchantT4() {} public CarpenterMerchantT4(Serial s) : base(s) {} public override void Serialize(GenericWriter w) { base.Serialize(w); w.Write(0); } public override void Deserialize(GenericReader r) { base.Deserialize(r); r.ReadInt(); } }

    // ================= 3. МАГ (MagesGuild) =================
    public abstract class BaseMageMerchant : BaseMerchantBot
    {
        public override string ProfessionName => "Mage";
        public override NpcGuild RequiredNpcGuild => Tier >= 3 ? NpcGuild.MagesGuild : NpcGuild.None;
        public override int RequiredGuildRank => Tier == 1 ? 11 : (Tier == 2 ? 8 : (Tier == 3 ? 5 : 2));

        private static readonly Type[] MageTypes = { typeof(BlackStaff), typeof(GnarledStaff), typeof(QuarterStaff), typeof(WizardsHat), typeof(Robe), typeof(Spellbook) };

        protected override void DressUp() { AddItem(new Robe(1154)); AddItem(new WizardsHat(1154)); AddItem(new Sandals(1175)); }
        public override bool IsItemForProfession(Item item) => item is BaseStaff || item is Spellbook || item is SpellScroll || item is BaseReagent;

        protected override void GenerateCraftedStock(Hashtable sellItems)
        {
            Item loot = Loot.Construct(MageTypes);
            ApplyTierMagicAttributes(loot);
            AddItemToSale(sellItems, loot, Utility.RandomMinMax(6000, 18000) * GetTierPriceMultiplier(), $"[Гильдия Магов T{Tier}]");
        }

        public BaseMageMerchant() : base() { }
        public BaseMageMerchant(Serial s) : base(s) { }
        public override void Serialize(GenericWriter w) { base.Serialize(w); w.Write(0); }
        public override void Deserialize(GenericReader r) { base.Deserialize(r); r.ReadInt(); }
    }
    public class MageMerchantT1 : BaseMageMerchant { public override int Tier => 1; [Constructable] public MageMerchantT1() {} public MageMerchantT1(Serial s) : base(s) {} public override void Serialize(GenericWriter w) { base.Serialize(w); w.Write(0); } public override void Deserialize(GenericReader r) { base.Deserialize(r); r.ReadInt(); } }
    public class MageMerchantT2 : BaseMageMerchant { public override int Tier => 2; [Constructable] public MageMerchantT2() {} public MageMerchantT2(Serial s) : base(s) {} public override void Serialize(GenericWriter w) { base.Serialize(w); w.Write(0); } public override void Deserialize(GenericReader r) { base.Deserialize(r); r.ReadInt(); } }
    public class MageMerchantT3 : BaseMageMerchant { public override int Tier => 3; [Constructable] public MageMerchantT3() {} public MageMerchantT3(Serial s) : base(s) {} public override void Serialize(GenericWriter w) { base.Serialize(w); w.Write(0); } public override void Deserialize(GenericReader r) { base.Deserialize(r); r.ReadInt(); } }
    public class MageMerchantT4 : BaseMageMerchant { public override int Tier => 4; [Constructable] public MageMerchantT4() {} public MageMerchantT4(Serial s) : base(s) {} public override void Serialize(GenericWriter w) { base.Serialize(w); w.Write(0); } public override void Deserialize(GenericReader r) { base.Deserialize(r); r.ReadInt(); } }

    // ================= 4. ВОИН (WarriorsGuild) =================
    public abstract class BaseWarriorMerchant : BaseMerchantBot
    {
        public override string ProfessionName => "Warrior";
        public override NpcGuild RequiredNpcGuild => Tier >= 3 ? NpcGuild.WarriorsGuild : NpcGuild.None;
        public override int RequiredGuildRank => Tier == 1 ? 11 : (Tier == 2 ? 8 : (Tier == 3 ? 5 : 2));

        protected override void DressUp() { AddItem(new PlateChest()); AddItem(new PlateLegs()); AddItem(new Cloak(1157)); AddItem(new Boots(1175)); }
        
        public override bool IsItemForProfession(Item item) => item is BaseShield || item is BasePoleArm || item is BaseAxe || item is BaseSword;
		protected override void GenerateCraftedStock(Hashtable sellItems)
		{
		Item loot = Utility.RandomBool() ? (Item)Loot.RandomShield() : Loot.RandomWeapon();
		ApplyTierMagicAttributes(loot);
		AddItemToSale(sellItems, loot, Utility.RandomMinMax(6000, 16000) * GetTierPriceMultiplier(), $"[Гильдия Воинов T{Tier}]");
		}
	public BaseWarriorMerchant() : base() { }
	public BaseWarriorMerchant(Serial s) : base(s) { }
	public override void Serialize(GenericWriter w) { base.Serialize(w); w.Write(0); }
	public override void Deserialize(GenericReader r) { base.Deserialize(r); r.ReadInt(); }
	}
	public class WarriorMerchantT1 : BaseWarriorMerchant { public override int Tier => 1; [Constructable] public WarriorMerchantT1() {} public WarriorMerchantT1(Serial s) : base(s) {} public override void Serialize(GenericWriter w) { base.Serialize(w); w.Write(0); } public override void Deserialize(GenericReader r) { base.Deserialize(r); r.ReadInt(); } }
	public class WarriorMerchantT2 : BaseWarriorMerchant { public override int Tier => 2; [Constructable] public WarriorMerchantT2() {} public WarriorMerchantT2(Serial s) : base(s) {} public override void Serialize(GenericWriter w) { base.Serialize(w); w.Write(0); } public override void Deserialize(GenericReader r) { base.Deserialize(r); r.ReadInt(); } }
	public class WarriorMerchantT3 : BaseWarriorMerchant { public override int Tier => 3; [Constructable] public WarriorMerchantT3() {} public WarriorMerchantT3(Serial s) : base(s) {} public override void Serialize(GenericWriter w) { base.Serialize(w); w.Write(0); } public override void Deserialize(GenericReader r) { base.Deserialize(r); r.ReadInt(); } }
	public class WarriorMerchantT4 : BaseWarriorMerchant { public override int Tier => 4; [Constructable] public WarriorMerchantT4() {} public WarriorMerchantT4(Serial s) : base(s) {} public override void Serialize(GenericWriter w) { base.Serialize(w); w.Write(0); } public override void Deserialize(GenericReader r) { base.Deserialize(r); r.ReadInt(); } }
// ================= 5. РЕЙНДЖЕР / ЛУЧНИК (RangersGuild) =================
public abstract class BaseRangerMerchant : BaseMerchantBot
{
public override string ProfessionName => "Ranger";
public override NpcGuild RequiredNpcGuild => Tier >= 3 ? NpcGuild.RangersGuild : NpcGuild.None;
public override int RequiredGuildRank => Tier == 1 ? 11 : (Tier == 2 ? 8 : (Tier == 3 ? 5 : 2));
protected override void DressUp() { AddItem(new StuddedChest()); AddItem(new StuddedLegs()); AddItem(new Cloak(1436)); AddItem(new Boots(1175)); }
public override bool IsItemForProfession(Item item) => item is BaseRanged || item is BaseQuiver || item is Arrow || item is Bolt;
protected override void GenerateCraftedStock(Hashtable sellItems)
{
Item loot = Loot.RandomRangedWeapon();
ApplyTierMagicAttributes(loot);
AddItemToSale(sellItems, loot, Utility.RandomMinMax(5500, 15000) * GetTierPriceMultiplier(), $"[Гильдия Рейнджеров T{Tier}]");
}

public BaseRangerMerchant() : base() { }
public BaseRangerMerchant(Serial s) : base(s) { }
public override void Serialize(GenericWriter w) { base.Serialize(w); w.Write(0); }
public override void Deserialize(GenericReader r) { base.Deserialize(r); r.ReadInt(); }
}
public class RangerMerchantT1 : BaseRangerMerchant { public override int Tier => 1; [Constructable] public RangerMerchantT1() {} public RangerMerchantT1(Serial s) : base(s) {} public override void Serialize(GenericWriter w) { base.Serialize(w); w.Write(0); } public override void Deserialize(GenericReader r) { base.Deserialize(r); r.ReadInt(); } }
public class RangerMerchantT2 : BaseRangerMerchant { public override int Tier => 2; [Constructable] public RangerMerchantT2() {} public RangerMerchantT2(Serial s) : base(s) {} public override void Serialize(GenericWriter w) { base.Serialize(w); w.Write(0); } public override void Deserialize(GenericReader r) { base.Deserialize(r); r.ReadInt(); } }
public class RangerMerchantT3 : BaseRangerMerchant { public override int Tier => 3; [Constructable] public RangerMerchantT3() {} public RangerMerchantT3(Serial s) : base(s) {} public override void Serialize(GenericWriter w) { base.Serialize(w); w.Write(0); } public override void Deserialize(GenericReader r) { base.Deserialize(r); r.ReadInt(); } }
public class RangerMerchantT4 : BaseRangerMerchant { public override int Tier => 4; [Constructable] public RangerMerchantT4() {} public RangerMerchantT4(Serial s) : base(s) {} public override void Serialize(GenericWriter w) { base.Serialize(w); w.Write(0); } public override void Deserialize(GenericReader r) { base.Deserialize(r); r.ReadInt(); } }
// ================= 6. ВОР (ThievesGuild) =================
public abstract class BaseThiefMerchant : BaseMerchantBot
{
public override string ProfessionName => "Thief";
public override NpcGuild RequiredNpcGuild => Tier >= 3 ? NpcGuild.ThievesGuild : NpcGuild.None;
public override int RequiredGuildRank => Tier == 1 ? 11 : (Tier == 2 ? 8 : (Tier == 3 ? 5 : 2));
private static readonly Type[] ThiefTypes = { typeof(Dagger), typeof(Kryss), typeof(Cloak), typeof(Bandana), typeof(LeatherGloves) };
protected override void DressUp() { AddItem(new HoodedShroudOfShadows(1109)); AddItem(new Boots(1109)); }
public override bool IsItemForProfession(Item item) => item is BaseKnife || item is Lockpick || item is SmokeBomb;
protected override void GenerateCraftedStock(Hashtable sellItems)
{
Item loot = Loot.Construct(ThiefTypes);
ApplyTierMagicAttributes(loot);
AddItemToSale(sellItems, loot, Utility.RandomMinMax(5000, 14000) * GetTierPriceMultiplier(), $"[Гильдия Воров T{Tier}]");
}
public BaseThiefMerchant() : base() { }
public BaseThiefMerchant(Serial s) : base(s) { }
public override void Serialize(GenericWriter w) { base.Serialize(w); w.Write(0); }
public override void Deserialize(GenericReader r) { base.Deserialize(r); r.ReadInt(); }
}
public class ThiefMerchantT1 : BaseThiefMerchant { public override int Tier => 1; [Constructable] public ThiefMerchantT1() {} public ThiefMerchantT1(Serial s) : base(s) {} public override void Serialize(GenericWriter w) { base.Serialize(w); w.Write(0); } public override void Deserialize(GenericReader r) { base.Deserialize(r); r.ReadInt(); } }
public class ThiefMerchantT2 : BaseThiefMerchant { public override int Tier => 2; [Constructable] public ThiefMerchantT2() {} public ThiefMerchantT2(Serial s) : base(s) {} public override void Serialize(GenericWriter w) { base.Serialize(w); w.Write(0); } public override void Deserialize(GenericReader r) { base.Deserialize(r); r.ReadInt(); } }
public class ThiefMerchantT3 : BaseThiefMerchant { public override int Tier => 3; [Constructable] public ThiefMerchantT3() {} public ThiefMerchantT3(Serial s) : base(s) {} public override void Serialize(GenericWriter w) { base.Serialize(w); w.Write(0); } public override void Deserialize(GenericReader r) { base.Deserialize(r); r.ReadInt(); } }
public class ThiefMerchantT4 : BaseThiefMerchant { public override int Tier => 4; [Constructable] public ThiefMerchantT4() {} public ThiefMerchantT4(Serial s) : base(s) {} public override void Serialize(GenericWriter w) { base.Serialize(w); w.Write(0); } public override void Deserialize(GenericReader r) { base.Deserialize(r); r.ReadInt(); } }
// ================= 7. МЕХАНИК / ЮВЕЛИР (TinkersGuild) =================
public abstract class BaseTinkerMerchant : BaseMerchantBot
{
public override string ProfessionName => "Tinker";
public override NpcGuild RequiredNpcGuild => Tier >= 3 ? NpcGuild.TinkersGuild : NpcGuild.None;
public override int RequiredGuildRank => Tier == 1 ? 11 : (Tier == 2 ? 8 : (Tier == 3 ? 5 : 2));
protected override void DressUp() { AddItem(new HalfApron(1150)); AddItem(new FancyShirt(1165)); AddItem(new LongPants(1109)); AddItem(new Shoes(1175)); }
public override bool IsItemForProfession(Item item) => item is BaseJewel || item is BaseTool;
protected override void GenerateCraftedStock(Hashtable sellItems)
{
Item loot = Loot.RandomJewelry();
ApplyTierMagicAttributes(loot);
AddItemToSale(sellItems, loot, Utility.RandomMinMax(7000, 20000) * GetTierPriceMultiplier(), $"[Ювелир T{Tier}]");
}

public BaseTinkerMerchant() : base() { }
public BaseTinkerMerchant(Serial s) : base(s) { }
public override void Serialize(GenericWriter w) { base.Serialize(w); w.Write(0); }
public override void Deserialize(GenericReader r) { base.Deserialize(r); r.ReadInt(); }
}
public class TinkerMerchantT1 : BaseTinkerMerchant { public override int Tier => 1; [Constructable] public TinkerMerchantT1() {} public TinkerMerchantT1(Serial s) : base(s) {} public override void Serialize(GenericWriter w) { base.Serialize(w); w.Write(0); } public override void Deserialize(GenericReader r) { base.Deserialize(r); r.ReadInt(); } }
public class TinkerMerchantT2 : BaseTinkerMerchant { public override int Tier => 2; [Constructable] public TinkerMerchantT2() {} public TinkerMerchantT2(Serial s) : base(s) {} public override void Serialize(GenericWriter w) { base.Serialize(w); w.Write(0); } public override void Deserialize(GenericReader r) { base.Deserialize(r); r.ReadInt(); } }
public class TinkerMerchantT3 : BaseTinkerMerchant { public override int Tier => 3; [Constructable] public TinkerMerchantT3() {} public TinkerMerchantT3(Serial s) : base(s) {} public override void Serialize(GenericWriter w) { base.Serialize(w); w.Write(0); } public override void Deserialize(GenericReader r) { base.Deserialize(r); r.ReadInt(); } }
public class TinkerMerchantT4 : BaseTinkerMerchant { public override int Tier => 4; [Constructable] public TinkerMerchantT4() {} public TinkerMerchantT4(Serial s) : base(s) {} public override void Serialize(GenericWriter w) { base.Serialize(w); w.Write(0); } public override void Deserialize(GenericReader r) { base.Deserialize(r); r.ReadInt(); } }
// ================= 8. ЛЕКАРЬ / АЛХИМИК (HealersGuild) =================
public abstract class BaseHealerMerchant : BaseMerchantBot
{
public override string ProfessionName => "Healer";
public override NpcGuild RequiredNpcGuild => Tier >= 3 ? NpcGuild.HealersGuild : NpcGuild.None;
public override int RequiredGuildRank => Tier == 1 ? 11 : (Tier == 2 ? 8 : (Tier == 3 ? 5 : 2));
private static readonly Type[] HealerTypes = { typeof(GreaterHealPotion), typeof(GreaterCurePotion), typeof(TotalRefreshPotion), typeof(Bandage) };
protected override void DressUp() { AddItem(new Robe(1150)); AddItem(new Sandals(1150)); }
public override bool IsItemForProfession(Item item) => item is BasePotion || item is Bandage;
protected override void GenerateCraftedStock(Hashtable sellItems)
{
Item loot = Loot.Construct(HealerTypes);
if (loot.Stackable) loot.Amount = 10 * Tier;
AddItemToSale(sellItems, loot, Utility.RandomMinMax(2000, 6000) * GetTierPriceMultiplier(), $"[Алхимик T{Tier}]");
}

public BaseHealerMerchant() : base() { }
public BaseHealerMerchant(Serial s) : base(s) { }
public override void Serialize(GenericWriter w) { base.Serialize(w); w.Write(0); }
public override void Deserialize(GenericReader r) { base.Deserialize(r); r.ReadInt(); }
}
public class HealerMerchantT1 : BaseHealerMerchant { public override int Tier => 1; [Constructable] public HealerMerchantT1() {} public HealerMerchantT1(Serial s) : base(s) {} public override void Serialize(GenericWriter w) { base.Serialize(w); w.Write(0); } public override void Deserialize(GenericReader r) { base.Deserialize(r); r.ReadInt(); } }
public class HealerMerchantT2 : BaseHealerMerchant { public override int Tier => 2; [Constructable] public HealerMerchantT2() {} public HealerMerchantT2(Serial s) : base(s) {} public override void Serialize(GenericWriter w) { base.Serialize(w); w.Write(0); } public override void Deserialize(GenericReader r) { base.Deserialize(r); r.ReadInt(); } }
public class HealerMerchantT3 : BaseHealerMerchant { public override int Tier => 3; [Constructable] public HealerMerchantT3() {} public HealerMerchantT3(Serial s) : base(s) {} public override void Serialize(GenericWriter w) { base.Serialize(w); w.Write(0); } public override void Deserialize(GenericReader r) { base.Deserialize(r); r.ReadInt(); } }
public class HealerMerchantT4 : BaseHealerMerchant { public override int Tier => 4; [Constructable] public HealerMerchantT4() {} public HealerMerchantT4(Serial s) : base(s) {} public override void Serialize(GenericWriter w) { base.Serialize(w); w.Write(0); } public override void Deserialize(GenericReader r) { base.Deserialize(r); r.ReadInt(); } }
// ================= 9. БАРД (BardsGuild) =================
public abstract class BaseBardMerchant : BaseMerchantBot
{
public override string ProfessionName => "Bard";
public override NpcGuild RequiredNpcGuild => Tier >= 3 ? NpcGuild.BardsGuild : NpcGuild.None;
public override int RequiredGuildRank => Tier == 1 ? 11 : (Tier == 2 ? 8 : (Tier == 3 ? 5 : 2));
protected override void DressUp() { AddItem(new FeatheredHat(1366)); AddItem(new FancyShirt(1366)); AddItem(new Cloak(1366)); AddItem(new Boots(1175)); }
public override bool IsItemForProfession(Item item) => item is BaseInstrument;
protected override void GenerateCraftedStock(Hashtable sellItems)
{
Item loot = Loot.RandomInstrument();
if (loot is BaseInstrument bi)
{
bi.Quality = ItemQuality.Exceptional;
bi.UsesRemaining = 350 + (Tier * 150);
if (Tier >= 2) bi.Slayer = BaseRunicTool.GetRandomSlayer();
}
AddItemToSale(sellItems, loot, Utility.RandomMinMax(4000, 12000) * GetTierPriceMultiplier(), $"[Бард T{Tier}]");
}

public BaseBardMerchant() : base() { }
public BaseBardMerchant(Serial s) : base(s) { }
public override void Serialize(GenericWriter w) { base.Serialize(w); w.Write(0); }
public override void Deserialize(GenericReader r) { base.Deserialize(r); r.ReadInt(); }
}
public class BardMerchantT1 : BaseBardMerchant { public override int Tier => 1; [Constructable] public BardMerchantT1() {} public BardMerchantT1(Serial s) : base(s) {} public override void Serialize(GenericWriter w) { base.Serialize(w); w.Write(0); } public override void Deserialize(GenericReader r) { base.Deserialize(r); r.ReadInt(); } }
public class BardMerchantT2 : BaseBardMerchant { public override int Tier => 2; [Constructable] public BardMerchantT2() {} public BardMerchantT2(Serial s) : base(s) {} public override void Serialize(GenericWriter w) { base.Serialize(w); w.Write(0); } public override void Deserialize(GenericReader r) { base.Deserialize(r); r.ReadInt(); } }
public class BardMerchantT3 : BaseBardMerchant { public override int Tier => 3; [Constructable] public BardMerchantT3() {} public BardMerchantT3(Serial s) : base(s) {} public override void Serialize(GenericWriter w) { base.Serialize(w); w.Write(0); } public override void Deserialize(GenericReader r) { base.Deserialize(r); r.ReadInt(); } }
public class BardMerchantT4 : BaseBardMerchant { public override int Tier => 4; [Constructable] public BardMerchantT4() {} public BardMerchantT4(Serial s) : base(s) {} public override void Serialize(GenericWriter w) { base.Serialize(w); w.Write(0); } public override void Deserialize(GenericReader r) { base.Deserialize(r); r.ReadInt(); } }
// ================= 10. ШАХТЁР (MinersGuild) =================
public abstract class BaseMinerMerchant : BaseMerchantBot
{
public override string ProfessionName => "Miner";
public override NpcGuild RequiredNpcGuild => Tier >= 3 ? NpcGuild.MinersGuild : NpcGuild.None;
public override int RequiredGuildRank => Tier == 1 ? 11 : (Tier == 2 ? 8 : (Tier == 3 ? 5 : 2));
private static readonly Type[] IngotTypes = { typeof(DullCopperIngot), typeof(ShadowIronIngot), typeof(CopperIngot), typeof(BronzeIngot), typeof(GoldIngot), typeof(AgapiteIngot), typeof(VeriteIngot), typeof(ValoriteIngot) };
protected override void DressUp() { AddItem(new HalfApron(1109)); AddItem(new Shirt(1109)); AddItem(new LongPants(1109)); AddItem(new Boots(1109)); }
public override bool IsItemForProfession(Item item) => item is BaseIngot || item is BaseOre || item is Pickaxe;
protected override void GenerateCraftedStock(Hashtable sellItems)
{
int minIdx = (Tier - 1) * 2;
int maxIdx = Math.Min(IngotTypes.Length - 1, Tier * 2);
Item loot = (Item)Activator.CreateInstance(IngotTypes[Utility.RandomMinMax(minIdx, maxIdx)]);
loot.Amount = 25 * Tier;
AddItemToSale(sellItems, loot, Utility.RandomMinMax(3000, 9000) * GetTierPriceMultiplier(), $"[Шахтёр T{Tier}]");
}

public BaseMinerMerchant() : base() { }
public BaseMinerMerchant(Serial s) : base(s) { }
public override void Serialize(GenericWriter w) { base.Serialize(w); w.Write(0); }
public override void Deserialize(GenericReader r) { base.Deserialize(r); r.ReadInt(); }
}
public class MinerMerchantT1 : BaseMinerMerchant { public override int Tier => 1; [Constructable] public MinerMerchantT1() {} public MinerMerchantT1(Serial s) : base(s) {} public override void Serialize(GenericWriter w) { base.Serialize(w); w.Write(0); } public override void Deserialize(GenericReader r) { base.Deserialize(r); r.ReadInt(); } }
public class MinerMerchantT2 : BaseMinerMerchant { public override int Tier => 2; [Constructable] public MinerMerchantT2() {} public MinerMerchantT2(Serial s) : base(s) {} public override void Serialize(GenericWriter w) { base.Serialize(w); w.Write(0); } public override void Deserialize(GenericReader r) { base.Deserialize(r); r.ReadInt(); } }
public class MinerMerchantT3 : BaseMinerMerchant { public override int Tier => 3; [Constructable] public MinerMerchantT3() {} public MinerMerchantT3(Serial s) : base(s) {} public override void Serialize(GenericWriter w) { base.Serialize(w); w.Write(0); } public override void Deserialize(GenericReader r) { base.Deserialize(r); r.ReadInt(); } }
public class MinerMerchantT4 : BaseMinerMerchant { public override int Tier => 4; [Constructable] public MinerMerchantT4() {} public MinerMerchantT4(Serial s) : base(s) {} public override void Serialize(GenericWriter w) { base.Serialize(w); w.Write(0); } public override void Deserialize(GenericReader r) { base.Deserialize(r); r.ReadInt(); } }
// ================= 11. РЫБАК (FishermensGuild) =================
public abstract class BaseFishermanMerchant : BaseMerchantBot
{
public override string ProfessionName => "Fisherman";
public override NpcGuild RequiredNpcGuild => Tier >= 3 ? NpcGuild.FishermensGuild : NpcGuild.None;
public override int RequiredGuildRank => Tier == 1 ? 11 : (Tier == 2 ? 8 : (Tier == 3 ? 5 : 2));
private static readonly Type[] FishTypes = { typeof(RawFishSteak), typeof(SpecialFishingNet), typeof(MessageInABottle), typeof(BigFish) };
protected override void DressUp() { AddItem(new FloppyHat(1365)); AddItem(new Shirt(1365)); AddItem(new ShortPants(1150)); AddItem(new ThighBoots(1175)); }
public override bool IsItemForProfession(Item item) => item is Fish || item is RawFishSteak || item is SpecialFishingNet || item is MessageInABottle || item is FishingPole;
protected override void GenerateCraftedStock(Hashtable sellItems)
{
int idx = Math.Min(FishTypes.Length - 1, Tier - 1);
Item loot = (Item)Activator.CreateInstance(FishTypes[Utility.RandomMinMax(0, idx)]);
if (loot.Stackable) loot.Amount = 20 * Tier;
AddItemToSale(sellItems, loot, Utility.RandomMinMax(2500, 8000) * GetTierPriceMultiplier(), $"[Рыбак T{Tier}]");
}

public BaseFishermanMerchant() : base() { }
public BaseFishermanMerchant(Serial s) : base(s) { }
public override void Serialize(GenericWriter w) { base.Serialize(w); w.Write(0); }
public override void Deserialize(GenericReader r) { base.Deserialize(r); r.ReadInt(); }
}
public class FishermanMerchantT1 : BaseFishermanMerchant { public override int Tier => 1; [Constructable] public FishermanMerchantT1() {} public FishermanMerchantT1(Serial s) : base(s) {} public override void Serialize(GenericWriter w) { base.Serialize(w); w.Write(0); } public override void Deserialize(GenericReader r) { base.Deserialize(r); r.ReadInt(); } }
public class FishermanMerchantT2 : BaseFishermanMerchant { public override int Tier => 2; [Constructable] public FishermanMerchantT2() {} public FishermanMerchantT2(Serial s) : base(s) {} public override void Serialize(GenericWriter w) { base.Serialize(w); w.Write(0); } public override void Deserialize(GenericReader r) { base.Deserialize(r); r.ReadInt(); } }
public class FishermanMerchantT3 : BaseFishermanMerchant { public override int Tier => 3; [Constructable] public FishermanMerchantT3() {} public FishermanMerchantT3(Serial s) : base(s) {} public override void Serialize(GenericWriter w) { base.Serialize(w); w.Write(0); } public override void Deserialize(GenericReader r) { base.Deserialize(r); r.ReadInt(); } }
public class FishermanMerchantT4 : BaseFishermanMerchant { public override int Tier => 4; [Constructable] public FishermanMerchantT4() {} public FishermanMerchantT4(Serial s) : base(s) {} public override void Serialize(GenericWriter w) { base.Serialize(w); w.Write(0); } public override void Deserialize(GenericReader r) { base.Deserialize(r); r.ReadInt(); } }
}
