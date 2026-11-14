using System;
using Server;
using Server.Items;
using Server.Mobiles;

namespace Server.Custom.EpicAlignment
{
    public class EpicBookOfDestiny : Item
    {
        private bool[] m_CustomSpells = new bool[4];
        private DateTime m_NextGuardSummon;
        private DateTime m_NextBossSummon;

        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime NextGuardSummon { get { return m_NextGuardSummon; } set { m_NextGuardSummon = value; } }

        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime NextBossSummon { get { return m_NextBossSummon; } set { m_NextBossSummon = value; } }

        public bool[] CustomSpells { get { return m_CustomSpells; } }

        [Constructable]
        public EpicBookOfDestiny() : base(0x22C5)
        {
            Name = "Книга Судьбы: Эпический Гримуар Аватара";
            Weight = 1.0;
            LootType = LootType.Blessed; 
            Hue = 2418;

            m_NextGuardSummon = DateTime.UtcNow;
            m_NextBossSummon = DateTime.UtcNow;
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);
            list.Add(1151495); // Стандартный Cliloc: Legendary Artifact
            list.Add(1060658, "Путь\tАватар Света (Good)");
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (from == null || !from.Alive) return;

            PlayerMobile pm = from as PlayerMobile;
            if (pm == null) return;

            if (!pm.IsDemigod || !pm.EpicGood)
            {
                pm.SendMessage(0x22, "Страницы Гримуара Судьбы ослепляют вас! Читать его может только Аватар Света.");
                return;
            }

            if (pm.Backpack == null || !this.IsChildOf(pm.Backpack))
            {
                pm.SendMessage(0x22, "Гримуар Судьбы должен находиться в вашем рюкзаке.");
                return;
            }

            pm.CloseGump(typeof(EpicAvatarBookGump));
            pm.SendGump(new EpicAvatarBookGump(pm, this));
        }

        public EpicBookOfDestiny(Serial serial) : base(serial) { }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)1);

            writer.Write(m_NextGuardSummon);
            writer.Write(m_NextBossSummon);

            writer.Write((int)m_CustomSpells.Length);
            for (int i = 0; i < m_CustomSpells.Length; i++)
                writer.Write(m_CustomSpells[i]);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();

            m_NextGuardSummon = reader.ReadDateTime();
            m_NextBossSummon = reader.ReadDateTime();

            if (version >= 1)
            {
                int count = reader.ReadInt();
                m_CustomSpells = new bool[count];
                for (int i = 0; i < count; i++)
                    m_CustomSpells[i] = reader.ReadBool();
            }
        }
    }
}
