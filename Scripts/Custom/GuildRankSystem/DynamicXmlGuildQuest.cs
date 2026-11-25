using System;
using System.Collections.Generic;
using Server;
using Server.Accounting;
using Server.Items;
using Server.Mobiles;

namespace Server.Engines.Quests
{
    public class DynamicXmlGuildQuest : BaseQuest
    {
        public XmlGuildQuestConfig Config { get; private set; }

        public override object Title => Config != null ? Config.Title : "Задание гильдии";
        public override object Description => Config != null ? Config.Description : "";
        public override object Refuse => "Возвращайся, если передумаешь.";
        public override object Uncomplete => "Ты еще не выполнил условия этого поручения.";
        public override object Complete => "Отличная работа! Гильдия ценит твой вклад.";

        public DynamicXmlGuildQuest() : base()
        {
        }

        public DynamicXmlGuildQuest(XmlGuildQuestConfig config) : base()
        {
            Config = config;

            if (config.ObjectiveType.Equals("Obtain", StringComparison.OrdinalIgnoreCase))
            {
                AddObjective(new ObtainObjective(config.TargetType, config.TargetName, config.Amount));
            }
            else if (config.ObjectiveType.Equals("SlayRank", StringComparison.OrdinalIgnoreCase))
            {
                AddObjective(new XmlSlayRankObjective(config.TargetRank, config.Amount));
            }
            else
            {
                AddObjective(new SlayObjective(config.TargetType, config.TargetName, config.Amount));
            }

            if (config.RemoveYoung)
            {
                AddReward(new BaseReward("Снятие статуса новичка (Young)"));
            }

            if (config.RewardGold > 0)
            {
                AddReward(new BaseReward($"{config.RewardGold} золотых монет"));
            }

            if (config.RewardItem != null)
            {
                AddReward(new BaseReward($"{config.RewardItem.Name} (x{config.RewardItemAmount})"));
            }
        }

        public override bool CanOffer()
        {
            if (Owner == null || Config == null)
                return false;

            // Если квест только для новичков (Young)
            if (Config.OnlyYoung && !Owner.Young)
                return false;

            // Проверка ранга гильдии
            if (Owner.QuestGuildRank > Config.RequiredRank)
                return false;

            // Проверка характеристик (статов)
            if (Owner.RawStr < Config.MinStr || Owner.RawDex < Config.MinDex || Owner.RawInt < Config.MinInt)
                return false;

            // Проверка навыков (скиллов)
            foreach (KeyValuePair<SkillName, double> req in Config.RequiredSkills)
            {
                if (Owner.Skills[req.Key].Base < req.Value)
                    return false;
            }

            return base.CanOffer();
        }

        public override void GiveRewards()
        {
            base.GiveRewards();

            if (Owner != null && Config != null)
            {
                if (Config.RemoveYoung && Owner.Young)
                {
                    if (Owner.Account is Account acc)
                    {
                        acc.RemoveYoungStatus(0);
                    }

                    Owner.Young = false;
                    Owner.SendMessage(0x35, "Ваш статус новичка (Young) успешно снят! Теперь вам открыт весь мир.");
                }

                if (Config.RewardGold > 0)
                {
                    Owner.AddToBackpack(new Gold(Config.RewardGold));
                }

                if (Config.RewardItem != null)
                {
                    try
                    {
                        if (Activator.CreateInstance(Config.RewardItem) is Item item)
                        {
                            if (item.Stackable)
                            {
                                item.Amount = Config.RewardItemAmount;
                                Owner.AddToBackpack(item);
                            }
                            else
                            {
                                Owner.AddToBackpack(item);
                                for (int i = 1; i < Config.RewardItemAmount; i++)
                                {
                                    if (Activator.CreateInstance(Config.RewardItem) is Item extra)
                                        Owner.AddToBackpack(extra);
                                }
                            }
                        }
                    }
                    catch { }
                }
            }
        }
        
        public override void OnCompleted()
        {
	        if (Owner != null)
	        {
		        Owner.PlaySound(CompleteSound);
		        Owner.SendMessage(0x35, "Испытание выполнено! Вернитесь к любому гильдмастеру, чтобы сдать квест и получить новый ранг.");
	        }
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0);
            writer.Write(Config != null ? Config.Id : string.Empty);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
            string questId = reader.ReadString();

            Config = XmlGuildQuestManager.Quests.Find(q => q.Id == questId);
        }
    }
}
