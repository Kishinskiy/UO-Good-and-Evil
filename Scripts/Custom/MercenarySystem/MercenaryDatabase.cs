using System;
using Server;

namespace Server.Custom.MercenarySystem
{
    public enum MercenaryClass
    {
        Knight = 0,
        Mage = 1,
        Ranger = 2,
        Bard = 3,
        Thief = 4,
        Paladin = 5,
        Necromancer = 6,
        Samurai = 7
    }

    public static class MercenaryClassUtility
    {
        public static MercenaryClass FromSerializedValue(int value)
        {
            switch (value)
            {
                case 8: return MercenaryClass.Mage;
                case 9: return MercenaryClass.Knight;
                default:
                    if (Enum.IsDefined(typeof(MercenaryClass), value))
                        return (MercenaryClass)value;

                    throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown mercenary class value.");
            }
        }
    }

    public class MercenaryProgression
    {
        public int Level { get; set; } = 1;
        public int Experience { get; set; } = 0;
        public int NextLevelExperience { get; set; } = 500;

        public void CheckLevelUp(Mobile merc, MercenaryClass mClass)
        {
            while (Level < 10 && Experience >= NextLevelExperience)
            {
                Experience -= NextLevelExperience;
                Level++;
                NextLevelExperience = (int)(500 * Math.Pow(1.8, Level - 1));

                ApplyLevelBonuses(merc, mClass, Level);
                merc.Hits = merc.HitsMax;
                merc.Mana = merc.ManaMax;
                merc.Stam = merc.StamMax;

                merc.PlaySound(0x5B6); 
                merc.PublicOverheadMessage(Network.MessageType.Regular, 0x35, false, $"*[Уровень Повышен! Ранг: {GetRankTitle(Level)}]*");
            }
        }

        public void Restore(Mobile merc, MercenaryClass mClass, int level, int experience, int nextLevelExperience)
        {
            int restoredLevel = Math.Max(1, Math.Min(10, level));

            Level = 1;
            Experience = 0;
            NextLevelExperience = 500;

            while (Level < restoredLevel)
            {
                Level++;
                NextLevelExperience = (int)(500 * Math.Pow(1.8, Level - 1));
                ApplyLevelBonuses(merc, mClass, Level);
            }

            Experience = Math.Max(0, experience);
            NextLevelExperience = Math.Max(1, nextLevelExperience);

            merc.Hits = merc.HitsMax;
            merc.Mana = merc.ManaMax;
            merc.Stam = merc.StamMax;
        }

        private static void ApplyLevelBonuses(Mobile merc, MercenaryClass mClass, int level)
        {
            int hpBonus = mClass == MercenaryClass.Knight || mClass == MercenaryClass.Paladin ? 50 : 25;

            merc.AddStatMod(new StatMod(StatType.Str, "MercLevelUpHP_" + level, hpBonus * 2, TimeSpan.Zero));

            if (mClass == MercenaryClass.Knight || mClass == MercenaryClass.Samurai)
                merc.RawStr += 15;
            else if (mClass == MercenaryClass.Mage || mClass == MercenaryClass.Necromancer)
                merc.RawInt += 15;
            else if (mClass == MercenaryClass.Ranger || mClass == MercenaryClass.Thief)
                merc.RawDex += 15;
            else if (mClass == MercenaryClass.Paladin || mClass == MercenaryClass.Bard)
            {
                merc.RawStr += 8;
                merc.RawInt += 8;
            }

            foreach (Skill skill in merc.Skills)
            {
                if (skill.Base > 0)
                {
                    skill.Cap += 10.0;
                    skill.Base += 10.0;
                }
            }
        }

        public static string GetRankTitle(int level)
        {
            switch (level)
            {
                case 1: return "Рекрут (Apprentice)";
                case 2: return "Послушник (Novice)";
                case 3: return "Ополченец (Footman)";
                case 4: return "Воин (Warrior)";
                case 5: return "Ветеран (Veteran)";
                case 6: return "Гвардеец (Guardsman)";
                case 7: return "Рыцарь (Knight)";
                case 8: return "Командор (Commander)";
                case 9: return "Чемпион (Champion)";
                case 10: return "Легенда (Hero)";
                default: return "Наёмник";
            }
        }
    }
}
