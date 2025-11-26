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
    private Tween moveTween;
    
    public override StatePriority StatePriority => StatePriority.Move;
    
    public override void StateStart()
    {
        base.StateStart();
        isBlockingChangeState = true;
        characCtrl.GetCharacterView().PlayAnimation(AnimationKey.MOVE);

        var moveDuration = SpecOptionCache.DefaultMoveDuration / characCtrl.GetCharacterStat().MoveSpeed;
        var jumpHeight = SpecOptionCache.CharacterMoveJumpHeight;
        // PrimeTweenExtensions.Jump(characCtrl.GetCharacterView().CachedTr, characCtrl.CurrentTile.View.Position, moveDuration, jumpHeight)
        //     .OnComplete(this, target => target.ChangeToIdleState());

        Ease ease = Ease.Linear;
        // if (characCtrl.SpecCharacter.atk_range == 1)
        // {
        //     ease = (isInRange) ? Ease.InQuad  : Ease.Linear;
        //     if (isInRange)
        //         moveDuration *= 0.2f;
        // }

        moveTween = Tween.Custom(
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

                if (characCtrl.IsAlive == false)
                {
                    Debug.LogColor($"[TEST] : {characCtrl.SpecCharacter.prefab_id} 캐릭터가 죽어서 이동 중단");

                    return;
                }

                characCtrl.Target = InGameObjectManager.Instance.GetOptimalAttackTarget(characCtrl);
                var isInRange = InGameObjectManager.Instance.IsInRange(characCtrl, characCtrl.Target);
                characCtrl.MoveToCharacter(isInRange, characCtrl.Target);
                isBlockingChangeState = false;
            }
        });
    }

    public override CharacterStateRunningResult CharacterStateRunning(float dt)
    {
        return CharacterStateRunningResult.CanCallAllWithoutActivate;
    }

    public override void StateEnd(bool isForced)
    {
        characCtrl.OnTileMoveEnd();
        base.StateEnd(isForced);
        moveTween.Stop();
    }
}
