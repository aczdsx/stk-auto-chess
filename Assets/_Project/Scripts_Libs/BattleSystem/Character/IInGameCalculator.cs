using System;

namespace CookApps.TeamBattle.BattleSystem
{
    public interface IInGameCalculator
    {
        float CrowdControlSlowRate { get; }
        float EffectCodeUpdatePendingTime { get; }
        float EffectCodeCooltimePendingTime { get; }
        float MaxCooltime { get; }
        float RegenHPPendingTime { get; }
        float MinDamageRate { get; }

        double CalculateDefaultDamage(double ad, double ap, CharacterController attacker, CharacterController target);
        float CalculateCooltimeRate(float skillCooltimeRate);
    }

    public static class InGameCalculator
    {
        private static IInGameCalculator instance;

        public static IInGameCalculator Instance
        {
            get
            {
                if (instance == null)
                {
                    throw new NullReferenceException("CharacterView is not initialized yet.");
                }

                return instance;
            }
        }

        public static void Initialize(IInGameCalculator instance)
        {
            InGameCalculator.instance = instance;
        }
    }
}
