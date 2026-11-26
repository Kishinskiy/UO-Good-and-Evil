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

        // Принимаем нужное количество очков из XML (по умолчанию 5000)
        public RankPointsObjective(int maxProgress = 5000) : base(maxProgress)
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
                if (pm.Quests == null)
                    return;

                foreach (BaseQuest quest in pm.Quests)
                {
                    // Проверяем активные XML-квесты игрока
                    if (quest is DynamicXmlGuildQuest)
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
                int targetRank = Math.Max(1, pm.QuestGuildRank - 1);
                int minAllowedRank = Math.Max(1, targetRank - RankSystemHelper.MaxRankAbove);
                int maxAllowedRank = Math.Min(10, targetRank + RankSystemHelper.MaxRankBelow);

                if (monsterRank < minAllowedRank)
                {
                    pm.SendMessage(0x22, $"Этот враг ({monsterRank}-й ранг) слишком опасен для вашего текущего испытания (нужны враги {minAllowedRank}–{maxAllowedRank} рангов).");
                }
                else
                {
                    pm.SendMessage(0x22, $"Этот враг ({monsterRank}-й ранг) слишком слаб для вашего текущего испытания (нужны враги {minAllowedRank}–{maxAllowedRank} рангов).");
                }
            }
        }
    }
}
