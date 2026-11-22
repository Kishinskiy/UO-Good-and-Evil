using System.Linq;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.Misc;
using Server.Engines.CityLoyalty;
using Server.Engines.Points;
using Server.Custom.CityLevels;
using Server.Custom.WeaponLeveling;

namespace Server.Custom.CityHunting
{
    public class CityHuntmaster : BaseCreature
    {
        public override bool IsInvulnerable => true;

        [Constructable]
        public CityHuntmaster() : base(AIType.AI_Vendor, FightMode.None, 10, 1, 0.2, 0.4)
        {
            Name = NameList.RandomName("male");
            Title = "Мастер городской охоты";
            Body = 0x190;
            Hue = Race.RandomSkinHue();
            Blessed = true;
            Frozen = true;

            EquipItem(new StuddedChest());
            EquipItem(new StuddedLegs());
            EquipItem(new Cloak(0x59B));
            EquipItem(new Boots());
            EquipItem(new CompositeBow());
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (!(from is PlayerMobile pm) || !pm.InRange(Location, 4))
                return;

            City? city = CityLevelProgression.GetCityAtLocation(Location, Map, allowRadius: false);
            if (!city.HasValue)
            {
                SayTo(pm, "Я принимаю заказы только находясь на службе внутри города.");
                return;
            }

            var completedContract = pm.Backpack?.Items
                .OfType<HuntingContract>()
                .FirstOrDefault(c => c.Owner == pm && c.City == city.Value && c.IsCompleted);

            if (completedContract != null)
            {
                GiveQuestRewards(pm, completedContract);
                return;
            }

            var activeContract = pm.Backpack?.Items
                .OfType<HuntingContract>()
                .FirstOrDefault(c => c.Owner == pm && !c.IsExpired);

            if (activeContract != null)
            {
                SayTo(pm, $"У тебя уже есть активный контракт на [{activeContract.TargetDisplayName}] ({activeContract.CurrentKills}/{activeContract.RequiredKills}). Заверши его или дождись окончания срока.");
                return;
            }

            HuntingContract newContract = CityHuntingSystem.GenerateContract(pm, city.Value);
            pm.AddToBackpack(newContract);

            SayTo(pm, $"Город {city.Value} нуждается в твоём клинке! У тебя есть 24 часа, чтобы истребить: {newContract.TargetDisplayName} ({newContract.RequiredKills} шт.).");
        }

        private void GiveQuestRewards(PlayerMobile pm, HuntingContract contract)
        {
            City city = contract.City;
            CityHuntingSystem.CompleteContractReward(pm, contract);
            SayTo(pm, $"Блестящая работа, {pm.Name}! Город {city} не забудет твой подвиг.");
        }


        public CityHuntmaster(Serial serial) : base(serial)
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
            reader.ReadInt();
            Frozen = true;
        }
    }
}
