using System;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.Engines.CityLoyalty;
using Server.Engines.Points;
using Server.Custom.CityLevels;
using LoyaltyRating = Server.Engines.CityLoyalty.LoyaltyRating;


namespace Server.Custom.CityHunting
{
    public static class CityHuntingSystem
    {
        private static readonly HuntTargetDef[] Tier1Targets =
        {
            new HuntTargetDef("Orc", "Орк", 25, 35, 3500),
            new HuntTargetDef("Skeleton", "Скелет", 25, 35, 3000),
            new HuntTargetDef("Ettin", "Эттин", 20, 30, 4500),
            new HuntTargetDef("Troll", "Тролль", 20, 30, 4500),
            new HuntTargetDef("Harpy", "Гарпия", 20, 30, 4000)
        };

        private static readonly HuntTargetDef[] Tier2Targets =
        {
            new HuntTargetDef("OgreLord", "Огр-лорд", 25, 35, 9000),
            new HuntTargetDef("Lich", "Лич", 25, 35, 10000),
            new HuntTargetDef("Gargoyle", "Гаргулья", 25, 35, 8500),
            new HuntTargetDef("AirElemental", "Воздушный элементаль", 25, 35, 9000),
            new HuntTargetDef("EarthElemental", "Земляной элементаль", 25, 35, 8500)
        };

        private static readonly HuntTargetDef[] Tier3Targets =
        {
            new HuntTargetDef("Dragon", "Дракон", 15, 25, 22000),
            new HuntTargetDef("Daemon", "Демон", 15, 25, 20000),
            new HuntTargetDef("LichLord", "Древний Лич", 15, 25, 24000),
            new HuntTargetDef("BloodElemental", "Кровавый элементаль", 15, 25, 23000),
            new HuntTargetDef("Titan", "Титан", 15, 25, 21000)
        };

        private static readonly HuntTargetDef[] BossTargets =
        {
            new HuntTargetDef("Barracoon", "Барракун (Чемпион)", 1, 1, 50000, isBoss: true),
            new HuntTargetDef("Rikktor", "Рикктор (Повелитель драконов)", 1, 1, 60000, isBoss: true),
            new HuntTargetDef("Semidar", "Семидар (Владыка демонов)", 1, 1, 60000, isBoss: true),
            new HuntTargetDef("Mephitis", "Мефитис (Королева пауков)", 1, 1, 55000, isBoss: true),
            new HuntTargetDef("Neira", "Нейра (Некромант)", 1, 1, 55000, isBoss: true),
            new HuntTargetDef("DreadHorn", "Дред Хорн (Peerless Босс)", 1, 1, 65000, isBoss: true),
            new HuntTargetDef("LadyMelisande", "Леди Мелисанда (Peerless Босс)", 1, 1, 65000, isBoss: true)
        };

        public static void Initialize()
        {
            EventSink.CreatureDeath += OnCreatureDeath;
        }

        public static int GetAvailableTier(PlayerMobile pm, City city)
        {
            double cityLevel = CityLevelSystem.GetCity(city)?.SkillValue ?? 0.0;
            var loyaltySys = CityLoyaltySystem.GetCityInstance(city);
            LoyaltyRating rating = LoyaltyRating.Unknown;

            if (loyaltySys != null)
            {
                var entry = loyaltySys.GetPlayerEntry<CityLoyaltyEntry>(pm);
                rating = loyaltySys.GetLoyaltyRating(pm, entry);
            }

            if (pm.Fame >= 10000 && rating >= LoyaltyRating.Adored && cityLevel >= 75.0)
                return 4;

            if (pm.Fame >= 7000 && rating >= LoyaltyRating.Admired && cityLevel >= 50.0)
                return 3;

            if (pm.Fame >= 3000 && rating >= LoyaltyRating.Respected && cityLevel >= 25.0)
                return 2;

            return 1;
        }

        public static HuntingContract GenerateContract(PlayerMobile pm, City city)
        {
            int tier = GetAvailableTier(pm, city);
            HuntTargetDef[] pool;

            switch (tier)
            {
                case 4: pool = BossTargets; break;
                case 3: pool = Tier3Targets; break;
                case 2: pool = Tier2Targets; break;
                default: pool = Tier1Targets; break;
            }

            HuntTargetDef selected = pool[Utility.Random(pool.Length)];
            int requiredKills = Utility.RandomMinMax(selected.MinKills, selected.MaxKills);

            return new HuntingContract(pm, city, selected, requiredKills, tier);
        }

        private static void OnCreatureDeath(CreatureDeathEventArgs e)
        {
            BaseCreature victim = e.Creature as BaseCreature;
            if (victim == null || victim.Summoned || victim.Controlled)
                return;

            Mobile killer = e.Killer;
            if (killer is BaseCreature bc)
                killer = bc.GetMaster();

            if (!(killer is PlayerMobile pm) || pm.Backpack == null)
                return;

            foreach (Item item in pm.Backpack.Items)
            {
                if (item is HuntingContract contract && !contract.Deleted && contract.Owner == pm)
                {
                    if (contract.IsExpired || contract.IsCompleted)
                        continue;

                    if (string.Equals(victim.GetType().Name, contract.TargetTypeName, StringComparison.OrdinalIgnoreCase))
                    {
                        contract.CurrentKills++;
                        contract.InvalidateProperties();

                        if (contract.IsCompleted)
                        {
                            pm.PlaySound(0x5B5);
                            pm.SendMessage(0x35, $"[Охота] Контракт выполнен ({contract.CurrentKills}/{contract.RequiredKills})! Вернитесь к городской доске объявлений или Мастеру Охоты в город {contract.City} за наградой.");
                        }
                        else
                        {
                            pm.SendMessage(0x59, $"[Охота] Цель повержена: {contract.TargetDisplayName} ({contract.CurrentKills} / {contract.RequiredKills}).");
                        }
                        break;
                    }
                }
            }
        }

        public static BoardPosting CreateBoardPosting(int tier)
        {
            HuntTargetDef[] pool;
            switch (tier)
            {
                case 4: pool = BossTargets; break;
                case 3: pool = Tier3Targets; break;
                case 2: pool = Tier2Targets; break;
                default: pool = Tier1Targets; break;
            }

            HuntTargetDef selected = pool[Utility.Random(pool.Length)];
            int kills = Utility.RandomMinMax(selected.MinKills, selected.MaxKills);

            return new BoardPosting
            {
                Target = selected,
                RequiredKills = kills,
                Tier = tier,
                GoldReward = selected.BaseGoldReward + (kills * 150)
            };
        }

        public static void CompleteContractReward(PlayerMobile pm, HuntingContract contract)
        {
            City city = contract.City;
            int gold = contract.GoldReward;
            bool isBoss = contract.IsBossQuest;
            int tier = contract.Tier;

            contract.Delete();

            Banker.Deposit(pm, gold);
            pm.SendMessage(0x35, $"Награда в размере {gold} золота переведена в вашу банковскую ячейку!");

            double cityGainChance = isBoss ? 1.0 : (0.35 * tier);
            CityLevelProgression.TryGainCitySkill(city, cityGainChance, pm);

            var loyaltySys = CityLoyaltySystem.GetCityInstance(city);
            if (loyaltySys != null)
            {
                var entry = loyaltySys.GetPlayerEntry<CityLoyaltyEntry>(pm, true);
                if (entry != null)
                {
                    int loveGain = isBoss ? 500 : (100 * tier);
                    entry.Love += loveGain;
                    pm.SendMessage(0x59, $"Ваша репутация (Love) в городе {city} выросла на +{loveGain}!");
                }
            }

            Server.Misc.Titles.AwardFame(pm, isBoss ? 1500 : (250 * tier), true);

            if (isBoss)
            {
                pm.AddToBackpack(new Server.Custom.WeaponLeveling.WeaponTemperingScroll());
                pm.SendMessage(0x35, "За голову легендарного босса вы получили Свиток древней закалки оружия!");
            }
            else if (tier >= 3 && Utility.RandomDouble() < 0.25)
            {
                pm.AddToBackpack(new Server.Custom.WeaponLeveling.WeaponTemperingScroll());
                pm.SendMessage(0x35, "В качестве особой премии вы получили Свиток древней закалки оружия!");
            }
        }
    }
}
