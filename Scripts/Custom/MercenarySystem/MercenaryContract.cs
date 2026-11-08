using System;
using Server;
using Server.Items;
using Server.Mobiles;

namespace Server.Custom.MercenarySystem
{
    public class MercenaryContract : Item
    {
        public const int BasePurchasePrice = 65000;
        public const decimal PriceIncreasePerLevel = 0.15m;

        private MercenaryClass m_TargetClass;
        private int m_TargetLevel;

        public static int GetPurchasePrice(MercenaryClass mercenaryClass)
        {
            return GetPurchasePrice(mercenaryClass, 1);
        }

        public static int GetPurchasePrice(MercenaryClass mercenaryClass, int level)
        {
            if (!Enum.IsDefined(typeof(MercenaryClass), mercenaryClass))
                throw new ArgumentOutOfRangeException(nameof(mercenaryClass));

            if (level < 1 || level > 10)
                throw new ArgumentOutOfRangeException(nameof(level));

            decimal price = BasePurchasePrice;

            for (int currentLevel = 1; currentLevel < level; currentLevel++)
                price *= 1m + PriceIncreasePerLevel;

            return (int)Math.Round(price, 0, MidpointRounding.AwayFromZero);
        }

        public static int GetResurrectionPrice(MercenaryClass mercenaryClass, int level)
        {
            return GetPurchasePrice(mercenaryClass, level) / 2;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public MercenaryClass TargetClass { get { return m_TargetClass; } set { m_TargetClass = value; InvalidateProperties(); } }

        [CommandProperty(AccessLevel.GameMaster)]
        public int TargetLevel { get { return m_TargetLevel; } set { m_TargetLevel = Math.Max(1, Math.Min(10, value)); InvalidateProperties(); } }

        public MercenaryContract(MercenaryClass mClass, int level) : base(0x14F0)
        {
            m_TargetClass = mClass;
            TargetLevel = level;
            Weight = 1.0;
            
            // Раскрашиваем свитки под новые архетипы
            switch(mClass)
            {
                case MercenaryClass.Knight: Hue = 2115; break;
                case MercenaryClass.Mage: Hue = 2418; break;
                case MercenaryClass.Ranger: Hue = 2125; break;
                case MercenaryClass.Bard: Hue = 1161; break;
                case MercenaryClass.Thief: Hue = 1109; break;
                case MercenaryClass.Paladin: Hue = 1266; break;
                case MercenaryClass.Necromancer: Hue = 1102; break;
                case MercenaryClass.Samurai: Hue = 2211; break;
                default: Hue = 0; break;
            }
            
            Name = $"Контракт: Наёмник ({mClass}), уровень {m_TargetLevel}";
        }

        public MercenaryContract(MercenaryClass mClass) : this(mClass, 1)
        {
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (!IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1042001); 
                return;
            }

            PlayerMobile pm = from as PlayerMobile;
            if (pm == null || !pm.Alive) return;

            if ((pm.Followers + 2) > pm.FollowersMax)
            {
                pm.SendMessage(0x22, "У вас недостаточно свободных slots контроля! Наёмник занимает 2 слота пати.");
                return;
            }

            BaseMercenary merc = new BaseMercenary(m_TargetClass);
            int nextLevelExperience = (int)(500 * Math.Pow(1.8, m_TargetLevel - 1));
            merc.RestoreProgression(m_TargetLevel, 0, nextLevelExperience);
            merc.MoveToWorld(pm.Location, pm.Map);

            merc.SetControlMaster(pm);
            merc.ControlTarget = pm;
            merc.ControlOrder = OrderType.Follow;

            pm.PlaySound(0x5BC); 
            pm.SendMessage(0x35, $"Вы успешно активировали контракт! К вам присоединился наёмник {merc.Name} {merc.Title}.");

            this.Delete(); 
        }

        public MercenaryContract(Serial s) : base(s) { }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)1);
            writer.Write((int)m_TargetClass);
            writer.Write(m_TargetLevel);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
            m_TargetClass = MercenaryClassUtility.FromSerializedValue(reader.ReadInt());
            TargetLevel = version >= 1 ? reader.ReadInt() : 1;
        }
    }
}
