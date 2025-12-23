using System.ComponentModel;
using CookApps.BattleSystem;
using UnityEngine;
using UnityEngine.TextCore.Text;

public class CharacterStateIdleHealer : CharacterStateIdle
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

        // 2. 타겟 찾기 (힐할 캐릭터 우선, 없으면 공격 타겟)
        if (characCtrl.Target == null)
        {
            characCtrl.Target = InGameObjectManager.Instance.GetLowestHPOurTeam(characCtrl);
        }

        // 힐할 캐릭터가 없으면 공격 타겟으로 전환
        if (characCtrl.Target == null || !InGameObjectManager.Instance.IsInRange(characCtrl, characCtrl.Target))
        {
            characCtrl.Target = InGameObjectManager.Instance.GetOptimalAttackTarget(characCtrl);
        }

        // 3. 타겟 처리 (범위 체크 및 상태 전환/이동)
        HandleTarget();

        return CharacterStateRunningResult.CanCallAllWithoutMove;
    }

    /// <summary>
    /// 타겟이 있을 때 범위 체크 후 공격 상태로 전환하거나 이동
    /// </summary>
    private void HandleTarget()
    {
        if (characCtrl.Target == null)
            return;

        var isInRange = InGameObjectManager.Instance.IsInRange(characCtrl, characCtrl.Target);

        if (isInRange)
        {
            characCtrl.AddNextState<CharacterStateAttack>();
        }
        else
        {
            characCtrl.MoveToCharacter(isInRange, characCtrl.Target);
        }
    }
}
