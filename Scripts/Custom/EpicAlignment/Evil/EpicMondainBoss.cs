using System;
using Server.Custom.EpicAlignment;
using Server.Items;
using Server.Spells;
using Server.Engines.CannedEvil;

namespace Server.Mobiles
{
    [CorpseName("останки Верховного Серафима")]
    public class SeraphimGuardian : BaseChampion
    {
        private DateTime m_NextAbilityTime;

        [Constructable]
        public SeraphimGuardian() : base(AIType.AI_Mage)
        {
            Name = "Аурелион";
            Title = "Верховный Серафим Света";
            Body = 400; // Тело человека (или 123 для Ethereal Warrior)
            Hue = 1153; // Сияющий небесно-белый/золотой оттенок (Pure White / Ice)

            SetStr(800, 1000);
            SetDex(150, 200);
            SetInt(600, 800);

            SetHits(35000);
            SetStam(200);
            SetMana(1000);

            SetDamage(22, 32);

            SetDamageType(ResistanceType.Physical, 20);
            SetDamageType(ResistanceType.Fire, 40);
            SetDamageType(ResistanceType.Energy, 40);

            SetResistance(ResistanceType.Physical, 60, 70);
            SetResistance(ResistanceType.Fire, 70, 80);
            SetResistance(ResistanceType.Cold, 50, 60);
            SetResistance(ResistanceType.Poison, 70, 80);
            SetResistance(ResistanceType.Energy, 60, 70);

            SetSkill(SkillName.EvalInt, 120.0);
            SetSkill(SkillName.Magery, 120.0);
            SetSkill(SkillName.Chivalry, 120.0); // Паладинская магия Света
            SetSkill(SkillName.Meditation, 120.0);
            SetSkill(SkillName.MagicResist, 130.0);
            SetSkill(SkillName.Tactics, 110.0);
            SetSkill(SkillName.Swords, 110.0);

            Fame = 22500;
            Karma = 22500; // Максимально светлая карма

            VirtualArmor = 70;

            // Небесная экипировка
            AddItem(new HoodedShroudOfShadows(1153));
            AddItem(new VikingSword { Hue = 1153, Name = "Клинок Рассвета", Movable = false });
        }

        // Реальные артефакты из сборки ServUO
        public override Type[] UniqueList => new Type[] 
        { 
            typeof(Aegis), 
            typeof(HolyKnightsBreastplate), 
            typeof(CrimsonCincture),
            typeof(EpicGrimoireOfChaos),
            typeof(EpicMondainAxe)
        };

        public override Type[] SharedList => new Type[] 
        { 
 
            typeof(LieutenantOfTheBritannianRoyalGuard),
            typeof(EmbroideredOakLeafCloak)
        };

        public override Type[] DecorativeList => new Type[] 
        { 
            typeof(MonsterStatuette), 
        };

        public override MonsterStatuetteType[] StatueTypes => new MonsterStatuetteType[] { MonsterStatuetteType.ArchDemon };
        public override ChampionSkullType SkullType => ChampionSkullType.Enlightenment;

        // Механика: Карающий Свет (урон по области + повышенный урон по персонажам Тьмы)
        public override void OnThink()
        {
            base.OnThink();

            if (Combatant != null && DateTime.UtcNow >= m_NextAbilityTime)
            {
                SmiteDarkness();
                m_NextAbilityTime = DateTime.UtcNow + TimeSpan.FromSeconds(15);
            }
        }

        private void SmiteDarkness()
        {
            Say("Свет Небес испепелит вашу порочную душу! Тьма не пройдет!");
            PlaySound(0x212);

            foreach (Mobile m in GetMobilesInRange(8))
            {
                if (m != this && m.Player && m.Alive && CanBeHarmful(m))
                {
                    DoHarmful(m);
                    int damage = Utility.RandomMinMax(30, 50);

                    // Наносим повышенный урон игрокам с отрицательной кармой ("силам Тьмы")
                    if (m.Karma < -5000)
                    {
                        damage += 25;
                        m.SendMessage(53, "Священный огонь обжигает вашу темную душу!");
                    }

                    AOS.Damage(m, this, damage, 0, 50, 0, 0, 50);
                    Hits += damage / 2;
                    m.FixedParticles(0x3709, 10, 30, 5052, 1153, 0, EffectLayer.LeftFoot);
                }
            }
        }

        // При получении урона призывает Ангелов-Хранителей
        public override void OnGotMeleeAttack(Mobile attacker)
        {
            base.OnGotMeleeAttack(attacker);

            if (0.15 >= Utility.RandomDouble())
            {
                Say("Воинство Света, на защиту Святилища!");
                BaseCreature spawn = new EtherealWarrior(); // Или EtherealWarrior для более жесткого боя
                spawn.MoveToWorld(Location, Map);
                spawn.Combatant = attacker;
            }
        }

        // Перехват смерти босса: запускаем выдачу статуса Владыки Тьмы (EpicEvil)
        public override void OnDeath(Container c)
        {
            EpicAvatarRewardSystem.OnDarkChampionDefeated(this);
            base.OnDeath(c);
        }

        public override void GenerateLoot()
        {
            AddLoot(LootPack.SuperBoss, 3);
            AddLoot(LootPack.Meager);
        }

        public override bool AlwaysMurderer => false;
        public override bool InitialInnocent => true; // Выглядит синим (добрым), пока не атакуют
        public override bool BardImmune => true;

        public SeraphimGuardian(Serial serial) : base(serial)
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
