using System;

namespace CookApps.TeamBattle.BattleSystem
{
    public interface IInGameCalculator
    {
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
