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
    /// 힐러 전용 타겟 찾기 (힐할 캐릭터 우선, 없으면 공격 타겟)
    /// </summary>
    public new static CookApps.BattleSystem.CharacterController FindTarget(CookApps.BattleSystem.CharacterController characCtrl)
    {
        CookApps.BattleSystem.CharacterController target = null;
        // 힐할 캐릭터 찾기
        var healTarget = InGameObjectManager.Instance.GetLowestHPOurTeam(characCtrl);
        if (healTarget != null && InGameObjectManager.Instance.IsInRange(characCtrl, healTarget, 2))
        {
            target = healTarget;
        }
        else
        {
            target = InGameObjectManager.Instance.GetOptimalAttackTarget(characCtrl);
        }

        if (target.AllianceType == characCtrl.AllianceType)
        {
            Debug.Log("Healer!! HealMode");
        }
        else
        {
            Debug.Log("Healer!! AttackMode");
        }

        // 힐할 캐릭터가 없거나 범위 밖이면 공격 타겟 반환
        return target;
    }
}
