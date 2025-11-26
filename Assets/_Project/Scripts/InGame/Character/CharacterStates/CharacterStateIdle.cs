using CookApps.BattleSystem;
using UnityEngine;

public class CharacterStateIdle : CharacterStateBase
{
    protected const float ScanTargetInterval = 0.1f;
    protected float scanTargetTime = 0f;

    public override StatePriority StatePriority => StatePriority.Idle;

    public override void StateStart()
    {
        base.StateStart();
        characCtrl.GetCharacterView().PlayAnimation(AnimationKey.IDLE);
        scanTargetTime = ScanTargetInterval;
    }

    public override CharacterStateRunningResult CharacterStateRunning(float dt)
    {
        // 1. 캐릭터가 Idle 상태로 있어야 하는지 체크
        if (characCtrl.NeedToBeCrowdControlState())
        {
            characCtrl.AddNextState<CharacterStateCC>();
            return CharacterStateRunningResult.CanCallEffectCodeOnUpdateAndOnCooltime;
        }

        scanTargetTime -= dt;
        if (scanTargetTime > 0f)
        {
            return CharacterStateRunningResult.CanCallEffectCodeOnUpdateAndOnCooltime;
        }
        scanTargetTime = ScanTargetInterval;

        if (characCtrl.Target == null)
        {
            characCtrl.Target = InGameObjectManager.Instance.GetOptimalAttackTarget(characCtrl);
        }
        if (characCtrl.Target is {IsAlive: false})
        {
            characCtrl.Target = null;
            return CharacterStateRunningResult.CanCallAllWithoutMove;
        }

        // 3. 적이 공격 범위 안에 들어왔는지 체크
        if (characCtrl.Target != null)
        {
            var isInRange = InGameObjectManager.Instance.IsInRange(characCtrl, characCtrl.Target);

            if (isInRange)
            {
                // 4-1. 공격 범위 안에 들어왔다면 공격 상태로 전환
                characCtrl.AddNextState<CharacterStateAttack>();
            }
            else
            {
                characCtrl.MoveCharacter(isInRange, characCtrl.Target);
            }
        }

        return CharacterStateRunningResult.CanCallAllWithoutMove;
    }
}
