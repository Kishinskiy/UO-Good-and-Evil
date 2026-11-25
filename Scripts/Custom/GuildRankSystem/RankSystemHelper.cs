using System;
using Server.Mobiles;

namespace Server.Engines.Quests
{
    public static class RankSystemHelper
    {
        // Определяем ранг монстра (10 = слабый, 1 = босс, 0 = мирный/слишком слабый)
        public static int GetMonsterRank(BaseCreature bc)
        {
            if (bc == null || bc.Summoned || bc.Controlled)
                return 0;

            int negativeKarma = bc.Karma < 0 ? Math.Abs(bc.Karma) : 0;
            int totalThreat = bc.Fame + negativeKarma;

            if (totalThreat < 500)
                return 0;

            if (totalThreat >= 25000) return 1;  // Древние драконы, балроны
            if (totalThreat >= 18000) return 2;  // Драконы, личи-лорды
            if (totalThreat >= 13000) return 3;  // Демоны, титаны
            if (totalThreat >= 9000)  return 4;  // Элементали крови, циклопы
            if (totalThreat >= 6000)  return 5;  // Огры-лорды, виверны
            if (totalThreat >= 4000)  return 6;  // Личи, горгульи
            if (totalThreat >= 2500)  return 7;  // Эттины, тролли
            if (totalThreat >= 1500)  return 8;  // Орки-капитаны, гарпии
            if (totalThreat >= 900)   return 9;  // Зомби, скелеты-воины

            return 10; // Обычные скелеты, крысы, монбаты
        }

        // Базовые очки за убийство монстра определенного ранга
        public static int GetBasePointsByMonsterRank(int monsterRank)
        {
            switch (monsterRank)
            {
                case 10: return 10;   // 250 штук для 2500 очков
                case 9:  return 16;
                case 8:  return 25;
                case 7:  return 40;
                case 6:  return 65;
                case 5:  return 100;
                case 4:  return 150;
                case 3:  return 220;
                case 2:  return 320;
                case 1:  return 500;  // 40 штук для 20000 очков
                default: return 0;
            }
        }

        // Сколько очков нужно набрать для перехода на следующий ранг
        public static int GetRequiredPointsForRank(int currentPlayerRank)
        {
            switch (currentPlayerRank)
            {
                case 11: return 2500;  // Испытание на 10-й ранг
                case 10: return 3500;  // Испытание на 9-й ранг
                case 9:  return 4500;  // Испытание на 8-й ранг
                case 8:  return 6000;  // Испытание на 7-й ранг
                case 7:  return 7500;  // Испытание на 6-й ранг
                case 6:  return 9500;  // Испытание на 5-й ранг
                case 5:  return 11500; // Испытание на 4-й ранг
                case 4:  return 14000; // Испытание на 3-й ранг
                case 3:  return 17000; // Испытание на 2-й ранг
                case 2:  return 20000; // Испытание на 1-й ранг (высший)
                default: return 2500;
            }
        }

        // Итоговый расчет очков с учетом разницы рангов
        public static int CalculateEarnedPoints(int playerRank, int monsterRank)
        {
            if (monsterRank <= 0)
                return 0;

            int targetRank = Math.Max(1, playerRank - 1);

            // Защита от фарма слабых мобов (разница больше 2 рангов)
            if (monsterRank > targetRank + 2)
                return 0;

            int points = GetBasePointsByMonsterRank(monsterRank);

            // Штраф 50%, если монстр на 1-2 ранга слабее текущего испытания
            if (monsterRank > targetRank)
            {
                points = Math.Max(1, points / 2);
            }

            return points;
        }
    }
}
