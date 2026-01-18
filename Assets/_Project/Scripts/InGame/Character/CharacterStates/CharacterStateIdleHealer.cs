using CookApps.BattleSystem;

public class CharacterStateIdleHealer : CharacterStateIdle
{
    public override CharacterStateRunningResult CharacterStateRunning(float dt)
    {
        // 1. 캐릭터가 Idle 상태로 있어야 하는지 체크 (CC 상태 필요 여부)
        if (characCtrl.NeedToBeCrowdControlState())
        {
            characCtrl.AddNextState<CharacterStateCC>();
            return CharacterStateRunningResult.CanCallEffectCodeOnUpdateAndOnCooltime;
        }

        // 2. 타겟 스캔 간격 체크 (성능 최적화)
        scanTargetTime -= dt;
        if (scanTargetTime > 0f)
        {
            return CharacterStateRunningResult.CanCallEffectCodeOnUpdateAndOnCooltime;
        }
        scanTargetTime = ScanTargetInterval;


        characCtrl.Target = characCtrl.FindTarget();
        
        
        // 4. 타겟이 죽었는지 확인
        if (characCtrl.Target is { IsAlive: false })
        {
            characCtrl.Target = null;
            return CharacterStateRunningResult.CanCallAllWithoutMove;
        }

        // 5. 타겟이 있을 경우 공격 범위 확인 및 행동 결정
        if (characCtrl.Target != null)
        {
            var isInRange = InGameObjectManager.Instance.IsInRange(characCtrl, characCtrl.Target);

            if (isInRange)
            {
                // 5-1. 공격 범위 안에 있으면 공격 상태로 전환
                // AddNextState는 상태 타입 맵을 확인하여 자동으로 CharacterStateAttackHealer 사용
                characCtrl.AddNextState<CharacterStateAttack>();
            }
            else
            {
                // 5-2. 공격 범위 밖에 있으면 타겟에게 이동
                characCtrl.MoveToCharacter(isInRange, characCtrl.Target);
            }
        }

        return CharacterStateRunningResult.CanCallAllWithoutMove;
    }

    /// <summary>
    /// 캐릭터가 힐러인지 확인합니다.
    /// </summary>
    private static bool IsHealer(CookApps.BattleSystem.CharacterController character)
    {
        if (character == null) return false;
        
        var idleStateType = character.FindStateType(typeof(CharacterStateIdle));
        if (idleStateType == typeof(CharacterStateIdleHealer))
            return true;
        
        var attackStateType = character.FindStateType(typeof(CharacterStateAttack));
        return attackStateType == typeof(CharacterStateAttackHealer);
    }
    
    /// <summary>
    /// 캐릭터의 체력 비율을 계산합니다 (CurrentHP/HP).
    /// </summary>
    private static double GetHpRatio(CookApps.BattleSystem.CharacterController character)
    {
        if (character == null) return 0;
        
        double maxHp = character.HP;
        if (maxHp <= 0) return 0;
        
        return character.CurrentHp / maxHp;
    }

    /// <summary>
    /// 힐러 전용 타겟 찾기 로직
    /// 1순위: 힐할 아군 (체력 낮은 순, 80% 미만, 힐러 제외)
    /// 2순위: 공격 범위 내 힐할 아군 (체력 가장 낮은 아군)
    /// 3순위: 공격 타겟 (적)
    /// </summary>
    public new static CookApps.BattleSystem.CharacterController FindTarget(CookApps.BattleSystem.CharacterController characCtrl)
    {
        CookApps.BattleSystem.CharacterController target = null;
        
        // 1단계: 전체 아군 중 체력이 낮은 순으로 정렬된 리스트에서 힐할 대상 찾기
        var sortedList = InGameObjectManager.Instance.GetLowestHPOurTeamSorted(characCtrl);
        CookApps.BattleSystem.CharacterController healTarget = null;
        
        if (sortedList != null && sortedList.Count > 0)
        {
            // 1차 필터링: 체력 비율 80% 이상이거나 힐러인 캐릭터 제외
            for (int i = 0; i < sortedList.Count; i++)
            {
                var candidate = sortedList[i];
                if (candidate == null || !candidate.IsAlive)
                    continue;
                
                double hpRatio = GetHpRatio(candidate);
                bool isHealer = IsHealer(candidate);
                
                // 체력 비율 80% 이상이거나 힐러면 제외
                if (hpRatio >= 0.5 || isHealer)
                    continue;
                
                healTarget = candidate;
                break;
            }
            
        }
        
        // 2단계: 힐할 대상이 공격 범위 안에 있는지 확인
        if (healTarget != null && InGameObjectManager.Instance.IsInRange(characCtrl, healTarget))
        {
            // 범위 안에 있으면 힐 타겟으로 선택
            target = healTarget;
        }
        else
        {
            target = InGameObjectManager.Instance.GetOptimalAttackTarget(characCtrl);
        }

        return target;
    }
}
