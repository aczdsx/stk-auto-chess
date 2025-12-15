using System;
using CookApps.AutoBattler;
using CookApps.Obfuscator;
using CookApps.BattleSystem;
using UnityEngine;

/// <summary>
// 공격속도 {0}% 회피율이 {0}% 증가합니다.
/// </summary>
[UseEffectCodeIds(CodeId)]
public partial class EffectCodeSynergyElementWind : EffectCodeSynergyBase
{
    public const int CodeId = 100201;
    private ObfuscatorFloat _statValue;

    public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container, IEffectCodeSource source)
    {
        base.Initialize(codeInfo, container, source);
        _statValue = codeInfo.GetCodeStatToFloat(0);

        Span<double> increaseStat = stackalloc double[1];
        increaseStat.Clear();
        increaseStat[0] = _statValue;
        EffectCodeHelper.AddOrMergeEffectCode(EffectCodeNameType.ATK_SPEED_PERCENT_UP, owner, increaseStat, source);
        base.AddSynergyAddEffectCodeIds(EffectCodeNameType.ATK_SPEED_PERCENT_UP);
        Debug.LogColor($"바람시너지 공격속도 {_statValue}% 증가", "green");
    }

    public override void Merge(EffectCodeInfo codeInfo, IEffectCodeSource source)
    {
        base.Merge(codeInfo, source);
        _statValue = codeInfo.GetCodeStatToFloat(0);
    }

    public override void OnPreRemoved()
    {
        base.OnPreRemoved();
    }
}
