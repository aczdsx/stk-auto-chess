using System;
using CookApps.AutoBattler;
using CookApps.BattleSystem;
using CookApps.TeamBattle;

public static class EffectCodeHelper
{
    public static void AddOrMergeEffectCode(EffectCodeNameType effectCodeNameType, CharacterController targetCharacter, Span<double> stats, IEffectCodeSource source)
    {
        var effectCodeInfo = new EffectCodeInfo((long)effectCodeNameType, 0, stats);
        targetCharacter.GetEffectCodeContainer().AddOrMergeEffectCode(effectCodeInfo, source);
    }
}
