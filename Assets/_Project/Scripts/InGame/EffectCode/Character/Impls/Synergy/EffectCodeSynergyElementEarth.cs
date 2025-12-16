using System;
using CookApps.AutoBattler;
using CookApps.Obfuscator;
using CookApps.BattleSystem;
using UnityEngine;
/// <summary>
// 물리 관통력{0}%, 마법 관통력 및 블럭율 증가 {0}% 근데 아직 블럭율 수치가 없음.
/// </summary>
[UseEffectCodeIds(CodeId)]
public partial class EffectCodeSynergyElementEarth : EffectCodeSynergyBase
{
    public const int CodeId = 100301;
    private ObfuscatorFloat _PierceValue;
    private ObfuscatorFloat _blockRateValue;

    public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container, IEffectCodeSource source)
    {
        base.Initialize(codeInfo, container, source);
        _PierceValue = codeInfo.GetCodeStatToFloat(0);
        _blockRateValue = codeInfo.GetCodeStatToFloat(1);

        Span<double> buffStats = stackalloc double[1];
        buffStats.Clear();

        buffStats[0] = _PierceValue;
        EffectCodeHelper.AddOrMergeEffectCode(EffectCodeNameType.AD_PIERCE_PERCENT_UP, owner, buffStats, source);
        base.AddSynergyAddEffectCodeIds(EffectCodeNameType.AD_PIERCE_PERCENT_UP);

        buffStats[0] = _PierceValue;
        EffectCodeHelper.AddOrMergeEffectCode(EffectCodeNameType.AP_PIERCE_PERCENT_UP, owner, buffStats, source);
        base.AddSynergyAddEffectCodeIds(EffectCodeNameType.AP_PIERCE_PERCENT_UP);

        Debug.LogColor($"대지시너지 물리 관통력 {_PierceValue}% 마법 관통력 {_PierceValue}% 증가", "green");
    }

    public override void Merge(EffectCodeInfo codeInfo, IEffectCodeSource source)
    {
        base.Merge(codeInfo, source);
        _PierceValue = codeInfo.GetCodeStatToFloat(0);   //atk_pierce  //res_pierce
        _blockRateValue = codeInfo.GetCodeStatToFloat(1);
    }

    public override void OnPreRemoved()
    {
        base.OnPreRemoved();
    }

}
