# InGame_New 튜토리얼 마이그레이션 설계

## 개요

레거시 InGame 전투 시스템에서 동작하던 튜토리얼을 InGame_New 오토체스 시스템으로 마이그레이션한다.
시뮬레이션 순수성을 유지하면서, 레거시 UI 시스템을 최대한 재사용한다.

## 핵심 결정 사항

| 항목 | 결정 |
|------|------|
| Pause 방식 | Runner/Adapter 레벨 (`LocalSimulationRunner`에서 tick 중단) |
| 트리거 범위 | 레거시 전투 트리거 + InGame_New 오토체스 페이즈 트리거 |
| UI 시스템 | 레거시 재사용 (TutorialController, 마스크, 터치 블로커 등) |
| 트리거 감지 | View 레이어 (`AutoChessViewBridge`) 이벤트 기반 |
| SPAWN_ENEMY | Command 기반으로 시뮬레이션에 요청 |

## 현재 상태 분석

### 재사용 가능 (변경 없음)

| 컴포넌트 | 역할 |
|----------|------|
| `TutorialController` | UI 표시, Strategy 패턴 실행, 마스크 애니메이션 |
| `TutorialTargetRegistry` | O(1) 타겟 룩업 |
| `TutorialTouchBlocker` | 3D 터치 블로킹 |
| `TutorialMaskRaycastFilter` | UI 레이캐스트 필터 |
| 기존 `ITutorialActionStrategy` 구현체들 | FOCUS_UI, FORCED_TOUCH_UI, SHOW_DIALOGUE_POP 등 |
| `TutorialDialogue` (SpecData) | 튜토리얼 데이터 구조 |
| `InGameTutorialCanvas.prefab` | UI 프리팹 |

### 수정 필요 (레거시 호환 유지하면서)

| 컴포넌트 | 수정 내용 | 이유 |
|----------|-----------|------|
| `TutorialManager` | `HandleTutorialClose`의 하드코딩된 정적 핸들러 호출을 콜백 기반으로 리팩터링 | 레거시 핸들러 직접 호출(L339-342)이 InGame_New와 충돌 |
| `TutorialManager` | `IsTutorialAction(type, key)` 오버로드 추가 | 현재 key 파라미터 미지원 |

### 교체 대상 (레거시 → InGame_New)

| 레거시 | InGame_New 대체 | 이유 |
|--------|-----------------|------|
| `TutorialSkillReadyHandler` | `TutorialNewSkillReadyHandler` | `InGameMainFlowManager.Pause()` → `LocalSimulationRunner.PauseTick()` |
| `TutorialEnemyDeadAllHandler` | `TutorialNewCombatEndHandler` | 동일 |
| `FlowStateStageCombat` 내 직접 호출 | `TutorialSimBridge` | View 레이어 이벤트 감지 방식으로 전환 |
| `TutorialActionSpawnEnemy` | `TutorialActionSpawnEnemyNew` (신규 Strategy) | 직접 생성 → Command 기반 |

### 신규 추가

| 컴포넌트 | 역할 |
|----------|------|
| `TutorialSimBridge` | AutoChessViewBridge ↔ TutorialManager 연결 |
| `TutorialNewCombatStartHandler` | COMBAT_START 핸들러 |
| `TutorialNewSkillReadyHandler` | SKILL_READY 핸들러 (deferred 로직 포함) |
| `TutorialNewCombatEndHandler` | COMBAT_END 핸들러 |
| `TutorialNewPhaseHandler` | Preparation/Shop 등 페이즈 핸들러 |
| `TutorialActionSpawnEnemyNew` | SPAWN_ENEMY 전략 (Command 기반) |
| `SpawnTutorialEnemy` 커맨드 | 튜토리얼 적 스폰 |
| `LocalSimulationRunner.PauseTick/ResumeTick` | Tick 일시정지 |
| `SimEventType.ManaFull` | 스킬 준비 이벤트 |

## 아키텍처

```
┌─ 기존 재사용 ────────────────────────────────────────────┐
│ TutorialController → ITutorialActionStrategy                  │
│ TutorialTargetRegistry, TouchBlocker, MaskRaycastFilter       │
│ TutorialDialogue (SpecData)                                    │
└───────────────────────────────────────────────────────────────┘
         ▲
         │ HandleTutorialAction() / HandleTutorialClose()
         │
┌─ 수정: TutorialManager ─────────────────────────────────────┐
│ HandleTutorialClose() 콜백 기반 리팩터링                      │
│ ├─ OnTutorialClosed 이벤트 추가                               │
│ ├─ 레거시: 기존 정적 핸들러를 OnTutorialClosed에 등록         │
│ └─ InGame_New: TutorialSimBridge 핸들러를 OnTutorialClosed에 등록│
│ IsTutorialAction(type, key) 오버로드 추가                     │
└───────────────────────────────────────────────────────────────┘
         ▲
         │ 핸들러 등록/해제 + HandleTutorialAction 호출
         │
┌─ 신규: TutorialSimBridge ────────────────────────────────────┐
│                                                               │
│  AutoChessViewBridge 이벤트 감지                              │
│  ├─ OnPhaseChanged → 페이즈 트리거 발행                       │
│  ├─ OnEvent(ManaFull) → SKILL_READY 트리거                    │
│  ├─ OnEvent(CombatResult) → COMBAT_END 트리거                 │
│  └─ OnEvent(UnitPurchased) → SHOP_PURCHASE 트리거             │
│                                                               │
│  핸들러 (Runner Pause/Resume 제어)                            │
│  ├─ TutorialNewCombatStartHandler                             │
│  ├─ TutorialNewSkillReadyHandler (deferred 포함)              │
│  ├─ TutorialNewCombatEndHandler                               │
│  └─ TutorialNewPhaseHandler                                   │
│                                                               │
│  LocalSimulationRunner.PauseTick() / ResumeTick()             │
└───────────────────────────────────────────────────────────────┘
         │
         │ PauseTick() — tick 중단
         ▼
┌─ LocalSimulationRunner ──────────────────────────────────────┐
│ Update()                                                      │
│   if (!_isRunning || _isPausedByTutorial || _world == null)   │
│       return;  // ← _tickAccumulator 증가 전에 반드시 early return │
│   _tickAccumulator += Time.deltaTime;                         │
│   ... tick loop ...                                           │
└───────────────────────────────────────────────────────────────┘
         │
         │ GameLoopSystem.Tick()
         ▼
┌─ Simulation (최소 변경) ─────────────────────────────────────┐
│ SimEventType.ManaFull 추가                                    │
│ CommandType.SpawnTutorialEnemy 추가                            │
│ CommandProcessor에 SpawnTutorialEnemy 처리 분기 추가           │
└───────────────────────────────────────────────────────────────┘
```

## 상세 설계

### 1. TutorialManager 리팩터링

레거시 `HandleTutorialClose()`의 하드코딩된 정적 호출을 콜백 기반으로 전환한다.

```csharp
// TutorialManager.cs 수정

// 신규: 튜토리얼 종료 콜백 이벤트
public event Action OnTutorialClosed;

// IsTutorialAction 오버로드 추가
public bool IsTutorialAction(TutorialTriggerType triggerType, string key)
{
    if (!IsTutorial || _specTutorialDataList.Count == 0) return false;
    return _specTutorialDataList.Find(l =>
        l.tutorial_trigger_type == triggerType &&
        l.tutorial_trigger_key == key) != null;
}

// HandleTutorialClose 수정
public bool HandleTutorialClose(Action action = null)
{
    if (!IsTutorial) return false;

    _tutorialController?.ClearTutorial();

    if (_canvas != null)
        IsTutorialCanvasEnabled = false;

    action?.Invoke();

    // 기존 하드코딩 호출 → 이벤트 기반으로 변경
    // 레거시 핸들러도 InGame_New 핸들러도 이 이벤트에 등록
    OnTutorialClosed?.Invoke();
    OnTutorialClosed = null; // one-shot: 호출 후 구독 해제

    TutorialTouchBlocker.Clear();
    return true;
}
```

**레거시 호환**: 레거시 핸들러들(`TutorialSkillReadyHandler`, `TutorialEnemyDeadAllHandler`, `TutorialActionSpawnEnemy`)은 기존 `HandleTutorialAction()` 호출 시 `OnTutorialClosed`에 자신의 Resume 메서드를 등록하도록 수정한다. 동작은 동일하되 호출 방식만 변경.

### 2. LocalSimulationRunner Pause 확장

```csharp
// LocalSimulationRunner.cs에 추가
private bool _isPausedByTutorial;

public void PauseTick()
{
    _isPausedByTutorial = true;
}

public void ResumeTick()
{
    _isPausedByTutorial = false;
}

// Update() 수정 — 반드시 _tickAccumulator 증가 전에 early return
private void Update()
{
    if (!_isRunning || _isPausedByTutorial || _world == null) return;
    // ↑ _tickAccumulator += Time.deltaTime보다 반드시 위에 위치
    // Pause 중 deltaTime 누적을 방지하여 Resume 후 틱 폭주 차단
    _tickAccumulator += Time.deltaTime;
    // ... 기존 tick 로직 ...
}
```

### 3. SimEventType.ManaFull 추가

```csharp
// SimulationEvents.cs - SimEventType enum에 추가
ManaFull,  // 유닛 마나가 가득 참 (스킬 사용 가능)

// SimEventQueue에 헬퍼 추가
public void PushManaFull(int entityId, int skillSpecId)
{
    Push(new SimEvent
    {
        Type = SimEventType.ManaFull,
        EntityId = entityId,
        Param0 = skillSpecId,
    });
}
```

**발행 위치**: `CombatAISystem`에서 유닛 마나가 MaxMana에 도달했을 때 Push.
기존 `SkillSystem.TryCast()` 직전 또는 마나 증가 로직에서 감지.

**이벤트 소실 대응**: `SimEventQueue.MaxEvents`(128개) 초과 시 이벤트가 조용히 드롭된다.
ManaFull은 유닛당 전투 중 1회만 발행되도록 플래그 관리한다 (유닛에 `HasPushedManaFull` 플래그).
추가로, View에서 `GameWorld` 유닛 마나 상태를 직접 폴링하는 fallback도 고려한다.

### 4. SpawnTutorialEnemy 커맨드

```csharp
// Enums.cs - CommandType에 추가
SpawnTutorialEnemy,

// Commands.cs - 팩토리 메서드 추가
public static GameCommand SpawnTutorialEnemy(byte playerIndex, int monsterSpecId, int col, int row)
    => new GameCommand
    {
        Type = CommandType.SpawnTutorialEnemy,
        PlayerIndex = playerIndex,
        Param0 = monsterSpecId,
        Param1 = col,
        Param2 = row,
    };
```

**CommandProcessor 처리**:
- 페이즈 검증: Combat 페이즈에서만 허용
- `CombatSetupSystem.SpawnTutorialUnit(matchState, monsterSpecId, col, row)` 호출
- 유닛 생성 후 `SimEventType.UnitSpawned` 이벤트 Push

### 5. TutorialSimBridge

```csharp
// Assets/_Project/Scripts/InGame_New/View/Tutorial/TutorialSimBridge.cs

public class TutorialSimBridge : IDisposable
{
    // AutoChessViewRoot에서 생성 시 설정, Strategy에서 접근 가능하도록 static
    public static TutorialSimBridge Instance { get; private set; }

    private readonly LocalSimulationRunner _localRunner;

    // 핸들러들
    private readonly TutorialNewCombatStartHandler _combatStartHandler;
    private readonly TutorialNewSkillReadyHandler _skillReadyHandler;
    private readonly TutorialNewCombatEndHandler _combatEndHandler;
    private readonly TutorialNewPhaseHandler _phaseHandler;

    public TutorialSimBridge(LocalSimulationRunner localRunner)
    {
        _localRunner = localRunner;
        Instance = this;

        _combatStartHandler = new TutorialNewCombatStartHandler(localRunner);
        _skillReadyHandler = new TutorialNewSkillReadyHandler(localRunner);
        _combatEndHandler = new TutorialNewCombatEndHandler(localRunner);
        _phaseHandler = new TutorialNewPhaseHandler(localRunner);
    }

    // AutoChessViewBridge에서 호출
    public void OnPhaseChanged(GamePhase prev, GamePhase current)
    {
        switch (current)
        {
            case GamePhase.Preparation:
                _phaseHandler.TryHandleTutorial(TutorialTriggerType.PREPARATION_START);
                break;
            case GamePhase.Combat:
                _combatStartHandler.TryHandleTutorial();
                break;
        }
    }

    // AutoChessViewBridge.DispatchEvent에서 호출
    public void OnSimEvent(ref SimEvent evt, GameWorld world)
    {
        switch (evt.Type)
        {
            case SimEventType.ManaFull:
                _skillReadyHandler.TryHandleTutorial(evt.EntityId);
                break;
            case SimEventType.CombatResult:
                _combatEndHandler.TryHandleTutorial();
                break;
            case SimEventType.UnitPurchased:
                _phaseHandler.TryHandleTutorial(TutorialTriggerType.SHOP_PURCHASE);
                break;
        }
    }

    // 튜토리얼 완료 후 SPAWN_ENEMY 커맨드 전송
    public void EnqueueSpawnCommand(int monsterSpecId, int col, int row)
    {
        _localRunner.EnqueueCommand(
            GameCommand.SpawnTutorialEnemy(0, monsterSpecId, col, row));
    }

    public void Dispose()
    {
        // 이벤트 구독 해제 (CLAUDE.md 규칙: Dispose 호출 필수)
        _combatStartHandler.Dispose();
        _skillReadyHandler.Dispose();
        _combatEndHandler.Dispose();
        _phaseHandler.Dispose();

        if (Instance == this)
            Instance = null;
    }
}
```

### 6. 핸들러 패턴

각 핸들러는 레거시 패턴을 따르되, Pause 대상은 `LocalSimulationRunner`, Resume는 `TutorialManager.OnTutorialClosed` 이벤트로 처리:

```csharp
public class TutorialNewSkillReadyHandler : IDisposable
{
    private readonly LocalSimulationRunner _runner;
    private bool _isPaused;
    private int _deferredEntityId = -1; // CHARACTER_DEAD 보류용

    public TutorialNewSkillReadyHandler(LocalSimulationRunner runner)
    {
        _runner = runner;
    }

    public bool TryHandleTutorial(int entityId)
    {
        // CHARACTER_DEAD가 아직 대기 중이면 SKILL_READY 보류 (seq 순서 보장)
        if (TutorialManager.Instance.IsTutorialAction(TutorialTriggerType.CHARACTER_DEAD))
        {
            _deferredEntityId = entityId;
            return true; // 소비하되 실행은 보류
        }

        if (!TutorialManager.Instance.IsTutorialAction(TutorialTriggerType.SKILL_READY))
            return false;

        _isPaused = true;
        _runner.PauseTick();

        // OnTutorialClosed에 Resume 등록 (one-shot)
        TutorialManager.Instance.OnTutorialClosed += ResumeAfterTutorial;

        TutorialManager.Instance.HandleTutorialAction(
            TutorialTriggerType.SKILL_READY, entityId.ToString());

        return true;
    }

    // CHARACTER_DEAD 튜토리얼 완료 후 보류된 SKILL_READY 처리
    public void TryProcessDeferred()
    {
        if (_deferredEntityId < 0) return;
        int entityId = _deferredEntityId;
        _deferredEntityId = -1;
        TryHandleTutorial(entityId);
    }

    private void ResumeAfterTutorial()
    {
        if (!_isPaused) return;
        _isPaused = false;
        _runner.ResumeTick();
    }

    public void Dispose()
    {
        // 강제 종료 시 이벤트 구독 해제
        if (_isPaused)
            TutorialManager.Instance.OnTutorialClosed -= ResumeAfterTutorial;
        _isPaused = false;
        _deferredEntityId = -1;
    }
}
```

### 7. TutorialActionSpawnEnemyNew (신규 Strategy)

레거시 `TutorialActionSpawnEnemy`는 수정하지 않고, 신규 Strategy를 별도 생성한다.

```csharp
// Assets/_Project/Scripts/InGame_New/View/Tutorial/TutorialActionSpawnEnemyNew.cs

public class TutorialActionSpawnEnemyNew : ITutorialActionStrategy
{
    public override async UniTask OnShow(TutorialActionContext context)
    {
        var bridge = TutorialSimBridge.Instance;
        // tutorial_action_key에서 monsterSpecId, col, row 파싱
        // 순차 스폰 (0.4~0.6s 간격)
        foreach (var spawnData in ParseSpawnData(context.CurrentTutorial))
        {
            bridge.EnqueueSpawnCommand(spawnData.MonsterSpecId, spawnData.Col, spawnData.Row);
            await UniTask.Delay(UnityEngine.Random.Range(400, 600));
        }
    }
    // ...
}
```

**Strategy 분기**: `TutorialController`의 Strategy 팩토리에서 InGame_New 모드일 때 `SPAWN_ENEMY` → `TutorialActionSpawnEnemyNew`로 매핑.
`TutorialController`에 `IsInGameNewMode` 플래그 추가, 또는 `TutorialManager` 초기화 시 Strategy 오버라이드 딕셔너리를 전달하는 방식.

### 8. AutoChessViewBridge 통합

```csharp
// AutoChessViewBridge.cs 수정

private TutorialSimBridge _tutorialBridge;

public void SetTutorialBridge(TutorialSimBridge bridge)
{
    _tutorialBridge = bridge;
}

// HandlePhaseChanged에 추가
private void HandlePhaseChanged(GamePhase prev, GamePhase current)
{
    _tutorialBridge?.OnPhaseChanged(prev, current);
    // ... 기존 로직 ...
}

// DispatchEvent에 추가
private void DispatchEvent(ref SimEvent evt, GameWorld world)
{
    _tutorialBridge?.OnSimEvent(ref evt, world);
    // ... 기존 switch 문 ...
}
```

## 트리거 매핑

### 레거시 트리거 → InGame_New 감지 위치

| 트리거 | 레거시 감지 위치 | InGame_New 감지 위치 |
|--------|------------------|---------------------|
| COMBAT_START | FlowStateStageCombat.StateInit | TutorialSimBridge.OnPhaseChanged(Combat) |
| SKILL_READY | CharacterController.Combat | TutorialSimBridge.OnSimEvent(ManaFull) |
| ENEMY_DEAD_ALL | FlowStateStageCombat.StateRunning | TutorialSimBridge.OnSimEvent(CombatResult) |
| CHARACTER_DEAD | FlowStateStageCombat.StateRunning | TutorialSimBridge.OnSimEvent(UnitDied) + 조건 체크 |
| SPAWN_ENEMY | TutorialActionSpawnEnemy 직접 생성 | SpawnTutorialEnemy 커맨드 → 시뮬레이션 |

### InGame_New 신규 트리거

| 트리거 | 감지 위치 | 설명 |
|--------|-----------|------|
| PREPARATION_START | OnPhaseChanged(Preparation) | 준비 페이즈 진입 |
| SHOP_PURCHASE | OnSimEvent(UnitPurchased) | 유닛 구매 |
| UNIT_PLACED | OnSimEvent(UnitMoved) + 조건 체크 | 보드에 유닛 배치 |
| SYNERGY_ACTIVATED | OnSimEvent(SynergyUpdated) | 시너지 발동 |
| COMBAT_END | OnSimEvent(CombatResult) | 전투 종료 |

**SpecEnums.cs의 `TutorialTriggerType`에 신규 값 추가 필요.**

## CombatResult Pause 타이밍

`CombatResult` 이벤트가 View에 도착하는 시점에 이미 `OnEnterResult()`(데미지 처리, 탈락 처리)가 완료된 상태다.
따라서 COMBAT_END 튜토리얼은 **전투 결과 처리 후, Result 페이즈 UI 표시 전**에 Pause된다.
전투 결과 처리 전에 Pause해야 하는 요구사항이 향후 발생하면, 시뮬레이션 레벨 확장이 필요하다 (현재 스코프 외).

## 시뮬레이션 영향 범위 (최소)

| 파일 | 변경 내용 | 영향도 |
|------|-----------|--------|
| `SimulationEvents.cs` | `ManaFull` 이벤트 타입 + Push 헬퍼 | 최소 (enum 값 1개, 메서드 1개) |
| `Enums.cs` | `CommandType.SpawnTutorialEnemy` | 최소 (enum 값 1개) |
| `Commands.cs` | `SpawnTutorialEnemy` 팩토리 메서드 | 최소 (메서드 1개) |
| `CommandProcessor.cs` | `SpawnTutorialEnemy` 처리 분기 | 최소 (case 1개) |
| `CombatAISystem.cs` | 마나 풀 시 `PushManaFull` 호출 + 유닛당 1회 플래그 | 최소 (조건문 1개) |
| `CombatSetupSystem.cs` | `SpawnTutorialUnit` 메서드 | 소규모 (메서드 1개) |

**시뮬레이션 코어 로직 변경 없음. 이벤트/커맨드 확장만.**

## 레거시 코드 수정 범위

| 파일 | 변경 내용 | 영향도 |
|------|-----------|--------|
| `TutorialManager.cs` | `OnTutorialClosed` 이벤트 추가, `HandleTutorialClose` 정적 호출 → 이벤트 발행, `IsTutorialAction` 오버로드 | 소규모 |
| `TutorialSkillReadyHandler.cs` | `TutorialManager.OnTutorialClosed`에 Resume 등록 방식으로 변경 | 최소 |
| `TutorialEnemyDeadAllHandler.cs` | 동일 | 최소 |
| `TutorialActionSpawnEnemy.cs` | 동일 | 최소 |
| `SpecEnums.cs` | `TutorialTriggerType`에 신규 값 추가 | 최소 |

## View 레이어 변경

| 파일 | 변경 내용 |
|------|-----------|
| `LocalSimulationRunner.cs` | `PauseTick()` / `ResumeTick()` 추가, Update early return 위치 주의 |
| `AutoChessViewBridge.cs` | `TutorialSimBridge` 참조 + 이벤트 전달 |
| `AutoChessViewRoot.cs` | `TutorialSimBridge` 생성/초기화 |

## 폴더 구조

```
Assets/_Project/Scripts/InGame_New/View/Tutorial/
├── TutorialSimBridge.cs                # 메인 브릿지 (이벤트 감지 + 핸들러 관리)
├── TutorialNewCombatStartHandler.cs    # COMBAT_START 핸들러
├── TutorialNewSkillReadyHandler.cs     # SKILL_READY 핸들러 (deferred 포함)
├── TutorialNewCombatEndHandler.cs      # COMBAT_END 핸들러
├── TutorialNewPhaseHandler.cs          # 페이즈 트리거 핸들러
└── TutorialActionSpawnEnemyNew.cs      # SPAWN_ENEMY 전략 (Command 기반)
```

## 제외 사항

- 레거시 InGame용 튜토리얼 코드 삭제하지 않음 (레거시 전투 모드 공존 가능)
- 슬로우 모션 연출 (레거시 `TutorialEnemyDeadAllHandler`의 0.4x 연출)은 1차 마이그레이션에서 제외, 추후 추가
- 전투 결과 처리 전 Pause (시뮬레이션 레벨 확장 필요)는 현재 스코프 외
- 새로운 튜토리얼 시나리오 기획/데이터는 이 설계 범위 밖
