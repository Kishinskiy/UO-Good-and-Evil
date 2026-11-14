using System;
using Server.Custom.EpicAlignment;
using Server.Items;
using Server.Spells;
using Server.Engines.CannedEvil;

namespace Server.Mobiles
{
    [CorpseName("останки Темного Лорда")]
    public class DarkLord : BaseChampion
    {
        private DateTime m_NextAbilityTime;

        [Constructable]
        public DarkLord() : base(AIType.AI_Mage)
        {
            Name = "Малкор";
            Title = "Темный Лорд Бездны";
            Body = 400;
            Hue = 1175;

            SetStr(800, 1000);
            SetDex(150, 200);
            SetInt(600, 800);

            SetHits(35000);
            SetStam(200);
            SetMana(1000);

            SetDamage(22, 32);

            SetDamageType(ResistanceType.Physical, 20);
            SetDamageType(ResistanceType.Cold, 40);
            SetDamageType(ResistanceType.Energy, 40);

            SetResistance(ResistanceType.Physical, 60, 70);
            SetResistance(ResistanceType.Fire, 50, 60);
            SetResistance(ResistanceType.Cold, 70, 80);
            SetResistance(ResistanceType.Poison, 70, 80);
            SetResistance(ResistanceType.Energy, 60, 70);

            SetSkill(SkillName.EvalInt, 120.0);
            SetSkill(SkillName.Magery, 120.0);
            SetSkill(SkillName.Necromancy, 120.0);
            SetSkill(SkillName.SpiritSpeak, 120.0);
            SetSkill(SkillName.Meditation, 120.0);
            SetSkill(SkillName.MagicResist, 130.0);
            SetSkill(SkillName.Tactics, 110.0);
            SetSkill(SkillName.Wrestling, 110.0);

            Fame = 22500;
            Karma = -22500;

            VirtualArmor = 70;

            AddItem(new HoodedShroudOfShadows(1175));
            AddItem(new Scythe { Hue = 1175, Movable = false });
        }

        public override Type[] UniqueList => new Type[] { typeof(EpicBookOfDestiny), typeof(EpicSwordOfVirtues) };
        public override Type[] SharedList => new Type[] { typeof(CloakOfPower) };
        public override Type[] DecorativeList => new Type[] { typeof(EvilIdolSkull) };
        public override MonsterStatuetteType[] StatueTypes => new MonsterStatuetteType[] { MonsterStatuetteType.Daemon };

        public override ChampionSkullType SkullType => ChampionSkullType.Death;

        public override void OnThink()
        {
            base.OnThink();

            if (Combatant != null && DateTime.UtcNow >= m_NextAbilityTime)
            {
                DrainLight();
                m_NextAbilityTime = DateTime.UtcNow + TimeSpan.FromSeconds(15);
            }
        }

        private void DrainLight()
        {
            Say("Ваш свет угаснет! Вы никогда не станете Аватаром!");
            PlaySound(0x1FB);

            foreach (Mobile m in GetMobilesInRange(8))
            {
                if (m != this && m.Player && m.Alive && CanBeHarmful(m))
                {
                    DoHarmful(m);
                    int damage = Utility.RandomMinMax(30, 50);
                    
                    if (m.Karma > 5000)
                    {
                        damage += 25;
                        m.SendMessage(38, "Тьма обжигает вашу светлую душу!");
                    }

                    AOS.Damage(m, this, damage, 0, 0, 50, 0, 50);
                    Hits += damage / 2;
                    m.FixedParticles(0x374A, 10, 15, 5013, 1175, 0, EffectLayer.Waist);
                }
            }
        }

        public override void OnGotMeleeAttack(Mobile attacker)
        {
            base.OnGotMeleeAttack(attacker);

            if (0.15 >= Utility.RandomDouble())
            {
                Say("Слуги Бездны, защитите меня!");
                BaseCreature spawn = new Daemon();
                spawn.MoveToWorld(Location, Map);
                spawn.Combatant = attacker;
            }
        }

        // ВОТ ЭТОТ БЛОК ПЕРЕХВАТЫВАЕТ СМЕРТЬ БОССА
        public override void OnDeath(Container c)
        {
            EpicAvatarRewardSystem.OnChampionDefeated(this);
            base.OnDeath(c);
        }

        public override void GenerateLoot()
        {
            AddLoot(LootPack.SuperBoss, 3);
            AddLoot(LootPack.Meager);
        }

        public override bool AlwaysMurderer => true;
        public override bool BardImmune => true;

        public DarkLord(Serial serial) : base(serial)
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
            int version = reader.ReadInt();
        }
    }
}
