using System;
using CookApps.AutoBattler;
using CookApps.BattleSystem;
using CookApps.Obfuscator;

/// <summary>
// 공격속도 {0}% 회피율이 {0}% 증가합니다.
/// </summary>
[UseEffectCodeIds(CodeId)]
public partial class EffectCodeSynergyElementWind : EffectCodeSynergyBase
{
    public const int CodeId = 100201;
    private ObfuscatorFloat _statValue;

    private ObfuscatorFloat _avoidValue;

    public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container, IEffectCodeSource source)
    {
        _synergyType = SynergyType.WIND;
        base.Initialize(codeInfo, container, source);
        _statValue = codeInfo.GetCodeStatToFloat(0);
        _avoidValue = codeInfo.GetCodeStatToFloat(1);

        AddSynergyAddEffectCodeIds();
        
        Debug.LogColor($"바람시너지 공격속도 {_statValue}% 증가", "green");
    }

    public override void Merge(EffectCodeInfo codeInfo, IEffectCodeSource source)
    {
        _synergyType = SynergyType.WIND;
        base.Merge(codeInfo, source);
        _statValue = codeInfo.GetCodeStatToFloat(0);
        _avoidValue = codeInfo.GetCodeStatToFloat(1);

        base.RemoveSynergyAddEffectCodeIds((long)EffectCodeNameType.ATK_SPEED_PERCENT_UP);
        base.RemoveSynergyAddEffectCodeIds((long)EffectCodeNameType.AVOID_PROB_PERCENT_UP);

        AddSynergyAddEffectCodeIds();
    }

    private void AddSynergyAddEffectCodeIds()
    {
        Span<double> increaseStat = stackalloc double[1];
        increaseStat.Clear();
        increaseStat[0] = _statValue * 0.01f;
        EffectCodeHelper.AddOrMergeEffectCode(EffectCodeNameType.ATK_SPEED_PERCENT_UP, owner, increaseStat, source);
        base.AddSynergyAddEffectCodeIds(EffectCodeNameType.ATK_SPEED_PERCENT_UP);

        increaseStat[0] = _avoidValue * 0.01f;
        EffectCodeHelper.AddOrMergeEffectCode(EffectCodeNameType.AVOID_PROB_PERCENT_UP, owner, increaseStat, source);
        base.AddSynergyAddEffectCodeIds(EffectCodeNameType.AVOID_PROB_PERCENT_UP);
    }

    public override void OnPreRemoved()
    {
        base.OnPreRemoved();
    }
}
