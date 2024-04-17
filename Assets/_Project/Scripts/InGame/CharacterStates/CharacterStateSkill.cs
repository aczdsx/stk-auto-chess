using CookApps.SampleTeamBattle;
using CookApps.TeamBattle.BattleSystem;
using UnityEngine;

public class CharacterStateSkill : CharacterStateBase
{
    private EffectCodeCharacterBase effectCode;
    private int skillIndex;
    private AnimationKey skillAnimationKey;
    private bool isSkillEnd;
    private int skillExecuteIndex;

    public void SetEffectCode(EffectCodeCharacterBase effectCode)
    {
        this.effectCode = effectCode;
    }

    public override void StateInit(object target)
    {
        base.StateInit(target);

        isSkillEnd = false;

        var characId = characCtrl.GetCharacterStat().CharacterId;
        var specCharac = SpecDataManager.Instance.SpecCharacter.Get(characId);
        skillIndex = 0;
        var skillIds = specCharac.skill_ids;
        for (int i = 0; i < skillIds.Length; i++)
        {
            if (skillIds[i] == effectCode.CodeId)
            {
                skillIndex = i;
                break;
            }
        }
        skillAnimationKey = AnimationKey.Skill1 + skillIndex;
    }

    public override void StateStart()
    {
        base.StateStart();
        characCtrl.GetCharacterView().PlayAnimation(skillAnimationKey);
    }

    public override CharacterStateRunningResult CharacterStateRunning(float dt)
    {
        if (characCtrl.NeedToBeIdle())
        {
            characCtrl.AddNextState<CharacterStateIdle>();
            return CharacterStateRunningResult.CanCallEffectCodeOnUpdateAndOnCooltime;
        }

        if (isSkillEnd)
        {
            effectCode.OnSkillAnimationEnd();
            characCtrl.AddNextState<CharacterStateIdle>();
            return CharacterStateRunningResult.CanCallEffectCodeOnUpdateAndOnCooltime;
        }

        // 스킬 사용중에는 다른 이펙트코드들의 쿨타임이나 업데이트를 호출하지 않는다.
        return CharacterStateRunningResult.None;
    }

    public override void AnimationEventCallback(string animName, AnimationEventKey eventKey)
    {
        if (animName != skillAnimationKey.ToAnimationName())
            return;

        if (eventKey is > AnimationEventKey.VFXStart and < AnimationEventKey.VFXEnd)
        {
            effectCode.OnSkillVFX(eventKey - AnimationEventKey.VFX1);
            return;
        }

        if (eventKey is > AnimationEventKey.ExecuteStart and < AnimationEventKey.ExecuteEnd)
        {
            effectCode.OnSkillExecute(skillExecuteIndex++, eventKey - AnimationEventKey.Execute1Per1);
            return;
        }

        if (eventKey == AnimationEventKey.End)
        {
            isSkillEnd = true;
        }
    }
}
