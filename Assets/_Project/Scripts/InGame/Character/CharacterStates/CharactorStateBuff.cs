using CookApps.BattleSystem;
using UnityEngine;

public class CharacterStateBuff : CharacterStateBase
{
    private EffectCodeCharacterBase effectCode;
    private int skillIndex;
    private AnimationKey skillAnimationKey;
    private bool isSkillEnd;
    private int skillExecuteIndex;


    public override void SetStateData(object effectCode)
    {
        this.effectCode = effectCode as EffectCodeCharacterBase;
    }

    public override StatePriority StatePriority => StatePriority.Buff;

    public override void StateStart()
    {
        base.StateStart();
        AnimationClip clip = characCtrl.GetCharacterView().PlayAnimation(AnimationKey.BUFF);
        Debug.Log($"{characCtrl.CharacterId} : AnimationClip clip = characCtrl.GetCharacterView().PlayAnimation(AnimationKey.BUFF);");
    }

    public override void AnimationEventCallback(AnimationKey animName, AnimationEventKey eventKey)
    {
        // if (animName != skillAnimationKey)
        //     return;

        // if (eventKey is > AnimationEventKey.VFXStart and < AnimationEventKey.VFXEnd)
        // {
        //     effectCode.OnSkillVFX(eventKey - AnimationEventKey.VFX1);
        //     return;
        // }

        // if (eventKey is > AnimationEventKey.ExecuteStart and < AnimationEventKey.ExecuteEnd)
        // {
        //     effectCode.OnSkillExecute(skillExecuteIndex++, eventKey - AnimationEventKey.Execute1Per1);
        //     return;
        // }

        // if (eventKey == AnimationEventKey.End)
        // {
        //     isSkillEnd = true;
        // }
    }
}