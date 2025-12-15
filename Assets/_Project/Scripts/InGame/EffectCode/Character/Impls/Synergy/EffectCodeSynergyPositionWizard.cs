using System;
using CookApps.Obfuscator;
using CookApps.BattleSystem;

/// <summary>
///마법사 타입 캐릭터 스킬 쿨타임 감소 속도 증가
/// </summary>
[UseEffectCodeIds(CodeId)]
public partial class EffectCodeSynergyPositionWizard : EffectCodeSynergyBase
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

        foreach (var skillID in owner.SpecCharacter.skill_ids)
        {
            EffectCodeCharacterBase eccBase =
                (EffectCodeCharacterBase) owner.GetEffectCodeContainer().GetEffectCode(skillID);
            if (eccBase != null)
            {
                if (!eccBase.IsSkillActivated)
                {
                    float durationTime = eccBase.GetDurationTime();
                    float newElapsedTime = durationTime * statValue;
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
