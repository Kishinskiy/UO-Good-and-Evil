using System;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.Engines.CityLoyalty;

namespace Server.Custom.CityHunting
{
    public class HuntingContract : Item
    {
        [CommandProperty(AccessLevel.GameMaster)]
        public PlayerMobile Owner { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public City City { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public string TargetTypeName { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public string TargetDisplayName { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public int RequiredKills { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public int CurrentKills { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public int GoldReward { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Tier { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool IsBossQuest { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime ExpiresAt { get; set; }

        public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
        public bool IsCompleted => CurrentKills >= RequiredKills;

        public HuntingContract(PlayerMobile owner, City city, HuntTargetDef def, int kills, int tier) : base(0x14EF)
        {
            Owner = owner;
            City = city;
            TargetTypeName = def.TypeName;
            TargetDisplayName = def.DisplayName;
            RequiredKills = kills;
            CurrentKills = 0;
            Tier = tier;
            IsBossQuest = def.IsBoss;
            GoldReward = def.BaseGoldReward + (kills * 150);
            ExpiresAt = DateTime.UtcNow.AddHours(24.0);

            Name = IsBossQuest ? $"Королевский указ на босса ({city})" : $"Контракт на охоту ({city})";
            Hue = IsBossQuest ? 0x22 : 0x59;
            Weight = 1.0;
            LootType = LootType.Blessed;
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);

            list.Add(1060658, "Город-заказчик\t{0}", City);
            list.Add(1060659, "Цель охоты\t{0}", TargetDisplayName);
            list.Add(1060660, "Прогресс\t{0} / {1} {2}", CurrentKills, RequiredKills, IsCompleted ? "(ВЫПОЛНЕНО)" : "");
            list.Add(1060661, "Награда\t{0} золота + репутация", GoldReward);

            if (IsExpired && !IsCompleted)
            {
                list.Add(1060662, "Статус\tПРОСРОЧЕН");
            }
            else if (!IsCompleted)
            {
                TimeSpan rem = ExpiresAt - DateTime.UtcNow;
                list.Add(1060662, "Осталось времени\t{0} ч. {1} мин.", (int)rem.TotalHours, rem.Minutes);
            }
        }

        public override void OnDoubleClick(Mobile from)
        {
            InvalidateProperties();
            if (IsExpired && !IsCompleted)
                from.SendMessage(0x22, "Срок действия этого контракта (24 часа) истёк. Вы можете выбросить его и взять новый.");
            else
                from.SendMessage(0x59, $"Контракт города {City}: убито {CurrentKills} из {RequiredKills} [{TargetDisplayName}].");
        }

        public HuntingContract(Serial serial) : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0);
            writer.Write(Owner);
            writer.Write((int)City);
            writer.Write(TargetTypeName);
            writer.Write(TargetDisplayName);
            writer.Write(RequiredKills);
            writer.Write(CurrentKills);
            writer.Write(GoldReward);
            writer.Write(Tier);
            writer.Write(IsBossQuest);
            writer.Write(ExpiresAt);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            reader.ReadInt();
            Owner = reader.ReadMobile() as PlayerMobile;
            City = (City)reader.ReadInt();
            TargetTypeName = reader.ReadString();
            TargetDisplayName = reader.ReadString();
            RequiredKills = reader.ReadInt();
            CurrentKills = reader.ReadInt();
            GoldReward = reader.ReadInt();
            Tier = reader.ReadInt();
            IsBossQuest = reader.ReadBool();
            ExpiresAt = reader.ReadDateTime();
        }
    }
}
