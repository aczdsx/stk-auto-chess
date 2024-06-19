using System;
using CookApps.AutoBattler;
using CookApps.Obfuscator;
using CookApps.BattleSystem;

/// <summary>
// 적군 스킬 쿹타임 감소 속도 감소
// 적군 스킬 쿹타임 감소 속도 {0}%감소
/// </summary>
[UseEffectCodeIds(CodeId)]
public class EffectCodeSynergyElementDark : EffectCodeCharacterBase
{
    public const int CodeId = 220401;
    private ObfuscatorFloat _statValue;

    public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container, IEffectCodeSource source)
    {
        base.Initialize(codeInfo, container, source);
        _statValue = codeInfo.GetCodeStatToFloat(0);
    }

    public override void Merge(EffectCodeInfo codeInfo, IEffectCodeSource source)
    {
        base.Merge(codeInfo, source);
        _statValue = codeInfo.GetCodeStatToFloat(0);
    }

    // [TODO] 적군감소????
}
