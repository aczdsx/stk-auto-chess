using CookApps.AutoBattler;
using CookApps.BattleSystem;
using LitMotion;
using UnityEngine;

/// <summary>
/// 1칸 이동하는 상태
/// 이동 후 idle로 전환
/// </summary>
public class CharacterStateForceMove : CharacterStateBase
{
    private MotionHandle _moveTween;
    private float? customMoveSpeed = null; // 커스텀 이동 속도 (null이면 기본값 사용)

    public override StatePriority StatePriority => StatePriority.Move;

    public override void SetStateData(object moveSpeed)
    {
        if (moveSpeed is float speed)
        {
            customMoveSpeed = speed;
        }
        else if (moveSpeed is int speedInt)
        {
            customMoveSpeed = speedInt;
        }
    }

    public override void StateStart()
    {
        base.StateStart();
        isBlockingChangeState = true;
        characCtrl.GetCharacterView().PlayAnimation(AnimationKey.MOVE);

        // 커스텀 이동 속도가 설정되어 있으면 사용, 없으면 캐릭터의 기본 이동 속도 사용
        float moveSpeed = customMoveSpeed ?? characCtrl.GetCharacterStat().MoveSpeed;
        var moveDuration = SpecOptionCache.DefaultMoveDuration / moveSpeed;

        Ease ease = Ease.Linear;
        // if (characCtrl.SpecCharacter.atk_range == 1)
        // {
        //     ease = (isInRange) ? Ease.InQuad  : Ease.Linear;
        //     if (isInRange)
        //         moveDuration *= 0.2f;
        // }

        _moveTween = LMotion.Create(
            characCtrl.Position3D,
            characCtrl.CurrentTile.View.Position,
            moveDuration)
            .WithEase(ease)
            .WithOnComplete(() =>
            {
                characCtrl.AddNextState<CharacterStateReady>();
                isBlockingChangeState = false;
            })
            .Bind(value =>
            {
                if (characCtrl != null)
                {
                    characCtrl.Position3D = value;
                }
            });
    }

    public override CharacterStateRunningResult CharacterStateRunning(float dt)
    {
        if (characCtrl.NeedToBeCrowdControlState())
        {
            // cc기 당했다면 cc상태로 변환 인데 isblockingchangestate 를 false 로 해야함
            isBlockingChangeState = false;
            characCtrl.AddNextState<CharacterStateCC>();
            return CharacterStateRunningResult.CanCallEffectCodeOnUpdateAndOnCooltime;
        }
        return CharacterStateRunningResult.CanCallAllWithoutActivate;
    }

    public override void StateEnd(bool isForced)
    {
        characCtrl.OnTileMoveEnd();
        base.StateEnd(isForced);
        _moveTween.TryCancel();
        _moveTween = default;
    }
}
