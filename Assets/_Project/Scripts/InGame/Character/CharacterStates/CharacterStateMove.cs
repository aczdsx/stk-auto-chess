using CookApps.AutoBattler;
using CookApps.BattleSystem;
using PrimeTween;
using UnityEngine;

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
        characCtrl.GetCharacterView().PlayAnimation(AnimationKey.MOVE);

        var moveDuration = SpecOptionCache.DefaultMoveDuration / characCtrl.GetCharacterStat().MoveSpeed;
        moveDuration = 1; // [TODO] 임시 moveDuration 나중에 삭제
        var jumpHeight = SpecOptionCache.CharacterMoveJumpHeight;
        // [TODO] Jump 동작을 제대로 안합니다 ㅠ
        // PrimeTweenExtensions.Jump(characCtrl.GetCharacterView().CachedTr, characCtrl.CurrentTile.View.Position, moveDuration, jumpHeight)
        //     .OnComplete(this, target => target.ChangeToIdleState());

        Tween.Custom(
            characCtrl.Position3D,
            characCtrl.CurrentTile.View.Position,
            moveDuration,
            (Vector3 value) =>
            {
                if (characCtrl != null)
                    characCtrl.Position3D = value;
            }).OnComplete(this, target => target.ChangeToIdleState());
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
