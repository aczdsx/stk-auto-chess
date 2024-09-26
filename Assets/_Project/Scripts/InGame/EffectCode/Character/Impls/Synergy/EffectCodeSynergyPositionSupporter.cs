using System;
using CookApps.Obfuscator;
using CookApps.BattleSystem;

/// <summary>
///서포터 타입 스킬 사용 공격력 가장 높은 아군 쿨타임 감소
/// </summary>
[UseEffectCodeIds(CodeId)]
public partial class EffectCodeSynergyPositionSupporter : EffectCodeCharacterBase
{
    public const int CodeId = 210501;
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

    public override void OnCombatStart()
    {
        base.OnCombatStart();

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
}
