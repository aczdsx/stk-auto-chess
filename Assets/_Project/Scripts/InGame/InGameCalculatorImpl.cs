using CookApps.TeamBattle.BattleSystem;
using UnityEngine;
using CharacterController = CookApps.TeamBattle.BattleSystem.CharacterController;

namespace CookApps.SampleTeamBattle
{
    public class InGameCalculatorImpl : IInGameCalculator
    {
        [RuntimeInitializeOnLoadMethod]
        private static void Initialize()
        {
            InGameCalculator.Initialize(new InGameCalculatorImpl());
        }

        public double CalculateDefaultDamage(double ad, double ap, CharacterController attacker, CharacterController target)
        {
            double defPenetration = attacker.DEFPenetration;
            double resPenetration = attacker.RESPenetration;
            // 방관이나 마관에 따른 방어력 및 저항력 감소 계산
            double def = target.DEF * (1f - defPenetration);
            double res = target.RES * (1f - resPenetration);

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

        public float CalculateCooltimeRate(float skillCooltimeRate)
        {
            return 1f - (Const.Instance.MaxCooltime * (1f - (1f / (1f + skillCooltimeRate))));
        }
    }
}
