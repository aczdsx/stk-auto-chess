using System;
using CookApps.AutoBattler;
using CookApps.Obfuscator;
using CookApps.BattleSystem;
using UnityEngine;
/// <summary>
/// 크리티컬 확률 {0}% 크리티컬 데미지 증가 {0}%
/// </summary>
[UseEffectCodeIds(CodeId)]
public partial class EffectCodeSynergyElementLightning : EffectCodeSynergyBase
{
    public const int CodeId = 100401;
    private ObfuscatorFloat _criticalRateValue;
    private ObfuscatorFloat _criticalPowerValue;

    public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container, IEffectCodeSource source)
    {
        base.Initialize(codeInfo, container, source);
        _criticalRateValue = codeInfo.GetCodeStatToFloat(0);
        _criticalPowerValue = codeInfo.GetCodeStatToFloat(1);

        AddSynergyAddEffectCodeIds();

        Debug.LogColor($"번개시너지 크리티컬 확률 {_criticalRateValue}% 크리티컬 데미지 {_criticalPowerValue}% 증가", "green");
    }

    public override void Merge(EffectCodeInfo codeInfo, IEffectCodeSource source)
    {
        base.Merge(codeInfo, source);
        _criticalRateValue = codeInfo.GetCodeStatToFloat(0);
        _criticalPowerValue = codeInfo.GetCodeStatToFloat(1);

        base.RemoveSynergyAddEffectCodeIds((long)EffectCodeNameType.CRIT_RATE_PERCENT_UP);
        base.RemoveSynergyAddEffectCodeIds((long)EffectCodeNameType.CRIT_POWER_PERCENT_UP);
        AddSynergyAddEffectCodeIds();   
    }

    private void AddSynergyAddEffectCodeIds()
    {
        Span<double> criticalStats = stackalloc double[1];
        criticalStats.Clear();
        criticalStats[0] = _criticalRateValue * 0.01f;

        EffectCodeHelper.AddOrMergeEffectCode(EffectCodeNameType.CRIT_RATE_PERCENT_UP, owner, criticalStats, source);
        base.AddSynergyAddEffectCodeIds(EffectCodeNameType.CRIT_RATE_PERCENT_UP);

        criticalStats[0] = _criticalPowerValue * 0.01f;
        EffectCodeHelper.AddOrMergeEffectCode(EffectCodeNameType.CRIT_POWER_PERCENT_UP, owner, criticalStats, source);
        base.AddSynergyAddEffectCodeIds(EffectCodeNameType.CRIT_POWER_PERCENT_UP);
    }

    public override void OnPreRemoved()
    {
        base.OnPreRemoved();
    }

}
