using System.Collections.Generic;
using CookApps.Obfuscator;

namespace CookApps.TeamBattle.BattleSystem
{
    public partial class CharacterController
    {
        #region Stat getter
        private EffectCodeInheritFlag needUpdateFlag;
        private ObfuscatorDouble postHP, postAD, postRecoveryHP;

        private ObfuscatorFloat
            postCriticalProb,
            postCriticalDamageRate,
            postDoubleCriticalProb,
            postDoubleCriticalDamageRate,
            postMoveSpeed,
            postAttackSpeed,
            postAttackRange,
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

        private CrowdControlType postCCImmune;

        public double HP
        {
            get
            {
                if (needUpdateFlag.IsIncludeFlag(EffectCodeInheritFlag.StatHP) ||
                    GetCharacterStat().DirtyFlags.IsIncludeFlag(EffectCodeInheritFlag.StatHP))
                {
                    needUpdateFlag.RemoveFlag(EffectCodeInheritFlag.StatHP);
                    GetCharacterStat().RemoveDirtyFlag(EffectCodeInheritFlag.StatHP);
                    List<EffectCodeStatBase> effectCodes = GetEffectCodeContainer().GetCharacterEffectCodesByFlag(EffectCodeInheritFlag.StatHP);

                    ObfuscatorDouble prevHp = postHP;
                    postHP = effectCodes.CalculateHP(GetCharacterStat().HP);
                    ObfuscatorDouble diff = postHP - prevHp;
                    if (0 < diff)
                    {
                        currHp += diff;
                    }

                    if (currHp > postHP)
                    {
                        currHp = postHP;
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
                if (needUpdateFlag.IsIncludeFlag(EffectCodeInheritFlag.StatAD) ||
                    GetCharacterStat().DirtyFlags.IsIncludeFlag(EffectCodeInheritFlag.StatAD))
                {
                    needUpdateFlag.RemoveFlag(EffectCodeInheritFlag.StatAD);
                    GetCharacterStat().RemoveDirtyFlag(EffectCodeInheritFlag.StatAD);
                    List<EffectCodeStatBase> effectCodes = GetEffectCodeContainer().GetCharacterEffectCodesByFlag(EffectCodeInheritFlag.StatAD);
                    postAD = effectCodes.CalculateAD(GetCharacterStat().AD);
                }

                return postAD;
            }
        }

        public double RecoveryHP
        {
            get
            {
                if (needUpdateFlag.IsIncludeFlag(EffectCodeInheritFlag.StatRecoveryHP) ||
                    GetCharacterStat().DirtyFlags.IsIncludeFlag(EffectCodeInheritFlag.StatRecoveryHP))
                {
                    needUpdateFlag.RemoveFlag(EffectCodeInheritFlag.StatRecoveryHP);
                    GetCharacterStat().RemoveDirtyFlag(EffectCodeInheritFlag.StatRecoveryHP);
                    List<EffectCodeStatBase> effectCodes = GetEffectCodeContainer().GetCharacterEffectCodesByFlag(EffectCodeInheritFlag.StatRecoveryHP);
                    postRecoveryHP = effectCodes.CalculateRecoveryHP(GetCharacterStat().HPRecovery);
                }

                return postRecoveryHP;
            }
        }

        public float MoveSpeed
        {
            get
            {
                if (needUpdateFlag.IsIncludeFlag(EffectCodeInheritFlag.StatMoveSpeed) ||
                    GetCharacterStat().DirtyFlags.IsIncludeFlag(EffectCodeInheritFlag.StatMoveSpeed))
                {
                    needUpdateFlag.RemoveFlag(EffectCodeInheritFlag.StatMoveSpeed);
                    GetCharacterStat().RemoveDirtyFlag(EffectCodeInheritFlag.StatMoveSpeed);
                    List<EffectCodeStatBase> effectCodes = GetEffectCodeContainer().GetCharacterEffectCodesByFlag(EffectCodeInheritFlag.StatMoveSpeed);
                    postMoveSpeed = effectCodes.CalculateMoveSpeed(GetCharacterStat().MoveSpeed);
                }

                return postMoveSpeed;
            }
        }

        public float AttackSpeed
        {
            get
            {
                if (needUpdateFlag.IsIncludeFlag(EffectCodeInheritFlag.StatAttackSpeed) ||
                    GetCharacterStat().DirtyFlags.IsIncludeFlag(EffectCodeInheritFlag.StatAttackSpeed))
                {
                    needUpdateFlag.RemoveFlag(EffectCodeInheritFlag.StatAttackSpeed);
                    GetCharacterStat().RemoveDirtyFlag(EffectCodeInheritFlag.StatAttackSpeed);
                    List<EffectCodeStatBase> effectCodes = GetEffectCodeContainer().GetCharacterEffectCodesByFlag(EffectCodeInheritFlag.StatAttackSpeed);
                    postAttackSpeed = effectCodes.CalculateAttackSpeed(GetCharacterStat().AttackSpeed);
                }

                return postAttackSpeed;
            }
        }

        public float AttackRange
        {
            get
            {
                if (needUpdateFlag.IsIncludeFlag(EffectCodeInheritFlag.StatAttackRange) ||
                    GetCharacterStat().DirtyFlags.IsIncludeFlag(EffectCodeInheritFlag.StatAttackRange))
                {
                    needUpdateFlag.RemoveFlag(EffectCodeInheritFlag.StatAttackRange);
                    GetCharacterStat().RemoveDirtyFlag(EffectCodeInheritFlag.StatAttackRange);
                    List<EffectCodeStatBase> effectCodes = GetEffectCodeContainer().GetCharacterEffectCodesByFlag(EffectCodeInheritFlag.StatAttackRange);
                    postAttackRange = effectCodes.CalculateAttackRange(GetCharacterStat().AttackRange);
                }

                return postAttackRange;
            }
        }

        public float SearchRange => postSearchRange;

        public float CriticalProb
        {
            get
            {
                if (needUpdateFlag.IsIncludeFlag(EffectCodeInheritFlag.StatCriticalProb) ||
                    GetCharacterStat().DirtyFlags.IsIncludeFlag(EffectCodeInheritFlag.StatCriticalProb))
                {
                    needUpdateFlag.RemoveFlag(EffectCodeInheritFlag.StatCriticalProb);
                    GetCharacterStat().RemoveDirtyFlag(EffectCodeInheritFlag.StatCriticalProb);
                    List<EffectCodeStatBase> effectCodes = GetEffectCodeContainer().GetCharacterEffectCodesByFlag(EffectCodeInheritFlag.StatCriticalProb);
                    postCriticalProb = effectCodes.CalculateCriticalProb(GetCharacterStat().CriticalProb);
                }

                return postCriticalProb;
            }
        }

        public float CriticalDamageRate
        {
            get
            {
                if (needUpdateFlag.IsIncludeFlag(EffectCodeInheritFlag.StatCriticalDamageRate) ||
                    GetCharacterStat().DirtyFlags.IsIncludeFlag(EffectCodeInheritFlag.StatCriticalDamageRate))
                {
                    needUpdateFlag.RemoveFlag(EffectCodeInheritFlag.StatCriticalDamageRate);
                    GetCharacterStat().RemoveDirtyFlag(EffectCodeInheritFlag.StatCriticalDamageRate);
                    List<EffectCodeStatBase> effectCodes = GetEffectCodeContainer().GetCharacterEffectCodesByFlag(EffectCodeInheritFlag.StatCriticalDamageRate);
                    postCriticalDamageRate = effectCodes.CalculateCriticalDamageRate(GetCharacterStat().CriticalDamageRate);
                }

                return postCriticalDamageRate;
            }
        }

        public float DoubleCriticalProb
        {
            get
            {
                if (needUpdateFlag.IsIncludeFlag(EffectCodeInheritFlag.StatDoubleCriticalProb) ||
                    GetCharacterStat().DirtyFlags.IsIncludeFlag(EffectCodeInheritFlag.StatDoubleCriticalProb))
                {
                    needUpdateFlag.RemoveFlag(EffectCodeInheritFlag.StatDoubleCriticalProb);
                    GetCharacterStat().RemoveDirtyFlag(EffectCodeInheritFlag.StatDoubleCriticalProb);
                    List<EffectCodeStatBase> effectCodes = GetEffectCodeContainer().GetCharacterEffectCodesByFlag(EffectCodeInheritFlag.StatDoubleCriticalProb);
                    postDoubleCriticalProb = effectCodes.CalculateDoubleCriticalProb(GetCharacterStat().DoubleCriticalProb);
                }

                return postDoubleCriticalProb;
            }
        }

        public float DoubleCriticalDamageRate
        {
            get
            {
                if (needUpdateFlag.IsIncludeFlag(EffectCodeInheritFlag.StatDoubleCriticalDamageRate) ||
                    GetCharacterStat().DirtyFlags.IsIncludeFlag(EffectCodeInheritFlag.StatDoubleCriticalDamageRate))
                {
                    needUpdateFlag.RemoveFlag(EffectCodeInheritFlag.StatDoubleCriticalDamageRate);
                    GetCharacterStat().RemoveDirtyFlag(EffectCodeInheritFlag.StatDoubleCriticalDamageRate);
                    List<EffectCodeStatBase> effectCodes = GetEffectCodeContainer().GetCharacterEffectCodesByFlag(EffectCodeInheritFlag.StatDoubleCriticalDamageRate);
                    postDoubleCriticalDamageRate = effectCodes.CalculateCriticalDamageRate(GetCharacterStat().DoubleCriticalDamageRate);
                }

                return postDoubleCriticalDamageRate;
            }
        }

        public float SkillDamageRate
        {
            get
            {
                if (needUpdateFlag.IsIncludeFlag(EffectCodeInheritFlag.StatSkillDamageRate) ||
                    GetCharacterStat().DirtyFlags.IsIncludeFlag(EffectCodeInheritFlag.StatSkillDamageRate))
                {
                    needUpdateFlag.RemoveFlag(EffectCodeInheritFlag.StatSkillDamageRate);
                    GetCharacterStat().RemoveDirtyFlag(EffectCodeInheritFlag.StatSkillDamageRate);
                    List<EffectCodeStatBase> effectCodes = GetEffectCodeContainer().GetCharacterEffectCodesByFlag(EffectCodeInheritFlag.StatSkillDamageRate);
                    postSkillDamageRate = effectCodes.CalculateSkillDamageRate(GetCharacterStat().SkillDamageRate);
                }

                return postSkillDamageRate;
            }
        }

        public float SkillCooltimeRate
        {
            get
            {
                if (needUpdateFlag.IsIncludeFlag(EffectCodeInheritFlag.StatSkillCooltimeRate) ||
                    GetCharacterStat().DirtyFlags.IsIncludeFlag(EffectCodeInheritFlag.StatSkillCooltimeRate))
                {
                    needUpdateFlag.RemoveFlag(EffectCodeInheritFlag.StatSkillCooltimeRate);
                    GetCharacterStat().RemoveDirtyFlag(EffectCodeInheritFlag.StatSkillCooltimeRate);
                    List<EffectCodeStatBase> effectCodes = GetEffectCodeContainer().GetCharacterEffectCodesByFlag(EffectCodeInheritFlag.StatSkillCooltimeRate);
                    postSkillCooltimeRate = effectCodes.CalculateSkillCooltimeRate(GetCharacterStat().SkillCooltimeRate);
                }

                return postSkillCooltimeRate;
            }
        }

        public float AttackDamageRate
        {
            get
            {
                if (needUpdateFlag.IsIncludeFlag(EffectCodeInheritFlag.StatAttackDamageRate) ||
                    GetCharacterStat().DirtyFlags.IsIncludeFlag(EffectCodeInheritFlag.StatAttackDamageRate))
                {
                    needUpdateFlag.RemoveFlag(EffectCodeInheritFlag.StatAttackDamageRate);
                    GetCharacterStat().RemoveDirtyFlag(EffectCodeInheritFlag.StatAttackDamageRate);
                    List<EffectCodeStatBase> effectCodes = GetEffectCodeContainer().GetCharacterEffectCodesByFlag(EffectCodeInheritFlag.StatAttackDamageRate);
                    postAttackDamageRate = effectCodes.CalculateAttackDamageRate(GetCharacterStat().AttackDamageRate);
                }

                return postAttackDamageRate;
            }
        }

        public float TakenDamageRate
        {
            get
            {
                if (needUpdateFlag.IsIncludeFlag(EffectCodeInheritFlag.StatTakenDamageRate) ||
                    GetCharacterStat().DirtyFlags.IsIncludeFlag(EffectCodeInheritFlag.StatTakenDamageRate))
                {
                    needUpdateFlag.RemoveFlag(EffectCodeInheritFlag.StatTakenDamageRate);
                    GetCharacterStat().RemoveDirtyFlag(EffectCodeInheritFlag.StatTakenDamageRate);
                    List<EffectCodeStatBase> effectCodes = GetEffectCodeContainer().GetCharacterEffectCodesByFlag(EffectCodeInheritFlag.StatTakenDamageRate);
                    postTakenDamageRate = effectCodes.CalculateTakenDamageRate(GetCharacterStat().TakenDamageRate);
                }

                return postTakenDamageRate;
            }
        }

        public float GivenHealRate
        {
            get
            {
                if (needUpdateFlag.IsIncludeFlag(EffectCodeInheritFlag.StatGivenHealRate) ||
                    GetCharacterStat().DirtyFlags.IsIncludeFlag(EffectCodeInheritFlag.StatGivenHealRate))
                {
                    needUpdateFlag.RemoveFlag(EffectCodeInheritFlag.StatGivenHealRate);
                    GetCharacterStat().RemoveDirtyFlag(EffectCodeInheritFlag.StatGivenHealRate);
                    List<EffectCodeStatBase> effectCodes = GetEffectCodeContainer().GetCharacterEffectCodesByFlag(EffectCodeInheritFlag.StatGivenHealRate);
                    postGivenHealRate = effectCodes.CalculateGivenHealRate(GetCharacterStat().GivenHealRate);
                }

                return postGivenHealRate;
            }
        }

        public float TakenHealRate
        {
            get
            {
                if (needUpdateFlag.IsIncludeFlag(EffectCodeInheritFlag.StatTakenHealRate) ||
                    GetCharacterStat().DirtyFlags.IsIncludeFlag(EffectCodeInheritFlag.StatTakenHealRate))
                {
                    needUpdateFlag.RemoveFlag(EffectCodeInheritFlag.StatTakenHealRate);
                    GetCharacterStat().RemoveDirtyFlag(EffectCodeInheritFlag.StatTakenHealRate);
                    List<EffectCodeStatBase> effectCodes = GetEffectCodeContainer().GetCharacterEffectCodesByFlag(EffectCodeInheritFlag.StatTakenHealRate);
                    postTakenHealRate = effectCodes.CalculateTakenHealRate(GetCharacterStat().TakenHealRate);
                }

                return postTakenHealRate;
            }
        }

        public bool IsImmuneCrowdControlType(CrowdControlType type)
        {
            if (needUpdateFlag.IsIncludeFlag(EffectCodeInheritFlag.StatCrowdControlImmune) ||
                GetCharacterStat().DirtyFlags.IsIncludeFlag(EffectCodeInheritFlag.StatCrowdControlImmune))
            {
                needUpdateFlag.RemoveFlag(EffectCodeInheritFlag.StatCrowdControlImmune);
                GetCharacterStat().RemoveDirtyFlag(EffectCodeInheritFlag.StatCrowdControlImmune);
                List<EffectCodeStatBase> effectCodes = GetEffectCodeContainer().GetCharacterEffectCodesByFlag(EffectCodeInheritFlag.StatCrowdControlImmune);
                postCCImmune = effectCodes.CalculateCrowdControlImmune();
            }

            return (postCCImmune & type) == type;
        }

        private void EffectCodeDirtyFlagHandler(EffectCodeInheritFlag flag)
        {
            needUpdateFlag |= flag;
        }
        #endregion
    }
}
