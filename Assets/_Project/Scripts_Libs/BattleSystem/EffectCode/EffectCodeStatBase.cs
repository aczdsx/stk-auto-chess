using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace CookApps.TeamBattle.BattleSystem
{
    [Flags]
    public enum EffectCodeInheritFlag : ulong
    {
        None = 0L,
        StatHP = 1L << 0,
        StatAD = 1L << 1,
        StatDEF = 1L << 2,
        StatAP = 1L << 3,
        StatRES = 1L << 4,
        StatRecoveryHP = 1L << 5,
        StatMoveSpeed = 1L << 6,
        StatCriticalProb = 1L << 7,
        StatCriticalDamageRate = 1L << 8,
        StatDoubleCriticalProb = 1L << 9,
        StatDoubleCriticalDamageRate = 1L << 10,
        StatAttackSpeed = 1L << 11,
        StatAttackRange = 1L << 12,
        StatSkillDamageRate = 1L << 13,
        StatSkillCooltimeRate = 1L << 14,
        StatAttackDamageRate = 1L << 15,
        StatTakenDamageRate = 1L << 16,
        StatGivenHealRate = 1L << 17,
        StatTakenHealRate = 1L << 18,
        StatCrowdControlImmune = 1L << 19,

        UseOnUpdate = 1L << 40,
        UseOnAttack = 1L << 41,
        UseOnCooltime = 1L << 42,
        UseOnKill = 1L << 43,
        UseOnHealed = 1L << 44,
        UseOnDamaged = 1L << 45,
        UseOnCritical = 1L << 46,
        UseIsReadyToActivate = 1L << 47,
        UseIsUseNormalAttack = 1L << 48,
        UseOnDead = 1L << 49,
        UseModifyDamageAmount = 1L << 50,
        UseModifyHealAmount = 1L << 51,
        UseModifyShieldAmount = 1L << 52,
        UseOnSkill = 1L << 53,
        UseOnCombatStart = 1L << 54,
    };

    public static class EffectCodeInheritFlagExtensions
    {
        private static Dictionary<Type, EffectCodeInheritFlag> flagDict = new ();
        private static EffectCodeInheritFlag[] allFlagTypes;
        private static EffectCodeInheritFlag allFlags = EffectCodeInheritFlag.None;

        public static EffectCodeInheritFlag GetFlag(this EffectCodeStatBase src)
        {
            if (flagDict.TryGetValue(src.GetType(), out EffectCodeInheritFlag flag))
            {
                return flag;
            }

            flag = EffectCodeInheritFlag.None;
            Type type = src.GetType();
            MethodInfo[] methods = type.GetMethods();
            foreach (MethodInfo method in methods)
            {
                if (method.DeclaringType != method.GetBaseDefinition().DeclaringType)
                {
                    object[] attrs = method.GetCustomAttributes(typeof(AssignEffectCodeFlagAttribute), true);
                    if (attrs.Length <= 0)
                    {
                        continue;
                    }

                    var attr = attrs[0] as AssignEffectCodeFlagAttribute;
                    flag |= attr?.Flag ?? EffectCodeInheritFlag.None;
                }
            }

            flagDict.Add(src.GetType(), flag);
            return flag;
        }

        public static bool IsIncludeFlag(this EffectCodeInheritFlag src, EffectCodeInheritFlag flag)
        {
            return (src & flag) == flag;
        }

        public static void AddFlag(this ref EffectCodeInheritFlag src, EffectCodeInheritFlag flag)
        {
            src |= flag;
        }

        public static void RemoveFlag(this ref EffectCodeInheritFlag src, EffectCodeInheritFlag flag)
        {
            src &= ~flag;
        }

        public static IEnumerable<EffectCodeInheritFlag> GetUniqueFlags(this EffectCodeInheritFlag src)
        {
            allFlagTypes ??= Enum.GetValues(typeof(EffectCodeInheritFlag)).Cast<EffectCodeInheritFlag>().ToArray();
            foreach (EffectCodeInheritFlag flagType in allFlagTypes)
            {
                if (src.IsIncludeFlag(flagType))
                {
                    yield return flagType;
                }
            }
        }

        public static EffectCodeInheritFlag AllFlags()
        {
            if (allFlags != EffectCodeInheritFlag.None)
            {
                return allFlags;
            }

            allFlagTypes ??= Enum.GetValues(typeof(EffectCodeInheritFlag)).Cast<EffectCodeInheritFlag>().ToArray();
            foreach (EffectCodeInheritFlag flagType in allFlagTypes)
            {
                allFlags.AddFlag(flagType);
            }

            return allFlags;
        }
    }

    public class AssignEffectCodeFlagAttribute : Attribute
    {
        public EffectCodeInheritFlag Flag { get; private set; }

        public AssignEffectCodeFlagAttribute(EffectCodeInheritFlag flag)
        {
            Flag = flag;
        }
    }

    public abstract class EffectCodeStatBase : EffectCodeBase
    {
        public static List<int> UseCodeIds = new ();
        public override EffectCodeType Type => EffectCodeType.Stat;

        public override EffectCodeLifeType LifeType => EffectCodeLifeType.Permanent;
        public virtual int CalcOrder => 0;

        [AssignEffectCodeFlag(EffectCodeInheritFlag.StatHP)]
        public virtual double GetIncrementFixedHP()
        {
            return 0;
        }

        [AssignEffectCodeFlag(EffectCodeInheritFlag.StatHP)]
        public virtual double GetIncrementPercentHP()
        {
            return 0d;
        }

        [AssignEffectCodeFlag(EffectCodeInheritFlag.StatAD)]
        public virtual double GetIncrementFixedAD()
        {
            return 0;
        }

        [AssignEffectCodeFlag(EffectCodeInheritFlag.StatAD)]
        public virtual double GetIncrementPercentAD()
        {
            return 0d;
        }

        [AssignEffectCodeFlag(EffectCodeInheritFlag.StatRecoveryHP)]
        public virtual double GetIncrementFixedRecoveryHP()
        {
            return 0;
        }

        [AssignEffectCodeFlag(EffectCodeInheritFlag.StatRecoveryHP)]
        public virtual double GetIncrementPercentRecoveryHP()
        {
            return 0d;
        }

        [AssignEffectCodeFlag(EffectCodeInheritFlag.StatMoveSpeed)]
        public virtual float GetIncrementFixedMoveSpeed()
        {
            return 0f;
        }

        [AssignEffectCodeFlag(EffectCodeInheritFlag.StatMoveSpeed)]
        public virtual float GetIncrementPercentMoveSpeed()
        {
            return 0f;
        }

        [AssignEffectCodeFlag(EffectCodeInheritFlag.StatCriticalProb)]
        public virtual float GetIncrementFixedCriticalProbRate()
        {
            return 0f;
        }

        [AssignEffectCodeFlag(EffectCodeInheritFlag.StatCriticalProb)]
        public virtual float GetIncrementPercentCriticalProbRate()
        {
            return 0f;
        }

        [AssignEffectCodeFlag(EffectCodeInheritFlag.StatCriticalDamageRate)]
        public virtual float GetIncrementFixedCriticalDamageRate()
        {
            return 0f;
        }

        [AssignEffectCodeFlag(EffectCodeInheritFlag.StatCriticalDamageRate)]
        public virtual float GetIncrementPercentCriticalDamageRate()
        {
            return 0f;
        }

        [AssignEffectCodeFlag(EffectCodeInheritFlag.StatDoubleCriticalProb)]
        public virtual float GetIncrementFixedDoubleCriticalProbRate()
        {
            return 0f;
        }

        [AssignEffectCodeFlag(EffectCodeInheritFlag.StatDoubleCriticalProb)]
        public virtual float GetIncrementPercentDoubleCriticalProbRate()
        {
            return 0f;
        }

        [AssignEffectCodeFlag(EffectCodeInheritFlag.StatDoubleCriticalDamageRate)]
        public virtual float GetIncrementFixedDoubleCriticalDamageRate()
        {
            return 0f;
        }

        [AssignEffectCodeFlag(EffectCodeInheritFlag.StatDoubleCriticalDamageRate)]
        public virtual float GetIncrementPercentDoubleCriticalDamageRate()
        {
            return 0f;
        }

        [AssignEffectCodeFlag(EffectCodeInheritFlag.StatAttackSpeed)]
        public virtual float GetIncrementFixedAtkSpeed()
        {
            return 0;
        }

        [AssignEffectCodeFlag(EffectCodeInheritFlag.StatAttackSpeed)]
        public virtual float GetIncrementPercentAtkSpeed()
        {
            return 0f;
        }

        [AssignEffectCodeFlag(EffectCodeInheritFlag.StatAttackRange)]
        public virtual float GetIncrementFixedAtkRange()
        {
            return 0;
        }

        [AssignEffectCodeFlag(EffectCodeInheritFlag.StatAttackRange)]
        public virtual float GetIncrementPercentAtkRange()
        {
            return 0f;
        }

        [AssignEffectCodeFlag(EffectCodeInheritFlag.StatSkillDamageRate)]
        public virtual float GetSkillDamageRate()
        {
            return 0f;
        }

        [AssignEffectCodeFlag(EffectCodeInheritFlag.StatSkillCooltimeRate)]
        public virtual float GetSkillCooltimeRate()
        {
            return 0f;
        }

        [AssignEffectCodeFlag(EffectCodeInheritFlag.StatAttackDamageRate)]
        public virtual float GetAttackDamageRate()
        {
            return 0f;
        }

        [AssignEffectCodeFlag(EffectCodeInheritFlag.StatTakenDamageRate)]
        public virtual float GetTakenDamageRate()
        {
            return 0f;
        }

        [AssignEffectCodeFlag(EffectCodeInheritFlag.StatGivenHealRate)]
        public virtual float GetGivenHealRate()
        {
            return 0f;
        }

        [AssignEffectCodeFlag(EffectCodeInheritFlag.StatTakenHealRate)]
        public virtual float GetTakenHealRate()
        {
            return 0f;
        }

        [AssignEffectCodeFlag(EffectCodeInheritFlag.StatCrowdControlImmune)]
        public virtual CrowdControlType GetCrowdControlImmune()
        {
            return CrowdControlType.None;
        }
    }

    public static class EffectCodeStatListExtension
    {
        private const int maxCalcOrder = 10;
        private static double[] fixedValues = new double[maxCalcOrder];
        private static double[] percentValues = new double[maxCalcOrder];

        public static double CalculateHP<T>(this IEnumerable<T> list, double basicStat) where T : EffectCodeStatBase
        {
            for (var i = 0; i < maxCalcOrder; i++)
            {
                fixedValues[i] = 0;
                percentValues[i] = 0f;
            }

            foreach (T code in list)
            {
                fixedValues[code.CalcOrder] += code.GetIncrementFixedHP();
                percentValues[code.CalcOrder] += code.GetIncrementPercentHP();
            }

            for (var i = 0; i < maxCalcOrder; i++)
            {
                basicStat = (basicStat + fixedValues[i]) * (1f + percentValues[i]);
            }

            if (basicStat < 0)
            {
                basicStat = 0;
            }

            return basicStat;
        }

        public static double CalculateAD<T>(this List<T> list, double basicStat) where T : EffectCodeStatBase
        {
            if (list.Count == 0)
            {
                return basicStat;
            }

            for (var i = 0; i < maxCalcOrder; i++)
            {
                fixedValues[i] = 0;
                percentValues[i] = 0f;
            }

            for (var i = 0; i < list.Count; i++)
            {
                T x = list[i];

                fixedValues[x.CalcOrder] += x.GetIncrementFixedAD();
                percentValues[x.CalcOrder] += x.GetIncrementPercentAD();
            }

            for (var i = 0; i < maxCalcOrder; i++)
            {
                basicStat = (basicStat + fixedValues[i]) * (1f + percentValues[i]);
            }

            if (basicStat < 0)
            {
                basicStat = 0;
            }

            return basicStat;
        }

        public static double CalculateRecoveryHP<T>(this List<T> list, double basicStat) where T : EffectCodeStatBase
        {
            if (list.Count == 0)
            {
                return basicStat;
            }

            for (var i = 0; i < maxCalcOrder; i++)
            {
                fixedValues[i] = 0;
                percentValues[i] = 0f;
            }

            for (var i = 0; i < list.Count; i++)
            {
                T x = list[i];

                fixedValues[x.CalcOrder] += x.GetIncrementFixedRecoveryHP();
                percentValues[x.CalcOrder] += x.GetIncrementPercentRecoveryHP();
            }

            for (var i = 0; i < maxCalcOrder; i++)
            {
                basicStat = (basicStat + fixedValues[i]) * (1f + percentValues[i]);
            }

            if (basicStat < 0)
            {
                basicStat = 0;
            }

            return basicStat;
        }

        public static float CalculateMoveSpeed<T>(this List<T> list, float basicStat) where T : EffectCodeStatBase
        {
            if (list.Count == 0)
            {
                return basicStat;
            }

            float fixedValue = 0;
            float percentValue = 0;
            for (var i = 0; i < list.Count; i++)
            {
                T x = list[i];
                fixedValue += x.GetIncrementFixedMoveSpeed();
                percentValue += x.GetIncrementPercentMoveSpeed();
            }

            return Mathf.Max(0, (basicStat + fixedValue) * (1f + percentValue));
        }

        public static float CalculateCriticalProb<T>(this List<T> list, float basicStat) where T : EffectCodeStatBase
        {
            if (list.Count == 0)
            {
                return basicStat;
            }

            float fixedValue = 0;
            float percentValue = 0;
            for (var i = 0; i < list.Count; i++)
            {
                T x = list[i];
                fixedValue += x.GetIncrementFixedCriticalProbRate();
                percentValue += x.GetIncrementPercentCriticalProbRate();
            }

            return Mathf.Max(0, (basicStat + fixedValue) * (1f + percentValue));
        }

        public static float CalculateCriticalDamageRate<T>(this List<T> list, float basicStat) where T : EffectCodeStatBase
        {
            if (list.Count == 0)
            {
                return basicStat;
            }

            float fixedValue = 0;
            float percentValue = 0;
            for (var i = 0; i < list.Count; i++)
            {
                T x = list[i];
                fixedValue += x.GetIncrementFixedCriticalDamageRate();
                percentValue += x.GetIncrementPercentCriticalDamageRate();
            }

            return Mathf.Max(0, (basicStat + fixedValue) * (1f + percentValue));
        }

        public static float CalculateDoubleCriticalProb<T>(this List<T> list, float basicStat) where T : EffectCodeStatBase
        {
            if (list.Count == 0)
            {
                return basicStat;
            }

            float fixedValue = 0;
            float percentValue = 0;
            for (var i = 0; i < list.Count; i++)
            {
                T x = list[i];
                fixedValue += x.GetIncrementFixedDoubleCriticalProbRate();
                percentValue += x.GetIncrementPercentDoubleCriticalProbRate();
            }

            return Mathf.Max(0, (basicStat + fixedValue) * (1f + percentValue));
        }

        public static float CalculateDoubleCriticalDamageRate<T>(this List<T> list, float basicStat) where T : EffectCodeStatBase
        {
            if (list.Count == 0)
            {
                return basicStat;
            }

            float fixedValue = 0;
            float percentValue = 0;
            for (var i = 0; i < list.Count; i++)
            {
                T x = list[i];
                fixedValue += x.GetIncrementFixedDoubleCriticalDamageRate();
                percentValue += x.GetIncrementPercentDoubleCriticalDamageRate();
            }

            return Mathf.Max(0, (basicStat + fixedValue) * (1f + percentValue));
        }

        public static float CalculateAttackSpeed<T>(this List<T> list, float basicStat) where T : EffectCodeStatBase
        {
            if (list.Count == 0)
            {
                return basicStat;
            }

            float fixedValue = 0;
            float percentValue = 0;
            for (var i = 0; i < list.Count; i++)
            {
                T x = list[i];
                fixedValue += x.GetIncrementFixedAtkSpeed();
                percentValue += x.GetIncrementPercentAtkSpeed();
            }

            return Mathf.Max(0, Mathf.Max(0f, (basicStat + fixedValue) * (1f + percentValue)));
        }

        public static float CalculateAttackRange<T>(this List<T> list, float basicStat) where T : EffectCodeStatBase
        {
            if (list.Count == 0)
            {
                return basicStat;
            }

            float fixedValue = 0;
            float percentValue = 0;
            for (var i = 0; i < list.Count; i++)
            {
                T x = list[i];
                fixedValue += x.GetIncrementFixedAtkRange();
                percentValue += x.GetIncrementPercentAtkRange();
            }

            return Mathf.Max(0, (basicStat + fixedValue) * (1f + percentValue));
        }

        public static float CalculateSkillDamageRate<T>(this List<T> list, float basicStat) where T : EffectCodeStatBase
        {
            if (list.Count == 0)
            {
                return basicStat;
            }

            float fixedValue = 0;
            for (var i = 0; i < list.Count; i++)
            {
                fixedValue += list[i].GetSkillDamageRate();
            }

            return Mathf.Max(0, basicStat + fixedValue);
        }

        public static float CalculateSkillCooltimeRate<T>(this List<T> list, float basicStat) where T : EffectCodeStatBase
        {
            if (list.Count == 0)
            {
                return basicStat;
            }

            float fixedValue = 0;
            for (var i = 0; i < list.Count; i++)
            {
                fixedValue += list[i].GetSkillCooltimeRate();
            }

            return Mathf.Max(0, basicStat + fixedValue);
        }

        public static float CalculateAttackDamageRate<T>(this List<T> list, float basicStat) where T : EffectCodeStatBase
        {
            if (list.Count == 0)
            {
                return basicStat;
            }

            float fixedValue = 0;
            for (var i = 0; i < list.Count; i++)
            {
                fixedValue += list[i].GetAttackDamageRate();
            }

            return Mathf.Max(0, basicStat + fixedValue);
        }

        public static float CalculateTakenDamageRate<T>(this List<T> list, float basicStat) where T : EffectCodeStatBase
        {
            if (list.Count == 0)
            {
                return basicStat;
            }

            float fixedValue = 0;
            for (var i = 0; i < list.Count; i++)
            {
                fixedValue += list[i].GetTakenDamageRate();
            }

            return Mathf.Max(0, basicStat + fixedValue);
        }

        public static float CalculateGivenHealRate<T>(this List<T> list, float basicStat) where T : EffectCodeStatBase
        {
            if (list.Count == 0)
            {
                return basicStat;
            }

            float fixedValue = 0;
            for (var i = 0; i < list.Count; i++)
            {
                fixedValue += list[i].GetGivenHealRate();
            }

            return Mathf.Max(0, basicStat + fixedValue);
        }

        public static float CalculateTakenHealRate<T>(this List<T> list, float basicStat) where T : EffectCodeStatBase
        {
            if (list.Count == 0)
            {
                return basicStat;
            }

            float fixedValue = 0;
            for (var i = 0; i < list.Count; i++)
            {
                fixedValue += list[i].GetTakenHealRate();
            }

            return Mathf.Max(0, basicStat + fixedValue);
        }

        public static CrowdControlType CalculateCrowdControlImmune<T>(this List<T> list) where T : EffectCodeStatBase
        {
            if (list.Count == 0)
            {
                return CrowdControlType.None;
            }

            var fixedValue = CrowdControlType.None;
            for (var i = 0; i < list.Count; i++)
            {
                fixedValue = fixedValue | list[i].GetCrowdControlImmune();
            }

            return fixedValue;
        }
    }
}
