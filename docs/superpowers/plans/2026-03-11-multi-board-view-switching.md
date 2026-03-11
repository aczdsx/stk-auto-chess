# 멀티보드 뷰 전환 구현 계획

> **For agentic workers:** REQUIRED: Use superpowers:subagent-driven-development (if subagents available) or superpowers:executing-plans to implement this plan. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 경쟁 모드에서 여러 보드(매치)의 전투를 즉시 전환(스냅)하며 볼 수 있도록 뷰 레이어를 확장한다.

**Architecture:** 싱글 보드 + 데이터 소스 교체 방식. 씬에 BoardGridView 1개를 유지하고, 전환 시 UnitView를 풀 회수 후 새 보드 데이터로 재스폰. SimEvent에 MatchIndex를 태깅하여 비활성 보드 이벤트를 필터링한다.

**Tech Stack:** Unity 6, C#, LitMotion (트랜지션 연출), UniTask

**설계 문서:** `docs/superpowers/specs/2026-03-11-multi-board-view-switching-design.md`

---

## 파일 구조

| 파일 | 역할 | 변경 유형 |
|---|---|---|
| `Simulation/Data/SimulationEvents.cs` | SimEvent에 MatchIndex 필드 추가, Push 헬퍼에 matchIndex 파라미터 추가 | 수정 |
| `Simulation/Combat/CombatSetupSystem.cs` | CombatMatchState에 MatchIndex 전파 확인 (이미 존재) | 확인만 |
| `Simulation/Combat/DamageSystem.cs` | 이벤트 Push 시 matchIndex 전달 | 수정 |
| `Simulation/Combat/MovementSystem.cs` | 이벤트 Push 시 matchIndex 전달 | 수정 |
| `Simulation/Combat/ProjectileSystem.cs` | 이벤트 Push 시 matchIndex 전달 | 수정 |
| `Simulation/Combat/SkillSystem.cs` | 이벤트 Push 시 matchIndex 전달 | 수정 |
| `Simulation/Combat/Skills/Custom/SimSkillClayChannel.cs` | 이벤트 Push 시 matchIndex 전달 | 수정 |
| `Simulation/Combat/Skills/Custom/SimSkillAprilBarrage.cs` | 이벤트 Push 시 matchIndex 전달 | 수정 |
| `Simulation/Combat/Skills/Custom/SimSkillEnkiWaveHeal.cs` | 이벤트 Push 시 matchIndex 전달 | 수정 |
| `View/AutoChessViewBridge.cs` | SwitchBoard(), _isSwitching 가드, SyncCombatViews 단일 매치 처리, ProcessEvents 매치 필터링 | 수정 |
| `View/Unit/UnitViewManager.cs` | ClearAllViews() 추가 | 수정 |
| `View/Combat/CombatViewManager.cs` | ClearEffects() 추가 | 수정 |
| `View/Board/BoardTransitionEffect.cs` | 페이드 트랜지션 컴포넌트 | 신규 |

> 모든 파일은 `Assets/_Project/Scripts/InGame_New/` 하위 경로.

---

## Chunk 1: 시뮬레이션 레이어 — SimEvent MatchIndex 태깅

### Task 1: SimEvent에 MatchIndex 필드 추가

**Files:**
- Modify: `Simulation/Data/SimulationEvents.cs:43-70` (SimEvent 구조체)

- [ ] **Step 1: SimEvent 구조체에 MatchIndex 필드 추가**

`SimulationEvents.cs`의 `SimEvent` 구조체(43행)에 필드 추가:

```csharp
public struct SimEvent
{
    public SimEventType Type;

    // 매치 귀속 (전투 이벤트 필터링용, 0xFF = 전역 이벤트)
    public byte MatchIndex;

    // 공통
    public byte PlayerIndex;
    // ... 기존 필드 그대로 유지
}
```

- [ ] **Step 2: Push 헬퍼 메서드에 matchIndex 파라미터 추가**

`SimEventQueue` 클래스(75행)의 전투 관련 Push 헬퍼들에 `byte matchIndex = 0xFF` 파라미터 추가. 전투 이벤트에만 적용:

```csharp
public void PushUnitMoved(int entityId, byte col, byte row, byte matchIndex = 0xFF)
{
    Push(new SimEvent
    {
        Type = SimEventType.UnitMoved,
        MatchIndex = matchIndex,
        EntityId = entityId,
        Col = col,
        Row = row,
    });
}

public void PushUnitAttacked(int attackerId, int targetId, int damage, bool isCrit, bool isProjectile, byte matchIndex = 0xFF)
{
    Push(new SimEvent
    {
        Type = SimEventType.UnitAttacked,
        MatchIndex = matchIndex,
        EntityId = attackerId,
        TargetEntityId = targetId,
        Value0 = damage,
        Flag0 = isCrit,
        Value1 = isProjectile ? 1 : 0,
    });
}

public void PushUnitDamaged(int targetId, int sourceId, int damage, DamageType damageType, byte matchIndex = 0xFF)
{
    Push(new SimEvent
    {
        Type = SimEventType.UnitDamaged,
        MatchIndex = matchIndex,
        EntityId = targetId,
        TargetEntityId = sourceId,
        Value0 = damage,
        Value1 = (int)damageType,
    });
}

public void PushUnitDied(int entityId, int killerId, byte matchIndex = 0xFF)
{
    Push(new SimEvent
    {
        Type = SimEventType.UnitDied,
        MatchIndex = matchIndex,
        EntityId = entityId,
        TargetEntityId = killerId,
    });
}

public void PushUnitCastSkill(int casterId, int targetId, int skillSpecId, byte matchIndex = 0xFF)
{
    Push(new SimEvent
    {
        Type = SimEventType.UnitCastSkill,
        MatchIndex = matchIndex,
        EntityId = casterId,
        TargetEntityId = targetId,
        Value0 = skillSpecId,
    });
}

public void PushProjectileSpawned(int sourceId, int targetId, ProjectileType projType,
    byte col, byte row, sbyte dirCol = 0, sbyte dirRow = 0, byte matchIndex = 0xFF)
{
    Push(new SimEvent
    {
        Type = SimEventType.ProjectileSpawned,
        MatchIndex = matchIndex,
        EntityId = sourceId,
        TargetEntityId = targetId,
        ProjType = projType,
        Col = col,
        Row = row,
        DirCol = (byte)dirCol,
        DirRow = (byte)dirRow,
    });
}

public void PushProjectileExploded(byte col, byte row, int radius, int skillSpecId = 0, byte matchIndex = 0xFF)
{
    Push(new SimEvent
    {
        Type = SimEventType.ProjectileExploded,
        MatchIndex = matchIndex,
        Col = col,
        Row = row,
        Radius = radius,
        Value0 = skillSpecId,
    });
}

public void PushSkillAreaEffect(int casterId, byte col, byte row, int radius, bool isRow = false, byte matchIndex = 0xFF)
{
    Push(new SimEvent
    {
        Type = SimEventType.SkillAreaEffect,
        MatchIndex = matchIndex,
        EntityId = casterId,
        Col = col,
        Row = row,
        Radius = radius,
        Flag0 = isRow,
    });
}
```

비전투 이벤트(`PushPhaseChanged`, `PushCombatResult`, `PushGoldChanged` 등)는 변경 불필요 — 기본값 `0xFF`(전역)로 유지.

- [ ] **Step 3: 커밋**

```bash
git add Assets/_Project/Scripts/InGame_New/Simulation/Data/SimulationEvents.cs
git commit -m "feat(InGame_New): SimEvent에 MatchIndex 필드 추가"
```

---

### Task 2: CombatMatchState에서 MatchIndex를 EventQueue Push에 전파

**Files:**
- Modify: `Simulation/Combat/DamageSystem.cs:101,136,207,228,409`
- Modify: `Simulation/Combat/MovementSystem.cs:210`
- Modify: `Simulation/Combat/ProjectileSystem.cs:171,204,239,269`
- Modify: `Simulation/Combat/SkillSystem.cs:79,131`
- Modify: `Simulation/Combat/Skills/Custom/SimSkillClayChannel.cs:70`
- Modify: `Simulation/Combat/Skills/Custom/SimSkillAprilBarrage.cs:79`
- Modify: `Simulation/Combat/Skills/Custom/SimSkillEnkiWaveHeal.cs:95`
- Modify: `Simulation/Combat/Skills/SkillHelpers.cs:297`

> **패턴**: 모든 `state.EventQueue?.PushXxx(...)` 호출에 `state.MatchIndex`를 마지막 인자로 전달.
> `CombatMatchState.MatchIndex`는 이미 존재하는 `byte MatchIndex` 필드 (Components.cs 참조).

- [ ] **Step 1: DamageSystem.cs 수정**

각 Push 호출의 마지막 인자에 `state.MatchIndex` 추가:

```csharp
// 101행 부근
state.EventQueue?.PushUnitDamaged(target.CombatId, source.CombatId, finalDamage, damageType, state.MatchIndex);

// 136행 부근
state.EventQueue?.PushUnitDied(target.SourceEntityId, attacker.SourceEntityId, state.MatchIndex);

// 207행 부근
state.EventQueue?.PushUnitAttacked(attacker.SourceEntityId, target.SourceEntityId, finalDamage, isCrit, false, state.MatchIndex);

// 228행 부근
state.EventQueue?.PushUnitAttacked(attacker.SourceEntityId, target.SourceEntityId, rawDamage, isCrit, true, state.MatchIndex);

// 409행 부근
state.EventQueue?.PushUnitAttacked(attacker.SourceEntityId, unit.SourceEntityId, finalDamage, isCrit, false, state.MatchIndex);
```

- [ ] **Step 2: MovementSystem.cs 수정**

```csharp
// 210행 부근
state.EventQueue?.PushUnitMoved(unit.SourceEntityId, (byte)bestCol, (byte)bestRow, state.MatchIndex);
```

- [ ] **Step 3: ProjectileSystem.cs 수정**

```csharp
// 171행 부근
state.EventQueue?.PushProjectileExploded(proj.TargetCol, proj.TargetRow, proj.AreaRadius, proj.SkillSpecId, state.MatchIndex);

// 204행 부근
state.EventQueue?.PushProjectileSpawned(sourceCombatId, targetCombatId, ProjectileType.Homing, srcCol, srcRow, 0, 0, state.MatchIndex);

// 239행 부근
state.EventQueue?.PushProjectileSpawned(sourceCombatId, CombatUnit.InvalidId, ProjectileType.Linear, srcCol, srcRow, dirCol, dirRow, state.MatchIndex);

// 269행 부근
state.EventQueue?.PushProjectileSpawned(sourceCombatId, CombatUnit.InvalidId, ProjectileType.AreaTarget, targetCol, targetRow, 0, 0, state.MatchIndex);
```

- [ ] **Step 4: SkillSystem.cs 수정**

```csharp
// 79행 부근
state.EventQueue?.PushUnitCastSkill(unit.CombatId, targetId, skillSpecId, state.MatchIndex);

// 131행 부근
state.EventQueue?.PushUnitCastSkill(unit.CombatId, targetId, skillSpecId, state.MatchIndex);
```

- [ ] **Step 5: SkillHelpers.cs 수정**

```csharp
// 297행 부근
state.EventQueue?.PushUnitMoved(target.SourceEntityId, (byte)col, (byte)row, state.MatchIndex);
```

- [ ] **Step 6: Custom Skill 파일들 수정**

SimSkillClayChannel.cs (70행 부근):
```csharp
state.EventQueue?.PushSkillAreaEffect(unit.CombatId, (byte)col, (byte)row, radius, false, state.MatchIndex);
```

SimSkillAprilBarrage.cs (79행 부근):
```csharp
state.EventQueue?.PushSkillAreaEffect(unit.CombatId, (byte)col, (byte)row, radius, false, state.MatchIndex);
```

SimSkillEnkiWaveHeal.cs (95행 부근):
```csharp
state.EventQueue?.PushSkillAreaEffect(unit.CombatId, (byte)col, (byte)row, radius, isRow, state.MatchIndex);
```

- [ ] **Step 7: 커밋**

```bash
git add Assets/_Project/Scripts/InGame_New/Simulation/Combat/
git commit -m "feat(InGame_New): 전투 이벤트 Push에 MatchIndex 전파"
```

---

## Chunk 2: 뷰 레이어 — 보드 전환 코어 로직

### Task 3: UnitViewManager.ClearAllViews() 추가

**Files:**
- Modify: `View/Unit/UnitViewManager.cs`

- [ ] **Step 1: ClearAllViews 메서드 추가**

`UnitViewManager.cs`의 `OnCombatEnd()` 메서드(242행) 근처에 추가:

```csharp
/// <summary>모든 활성 뷰를 풀로 회수 (보드 전환용)</summary>
public void ClearAllViews()
{
    // 전투 뷰 회수
    foreach (var kvp in _combatUnitViews)
        ReturnToPool(kvp.Value);
    _combatUnitViews.Clear();

    // 보드 뷰 회수
    foreach (var kvp in _boardUnitViews)
        ReturnToPool(kvp.Value);
    _boardUnitViews.Clear();
}
```

- [ ] **Step 2: 커밋**

```bash
git add Assets/_Project/Scripts/InGame_New/View/Unit/UnitViewManager.cs
git commit -m "feat(InGame_New): UnitViewManager.ClearAllViews() 추가"
```

---

### Task 4: CombatViewManager.ClearEffects() 추가

**Files:**
- Modify: `View/Combat/CombatViewManager.cs`

- [ ] **Step 1: ClearEffects 메서드 추가**

`CombatViewManager.cs`의 `OnCombatEnd()` 메서드(81행) 근처에 추가:

```csharp
/// <summary>모든 VFX + 지연 큐 정리 (보드 전환용)</summary>
public void ClearEffects()
{
    _pendingProjectiles.Clear();
    _pendingMeleeAttacks.Clear();
    _pendingMeleeTargetIds.Clear();
    _tileEffectManager?.HideAll();
    ClearAllProjectiles();
}
```

> `OnCombatEnd()`와 유사하지만 `_isCombatActive`를 변경하지 않음 (전투 중 보드 전환이므로).

- [ ] **Step 2: 커밋**

```bash
git add Assets/_Project/Scripts/InGame_New/View/Combat/CombatViewManager.cs
git commit -m "feat(InGame_New): CombatViewManager.ClearEffects() 추가"
```

---

### Task 5: BoardTransitionEffect 컴포넌트 생성

**Files:**
- Create: `View/Board/BoardTransitionEffect.cs`

- [ ] **Step 1: 트랜지션 컴포넌트 작성**

```csharp
using System;
using LitMotion;
using LitMotion.Extensions;
using UnityEngine;

namespace CookApps.AutoChess.View
{
    /// <summary>
    /// 보드 전환 시 페이드 트랜지션 연출.
    /// CanvasGroup alpha를 이용한 페이드아웃/인.
    /// </summary>
    public class BoardTransitionEffect : MonoBehaviour
    {
        [SerializeField] private CanvasGroup _canvasGroup;

        private const float FadeOutDuration = 0.15f;
        private const float FadeInDuration = 0.15f;

        private bool _isPlaying;
        public bool IsPlaying => _isPlaying;

        private MotionHandle _handle;

        /// <summary>
        /// 페이드아웃 → onMidpoint 콜백 → 페이드인 실행.
        /// </summary>
        public void PlayTransition(Action onMidpoint)
        {
            if (_isPlaying) return;
            _isPlaying = true;

            // 페이드아웃 (alpha 1 → 0)
            _handle = LMotion.Create(1f, 0f, FadeOutDuration)
                .WithEase(Ease.Linear)
                .WithOnComplete(() =>
                {
                    onMidpoint?.Invoke();

                    // 페이드인 (alpha 0 → 1)
                    _handle = LMotion.Create(0f, 1f, FadeInDuration)
                        .WithEase(Ease.Linear)
                        .WithOnComplete(() => _isPlaying = false)
                        .BindToCanvasGroupAlpha(_canvasGroup);
                })
                .BindToCanvasGroupAlpha(_canvasGroup);
        }

        private void OnDestroy()
        {
            if (_handle.IsActive()) _handle.Cancel();
        }
    }
}
```

- [ ] **Step 2: 커밋**

```bash
git add Assets/_Project/Scripts/InGame_New/View/Board/BoardTransitionEffect.cs
git commit -m "feat(InGame_New): BoardTransitionEffect 컴포넌트 추가"
```

---

### Task 6: AutoChessViewBridge — SwitchBoard 및 이벤트 필터링

**Files:**
- Modify: `View/AutoChessViewBridge.cs`

이 태스크가 핵심. 4가지를 변경:
1. `_isSwitching` 가드 플래그 + `_activeBoardIndex` 분리
2. `SwitchBoard()` 메서드
3. `SyncCombatViews()` 단일 매치 처리
4. `ProcessEvents()` 매치 필터링

- [ ] **Step 1: 필드 추가 및 Initialize 수정**

`AutoChessViewBridge.cs` 상단(32행 부근)에 필드 추가:

```csharp
private int _localPlayerIndex;      // 로컬 플레이어 인덱스 (불변)
private int _activeBoardIndex;      // 현재 표시 중인 보드 (전환 가능)
private bool _isSwitching;          // 보드 전환 트랜지션 중 여부
private BoardTransitionEffect _transitionEffect;
```

`Initialize()` 메서드(38행) 수정:

```csharp
public void Initialize(int localPlayerIndex = 0)
{
    _localPlayerIndex = localPlayerIndex;
    _activeBoardIndex = localPlayerIndex;

    _unitViewManager.Initialize();
    _combatViewManager.Initialize();
    _boardGridView.Initialize();

    _unitViewManager.SetActiveBoard(_activeBoardIndex);

    _viewsReadySource = new UniTaskCompletionSource();
    _unitViewManager.OnAllBoardViewsReady += () => _viewsReadySource.TrySetResult();

    _runner.OnTick += HandleTick;
    _runner.OnPhaseChanged += HandlePhaseChanged;
}
```

- [ ] **Step 2: HandleTick에 _isSwitching 가드 추가**

```csharp
private void HandleTick(GameWorld world)
{
    // 보드 전환 트랜지션 중에는 동기화 스킵
    if (_isSwitching) return;

    ProcessEvents(world);

    if (world.IsCombatActive)
    {
        SyncCombatViews(world);
    }
    else
    {
        _unitViewManager.SyncBoardUnits(world);
    }

    _autoChessUI?.SyncState(world);
}
```

- [ ] **Step 3: SyncCombatViews를 _activeBoardIndex 기준 단일 매치로 변경**

```csharp
private void SyncCombatViews(GameWorld world)
{
    for (int i = 0; i < GameWorld.MaxCombatMatches; i++)
    {
        var matchState = world.CombatMatchStates[i];
        if (matchState == null) continue;

        // _activeBoardIndex에 해당하는 매치만 표시
        if (matchState.PlayerA != _activeBoardIndex && matchState.PlayerB != _activeBoardIndex)
            continue;

        _unitViewManager.SyncCombatUnits(matchState, _activeBoardIndex);
        return; // 단일 매치만 처리
    }
}
```

- [ ] **Step 4: FindBoardIndexForMatch를 _activeBoardIndex 기준으로 수정**

```csharp
private int FindBoardIndexForMatch(GameWorld world, int matchIndex)
{
    var match = world.Matches[matchIndex];
    if (match.PlayerA == _activeBoardIndex || match.PlayerB == _activeBoardIndex)
        return _activeBoardIndex;
    return match.PlayerA;
}
```

- [ ] **Step 5: ProcessEvents에 매치 필터링 추가**

```csharp
private void ProcessEvents(GameWorld world)
{
    var queue = world.EventQueue;
    for (int i = 0; i < queue.Count; i++)
    {
        ref var evt = ref queue.Events[i];

        // 전투 이벤트: 활성 보드의 매치가 아니면 스킵
        if (evt.MatchIndex != 0xFF && !IsActiveMatch(world, evt.MatchIndex))
            continue;

        DispatchEvent(ref evt, world);
    }
    queue.Clear();
}

/// <summary>해당 매치가 현재 활성 보드의 매치인지 확인</summary>
private bool IsActiveMatch(GameWorld world, byte matchIndex)
{
    if (matchIndex >= GameWorld.MaxCombatMatches) return false;
    var match = world.Matches[matchIndex];
    return match.PlayerA == _activeBoardIndex || match.PlayerB == _activeBoardIndex;
}
```

- [ ] **Step 6: SwitchBoard 메서드 추가**

```csharp
/// <summary>보드 전환 (관전 또는 자기 보드 복귀)</summary>
public void SwitchBoard(int targetBoardIndex)
{
    if (_isSwitching) return;
    if (targetBoardIndex == _activeBoardIndex) return;

    if (_transitionEffect != null)
    {
        _isSwitching = true;
        _transitionEffect.PlayTransition(() =>
        {
            ExecuteBoardSwitch(targetBoardIndex);
            _isSwitching = false;
        });
    }
    else
    {
        // 트랜지션 이펙트 없으면 즉시 전환
        ExecuteBoardSwitch(targetBoardIndex);
    }
}

private void ExecuteBoardSwitch(int targetBoardIndex)
{
    _unitViewManager.ClearAllViews();
    _combatViewManager.ClearEffects();
    _boardInputHandler?.CancelGhostDrag();

    _activeBoardIndex = targetBoardIndex;
    _unitViewManager.SetActiveBoard(_activeBoardIndex);

    // 자기 보드가 아니면 입력 차단
    _boardInputHandler?.SetEnabled(_activeBoardIndex == _localPlayerIndex && _lastPhase == GamePhase.Preparation);
}
```

- [ ] **Step 7: SetSpectateBoard를 SwitchBoard로 리다이렉트**

기존 `SetSpectateBoard`(317행)를 수정하여 새 `SwitchBoard`를 사용:

```csharp
public void SetSpectateBoard(int boardIndex)
{
    SwitchBoard(boardIndex);
}
```

- [ ] **Step 8: HandlePhaseChanged에 자동 복귀 로직 추가**

`HandlePhaseChanged()`의 `Preparation` 케이스에 자동 복귀 추가:

```csharp
case GamePhase.Preparation:
    // 다른 보드 구경 중이었으면 자기 보드로 복귀
    if (_activeBoardIndex != _localPlayerIndex)
    {
        ExecuteBoardSwitch(_localPlayerIndex);
    }
    _unitViewManager.OnCombatEnd();
    _combatViewManager.OnCombatEnd();
    _boardGridView.OnPreparation();
    _autoChessUI?.OnPhaseChanged(newPhase);
    if (_autoChessUI != null) _autoChessUI.gameObject.SetActive(true);
    _boardInputHandler?.SetEnabled(true);
    break;
```

- [ ] **Step 9: Result 페이즈의 SyncCombatViews도 _activeBoardIndex 기준으로 수정**

`HandlePhaseChanged()`의 `Result` 케이스(111행):

```csharp
case GamePhase.Result:
{
    var world = _runner.GetWorld();
    for (int i = 0; i < GameWorld.MaxCombatMatches; i++)
    {
        var matchState = world.CombatMatchStates[i];
        if (matchState == null) continue;
        if (matchState.PlayerA != _activeBoardIndex && matchState.PlayerB != _activeBoardIndex)
            continue;
        _unitViewManager.SyncCombatUnits(matchState, _activeBoardIndex);
    }
    _unitViewManager.ForceAllCombatViewsIdle();
    _autoChessUI?.OnPhaseChanged(newPhase);
    break;
}
```

- [ ] **Step 10: SetTransitionEffect setter 추가**

```csharp
public void SetTransitionEffect(BoardTransitionEffect effect) => _transitionEffect = effect;
```

- [ ] **Step 11: 커밋**

```bash
git add Assets/_Project/Scripts/InGame_New/View/AutoChessViewBridge.cs
git commit -m "feat(InGame_New): 보드 전환 핵심 로직 (SwitchBoard, 이벤트 필터링, 틱 가드)"
```

---

## Chunk 3: 뷰 레이어 — 엣지 케이스 처리

### Task 7: 탈락 플레이어 보드 자동 복귀

**Files:**
- Modify: `View/AutoChessViewBridge.cs`

- [ ] **Step 1: DispatchEvent에서 PlayerEliminated 처리 추가**

`DispatchEvent()` 메서드의 `PlayerEliminated` 케이스(222행)에 추가:

```csharp
case SimEventType.PlayerEliminated:
    // 보고 있던 플레이어가 탈락하면 자기 보드로 복귀
    if (evt.PlayerIndex == _activeBoardIndex && _activeBoardIndex != _localPlayerIndex)
    {
        SwitchBoard(_localPlayerIndex);
    }
    _autoChessUI?.OnPlayerEliminated(evt.PlayerIndex, evt.Value0);
    break;
```

- [ ] **Step 2: 커밋**

```bash
git add Assets/_Project/Scripts/InGame_New/View/AutoChessViewBridge.cs
git commit -m "feat(InGame_New): 탈락 플레이어 보드 구경 시 자동 복귀"
```

---

### Task 8: TargetLineManager 보드 전환 시 정리

**Files:**
- Modify: `View/AutoChessViewBridge.cs`

- [ ] **Step 1: ViewBridge에 TargetLineManager 참조 추가**

필드 추가(13행 부근):

```csharp
private TargetLineManager _targetLineManager;
```

setter 추가:

```csharp
public void SetTargetLineManager(TargetLineManager manager) => _targetLineManager = manager;
```

- [ ] **Step 2: ExecuteBoardSwitch에 TargetLineManager 정리 추가**

`ExecuteBoardSwitch()` 메서드에서 `_unitViewManager.ClearAllViews()` 직전에 추가:

```csharp
_targetLineManager?.StopDrawing();
```

- [ ] **Step 3: 커밋**

```bash
git add Assets/_Project/Scripts/InGame_New/View/AutoChessViewBridge.cs
git commit -m "feat(InGame_New): 보드 전환 시 TargetLineManager 정리"
```

---

### Task 9: AutoChessViewRoot에서 BoardTransitionEffect 연결

**Files:**
- Modify: `View/AutoChessViewRoot.cs`

> 이 태스크는 AutoChessViewRoot의 초기화 흐름에 BoardTransitionEffect와 TargetLineManager를 ViewBridge에 연결하는 작업.

- [ ] **Step 1: AutoChessViewRoot.cs 읽고 초기화 흐름 확인**

파일을 읽어 정확한 초기화 순서와 ViewBridge.Setup() 호출 위치를 확인.

- [ ] **Step 2: BoardTransitionEffect 생성 및 ViewBridge에 연결**

초기화 흐름에서 ViewBridge.Setup() 이후, Initialize() 이전에 추가:

```csharp
// BoardTransitionEffect (기존 UI CanvasGroup에 부착하거나, 전용 오버레이 생성)
var transitionEffect = /* 기존 UI에서 가져오거나 생성 */;
_viewBridge.SetTransitionEffect(transitionEffect);
_viewBridge.SetTargetLineManager(_targetLineManager);
```

> 정확한 코드는 AutoChessViewRoot 읽은 후 결정. CanvasGroup이 이미 있으면 그것을 사용, 없으면 전용 오버레이 Canvas 생성.

- [ ] **Step 3: 커밋**

```bash
git add Assets/_Project/Scripts/InGame_New/View/AutoChessViewRoot.cs
git commit -m "feat(InGame_New): ViewRoot에서 BoardTransitionEffect 연결"
```

---

## Chunk 4: 통합 검증

### Task 10: 에디터 테스트

- [ ] **Step 1: 컴파일 확인**

Unity Editor에서 컴파일 에러 없는지 확인.

- [ ] **Step 2: 기존 단일 보드 동작 확인**

기존 1v1 또는 2인 매치에서 전투가 정상 동작하는지 확인. MatchIndex 기본값(0xFF → 전역, 또는 0 → 첫 매치)이 기존 로직을 깨지 않는지 확인.

- [ ] **Step 3: SwitchBoard 호출 테스트**

에디터 스크립트 또는 디버그 버튼으로 `ViewBridge.SwitchBoard(1)` 등을 호출하여:
- UnitView가 정상 회수/재스폰되는지
- 이벤트 필터링이 동작하는지 (다른 매치의 데미지 텍스트가 안 뜨는지)
- 트랜지션 페이드가 동작하는지

- [ ] **Step 4: 최종 커밋**

```bash
git add -A
git commit -m "feat(InGame_New): 멀티보드 뷰 전환 통합 완료"
```
