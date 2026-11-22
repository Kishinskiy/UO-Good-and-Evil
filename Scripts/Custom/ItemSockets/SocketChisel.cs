using System;
using Server;
using Server.Items;
using Server.Targeting;
using Server.Custom.JewelryCrafting;

namespace Server.Custom.ItemSockets
{
    public class SocketChisel : Item
    {
        private int _usesRemaining;

        [CommandProperty(AccessLevel.GameMaster)]
        public int UsesRemaining
        {
            get => _usesRemaining;
            set { _usesRemaining = value; InvalidateProperties(); }
        }

        [Constructable]
        public SocketChisel() : this(25)
        {
        }

        [Constructable]
        public SocketChisel(int uses) : base(0x1026)
        {
            Name = "Ювелирное зубило кузнеца (Инкрустация)";
            Hue = 0x489;
            Weight = 1.0;
            _usesRemaining = uses;
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);
            list.Add(1060584, _usesRemaining.ToString());
            list.Add(1060658, "Назначение\tИнкрустация камней в экипировку");
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (!IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1042001);
                return;
            }

            if (_usesRemaining <= 0)
            {
                from.SendMessage(0x22, "Инструмент затупился и больше не пригоден для работы.");
                Delete();
                return;
            }

            from.SendMessage(0x59, "Выберите ограненный камень ювелира или обычный драгоценный камень в рюкзаке:");
            from.Target = new SelectGemTarget(this);
        }

        private class SelectGemTarget : Target
        {
            private readonly SocketChisel _tool;

            public SelectGemTarget(SocketChisel tool) : base(-1, false, TargetFlags.None)
            {
                _tool = tool;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (_tool == null || _tool.Deleted)
                    return;

                if (!(targeted is Item gem) || !gem.IsChildOf(from.Backpack))
                {
                    from.SendMessage(0x22, "Выберите камень из вашего рюкзака.");
                    return;
                }

                from.SendMessage(0x59, "Теперь выберите оружие, броню, щит или украшение для инкрустации:");
                from.Target = new SelectEquipmentTarget(_tool, gem);
            }
        }

        public class SelectEquipmentTarget : Target
        {
            private readonly SocketChisel _tool;
            private readonly Item _gem;

            public SelectEquipmentTarget(SocketChisel tool, Item gem) : base(-1, false, TargetFlags.None)
            {
                _tool = tool;
                _gem = gem;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (_tool == null || _tool.Deleted || _gem == null || _gem.Deleted)
                    return;

                if (!(targeted is Item item))
                {
                    from.SendMessage(0x22, "Выберите предмет экипировки.");
                    return;
                }

                ItemSocketSystem.TrySocketAnyGem(from, _gem, item);

                _tool.UsesRemaining--;
                if (_tool.UsesRemaining <= 0)
                {
                    from.SendMessage(0x22, "Ваше ювелирное зубило окончательно затупилось.");
                    _tool.Delete();
                }
            }
        }

        public SocketChisel(Serial serial) : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0);
            writer.Write(_usesRemaining);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            reader.ReadInt();
            _usesRemaining = reader.ReadInt();
        }
    }
}