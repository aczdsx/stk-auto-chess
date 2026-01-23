using System;
using System.Collections.Generic;
using CookApps.AutoBattler;
using CookApps.BattleSystem;

public static class EffectCodeHelper
{
    static readonly HashSet<EffectCodeNameType> _immuneTypes = new HashSet<EffectCodeNameType>
    {
            EffectCodeNameType.CC_STUN,
            EffectCodeNameType.CC_KNOCKBACK,
            EffectCodeNameType.CC_AIRBORNE,
            EffectCodeNameType.CC_MISA_RESTRAINT,
            EffectCodeNameType.CHAPTER_ICE,
            EffectCodeNameType.CHAPTER_SANDSTORM,
            EffectCodeNameType.CHAPTER_RANDOM_MOVE,
            EffectCodeNameType.DEBUFF_ATK_SPEED_DOWN,
            EffectCodeNameType.DEBUFF_COOL_DOWN_SPEED_PERCENT_DOWN,
            EffectCodeNameType.DEBUFF_AD_PERCENT_DOWN,
            EffectCodeNameType.DEBUFF_FIRE,
            EffectCodeNameType.DEBUFF_ICE,
            EffectCodeNameType.DEBUFF_SILENCE,
            EffectCodeNameType.DEBUFF_AD_DOWN,
            EffectCodeNameType.DEBUFF_MISA,
            EffectCodeNameType.DEBUFF_AIRBORNE,
            EffectCodeNameType.DEBUFF_AD_REDUCE_PERCENT_DOWN,
            EffectCodeNameType.DEBUFF_HEAL_RATE_DOWN,
    };

    // 강제 제거 불가능한 CC 타입
    static readonly HashSet<EffectCodeNameType> _cantForceRemoveCCTypes = new HashSet<EffectCodeNameType>
    {
        EffectCodeNameType.CC_KNOCKBACK,
        EffectCodeNameType.CC_AIRBORNE,
    };

    public static EffectCodeBase AddOrMergeEffectCode(EffectCodeNameType effectCodeNameType, CharacterController targetCharacter, Span<double> stats, IEffectCodeSource source)
    {
        var effectCodeContainer = targetCharacter.GetEffectCodeContainer();
        if (effectCodeContainer == null)
            return null;

        bool isImmuneType = _immuneTypes.Contains(effectCodeNameType);
        bool hasImmune = targetCharacter.HasBuffDebuffType(BuffDebuffType.Immune);

        // 면역 체크: 면역 타입이고 면역 버프가 있으면 이펙트 코드를 적용하지 않음
        if (isImmuneType && hasImmune)
        {
            var effectcodes = effectCodeContainer.GetCharacterEffectCodesByFlag(EffectCodeInheritFlag.UseOnCanceledCC);
            EffectCodeForLoopHelper.CallWithArgs(effectcodes, EffectCodeCharacterLambda.CallOnCanceledCCLambda, targetCharacter, effectCodeNameType);
            return null;
        }
        
        // 이펙트 코드 적용
        var effectCodeInfo = new EffectCodeInfo((long)effectCodeNameType, 0, stats);
        return effectCodeContainer.AddOrMergeEffectCode(effectCodeInfo, source);
    }

    public static void RemoveAllDebuff(CharacterController targetCharacter)
    {
        var ecc = targetCharacter.GetEffectCodeContainer();
        var debuffs = ecc.GetEffectCodesByType(EffectCodeType.Debuff);
        foreach (var debuff in debuffs)
        {
            ecc.RemoveEffectCode(debuff.CodeId);
        }
    }

    public static void RemoveAllCrowdControl(CharacterController targetCharacter)
    {
        var ecc = targetCharacter.GetEffectCodeContainer();
        var crowdControls = ecc.GetEffectCodesByType(EffectCodeType.CrowdControl);
        foreach (var crowdControl in crowdControls)
        {
            if (_cantForceRemoveCCTypes.Contains((EffectCodeNameType)crowdControl.CodeId))
            {
                continue;
            }
            ecc.RemoveEffectCode(crowdControl.CodeId);
        }
    }
}
