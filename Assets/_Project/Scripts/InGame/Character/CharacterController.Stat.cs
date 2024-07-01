using System.Collections.Generic;
using CookApps.Obfuscator;
using UnityEngine;

namespace CookApps.BattleSystem
{
    public partial class CharacterController
    {
        #region Stat getter
        private EffectCodeInheritFlag needUpdateFlag;

        private ObfuscatorDouble
            postHP,
            postAD,
            postAP,
            postDEF,
            postRES,
            postDEFPenetration,
            postRESPenetration,
            postRecoveryHP;

        private ObfuscatorFloat
            postCriticalProb,
            postCriticalDamageRate,
            postDoubleCriticalProb,
            postDoubleCriticalDamageRate,
            postMoveSpeed,
            postAttackSpeed,
            postSearchRange,
            postSkillRange,
            postSkillDamageRate,
            postSkillActivateCountRate,
            postSkillCooltimeRate,
            postAttackDamageRate,
            postNormalAttackDamageRate,
            postTakenDamageRate,
            postBossAttackDamageRate,
            postCommonAttackDamageRate,
            postGivenHealRate,
            postTakenHealRate,
            postGoldGainIndividual,
            postExpGainIndividual,
            postDungeonAttackDamageRateIndividual,
            postStatusDamageRate;

        private ObfuscatorInt postAttackRange;

        private AttackRangeShape _postAttackRangeShapeType;

        private CrowdControlType postCCImmune;

        public double HP
        {
            get
            {
                if (needUpdateFlag.HasFlag(EffectCodeInheritFlag.StatHP) ||
                    GetCharacterStat().DirtyFlags.HasFlag(EffectCodeInheritFlag.StatHP))
                {
                    needUpdateFlag.RemoveFlag(EffectCodeInheritFlag.StatHP);
                    GetCharacterStat().RemoveDirtyFlag(EffectCodeInheritFlag.StatHP);
                    var effectCodes = GetEffectCodeContainer().GetCharacterEffectCodesByFlag(EffectCodeInheritFlag.StatHP);

                    ObfuscatorDouble prevHp = postHP;
                    postHP = effectCodes.CalculateHP(GetCharacterStat().HP);
                    ObfuscatorDouble diff = postHP - prevHp;
                    if (0 < diff)
                    {
                        _currHp += diff;
                    }

                    if (_currHp > postHP)
                    {
                        _currHp = postHP;
                    }

                    UpdateHp();
                }

                return postHP;
            }
        }

        public double AD
        {
            get
            {
                if (needUpdateFlag.HasFlag(EffectCodeInheritFlag.StatAD) ||
                    GetCharacterStat().DirtyFlags.HasFlag(EffectCodeInheritFlag.StatAD))
                {
                    needUpdateFlag.RemoveFlag(EffectCodeInheritFlag.StatAD);
                    GetCharacterStat().RemoveDirtyFlag(EffectCodeInheritFlag.StatAD);
                    var effectCodes = GetEffectCodeContainer().GetCharacterEffectCodesByFlag(EffectCodeInheritFlag.StatAD);
                    postAD = effectCodes.CalculateAD(GetCharacterStat().AD);
                }

                return postAD;
            }
        }

        public double AP
        {
            get
            {
                if (needUpdateFlag.HasFlag(EffectCodeInheritFlag.StatAP) ||
                    GetCharacterStat().DirtyFlags.HasFlag(EffectCodeInheritFlag.StatAP))
                {
                    needUpdateFlag.RemoveFlag(EffectCodeInheritFlag.StatAP);
                    GetCharacterStat().RemoveDirtyFlag(EffectCodeInheritFlag.StatAP);
                    var effectCodes = GetEffectCodeContainer().GetCharacterEffectCodesByFlag(EffectCodeInheritFlag.StatAP);
                    postAP = effectCodes.CalculateAD(GetCharacterStat().AP);
                }

                return postAP;
            }
        }

        public double DEF
        {
            get
            {
                if (needUpdateFlag.HasFlag(EffectCodeInheritFlag.StatDEF) ||
                    GetCharacterStat().DirtyFlags.HasFlag(EffectCodeInheritFlag.StatDEF))
                {
                    needUpdateFlag.RemoveFlag(EffectCodeInheritFlag.StatDEF);
                    GetCharacterStat().RemoveDirtyFlag(EffectCodeInheritFlag.StatDEF);
                    var effectCodes = GetEffectCodeContainer().GetCharacterEffectCodesByFlag(EffectCodeInheritFlag.StatDEF);
                    postDEF = effectCodes.CalculateAD(GetCharacterStat().DEF);
                }

                return postDEF;
            }
        }

        public double RES
        {
            get
            {
                if (needUpdateFlag.HasFlag(EffectCodeInheritFlag.StatRES) ||
                    GetCharacterStat().DirtyFlags.HasFlag(EffectCodeInheritFlag.StatRES))
                {
                    needUpdateFlag.RemoveFlag(EffectCodeInheritFlag.StatRES);
                    GetCharacterStat().RemoveDirtyFlag(EffectCodeInheritFlag.StatRES);
                    var effectCodes = GetEffectCodeContainer().GetCharacterEffectCodesByFlag(EffectCodeInheritFlag.StatRES);
                    postRES = effectCodes.CalculateAD(GetCharacterStat().RES);
                }

                return postRES;
            }
        }

        public double DEFPenetration
        {
            get
            {
                if (needUpdateFlag.HasFlag(EffectCodeInheritFlag.StatDEFPenetration) ||
                    GetCharacterStat().DirtyFlags.HasFlag(EffectCodeInheritFlag.StatDEFPenetration))
                {
                    needUpdateFlag.RemoveFlag(EffectCodeInheritFlag.StatDEFPenetration);
                    GetCharacterStat().RemoveDirtyFlag(EffectCodeInheritFlag.StatDEFPenetration);
                    var effectCodes = GetEffectCodeContainer().GetCharacterEffectCodesByFlag(EffectCodeInheritFlag.StatDEFPenetration);
                    postDEFPenetration = effectCodes.CalculateAD(GetCharacterStat().DEFPenetration);
                }

                return postDEFPenetration;
            }
        }

        public double RESPenetration
        {
            get
            {
                if (needUpdateFlag.HasFlag(EffectCodeInheritFlag.StatRESPenetration) ||
                    GetCharacterStat().DirtyFlags.HasFlag(EffectCodeInheritFlag.StatRESPenetration))
                {
                    needUpdateFlag.RemoveFlag(EffectCodeInheritFlag.StatRESPenetration);
                    GetCharacterStat().RemoveDirtyFlag(EffectCodeInheritFlag.StatRESPenetration);
                    var effectCodes = GetEffectCodeContainer().GetCharacterEffectCodesByFlag(EffectCodeInheritFlag.StatRESPenetration);
                    postRESPenetration = effectCodes.CalculateAD(GetCharacterStat().RESPenetration);
                }

                return postRESPenetration;
            }
        }

        public double RecoveryHP
        {
            get
            {
                if (needUpdateFlag.HasFlag(EffectCodeInheritFlag.StatRecoveryHP) ||
                    GetCharacterStat().DirtyFlags.HasFlag(EffectCodeInheritFlag.StatRecoveryHP))
                {
                    needUpdateFlag.RemoveFlag(EffectCodeInheritFlag.StatRecoveryHP);
                    GetCharacterStat().RemoveDirtyFlag(EffectCodeInheritFlag.StatRecoveryHP);
                    var effectCodes = GetEffectCodeContainer().GetCharacterEffectCodesByFlag(EffectCodeInheritFlag.StatRecoveryHP);
                    postRecoveryHP = effectCodes.CalculateRecoveryHP(GetCharacterStat().HPRecovery);
                }

                return postRecoveryHP;
            }
        }

        public float MoveSpeed
        {
            get
            {
                if (needUpdateFlag.HasFlag(EffectCodeInheritFlag.StatMoveSpeed) ||
                    GetCharacterStat().DirtyFlags.HasFlag(EffectCodeInheritFlag.StatMoveSpeed))
                {
                    needUpdateFlag.RemoveFlag(EffectCodeInheritFlag.StatMoveSpeed);
                    GetCharacterStat().RemoveDirtyFlag(EffectCodeInheritFlag.StatMoveSpeed);
                    var effectCodes = GetEffectCodeContainer().GetCharacterEffectCodesByFlag(EffectCodeInheritFlag.StatMoveSpeed);
                    postMoveSpeed = effectCodes.CalculateMoveSpeed(GetCharacterStat().MoveSpeed);
                }

                return postMoveSpeed;
            }
        }

        public float AttackSpeed
        {
            get
            {
                if (needUpdateFlag.HasFlag(EffectCodeInheritFlag.StatAttackSpeed) ||
                    GetCharacterStat().DirtyFlags.HasFlag(EffectCodeInheritFlag.StatAttackSpeed))
                {
                    needUpdateFlag.RemoveFlag(EffectCodeInheritFlag.StatAttackSpeed);
                    GetCharacterStat().RemoveDirtyFlag(EffectCodeInheritFlag.StatAttackSpeed);
                    var effectCodes = GetEffectCodeContainer().GetCharacterEffectCodesByFlag(EffectCodeInheritFlag.StatAttackSpeed);
                    postAttackSpeed = effectCodes.CalculateAttackSpeed(GetCharacterStat().AttackSpeed);
                }

                return postAttackSpeed;
            }
        }

        public AttackRangeShape AttackRangeShapeType
        {
            get
            {
                if (needUpdateFlag.HasFlag(EffectCodeInheritFlag.StatAttackRangeShape) ||
                    GetCharacterStat().DirtyFlags.HasFlag(EffectCodeInheritFlag.StatAttackRangeShape))
                {
                    needUpdateFlag.RemoveFlag(EffectCodeInheritFlag.StatAttackRangeShape);
                    GetCharacterStat().RemoveDirtyFlag(EffectCodeInheritFlag.StatAttackRangeShape);
                    var effectCodes = GetEffectCodeContainer().GetCharacterEffectCodesByFlag(EffectCodeInheritFlag.StatAttackRangeShape);
                    _postAttackRangeShapeType = effectCodes.CalculateAttackRangeShape(GetCharacterStat().AttackRangeShape);
                }

                return _postAttackRangeShapeType;
            }
        }

        public int AttackRange
        {
            get
            {
                if (needUpdateFlag.HasFlag(EffectCodeInheritFlag.StatAttackRange) ||
                    GetCharacterStat().DirtyFlags.HasFlag(EffectCodeInheritFlag.StatAttackRange))
                {
                    needUpdateFlag.RemoveFlag(EffectCodeInheritFlag.StatAttackRange);
                    GetCharacterStat().RemoveDirtyFlag(EffectCodeInheritFlag.StatAttackRange);
                    var effectCodes = GetEffectCodeContainer().GetCharacterEffectCodesByFlag(EffectCodeInheritFlag.StatAttackRange);
                    postAttackRange = effectCodes.CalculateAttackRange(GetCharacterStat().AttackRange);
                }

                return postAttackRange;
            }
        }

        public float CriticalProb
        {
            get
            {
                if (needUpdateFlag.HasFlag(EffectCodeInheritFlag.StatCriticalProb) ||
                    GetCharacterStat().DirtyFlags.HasFlag(EffectCodeInheritFlag.StatCriticalProb))
                {
                    needUpdateFlag.RemoveFlag(EffectCodeInheritFlag.StatCriticalProb);
                    GetCharacterStat().RemoveDirtyFlag(EffectCodeInheritFlag.StatCriticalProb);
                    var effectCodes = GetEffectCodeContainer().GetCharacterEffectCodesByFlag(EffectCodeInheritFlag.StatCriticalProb);
                    postCriticalProb = effectCodes.CalculateCriticalProb(GetCharacterStat().CriticalProb);
                }

                return postCriticalProb;
            }
        }

        public float CriticalDamageRate
        {
            get
            {
                if (needUpdateFlag.HasFlag(EffectCodeInheritFlag.StatCriticalDamageRate) ||
                    GetCharacterStat().DirtyFlags.HasFlag(EffectCodeInheritFlag.StatCriticalDamageRate))
                {
                    needUpdateFlag.RemoveFlag(EffectCodeInheritFlag.StatCriticalDamageRate);
                    GetCharacterStat().RemoveDirtyFlag(EffectCodeInheritFlag.StatCriticalDamageRate);
                    var effectCodes = GetEffectCodeContainer().GetCharacterEffectCodesByFlag(EffectCodeInheritFlag.StatCriticalDamageRate);
                    postCriticalDamageRate = effectCodes.CalculateCriticalDamageRate(GetCharacterStat().CriticalDamageRate);
                }

                return postCriticalDamageRate;
            }
        }

        public float DoubleCriticalProb
        {
            get
            {
                if (needUpdateFlag.HasFlag(EffectCodeInheritFlag.StatDoubleCriticalProb) ||
                    GetCharacterStat().DirtyFlags.HasFlag(EffectCodeInheritFlag.StatDoubleCriticalProb))
                {
                    needUpdateFlag.RemoveFlag(EffectCodeInheritFlag.StatDoubleCriticalProb);
                    GetCharacterStat().RemoveDirtyFlag(EffectCodeInheritFlag.StatDoubleCriticalProb);
                    var effectCodes = GetEffectCodeContainer().GetCharacterEffectCodesByFlag(EffectCodeInheritFlag.StatDoubleCriticalProb);
                    postDoubleCriticalProb = effectCodes.CalculateDoubleCriticalProb(GetCharacterStat().DoubleCriticalProb);
                }

                return postDoubleCriticalProb;
            }
        }

        public float DoubleCriticalDamageRate
        {
            get
            {
                if (needUpdateFlag.HasFlag(EffectCodeInheritFlag.StatDoubleCriticalDamageRate) ||
                    GetCharacterStat().DirtyFlags.HasFlag(EffectCodeInheritFlag.StatDoubleCriticalDamageRate))
                {
                    needUpdateFlag.RemoveFlag(EffectCodeInheritFlag.StatDoubleCriticalDamageRate);
                    GetCharacterStat().RemoveDirtyFlag(EffectCodeInheritFlag.StatDoubleCriticalDamageRate);
                    var effectCodes = GetEffectCodeContainer().GetCharacterEffectCodesByFlag(EffectCodeInheritFlag.StatDoubleCriticalDamageRate);
                    postDoubleCriticalDamageRate = effectCodes.CalculateCriticalDamageRate(GetCharacterStat().DoubleCriticalDamageRate);
                }

                return postDoubleCriticalDamageRate;
            }
        }

        public float SkillDamageRate
        {
            get
            {
                if (needUpdateFlag.HasFlag(EffectCodeInheritFlag.StatSkillDamageRate) ||
                    GetCharacterStat().DirtyFlags.HasFlag(EffectCodeInheritFlag.StatSkillDamageRate))
                {
                    needUpdateFlag.RemoveFlag(EffectCodeInheritFlag.StatSkillDamageRate);
                    GetCharacterStat().RemoveDirtyFlag(EffectCodeInheritFlag.StatSkillDamageRate);
                    var effectCodes = GetEffectCodeContainer().GetCharacterEffectCodesByFlag(EffectCodeInheritFlag.StatSkillDamageRate);
                    postSkillDamageRate = effectCodes.CalculateSkillDamageRate(GetCharacterStat().SkillDamageRate);
                }

                return postSkillDamageRate;
            }
        }

        public float SkillCooltimeRate
        {
            get
            {
                if (needUpdateFlag.HasFlag(EffectCodeInheritFlag.StatSkillCooltimeRate) ||
                    GetCharacterStat().DirtyFlags.HasFlag(EffectCodeInheritFlag.StatSkillCooltimeRate))
                {
                    needUpdateFlag.RemoveFlag(EffectCodeInheritFlag.StatSkillCooltimeRate);
                    GetCharacterStat().RemoveDirtyFlag(EffectCodeInheritFlag.StatSkillCooltimeRate);
                    var effectCodes = GetEffectCodeContainer().GetCharacterEffectCodesByFlag(EffectCodeInheritFlag.StatSkillCooltimeRate);
                    postSkillCooltimeRate = 1 + effectCodes.CalculateSkillCooltimeRate(GetCharacterStat().SkillCooltimeRate);
                }

                return Mathf.Max(0, postSkillCooltimeRate);
            }
        }

        public float AttackDamageRate
        {
            get
            {
                if (needUpdateFlag.HasFlag(EffectCodeInheritFlag.StatAttackDamageRate) ||
                    GetCharacterStat().DirtyFlags.HasFlag(EffectCodeInheritFlag.StatAttackDamageRate))
                {
                    needUpdateFlag.RemoveFlag(EffectCodeInheritFlag.StatAttackDamageRate);
                    GetCharacterStat().RemoveDirtyFlag(EffectCodeInheritFlag.StatAttackDamageRate);
                    var effectCodes = GetEffectCodeContainer().GetCharacterEffectCodesByFlag(EffectCodeInheritFlag.StatAttackDamageRate);
                    postAttackDamageRate = effectCodes.CalculateTotalDamageRate(GetCharacterStat().AttackDamageRate);
                }

                return postAttackDamageRate;
            }
        }

        public float TakenDamageRate
        {
            get
            {
                if (needUpdateFlag.HasFlag(EffectCodeInheritFlag.StatTakenDamageRate) ||
                    GetCharacterStat().DirtyFlags.HasFlag(EffectCodeInheritFlag.StatTakenDamageRate))
                {
                    needUpdateFlag.RemoveFlag(EffectCodeInheritFlag.StatTakenDamageRate);
                    GetCharacterStat().RemoveDirtyFlag(EffectCodeInheritFlag.StatTakenDamageRate);
                    var effectCodes = GetEffectCodeContainer().GetCharacterEffectCodesByFlag(EffectCodeInheritFlag.StatTakenDamageRate);
                    postTakenDamageRate = effectCodes.CalculateTakenDamageRate(GetCharacterStat().TakenDamageRate);
                }

                return postTakenDamageRate;
            }
        }

        public float GivenHealRate
        {
            get
            {
                if (needUpdateFlag.HasFlag(EffectCodeInheritFlag.StatGivenHealRate) ||
                    GetCharacterStat().DirtyFlags.HasFlag(EffectCodeInheritFlag.StatGivenHealRate))
                {
                    needUpdateFlag.RemoveFlag(EffectCodeInheritFlag.StatGivenHealRate);
                    GetCharacterStat().RemoveDirtyFlag(EffectCodeInheritFlag.StatGivenHealRate);
                    var effectCodes = GetEffectCodeContainer().GetCharacterEffectCodesByFlag(EffectCodeInheritFlag.StatGivenHealRate);
                    postGivenHealRate = effectCodes.CalculateGivenHealRate(GetCharacterStat().GivenHealRate);
                }

                return postGivenHealRate;
            }
        }

        public float TakenHealRate
        {
            get
            {
                if (needUpdateFlag.HasFlag(EffectCodeInheritFlag.StatTakenHealRate) ||
                    GetCharacterStat().DirtyFlags.HasFlag(EffectCodeInheritFlag.StatTakenHealRate))
                {
                    needUpdateFlag.RemoveFlag(EffectCodeInheritFlag.StatTakenHealRate);
                    GetCharacterStat().RemoveDirtyFlag(EffectCodeInheritFlag.StatTakenHealRate);
                    var effectCodes = GetEffectCodeContainer().GetCharacterEffectCodesByFlag(EffectCodeInheritFlag.StatTakenHealRate);
                    postTakenHealRate = effectCodes.CalculateTakenHealRate(GetCharacterStat().TakenHealRate);
                }

                return postTakenHealRate;
            }
        }

        public bool IsImmuneCrowdControlType(CrowdControlType type)
        {
            if (needUpdateFlag.HasFlag(EffectCodeInheritFlag.StatCrowdControlImmune) ||
                GetCharacterStat().DirtyFlags.HasFlag(EffectCodeInheritFlag.StatCrowdControlImmune))
            {
                needUpdateFlag.RemoveFlag(EffectCodeInheritFlag.StatCrowdControlImmune);
                GetCharacterStat().RemoveDirtyFlag(EffectCodeInheritFlag.StatCrowdControlImmune);
                var effectCodes = GetEffectCodeContainer().GetCharacterEffectCodesByFlag(EffectCodeInheritFlag.StatCrowdControlImmune);
                postCCImmune = effectCodes.CalculateCrowdControlImmune();
            }

            return (postCCImmune & type) == type;
        }

        private void EffectCodeOnChangedDirtyFlagHandler(EffectCodeInheritFlag flag)
        {
            needUpdateFlag |= flag;
        }
        #endregion
    }
}
