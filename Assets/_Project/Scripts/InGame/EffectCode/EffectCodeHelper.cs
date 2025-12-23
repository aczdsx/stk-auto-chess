using System;
using System.Collections.Generic;
using CookApps.AutoBattler;
using CookApps.BattleSystem;
using CookApps.TeamBattle;

public static class EffectCodeHelper
{
    static readonly HashSet<EffectCodeNameType> _immuneTypes = new HashSet<EffectCodeNameType>
        {
            // EffectCodeNameType.TILE_BURN,
            EffectCodeNameType.STUN,
            EffectCodeNameType.KNOCKBACK,
            EffectCodeNameType.AIRBORNE,
            EffectCodeNameType.MISA_RESTRAINT,
            // EffectCodeNameType.CHAPTER_FIRE,
            EffectCodeNameType.CHAPTER_ICE,
            // EffectCodeNameType.CHAPTER_LANDMINE,
            EffectCodeNameType.CHAPTER_SANDSTORM,
            EffectCodeNameType.CHAPTER_RANDOM_MOVE,
            EffectCodeNameType.DEBUFF_ATK_SPEED_DOWN,
            EffectCodeNameType.DEBUFF_COOL_DOWN_SPEED_PERCENT_DOWN,
            EffectCodeNameType.DEBUFF_AD_PERCENT_DOWN,
            EffectCodeNameType.DEBUFF_FIRE,
            EffectCodeNameType.DEBUFF_ICE,
            EffectCodeNameType.DEBUFF_SILENCE,
            EffectCodeNameType.DEBUFF_AD_DOWN,
            EffectCodeNameType.DEBUFF_AIRBORNE,
            EffectCodeNameType.DEBUFF_AD_REDUCE_PERCENT_DOWN,
            EffectCodeNameType.DEBUFF_HEAL_RATE_DOWN,
        };
    public static void AddOrMergeEffectCode(EffectCodeNameType effectCodeNameType, CharacterController targetCharacter, Span<double> stats, IEffectCodeSource source)
    {
        bool isImmuneType = _immuneTypes.Contains(effectCodeNameType);
        bool hasImmuneBuff = targetCharacter.HasBuffDebuffType(BuffDebuffType.Immune) && isImmuneType;

        if (!hasImmuneBuff)
        {
            var effectCodeInfo = new EffectCodeInfo((long)effectCodeNameType, 0, stats);
            if (targetCharacter.GetEffectCodeContainer() != null)
                targetCharacter.GetEffectCodeContainer().AddOrMergeEffectCode(effectCodeInfo, source);
        }
    }
}
