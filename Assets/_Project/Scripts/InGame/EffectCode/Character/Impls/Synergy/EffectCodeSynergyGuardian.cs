using System;
using CookApps.Obfuscator;
using CookApps.BattleSystem;

/// <summary>
///가디언 타입 캐릭터 공격 시 쿨타임 감소
/// </summary>
[UseEffectCodeIds(CodeId)]
public class EffectCodeSynergyGuardian : EffectCodeCharacterBase
{
    public const int CodeId = 210001;
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

    public override void OnAttack()
    {
        base.OnAttack();
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
