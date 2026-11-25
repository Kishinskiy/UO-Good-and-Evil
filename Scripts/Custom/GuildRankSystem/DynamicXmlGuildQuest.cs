using System;
using Server;
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

        // Пустой конструктор нужен для корректной сериализации/десериализации ServUO
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
            else
            {
                AddObjective(new SlayObjective(config.TargetType, config.TargetName, config.Amount));
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

            // Проверяем ранг игрока (1 - высший, 10 - низший)
            if (Owner.QuestGuildRank > Config.RequiredRank)
                return false;

            return base.CanOffer();
        }

        public override void GiveRewards()
        {
            base.GiveRewards();

            if (Owner != null && Config != null)
            {
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

            // Восстанавливаем конфигурацию квеста из XML после рестарта сервера
            Config = XmlGuildQuestManager.Quests.Find(q => q.Id == questId);
        }
    }
}
