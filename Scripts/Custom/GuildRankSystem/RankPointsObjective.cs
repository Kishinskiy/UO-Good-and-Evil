using System;
using Server;
using Server.Mobiles;

namespace Server.Engines.Quests
{
    public class RankPointsObjective : BaseObjective
    {
        public override object ObjectiveDescription
        {
            get { return $"Наберите {MaxProgress} очков охоты, побеждая достойных противников."; }
        }

        public RankPointsObjective() : base(2500)
        {
        }

        public static void Initialize()
        {
            EventSink.CreatureDeath += OnCreatureDeath;
        }

        private static void OnCreatureDeath(CreatureDeathEventArgs e)
        {
            if (e.Killer is PlayerMobile pm && e.Creature is BaseCreature bc)
            {
                // Проверяем все активные квесты игрока
                if (pm.Quests == null)
                    return;

                foreach (BaseQuest quest in pm.Quests)
                {
                    if (quest is RankTrialQuest)
                    {
                        foreach (BaseObjective obj in quest.Objectives)
                        {
                            if (obj is RankPointsObjective rankObj && !rankObj.Completed)
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

            if (monsterRank == 0)
                return;

            int earned = RankSystemHelper.CalculateEarnedPoints(pm.QuestGuildRank, monsterRank);

            if (earned > 0)
            {
                CurProgress += earned;
                pm.SendMessage(0x59, $"Убит враг {monsterRank}-го ранга! Очки испытания: +{earned} ({CurProgress}/{MaxProgress})");
            }
            else
            {
                pm.SendMessage(0x22, $"Этот враг ({monsterRank}-й ранг) слишком слаб для вашего текущего испытания.");
            }
        }
    }
}
