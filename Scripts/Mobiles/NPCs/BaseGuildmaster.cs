using Server.ContextMenus;
using Server.Items;
using Server.Network;
using System;
using System.Collections.Generic;
using Server.Engines.Quests;

namespace Server.Mobiles
{
    public abstract class BaseGuildmaster : BaseVendor
    {
        private readonly List<SBInfo> m_SBInfos = new List<SBInfo>();
        public BaseGuildmaster(string title)
            : base(title)
        {
            Title = string.Format("the {0} {1}", title, Female ? "guildmistress" : "guildmaster");
        }

        public BaseGuildmaster(Serial serial)
            : base(serial)
        {
        }

        public override bool IsActiveVendor => false;
        public override bool ClickTitle => false;
        public virtual int JoinCost => 500;
        public virtual TimeSpan JoinAge => TimeSpan.FromDays(0.0);
        public virtual TimeSpan JoinGameAge => TimeSpan.FromDays(2.0);
        public virtual TimeSpan QuitAge => TimeSpan.FromDays(7.0);
        public virtual TimeSpan QuitGameAge => TimeSpan.FromDays(4.0);
        protected override List<SBInfo> SBInfos => m_SBInfos;
        public override void InitSBInfo()
        {
        }

        public virtual bool CheckCustomReqs(PlayerMobile pm)
        {
	        return true;
        }

        public virtual void SayGuildTo(Mobile m)
        {
            SayTo(m, 1008055 + (int)NpcGuild);
        }

        public virtual void SayWelcomeTo(Mobile m)
        {
            SayTo(m, 1008054); // Welcome to the guild! Thou shalt find that fellow members shall grant thee lower prices in shops.
        }

        public virtual void SayPriceTo(Mobile m)
        {
            m.Send(new MessageLocalizedAffix(m.NetState, Serial, Body, MessageType.Regular, SpeechHue, 3, 1008052, Name, AffixType.Append, JoinCost.ToString(), ""));
        }

        public virtual bool WasNamed(string speech)
        {
            string name = Name;

            return (name != null && Insensitive.StartsWith(speech, name));
        }

        public override bool HandlesOnSpeech(Mobile from)
        {
            if (from.InRange(Location, 2))
                return true;

            return base.HandlesOnSpeech(from);
        }

        public override void OnSpeech(SpeechEventArgs e)
        {
            Mobile from = e.Mobile;

            if (!e.Handled && from is PlayerMobile && from.InRange(Location, 2) && WasNamed(e.Speech))
            {
                PlayerMobile pm = (PlayerMobile)from;

                if (e.HasKeyword(0x0004)) // *join* | *member*
                {
                    if (pm.NpcGuild == NpcGuild)
                        SayTo(from, 501047); // Thou art already a member of our guild.
                    else if (pm.NpcGuild != NpcGuild.None)
                        SayTo(from, 501046); // Thou must resign from thy other guild first.
                    else if (pm.GameTime < JoinGameAge || (pm.CreationTime + JoinAge) > DateTime.UtcNow)
                        SayTo(from, 501048); // You are too young to join my guild...
                    else if (CheckCustomReqs(pm))
                        SayPriceTo(from);

                    e.Handled = true;
                }
                else if (e.HasKeyword(0x0005)) // *resign* | *quit*
                {
                    if (pm.NpcGuild != NpcGuild)
                    {
                        SayTo(from, 501052); // Thou dost not belong to my guild!
                    }
                    else if ((pm.NpcGuildJoinTime + QuitAge) > DateTime.UtcNow || (pm.NpcGuildGameTime + QuitGameAge) > pm.GameTime)
                    {
                        SayTo(from, 501053); // You just joined my guild! You must wait a week to resign.
                    }
                    else
                    {
                        SayTo(from, 501054); // I accept thy resignation.
                        pm.NpcGuild = NpcGuild.None;
                    }

                    e.Handled = true;
                }
            }

            base.OnSpeech(e);
        }

        public virtual Type[] GuildQuests
        {
            get
            {
                return new Type[]
                {
                    typeof(RankTrialQuest)
                    // Сюда в будущем добавляйте новые квесты гильдии, например:
                    // typeof(MyRank5GuildQuest),
                };
            }
        }

                public override void OnDoubleClick(Mobile from)
        {
            if (from is PlayerMobile pm && pm.InRange(Location, 3))
            {
                // 1. Проверяем активные квесты (испытание ранга или взятый XML-квест)
                foreach (BaseQuest active in pm.Quests)
                {
                    if (active is RankTrialQuest || active is DynamicXmlGuildQuest)
                    {
                        if (active.Completed)
                        {
                            active.GiveRewards();
                        }
                        else
                        {
                            pm.SendGump(new MondainQuestGump(active, MondainQuestGump.Section.InProgress, false));
                        }
                        return;
                    }
                }

                // 2. Собираем доступные квесты
                List<BaseQuest> availableQuests = new List<BaseQuest>();

                // Добавляем испытание ранга, если еще не 1-й ранг
                RankTrialQuest trial = new RankTrialQuest { Owner = pm, Quester = this };
                if (trial.CanOffer())
                {
                    availableQuests.Add(trial);
                }

                // Добавляем квесты из XML, подходящие по рангу и типу гильдмастера
                string masterTypeName = GetType().Name; // Например, "MageGuildmaster"

                foreach (XmlGuildQuestConfig cfg in XmlGuildQuestManager.Quests)
                {
                    if (cfg.GuildType.Equals("Any", StringComparison.OrdinalIgnoreCase) ||
                        masterTypeName.IndexOf(cfg.GuildType, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        DynamicXmlGuildQuest xmlQuest = new DynamicXmlGuildQuest(cfg)
                        {
                            Owner = pm,
                            Quester = this
                        };

                        if (xmlQuest.CanOffer())
                        {
                            availableQuests.Add(xmlQuest);
                        }
                    }
                }

                // 3. Предлагаем первый доступный квест (или меню выбора, если их несколько)
                if (availableQuests.Count == 1)
                {
	                pm.SendGump(new MondainQuestGump(availableQuests[0]));
	                return;
                }
                else if (availableQuests.Count > 1)
                {
	                pm.CloseGump(typeof(GuildQuestSelectGump));
	                pm.SendGump(new GuildQuestSelectGump(pm, availableQuests));
	                return;
                }
            }

            base.OnDoubleClick(from);
        }

        public class GuildQuestSelectGump : Server.Gumps.Gump
        {
	        private readonly PlayerMobile m_Player;
	        private readonly List<BaseQuest> m_Quests;

	        public GuildQuestSelectGump(PlayerMobile player, List<BaseQuest> quests) : base(100, 100)
	        {
		        m_Player = player;
		        m_Quests = quests;

		        AddPage(0);
		        AddBackground(0, 0, 360, 100 + (quests.Count * 35), 9270);
		        AddLabel(25, 20, 1153, $"Выберите задание гильдии (Ваш статус: {player.GetGuildRankTitle()}):");

		        for (int i = 0; i < quests.Count; i++)
		        {
			        int y = 55 + (i * 35);
			        AddButton(25, y, 4005, 4007, i + 1, Server.Gumps.GumpButtonType.Reply, 0);
			        AddLabel(65, y + 2, 0x480, quests[i].Title.ToString());
		        }
	        }

	        public override void OnResponse(NetState sender, Server.Gumps.RelayInfo info)
	        {
		        int index = info.ButtonID - 1;
		        if (index >= 0 && index < m_Quests.Count)
		        {
			        m_Player.SendGump(new MondainQuestGump(m_Quests[index]));
		        }
	        }
        }



        public override bool OnGoldGiven(Mobile from, Gold dropped)
        {
            if (from is PlayerMobile && dropped.Amount == JoinCost)
            {
                PlayerMobile pm = (PlayerMobile)from;

                if (pm.NpcGuild == NpcGuild)
                {
                    SayTo(from, 501047); // Thou art already a member of our guild.
                }
                else if (pm.NpcGuild != NpcGuild.None)
                {
                    SayTo(from, 501046); // Thou must resign from thy other guild first.
                }
                else if (pm.GameTime < JoinGameAge || (pm.CreationTime + JoinAge) > DateTime.UtcNow)
                {
                    SayTo(from, 501048); // You are too young to join my guild...
                }
                else if (CheckCustomReqs(pm))
                {
                    SayWelcomeTo(from);

                    pm.NpcGuild = NpcGuild;
                    pm.NpcGuildJoinTime = DateTime.UtcNow;
                    pm.NpcGuildGameTime = pm.GameTime;

                    dropped.Delete();
                    return true;
                }

                return false;
            }

            return base.OnGoldGiven(from, dropped);
        }

        public override void AddCustomContextEntries(Mobile from, List<ContextMenuEntry> list)
        {
            base.AddCustomContextEntries(from, list);
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }
}
