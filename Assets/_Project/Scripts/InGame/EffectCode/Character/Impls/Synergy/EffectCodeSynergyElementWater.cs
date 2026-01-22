using System;
using CookApps.AutoBattler;
using CookApps.BattleSystem;
using CookApps.Obfuscator;

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
        _synergyType = SynergyType.WATER;
        base.Initialize(codeInfo, container, source);
        _hpValue = codeInfo.GetCodeStatToFloat(0);
        _defValue = codeInfo.GetCodeStatToFloat(1);

        AddSynergyAddEffectCodeIds();

    }

    public override void Merge(EffectCodeInfo codeInfo, IEffectCodeSource source)
    {
        _synergyType = SynergyType.WATER;
        base.Merge(codeInfo, source);
        _hpValue = codeInfo.GetCodeStatToFloat(0);
        _defValue = codeInfo.GetCodeStatToFloat(1);

        base.RemoveSynergyAddEffectCodeIds((long)EffectCodeNameType.HP_PERCENT_UP);
        base.RemoveSynergyAddEffectCodeIds((int)EffectCodeNameType.DEF_PERCENT_UP);
        AddSynergyAddEffectCodeIds();
    }


    void AddSynergyAddEffectCodeIds()
    {
        Span<double> eccStats = stackalloc double[1];
        eccStats.Clear();
        eccStats[0] = _hpValue * 0.01f;

        EffectCodeHelper.AddOrMergeEffectCode(EffectCodeNameType.HP_PERCENT_UP, owner, eccStats, source);
        base.AddSynergyAddEffectCodeIds(EffectCodeNameType.HP_PERCENT_UP);
        owner.SetMaxHealth();

        eccStats[0] = _defValue * 0.01f;
        var effectCodeInfo = new EffectCodeInfo((int)EffectCodeNameType.DEF_PERCENT_UP, 0, eccStats);
        owner.GetEffectCodeContainer().AddOrMergeEffectCode(effectCodeInfo, source);
        base.AddSynergyAddEffectCodeIds((int)EffectCodeNameType.DEF_PERCENT_UP);
        Debug.LogColor($"물시너지 HP {_hpValue}% 방어력 {_defValue}% 증가", "green");
    }

    public override void OnPreRemoved()
    {
        base.OnPreRemoved();
    }
}
