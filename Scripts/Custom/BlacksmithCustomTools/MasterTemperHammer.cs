// **********
// ServUO - MasterTemperHammer.cs
// **********

using Server.Engines.Craft;
using Server.Items;

namespace Server.Custom.BlacksmithTools
{
	    public class MasterTemperHammer : Item
    {
        private int _usesRemaining;

        [CommandProperty(AccessLevel.GameMaster)]
        public int UsesRemaining
        {
            get => _usesRemaining;
            set { _usesRemaining = value; InvalidateProperties(); }
        }

        [Constructable]
        public MasterTemperHammer() : this(20)
        {
        }

        [Constructable]
        public MasterTemperHammer(int uses) : base(0x13E3)
        {
            Name = "Молот глубокой закалки";
            Hue = 0x482;
            Weight = 4.0;
            _usesRemaining = uses;
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);
            list.Add(1060584, _usesRemaining.ToString());
            list.Add(1060658, "Эффект\tВосстановление прочности без износа");
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (!IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1042001);
                return;
            }

            if (from.Skills[SkillName.Blacksmith].Value < 90.0)
            {
                from.SendMessage(0x22, "Для работы этим молотом требуется навык Blacksmithy 90.0.");
                return;
            }

            from.SendMessage(0x59, "Выберите повреждённое металлическое оружие или броню в рюкзаке:");
            from.Target = new RepairTarget(this);
        }

        private class RepairTarget : Server.Targeting.Target
        {
            private readonly MasterTemperHammer _hammer;

            public RepairTarget(MasterTemperHammer hammer) : base(-1, false, Server.Targeting.TargetFlags.None)
            {
                _hammer = hammer;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (_hammer == null || _hammer.Deleted)
                    return;

                if (!(targeted is Item item) || !item.IsChildOf(from.Backpack))
                {
                    from.SendMessage(0x22, "Предмет должен лежать в вашем рюкзаке.");
                    return;
                }

                DefBlacksmithy.CheckAnvilAndForge(from, 3, out bool anvil, out bool forge);
                if (!anvil || !forge)
                {
                    from.SendMessage(0x22, "Вы должны стоять рядом с наковальней и горном!");
                    return;
                }

                if (item is BaseWeapon w)
                {
                    if (w.HitPoints >= w.MaxHitPoints)
                    {
                        from.SendMessage(0x22, "Этот предмет не нуждается в починке.");
                        return;
                    }
                    w.HitPoints = w.MaxHitPoints;
                }
                else if (item is BaseArmor a)
                {
                    if (a.HitPoints >= a.MaxHitPoints)
                    {
                        from.SendMessage(0x22, "Эта броня не нуждается в починке.");
                        return;
                    }
                    a.HitPoints = a.MaxHitPoints;
                }
                else
                {
                    from.SendMessage(0x22, "Этот молот подходит только для оружия и брони.");
                    return;
                }

                from.PlaySound(0x2A);
                from.SendMessage(0x35, "Вы идеально закалили и восстановили предмет без потери прочности!");

                _hammer.UsesRemaining--;
                if (_hammer.UsesRemaining <= 0)
                {
                    from.SendMessage(0x22, "Ваш молот глубокой закалки разрушился.");
                    _hammer.Delete();
                }
            }
        }

        public MasterTemperHammer(Serial serial) : base(serial) { }

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
