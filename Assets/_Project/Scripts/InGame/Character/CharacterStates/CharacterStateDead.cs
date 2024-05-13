using CookApps.BattleSystem;

public class CharacterStateDead : CharacterStateBase
{
    public override void StateStart()
    {
        base.StateStart();
        characCtrl.GetCharacterView().PlayAnimation(AnimationKey.DEAD);
    }

    public override CharacterStateRunningResult CharacterStateRunning(float dt)
    {
        return CharacterStateRunningResult.None;
    }

    public override void AnimationEventCallback(string animName, AnimationEventKey eventKey)
    {
        base.AnimationEventCallback(animName, eventKey);
        // Debug.Log($"dead AnimationEventCallback {animName}, {eventKey}");

        if (animName != AnimationKey.DEAD.ToAnimationName())
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
