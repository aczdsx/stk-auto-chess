using CookApps.BattleSystem;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class CharacterStateDead : CharacterStateBase
{
    public override void StateStart()
    {
        base.StateStart();
        AnimationClip clip = characCtrl.GetCharacterView().PlayAnimation(AnimationKey.DEAD);
        characCtrl.GetCharacterView().SetDeadSprite(clip);
        characCtrl.IsBlockChangeState = true;

        Transform skillRootTransform = characCtrl.GetCharacterView().SkillRootTransform;
        Transform playgroundTransform = InGameObjectManager.Instance.Playground;
        for (int i = skillRootTransform.childCount - 1; i >= 0; i--)
        {
            skillRootTransform.GetChild(i).parent = playgroundTransform;
        }
    }

    public override CharacterStateRunningResult CharacterStateRunning(float dt)
    {
        return CharacterStateRunningResult.None;
    }

    public override void AnimationEventCallback(AnimationKey animName, AnimationEventKey eventKey)
    {
        base.AnimationEventCallback(animName, eventKey);
        // Debug.Log($"dead AnimationEventCallback {animName}, {eventKey}");

        if (animName != AnimationKey.DEAD)
        {
            return;
        }

        if (eventKey == AnimationEventKey.End)
        {
            if (characCtrl.AllianceType == AllianceType.Enemy)
            {
                InGameObjectManager.Instance.RemoveEnemyFromField(characCtrl);
            }
            else
            {
                InGameObjectManager.Instance.RemoveCharacterFromField(characCtrl);
            }
        }
    }
}
