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
        public override object Uncomplete => "Ты еще не выполнил все условия этого поручения.";
        public override object Complete => "Отличная работа! Гильдия ценит твой вклад.";

        public DynamicXmlGuildQuest() : base()
        {
        }

        public DynamicXmlGuildQuest(XmlGuildQuestConfig config) : base()
        {
            Config = config;
            InitFromConfig();
        }

        private void InitFromConfig()
        {
            if (Config == null)
                return;

            Objectives.Clear();
            Rewards.Clear();

            foreach (var obj in Config.Objectives)
            {
                if (obj.Type.Equals("Obtain", StringComparison.OrdinalIgnoreCase))
                {
                    AddObjective(new ObtainObjective(obj.TargetType, obj.TargetName, obj.Amount));
                }
                else if (obj.Type.Equals("SlayRank", StringComparison.OrdinalIgnoreCase))
                {
                    AddObjective(new XmlSlayRankObjective(obj.TargetRank, obj.Amount));
                }
                else if (obj.Type.Equals("HuntPoints", StringComparison.OrdinalIgnoreCase))
                {
                    AddObjective(new RankPointsObjective(obj.Amount));
                }
                else
                {
                    AddObjective(new SlayObjective(obj.TargetType, obj.TargetName, obj.Amount));
                }
            }

            if (Config.SetRank > 0)
            {
                AddReward(new BaseReward($"Повышение до {Config.SetRank}-го ранга гильдии"));
            }

            if (Config.RemoveYoung)
            {
                AddReward(new BaseReward("Снятие статуса новичка (Young)"));
            }

            if (Config.RewardGold > 0)
            {
                AddReward(new BaseReward($"{Config.RewardGold:N0} золотых монет"));
            }

            if (Config.RewardItem != null)
            {
                AddReward(new BaseReward($"{Config.RewardItem.Name} (x{Config.RewardItemAmount})"));
            }
        }

        public override bool CanOffer()
        {
            if (Owner == null || Config == null)
                return false;

            if (Config.OnlyYoung && !Owner.Young)
                return false;

            if (Config.ExactRank > 0 && Owner.QuestGuildRank != Config.ExactRank)
                return false;

            if (Owner.QuestGuildRank > Config.RequiredRank)
                return false;

            if (!string.IsNullOrEmpty(Config.RequireNpcGuild))
            {
                if (Config.RequireNpcGuild.Equals("Any", StringComparison.OrdinalIgnoreCase))
                {
                    if (Owner.NpcGuild == NpcGuild.None)
                        return false;
                }
                else if (Enum.TryParse(Config.RequireNpcGuild, true, out NpcGuild reqGuild))
                {
                    if (Owner.NpcGuild != reqGuild)
                        return false;
                }
            }

            if (Config.RequireHarrowerKill && (Owner.ChampionTitles == null || Owner.ChampionTitles.Harrower <= 0))
                return false;

            if (Owner.RawStr < Config.MinStr || Owner.RawDex < Config.MinDex || Owner.RawInt < Config.MinInt)
                return false;

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
                if (Config.SetRank > 0 && Owner.QuestGuildRank > Config.SetRank)
                {
                    Owner.QuestGuildRank = Config.SetRank;
                    Owner.SendMessage(0x35, $"Поздравляем! Ваш новый статус: {Owner.GetGuildRankTitle()}!");
                }

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
                    if (Config.RewardGold <= 60000)
                    {
                        Owner.AddToBackpack(new Gold(Config.RewardGold));
                    }
                    else
                    {
                        Banker.Deposit(Owner, Config.RewardGold);
                        Owner.SendMessage(0x35, $"Награда ({Config.RewardGold:N0} золота) переведена на ваш банковский счет.");
                    }
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
                if (Config != null && Config.SetRank > 0)
                {
                    Owner.SendMessage(0x35, "Испытание выполнено! Вернитесь к любому гильдмастеру, чтобы сдать квест и получить новый ранг.");
                }
                else
                {
                    Owner.SendMessage(0x35, "Задание выполнено! Вернитесь к гильдмастеру, чтобы сдать квест и получить награду.");
                }
            }
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(1);
            writer.Write(Config != null ? Config.Id : string.Empty);

            writer.Write(Objectives.Count);
            foreach (BaseObjective obj in Objectives)
            {
                writer.Write(obj.CurProgress);
                writer.Write(obj.Completed);
            }
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
            string questId = reader.ReadString();

            XmlGuildQuestManager.EnsureLoaded();

            Config = XmlGuildQuestManager.Quests.Find(q => q.Id == questId);

            if (Config == null)
            {
                Console.WriteLine($"[GuildRankSystem] ОШИБКА десериализации: квест с id '{questId}' не найден в GuildQuests.xml!");
            }
            else
            {
                InitFromConfig();
            }

            if (version >= 1)
            {
                int objCount = reader.ReadInt();
                for (int i = 0; i < objCount; i++)
                {
                    int curProgress = reader.ReadInt();
                    bool completed = reader.ReadBool();

                    if (i < Objectives.Count)
                    {
                        Objectives[i].CurProgress = curProgress;
                        if (completed && !Objectives[i].Completed)
                        {
                            Objectives[i].Complete();
                        }
                    }
                }
            }
        }
    }
}
