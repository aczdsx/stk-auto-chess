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

    public override CharacterController.DamageInfo OnDamaged(CharacterController.DamageInfo damageAmount,  CharacterController attacker, bool isPure)
    {
        base.OnDamaged(damageAmount, attacker, isPure);

        if (damageAmount.isAD == false)
        {
            foreach (var skillID in owner.SpecCharacter.skill_ids)
            {
                EffectCodeCharacterBase eccBase =
                    (EffectCodeCharacterBase) owner.GetEffectCodeContainer().GetEffectCode(skillID);
                if (eccBase != null)
                {
                    if (!eccBase.IsSkillActivated)
                    {
                        float durationTime = eccBase.GetDurationTime();
                        float elapsedTime = eccBase.GetElapsedTime();
                        float decreasedTime = durationTime * statValue;
                        float newElapsedTime = elapsedTime + decreasedTime;
                        eccBase.SetElapsedTime(newElapsedTime);
                    }
                }
            }
        }

        return damageAmount;
    }
}
