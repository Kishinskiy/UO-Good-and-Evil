namespace Server.Custom.CityHunting
{
    public class HuntTargetDef
    {
        public string TypeName { get; }
        public string DisplayName { get; }
        public int MinKills { get; }
        public int MaxKills { get; }
        public int BaseGoldReward { get; }
        public bool IsBoss { get; }

        public HuntTargetDef(string typeName, string displayName, int minKills, int maxKills, int gold, bool isBoss = false)
        {
            TypeName = typeName;
            DisplayName = displayName;
            MinKills = minKills;
            MaxKills = maxKills;
            BaseGoldReward = gold;
            IsBoss = isBoss;
        }
    }
}
