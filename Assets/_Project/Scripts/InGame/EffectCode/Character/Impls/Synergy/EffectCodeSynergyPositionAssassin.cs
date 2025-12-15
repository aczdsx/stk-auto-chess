using System;
using CookApps.Obfuscator;
using CookApps.BattleSystem;

/// <summary>
///암살자 타입 캐릭터 적 처치 시 스킬 쿨타임 감소
/// </summary>
[UseEffectCodeIds(CodeId)]
public partial class EffectCodeSynergyPositionAssassin : EffectCodeSynergyBase
{
    public const int CodeId = 210401;
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
        foreach (var skillID in owner.SpecCharacter.skill_ids)
        {
            EffectCodeCharacterBase eccBase =
                (EffectCodeCharacterBase)owner.GetEffectCodeContainer().GetEffectCode(skillID);
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
    public override void OnPreRemoved()
    {
        base.OnPreRemoved();
    }
}
