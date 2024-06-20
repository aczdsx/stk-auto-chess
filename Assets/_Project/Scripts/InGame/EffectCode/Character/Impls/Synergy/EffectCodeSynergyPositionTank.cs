using System;
using CookApps.Obfuscator;
using CookApps.BattleSystem;

/// <summary>
///탱커 타입 캐릭터 피격 시 스킬 쿨타임 감소
/// </summary>
[UseEffectCodeIds(CodeId)]
public class EffectCodeSynergyPositionTank : EffectCodeCharacterBase
{
    public const int CodeId = 210101;
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

    // [TODO] 피격 당했을 시
    public override double OnDamaged(double damageAmount,  CharacterController attacker, bool isPure)
    {
        base.OnDamaged(damageAmount, attacker, isPure);

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

        return damageAmount;
    }
}
