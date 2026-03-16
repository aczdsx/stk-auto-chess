using System;
using UnityEngine;

namespace CookApps.AutoChess
{
    /// <summary>
    /// 전투력(CP) 계산기. 구 프레임워크(CharacterStatData) 공식을 정수 스탯 기반으로 포팅.
    /// </summary>
    public static class CombatPowerCalculator
    {
        /// <summary>
        /// 프리컴뱃/UI용 CP 계산. UnitData 자체 스탯 사용.
        /// </summary>
        public static int Calculate(ref UnitData unit)
        {
            int critChance = unit.CritRate > 0 ? unit.CritRate : 25;
            int critMultiplier = unit.CritPower > 0 ? unit.CritPower : 150;

            return CalcInternal(
                unit.Attack, unit.AttackSpeed, unit.MaxHP,
                unit.Def, unit.AdReduce, unit.ApReduce,
                critChance, critMultiplier, unit.AtkPierce);
        }

        /// <summary>
        /// 인컴뱃용 CP 계산. CombatUnit에 모든 스탯 포함.
        /// </summary>
        public static int Calculate(ref CombatUnit cu)
        {
            return CalcInternal(
                cu.Attack, cu.AttackSpeed, cu.MaxHP,
                cu.Def, cu.AdReduce, cu.ApReduce,
                cu.CritRate, cu.CritPower, cu.AtkPierce);
        }

        /// <summary>
        /// PvE 적 유닛 CP 계산. PvEEnemyData 자체 스탯 사용.
        /// </summary>
        public static int Calculate(ref PvEEnemyData enemy)
        {
            int critChance = enemy.CritRate > 0 ? enemy.CritRate : 25;
            int critMultiplier = enemy.CritPower > 0 ? enemy.CritPower : 150;

            return CalcInternal(
                enemy.Attack, enemy.AttackSpeed, enemy.MaxHP,
                enemy.Def, enemy.AdReduce, enemy.ApReduce,
                critChance, critMultiplier, enemy.AtkPierce);
        }

        /// <summary>
        /// 구 스펙(ISpecCharacterInfo) + 별 레벨로 CP 계산. 벤치 슬롯 UI용.
        /// </summary>
        public static int CalculateFromOldSpec(int hp, int atk, int def, int adReduce, int apReduce,
            int attackSpeed, int critRate, int critPower, int atkPierce)
        {
            return CalcInternal(atk, attackSpeed, hp, def, adReduce, apReduce,
                critRate, critPower, atkPierce);
        }

        private static int CalcInternal(
            int attack, int attackSpeed, int maxHP,
            int def, int adReduce, int apReduce,
            int critRate, int critPower, int atkPierce)
        {
            // OP (공격력 지수)
            float atkSpeedF = attackSpeed / 100f;
            float critPowerF = critPower / 100f;
            float critRateF = critRate / 100f;
            float effectiveCritMul = 1f + (critPowerF - 1f) * critRateF;
            float pierceMul = 1f + 0.6f * Mathf.Clamp(atkPierce / 100f, 0f, 0.7f);
            float op = attack * atkSpeedF * effectiveCritMul * pierceMul;

            // DP (방어력 지수)
            // DEF(def) 최종 감산 + 물리/마법 저항 평균
            float defMul = 100f / (100f + def);
            float avgResist = Mathf.Clamp((adReduce + apReduce) / 200f, 0f, 0.7f);
            float resistMul = 1f - avgResist;
            float takenMul = defMul * resistMul;
            float dp = takenMul > 0f ? maxHP / takenMul : maxHP;

            // CP
            return (int)Math.Round(7.0 * Math.Sqrt(op) + 5.0 * Math.Sqrt(dp) + 1.0);
        }
    }
}
