using System;
using System.Collections.Generic;
using Server;
using Server.Items;

namespace Server.Items
{
    public abstract class BaseTradeCrate : Container
    {
        private Type m_AllowedType;
        private int m_Capacity;

        [CommandProperty(AccessLevel.GameMaster)]
        public Type AllowedType
        {
            get { return m_AllowedType; }
            set { m_AllowedType = value; UpdateCrateName(); InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Capacity
        {
            get { return m_Capacity; }
            set { m_Capacity = value; UpdateCrateName(); InvalidateProperties(); }
        }

        // Реальное количество единиц товара внутри (с учетом стопок Amount)
        [CommandProperty(AccessLevel.GameMaster)]
        public int CurrentAmount
        {
            get
            {
                int count = 0;
                if (Items != null)
                {
                    foreach (Item item in Items)
                    {
                        count += item.Amount;
                    }
                }
                return count;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool IsFull
        {
            get { return CurrentAmount >= m_Capacity; }
        }

        public Item SampleItem
        {
            get
            {
                if (Items != null && Items.Count > 0)
                    return Items[0];
                return null;
            }
        }

        public BaseTradeCrate(int itemID, int capacity, double baseWeight) : base(itemID)
        {
            m_Capacity = capacity;
            Weight = baseWeight;
            MaxItems = capacity;
            UpdateCrateName();
        }

        public BaseTradeCrate(Serial serial) : base(serial)
        {
        }

        public override bool OnDragDrop(Mobile from, Item dropped)
        {
            return TryAddItem(from, dropped, Point3D.Zero, false);
        }

        public override bool OnDragDropInto(Mobile from, Item item, Point3D p)
        {
            return TryAddItem(from, item, p, true);
        }

        private bool TryAddItem(Mobile from, Item item, Point3D p, bool into)
        {
            if (item is Container)
            {
                from.SendMessage("Нельзя положить контейнер внутрь торгового ящика.");
                return false;
            }

            if (IsFull)
            {
                from.SendMessage("Ящик уже полностью заполнен.");
                return false;
            }

            if (m_AllowedType != null && item.GetType() != m_AllowedType)
            {
                from.SendMessage("В этот ящик можно складывать только предметы того же типа, что и первый предмет.");
                return false;
            }

            int spaceLeft = m_Capacity - CurrentAmount;

            // Если предмет стакается и его больше, чем свободного места в ящике
            if (item.Stackable && item.Amount > spaceLeft)
            {
                // Отделяем ровно столько, сколько влезет в ящик
                Item toAdd = Mobile.LiftItemDupe(item, item.Amount - spaceLeft);
                if (toAdd != null)
                {
                    if (m_AllowedType == null)
                        m_AllowedType = toAdd.GetType();

                    DropItem(toAdd);
                    UpdateCrateName();
                    InvalidateProperties();
                    from.SendMessage("Вы положили {0} шт. Ящик полностью заполнен, остаток остался у вас.", spaceLeft);
                    return false; // Возвращаем false, чтобы остаток исходной стопки вернулся в руки/рюкзак игрока
                }
            }

            bool added = into ? base.OnDragDropInto(from, item, p) : base.OnDragDrop(from, item);

            if (added)
            {
                if (m_AllowedType == null)
                    m_AllowedType = item.GetType();

                UpdateCrateName();
                InvalidateProperties();
            }

            return added;
        }

        public override void OnItemRemoved(Item item)
        {
            base.OnItemRemoved(item);

            if (CurrentAmount == 0)
            {
                m_AllowedType = null;
            }

            UpdateCrateName();
            InvalidateProperties();
        }

        public void UpdateCrateName()
        {
            string baseName = String.Format("Торговый ящик ({0} шт.)", m_Capacity);

            if (m_AllowedType != null && CurrentAmount > 0)
            {
                string itemName = SampleItem != null && !String.IsNullOrEmpty(SampleItem.Name)
                    ? SampleItem.Name
                    : m_AllowedType.Name;

                Name = String.Format("{0} [{1}: {2}/{3}]", baseName, itemName, CurrentAmount, m_Capacity);
            }
            else
            {
                Name = String.Format("{0} [Пусто]", baseName);
            }
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);

            string typeName = m_AllowedType != null ? m_AllowedType.Name : "Пусто (любой товар)";
            list.Add(1060658, "Тип товара\t{0}", typeName);
            list.Add(1060659, "Заполнено\t{0} / {1}", CurrentAmount, m_Capacity);

            if (IsFull)
                list.Add(1060660, "Статус\tГотов к продаже");
            else
                list.Add(1060661, "Статус\tНе заполнен");
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0);

            writer.Write(m_Capacity);
            writer.Write(m_AllowedType != null ? m_AllowedType.FullName : null);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();

            m_Capacity = reader.ReadInt();
            string typeName = reader.ReadString();

            if (!string.IsNullOrEmpty(typeName))
                m_AllowedType = ScriptCompiler.FindTypeByFullName(typeName);

            UpdateCrateName();
        }
    }

    public class SmallTradeCrate : BaseTradeCrate
    {
        [Constructable]
        public SmallTradeCrate() : base(0xE3F, 50, 5.0) { }

        public SmallTradeCrate(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write(0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); int v = reader.ReadInt(); }
    }

    public class MediumTradeCrate : BaseTradeCrate
    {
        [Constructable]
        public MediumTradeCrate() : base(0xE3D, 100, 10.0) { }

        public MediumTradeCrate(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write(0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); int v = reader.ReadInt(); }
    }

    public class LargeTradeCrate : BaseTradeCrate
    {
        [Constructable]
        public LargeTradeCrate() : base(0xE3C, 200, 15.0) { }

        public LargeTradeCrate(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write(0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); int v = reader.ReadInt(); }
    }
}
