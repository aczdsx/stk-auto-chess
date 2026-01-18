using CookApps.BattleSystem;

/// <summary>
/// 가장 먼거리 타겟을 찾는 state입니다.
/// </summary>
public class CharacterStateIdleFarTarget : CharacterStateIdle
{
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

        // 2. 타겟 찾기
        if (characCtrl.Target == null)
        {
            characCtrl.Target = characCtrl.FindTarget();
        }


        if (characCtrl.Target is { IsAlive: false })
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
                characCtrl.MoveToCharacter(isInRange, characCtrl.Target);
            }
        }

        return CharacterStateRunningResult.CanCallAllWithoutMove;
    }

    /// <summary>
    /// 가장 먼거리 타겟 찾기
    /// </summary>
    public new static CookApps.BattleSystem.CharacterController FindTarget(CookApps.BattleSystem.CharacterController characCtrl)
    {
        var target = InGameObjectManager.Instance.GetFarthestTargetByManhattanDistance(characCtrl);

        return target;
    }
}
