using System;
using Server.Mobiles;

namespace Server.Engines.Quests
{
	public class RankTrialQuest : BaseQuest
	{
		public override object Title { get { return "Испытание Гильдии"; } }

		public override object Description
		{
			get
			{
				return "Уничтожай опасных существ, чтобы доказать свою силу. Чем выше ранг угрозы монстра, тем быстрее ты завершишь испытание.";
			}
		}

		public override object Refuse { get { return "Возвращайся, когда будешь готов к испытанию."; } }
		public override object Uncomplete { get { return "Твое испытание еще не окончено. Продолжай охоту."; } }
		public override object Complete { get { return "Великолепно! Ты доказал, что достоин нового ранга в гильдии."; } }

		public RankTrialQuest() : base()
		{
			AddObjective(new RankPointsObjective());
			AddReward(new BaseReward("Повышение ранга гильдии"));
		}

		public override void OnAccept()
		{
			base.OnAccept();

			if (Owner != null && Objectives.Count > 0)
			{
				Objectives[0].MaxProgress = RankSystemHelper.GetRequiredPointsForRank(Owner.QuestGuildRank);
			}
		}

		public override bool CanOffer()
		{
			if (Owner != null && Owner.QuestGuildRank <= 1)
				return false;

			return base.CanOffer();
		}

		public override void GiveRewards()
		{
			base.GiveRewards();

			if (Owner != null && Owner.QuestGuildRank > 1)
			{
				Owner.QuestGuildRank--;
				Owner.SendMessage(0x35, $"Поздравляем! Ваш новый статус: {Owner.GetGuildRankTitle()}!");
			}
		}
	}
}
