using CookApps.BattleSystem;
using CookApps.Obfuscator;

/// <summary>
///탱커 타입 캐릭터 피격 시 스킬 쿨타임 감소
/// </summary>
[UseEffectCodeIds(CodeId)]
public partial class EffectCodeSynergyPositionTank : EffectCodeSynergyBase
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

    public override CharacterController.DamageInfo OnDamaged(CharacterController.DamageInfo damageAmount,  CharacterController attacker, bool isPure)
    {
        base.OnDamaged(damageAmount, attacker, isPure);

        if (damageAmount.isAD)
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
    public override void OnPreRemoved()
    {
        base.OnPreRemoved();
    }
}
