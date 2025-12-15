using System;
using CookApps.AutoBattler;
using CookApps.Obfuscator;
using CookApps.BattleSystem;
using UnityEngine;
/// <summary>
/// 아군 전체 보호막 생성
/// HP {0}% 및 방어력 {0}% 증가합니다.
/// </summary>
[UseEffectCodeIds(CodeId)]
public partial class EffectCodeSynergyElementWater : EffectCodeSynergyBase
{
    public const int CodeId = 100501;
    private ObfuscatorFloat _hpValue;
    private ObfuscatorFloat _defValue;

    public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container, IEffectCodeSource source)
    {
        base.Initialize(codeInfo, container, source);
        _hpValue = codeInfo.GetCodeStatToFloat(0);
        _defValue = codeInfo.GetCodeStatToFloat(1);

        Span<double> eccStats = stackalloc double[1];
        eccStats.Clear();
        eccStats[0] = _hpValue;

        EffectCodeHelper.AddOrMergeEffectCode(EffectCodeNameType.HP_PERCENT_UP, owner, eccStats, source);
        base.AddSynergyAddEffectCodeIds(EffectCodeNameType.HP_PERCENT_UP);

        eccStats[0] = _defValue;
        EffectCodeHelper.AddOrMergeEffectCode(EffectCodeNameType.DEF_PERCENT_UP, owner, eccStats, source);
        base.AddSynergyAddEffectCodeIds(EffectCodeNameType.DEF_PERCENT_UP);

        eccStats[0] = _defValue;
        EffectCodeHelper.AddOrMergeEffectCode(EffectCodeNameType.RES_PERCENT_UP, owner, eccStats, source);
        base.AddSynergyAddEffectCodeIds(EffectCodeNameType.RES_PERCENT_UP);
        Debug.LogColor($"물시너지 HP {_hpValue}% 방어력 {_defValue}% 증가", "green");

    }

    public override void Merge(EffectCodeInfo codeInfo, IEffectCodeSource source)
    {
        base.Merge(codeInfo, source);
        _hpValue = codeInfo.GetCodeStatToFloat(0);
        _defValue = codeInfo.GetCodeStatToFloat(1);
    }

    public override void OnPreRemoved()
    {
        base.OnPreRemoved();
    }
}
