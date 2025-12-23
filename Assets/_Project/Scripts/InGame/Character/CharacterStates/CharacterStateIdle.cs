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
            characCtrl.Target = FindTargetInstance();
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
    /// 타겟을 찾는 static 메서드 (인스턴스 없이 호출 가능)
    /// CharacterController의 상태 타입 맵을 확인하여 적절한 타겟 찾기 로직 사용
    /// </summary>
    public static CookApps.BattleSystem.CharacterController FindTarget(CookApps.BattleSystem.CharacterController characCtrl)
    {
        // CharacterController가 가질 수 있는 Idle 상태 타입 확인
        var idleStateType = characCtrl.FindStateType(typeof(CharacterStateIdle));
        
        // CharacterStateIdleHealer 타입인 경우 힐러 로직 사용
        if (idleStateType == typeof(CharacterStateIdleHealer))
        {
            var healTarget = InGameObjectManager.Instance.GetLowestHPOurTeam(characCtrl);
            if (healTarget != null && InGameObjectManager.Instance.IsInRange(characCtrl, healTarget, 2))
            {
                return healTarget;
            }
        }
        
        // 기본 로직: 공격 타겟 찾기
        return InGameObjectManager.Instance.GetOptimalAttackTarget(characCtrl);
    }

    /// <summary>
    /// 타겟을 찾는 인스턴스 메서드 (서브클래스에서 오버라이드 가능)
    /// </summary>
    protected virtual CookApps.BattleSystem.CharacterController FindTargetInstance()
    {
        return InGameObjectManager.Instance.GetOptimalAttackTarget(characCtrl);
    }
}
