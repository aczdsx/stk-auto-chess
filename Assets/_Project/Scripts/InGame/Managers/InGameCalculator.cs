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

        public static double CalculateDefaultDamage(double ad, double ap, CharacterController target, CharacterController attacker = null)
        {
            // 방관이나 마관에 따른 방어력 및 저항력 계산
            double def = attacker != null 
                ? target.DEF * (1f - attacker.DEFPenetration) 
                : target.DEF;
            
            double res = attacker != null 
                ? target.RES * (1f - attacker.RESPenetration) 
                : target.RES;

            double damage = 0;
            
            if (def >= 0)
                damage += ad * 50f / (50f + def);

            if (res >= 0)
                damage += ap * 50f / (50f + res);

            // Debug.LogColor($"[{target.SpecCharacter.prefab_id}] {damage} : {ad} : {ap} : {def} : {res}", "cyan");

            return damage;
        }

        public static float CalculateCooltimeRate(float skillCooltimeRate)
        {
            return 1f - (MaxCooltime * (1f - (1f / (1f + skillCooltimeRate))));
        }
    }
}
