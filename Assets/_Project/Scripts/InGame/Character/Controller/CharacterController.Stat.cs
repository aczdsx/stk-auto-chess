using CookApps.Obfuscator;

namespace CookApps.BattleSystem
{
    public partial class CharacterController
    {
        private static readonly float RESIST_CAP = 0.7f;
        #region Stat getter

        private EffectCodeInheritFlag needUpdateFlag;

        private ObfuscatorDouble
            postHP,
            postAD,
            postADReduce,
            postAPReduce,
            postAP,
            postAPPierce,
            postRecoveryHP,
            postBlockingProb
            ;

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
            postStatusDamageRate,
            postPureDamageProb,
            postAvoidProb,
            postADPierce,
            postHitProb;

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
                    var effectCodes = GetEffectCodeContainer()
                        .GetCharacterEffectCodesByFlag(EffectCodeInheritFlag.StatHP);

                    var prevHp = postHP;
                    postHP = effectCodes.CalculateHP(GetCharacterStat().HP);
                    var diff = postHP - prevHp;
                    if (0 < diff) _currHp += diff;

                    if (_currHp > postHP) _currHp = postHP;

                    Debug.LogColor($"UpdateHP: {prevHp} -> {postHP}","red");

                    UpdateHpBar();
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
                    var effectCodes = GetEffectCodeContainer()
                        .GetCharacterEffectCodesByFlag(EffectCodeInheritFlag.StatAD);
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
                    var effectCodes = GetEffectCodeContainer()
                        .GetCharacterEffectCodesByFlag(EffectCodeInheritFlag.StatAP);
                    postAP = effectCodes.CalculateAP(GetCharacterStat().AP);
                }

                return postAP;
            }
        }

        public double ADReduce
        {
            get
            {
                if (needUpdateFlag.HasFlag(EffectCodeInheritFlag.StatADReduce) ||
                    GetCharacterStat().DirtyFlags.HasFlag(EffectCodeInheritFlag.StatADReduce))
                {
                    needUpdateFlag.RemoveFlag(EffectCodeInheritFlag.StatADReduce);
                    GetCharacterStat().RemoveDirtyFlag(EffectCodeInheritFlag.StatADReduce);
                    var effectCodes = GetEffectCodeContainer()
                        .GetCharacterEffectCodesByFlag(EffectCodeInheritFlag.StatADReduce);
                    postADReduce = effectCodes.CalculateADReduce(GetCharacterStat().ADReduce);
                }

                return postADReduce;
            }
        }

        public double APReduce
        {
            get
            {
                if (needUpdateFlag.HasFlag(EffectCodeInheritFlag.StatAPReduce) ||
                    GetCharacterStat().DirtyFlags.HasFlag(EffectCodeInheritFlag.StatAPReduce))
                {
                    needUpdateFlag.RemoveFlag(EffectCodeInheritFlag.StatAPReduce);
                    GetCharacterStat().RemoveDirtyFlag(EffectCodeInheritFlag.StatAPReduce);
                    var effectCodes = GetEffectCodeContainer()
                        .GetCharacterEffectCodesByFlag(EffectCodeInheritFlag.StatAPReduce);
                    postAPReduce = effectCodes.CalculateAPReduce(GetCharacterStat().APReduce);
                }

                return postAPReduce;
            }
        }

        public float ADPierce
        {
            get
            {
                if (needUpdateFlag.HasFlag(EffectCodeInheritFlag.StatADPierce) ||
                    GetCharacterStat().DirtyFlags.HasFlag(EffectCodeInheritFlag.StatADPierce))
                {
                    needUpdateFlag.RemoveFlag(EffectCodeInheritFlag.StatADPierce);
                    GetCharacterStat().RemoveDirtyFlag(EffectCodeInheritFlag.StatADPierce);
                    var effectCodes = GetEffectCodeContainer()
                        .GetCharacterEffectCodesByFlag(EffectCodeInheritFlag.StatADPierce);
                    postADPierce =
                        effectCodes.CalculateADPierce((float)GetCharacterStat().ADPierce);
                }

                return postADPierce;
            }
        }

        public double APPierce
        {
            get
            {
                if (needUpdateFlag.HasFlag(EffectCodeInheritFlag.StatAPPierce) ||
                    GetCharacterStat().DirtyFlags.HasFlag(EffectCodeInheritFlag.StatAPPierce))
                {
                    needUpdateFlag.RemoveFlag(EffectCodeInheritFlag.StatAPPierce);
                    GetCharacterStat().RemoveDirtyFlag(EffectCodeInheritFlag.StatAPPierce);
                    var effectCodes = GetEffectCodeContainer()
                        .GetCharacterEffectCodesByFlag(EffectCodeInheritFlag.StatAPPierce);
                    postAPPierce =
                        effectCodes.CalculateAPPierce(GetCharacterStat().APPierce);
                }

                return postAPPierce;
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
                    var effectCodes = GetEffectCodeContainer()
                        .GetCharacterEffectCodesByFlag(EffectCodeInheritFlag.StatRecoveryHP);
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
                    var effectCodes = GetEffectCodeContainer()
                        .GetCharacterEffectCodesByFlag(EffectCodeInheritFlag.StatMoveSpeed);
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
                    var effectCodes = GetEffectCodeContainer()
                        .GetCharacterEffectCodesByFlag(EffectCodeInheritFlag.StatAttackSpeed);
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
                    var effectCodes = GetEffectCodeContainer()
                        .GetCharacterEffectCodesByFlag(EffectCodeInheritFlag.StatAttackRangeShape);
                    _postAttackRangeShapeType =
                        effectCodes.CalculateAttackRangeShape(GetCharacterStat().AttackRangeShape);
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
                    var effectCodes = GetEffectCodeContainer()
                        .GetCharacterEffectCodesByFlag(EffectCodeInheritFlag.StatAttackRange);
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
                    var effectCodes = GetEffectCodeContainer()
                        .GetCharacterEffectCodesByFlag(EffectCodeInheritFlag.StatCriticalProb);
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
                    var effectCodes = GetEffectCodeContainer()
                        .GetCharacterEffectCodesByFlag(EffectCodeInheritFlag.StatCriticalDamageRate);
                    postCriticalDamageRate =
                        effectCodes.CalculateCriticalDamageRate(GetCharacterStat().CriticalDamageRate);
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
                    var effectCodes = GetEffectCodeContainer()
                        .GetCharacterEffectCodesByFlag(EffectCodeInheritFlag.StatDoubleCriticalProb);
                    postDoubleCriticalProb =
                        effectCodes.CalculateDoubleCriticalProb(GetCharacterStat().DoubleCriticalProb);
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
                    var effectCodes = GetEffectCodeContainer()
                        .GetCharacterEffectCodesByFlag(EffectCodeInheritFlag.StatDoubleCriticalDamageRate);
                    postDoubleCriticalDamageRate =
                        effectCodes.CalculateCriticalDamageRate(GetCharacterStat().DoubleCriticalDamageRate);
                }

                return postDoubleCriticalDamageRate;
            }
        }
        public float PureDamageProb
        {
            get
            {
                if (needUpdateFlag.HasFlag(EffectCodeInheritFlag.StatPureDamageProb) ||
                    GetCharacterStat().DirtyFlags.HasFlag(EffectCodeInheritFlag.StatPureDamageProb))
                {
                    needUpdateFlag.RemoveFlag(EffectCodeInheritFlag.StatPureDamageProb);
                    GetCharacterStat().RemoveDirtyFlag(EffectCodeInheritFlag.StatPureDamageProb);
                    var effectCodes = GetEffectCodeContainer()
                        .GetCharacterEffectCodesByFlag(EffectCodeInheritFlag.StatPureDamageProb);
                    postPureDamageProb =
                        effectCodes.CalculatePureDamageProb(GetCharacterStat().PureDamageProb);
                }

                return postPureDamageProb;
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
                    var effectCodes = GetEffectCodeContainer()
                        .GetCharacterEffectCodesByFlag(EffectCodeInheritFlag.StatSkillDamageRate);
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
                    var effectCodes = GetEffectCodeContainer()
                        .GetCharacterEffectCodesByFlag(EffectCodeInheritFlag.StatSkillCooltimeRate);
                    postSkillCooltimeRate =
                        effectCodes.CalculateSkillCooltimeRate(GetCharacterStat().SkillCooltimeRate);
                }

                return postSkillCooltimeRate;
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
                    var effectCodes = GetEffectCodeContainer()
                        .GetCharacterEffectCodesByFlag(EffectCodeInheritFlag.StatAttackDamageRate);
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
                    var effectCodes = GetEffectCodeContainer()
                        .GetCharacterEffectCodesByFlag(EffectCodeInheritFlag.StatTakenDamageRate);
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
                    var effectCodes = GetEffectCodeContainer()
                        .GetCharacterEffectCodesByFlag(EffectCodeInheritFlag.StatGivenHealRate);
                    postGivenHealRate = effectCodes.CalculateGivenHealRate(GetCharacterStat().GivenHealRate);
                }

                return postGivenHealRate;
            }
        }

        public float BlockingProb
        {
            get
            {
                if (needUpdateFlag.HasFlag(EffectCodeInheritFlag.StatBlockingProb) ||
                    GetCharacterStat().DirtyFlags.HasFlag(EffectCodeInheritFlag.StatBlockingProb))
                {
                    needUpdateFlag.RemoveFlag(EffectCodeInheritFlag.StatBlockingProb);
                    GetCharacterStat().RemoveDirtyFlag(EffectCodeInheritFlag.StatBlockingProb);
                    var effectCodes = GetEffectCodeContainer()
                        .GetCharacterEffectCodesByFlag(EffectCodeInheritFlag.StatBlockingProb);
                    postBlockingProb = effectCodes.CalculateBlockingProb(GetCharacterStat().BlockingProb);
                }

                return postGivenHealRate;
            }
        }

        public float AvoidProb
        {
            get
            {
                if (needUpdateFlag.HasFlag(EffectCodeInheritFlag.StatAvoidProb) ||
                    GetCharacterStat().DirtyFlags.HasFlag(EffectCodeInheritFlag.StatAvoidProb))
                {
                    needUpdateFlag.RemoveFlag(EffectCodeInheritFlag.StatAvoidProb);
                    GetCharacterStat().RemoveDirtyFlag(EffectCodeInheritFlag.StatAvoidProb);
                    var effectCodes = GetEffectCodeContainer()
                        .GetCharacterEffectCodesByFlag(EffectCodeInheritFlag.StatAvoidProb);
                    postAvoidProb = effectCodes.CalculateAvoidProb(GetCharacterStat().AvoidProb);
                }
                return postAvoidProb;
            }
        }

        public float HitProb
        {
            get
            {
                if (needUpdateFlag.HasFlag(EffectCodeInheritFlag.StatHitProb) ||
                    GetCharacterStat().DirtyFlags.HasFlag(EffectCodeInheritFlag.StatHitProb))
                {
                    needUpdateFlag.RemoveFlag(EffectCodeInheritFlag.StatHitProb);
                    GetCharacterStat().RemoveDirtyFlag(EffectCodeInheritFlag.StatHitProb);
                    var effectCodes = GetEffectCodeContainer()
                        .GetCharacterEffectCodesByFlag(EffectCodeInheritFlag.StatHitProb);
                    postHitProb = effectCodes.CalculateHitProb(GetCharacterStat().HitProb);
                }
                return postHitProb;
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
                    var effectCodes = GetEffectCodeContainer()
                        .GetCharacterEffectCodesByFlag(EffectCodeInheritFlag.StatTakenHealRate);
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
                var effectCodes = GetEffectCodeContainer()
                    .GetCharacterEffectCodesByFlag(EffectCodeInheritFlag.StatCrowdControlImmune);
                postCCImmune = effectCodes.CalculateCrowdControlImmune();
            }

            return (postCCImmune & type) == type;
        }

        private void EffectCodeOnChangedDirtyFlagHandler(EffectCodeInheritFlag flag)
        {
            needUpdateFlag |= flag;
            if (flag.HasFlag(EffectCodeInheritFlag.StatHP))
            {
                UpdateHpBar();
            }
        }

        /// <summary>
        /// MaxHP와 CurrentHP를 강제로 오버라이드 (튜토리얼 등 특수 상황용)
        /// </summary>
        /// <param name="hp">설정할 HP 값</param>
        public void OverrideHp(double hp)
        {
            postHP = hp;
            _currHp = hp;
            UpdateHpBar();
        }

        /// <summary>
        /// 이동속도를 강제로 오버라이드 (튜토리얼 등 특수 상황용)
        /// </summary>
        /// <param name="moveSpeed">설정할 이동속도 값</param>
        public void OverrideMoveSpeed(float moveSpeed)
        {
            postMoveSpeed = moveSpeed;
        }

        #endregion
    }
}