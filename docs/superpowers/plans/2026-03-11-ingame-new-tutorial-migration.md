# InGame_New 튜토리얼 마이그레이션 구현 계획

> **For agentic workers:** REQUIRED: Use superpowers:subagent-driven-development (if subagents available) or superpowers:executing-plans to implement this plan. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 레거시 InGame 전투 튜토리얼을 InGame_New 오토체스 시스템으로 마이그레이션한다.

**Architecture:** Runner/Adapter 레벨 Pause, View 레이어 이벤트 감지, 레거시 UI 재사용. TutorialManager의 HandleTutorialClose를 콜백 기반으로 리팩터링하고, TutorialSimBridge가 AutoChessViewBridge 이벤트를 감지하여 TutorialManager에 전달한다.

**Tech Stack:** Unity 6, C#, UniTask, InGame_New Simulation/View 분리 구조

**Spec:** `Assets/_Project/docs/superpowers/specs/2026-03-11-ingame-new-tutorial-migration-design.md`

---

## 파일 구조

### 신규 생성
| 파일 | 역할 |
|------|------|
| `Scripts/InGame_New/View/Tutorial/TutorialSimBridge.cs` | AutoChessViewBridge ↔ TutorialManager 연결 |
| `Scripts/InGame_New/View/Tutorial/TutorialNewCombatStartHandler.cs` | COMBAT_START 핸들러 |
| `Scripts/InGame_New/View/Tutorial/TutorialNewSkillReadyHandler.cs` | SKILL_READY 핸들러 (deferred 포함) |
| `Scripts/InGame_New/View/Tutorial/TutorialNewCombatEndHandler.cs` | COMBAT_END 핸들러 |
| `Scripts/InGame_New/View/Tutorial/TutorialNewPhaseHandler.cs` | 페이즈 트리거 핸들러 |
| `Scripts/InGame_New/View/Tutorial/TutorialActionSpawnEnemyNew.cs` | SPAWN_ENEMY 전략 (Command 기반) |

### 수정
| 파일 | 변경 내용 |
|------|-----------|
| `Scripts/Common/Managers/TutorialManager.cs` | OnTutorialClosed 이벤트, IsTutorialAction 오버로드, HandleTutorialClose 리팩터링 |
| `Scripts/Common/Tutorial/TutorialSkillReadyHandler.cs` | OnTutorialClosed 이벤트 등록 방식으로 전환 |
| `Scripts/Common/Tutorial/TutorialEnemyDeadAllHandler.cs` | 동일 |
| `Scripts/Common/Tutorial/TutorialActionSpawnEnemy.cs` | 동일 |
| `Scripts/InGame_New/Simulation/Data/SimulationEvents.cs` | ManaFull 이벤트 추가 |
| `Scripts/InGame_New/Simulation/Data/Enums.cs` | CommandType.SpawnTutorialEnemy 추가 |
| `Scripts/InGame_New/Simulation/Data/Commands.cs` | SpawnTutorialEnemy 팩토리 추가 |
| `Scripts/InGame_New/Simulation/Core/CommandProcessor.cs` | SpawnTutorialEnemy 처리 |
| `Scripts/InGame_New/Simulation/Combat/CombatAISystem.cs` | ManaFull 이벤트 발행 |
| `Scripts/InGame_New/Simulation/Combat/CombatSetupSystem.cs` | SpawnTutorialUnit 메서드 |
| `Scripts/InGame_New/Adapter/Local/LocalSimulationRunner.cs` | PauseTick/ResumeTick |
| `Scripts/InGame_New/View/AutoChessViewBridge.cs` | TutorialSimBridge 연결 |
| `Scripts/InGame_New/View/AutoChessViewRoot.cs` | TutorialSimBridge 생성 |
| `Scripts/Spec/SpecData/SpecEnums.cs` | TutorialTriggerType 신규 값 |
| `Scripts/Common/TutorialController.cs` | Strategy 분기 (InGame_New 모드) |

---

## Chunk 1: TutorialManager 리팩터링 + 레거시 핸들러 전환

### Task 1: TutorialManager에 OnTutorialClosed 이벤트 추가

**Files:**
- Modify: `Assets/_Project/Scripts/Common/Managers/TutorialManager.cs:261-268, 320-359`

- [ ] **Step 1: TutorialManager에 OnTutorialClosed 이벤트 필드 추가**

`TutorialManager.cs`의 멤버 필드 영역에 추가:

```csharp
/// <summary>
/// HandleTutorialClose 호출 시 발행. one-shot: 발행 후 자동 해제.
/// 레거시/InGame_New 핸들러 모두 이 이벤트에 Resume 콜백 등록.
/// </summary>
public event Action OnTutorialClosed;
```

- [ ] **Step 2: HandleTutorialClose에서 하드코딩 정적 호출을 OnTutorialClosed로 교체**

`TutorialManager.cs` L338-342의 기존 코드:
```csharp
// 인게임 전용 핸들러 처리
TutorialActionSpawnEnemy.ResumeGameIfPaused();
TutorialSkillReadyHandler.ResumeAndActivateSkill();
TutorialEnemyDeadAllHandler.ResumeAndEndCombat();
TutorialSkillReadyHandler.TryProcessDeferredSkillReady();
```

다음으로 교체:
```csharp
// 인게임 핸들러 처리 (레거시/InGame_New 모두 이벤트 기반)
var closed = OnTutorialClosed;
OnTutorialClosed = null; // one-shot: 호출 전 구독 해제하여 재진입 방지
closed?.Invoke();
```

- [ ] **Step 3: IsTutorialAction 오버로드 추가**

`TutorialManager.cs` L268 뒤에 추가:

```csharp
public bool IsTutorialAction(TutorialTriggerType tutorialTriggerType, string key)
{
    if (!IsTutorial || _specTutorialDataList.Count == 0)
    {
        return false;
    }
    return _specTutorialDataList.Find(l =>
        l.tutorial_trigger_type == tutorialTriggerType &&
        l.tutorial_trigger_key == key) != null;
}
```

- [ ] **Step 4: 컴파일 확인**

Unity Editor에서 컴파일 에러 없는지 확인.

- [ ] **Step 5: 커밋**

```bash
git add Assets/_Project/Scripts/Common/Managers/TutorialManager.cs
git commit -m "refactor: TutorialManager HandleTutorialClose를 OnTutorialClosed 이벤트 기반으로 전환"
```

---

### Task 2: 레거시 TutorialSkillReadyHandler를 이벤트 등록 방식으로 전환

**Files:**
- Modify: `Assets/_Project/Scripts/Common/Tutorial/TutorialSkillReadyHandler.cs`

- [ ] **Step 1: ResumeAndActivateSkill을 OnTutorialClosed에 등록하는 방식으로 변경**

`TutorialSkillReadyHandler.cs`에서 `ProcessSkillReadyTutorial` 메서드 (L65-95) 내에서 `HandleTutorialAction` 호출 직전에 OnTutorialClosed 구독 추가:

```csharp
// HandleTutorialAction 호출 전에 Resume 콜백 등록
TutorialManager.Instance.OnTutorialClosed += ResumeAndActivateSkill;
```

그리고 `ResumeAndActivateSkill` (L101-125)에서 시작 부분에 구독 해제 추가:

```csharp
public static void ResumeAndActivateSkill()
{
    // OnTutorialClosed에서 호출되므로 구독 해제 불필요 (one-shot)
    // 기존 로직 그대로 유지
```

`TryProcessDeferredSkillReady` (L132-159)도 동일하게 OnTutorialClosed 등록:

```csharp
TutorialManager.Instance.OnTutorialClosed += TryProcessDeferredSkillReady;
```

- [ ] **Step 2: 컴파일 및 레거시 동작 확인**

Unity Editor에서 컴파일 확인. 레거시 InGame 튜토리얼이 기존과 동일하게 동작하는지 수동 테스트.

- [ ] **Step 3: 커밋**

```bash
git add Assets/_Project/Scripts/Common/Tutorial/TutorialSkillReadyHandler.cs
git commit -m "refactor: TutorialSkillReadyHandler를 OnTutorialClosed 이벤트 등록 방식으로 전환"
```

---

### Task 3: 레거시 TutorialEnemyDeadAllHandler를 이벤트 등록 방식으로 전환

**Files:**
- Modify: `Assets/_Project/Scripts/Common/Tutorial/TutorialEnemyDeadAllHandler.cs`

- [ ] **Step 1: ResumeAndEndCombat을 OnTutorialClosed에 등록**

`TutorialEnemyDeadAllHandler.cs`의 `SlowMotionThenShowTutorial` 메서드 (L74-103) 내 `HandleTutorialAction` 호출 직전에:

```csharp
TutorialManager.Instance.OnTutorialClosed += ResumeAndEndCombat;
```

- [ ] **Step 2: 컴파일 확인**

- [ ] **Step 3: 커밋**

```bash
git add Assets/_Project/Scripts/Common/Tutorial/TutorialEnemyDeadAllHandler.cs
git commit -m "refactor: TutorialEnemyDeadAllHandler를 OnTutorialClosed 이벤트 등록 방식으로 전환"
```

---

### Task 4: 레거시 TutorialActionSpawnEnemy를 이벤트 등록 방식으로 전환

**Files:**
- Modify: `Assets/_Project/Scripts/Common/Tutorial/TutorialActionSpawnEnemy.cs`

- [ ] **Step 1: ResumeGameIfPaused를 OnTutorialClosed에 등록**

`TutorialActionSpawnEnemy.cs`에서 게임을 일시정지하는 시점(OnShow 내)에:

```csharp
TutorialManager.Instance.OnTutorialClosed += ResumeGameIfPaused;
```

- [ ] **Step 2: 컴파일 확인**

- [ ] **Step 3: 커밋**

```bash
git add Assets/_Project/Scripts/Common/Tutorial/TutorialActionSpawnEnemy.cs
git commit -m "refactor: TutorialActionSpawnEnemy를 OnTutorialClosed 이벤트 등록 방식으로 전환"
```

---

## Chunk 2: Simulation 레이어 확장

### Task 5: SimEventType.ManaFull 이벤트 추가

**Files:**
- Modify: `Assets/_Project/Scripts/InGame_New/Simulation/Data/SimulationEvents.cs:37-40, 273-285`

- [ ] **Step 1: SimEventType enum에 ManaFull 추가**

`SimulationEvents.cs` L40 (`SkillRectAreaEffect` 뒤)에 추가:

```csharp
ManaFull,
```

- [ ] **Step 2: SimEventQueue에 PushManaFull 헬퍼 추가**

`SimulationEvents.cs`의 마지막 Push 헬퍼(`PushSkillRectAreaEffect`, L273-285) 뒤에 추가:

```csharp
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

- [ ] **Step 3: 컴파일 확인**

- [ ] **Step 4: 커밋**

```bash
git add Assets/_Project/Scripts/InGame_New/Simulation/Data/SimulationEvents.cs
git commit -m "feat: SimEventType.ManaFull 이벤트 추가"
```

---

### Task 6: CombatAISystem에서 ManaFull 이벤트 발행

**Files:**
- Modify: `Assets/_Project/Scripts/InGame_New/Simulation/Combat/CombatAISystem.cs:152-158`
- Modify: `Assets/_Project/Scripts/InGame_New/Simulation/Data/Components.cs` (CombatUnit에 플래그)

- [ ] **Step 1: CombatUnit에 HasPushedManaFull 플래그 추가**

`Components.cs`의 `CombatUnit` struct에 추가:

```csharp
public bool HasPushedManaFull; // ManaFull 이벤트 중복 발행 방지
```

- [ ] **Step 2: CombatAISystem 마나 체크 부분에 ManaFull Push 추가**

`CombatAISystem.cs` L152-158 근처, 마나 체크 로직:

```csharp
if (unit.CurrentMana >= unit.MaxMana && unit.MaxMana > 0)
```

이 조건문 안, `SkillSystem.TryCast` 호출 전에 추가:

```csharp
if (!unit.HasPushedManaFull)
{
    unit.HasPushedManaFull = true;
    state.EventQueue?.PushManaFull(unit.EntityId, unit.SkillSpecId);
}
```

- [ ] **Step 3: CombatSetupSystem에서 HasPushedManaFull 초기화 확인**

`CombatSetupSystem.cs`에서 `CombatUnit` 초기화 시 `HasPushedManaFull = false`가 기본값이므로 별도 설정 불필요 (bool 기본값 false).

- [ ] **Step 4: 컴파일 확인**

- [ ] **Step 5: 커밋**

```bash
git add Assets/_Project/Scripts/InGame_New/Simulation/Combat/CombatAISystem.cs Assets/_Project/Scripts/InGame_New/Simulation/Data/Components.cs
git commit -m "feat: CombatAISystem에서 마나 풀 시 ManaFull 이벤트 발행"
```

---

### Task 7: SpawnTutorialEnemy 커맨드 추가

**Files:**
- Modify: `Assets/_Project/Scripts/InGame_New/Simulation/Data/Enums.cs:79-94`
- Modify: `Assets/_Project/Scripts/InGame_New/Simulation/Data/Commands.cs:159-167`
- Modify: `Assets/_Project/Scripts/InGame_New/Simulation/Core/CommandProcessor.cs:26-67, 71-95`

- [ ] **Step 1: CommandType enum에 SpawnTutorialEnemy 추가**

`Enums.cs`의 `CommandType` enum 마지막 항목 뒤에 추가:

```csharp
SpawnTutorialEnemy,
```

- [ ] **Step 2: GameCommand에 팩토리 메서드 추가**

`Commands.cs`의 마지막 팩토리 메서드(`UnequipItem`, L159-167) 뒤에 추가:

```csharp
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

- [ ] **Step 3: CommandProcessor.IsCommandAllowedInPhase에 SpawnTutorialEnemy 허용**

`CommandProcessor.cs`의 `IsCommandAllowedInPhase` (L71-95)에서 Combat 페이즈 허용 목록에 추가:

```csharp
case CommandType.SpawnTutorialEnemy:
    return phase == GamePhase.Combat;
```

- [ ] **Step 4: CommandProcessor.ProcessCommand switch에 처리 분기 추가**

`CommandProcessor.cs`의 `ProcessCommand` switch 문 (L26-67)에 추가:

```csharp
case CommandType.SpawnTutorialEnemy:
    ProcessSpawnTutorialEnemy(world, in cmd);
    break;
```

그리고 메서드 추가:

```csharp
private static void ProcessSpawnTutorialEnemy(GameWorld world, in GameCommand cmd)
{
    var matchIndex = world.FindMatchIndexForPlayer(cmd.PlayerIndex);
    if (matchIndex < 0) return;

    ref var match = ref world.Matches[matchIndex];
    CombatSetupSystem.SpawnTutorialUnit(ref match, cmd.Param0, cmd.Param1, cmd.Param2);
}
```

- [ ] **Step 5: 컴파일 확인**

`CombatSetupSystem.SpawnTutorialUnit`은 아직 없으므로 컴파일 에러 예상. Task 8에서 구현.

- [ ] **Step 6: 커밋 (Task 8과 함께)**

---

### Task 8: CombatSetupSystem.SpawnTutorialUnit 구현

**Files:**
- Modify: `Assets/_Project/Scripts/InGame_New/Simulation/Combat/CombatSetupSystem.cs`

- [ ] **Step 1: SpawnTutorialUnit 메서드 추가**

`CombatSetupSystem.cs`에 추가. 기존 `SpawnPvEEnemies` (L195-250) 패턴을 참고:

```csharp
/// <summary>
/// 튜토리얼용 적 유닛을 전투 중 동적으로 스폰한다.
/// TutorialSimBridge → Command → CommandProcessor에서 호출.
/// </summary>
public static void SpawnTutorialUnit(ref CombatMatchState match, int monsterSpecId, int col, int row)
{
    // 빈 슬롯 찾기
    int slot = -1;
    for (int i = 0; i < match.Units.Length; i++)
    {
        if (!match.Units[i].IsAlive && match.Units[i].EntityId == 0)
        {
            slot = i;
            break;
        }
    }
    if (slot < 0) return; // 슬롯 부족

    var spec = AutoChessSpecAdapter.GetMonsterSpec(monsterSpecId);
    if (spec == null) return;

    ref var unit = ref match.Units[slot];
    unit = new CombatUnit
    {
        EntityId = match.NextEntityId++,
        SpecId = monsterSpecId,
        TeamIndex = 1, // 적 팀
        Col = col,
        Row = row,
        IsAlive = true,
        CurrentHp = spec.Hp,
        MaxHp = spec.Hp,
        Attack = spec.Attack,
        AttackRange = spec.AttackRange,
        AttackSpeed = spec.AttackSpeed,
        MoveSpeed = spec.MoveSpeed,
        CurrentMana = 0,
        MaxMana = spec.MaxMana,
        CombatState = CombatState.Idle,
    };

    // 그리드 등록
    match.SetGrid(col, row, unit.EntityId);

    // UnitSpawned 이벤트
    match.EventQueue?.Push(new SimEvent
    {
        Type = SimEventType.UnitSpawned,
        EntityId = unit.EntityId,
        Param0 = monsterSpecId,
        Param1 = col,
        Param2 = row,
    });
}
```

> **Note**: 실제 `CombatUnit` 필드명과 `AutoChessSpecAdapter.GetMonsterSpec` 시그니처는 기존 코드에 맞춰 조정 필요. 위는 패턴 참고용.

- [ ] **Step 2: 컴파일 확인**

Task 7 + Task 8 함께 컴파일 확인.

- [ ] **Step 3: 커밋**

```bash
git add Assets/_Project/Scripts/InGame_New/Simulation/Data/Enums.cs Assets/_Project/Scripts/InGame_New/Simulation/Data/Commands.cs Assets/_Project/Scripts/InGame_New/Simulation/Core/CommandProcessor.cs Assets/_Project/Scripts/InGame_New/Simulation/Combat/CombatSetupSystem.cs
git commit -m "feat: SpawnTutorialEnemy 커맨드 + CombatSetupSystem.SpawnTutorialUnit 구현"
```

---

### Task 9: SpecEnums.cs에 신규 TutorialTriggerType 추가

**Files:**
- Modify: `Assets/_Project/Scripts/Spec/SpecData/SpecEnums.cs:956-988`

- [ ] **Step 1: TutorialTriggerType enum에 InGame_New 트리거 추가**

`SpecEnums.cs`의 `TutorialTriggerType` enum (L956-988) 마지막 항목 뒤에 추가:

```csharp
// InGame_New 오토체스 전용 트리거
PREPARATION_START = 30,
SHOP_PURCHASE = 31,
UNIT_PLACED = 32,
SYNERGY_ACTIVATED = 33,
COMBAT_END = 34,
```

> **Note**: enum 값은 기존 마지막 값(FOCUS_OBJECT=29) 다음부터 할당.

- [ ] **Step 2: 컴파일 확인**

- [ ] **Step 3: 커밋**

```bash
git add Assets/_Project/Scripts/Spec/SpecData/SpecEnums.cs
git commit -m "feat: TutorialTriggerType에 InGame_New 오토체스 트리거 추가"
```

---

## Chunk 3: LocalSimulationRunner Pause + View 레이어 연결

### Task 10: LocalSimulationRunner PauseTick/ResumeTick 구현

**Files:**
- Modify: `Assets/_Project/Scripts/InGame_New/Adapter/Local/LocalSimulationRunner.cs:23, 86-131`

- [ ] **Step 1: _isPausedByTutorial 필드 추가**

`LocalSimulationRunner.cs` L23 (`_isRunning` 선언) 근처에 추가:

```csharp
private bool _isPausedByTutorial;
```

- [ ] **Step 2: PauseTick/ResumeTick 메서드 추가**

```csharp
public void PauseTick()
{
    _isPausedByTutorial = true;
}

public void ResumeTick()
{
    _isPausedByTutorial = false;
}
```

- [ ] **Step 3: Update()에 early return 추가**

`LocalSimulationRunner.cs` L86의 기존 코드:
```csharp
if (!_isRunning || _world == null) return;
```

다음으로 변경:
```csharp
if (!_isRunning || _isPausedByTutorial || _world == null) return;
```

> **중요**: 이 early return은 `_tickAccumulator += Time.deltaTime` 보다 반드시 위에 위치해야 한다. Pause 중 deltaTime 누적을 방지하여 Resume 후 틱 폭주를 차단.

- [ ] **Step 4: 컴파일 확인**

- [ ] **Step 5: 커밋**

```bash
git add Assets/_Project/Scripts/InGame_New/Adapter/Local/LocalSimulationRunner.cs
git commit -m "feat: LocalSimulationRunner에 PauseTick/ResumeTick 추가"
```

---

### Task 11: AutoChessViewBridge에 TutorialSimBridge 연결

**Files:**
- Modify: `Assets/_Project/Scripts/InGame_New/View/AutoChessViewBridge.cs:87-128, 168-178`

- [ ] **Step 1: _tutorialBridge 필드 + setter 추가**

`AutoChessViewBridge.cs` 멤버 필드 영역에 추가:

```csharp
private TutorialSimBridge _tutorialBridge;

public void SetTutorialBridge(TutorialSimBridge bridge)
{
    _tutorialBridge = bridge;
}
```

- [ ] **Step 2: HandlePhaseChanged에 튜토리얼 브릿지 호출 추가**

`HandlePhaseChanged` 메서드 (L87-128) 시작 부분에 추가:

```csharp
_tutorialBridge?.OnPhaseChanged(prev, current);
```

- [ ] **Step 3: DispatchEvent에 튜토리얼 브릿지 호출 추가**

`DispatchEvent` 메서드 (L168) 시작 부분에 추가:

```csharp
_tutorialBridge?.OnSimEvent(ref evt, world);
```

- [ ] **Step 4: 컴파일 확인**

`TutorialSimBridge` 클래스가 아직 없으므로 컴파일 에러 예상. Task 12에서 생성.

---

### Task 12: TutorialSimBridge 구현

**Files:**
- Create: `Assets/_Project/Scripts/InGame_New/View/Tutorial/TutorialSimBridge.cs`

- [ ] **Step 1: 폴더 생성 확인**

```bash
ls Assets/_Project/Scripts/InGame_New/View/
# Tutorial 폴더가 없으면 생성됨 (파일 생성 시 자동)
```

- [ ] **Step 2: TutorialSimBridge.cs 작성**

```csharp
using System;
using CookApps.AutoBattler;

namespace CookApps.AutoBattler.InGameNew
{
    public class TutorialSimBridge : IDisposable
    {
        public static TutorialSimBridge Instance { get; private set; }

        private readonly LocalSimulationRunner _localRunner;

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
                case SimEventType.UnitMoved:
                    _phaseHandler.TryHandleTutorial(TutorialTriggerType.UNIT_PLACED);
                    break;
                case SimEventType.SynergyUpdated:
                    _phaseHandler.TryHandleTutorial(TutorialTriggerType.SYNERGY_ACTIVATED);
                    break;
            }
        }

        public void EnqueueSpawnCommand(int monsterSpecId, int col, int row)
        {
            _localRunner.EnqueueCommand(
                GameCommand.SpawnTutorialEnemy(0, monsterSpecId, col, row));
        }

        public void Dispose()
        {
            _combatStartHandler.Dispose();
            _skillReadyHandler.Dispose();
            _combatEndHandler.Dispose();
            _phaseHandler.Dispose();

            if (Instance == this)
                Instance = null;
        }
    }
}
```

> **Note**: 네임스페이스는 InGame_New View 레이어의 기존 패턴에 맞춰 조정. `GamePhase`, `SimEvent` 등의 using도 실제 코드에 맞춰 추가.

- [ ] **Step 3: 컴파일 확인**

핸들러 클래스들이 아직 없으므로 Task 13-16에서 생성 후 함께 컴파일.

---

## Chunk 4: InGame_New 전용 핸들러 구현

### Task 13: TutorialNewCombatStartHandler 구현

**Files:**
- Create: `Assets/_Project/Scripts/InGame_New/View/Tutorial/TutorialNewCombatStartHandler.cs`

- [ ] **Step 1: TutorialNewCombatStartHandler.cs 작성**

```csharp
using System;
using CookApps.AutoBattler;

namespace CookApps.AutoBattler.InGameNew
{
    public class TutorialNewCombatStartHandler : IDisposable
    {
        private readonly LocalSimulationRunner _runner;
        private bool _isPaused;

        public TutorialNewCombatStartHandler(LocalSimulationRunner runner)
        {
            _runner = runner;
        }

        public bool TryHandleTutorial()
        {
            if (!TutorialManager.Instance.IsTutorialAction(TutorialTriggerType.COMBAT_START))
                return false;

            _isPaused = true;
            _runner.PauseTick();

            TutorialManager.Instance.OnTutorialClosed += ResumeAfterTutorial;
            TutorialManager.Instance.HandleTutorialAction(TutorialTriggerType.COMBAT_START, "0");

            return true;
        }

        private void ResumeAfterTutorial()
        {
            if (!_isPaused) return;
            _isPaused = false;
            _runner.ResumeTick();
        }

        public void Dispose()
        {
            if (_isPaused)
                TutorialManager.Instance.OnTutorialClosed -= ResumeAfterTutorial;
            _isPaused = false;
        }
    }
}
```

- [ ] **Step 2: 커밋은 Task 16 이후 일괄**

---

### Task 14: TutorialNewSkillReadyHandler 구현

**Files:**
- Create: `Assets/_Project/Scripts/InGame_New/View/Tutorial/TutorialNewSkillReadyHandler.cs`

- [ ] **Step 1: TutorialNewSkillReadyHandler.cs 작성**

```csharp
using System;
using CookApps.AutoBattler;

namespace CookApps.AutoBattler.InGameNew
{
    /// <summary>
    /// SKILL_READY 튜토리얼 핸들러.
    /// CHARACTER_DEAD가 대기 중이면 SKILL_READY를 보류하여 seq 순서를 보장한다.
    /// </summary>
    public class TutorialNewSkillReadyHandler : IDisposable
    {
        private readonly LocalSimulationRunner _runner;
        private bool _isPaused;
        private int _deferredEntityId = -1;

        public TutorialNewSkillReadyHandler(LocalSimulationRunner runner)
        {
            _runner = runner;
        }

        public bool TryHandleTutorial(int entityId)
        {
            // CHARACTER_DEAD 대기 중이면 SKILL_READY 보류 (seq 순서 보장)
            if (TutorialManager.Instance.IsTutorialAction(TutorialTriggerType.CHARACTER_DEAD))
            {
                _deferredEntityId = entityId;
                return true;
            }

            if (!TutorialManager.Instance.IsTutorialAction(TutorialTriggerType.SKILL_READY))
                return false;

            _isPaused = true;
            _runner.PauseTick();

            TutorialManager.Instance.OnTutorialClosed += ResumeAfterTutorial;
            TutorialManager.Instance.HandleTutorialAction(
                TutorialTriggerType.SKILL_READY, entityId.ToString());

            return true;
        }

        /// <summary>
        /// CHARACTER_DEAD 튜토리얼 완료 후 보류된 SKILL_READY 처리.
        /// TutorialSimBridge에서 CHARACTER_DEAD 완료 시 호출.
        /// </summary>
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
            if (_isPaused)
                TutorialManager.Instance.OnTutorialClosed -= ResumeAfterTutorial;
            _isPaused = false;
            _deferredEntityId = -1;
        }
    }
}
```

---

### Task 15: TutorialNewCombatEndHandler 구현

**Files:**
- Create: `Assets/_Project/Scripts/InGame_New/View/Tutorial/TutorialNewCombatEndHandler.cs`

- [ ] **Step 1: TutorialNewCombatEndHandler.cs 작성**

```csharp
using System;
using CookApps.AutoBattler;

namespace CookApps.AutoBattler.InGameNew
{
    public class TutorialNewCombatEndHandler : IDisposable
    {
        private readonly LocalSimulationRunner _runner;
        private bool _isPaused;

        public TutorialNewCombatEndHandler(LocalSimulationRunner runner)
        {
            _runner = runner;
        }

        public bool TryHandleTutorial()
        {
            if (!TutorialManager.Instance.IsTutorialAction(TutorialTriggerType.COMBAT_END))
                return false;

            _isPaused = true;
            _runner.PauseTick();

            TutorialManager.Instance.OnTutorialClosed += ResumeAfterTutorial;
            TutorialManager.Instance.HandleTutorialAction(TutorialTriggerType.COMBAT_END, "0");

            return true;
        }

        private void ResumeAfterTutorial()
        {
            if (!_isPaused) return;
            _isPaused = false;
            _runner.ResumeTick();
        }

        public void Dispose()
        {
            if (_isPaused)
                TutorialManager.Instance.OnTutorialClosed -= ResumeAfterTutorial;
            _isPaused = false;
        }
    }
}
```

---

### Task 16: TutorialNewPhaseHandler 구현

**Files:**
- Create: `Assets/_Project/Scripts/InGame_New/View/Tutorial/TutorialNewPhaseHandler.cs`

- [ ] **Step 1: TutorialNewPhaseHandler.cs 작성**

```csharp
using System;
using CookApps.AutoBattler;

namespace CookApps.AutoBattler.InGameNew
{
    /// <summary>
    /// Preparation/Shop/Synergy 등 오토체스 페이즈 관련 튜토리얼 핸들러.
    /// 다양한 TutorialTriggerType을 동적으로 처리한다.
    /// </summary>
    public class TutorialNewPhaseHandler : IDisposable
    {
        private readonly LocalSimulationRunner _runner;
        private bool _isPaused;

        public TutorialNewPhaseHandler(LocalSimulationRunner runner)
        {
            _runner = runner;
        }

        public bool TryHandleTutorial(TutorialTriggerType triggerType)
        {
            if (!TutorialManager.Instance.IsTutorialAction(triggerType))
                return false;

            _isPaused = true;
            _runner.PauseTick();

            TutorialManager.Instance.OnTutorialClosed += ResumeAfterTutorial;
            TutorialManager.Instance.HandleTutorialAction(triggerType, "0");

            return true;
        }

        private void ResumeAfterTutorial()
        {
            if (!_isPaused) return;
            _isPaused = false;
            _runner.ResumeTick();
        }

        public void Dispose()
        {
            if (_isPaused)
                TutorialManager.Instance.OnTutorialClosed -= ResumeAfterTutorial;
            _isPaused = false;
        }
    }
}
```

- [ ] **Step 2: 전체 컴파일 확인**

Task 11-16 모두 작성 후 Unity Editor에서 컴파일 확인.

- [ ] **Step 3: 커밋**

```bash
git add Assets/_Project/Scripts/InGame_New/View/Tutorial/ Assets/_Project/Scripts/InGame_New/View/AutoChessViewBridge.cs
git commit -m "feat: TutorialSimBridge + InGame_New 전용 핸들러 구현"
```

---

## Chunk 5: AutoChessViewRoot 통합 + Strategy 분기

### Task 17: AutoChessViewRoot에서 TutorialSimBridge 생성/초기화

**Files:**
- Modify: `Assets/_Project/Scripts/InGame_New/View/AutoChessViewRoot.cs:65-104`

- [ ] **Step 1: AutoChessViewRoot의 Initialize 메서드에서 TutorialSimBridge 생성**

`AutoChessViewRoot.cs`의 Initialize 메서드 (L65-104)에서 `AutoChessViewBridge` 생성 후:

```csharp
// TutorialSimBridge 생성 및 ViewBridge에 연결
var tutorialBridge = new TutorialSimBridge(_localRunner);
_viewBridge.SetTutorialBridge(tutorialBridge);
```

- [ ] **Step 2: Dispose/정리 시 TutorialSimBridge.Dispose 호출**

AutoChessViewRoot의 정리 메서드에서:

```csharp
TutorialSimBridge.Instance?.Dispose();
```

- [ ] **Step 3: 컴파일 확인**

- [ ] **Step 4: 커밋**

```bash
git add Assets/_Project/Scripts/InGame_New/View/AutoChessViewRoot.cs
git commit -m "feat: AutoChessViewRoot에서 TutorialSimBridge 초기화"
```

---

### Task 18: TutorialActionSpawnEnemyNew Strategy 구현

**Files:**
- Create: `Assets/_Project/Scripts/InGame_New/View/Tutorial/TutorialActionSpawnEnemyNew.cs`

- [ ] **Step 1: TutorialActionSpawnEnemyNew.cs 작성**

레거시 `TutorialActionSpawnEnemy`를 참고하되, 직접 생성 대신 Command 전송:

```csharp
using System;
using Cysharp.Threading.Tasks;
using CookApps.AutoBattler;

namespace CookApps.AutoBattler.InGameNew
{
    /// <summary>
    /// InGame_New용 SPAWN_ENEMY 전략.
    /// 레거시와 달리 직접 생성하지 않고 SpawnTutorialEnemy 커맨드를 전송한다.
    /// </summary>
    public class TutorialActionSpawnEnemyNew : ITutorialActionStrategy
    {
        public async UniTask OnShow(TutorialActionContext context)
        {
            var tutorial = context.CurrentTutorial;
            var bridge = TutorialSimBridge.Instance;
            if (bridge == null) return;

            // tutorial_action_key 파싱: "monsterSpecId,col,row;monsterSpecId,col,row;..."
            var spawnEntries = tutorial.tutorial_action_key.Split(';');
            foreach (var entry in spawnEntries)
            {
                if (string.IsNullOrEmpty(entry)) continue;

                var parts = entry.Split(',');
                if (parts.Length < 3) continue;

                int monsterSpecId = int.Parse(parts[0]);
                int col = int.Parse(parts[1]);
                int row = int.Parse(parts[2]);

                bridge.EnqueueSpawnCommand(monsterSpecId, col, row);

                // 스폰 간격 연출 (0.4~0.6초)
                await UniTask.Delay(UnityEngine.Random.Range(400, 600));
            }
        }

        public void OnUpdate(TutorialActionContext context)
        {
            // 스폰 후 자동 진행
        }

        public bool CanProceedOnDimmedClick() => false;

        public void OnNext(TutorialActionContext context) { }

        public void OnClear(TutorialActionContext context) { }

        public void RestorePreviousState(TutorialActionContext context) { }
    }
}
```

> **Note**: `ITutorialActionStrategy`의 실제 인터페이스 시그니처에 맞춰 메서드 조정 필요. 레거시 `TutorialActionSpawnEnemy.cs`의 인터페이스 구현 패턴을 정확히 따를 것.

- [ ] **Step 2: 커밋**

```bash
git add Assets/_Project/Scripts/InGame_New/View/Tutorial/TutorialActionSpawnEnemyNew.cs
git commit -m "feat: TutorialActionSpawnEnemyNew — Command 기반 SPAWN_ENEMY 전략"
```

---

### Task 19: TutorialController에서 InGame_New 모드 Strategy 분기

**Files:**
- Modify: `Assets/_Project/Scripts/Common/TutorialController.cs:73-87`

- [ ] **Step 1: TutorialController에 Strategy 오버라이드 지원 추가**

`TutorialController.cs`의 Strategy 딕셔너리 (L73-87) 관련 코드에서, InGame_New 모드일 때 SPAWN_ENEMY를 `TutorialActionSpawnEnemyNew`로 매핑하는 분기 추가:

```csharp
// 기존 _strategies 딕셔너리에 InGame_New 오버라이드 적용
private static readonly Dictionary<TutorialActionType, ITutorialActionStrategy> _strategyOverrides
    = new Dictionary<TutorialActionType, ITutorialActionStrategy>();

public static void SetStrategyOverride(TutorialActionType actionType, ITutorialActionStrategy strategy)
{
    _strategyOverrides[actionType] = strategy;
}

public static void ClearStrategyOverrides()
{
    _strategyOverrides.Clear();
}
```

그리고 Strategy 조회 메서드에서:

```csharp
private ITutorialActionStrategy GetStrategy(TutorialActionType actionType)
{
    if (_strategyOverrides.TryGetValue(actionType, out var overrideStrategy))
        return overrideStrategy;
    if (_strategies.TryGetValue(actionType, out var strategy))
        return strategy;
    return _strategies[TutorialActionType.NONE];
}
```

- [ ] **Step 2: TutorialSimBridge 생성자에서 오버라이드 등록**

`TutorialSimBridge.cs` 생성자에 추가:

```csharp
TutorialController.SetStrategyOverride(
    TutorialActionType.SPAWN_ENEMY, new TutorialActionSpawnEnemyNew());
```

`Dispose`에서:

```csharp
TutorialController.ClearStrategyOverrides();
```

- [ ] **Step 3: 컴파일 확인**

- [ ] **Step 4: 전체 수동 테스트**

1. 레거시 InGame 튜토리얼이 기존과 동일하게 동작하는지 확인
2. InGame_New에서 튜토리얼 트리거가 감지되는지 확인 (디버그 로그)
3. Pause/Resume이 정상 동작하는지 확인

- [ ] **Step 5: 커밋**

```bash
git add Assets/_Project/Scripts/Common/TutorialController.cs Assets/_Project/Scripts/InGame_New/View/Tutorial/TutorialSimBridge.cs
git commit -m "feat: TutorialController Strategy 오버라이드 + InGame_New SPAWN_ENEMY 분기"
```

---

## Chunk 6: 최종 통합 커밋

### Task 20: 최종 점검 및 통합

- [ ] **Step 1: 전체 컴파일 확인**

Unity Editor에서 에러/경고 없는지 확인.

- [ ] **Step 2: 레거시 InGame 튜토리얼 회귀 테스트**

- 스테이지 10001~10003 진입 → COMBAT_START 튜토리얼 동작 확인
- SKILL_READY 튜토리얼 동작 확인
- ENEMY_DEAD_ALL 튜토리얼 동작 확인

- [ ] **Step 3: InGame_New 튜토리얼 기본 동작 테스트**

- InGame_New 전투 진입 → Preparation/Combat 페이즈 전환 시 디버그 로그 확인
- ManaFull 이벤트 발행 확인 (CombatAISystem 로그)
- TutorialSimBridge.Dispose 정상 호출 확인

- [ ] **Step 4: 최종 커밋**

```bash
git add -A
git commit -m "feat: InGame_New 튜토리얼 마이그레이션 — TutorialSimBridge 기반 통합 완료"
```
