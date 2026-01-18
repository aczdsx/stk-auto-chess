using CookApps.BattleSystem;

public class CharacterStateReady : CharacterStateBase
{
    private const float ScanTargetInterval = 0.1f;
    private float scanTargetTime = 0f;

    public override StatePriority StatePriority => StatePriority.Ready;
    
    public override void StateStart()
    {
        base.StateStart();
        characCtrl.GetCharacterView().PlayAnimation(AnimationKey.IDLE);
    }

    public override CharacterStateRunningResult CharacterStateRunning(float dt)
    {
        return CharacterStateRunningResult.CanCallMove;
    }
}
