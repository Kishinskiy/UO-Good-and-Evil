using System;
using Server;
using Server.Items;

namespace Server.Custom.Farms
{
    public class FarmProduceBarrel : Container
    {
        private Type m_AllowedType;
        private int m_Capacity = 500; // Вместимость бочки (500 единиц продуктов)

        [CommandProperty(AccessLevel.GameMaster)]
        public Type AllowedType
        {
            get => m_AllowedType;
            set { m_AllowedType = value; UpdateBarrelName(); InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Capacity
        {
            get => m_Capacity;
            set { m_Capacity = value; UpdateBarrelName(); InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int CurrentAmount
        {
            get
            {
                int count = 0;
                if (Items != null)
                {
                    foreach (Item item in Items)
                        count += item.Amount;
                }
                return count;
            }
        }

        public bool IsFull => CurrentAmount >= m_Capacity;

        public Item SampleItem => (Items != null && Items.Count > 0) ? Items[0] : null;

        [Constructable]
        public FarmProduceBarrel() : base(0x0E77) // Графика деревянной бочки (Open Barrel)
        {
            Weight = 15.0;
            MaxItems = 500;
            UpdateBarrelName();
        }

        public FarmProduceBarrel(Serial serial) : base(serial)
        {
        }

        public override bool OnDragDrop(Mobile from, Item dropped)
        {
            return TryAddProduce(from, dropped, Point3D.Zero, false);
        }

        public override bool OnDragDropInto(Mobile from, Item item, Point3D p)
        {
            return TryAddProduce(from, item, p, true);
        }

        private bool TryAddProduce(Mobile from, Item item, Point3D p, bool into)
        {
            // Проверяем, что предмет является продуктом/едой (или пшеницей/семенами, если нужно)
            if (!(item is Food) && !(item is WheatSheaf))
            {
                from.SendMessage("В эту бочку можно складывать только фермерские продукты и урожай.");
                return false;
            }

            if (IsFull)
            {
                from.SendMessage("Бочка уже заполнена доверху.");
                return false;
            }

            if (m_AllowedType != null && item.GetType() != m_AllowedType)
            {
                from.SendMessage("В этой бочке уже хранится другой вид продуктов.");
                return false;
            }

            int spaceLeft = m_Capacity - CurrentAmount;

            // Если стопка больше свободного места — забираем только сколько влезет
            if (item.Stackable && item.Amount > spaceLeft)
            {
                Item toAdd = Mobile.LiftItemDupe(item, item.Amount - spaceLeft);
                if (toAdd != null)
                {
                    if (m_AllowedType == null)
                        m_AllowedType = toAdd.GetType();

                    DropItem(toAdd);
                    UpdateBarrelName();
                    InvalidateProperties();
                    from.SendMessage("Вы положили {0} шт. Бочка заполнена, остаток вернулся в рюкзак.", spaceLeft);
                    return false;
                }
            }

            bool added = into ? base.OnDragDropInto(from, item, p) : base.OnDragDrop(from, item);

            if (added)
            {
                if (m_AllowedType == null)
                    m_AllowedType = item.GetType();

                // Объединяем стопки одного типа внутри бочки, чтобы не плодить слоты
                StackItemsInside(item);

                UpdateBarrelName();
                InvalidateProperties();
            }

            return added;
        }

        // Автоматическое объединение стопок внутри бочки
        private void StackItemsInside(Item newlyAdded)
        {
            if (!newlyAdded.Stackable)
                return;

            for (int i = Items.Count - 1; i >= 0; i--)
            {
                Item existing = Items[i];
                if (existing != newlyAdded && existing.GetType() == newlyAdded.GetType() && existing.Hue == newlyAdded.Hue)
                {
                    existing.Amount += newlyAdded.Amount;
                    newlyAdded.Delete();
                    break;
                }
            }
        }

        public override void OnItemRemoved(Item item)
        {
            base.OnItemRemoved(item);

            if (CurrentAmount == 0)
                m_AllowedType = null;

            UpdateBarrelName();
            InvalidateProperties();
        }

        public void UpdateBarrelName()
        {
            if (m_AllowedType != null && CurrentAmount > 0)
            {
                string itemName = SampleItem != null && !string.IsNullOrEmpty(SampleItem.Name)
                    ? SampleItem.Name
                    : m_AllowedType.Name;

                Name = string.Format("Бочка для продуктов [{0}: {1}/{2}]", itemName, CurrentAmount, m_Capacity);
            }
            else
            {
                Name = string.Format("Бочка для продуктов (0/{0}) [Пусто]", m_Capacity);
            }
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);

            string typeName = m_AllowedType != null ? m_AllowedType.Name : "Пусто (любой урожай)";
            list.Add(1060658, "Содержимое\t{0}", typeName);
            list.Add(1060659, "Заполнено\t{0} / {1}", CurrentAmount, m_Capacity);
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0);

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

            UpdateBarrelName();
        }
    }
}
