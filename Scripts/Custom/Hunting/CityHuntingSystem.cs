using System;
using System.IO;
using System.Xml;
using System.Globalization;
using System.Collections.Generic;
using System.Linq;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.Engines.CityLoyalty;
using Server.Engines.Points;
using Server.Custom.CityLevels;
using LoyaltyRating = Server.Engines.CityLoyalty.LoyaltyRating;

namespace Server.Custom.CityHunting
{
    public class TierRequirementDef
    {
        public int Level { get; set; }
        public int MinFame { get; set; }
        public LoyaltyRating MinLoyalty { get; set; }
        public double MinCityLevel { get; set; }
    }

    public static class CityHuntingSystem
    {
        private static readonly string ConfigXmlPath = Path.Combine(Core.BaseDirectory, "Data/Custom/CityHuntingTargets.xml");
        private static readonly Dictionary<int, List<HuntTargetDef>> TargetsByTier = new Dictionary<int, List<HuntTargetDef>>();
        private static readonly List<TierRequirementDef> TierRequirements = new List<TierRequirementDef>();

        public static int MaxTier => TierRequirements.Count > 0 ? TierRequirements.Max(t => t.Level) : 8;

        public static void Initialize()
        {
            LoadConfigFromXml();
            EventSink.CreatureDeath += OnCreatureDeath;
        }

        public static void LoadConfigFromXml()
        {
            TargetsByTier.Clear();
            TierRequirements.Clear();

            if (!File.Exists(ConfigXmlPath))
            {
                Console.WriteLine($"[CityHuntingSystem] XML конфиг не найден: {ConfigXmlPath}");
                return;
            }

            try
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(ConfigXmlPath);

                // 1. Загружаем требования для каждого Tier
                XmlNodeList tierNodes = doc.GetElementsByTagName("Tier");
                foreach (XmlNode node in tierNodes)
                {
                    if (node.Attributes == null)
                        continue;

                    int level = Utility.ToInt32(node.Attributes["level"]?.Value ?? "1");
                    int minFame = Utility.ToInt32(node.Attributes["minFame"]?.Value ?? "0");

                    string loyaltyStr = node.Attributes["minLoyalty"]?.Value ?? "Unknown";
                    if (!Enum.TryParse(loyaltyStr, true, out LoyaltyRating minLoyalty))
                        minLoyalty = LoyaltyRating.Unknown;

                    double minCityLevel = 0.0;
                    if (double.TryParse(node.Attributes["minCityLevel"]?.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out double parsedLvl))
                        minCityLevel = parsedLvl;

                    TierRequirements.Add(new TierRequirementDef
                    {
                        Level = level,
                        MinFame = minFame,
                        MinLoyalty = minLoyalty,
                        MinCityLevel = minCityLevel
                    });

                    if (!TargetsByTier.ContainsKey(level))
                        TargetsByTier[level] = new List<HuntTargetDef>();
                }

                // Сортируем от высшего тира к низшему для быстрой проверки
                TierRequirements.Sort((a, b) => b.Level.CompareTo(a.Level));

                // 2. Загружаем цели охоты
                XmlNodeList targetNodes = doc.GetElementsByTagName("Target");
                int loadedTargets = 0;

                foreach (XmlNode node in targetNodes)
                {
                    if (node.Attributes == null)
                        continue;

                    int tier = Utility.ToInt32(node.Attributes["tier"]?.Value ?? "1");
                    string typeName = node.Attributes["type"]?.Value;
                    string displayName = node.Attributes["name"]?.Value;
                    int minKills = Utility.ToInt32(node.Attributes["minKills"]?.Value ?? "10");
                    int maxKills = Utility.ToInt32(node.Attributes["maxKills"]?.Value ?? "20");
                    int gold = Utility.ToInt32(node.Attributes["gold"]?.Value ?? "1000");
                    bool isBoss = bool.TryParse(node.Attributes["isBoss"]?.Value, out bool b) && b;

                    if (string.IsNullOrWhiteSpace(typeName) || string.IsNullOrWhiteSpace(displayName))
                        continue;

                    if (!TargetsByTier.ContainsKey(tier))
                        TargetsByTier[tier] = new List<HuntTargetDef>();

                    TargetsByTier[tier].Add(new HuntTargetDef(typeName, displayName, minKills, maxKills, gold, isBoss));
                    loadedTargets++;
                }

                Console.WriteLine($"[CityHuntingSystem] Загружено тиров: {TierRequirements.Count}, целей охоты: {loadedTargets}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CityHuntingSystem] Ошибка чтения XML конфига: {ex.Message}");
            }
        }

        private static HuntTargetDef GetRandomTargetForTier(int tier)
        {
            if (TargetsByTier.TryGetValue(tier, out var list) && list.Count > 0)
                return list[Utility.Random(list.Count)];

            var fallback = TargetsByTier.Values.SelectMany(x => x).ToList();
            return fallback.Count > 0 ? fallback[Utility.Random(fallback.Count)] : new HuntTargetDef("Orc", "Орк", 10, 15, 2000);
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

            // Проверяем от максимального Tier к минимальному
            foreach (var req in TierRequirements)
            {
                if (pm.Fame >= req.MinFame && rating >= req.MinLoyalty && cityLevel >= req.MinCityLevel)
                    return req.Level;
            }

            return 1;
        }

        public static HuntingContract GenerateContract(PlayerMobile pm, City city)
        {
            int tier = GetAvailableTier(pm, city);
            HuntTargetDef selected = GetRandomTargetForTier(tier);
            int requiredKills = Utility.RandomMinMax(selected.MinKills, selected.MaxKills);

            return new HuntingContract(pm, city, selected, requiredKills, tier);
        }

        public static BoardPosting CreateBoardPosting(int tier)
        {
            HuntTargetDef selected = GetRandomTargetForTier(tier);
            int kills = Utility.RandomMinMax(selected.MinKills, selected.MaxKills);

            return new BoardPosting
            {
                Target = selected,
                RequiredKills = kills,
                Tier = tier,
                GoldReward = selected.BaseGoldReward + (kills * 150)
            };
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

        public static void CompleteContractReward(PlayerMobile pm, HuntingContract contract)
        {
            City city = contract.City;
            int gold = contract.GoldReward;
            bool isBoss = contract.IsBossQuest;
            int tier = contract.Tier;

            contract.Delete();

            Banker.Deposit(pm, gold);
            pm.SendMessage(0x35, $"Награда в размере {gold} золота переведена в вашу банковскую ячейку!");

            // Шанс роста навыка города и прирост репутации масштабируются под 8 тиров
            double cityGainChance = isBoss ? 1.0 : Math.Min(1.0, 0.15 * tier);
            CityLevelProgression.TryGainCitySkill(city, cityGainChance, pm);

            var loyaltySys = CityLoyaltySystem.GetCityInstance(city);
            if (loyaltySys != null)
            {
                var entry = loyaltySys.GetPlayerEntry<CityLoyaltyEntry>(pm, true);
                if (entry != null)
                {
                    int loveGain = isBoss ? (100 * tier) : (50 * tier);
                    entry.Love += loveGain;
                    pm.SendMessage(0x59, $"Ваша репутация (Love) в городе {city} выросла на +{loveGain}!");
                }
            }
            Server.Misc.Titles.AwardFame(pm, isBoss ? (300 * tier) : (120 * tier), true);
            if (isBoss)
            {
	            pm.AddToBackpack(new Server.Custom.WeaponLeveling.WeaponTemperingScroll());
	            pm.SendMessage(0x35, "За голову легендарного босса вы получили Свиток древней закалки оружия!");
            }
            else if (tier >= 5 && Utility.RandomDouble() < (0.05 * tier))
            {
	            pm.AddToBackpack(new Server.Custom.WeaponLeveling.WeaponTemperingScroll());
	            pm.SendMessage(0x35, "В качестве особой премии вы получили Свиток древней закалки оружия!");
            }
        }
    }
}
