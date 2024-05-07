using CookApps.BattleSystem;
using UnityEngine;
using CharacterController = CookApps.BattleSystem.CharacterController;

namespace CookApps.AutoBattler
{
    public static class InGameCalculator
    {
        public static float CrowdControlSlowRate => SpecOptionCache.CrowdControlSlowRate;

        public static float EffectCodeUpdatePendingTime => SpecOptionCache.EffectCodeUpdatePendingTime;

        public static float EffectCodeCooltimePendingTime => SpecOptionCache.EffectCodeCooltimePendingTime;

        public static float MaxCooltime => SpecOptionCache.MaxCooltime;

        public static float RegenHPPendingTime => SpecOptionCache.RegenHPPendingTime;

        public static float MinDamageRate => SpecOptionCache.MinDamageRate;

        public static double CalculateDefaultDamage(double ad, double ap, CharacterController attacker, CharacterController target)
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

        public static float CalculateCooltimeRate(float skillCooltimeRate)
        {
            return 1f - (MaxCooltime * (1f - (1f / (1f + skillCooltimeRate))));
        }
    }
}
