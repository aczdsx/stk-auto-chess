using CookApps.BattleSystem;

public class CharacterStateCC : CharacterStateBase
{
    public override StatePriority StatePriority => StatePriority.CC;
    
    public override void StateStart()
    {
        base.StateStart();
        characCtrl.GetCharacterView().PlayAnimation(AnimationKey.DEAD, true);
        isBlockingChangeState = true;
    }

    public override CharacterStateRunningResult CharacterStateRunning(float dt)
    {
        if (!characCtrl.NeedToBeCrowdControlState())
        {
            isBlockingChangeState = false;
            characCtrl.AddNextState<CharacterStateIdle>();
        }
        return CharacterStateRunningResult.CanCallEffectCodeOnUpdateAndOnCooltime;
    }
}
