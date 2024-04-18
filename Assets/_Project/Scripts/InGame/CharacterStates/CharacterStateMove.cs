using CookApps.SampleTeamBattle;
using CookApps.TeamBattle.BattleSystem;

/// <summary>
/// 1칸 이동하는 상태
/// 이동 후 idle로 전환
/// </summary>
public class CharacterStateMove : CharacterStateBase
{
    private bool isMoving;

    public override void StateStart()
    {
        base.StateStart();
        characCtrl.GetCharacterView().PlayAnimation(AnimationKey.Walk);
        var moveDuration = SpecOptionCache.DefaultMoveDuration / characCtrl.GetCharacterStat().MoveSpeed;
        var jumpHeight = SpecOptionCache.CharacterMoveJumpHeight;
        PrimeTweenExtensions.Jump(characCtrl.GetCharacterView().CachedTr, characCtrl.CurrentTile.View.Position, moveDuration, jumpHeight)
            .OnComplete(this, target => target.ChangeToIdleState());
    }

    public override CharacterStateRunningResult CharacterStateRunning(float dt)
    {
        return CharacterStateRunningResult.CanCallAllWithoutActivate;
    }

    private void ChangeToIdleState()
    {
        characCtrl.AddNextState<CharacterStateIdle>();
    }
}
