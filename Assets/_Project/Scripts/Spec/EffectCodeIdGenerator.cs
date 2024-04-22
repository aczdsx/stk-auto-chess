using CookApps.TeamBattle.BattleSystem;

namespace CookApps.SampleTeamBattle
{
    public enum StatCodeType
    {
        Character = 1,
        Equipment,
        Synergy,
    }

    public static class EffectCodeIdGenerator
    {
        public static int MakeStatCodeId(StatCodeType type, int targetId, CharacterEffect statType)
        {
            return (int)type * 100000000 + targetId * 1000 + (int)statType;
        }

        public static int MakeCrowdControlCodeId(CrowdControlType type)
        {
            return -10000 + (int)type;
        }
    }
}
