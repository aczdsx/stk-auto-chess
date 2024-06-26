using System;
using CookApps.Obfuscator;
using CookApps.BattleSystem;

/// <summary>
///저격수 타입 캐릭터 공격 시 스킬 쿨타임 감소
/// </summary>
[UseEffectCodeIds(CodeId)]
public class EffectCodeSynergyPositionRanger : EffectCodeCharacterBase
{
    public const int CodeId = 210301;
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

    public override void OnCritical()
    {
        base.OnCritical();

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
