using CookApps.BattleSystem;

namespace CookApps.AutoBattler
{
    public static class BattleSystemExtensions
    {
        public static BuffDebuffType ToBuffDebuffType(this CrowdControlType type)
        {
            switch (type)
            {
                case CrowdControlType.Entangle:
                    return BuffDebuffType.Entangle;
                case CrowdControlType.Airborne:
                case CrowdControlType.Stun:
                    return BuffDebuffType.Stun;
                case CrowdControlType.Slowing:
                    return BuffDebuffType.Slow;
                case CrowdControlType.Provocation:
                    return BuffDebuffType.Provocation;
                case CrowdControlType.Freezing:
                    return BuffDebuffType.Freezing;
                case CrowdControlType.KnockBack:
                default:
                    return BuffDebuffType.None;
            }
        }
    }
}
