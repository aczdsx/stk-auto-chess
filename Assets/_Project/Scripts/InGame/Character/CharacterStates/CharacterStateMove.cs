using CookApps.AutoBattler;
using CookApps.BattleSystem;
using LitMotion;
using UnityEngine;

/// <summary>
/// 1칸 이동하는 상태
/// 이동 후 idle로 전환
/// </summary>
public class CharacterStateMove : CharacterStateBase
{
    private MotionHandle _moveTween;

    public override StatePriority StatePriority => StatePriority.Move;

    public override void StateStart()
    {
        base.StateStart();
        isBlockingChangeState = true;
        characCtrl.GetCharacterView().PlayAnimation(AnimationKey.MOVE);

        var moveDuration = SpecOptionCache.DefaultMoveDuration / characCtrl.GetCharacterStat().MoveSpeed;

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
                if (characCtrl == null)
                    return;

                if (characCtrl.IsAlive == false)
                {
                    Debug.LogColor($"[TEST] : {characCtrl.SpecCharacter.prefab_id} 캐릭터가 죽어서 이동 중단");

                    return;
                }
                characCtrl.Target = characCtrl.FindTarget();
                var isInRange = InGameObjectManager.Instance.IsInRange(characCtrl, characCtrl.Target);
                characCtrl.MoveToCharacter(isInRange, characCtrl.Target);
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
