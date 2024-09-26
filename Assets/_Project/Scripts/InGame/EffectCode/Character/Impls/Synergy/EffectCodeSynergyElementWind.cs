using System;
using CookApps.AutoBattler;
using CookApps.Obfuscator;
using CookApps.BattleSystem;

/// <summary>
///대지 시너지만 적용
// (물리, 마법) 방어력 증가   
/// </summary>
[UseEffectCodeIds(CodeId)]
public partial class EffectCodeSynergyElementWind : EffectCodeCharacterBase
{
    public const int CodeId = 220501;
    private ObfuscatorFloat _statValue;

    public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container, IEffectCodeSource source)
    {
        base.Initialize(codeInfo, container, source);
        _statValue = codeInfo.GetCodeStatToFloat(0);
        
        Span<double> buffStats = stackalloc double[3];
        buffStats.Clear();
        buffStats[0] = codeId;
        buffStats[1] = 999f;
        buffStats[2] = _statValue;
        var effectCodeID = new EffectCodeInfo((long)EffectCodeNameType.BUFF_ATK_SPEED_UP, 0, buffStats);
        owner.GetEffectCodeContainer().AddOrMergeEffectCode(effectCodeID, source);
    }

    public override void Merge(EffectCodeInfo codeInfo, IEffectCodeSource source)
    {
        base.Merge(codeInfo, source);
        _statValue = codeInfo.GetCodeStatToFloat(0);
    }
}
