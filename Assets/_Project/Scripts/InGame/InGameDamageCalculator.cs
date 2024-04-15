using CookApps.TeamBattle.BattleSystem;
using UnityEngine;

namespace CookApps.SampleTeamBattle
{
    public class InGameDamageCalculator : IDamageCalculator
    {
        [RuntimeInitializeOnLoadMethod]
        private static void Initialize()
        {
            DamageCalculator.Initialize(new InGameDamageCalculator());
        }

        public double CalculateDefaultDamage(double ad, double ap, double def, double res, double defPenetration, double resPenetration)
        {
            double damage = ad;
            if (def > 0)
            {
                damage += ad * 50f / (50f + def);
            }
            else
            {
                damage += ad * (2f - (50f / (50f - def)));
            }

            if (res > 0)
            {
                damage += ap * 50f / (50f + res);
            }
            else
            {
                damage += ap * (2f - (50f / (50f - res)));
            }

            return damage;
        }
    }
}
