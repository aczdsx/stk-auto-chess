# 전투 시스템 설계

> 유닛 AI, 타겟팅, 이동, 공격, 스킬, 데미지 계산, 전투 결과 처리를 정의한다.
>
> **GameMode 적용**: 전투 시스템은 **모든 모드의 공통 기반**이다.
> - ClassicBattle: 사전정의 AI 적과 1회 전투 (MatchmakerSystem 비활성, PvE 전용)
> - PvECampaign: 모든 라운드가 크립 웨이브 (MatchmakerSystem → PvE 매치만 생성)
> - Competitive: PvP + PvE 혼합 (MatchmakerSystem 풀 활성)

---

## 1. 전투 흐름 개요

```
전투 페이즈 시작
  │
  ▼
[전투 준비]
  1. 매칭 결정 (4인 → 2개 매치)
  2. 보드 유닛 복제 → CombatUnit 생성
  3. 상대 유닛 미러링 배치
  4. 시너지 효과 적용
  5. 아이템 효과 적용
  │
  ▼
[전투 루프] ◄────────────────────┐
  1. 타겟 탐색                    │
  2. 이동 / 공격 / 스킬 시전       │
  3. 투사체 처리 (비행 중 투사체)   │
     - Homing: 프레임 카운트 감소  │
     - Linear: 칸 이동 + 관통 히트 │
     - AreaTarget: 도착 시 범위폭발│
  4. 데미지 계산 & 적용            │
  5. 마나 충전                     │
  6. 사망 체크 & 제거              │
  7. 종료 조건 미충족 ─────────────┘
  │
  ▼ 종료 조건 충족
[전투 종료]
  1. 승패 판정
  2. 패배자 HP 감소 계산
  3. CombatUnit 엔티티 전부 삭제
  4. 결과 이벤트 발행
```

---

## 2. 유닛 상태 머신

### 2.1 상태 전이도

```
                    ┌──────────────────────┐
                    │                      │
                    ▼                      │
  ┌───────┐    ┌────────┐    ┌──────────┐  │
  │ Idle  │───▶│ Moving │───▶│Attacking │──┘
  └───┬───┘    └────────┘    └──────────┘
      │             │              │
      │             │              ▼
      │             │        ┌───────────┐
      │             └───────▶│CastingSkill│
      │                      └─────┬─────┘
      │                            │
      ▼                            ▼
  ┌──────────────┐          ┌──────┐
  │CrowdControlled│─────────▶│ Dead │
  └──────────────┘          └──────┘
        ▲                     ▲
        │                     │
    (CC 효과 적용)          (HP ≤ 0)
```

### 2.2 상태 설명

| 상태 | 설명 | 전이 조건 |
|------|------|-----------|
| **Idle** | 전투 시작 직후 또는 타겟 없을 때 | 타겟 발견 → Moving/Attacking |
| **Moving** | 타겟을 향해 이동 중 | 사거리 진입 → Attacking |
| **Attacking** | 기본 공격 수행 | 쿨타임 대기 → 타겟 재탐색 → Idle/Moving |
| **CastingSkill** | 스킬 시전 중 | 시전 완료 → Idle |
| **CrowdControlled** | 기절/넉백/침묵 등 | CC 해제 → Idle |
| **Dead** | 사망 | 종료 상태 (제거 대기) |

### 2.3 상태 처리 코드

```csharp
public unsafe class CombatUnitAISystem : SystemMainThread
{
    public override void Update(Frame f)
    {
        if (f.Global->CurrentPhase != GamePhase.Combat) return;

        var filter = f.Filter<CombatUnit>();
        while (filter.NextUnsafe(out var entity, out var unit))
        {
            if (unit->State == CombatState.Dead) continue;

            switch (unit->State)
            {
                case CombatState.Idle:
                    ProcessIdle(f, entity, unit);
                    break;
                case CombatState.Moving:
                    ProcessMoving(f, entity, unit);
                    break;
                case CombatState.Attacking:
                    ProcessAttacking(f, entity, unit);
                    break;
                case CombatState.CastingSkill:
                    ProcessCastingSkill(f, entity, unit);
                    break;
                case CombatState.CrowdControlled:
                    ProcessCC(f, entity, unit);
                    break;
            }
        }
    }
}
```

---

## 3. 타겟팅 시스템

### 3.1 타겟 선택 규칙

```
기본 타겟 선택 (Default Targeting):
  1. 적 유닛 중 살아있는 유닛만 대상
  2. 현재 위치에서 가장 가까운 적 유닛 선택
  3. 거리가 동일하면 → 현재 HP가 낮은 유닛 우선
  4. HP도 동일하면 → 엔티티 인덱스가 낮은 유닛 (결정론적 일관성)

거리 계산 (사거리/타겟팅용):
  - 맨해튼 거리 (Manhattan Distance)
  - dist = |col1-col2| + |row1-row2|
  - 상하좌우 인접 = 거리 1, 대각선 = 거리 2
  - 대각선 위치의 적은 사거리 1로 공격 불가
  - 사거리 1 = 상하좌우 인접만 공격 (근접)
  - 사거리 2 = 2칸 이내 (대각선 포함)
  - 사거리 3~4 = 원거리 공격

  사거리별 공격 가능 범위 (X = 공격 가능, O = 유닛):

  사거리 1:          사거리 2:          사거리 3:
    . X .              . X .              X X X
    X O X              X X X              X X X
    . X .              X X X              X O X
                       . X .              X X X
                                          X X X
```

### 3.2 특수 타겟팅

```
챔피언 스킬이나 아이템에 의한 특수 타겟팅:

| 타겟 모드 | 설명 |
|-----------|------|
| Nearest | 가장 가까운 적 (기본) |
| Farthest | 가장 먼 적 |
| LowestHP | HP가 가장 낮은 적 |
| HighestHP | HP가 가장 높은 적 |
| HighestAttack | 공격력이 가장 높은 적 |
| Random | 랜덤 (Quantum RNG) |
```

### 3.3 타겟 갱신 조건

```
타겟을 재탐색하는 경우:
  1. 현재 타겟이 사망
  2. 현재 타겟이 CC로 인해 위치 변경 (넉백 등)
  3. Idle 상태 진입 시
  4. 스킬 시전 완료 후

타겟을 유지하는 경우:
  - Moving 중 → 도달할 때까지 유지
  - Attacking 중 → 공격 사이클 완료까지 유지
```

### 3.4 TargetingSystem 코드

```csharp
public unsafe class TargetingSystem : SystemMainThread
{
    public override void Update(Frame f)
    {
        if (f.Global->CurrentPhase != GamePhase.Combat) return;

        var filter = f.Filter<CombatUnit>();
        while (filter.NextUnsafe(out var entity, out var unit))
        {
            if (unit->State == CombatState.Dead) continue;
            if (unit->State == CombatState.CrowdControlled) continue;

            // 타겟 유효성 검사
            if (unit->Target != EntityRef.None)
            {
                if (!f.Exists(unit->Target)) unit->Target = EntityRef.None;
                else
                {
                    var target = f.Get<CombatUnit>(unit->Target);
                    if (target->State == CombatState.Dead)
                        unit->Target = EntityRef.None;
                }
            }

            // 타겟 재탐색
            if (unit->Target == EntityRef.None &&
                (unit->State == CombatState.Idle || unit->State == CombatState.Moving))
            {
                unit->Target = FindNearestEnemy(f, entity, unit);

                if (unit->Target == EntityRef.None)
                {
                    unit->State = CombatState.Idle;
                }
                else
                {
                    // 사거리 내인지 체크 (맨해튼 거리)
                    var targetUnit = f.Get<CombatUnit>(unit->Target);
                    int dist = ManhattanDistance(
                        unit->GridCol, unit->GridRow,
                        targetUnit->GridCol, targetUnit->GridRow);

                    unit->State = dist <= unit->AttackRange
                        ? CombatState.Attacking
                        : CombatState.Moving;
                }
            }
        }
    }

    private EntityRef FindNearestEnemy(Frame f, EntityRef self, CombatUnit* selfUnit)
    {
        EntityRef closest = EntityRef.None;
        int closestDist = int.MaxValue;
        FP closestHP = FP.MaxValue;

        var filter = f.Filter<CombatUnit>();
        while (filter.NextUnsafe(out var entity, out var unit))
        {
            if (entity == self) continue;
            if (unit->Owner == selfUnit->Owner) continue; // 아군 스킵
            if (unit->State == CombatState.Dead) continue;

            int dist = ManhattanDistance(
                selfUnit->GridCol, selfUnit->GridRow,
                unit->GridCol, unit->GridRow);

            if (dist < closestDist ||
                (dist == closestDist && unit->CurrentHP < closestHP))
            {
                closest = entity;
                closestDist = dist;
                closestHP = unit->CurrentHP;
            }
        }

        return closest;
    }

    // 맨해튼 거리: 대각선은 2칸 (공격 사거리 판정용)
    public static int ManhattanDistance(int col1, int row1, int col2, int row2)
    {
        return Math.Abs(col1 - col2) + Math.Abs(row1 - row2);
    }
}
```

---

## 4. 이동 시스템

### 4.1 이동 규칙

```
이동 방식: 그리드 기반 칸 이동

핵심 규칙:
  - 1칸에 1유닛만 존재 가능
  - 매 이동 턴마다 인접 칸 중 1칸 이동
  - 이동 방향: 설정에 따라 4방향(상하좌우) 또는 8방향(+대각선)
    → BoardConfigAsset.AllowDiagonalMovement 옵션으로 제어
  - 이동 속도 = MoveSpeed (칸/초) → MoveInterval = 1/MoveSpeed
  - MoveCooldown이 0 이하가 되면 1칸 이동 실행
  - 타겟을 향해 최적 경로의 다음 칸으로 이동

전투장 그리드:
  - 7열 × 8행 = 56칸
  - CombatGrid 컴포넌트로 점유 상태 관리
  - 이동 전 대상 칸이 비어있는지 반드시 확인

이동 불가 시:
  - 대상 칸이 이미 점유됨 → 우회 경로 탐색
  - 우회도 불가 → 이동 대기 (MoveCooldown 리셋하지 않음)
  - 사거리 내에 타겟이 있으면 이동 불필요 → Attacking 전환

대각선 이동 옵션:
  - AllowDiagonalMovement = true:  8방향 이동, 대각선 1턴 소비
  - AllowDiagonalMovement = false: 4방향(상하좌우)만 이동
  - 기획 밸런스에 따라 런타임 변경 없이 설정으로 결정
```

### 4.2 경로 탐색 (Pathfinding)

```
간이 경로 탐색:

FindNextStep(currentCol, currentRow, targetCol, targetRow, grid):
  1. 타겟까지의 맨해튼 거리가 AttackRange 이내면 → 이동 불필요
  2. 인접 칸 수집 (AllowDiagonalMovement에 따라 4방향 또는 8방향)
  3. 빈 칸만 후보로 필터링
  4. 각 후보 칸에서 타겟까지의 맨해튼 거리 계산
  5. 거리가 가장 짧은 칸 선택
  6. 거리가 동일한 후보가 여러 개면:
     a. 직선 방향(상하좌우) 우선 (대각선보다)
     b. 그래도 동일하면 → row 방향 우선 (전진 우선)
  7. 모든 인접 칸이 막혀있으면 → 이동 실패 (대기)

참고:
  - 7×8 = 56칸으로 매우 작음, 탐색 비용 무시할 수 있음
  - 필요하면 A* 적용 가능하나 규모상 그리디 1-step으로 충분
```

### 4.3 이동 처리 코드

```csharp
private void ProcessMoving(Frame f, EntityRef entity, CombatUnit* unit)
{
    if (unit->Target == EntityRef.None)
    {
        unit->State = CombatState.Idle;
        return;
    }

    var target = f.Get<CombatUnit>(unit->Target);

    // 사거리 체크 (맨해튼 거리)
    int dist = ManhattanDistance(
        unit->GridCol, unit->GridRow,
        target->GridCol, target->GridRow);

    if (dist <= unit->AttackRange)
    {
        // 사거리 진입 → 공격 상태로 전환
        unit->State = CombatState.Attacking;
        unit->AttackCooldown = FP._0;
        return;
    }

    // 이동 쿨다운 대기
    unit->MoveCooldown -= f.DeltaTime;
    if (unit->MoveCooldown > FP._0) return;

    // 다음 이동 칸 탐색
    var grid = GetCombatGrid(f, entity);
    var nextCell = FindNextStep(f, grid,
        unit->GridCol, unit->GridRow,
        target->GridCol, target->GridRow);

    if (nextCell.HasValue)
    {
        int oldIndex = unit->GridCol + unit->GridRow * 7;
        int newIndex = nextCell.Value.col + nextCell.Value.row * 7;

        // 그리드 점유 갱신
        grid->Tiles[oldIndex] = EntityRef.None;
        grid->Tiles[newIndex] = entity;

        // 유닛 좌표 갱신
        unit->GridCol = (byte)nextCell.Value.col;
        unit->GridRow = (byte)nextCell.Value.row;

        // 이동 쿨다운 리셋
        unit->MoveCooldown = unit->MoveInterval;

        // View 이벤트 (이동 애니메이션 트리거)
        f.Events.UnitMoved(entity, (byte)nextCell.Value.col, (byte)nextCell.Value.row);
    }
    // else: 이동 불가 → 다음 프레임에 재시도 (쿨다운 리셋 안 함)
}

private (int col, int row)? FindNextStep(Frame f, CombatGrid* grid,
    int fromCol, int fromRow, int toCol, int toRow)
{
    var config = f.FindAsset<BoardConfigAsset>(f.RuntimeConfig.BoardConfig);
    int bestDist = int.MaxValue;
    (int col, int row)? best = null;

    // 직선 방향 (항상 사용)
    int[,] straightDirs = {
        { 0, 1}, { 0,-1}, {-1, 0}, { 1, 0}
    };

    // 대각선 방향 (옵션)
    int[,] diagonalDirs = {
        {-1, 1}, { 1, 1}, {-1,-1}, { 1,-1}
    };

    // 직선 방향 탐색 (우선순위 높음)
    for (int d = 0; d < 4; d++)
    {
        int nc = fromCol + straightDirs[d, 0];
        int nr = fromRow + straightDirs[d, 1];

        if (nc < 0 || nc >= 7 || nr < 0 || nr >= 8) continue;

        int idx = nc + nr * 7;
        if (grid->Tiles[idx] != EntityRef.None) continue;

        int dist = ManhattanDistance(nc, nr, toCol, toRow);
        if (dist < bestDist)
        {
            bestDist = dist;
            best = (nc, nr);
        }
    }

    // 대각선 방향 탐색 (옵션이 켜져 있을 때만)
    if (config.AllowDiagonalMovement)
    {
        for (int d = 0; d < 4; d++)
        {
            int nc = fromCol + diagonalDirs[d, 0];
            int nr = fromRow + diagonalDirs[d, 1];

            if (nc < 0 || nc >= 7 || nr < 0 || nr >= 8) continue;

            int idx = nc + nr * 7;
            if (grid->Tiles[idx] != EntityRef.None) continue;

            int dist = ManhattanDistance(nc, nr, toCol, toRow);
            if (dist < bestDist)
            {
                bestDist = dist;
                best = (nc, nr);
            }
        }
    }

    return best;
}
```

### 4.4 특수 이동: 후방 점프 (Backline Jump)

```
특정 챔피언(암살자 등)은 전투 시작 직후 적 후방으로 점프:

발동 조건:
  - HasBacklineJump = true (ChampionSpec 또는 시너지 효과)
  - 전투 시작 첫 프레임에 1회만 발동
  - 시너지(암살자 등)로 부여되는 경우도 있음

점프 규칙:
  1. 적 진영의 최후방 Row에서 빈 칸 탐색
     - 자기가 아래쪽(Row 0~3)이면 → Row 7(적 최후방)부터 탐색
     - 자기가 위쪽(Row 4~7)이면 → Row 0(적 최후방)부터 탐색
  2. 가장 가까운 적 유닛 인접 칸 우선
  3. 최후방 Row에 빈 칸 없으면 → 그 앞 Row로 확장
  4. 점프 후 즉시 타겟 재탐색

시각 연출:
  - 원래 위치에서 사라짐 → 점프 궤적 이펙트 → 착지 위치에 등장
  - 착지 시 이펙트 (먼지, 임팩트)
```

### 4.5 BacklineJumpSystem 코드

```csharp
public unsafe class BacklineJumpSystem : SystemSignalsOnly,
    ISignalOnPhaseStarted
{
    public void OnPhaseStarted(Frame f, GamePhase phase)
    {
        if (phase != GamePhase.Combat) return;

        // 전투 시작 직후 후방 점프 처리
        var grids = f.Filter<CombatGrid>();
        while (grids.NextUnsafe(out var gridEntity, out var grid))
        {
            var units = f.Filter<CombatUnit>();
            while (units.NextUnsafe(out var entity, out var unit))
            {
                if (!unit->HasBacklineJump) continue;
                if (unit->BacklineJumpDone) continue;

                ExecuteBacklineJump(f, entity, unit, grid);
                unit->BacklineJumpDone = true;
            }
        }
    }

    private void ExecuteBacklineJump(Frame f, EntityRef entity,
                                      CombatUnit* unit, CombatGrid* grid)
    {
        // 점프 대상 Row 결정 (적 최후방)
        bool isBottomSide = unit->GridRow < 4;
        int startRow = isBottomSide ? 7 : 0;
        int rowDir = isBottomSide ? -1 : 1;

        // 적 최후방 Row부터 빈 칸 탐색
        for (int r = startRow; isBottomSide ? r >= 4 : r <= 3; r += rowDir)
        {
            // 해당 Row에서 적 유닛에 인접한 빈 칸 우선
            var bestCell = FindBestJumpTarget(f, grid, unit, r);
            if (bestCell.HasValue)
            {
                // 원래 위치 해제
                int oldIdx = unit->GridCol + unit->GridRow * 7;
                grid->Tiles[oldIdx] = EntityRef.None;

                // 새 위치 점유
                int newIdx = bestCell.Value.col + bestCell.Value.row * 7;
                grid->Tiles[newIdx] = entity;
                unit->GridCol = (byte)bestCell.Value.col;
                unit->GridRow = (byte)bestCell.Value.row;

                // 타겟 초기화 (새 위치에서 재탐색)
                unit->Target = EntityRef.None;

                f.Events.UnitJumped(entity,
                    (byte)bestCell.Value.col, (byte)bestCell.Value.row);
                return;
            }
        }
        // 빈 칸 없으면 점프 실패 (원래 위치 유지)
    }
}
```

### 4.6 이동 관련 View 이벤트

```qtn
// 유닛이 한 칸 이동
event UnitMoved {
    entity_ref Unit;
    Byte NewCol;
    Byte NewRow;
}

// 유닛이 후방 점프 (특수 이동)
event UnitJumped {
    entity_ref Unit;
    Byte TargetCol;
    Byte TargetRow;
}
```

### 4.7 View 레이어 이동 보간

```
Quantum에서 유닛은 즉시 칸을 이동하지만,
View에서는 LitMotion으로 부드러운 보간 처리:

일반 이동 (UnitMoved):
  - 현재 월드 위치 → 대상 칸 월드 위치
  - LitMotion.Create(currentPos, targetPos, 0.2f) 선형 보간
  - 이동 중 Spine 걷기 애니메이션

후방 점프 (UnitJumped):
  - 원래 위치에서 페이드아웃 + 점프 궤적 파티클
  - 착지 위치에 페이드인 + 임팩트 이펙트
  - 총 연출 시간: ~0.5초
```

---

## 5. 공격 시스템

### 5.1 기본 공격 사이클

```
공격 쿨타임 = 1 / AttackSpeed (초)

공격 사이클:
  1. 쿨타임 대기 (AttackCooldown 감소)
  2. 쿨타임 ≤ 0 → 공격 실행
  3. 근접(사거리 1) → 즉시 데미지 적용
     원거리(사거리 2+) → PendingDamage 큐에 등록 (비행 시간 후 적용)
  4. 공격자 마나 충전 (+공격 시 마나 획득)
  5. 피격자 마나 충전은 실제 피격 시점에 적용
     근접: 즉시 / 원거리: 투사체 도착 시
  6. 쿨타임 리셋
  7. 타겟 사거리 이탈 체크 → Moving 전환

공격 시 View 이벤트:
  - UnitAttacked(attacker, target, damage, isCrit, isProjectile, projectileSpecId)
  - isProjectile = true → View에서 투사체 VFX 스폰
```

### 5.2 공격 처리 코드

```csharp
private void ProcessAttacking(Frame f, EntityRef entity, CombatUnit* unit)
{
    if (unit->Target == EntityRef.None)
    {
        unit->State = CombatState.Idle;
        return;
    }

    var target = f.Get<CombatUnit>(unit->Target);

    // 타겟 사거리 체크 (맨해튼 거리)
    int dist = ManhattanDistance(
        unit->GridCol, unit->GridRow,
        target->GridCol, target->GridRow);
    if (dist > unit->AttackRange)
    {
        unit->State = CombatState.Moving;
        return;
    }

    // 쿨타임 감소
    unit->AttackCooldown -= f.DeltaTime;

    if (unit->AttackCooldown <= FP._0)
    {
        // 공격 실행
        FP damage = CalculatePhysicalDamage(unit->Attack, target->Armor);

        // 크리티컬 처리
        bool isCrit = false;
        FP critChance = GetCritChance(f, entity, unit);
        if (critChance > FP._0)
        {
            FP roll = f.RNG->Next();
            if (roll < critChance)
            {
                FP critMultiplier = GetCritMultiplier(f, entity, unit);
                damage *= critMultiplier;
                isCrit = true;
            }
        }

        bool isRanged = unit->AttackRange >= 2;

        if (isRanged)
        {
            // 원거리 → 투사체 생성 (데미지는 도착 시 적용)
            int travelFrames = dist * unit->ProjectileFramesPerTile;
            EnqueueProjectile(f, entity, unit->Target, damage, isCrit,
                DamageType.Physical, travelFrames, unit->ProjectileSpecId);

            // 공격자 마나는 발사 시점에 충전
            unit->CurrentMana += GetAttackManaGain(f, entity, unit);
            ClampMana(unit);
        }
        else
        {
            // 근접 → 즉시 데미지 적용
            ApplyDamage(f, entity, unit->Target, target, damage, DamageType.Physical);

            // 마나 충전 (공격자 + 피격자 모두 즉시)
            unit->CurrentMana += GetAttackManaGain(f, entity, unit);
            target->CurrentMana += GetHitManaGain(f, unit->Target, target);
            ClampMana(unit);
            ClampMana(target);
        }

        // 마나 가득 → 스킬 시전 체크
        if (unit->CurrentMana >= unit->MaxMana)
        {
            TryActivateSkill(f, entity, unit);
        }

        // 쿨타임 리셋
        unit->AttackCooldown = FP._1 / unit->AttackSpeed;

        // View 이벤트
        f.Events.UnitAttacked(entity, unit->Target, damage, isCrit,
            isRanged, unit->ProjectileSpecId);
    }
}
```

---

## 5A. 투사체 시스템

> 투사체는 데미지를 즉시 적용하지 않고, **비행 시간 후 도착 시 적용**한다.
> 공격자가 사망해도 이미 발사된 투사체는 계속 비행하며, 타겟이 사망하면 소멸한다.
> 이를 통해 원거리 공격 vs 근접 공격의 전술적 차이가 생긴다.

### 5A.1 투사체 타입

```
ProjectileType:
  1. Homing (타겟팅 투사체)
     - 특정 타겟을 추적하여 비행
     - 도착 시 해당 타겟에만 데미지
     - 타겟이 이동해도 따라감 (호밍)
     - 예: 기본 원거리 공격, 단일 타겟 스킬
     - 시뮬레이션: 위치 추적 불필요, 프레임 카운트만으로 충분

  2. Linear (논타겟팅 직선 투사체)
     - 발사 방향으로 직선 이동
     - 경로상의 모든 유닛에 데미지 (관통)
     - 타겟을 추적하지 않음
     - 예: 에즈리얼 궁, 관통 화살
     - 시뮬레이션: 매 프레임 그리드 칸 이동 + 점유 유닛 체크

  3. AreaTarget (범위 투사체)
     - 특정 위치(좌표)로 비행
     - 도착 시 범위 내 모든 유닛에 데미지
     - 예: 포격, 메테오, 럭스 궁
     - 시뮬레이션: 프레임 카운트 + 도착 시 범위 스캔
```

### 5A.2 투사체 컴포넌트

```qtn
enum ProjectileType { Homing, Linear, AreaTarget }

// 글로벌 투사체 큐 (보드 단위)
component ProjectileQueue {
    // 최대 동시 비행 투사체 수 (16 = 양측 최대 8유닛)
    array<Projectile>[32] Entries;
    Int32 Count;
}

struct Projectile {
    entity_ref Source;               // 발사한 유닛 (사망해도 유지)
    entity_ref Target;               // Homing 타겟 (Linear/Area는 None)
    ProjectileType Type;
    FP Damage;
    Boolean IsCrit;
    Byte DamageType;                 // Physical=0, Magical=1, True=2
    Int32 SkillSpecId;               // 0 = 기본공격
    Int32 ProjectileSpecId;          // VFX 참조용

    // Homing / AreaTarget 용
    Int32 RemainingFrames;           // 도착까지 남은 프레임

    // Linear 용
    Byte CurrentCol;                 // 현재 위치 (그리드 좌표)
    Byte CurrentRow;
    SByte DirCol;                    // 이동 방향 (-1, 0, +1)
    SByte DirRow;
    Int32 MoveInterval;              // N프레임마다 1칸 이동
    Int32 MoveTimer;                 // 이동 타이머
    Int32 MaxDistance;               // 최대 비행 거리 (칸)
    Int32 TraveledDistance;          // 이미 이동한 거리

    // AreaTarget 용
    Byte TargetCol;                  // 목표 좌표
    Byte TargetRow;
    Int32 AreaRadius;                // 폭발 반경 (맨해튼 거리)

    // 공통
    Int64 HitMask;                   // 이미 히트한 유닛 비트마스크 (Linear 관통용)
}
```

### 5A.3 ProjectileSystem

```csharp
public unsafe class ProjectileSystem : SystemMainThread
{
    public override void Update(Frame f)
    {
        if (f.Global->CurrentPhase != GamePhase.Combat) return;

        var filter = f.Filter<ProjectileQueue, CombatGrid>();
        while (filter.NextUnsafe(out var gridEntity, out var queue, out var grid))
        {
            // 역순 순회 (제거 시 swap-back)
            for (int i = queue->Count - 1; i >= 0; i--)
            {
                ref var proj = ref queue->Entries[i];

                switch (proj.Type)
                {
                    case ProjectileType.Homing:
                        ProcessHoming(f, ref proj, queue, grid, i);
                        break;
                    case ProjectileType.Linear:
                        ProcessLinear(f, ref proj, queue, grid, i);
                        break;
                    case ProjectileType.AreaTarget:
                        ProcessAreaTarget(f, ref proj, queue, grid, i);
                        break;
                }
            }
        }
    }
}
```

### 5A.4 Homing 투사체 처리

```csharp
private void ProcessHoming(Frame f, ref Projectile proj,
    ProjectileQueue* queue, CombatGrid* grid, int index)
{
    proj.RemainingFrames--;

    if (proj.RemainingFrames <= 0)
    {
        // 도착 → 타겟 생존 확인
        if (proj.Target != EntityRef.None && f.Exists(proj.Target))
        {
            var target = f.Get<CombatUnit>(proj.Target);
            if (target->State != CombatState.Dead)
            {
                // 데미지 적용
                ApplyDamage(f, proj.Source, proj.Target, target,
                    proj.Damage, (DamageType)proj.DamageType);

                // 피격자 마나 충전 (도착 시점)
                target->CurrentMana += GetHitManaGain(f, proj.Target, target);
                ClampMana(target);

                // View 이벤트
                f.Events.ProjectileHit(proj.Source, proj.Target,
                    proj.Damage, proj.IsCrit, proj.ProjectileSpecId);
            }
        }
        // 타겟 사망/소멸 → 투사체 소멸 (데미지 없음)

        RemoveProjectile(queue, index);
    }
}
```

### 5A.5 Linear 투사체 처리

```csharp
private void ProcessLinear(Frame f, ref Projectile proj,
    ProjectileQueue* queue, CombatGrid* grid, int index)
{
    proj.MoveTimer--;

    if (proj.MoveTimer <= 0)
    {
        // 다음 칸으로 이동
        int nextCol = proj.CurrentCol + proj.DirCol;
        int nextRow = proj.CurrentRow + proj.DirRow;

        // 그리드 범위 밖 → 소멸
        if (nextCol < 0 || nextCol >= 7 || nextRow < 0 || nextRow >= 8)
        {
            f.Events.ProjectileExpired(proj.Source, proj.ProjectileSpecId,
                proj.CurrentCol, proj.CurrentRow);
            RemoveProjectile(queue, index);
            return;
        }

        proj.CurrentCol = (byte)nextCol;
        proj.CurrentRow = (byte)nextRow;
        proj.TraveledDistance++;
        proj.MoveTimer = proj.MoveInterval; // 타이머 리셋

        // 현재 칸에 유닛이 있으면 히트 판정
        int tileIdx = nextCol + nextRow * 7;
        EntityRef occupant = grid->Tiles[tileIdx];
        if (occupant != EntityRef.None && f.Exists(occupant))
        {
            var target = f.Get<CombatUnit>(occupant);
            var source = f.Exists(proj.Source)
                ? f.Get<CombatUnit>(proj.Source) : null;

            // 적 유닛만 히트 (아군 스킵)
            bool isEnemy = source == null ||
                target->Owner != source->Owner;

            // 이미 히트한 유닛은 스킵 (비트마스크로 체크)
            int entityIndex = occupant.Index;
            bool alreadyHit = (proj.HitMask & (1L << (entityIndex % 64))) != 0;

            if (isEnemy && !alreadyHit && target->State != CombatState.Dead)
            {
                // 데미지 적용
                ApplyDamage(f, proj.Source, occupant, target,
                    proj.Damage, (DamageType)proj.DamageType);

                // 피격자 마나 충전
                target->CurrentMana += GetHitManaGain(f, occupant, target);
                ClampMana(target);

                // 히트 마스크 갱신
                proj.HitMask |= (1L << (entityIndex % 64));

                f.Events.ProjectileHit(proj.Source, occupant,
                    proj.Damage, proj.IsCrit, proj.ProjectileSpecId);
            }
        }

        // 최대 비행 거리 도달 → 소멸
        if (proj.TraveledDistance >= proj.MaxDistance)
        {
            f.Events.ProjectileExpired(proj.Source, proj.ProjectileSpecId,
                proj.CurrentCol, proj.CurrentRow);
            RemoveProjectile(queue, index);
        }
        else
        {
            // View에 위치 갱신 이벤트
            f.Events.ProjectileMoved(proj.Source, proj.ProjectileSpecId,
                (byte)nextCol, (byte)nextRow);
        }
    }
}
```

### 5A.6 AreaTarget 투사체 처리

```csharp
private void ProcessAreaTarget(Frame f, ref Projectile proj,
    ProjectileQueue* queue, CombatGrid* grid, int index)
{
    proj.RemainingFrames--;

    if (proj.RemainingFrames <= 0)
    {
        // 도착 → 범위 내 모든 적 유닛에 데미지
        var source = f.Exists(proj.Source)
            ? f.Get<CombatUnit>(proj.Source) : null;

        var units = f.Filter<CombatUnit>();
        while (units.NextUnsafe(out var entity, out var unit))
        {
            if (unit->State == CombatState.Dead) continue;

            // 적 유닛만
            bool isEnemy = source == null ||
                unit->Owner != source->Owner;
            if (!isEnemy) continue;

            // 범위 체크 (맨해튼 거리)
            int dist = ManhattanDistance(
                unit->GridCol, unit->GridRow,
                proj.TargetCol, proj.TargetRow);
            if (dist > proj.AreaRadius) continue;

            ApplyDamage(f, proj.Source, entity, unit,
                proj.Damage, (DamageType)proj.DamageType);

            // 피격자 마나 충전
            unit->CurrentMana += GetHitManaGain(f, entity, unit);
            ClampMana(unit);

            f.Events.ProjectileHit(proj.Source, entity,
                proj.Damage, proj.IsCrit, proj.ProjectileSpecId);
        }

        // 범위 폭발 View 이벤트
        f.Events.ProjectileExploded(proj.Source, proj.ProjectileSpecId,
            proj.TargetCol, proj.TargetRow, proj.AreaRadius);

        RemoveProjectile(queue, index);
    }
}
```

### 5A.7 투사체 생성 헬퍼

```csharp
// Homing 투사체 생성 (기본 원거리 공격, 단일 타겟 스킬)
private void EnqueueProjectile(Frame f, EntityRef source, EntityRef target,
    FP damage, bool isCrit, DamageType dmgType,
    int travelFrames, int projectileSpecId)
{
    var queue = GetProjectileQueue(f, source);
    if (queue->Count >= 32) return; // 오버플로 방지

    queue->Entries[queue->Count] = new Projectile
    {
        Source = source,
        Target = target,
        Type = ProjectileType.Homing,
        Damage = damage,
        IsCrit = isCrit,
        DamageType = (byte)dmgType,
        ProjectileSpecId = projectileSpecId,
        RemainingFrames = travelFrames,
    };
    queue->Count++;
}

// Linear 투사체 생성 (논타겟팅 직선 관통)
private void EnqueueLinearProjectile(Frame f, EntityRef source,
    int startCol, int startRow, int dirCol, int dirRow,
    FP damage, bool isCrit, DamageType dmgType,
    int moveInterval, int maxDistance, int projectileSpecId)
{
    var queue = GetProjectileQueue(f, source);
    if (queue->Count >= 32) return;

    queue->Entries[queue->Count] = new Projectile
    {
        Source = source,
        Target = EntityRef.None,
        Type = ProjectileType.Linear,
        Damage = damage,
        IsCrit = isCrit,
        DamageType = (byte)dmgType,
        ProjectileSpecId = projectileSpecId,
        CurrentCol = (byte)startCol,
        CurrentRow = (byte)startRow,
        DirCol = (sbyte)dirCol,
        DirRow = (sbyte)dirRow,
        MoveInterval = moveInterval,
        MoveTimer = moveInterval,
        MaxDistance = maxDistance,
        TraveledDistance = 0,
        HitMask = 0,
    };
    queue->Count++;

    f.Events.ProjectileSpawned(source, projectileSpecId,
        ProjectileType.Linear, (byte)startCol, (byte)startRow);
}

// AreaTarget 투사체 생성 (범위 폭격)
private void EnqueueAreaProjectile(Frame f, EntityRef source,
    int targetCol, int targetRow, int areaRadius,
    FP damage, bool isCrit, DamageType dmgType,
    int travelFrames, int projectileSpecId)
{
    var queue = GetProjectileQueue(f, source);
    if (queue->Count >= 32) return;

    queue->Entries[queue->Count] = new Projectile
    {
        Source = source,
        Target = EntityRef.None,
        Type = ProjectileType.AreaTarget,
        Damage = damage,
        IsCrit = isCrit,
        DamageType = (byte)dmgType,
        ProjectileSpecId = projectileSpecId,
        RemainingFrames = travelFrames,
        TargetCol = (byte)targetCol,
        TargetRow = (byte)targetRow,
        AreaRadius = areaRadius,
    };
    queue->Count++;

    f.Events.ProjectileSpawned(source, projectileSpecId,
        ProjectileType.AreaTarget, (byte)targetCol, (byte)targetRow);
}

// 투사체 제거 (swap-back)
private void RemoveProjectile(ProjectileQueue* queue, int index)
{
    queue->Count--;
    if (index < queue->Count)
    {
        queue->Entries[index] = queue->Entries[queue->Count];
    }
}
```

### 5A.8 투사체 비행 시간 계산

```
비행 프레임 수 계산:

Homing (타겟팅):
  travelFrames = distance × ProjectileFramesPerTile
  distance = 맨해튼 거리 (발사 위치 ↔ 타겟 위치)
  ProjectileFramesPerTile = 챔피언 스펙에 정의 (기본값: 4프레임/칸)
  예) 거리 3, 4프레임/칸 → 12프레임 = 0.4초 (30FPS 기준)

Linear (직선 관통):
  MoveInterval = N프레임마다 1칸 이동 (스킬 스펙에 정의)
  MaxDistance = 최대 비행 칸 수 (7 = 보드 가로 전체)
  총 비행 시간 = MoveInterval × MaxDistance 프레임

AreaTarget (범위):
  travelFrames = distance × FramesPerTile (Homing과 동일)
  distance = 발사 위치 ↔ 목표 좌표의 맨해튼 거리

근접 (사거리 1):
  투사체 없음, 즉시 데미지 적용

CombatUnit에 추가되는 필드:
  Int32 ProjectileFramesPerTile;   // 기본공격 투사체 속도 (0 = 근접)
  Int32 ProjectileSpecId;          // 기본공격 투사체 VFX 참조
```

### 5A.9 스킬 투사체 연동

```
스킬에서도 투사체를 사용할 수 있다.
SkillSpecAsset(§7.3)의 ProjectileType 필드로 제어:

  - ProjectileType == None → 즉시 적용 (기존 방식)
  - ProjectileType == Homing → EnqueueProjectile() 호출
  - ProjectileType == Linear → EnqueueLinearProjectile() 호출
  - ProjectileType == AreaTarget → EnqueueAreaProjectile() 호출

SkillEffectSystem(§7.5)에서 투사체 분기 처리:
  1. OnSkillActivated 시그널 수신
  2. ProjectileType 확인
  3. None → ApplySkillEffect() (즉시)
  4. 그 외 → SpawnSkillProjectile() (투사체 생성, 도착 시 데미지)

→ 전체 정의 및 예시는 §7.3 SkillSpecAsset, §7.5 SkillEffectSystem 참조
```

---

## 6. 데미지 계산

### 6.1 물리 데미지

```
물리 데미지 = 공격력 × (100 / (100 + 방어력))

FP CalculatePhysicalDamage(FP attack, FP armor):
  - armor 하한: 0 (음수 방어력은 없음, 방어력 감소 디버프는 0까지)
  - reduction = 100 / (100 + armor)
  - result = attack * reduction
```

### 6.2 마법 데미지

```
마법 데미지 = 스킬 위력 × (100 / (100 + 마법 저항))

FP CalculateMagicDamage(FP power, FP magicResist):
  - 물리 데미지와 동일한 감쇠 공식
  - magicResist 하한: 0
```

### 6.3 순수 데미지

```
순수 데미지 = 고정 수치 (방어/마저 무시)
  - 특수 스킬이나 아이템 효과에서 사용
  - 방어 계산 스킵
```

### 6.4 데미지 적용 코드

```csharp
public enum DamageType { Physical, Magical, True }

private void ApplyDamage(Frame f, EntityRef source, EntityRef targetEntity,
                          CombatUnit* target, FP rawDamage, DamageType type)
{
    FP finalDamage;

    switch (type)
    {
        case DamageType.Physical:
            finalDamage = CalculatePhysicalDamage(rawDamage, target->Armor);
            break;
        case DamageType.Magical:
            finalDamage = CalculateMagicDamage(rawDamage, target->MagicResist);
            break;
        case DamageType.True:
            finalDamage = rawDamage;
            break;
        default:
            finalDamage = rawDamage;
            break;
    }

    // 최소 데미지 보장
    if (finalDamage < FP._1) finalDamage = FP._1;

    target->CurrentHP -= finalDamage;

    // 사망 체크
    if (target->CurrentHP <= FP._0)
    {
        target->CurrentHP = FP._0;
        target->State = CombatState.Dead;
        f.Events.UnitDied(targetEntity, source);
        f.Signals.OnUnitDied(targetEntity, source);
    }
    else
    {
        f.Events.UnitDamaged(targetEntity, source, finalDamage, type);
    }
}
```

---

## 7. 스킬 시스템

### 7.1 마나 & 스킬 발동

```
마나 충전 경로:
  - 기본 공격 시: +10 마나 (기본값, ChampionSpec에서 오버라이드)
  - 피격 시: +10 마나 (기본값)
  - 일부 아이템/시너지 효과

스킬 발동:
  1. CurrentMana ≥ MaxMana
  2. 현재 상태가 Idle 또는 Attacking
  3. 스킬 시전 시작 → CastingSkill 상태
  4. 마나 리셋 (CurrentMana = 0)
  5. 시전 시간(CastTime) 후 효과 적용
  6. Idle로 복귀

MaxMana 기본값: 챔피언마다 다름 (60 ~ 120 범위)
  - 마나 낮은 챔피언 → 스킬 자주 사용 (유틸리티형)
  - 마나 높은 챔피언 → 강력한 1회 스킬 (딜러형)
```

### 7.2 스킬 런타임 컴포넌트

```qtn
component SkillData {
    Int32 SkillSpecId;
    FP ManaCost;        // = MaxMana (스킬 사용 시 전부 소비)
    FP CastTime;        // 시전 시간 (프레임 단위)
    FP CastElapsed;     // 시전 경과 시간
    SkillTargetType TargetType;
}

enum SkillTargetType {
    CurrentTarget,      // 현재 공격 대상
    NearestEnemy,       // 가장 가까운 적
    AllEnemies,         // 적 전체
    AllAllies,          // 아군 전체
    Self,               // 자기 자신
    Area,               // 범위 (타겟 위치 중심)
    RandomEnemies,      // 랜덤 N명
}
```

### 7.3 스킬 스펙 (Asset)

```csharp
public enum SkillEffectType
{
    Damage,             // 데미지 (물리/마법/순수)
    Heal,               // 회복
    Buff,               // 아군 버프 (스탯 증가, 보호막 등)
    Debuff,             // 적 디버프 (스탯 감소, 지속 데미지 등)
    Summon,             // 유닛 소환
    CrowdControl,       // CC (기절, 넉백, 침묵 등)
}

public enum SkillProjectileType
{
    None,               // 즉시 적용 (투사체 없음)
    Homing,             // 타겟 추적 투사체
    Linear,             // 직선 관통 투사체
    AreaTarget,         // 범위 폭발 투사체
}

public class SkillSpecAsset : AssetObject
{
    // ─── 기본 정보 ───
    public int SkillId;
    public string Name;
    public Sprite Icon;

    // ─── 발동 조건 ───
    public FP ManaCost;                     // = MaxMana (전부 소비)
    public FP CastTime;                     // 시전 시간 (초)
    public SkillTargetType TargetType;      // 타겟 선택 방식

    // ─── 효과 ───
    public SkillEffectType EffectType;      // 주 효과 유형
    public DamageType DamageType;           // Physical / Magical / True
    public FP BaseDamage;                   // 기본 데미지/회복량
    public FP ScalingFactor;               // 스케일링 계수 (AP 비례 등)
    public FP Duration;                     // 버프/디버프/CC 지속 시간

    // ─── 범위 ───
    public int TargetCount;                 // 대상 수 (RandomEnemies: N명)
    public int AreaRadius;                  // Area 타입: 맨해튼 거리 반경

    // ─── 투사체 ───
    public SkillProjectileType ProjectileType;  // 투사체 유형 (None=즉시)
    public int ProjectileSpecId;                // VFX 프리팹 참조
    public int ProjectileSpeed;                 // 프레임/칸 (Homing/AreaTarget)
    public int LinearMaxDistance;               // Linear 전용: 최대 비행 거리 (칸)

    // ─── CC 상세 ───
    public CCType CrowdControlType;         // Stun, Silence, Knockback 등
    public FP KnockbackDistance;            // 넉백 거리 (칸)

    // ─── 버프/디버프 상세 ───
    public FP BuffAttack;                   // 공격력 변경량
    public FP BuffAttackSpeedPercent;       // 공격속도 변경 (%)
    public FP BuffArmor;                    // 방어력 변경량
    public FP BuffMagicResist;              // 마법저항 변경량
    public FP ShieldAmount;                 // 보호막 수치

    // ─── 소환 ───
    public int SummonChampionSpecId;        // 소환할 유닛 스펙
    public int SummonCount;                 // 소환 수
    public int SummonDurationFrames;        // 소환 유닛 생존 시간 (0=영구)
}
```

```
SkillSpecAsset 사용 예시:

[단일 타겟 마법 데미지 + 투사체]
  Name: "화염구"
  TargetType: CurrentTarget
  EffectType: Damage
  DamageType: Magical
  BaseDamage: 250
  ScalingFactor: 1.5 (AP의 150%)
  ProjectileType: Homing
  ProjectileSpecId: 2001
  ProjectileSpeed: 4

[범위 폭발 스킬]
  Name: "메테오"
  TargetType: Area
  EffectType: Damage
  DamageType: Magical
  BaseDamage: 200
  AreaRadius: 2
  ProjectileType: AreaTarget
  ProjectileSpecId: 2002
  ProjectileSpeed: 3

[직선 관통 스킬]
  Name: "빛의 창"
  TargetType: CurrentTarget (발사 방향 결정용)
  EffectType: Damage
  DamageType: Magical
  BaseDamage: 300
  ProjectileType: Linear
  ProjectileSpecId: 2003
  ProjectileSpeed: 2
  LinearMaxDistance: 7

[아군 전체 힐 - 즉발]
  Name: "치유의 빛"
  TargetType: AllAllies
  EffectType: Heal
  BaseDamage: 150 (회복량으로 사용)
  ScalingFactor: 1.0
  ProjectileType: None

[CC 스킬 - 기절]
  Name: "벼락"
  TargetType: CurrentTarget
  EffectType: CrowdControl
  DamageType: Magical
  BaseDamage: 100
  CrowdControlType: Stun
  Duration: 2.0
  ProjectileType: Homing
  ProjectileSpecId: 2004
  ProjectileSpeed: 3

[소환 스킬]
  Name: "소환수 부름"
  TargetType: Self
  EffectType: Summon
  SummonChampionSpecId: 9001
  SummonCount: 2
  SummonDurationFrames: 300 (10초)
  ProjectileType: None
```

### 7.4 스킬 시전 처리

```csharp
private void TryActivateSkill(Frame f, EntityRef entity, CombatUnit* unit)
{
    // 스킬이 없는 유닛은 무시
    if (!f.Has<SkillData>(entity)) return;

    var skill = f.Get<SkillData>(entity);
    if (unit->CurrentMana < skill->ManaCost) return;

    // 마나 소비
    unit->CurrentMana = FP._0;

    // 시전 시작
    unit->State = CombatState.CastingSkill;
    skill->CastElapsed = FP._0;

    f.Events.UnitCastSkill(entity, skill->SkillSpecId);
}

private void ProcessCastingSkill(Frame f, EntityRef entity, CombatUnit* unit)
{
    var skill = f.Get<SkillData>(entity);
    skill->CastElapsed += f.DeltaTime;

    if (skill->CastElapsed >= skill->CastTime)
    {
        // 스킬 효과 적용
        f.Signals.OnSkillActivated(entity, skill->SkillSpecId);

        // 상태 복귀
        unit->State = CombatState.Idle;
        unit->Target = EntityRef.None; // 타겟 재탐색
    }
}
```

### 7.5 스킬 효과 시스템

```csharp
// 스킬 효과는 SkillEffectSystem에서 Signal로 처리
public unsafe class SkillEffectSystem : SystemSignalsOnly,
    ISignalOnSkillActivated
{
    public void OnSkillActivated(Frame f, EntityRef caster, int skillSpecId)
    {
        var spec = f.FindAsset<SkillSpecAsset>(skillSpecId);
        var unit = f.Get<CombatUnit>(caster);

        // 투사체 타입에 따라 즉시 적용 or 투사체 생성
        if (spec.ProjectileType != SkillProjectileType.None)
        {
            // 투사체 있는 스킬 → 투사체 생성 (데미지는 도착 시 적용)
            SpawnSkillProjectile(f, caster, unit, spec);
        }
        else
        {
            // 즉발 스킬 → 효과 즉시 적용
            ApplySkillEffect(f, caster, unit, spec);
        }
    }

    private void SpawnSkillProjectile(Frame f, EntityRef caster,
        CombatUnit* unit, SkillSpecAsset spec)
    {
        FP damage = CalculateSkillDamage(unit, spec);

        switch (spec.ProjectileType)
        {
            case SkillProjectileType.Homing:
            {
                // 타겟까지 거리 기반 비행 시간
                var target = f.Get<CombatUnit>(unit->Target);
                int dist = ManhattanDistance(
                    unit->GridCol, unit->GridRow,
                    target->GridCol, target->GridRow);
                int frames = dist * spec.ProjectileSpeed;

                EnqueueProjectile(f, caster, unit->Target, damage, false,
                    spec.DamageType, frames, spec.ProjectileSpecId);
                break;
            }
            case SkillProjectileType.Linear:
            {
                // 타겟 방향으로 직선 발사
                var target = f.Get<CombatUnit>(unit->Target);
                int dirCol = Math.Sign(target->GridCol - unit->GridCol);
                int dirRow = Math.Sign(target->GridRow - unit->GridRow);
                // 주 방향만 사용 (대각선 방지)
                if (Math.Abs(target->GridCol - unit->GridCol) >=
                    Math.Abs(target->GridRow - unit->GridRow))
                    dirRow = 0;
                else
                    dirCol = 0;

                EnqueueLinearProjectile(f, caster,
                    unit->GridCol, unit->GridRow, dirCol, dirRow,
                    damage, false, spec.DamageType,
                    spec.ProjectileSpeed, spec.LinearMaxDistance,
                    spec.ProjectileSpecId);
                break;
            }
            case SkillProjectileType.AreaTarget:
            {
                // 타겟 위치로 범위 폭격
                var target = f.Get<CombatUnit>(unit->Target);
                int dist = ManhattanDistance(
                    unit->GridCol, unit->GridRow,
                    target->GridCol, target->GridRow);
                int frames = dist * spec.ProjectileSpeed;

                EnqueueAreaProjectile(f, caster,
                    target->GridCol, target->GridRow, spec.AreaRadius,
                    damage, false, spec.DamageType,
                    frames, spec.ProjectileSpecId);
                break;
            }
        }
    }

    private void ApplySkillEffect(Frame f, EntityRef caster,
        CombatUnit* unit, SkillSpecAsset spec)
    {
        switch (spec.EffectType)
        {
            case SkillEffectType.Damage:
                ApplyDamageSkill(f, caster, unit, spec);
                break;
            case SkillEffectType.Heal:
                ApplyHealSkill(f, caster, unit, spec);
                break;
            case SkillEffectType.Buff:
                ApplyBuffSkill(f, caster, unit, spec);
                break;
            case SkillEffectType.Debuff:
                ApplyDebuffSkill(f, caster, unit, spec);
                break;
            case SkillEffectType.Summon:
                ApplySummonSkill(f, caster, unit, spec);
                break;
            case SkillEffectType.CrowdControl:
                ApplyCCSkill(f, caster, unit, spec);
                break;
        }
    }

    private FP CalculateSkillDamage(CombatUnit* unit, SkillSpecAsset spec)
    {
        // 기본 데미지 + (스케일링 × 주력 스탯)
        FP scalingStat = spec.DamageType == DamageType.Physical
            ? unit->Attack
            : unit->SpellPower;
        return spec.BaseDamage + scalingStat * spec.ScalingFactor;
    }
}
```

---

## 8. 전투 결과 처리

### 8.1 전투 종료 조건

```
1. 한쪽 전멸: 한 플레이어의 CombatUnit이 모두 Dead
2. 타임아웃: PhaseTimer ≤ 0 (최대 45초)

타임아웃 판정:
  - 양쪽 잔여 HP 합산 비교
  - HP 비율이 높은 쪽 승리
  - 동률 → 무승부 (양쪽 모두 HP 감소 없음)
```

### 8.2 플레이어 데미지 계산

```
패배자가 받는 HP 데미지:

PlayerDamage = BaseDamage + SurvivingUnitDamage

BaseDamage:
  - 현재 스테이지에 따라 증가
  - Stage 1: 0, Stage 2: 1, Stage 3: 2, Stage 4: 3, ...

SurvivingUnitDamage:
  - 생존한 적 유닛별 (별 등급에 따른 데미지)
  - 1★: 1데미지
  - 2★: 2데미지
  - 3★: 3데미지

예시:
  Stage 3에서 적 2★×2, 1★×1 생존
  → BaseDamage(2) + 2+2+1 = 7 데미지
```

### 8.3 데미지 계산 코드

```csharp
public unsafe class PlayerDamageSystem : SystemSignalsOnly,
    ISignalOnCombatEnd
{
    public void OnCombatEnd(Frame f, PlayerRef winner, PlayerRef loser, int matchIndex)
    {
        // 생존 유닛 데미지 계산
        int unitDamage = 0;
        var filter = f.Filter<CombatUnit>();
        while (filter.NextUnsafe(out var entity, out var unit))
        {
            if (unit->Owner != winner) continue;
            if (unit->State == CombatState.Dead) continue;

            unitDamage += unit->StarLevel; // 1★=1, 2★=2, 3★=3
        }

        // 기본 데미지 (스테이지 기반)
        int baseDamage = f.Global->CurrentStage - 1;

        int totalDamage = baseDamage + unitDamage;

        // 패배자 HP 감소
        var loserData = GetPlayerData(f, loser);
        loserData->HP -= totalDamage;

        f.Events.CombatResult(winner, loser, totalDamage);
        f.Signals.OnPlayerDamaged(loser, totalDamage, loserData->HP);

        // 탈락 체크
        if (loserData->HP <= 0)
        {
            loserData->HP = 0;
            f.Global->AlivePlayerCount--;
            int rank = f.Global->AlivePlayerCount + 1;
            f.Signals.OnPlayerEliminated(loser, rank);
            f.Events.PlayerEliminated(loser, rank);
        }
    }
}
```

---

## 9. 커맨더 스킬 (전투 중 플레이어 개입)

### 9.1 시스템 개요

```
커맨더 스킬 = 플레이어가 전투 중 직접 발동하는 스킬
  - 유닛 스킬과 별개
  - 전투의 흐름에 전략적으로 개입

특성:
  - 플레이어당 선택된 1~2개 보유
  - 쿨타임 기반 (전투 내 재사용 가능 or 1회 제한)
  - 타겟 지정형 / 즉시 발동형 / 패시브형
```

### 9.2 커맨더 스킬 컴포넌트

```qtn
component CommanderSkillState {
    // 최대 2개 슬롯
    array<Int32>[2] SkillSpecIds;
    array<FP>[2] Cooldowns;
    array<Byte>[2] UsesRemaining;    // 0 = 무제한
}
```

### 9.3 CommanderSkillSystem

```csharp
public unsafe class CommanderSkillSystem : SystemMainThread
{
    public override void Update(Frame f)
    {
        if (f.Global->CurrentPhase != GamePhase.Combat) return;

        // 쿨타임 감소
        var filter = f.Filter<CommanderSkillState>();
        while (filter.NextUnsafe(out var entity, out var state))
        {
            for (int i = 0; i < 2; i++)
            {
                if (state->Cooldowns[i] > FP._0)
                    state->Cooldowns[i] -= f.DeltaTime;
            }
        }

        // 커맨드 처리
        for (int p = 0; p < f.PlayerCount; p++)
        {
            var player = (PlayerRef)p;
            foreach (var cmd in f.GetPlayerCommands<UseCommanderSkillCommand>(player))
            {
                ProcessCommanderSkill(f, player, cmd);
            }
        }
    }

    private void ProcessCommanderSkill(Frame f, PlayerRef player,
                                        UseCommanderSkillCommand cmd)
    {
        var playerEntity = GetPlayerEntity(f, player);
        var state = f.Get<CommanderSkillState>(playerEntity);

        int idx = cmd.SkillIndex;
        if (idx < 0 || idx >= 2) return;
        if (state->SkillSpecIds[idx] == 0) return; // 빈 슬롯
        if (state->Cooldowns[idx] > FP._0) return; // 쿨다운 중
        if (state->UsesRemaining[idx] == 0) return; // 사용 횟수 소진

        var spec = f.FindAsset<CommanderSkillSpecAsset>(state->SkillSpecIds[idx]);

        // 효과 적용
        ApplyCommanderSkillEffect(f, player, spec, cmd.TargetPos);

        // 쿨타임 시작
        state->Cooldowns[idx] = spec.Cooldown;

        // 사용 횟수 차감
        if (state->UsesRemaining[idx] > 0)
            state->UsesRemaining[idx]--;

        f.Events.CommanderSkillUsed(player, idx, state->SkillSpecIds[idx]);
    }
}
```

---

## 10. PvE (크립) 전투

### 10.1 크립 웨이브

```
PvE 라운드에서는 상대가 다른 플레이어가 아닌 AI 몬스터.

CreepWaveAsset:
  - 몬스터 종류 & 배치 위치
  - 몬스터 스탯 (HP, Attack, Armor 등)
  - 아이템 드롭 테이블

특이사항:
  - 모든 플레이어가 동일한 크립과 대전 (각자 독립)
  - 크립은 플레이어 유닛과 동일한 AI 사용
  - 크립은 스킬이 없거나 단순한 스킬만 보유
  - 패배해도 탈락하지 않음 (소량 HP 감소만)
```

### 10.2 아이템 드롭

```
크립 라운드 승리 시:
  - RoundConfig의 ItemDropCount만큼 아이템 드롭
  - 드롭 아이템은 기본 아이템 (조합 재료)
  - 드롭 테이블은 CreepWaveAsset에서 정의

크립 라운드 패배 시:
  - 아이템 드롭 없음 or 감소된 수량

4인 게임 조정:
  - 드롭 수량은 TFT 대비 동일 또는 약간 증가
  - 풀 크기가 작으므로 아이템 중요도 상대적으로 높음
```

---

## 11. View 이벤트 정의

```qtn
// 기본 공격 (근접: isProjectile=false, 원거리: isProjectile=true)
event UnitAttacked {
    entity_ref Attacker;
    entity_ref Target;
    FP Damage;
    Boolean IsCrit;
    Boolean IsProjectile;       // true → View에서 투사체 VFX 스폰
    Int32 ProjectileSpecId;     // 투사체 VFX 프리팹 참조 (0 = 없음)
}

// 피해
event UnitDamaged {
    entity_ref Target;
    entity_ref Source;
    FP Damage;
    Byte DamageType;     // Physical=0, Magical=1, True=2
}

// 사망
event UnitDied {
    entity_ref Unit;
    entity_ref Killer;
}

// 스킬 시전
event UnitCastSkill {
    entity_ref Caster;
    Int32 SkillSpecId;
}

// 스킬 효과 적중 (투사체 없는 즉발 스킬)
event SkillHit {
    entity_ref Caster;
    entity_ref Target;
    Int32 SkillSpecId;
    FP Value;
}

// 회복
event UnitHealed {
    entity_ref Target;
    entity_ref Source;
    FP Amount;
}

// 커맨더 스킬 사용
event CommanderSkillUsed {
    player_ref Player;
    Byte SkillSlot;
    Int32 SkillSpecId;
}

// 상태이상
event CrowdControlApplied {
    entity_ref Target;
    entity_ref Source;
    Byte CCType;
    FP Duration;
}

// ─── 투사체 이벤트 ───

// 투사체 스폰 (Linear/AreaTarget 전용, Homing은 UnitAttacked로 처리)
event ProjectileSpawned {
    entity_ref Source;
    Int32 ProjectileSpecId;
    Byte ProjectileType;        // 0=Homing, 1=Linear, 2=AreaTarget
    Byte StartCol;
    Byte StartRow;
}

// 투사체 히트 (Homing 도착 / Linear 관통 히트)
event ProjectileHit {
    entity_ref Source;
    entity_ref Target;
    FP Damage;
    Boolean IsCrit;
    Int32 ProjectileSpecId;
}

// Linear 투사체 위치 갱신 (매 칸 이동 시)
event ProjectileMoved {
    entity_ref Source;
    Int32 ProjectileSpecId;
    Byte NewCol;
    Byte NewRow;
}

// 투사체 소멸 (최대 거리 도달 / 그리드 범위 이탈)
event ProjectileExpired {
    entity_ref Source;
    Int32 ProjectileSpecId;
    Byte LastCol;
    Byte LastRow;
}

// AreaTarget 투사체 폭발
event ProjectileExploded {
    entity_ref Source;
    Int32 ProjectileSpecId;
    Byte TargetCol;
    Byte TargetRow;
    Int32 Radius;
}
```

---

## 12. 전투 시뮬레이션 성능 고려

```
프레임 레이트:
  - Quantum 시뮬레이션: 30 FPS (기본)
  - Unity 렌더링: 별도 (모바일 30~60 FPS)
  - 시뮬레이션은 렌더링과 독립적

유닛 최대 수:
  - 한 매치: 최대 8 + 8 = 16유닛 (양측)
  - 전체 (2개 매치): 최대 32유닛 동시 시뮬레이션
  - 소환 유닛 포함: ~40유닛 (극단적 경우)

최적화:
  - Entity 필터링은 Quantum의 Archetype 기반으로 빠름
  - 거리 계산은 맨해튼 거리 (정수 연산, 매우 가벼움)
  - 경로 탐색은 8방향 인접 칸만 체크 (O(8), 극히 가벼움)
  - CombatGrid 배열로 점유 확인 O(1)
  - 타겟 탐색은 적 유닛만 순회 (아군 스킵)
  - 7×8 = 56칸으로 BFS도 O(56) → 무시할 수 있는 비용
  - 투사체: 배열 순회 O(32), swap-back 제거 O(1)
    → Linear 투사체 칸 이동은 프레임당 최대 1칸이므로 부하 미미
```
