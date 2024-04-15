using System;

namespace CookApps.TeamBattle.BattleSystem
{
    public interface IDamageCalculator
    {
        double CalculateDefaultDamage(double ad, double ap, double def, double res, double defPenetration, double resPenetration);
    }

    public static class DamageCalculator
    {
        private static IDamageCalculator instance;

        public static IDamageCalculator Instance
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

        public static void Initialize(IDamageCalculator instance)
        {
            DamageCalculator.instance = instance;
        }
    }
}
