using CookApps.BattleSystem;
using UnityEngine;

public class CharacterStateDead : CharacterStateBase
{
    public override StatePriority StatePriority => StatePriority.Dead;
    
    public override void StateStart()
    {
        base.StateStart();
        AnimationClip clip = characCtrl.GetCharacterView().PlayAnimation(AnimationKey.DEAD);
        if (characCtrl.SpecCharacter.prefab_id != 20403)
            characCtrl.GetCharacterView().SetDeadSprite(clip);
        isBlockingChangeState = true;

        Transform skillRootTransform = characCtrl.GetCharacterView().SkillRootTransform;
        Transform playgroundTransform = InGameObjectManager.Instance.Playground;
        for (int i = skillRootTransform.childCount - 1; i >= 0; i--)
        {
            skillRootTransform.GetChild(i).parent = playgroundTransform;
        }

        characCtrl.CurrentTile.SetUnoccupied();
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
            InGameObjectManager.Instance.RemoveCharacterFromField(characCtrl);
        }
    }
}
