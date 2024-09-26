using System;

public class UseEffectCodeIdsAttribute : Attribute
{
    public int[] CodeIds { get; }

    public UseEffectCodeIdsAttribute(params int[] codeIds)
    {
#if UNITY_EDITOR
        CodeIds = codeIds;
#endif
    }
}


public class AssignEffectCodeFlagAttribute : Attribute
{
    public AssignEffectCodeFlagAttribute(EffectCodeInheritFlag flag)
    {
    }
}

[Flags]
public enum EffectCodeInheritFlag : UInt64
{
    None                                    = 0L,
    StatHP                                  = 1L << 0,
    StatAD                                  = 1L << 1,
    StatRecoveryHP                          = 1L << 2,
    StatMoveSpeed                           = 1L << 3,
    StatCriticalProb                        = 1L << 4,
    StatCriticalDamageRate                  = 1L << 5,
    StatDoubleCriticalProb                  = 1L << 6,
    StatDoubleCriticalDamageRate            = 1L << 7,
    StatAttackSpeed                         = 1L << 8,
    StatAttackRange                         = 1L << 9,
    StatSkillDamageRate                     = 1L << 10,
    StatSkillCooltimeRate                   = 1L << 11,
    StatNormalAttackDamageRate              = 1L << 12,
    StatAttackDamageRate                    = 1L << 13,
    StatTakenDamageRate                     = 1L << 14,
    StatGivenHealRate                       = 1L << 15,
    StatTakenHealRate                       = 1L << 16,
    StatCrowdControlImmune                  = 1L << 17,
    UseOnUpdate                             = 1L << 18,
    UseOnAttack                             = 1L << 19,
    UseOnCooltime                           = 1L << 20,
    UseOnKill                               = 1L << 21,
    UseOnHealed                             = 1L << 22,
    UseOnDamaged                            = 1L << 23,
    UseOnCritical                           = 1L << 24,
    UseIsReadyToActivate                    = 1L << 25,
    UseIsUseNormalAttack                    = 1L << 26,
    StatBloodSucking                        = 1L << 27,
    StatBossAttackDamageRate                = 1L << 28,
    UseOnDead                               = 1L << 29,
    UseModifyDamageAmount                   = 1L << 30,
    UseModifyHealAmount                     = 1L << 31,
    UseModifyShieldAmount                   = 1L << 32,
    UseOnSkill                              = 1L << 33, // 변경 가능
    StatGoldGainIndividual                  = 1L << 34,
    StatExpGainIndividual                   = 1L << 35,
    StatDungeonAttackDamageRate             = 1L << 36,
    StatStatusDamageRate                    = 1L << 37,
    StatHeroExpGainIndividual               = 1L << 38,
    StatItemDropIndividual                  = 1L << 39,
    MAX                                     = 1L << 40,
    All                                     = ~(-1L << 40)
};
