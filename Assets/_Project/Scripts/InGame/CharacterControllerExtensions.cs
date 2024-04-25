using CookApps.BattleSystem;

namespace CookApps.SampleTeamBattle
{
    public static class BattleSystemExtensions
    {
        // TODO: Character Controller에 통합할 것
        public static void AddBuffDebuffTypeWrapped(this CharacterController characterController, BuffDebuffType type)
        {
            characterController.AddBuffDebuffType(type);
            IInGameEffectView effect = null;    // effect = InGameEffectManager.GetEffectViewFromPool(type);
            if (!characterController.AddBuffDebuffEffectView(type, effect))
            {
                // effect.ReturnToPool();
            }
        }

        // TODO: Character Controller에 통합할 것
        public static void AddCrowdControlWrapped(this CharacterController characterController, CrowdControlType type)
        {
            characterController.AddCrowdControl(type);
            characterController.AddBuffDebuffTypeWrapped(type.ToBuffDebuffType());
        }

        // TODO: Character Controller에 통합할 것
        public static void RemoveBuffDebuffTypeWrapped(this CharacterController characterController, BuffDebuffType type)
        {
            var (currCount, effectView) = characterController.RemoveBuffDebuffType(type);
            if (currCount == 0)
            {
                // effectView.ReturnToPool();
            }
        }

        // TODO: Character Controller에 통합할 것
        public static void RemoveCrowdControlWrapped(this CharacterController characterController, CrowdControlType type)
        {
            characterController.RemoveCrowdControl(type);
            characterController.RemoveBuffDebuffTypeWrapped(type.ToBuffDebuffType());
        }

        public static BuffDebuffType ToBuffDebuffType(this CrowdControlType type)
        {
            switch (type)
            {
                case CrowdControlType.Entangle:
                    return BuffDebuffType.Entangle;
                case CrowdControlType.Airborne:
                case CrowdControlType.KnockBack:
                case CrowdControlType.Stun:
                    return BuffDebuffType.Stun;
                case CrowdControlType.Slowing:
                    return BuffDebuffType.Slow;
                case CrowdControlType.Provocation:
                    return BuffDebuffType.Provocation;
                case CrowdControlType.Freezing:
                    return BuffDebuffType.Freezing;
                default:
                    return BuffDebuffType.None;
            }
        }
    }
}
