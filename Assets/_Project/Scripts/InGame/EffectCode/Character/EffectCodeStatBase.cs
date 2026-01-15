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
        StatDEF = 1L << 2,//방어력
        StatADReduce = 1L << 3, //물방
        StatADPierce = 1L << 4, //물리관통력
        StatAP = 1L << 5, //마법공격력
        StatAPReduce = 1L << 6,//마방
        StatAPPierce = 1L << 7,//마관
        StatRecoveryHP = 1L << 8,//자힐 재생력
        StatMoveSpeed = 1L << 9,
        StatCriticalProb = 1L << 10,
        StatCriticalDamageRate = 1L << 11,
        StatDoubleCriticalProb = 1L << 12,
        StatDoubleCriticalDamageRate = 1L << 13,
        StatAttackSpeed = 1L << 14,
        StatAttackRange = 1L << 15,
        StatAttackRangeShape = 1L << 16,
        StatSkillDamageRate = 1L << 17,
        StatSkillCooltimeRate = 1L << 18,
        StatAttackDamageRate = 1L << 19,
        StatTakenDamageRate = 1L << 20,
        StatGivenHealRate = 1L << 21,
        StatTakenHealRate = 1L << 22,
        StatCrowdControlImmune = 1L << 23,
        StatPureDamageProb = 1L << 24,
        StatBlockingProb = 1L << 25,//블로킹율
        StatAvoidProb = 1L << 26,//회피율
        StatHitProb = 1L << 27,//명중율
        StatAll = ~(0xffffffffffffff << 28),
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
        UseOnAttackEnd = 1L << 55,
        UseAddSkillCooltime = 1L << 56,
        UseModifyDamageTestFlags = 1L << 57,// 데미지 테스트 중에 스킵하고 싶은 로직이 있다면 해당 함수 오버라이딩
        UseOnHpChange = 1L << 58,// 체력 변동 시 호출되는 함수
        UseOnCanceledCC = 1L << 59,// CC가 이뮨에 의해 캔슬되었을 때 호출되는 함수
        UseOnStateNormalAttackDamageEvent = 1L << 60,// 일반 공격 상태에서 데미지 이벤트 시 호출되는 함수 이걸로 데미지를 부여해야한다
        #endregion
        MAX = 1L << 62,
    };

    public static class EffectCodeInheritFlagExtensions
    {
        private static EffectCodeInheritFlag[] allFlagTypes;
        private static EffectCodeInheritFlag allFlags = EffectCodeInheritFlag.None;

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

        public static IReadOnlyList<EffectCodeInheritFlag> GetAllFlagTypes()
        {
            return allFlagTypes ??= Enum.GetValues(typeof(EffectCodeInheritFlag)).Cast<EffectCodeInheritFlag>().ToArray();
        }

        public static void GetUniqueFlags(this EffectCodeInheritFlag src, List<EffectCodeInheritFlag> result)
        {
            ulong flagInt = 1;
            while (flagInt != (ulong)EffectCodeInheritFlag.MAX)
            {
                var flag = (EffectCodeInheritFlag)flagInt;
                if (src.IsIncludeFlag(flag))
                {
                    result.Add(flag);
                }

                flagInt <<= 1;
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
        public AssignEffectCodeFlagAttribute(EffectCodeInheritFlag flag)
        {
        }
    }

    public abstract class EffectCodeStatBase : EffectCodeBase
    {
        public virtual EffectCodeInheritFlag GetFlag()
        {
            return EffectCodeInheritFlag.None;
        }

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
        /// +일 경우 최종 방어력 고정 증가, -일 경우 최종 방어력 고정 감소
        /// </summary>
        /// <returns></returns>
        [AssignEffectCodeFlag(EffectCodeInheritFlag.StatDEF)]
        public virtual double GetIncrementFixedDEF()
        {
            return 0;
        }

        /// <summary>
        /// +일 경우 최종 방어력 퍼센트 증가, -일 경우 최종 방어력 퍼센트 감소
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
        [AssignEffectCodeFlag(EffectCodeInheritFlag.StatADReduce)]
        public virtual double GetIncrementFixedADReduce()
        {
            return 0;
        }

        /// <summary>
        /// +일 경우 방어관통력 퍼센트 증가, -일 경우 방어관통력 퍼센트 감소
        /// </summary>
        /// <returns></returns>
        [AssignEffectCodeFlag(EffectCodeInheritFlag.StatADReduce)]
        public virtual double GetIncrementPercentADReduce()
        {
            return 0d;
        }

        /// <summary>
        /// +일 경우 방어관통력 고정 증가, -일 경우 방어관통력 고정 감소
        /// </summary>
        /// <returns></returns>
        [AssignEffectCodeFlag(EffectCodeInheritFlag.StatADPierce)]
        public virtual double GetIncrementFixedADPierce()
        {
            return 0;
        }

        /// <summary>
        /// +일 경우 방어관통력 퍼센트 증가, -일 경우 방어관통력 퍼센트 감소
        /// </summary>
        /// <returns></returns>
        [AssignEffectCodeFlag(EffectCodeInheritFlag.StatADPierce)]
        public virtual double GetIncrementPercentADPierce()
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
        [AssignEffectCodeFlag(EffectCodeInheritFlag.StatAPReduce)]
        public virtual double GetIncrementFixedAPReduce()
        {
            return 0;
        }

        /// <summary>
        /// +일 경우 저항력 퍼센트 증가, -일 경우 저항력 퍼센트 감소
        /// </summary>
        /// <returns></returns>
        [AssignEffectCodeFlag(EffectCodeInheritFlag.StatAPReduce)]
        public virtual double GetIncrementPercentAPReduce()
        {
            return 0d;
        }

        /// <summary>
        /// +일 경우 저항관통력 고정 증가, -일 경우 저항관통력 고정 감소
        /// </summary>
        /// <returns></returns>
        [AssignEffectCodeFlag(EffectCodeInheritFlag.StatAPPierce)]
        public virtual double GetIncrementFixedAPPierce()
        {
            return 0;
        }

        /// <summary>
        /// +일 경우 저항관통력 퍼센트 증가, -일 경우 저항관통력 퍼센트 감소
        /// </summary>
        /// <returns></returns>
        [AssignEffectCodeFlag(EffectCodeInheritFlag.StatAPPierce)]
        public virtual double GetIncrementPercentAPPierce()
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
        /// +일 경우 순수 대미지 확률 고정 증가, -일 경우 순수 대미지 확률 고정 감소
        /// </summary>
        /// <returns></returns>
        [AssignEffectCodeFlag(EffectCodeInheritFlag.StatPureDamageProb)]
        public virtual float GetIncrementFixedPureDamageProb()
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
        /// +일 경우 주는 회복량 배율 증가, -일 경우 주는 회복량 배율 감소
        /// </summary>
        /// <returns></returns>
        [AssignEffectCodeFlag(EffectCodeInheritFlag.StatGivenHealRate)]
        public virtual float GetIncrementPercentGivenHealRate()
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
        /// +일 경우 받는 회복량 배율 증가, -일 경우 받는 회복량 배율 감소
        /// </summary>
        /// <returns></returns>
        [AssignEffectCodeFlag(EffectCodeInheritFlag.StatTakenHealRate)]
        public virtual float GetIncrementPercentTakenHealRate()
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

        [AssignEffectCodeFlag(EffectCodeInheritFlag.StatBlockingProb)]
        public virtual float GetIncrementFixedBlockingProb()
        {
            return 0f;
        }

        [AssignEffectCodeFlag(EffectCodeInheritFlag.StatBlockingProb)]
        public virtual float GetIncrementPercentBlockingProb()
        {
            return 0f;
        }

        [AssignEffectCodeFlag(EffectCodeInheritFlag.StatAvoidProb)]
        public virtual float GetIncrementFixedAvoidProb()
        {
            return 0f;
        }

        [AssignEffectCodeFlag(EffectCodeInheritFlag.StatAvoidProb)]
        public virtual float GetIncrementPercentAvoidProb()
        {
            return 0f;
        }

        [AssignEffectCodeFlag(EffectCodeInheritFlag.StatHitProb)]
        public virtual float GetIncrementFixedHitProb()
        {
            return 0f;
        }

        [AssignEffectCodeFlag(EffectCodeInheritFlag.StatHitProb)]
        public virtual float GetIncrementPercentHitProb()
        {
            return 0f;
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

        public static double CalculateADReduce<T>(this IReadOnlyList<T> list, double basicStat) where T : EffectCodeStatBase
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

                fixedValues[x.CalcOrder] += x.GetIncrementFixedADReduce();
                percentValues[x.CalcOrder] += x.GetIncrementPercentADReduce();
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
        public static float CalculateADPierce<T>(this IReadOnlyList<T> list, float basicStat) where T : EffectCodeStatBase
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

                fixedValues[x.CalcOrder] += x.GetIncrementFixedADPierce();
                percentValues[x.CalcOrder] += x.GetIncrementPercentADPierce();
            }

            for (var i = 0; i < maxCalcOrder; i++)
            {
                basicStat = (basicStat + (float)fixedValues[i]) * (1f + (float)percentValues[i]);
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

        public static double CalculateAPReduce<T>(this IReadOnlyList<T> list, double basicStat) where T : EffectCodeStatBase
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

                fixedValues[x.CalcOrder] += x.GetIncrementFixedAPReduce();
                percentValues[x.CalcOrder] += x.GetIncrementPercentAPReduce();
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

        public static double CalculateAPPierce<T>(this IReadOnlyList<T> list, double basicStat) where T : EffectCodeStatBase
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

                fixedValues[x.CalcOrder] += x.GetIncrementFixedAPPierce();
                percentValues[x.CalcOrder] += x.GetIncrementPercentAPPierce();
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
        public static float CalculatePureDamageProb<T>(this IReadOnlyList<T> list, float basicStat) where T : EffectCodeStatBase
        {
            if (list.Count == 0)
            {
                return basicStat;
            }

            float fixedValue = 0;
            for (var i = 0; i < list.Count; i++)
            {
                T x = list[i];
                fixedValue += x.GetIncrementFixedPureDamageProb();
            }

            return Mathf.Max(0, (basicStat + fixedValue));
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
            float percentValue = 0;
            for (var i = 0; i < list.Count; i++)
            {
                fixedValue += list[i].GetIncrementFixedGivenHealRate();
                percentValue += list[i].GetIncrementPercentGivenHealRate();
            }

            return Mathf.Max(0, (basicStat + fixedValue) * (1f + percentValue));
        }

        public static float CalculateTakenHealRate<T>(this IReadOnlyList<T> list, float basicStat) where T : EffectCodeStatBase
        {
            if (list.Count == 0)
            {
                return basicStat;
            }

            float fixedValue = 0;
            float percentValue = 0;
            for (var i = 0; i < list.Count; i++)
            {
                fixedValue += list[i].GetIncrementFixedTakenHealRate();
                percentValue += list[i].GetIncrementPercentTakenHealRate();
            }

            return Mathf.Max(0, (basicStat + fixedValue) * (1f + percentValue));
        }

        public static float CalculateBlockingProb<T>(this IReadOnlyList<T> list, float basicStat) where T : EffectCodeStatBase
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
                fixedValue += x.GetIncrementFixedBlockingProb();
                percentValue += x.GetIncrementPercentBlockingProb();
            }

            return Mathf.Max(0, (basicStat + fixedValue) * (1f + percentValue));
        }

        public static float CalculateAvoidProb<T>(this IReadOnlyList<T> list, float basicStat) where T : EffectCodeStatBase
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
                fixedValue += x.GetIncrementFixedAvoidProb();
                percentValue += x.GetIncrementPercentAvoidProb();
            }
            return Mathf.Max(0, (basicStat + fixedValue) * (1f + percentValue));
        }

        public static float CalculateHitProb<T>(this IReadOnlyList<T> list, float basicStat) where T : EffectCodeStatBase
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
                fixedValue += x.GetIncrementFixedHitProb();
                percentValue += x.GetIncrementPercentHitProb();
            }
            return Mathf.Max(0, (basicStat + fixedValue) * (1f + percentValue));
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
