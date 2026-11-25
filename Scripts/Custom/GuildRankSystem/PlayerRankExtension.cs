using System;
using Server;

namespace Server.Mobiles
{
	public partial class PlayerMobile
	{
		private int m_QuestGuildRank = 11; // 11 = нет ранга, 10 = низший, 1 = высший

		[CommandProperty(AccessLevel.GameMaster)]
		public int QuestGuildRank
		{
			get { return m_QuestGuildRank; }
			set
			{
				m_QuestGuildRank = Math.Max(1, Math.Min(11, value));
				InvalidateProperties();
			}
		}

		public string GetGuildRankTitle()
		{
			return m_QuestGuildRank > 10 ? "Без ранга" : $"{m_QuestGuildRank}-й ранг";
		}
	}
}
