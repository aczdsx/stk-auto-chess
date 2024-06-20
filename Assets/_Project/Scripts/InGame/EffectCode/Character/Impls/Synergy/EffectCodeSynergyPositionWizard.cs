using System;
using CookApps.Obfuscator;
using CookApps.BattleSystem;

/// <summary>
///마법사 타입 캐릭터 스킬 쿨타임 감소 속도 증가
/// </summary>
[UseEffectCodeIds(CodeId)]
public class EffectCodeSynergyPositionWizard : EffectCodeCharacterBase
{
    public const int CodeId = 210201;
    private ObfuscatorFloat statValue;

    public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container, IEffectCodeSource source)
    {
        base.Initialize(codeInfo, container, source);
        statValue = codeInfo.GetCodeStatToFloat(0);
    }

    public override void Merge(EffectCodeInfo codeInfo, IEffectCodeSource source)
    {
        base.Merge(codeInfo, source);
        statValue = codeInfo.GetCodeStatToFloat(0);
    }

    public override void OnSkill(EffectCodeBase skillEffectCode)
    {
        base.OnSkill(skillEffectCode);

        var list = container.GetCharacterEffectCodesByFlag(EffectCodeInheritFlag.UseOnCooltime);
        foreach (var ec in list)
        {
            EffectCodeCharacterBase eccBase = ((EffectCodeCharacterBase) ec);
            float durationTime = eccBase.GetDurationTime();
            float elapsedTime = eccBase.GetDurationTime();
            float decreasedTime = durationTime * statValue;
            float newElapsedTime = Math.Max(0, elapsedTime - decreasedTime);
            eccBase.SetElapsedTime(newElapsedTime);
        }
    }
}
