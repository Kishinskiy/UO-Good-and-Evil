using System;
using System.Collections.Generic;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.Custom.MercenarySystem;

namespace Server.Mobiles
{
    public class MercenaryCaptain : BaseVendor
    {
        private readonly List<SBInfo> m_SBInfos = new List<SBInfo>();
        protected override List<SBInfo> SBInfos { get { return m_SBInfos; } }

        [Constructable]
        public MercenaryCaptain() : base("Капитан Наёмников (Recruiter)")
        {
            Name = NameList.RandomName("male");
            Title = ",Mercenary Captain";
            RangeHome = 4;
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (from == null || !from.Alive || !from.InRange(this.Location, 2)) return;

            PlayerMobile pm = from as PlayerMobile;
            if (pm == null) return;

            // Ищем, есть ли у игрока павшие наёмники в нашей новой RescueSystem
            var fallenList = MercenaryRescueSystem.GetFallenMercenaries(pm);

            if (fallenList != null && fallenList.Count > 0)
            {
                // Если есть погибшие, открываем интерактивное меню выкупа (Гамп)
                pm.CloseGump(typeof(MercenaryBuybackGump));
                pm.SendGump(new MercenaryBuybackGump(this, pm, fallenList));
            }
            else
            {
                // Если все живы — открываем стандартное окно покупки новых контрактов из SBMercenaryCaptain
                base.OnDoubleClick(from);
            }
        }

        public void OpenShop(Mobile from)
        {
            if (from != null && !from.Deleted && from.Alive && from.InRange(Location, 2))
                base.OnDoubleClick(from);
        }

        public override void InitSBInfo()
        {
            m_SBInfos.Add(new SBMercenaryCaptain());
        }

        public override void InitOutfit()
        {
            AddItem(new PlateChest() { Hue = 2115 });
            AddItem(new PlateLegs() { Hue = 2115 });
            AddItem(new Longsword());
            AddItem(new Boots());
            Utility.AssignRandomHair(this);
        }

        public MercenaryCaptain(Serial s) : base(s) { }

        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write((int)0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); int version = reader.ReadInt(); }
    }
}

namespace Server.Items
{
    public class SBMercenaryCaptain : SBInfo
    {
        private readonly List<IBuyItemInfo> m_BuyInfo = new List<IBuyItemInfo>();
        private readonly IShopSellInfo m_SellInfo = new GenericSellInfo();

        public override IShopSellInfo SellInfo { get { return m_SellInfo; } }
        public override List<IBuyItemInfo> BuyInfo { get { return m_BuyInfo; } }

        public SBMercenaryCaptain()
        {
            AddContractLevels(MercenaryClass.Knight, 2115);
            AddContractLevels(MercenaryClass.Mage, 2418);
            AddContractLevels(MercenaryClass.Ranger, 2125);
            AddContractLevels(MercenaryClass.Bard, 1161);
            AddContractLevels(MercenaryClass.Thief, 1109);
            AddContractLevels(MercenaryClass.Paladin, 1266);
            AddContractLevels(MercenaryClass.Necromancer, 1102);
            AddContractLevels(MercenaryClass.Samurai, 2211);
        }

        private void AddContractLevels(MercenaryClass mercenaryClass, int hue)
        {
            for (int level = 1; level <= 3; level++)
            {
                int price = MercenaryContract.GetPurchasePrice(mercenaryClass, level);

                m_BuyInfo.Add(new GenericBuyInfo(
                    $"Контракт: Наёмник ({mercenaryClass}), уровень {level}",
                    typeof(MercenaryContract),
                    price,
                    20,
                    0x14F0,
                    hue,
                    new object[] { mercenaryClass, level }));
            }
        }
    }
}
