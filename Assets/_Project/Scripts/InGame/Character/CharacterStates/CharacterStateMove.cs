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
        var jumpHeight = SpecOptionCache.CharacterMoveJumpHeight;
        // PrimeTweenExtensions.Jump(characCtrl.GetCharacterView().CachedTr, characCtrl.CurrentTile.View.Position, moveDuration, jumpHeight)
        //     .OnComplete(this, target => target.ChangeToIdleState());

        var isInRange = InGameObjectManager.Instance.IsInRange(characCtrl, characCtrl.Target);
        Ease ease = (isInRange) ? Ease.InQuad  : Ease.Linear;

        Tween.Custom(
            characCtrl.Position3D,
            characCtrl.CurrentTile.View.Position,
            moveDuration,
            (Vector3 value) =>
            {
                if (characCtrl != null)
                    characCtrl.Position3D = value;
            },
            ease: ease).OnComplete(this, target =>
        {
            if (target != null)
            {
                if (characCtrl == null)
                    return;

                characCtrl.MoveCharacter(isInRange);
            }
        });
    }

    public override CharacterStateRunningResult CharacterStateRunning(float dt)
    {
        return CharacterStateRunningResult.CanCallAllWithoutActivate;
    }
}
