using System;
using Server;
using Server.Mobiles;

namespace Server.Engines.Quests
{
	public class XmlSlayRankObjective : BaseObjective
	{
		private readonly int m_TargetRank;

		public override object ObjectiveDescription
		{
			get { return $"Убейте {MaxProgress} монстров {m_TargetRank}-го ранга (убито: {CurProgress}/{MaxProgress})."; }
		}

		public XmlSlayRankObjective(int targetRank, int amount) : base(amount)
		{
			m_TargetRank = targetRank;
		}

		public static void Initialize()
		{
			EventSink.CreatureDeath += OnCreatureDeath;
		}

		private static void OnCreatureDeath(CreatureDeathEventArgs e)
		{
			if (e.Killer is PlayerMobile pm && e.Creature is BaseCreature bc && pm.Quests != null)
			{
				foreach (BaseQuest quest in pm.Quests)
				{
					if (quest is DynamicXmlGuildQuest)
					{
						foreach (BaseObjective obj in quest.Objectives)
						{
							if (obj is XmlSlayRankObjective rankObj && !rankObj.Completed)
							{
								rankObj.ProcessKill(pm, bc);
							}
						}
					}
				}
			}
		}

		public void ProcessKill(PlayerMobile pm, BaseCreature bc)
		{
			int monsterRank = RankSystemHelper.GetMonsterRank(bc);

			if (monsterRank == m_TargetRank)
			{
				CurProgress++;
				pm.SendMessage(0x59, $"Убит монстр {m_TargetRank}-го ранга! Прогресс: {CurProgress}/{MaxProgress}");
			}
		}
	}
}
