using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace CookApps.BattleSystem
{
    [Flags]
    public enum EffectCodeInheritFlag : ulong
    {
        None = 0L,

        #region Stat
        StatHP = 1L << 0,
        StatAD = 1L << 1,
        StatDEF = 1L << 2,
        StatDEFPenetration = 1L << 3,
        StatAP = 1L << 4,
        StatRES = 1L << 5,
        StatRESPenetration = 1L << 6,
        StatRecoveryHP = 1L << 7,
        StatMoveSpeed = 1L << 8,
        StatCriticalProb = 1L << 9,
        StatCriticalDamageRate = 1L << 10,
        StatDoubleCriticalProb = 1L << 11,
        StatDoubleCriticalDamageRate = 1L << 12,
        StatAttackSpeed = 1L << 13,
        StatAttackRange = 1L << 14,
        StatAttackRangeShape = 1L << 15,
        StatSkillDamageRate = 1L << 16,
        StatSkillCooltimeRate = 1L << 17,
        StatAttackDamageRate = 1L << 18,
        StatTakenDamageRate = 1L << 19,
        StatGivenHealRate = 1L << 20,
        StatTakenHealRate = 1L << 21,
        StatCrowdControlImmune = 1L << 22,
        StatAll = ~(0xffffffffffffff << 22),
        #endregion

        #region Event
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
        #endregion
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

        public static void AddFlag(this ref EffectCodeInheritFlag src, EffectCodeInheritFlag flag)
        {
            src |= flag;
        }

        public static void RemoveFlag(this ref EffectCodeInheritFlag src, EffectCodeInheritFlag flag)
        {
            src &= ~flag;
        }

        public static IReadOnlyList<EffectCodeInheritFlag> GetAllFlagTypes()
        {
            return allFlagTypes ??= Enum.GetValues(typeof(EffectCodeInheritFlag)).Cast<EffectCodeInheritFlag>().ToArray();
        }

        public static IEnumerable<EffectCodeInheritFlag> GetUniqueFlags(this EffectCodeInheritFlag src)
        {
            IReadOnlyList<EffectCodeInheritFlag> allFlagTypes = GetAllFlagTypes();
            foreach (EffectCodeInheritFlag flagType in allFlagTypes)
            {
                if (src.HasFlag(flagType))
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
        public override EffectCodeType Type => EffectCodeType.Stat;
        public virtual int CalcOrder => 0;

        /// <summary>
        /// +일 경우 체력 고정 증가, -일 경우 체력 고정 감소
        /// </summary>
        /// <returns></returns>
        [AssignEffectCodeFlag(EffectCodeInheritFlag.StatHP)]
        public virtual double GetIncrementFixedHP()
        {
            return 0;
        }

        /// <summary>
        /// +일 경우 체력 퍼센트 증가, -일 경우 체력 퍼센트 감소
        /// </summary>
        /// <returns></returns>
        [AssignEffectCodeFlag(EffectCodeInheritFlag.StatHP)]
        public virtual double GetIncrementPercentHP()
        {
            return 0d;
        }

        /// <summary>
        /// +일 경우 공격력 퍼센트 증가, -일 경우 공격력 퍼센트 감소
        /// </summary>
        /// <returns></returns>
        [AssignEffectCodeFlag(EffectCodeInheritFlag.StatAD)]
        public virtual double GetIncrementFixedAD()
        {
            return 0;
        }

        /// <summary>
        /// +일 경우 공격력 퍼센트 증가, -일 경우 공격력 퍼센트 감소
        /// </summary>
        /// <returns></returns>
        [AssignEffectCodeFlag(EffectCodeInheritFlag.StatAD)]
        public virtual double GetIncrementPercentAD()
        {
            return 0d;
        }

        /// <summary>
        /// +일 경우 방어력 고정 증가, -일 경우 방어력 고정 감소
        /// </summary>
        /// <returns></returns>
        [AssignEffectCodeFlag(EffectCodeInheritFlag.StatDEF)]
        public virtual double GetIncrementFixedDEF()
        {
            return 0;
        }

        /// <summary>
        /// +일 경우 방어력 퍼센트 증가, -일 경우 방어력 퍼센트 감소
        /// </summary>
        /// <returns></returns>
        [AssignEffectCodeFlag(EffectCodeInheritFlag.StatDEF)]
        public virtual double GetIncrementPercentDEF()
        {
            return 0d;
        }

        /// <summary>
        /// +일 경우 방어관통력 고정 증가, -일 경우 방어관통력 고정 감소
        /// </summary>
        /// <returns></returns>
        [AssignEffectCodeFlag(EffectCodeInheritFlag.StatDEFPenetration)]
        public virtual double GetIncrementFixedDEFPenetration()
        {
            return 0;
        }

        /// <summary>
        /// +일 경우 방어관통력 퍼센트 증가, -일 경우 방어관통력 퍼센트 감소
        /// </summary>
        /// <returns></returns>
        [AssignEffectCodeFlag(EffectCodeInheritFlag.StatDEFPenetration)]
        public virtual double GetIncrementPercentDEFPenetration()
        {
            return 0d;
        }

        /// <summary>
        /// +일 경우 주문력 고정 증가, -일 경우 주문력 고정 감소
        /// </summary>
        /// <returns></returns>
        [AssignEffectCodeFlag(EffectCodeInheritFlag.StatAP)]
        public virtual double GetIncrementFixedAP()
        {
            return 0;
        }

        /// <summary>
        /// +일 경우 주문력 퍼센트 증가, -일 경우 주문력 퍼센트 감소
        /// </summary>
        /// <returns></returns>
        [AssignEffectCodeFlag(EffectCodeInheritFlag.StatAP)]
        public virtual double GetIncrementPercentAP()
        {
            return 0d;
        }


        /// <summary>
        /// +일 경우 저항력 고정 증가, -일 경우 저항력 고정 감소
        /// </summary>
        /// <returns></returns>
        [AssignEffectCodeFlag(EffectCodeInheritFlag.StatRES)]
        public virtual double GetIncrementFixedRES()
        {
            return 0;
        }

        /// <summary>
        /// +일 경우 저항력 퍼센트 증가, -일 경우 저항력 퍼센트 감소
        /// </summary>
        /// <returns></returns>
        [AssignEffectCodeFlag(EffectCodeInheritFlag.StatRES)]
        public virtual double GetIncrementPercentRES()
        {
            return 0d;
        }

        /// <summary>
        /// +일 경우 저항관통력 고정 증가, -일 경우 저항관통력 고정 감소
        /// </summary>
        /// <returns></returns>
        [AssignEffectCodeFlag(EffectCodeInheritFlag.StatRESPenetration)]
        public virtual double GetIncrementFixedRESPenetration()
        {
            return 0;
        }

        /// <summary>
        /// +일 경우 저항관통력 퍼센트 증가, -일 경우 저항관통력 퍼센트 감소
        /// </summary>
        /// <returns></returns>
        [AssignEffectCodeFlag(EffectCodeInheritFlag.StatRESPenetration)]
        public virtual double GetIncrementPercentRESPenetration()
        {
            return 0d;
        }

        /// <summary>
        /// +일 경우 회복력 고정 증가, -일 경우 회복력 고정 감소
        /// </summary>
        /// <returns></returns>
        [AssignEffectCodeFlag(EffectCodeInheritFlag.StatRecoveryHP)]
        public virtual double GetIncrementFixedRecoveryHP()
        {
            return 0;
        }

        /// <summary>
        /// +일 경우 회복력 퍼센트 증가, -일 경우 회복력 퍼센트 감소
        /// </summary>
        /// <returns></returns>
        [AssignEffectCodeFlag(EffectCodeInheritFlag.StatRecoveryHP)]
        public virtual double GetIncrementPercentRecoveryHP()
        {
            return 0d;
        }

        /// <summary>
        /// +일 경우 이동속도 고정 증가, -일 경우 이동속도 고정 감소
        /// </summary>
        /// <returns></returns>
        [AssignEffectCodeFlag(EffectCodeInheritFlag.StatMoveSpeed)]
        public virtual float GetIncrementFixedMoveSpeed()
        {
            return 0f;
        }

        /// <summary>
        /// +일 경우 이동속도 퍼센트 증가, -일 경우 이동속도 퍼센트 감소
        /// </summary>
        /// <returns></returns>
        [AssignEffectCodeFlag(EffectCodeInheritFlag.StatMoveSpeed)]
        public virtual float GetIncrementPercentMoveSpeed()
        {
            return 0f;
        }

        /// <summary>
        /// +일 경우 크리티컬 확률 고정 증가, -일 경우 크리티컬 확률 고정 감소
        /// </summary>
        /// <returns></returns>
        [AssignEffectCodeFlag(EffectCodeInheritFlag.StatCriticalProb)]
        public virtual float GetIncrementFixedCriticalProbRate()
        {
            return 0f;
        }

        /// <summary>
        /// +일 경우 크리티컬 확률 퍼센트 증가, -일 경우 크리티컬 확률 퍼센트 감소
        /// </summary>
        /// <returns></returns>
        [AssignEffectCodeFlag(EffectCodeInheritFlag.StatCriticalProb)]
        public virtual float GetIncrementPercentCriticalProbRate()
        {
            return 0f;
        }

        /// <summary>
        /// +일 경우 크리티컬 대미지 고정 증가, -일 경우 크리티컬 대미지 고정 감소
        /// </summary>
        /// <returns></returns>
        [AssignEffectCodeFlag(EffectCodeInheritFlag.StatCriticalDamageRate)]
        public virtual float GetIncrementFixedCriticalDamageRate()
        {
            return 0f;
        }

        /// <summary>
        /// +일 경우 크리티컬 대미지 퍼센트 증가, -일 경우 크리티컬 대미지 퍼센트 감소
        /// </summary>
        /// <returns></returns>
        [AssignEffectCodeFlag(EffectCodeInheritFlag.StatCriticalDamageRate)]
        public virtual float GetIncrementPercentCriticalDamageRate()
        {
            return 0f;
        }

        /// <summary>
        /// +일 경우 더블 크리티컬 확률 고정 증가, -일 경우 더블 크리티컬 확률 고정 감소
        /// </summary>
        /// <returns></returns>
        [AssignEffectCodeFlag(EffectCodeInheritFlag.StatDoubleCriticalProb)]
        public virtual float GetIncrementFixedDoubleCriticalProbRate()
        {
            return 0f;
        }

        /// <summary>
        /// +일 경우 더블 크리티컬 확률 퍼센트 증가, -일 경우 더블 크리티컬 확률 퍼센트 감소
        /// </summary>
        /// <returns></returns>
        [AssignEffectCodeFlag(EffectCodeInheritFlag.StatDoubleCriticalProb)]
        public virtual float GetIncrementPercentDoubleCriticalProbRate()
        {
            return 0f;
        }

        /// <summary>
        /// +일 경우 더블 크리티컬 대미지 배율 고정 증가, -일 경우 더블 크리티컬 대미지 배율 고정 감소
        /// </summary>
        /// <returns></returns>
        [AssignEffectCodeFlag(EffectCodeInheritFlag.StatDoubleCriticalDamageRate)]
        public virtual float GetIncrementFixedDoubleCriticalDamageRate()
        {
            return 0f;
        }

        /// <summary>
        /// +일 경우 더블 크리티컬 대미지 배율 퍼센트 증가, -일 경우 더블 크리티컬 대미지 배율 퍼센트 감소
        /// </summary>
        /// <returns></returns>
        [AssignEffectCodeFlag(EffectCodeInheritFlag.StatDoubleCriticalDamageRate)]
        public virtual float GetIncrementPercentDoubleCriticalDamageRate()
        {
            return 0f;
        }

        /// <summary>
        /// +일 경우 공격속도 고정 증가, -일 경우 공격속도 고정 감소
        /// </summary>
        /// <returns></returns>
        [AssignEffectCodeFlag(EffectCodeInheritFlag.StatAttackSpeed)]
        public virtual float GetIncrementFixedAttackSpeed()
        {
            return 0;
        }

        /// <summary>
        /// +일 경우 공격속도 퍼센트 증가, -일 경우 공격속도 퍼센트 감소
        /// </summary>
        /// <returns></returns>
        [AssignEffectCodeFlag(EffectCodeInheritFlag.StatAttackSpeed)]
        public virtual float GetIncrementPercentAttackSpeed()
        {
            return 0f;
        }

        /// <summary>
        /// +일 경우 공격범위 고정 증가, -일 경우 공격범위 고정 감소
        /// </summary>
        /// <returns></returns>
        [AssignEffectCodeFlag(EffectCodeInheritFlag.StatAttackRange)]
        public virtual float GetIncrementFixedAttackRange()
        {
            return 0;
        }

        /// <summary>
        /// +일 경우 공격범위 퍼센트 증가, -일 경우 공격범위 퍼센트 감소
        /// </summary>
        /// <returns></returns>
        [AssignEffectCodeFlag(EffectCodeInheritFlag.StatAttackRange)]
        public virtual float GetIncrementPercentAttackRange()
        {
            return 0f;
        }

        /// <summary>
        /// 공격범위 모양을 변경합니다.
        /// </summary>
        /// <param name="prevShape"></param>
        /// <returns></returns>
        [AssignEffectCodeFlag(EffectCodeInheritFlag.StatAttackRangeShape)]
        public virtual AttackRangeShape ModifyAttackRangeShape(AttackRangeShape prevShape)
        {
            return prevShape;
        }

        /// <summary>
        /// +일 경우 스킬 대미지 배율 증가, -일 경우 스킬 대미지 배율 감소
        /// </summary>
        /// <returns></returns>
        [AssignEffectCodeFlag(EffectCodeInheritFlag.StatSkillDamageRate)]
        public virtual float GetIncrementFixedSkillDamageRate()
        {
            return 0f;
        }

        /// <summary>
        /// +일 경우 스킬 쿨타임 배율 증가, -일 경우 스킬 쿨타임 배율 감소
        /// </summary>
        /// <returns></returns>
        [AssignEffectCodeFlag(EffectCodeInheritFlag.StatSkillCooltimeRate)]
        public virtual float GetIncrementFixedSkillCooltimeRate()
        {
            return 0f;
        }

        /// <summary>
        /// +일 경우 총피해량 배율 증가, -일 경우 총피해량 배율 감소
        /// </summary>
        /// <returns></returns>
        [AssignEffectCodeFlag(EffectCodeInheritFlag.StatAttackDamageRate)]
        public virtual float GetIncrementFixedTotalDamageRate()
        {
            return 0f;
        }

        /// <summary>
        /// +일 경우 받는 피해량 배율 증가, -일 경우 받는 피해량 배율 감소
        /// </summary>
        /// <returns></returns>
        [AssignEffectCodeFlag(EffectCodeInheritFlag.StatTakenDamageRate)]
        public virtual float GetIncrementFixedTakenDamageRate()
        {
            return 0f;
        }

        /// <summary>
        /// +일 경우 주는 회복량 배율 증가, -일 경우 주는 회복량 배율 감소
        /// </summary>
        /// <returns></returns>
        [AssignEffectCodeFlag(EffectCodeInheritFlag.StatGivenHealRate)]
        public virtual float GetIncrementFixedGivenHealRate()
        {
            return 0f;
        }

        /// <summary>
        /// +일 경우 받는 회복량 배율 증가, -일 경우 받는 회복량 배율 감소
        /// </summary>
        /// <returns></returns>
        [AssignEffectCodeFlag(EffectCodeInheritFlag.StatTakenHealRate)]
        public virtual float GetIncrementFixedTakenHealRate()
        {
            return 0f;
        }

        /// <summary>
        /// CC기 면역 여부
        /// </summary>
        /// <returns></returns>
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

        public static double CalculateHP<T>(this IReadOnlyList<T> list, double basicStat) where T : EffectCodeStatBase
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

        public static double CalculateAD<T>(this IReadOnlyList<T> list, double basicStat) where T : EffectCodeStatBase
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

        public static double CalculateDEF<T>(this IReadOnlyList<T> list, double basicStat) where T : EffectCodeStatBase
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

                fixedValues[x.CalcOrder] += x.GetIncrementFixedDEF();
                percentValues[x.CalcOrder] += x.GetIncrementPercentDEF();
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

        public static double CalculateDEFPenetration<T>(this IReadOnlyList<T> list, double basicStat) where T : EffectCodeStatBase
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

                fixedValues[x.CalcOrder] += x.GetIncrementFixedDEFPenetration();
                percentValues[x.CalcOrder] += x.GetIncrementPercentDEFPenetration();
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

        public static double CalculateAP<T>(this IReadOnlyList<T> list, double basicStat) where T : EffectCodeStatBase
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

                fixedValues[x.CalcOrder] += x.GetIncrementFixedAP();
                percentValues[x.CalcOrder] += x.GetIncrementPercentAP();
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

        public static double CalculateRES<T>(this IReadOnlyList<T> list, double basicStat) where T : EffectCodeStatBase
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

                fixedValues[x.CalcOrder] += x.GetIncrementFixedRES();
                percentValues[x.CalcOrder] += x.GetIncrementPercentRES();
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

        public static double CalculateRESPenetration<T>(this IReadOnlyList<T> list, double basicStat) where T : EffectCodeStatBase
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

                fixedValues[x.CalcOrder] += x.GetIncrementFixedRESPenetration();
                percentValues[x.CalcOrder] += x.GetIncrementPercentRESPenetration();
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

        public static double CalculateRecoveryHP<T>(this IReadOnlyList<T> list, double basicStat) where T : EffectCodeStatBase
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

        public static float CalculateMoveSpeed<T>(this IReadOnlyList<T> list, float basicStat) where T : EffectCodeStatBase
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

        public static float CalculateCriticalProb<T>(this IReadOnlyList<T> list, float basicStat) where T : EffectCodeStatBase
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

        public static float CalculateCriticalDamageRate<T>(this IReadOnlyList<T> list, float basicStat) where T : EffectCodeStatBase
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

        public static float CalculateDoubleCriticalProb<T>(this IReadOnlyList<T> list, float basicStat) where T : EffectCodeStatBase
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

        public static float CalculateDoubleCriticalDamageRate<T>(this IReadOnlyList<T> list, float basicStat) where T : EffectCodeStatBase
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

        public static float CalculateAttackSpeed<T>(this IReadOnlyList<T> list, float basicStat) where T : EffectCodeStatBase
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
                fixedValue += x.GetIncrementFixedAttackSpeed();
                percentValue += x.GetIncrementPercentAttackSpeed();
            }

            return Mathf.Max(0, Mathf.Max(0f, (basicStat + fixedValue) * (1f + percentValue)));
        }

        public static int CalculateAttackRange<T>(this IReadOnlyList<T> list, int basicStat) where T : EffectCodeStatBase
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
                fixedValue += x.GetIncrementFixedAttackRange();
                percentValue += x.GetIncrementPercentAttackRange();
            }

            return Mathf.Max(0, Mathf.RoundToInt((basicStat + fixedValue) * (1f + percentValue)));
        }

        public static AttackRangeShape CalculateAttackRangeShape<T>(this IReadOnlyList<T> list, AttackRangeShape basicStat) where T : EffectCodeStatBase
        {
            if (list.Count == 0)
            {
                return basicStat;
            }

            for (var i = 0; i < list.Count; i++)
            {
                basicStat = list[i].ModifyAttackRangeShape(basicStat);
            }

            return basicStat;
        }

        public static float CalculateSkillDamageRate<T>(this IReadOnlyList<T> list, float basicStat) where T : EffectCodeStatBase
        {
            if (list.Count == 0)
            {
                return basicStat;
            }

            float fixedValue = 0;
            for (var i = 0; i < list.Count; i++)
            {
                fixedValue += list[i].GetIncrementFixedSkillDamageRate();
            }

            return Mathf.Max(0, basicStat + fixedValue);
        }

        public static float CalculateSkillCooltimeRate<T>(this IReadOnlyList<T> list, float basicStat) where T : EffectCodeStatBase
        {
            if (list.Count == 0)
            {
                return basicStat;
            }

            float fixedValue = 0;
            for (var i = 0; i < list.Count; i++)
            {
                fixedValue += list[i].GetIncrementFixedSkillCooltimeRate();
            }

            return basicStat + fixedValue;
        }

        public static float CalculateTotalDamageRate<T>(this IReadOnlyList<T> list, float basicStat) where T : EffectCodeStatBase
        {
            if (list.Count == 0)
            {
                return basicStat;
            }

            float fixedValue = 0;
            for (var i = 0; i < list.Count; i++)
            {
                fixedValue += list[i].GetIncrementFixedTotalDamageRate();
            }

            return Mathf.Max(0, basicStat + fixedValue);
        }

        public static float CalculateTakenDamageRate<T>(this IReadOnlyList<T> list, float basicStat) where T : EffectCodeStatBase
        {
            if (list.Count == 0)
            {
                return basicStat;
            }

            float fixedValue = 0;
            for (var i = 0; i < list.Count; i++)
            {
                fixedValue += list[i].GetIncrementFixedTakenDamageRate();
            }

            return Mathf.Max(0, basicStat + fixedValue);
        }

        public static float CalculateGivenHealRate<T>(this IReadOnlyList<T> list, float basicStat) where T : EffectCodeStatBase
        {
            if (list.Count == 0)
            {
                return basicStat;
            }

            float fixedValue = 0;
            for (var i = 0; i < list.Count; i++)
            {
                fixedValue += list[i].GetIncrementFixedGivenHealRate();
            }

            return Mathf.Max(0, basicStat + fixedValue);
        }

        public static float CalculateTakenHealRate<T>(this IReadOnlyList<T> list, float basicStat) where T : EffectCodeStatBase
        {
            if (list.Count == 0)
            {
                return basicStat;
            }

            float fixedValue = 0;
            for (var i = 0; i < list.Count; i++)
            {
                fixedValue += list[i].GetIncrementFixedTakenHealRate();
            }

            return Mathf.Max(0, basicStat + fixedValue);
        }

        public static CrowdControlType CalculateCrowdControlImmune<T>(this IReadOnlyList<T> list) where T : EffectCodeStatBase
        {
            if (list.Count == 0)
            {
                return CrowdControlType.None;
            }

            var fixedValue = CrowdControlType.None;
            for (var i = 0; i < list.Count; i++)
            {
                fixedValue |= list[i].GetCrowdControlImmune();
            }

            return fixedValue;
        }
    }
}
