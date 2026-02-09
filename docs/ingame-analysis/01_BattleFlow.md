# 전투 흐름 분석

> 관련 파일:
> - `InGame/Managers/InGameManager.cs`
> - `InGame/Managers/InGameMainFlowManager.cs`
> - `InGame/StateMachine/StateBase.cs`
> - `InGame/GameFlowStates/Stage/*.cs`

---

## 1. 전체 전투 시퀀스

```
[씬 진입 / 서버 응답]
        │
        ▼
InGameManager.StartInGame<T>(stageData)
  ├─ IsInGamePlaying = true
  ├─ IsInGameCombat = true
  ├─ EffectCodeContainerTeam 생성
  ├─ InGameMainFlowManager.StartInGameMainLoop<T>(stageData)
  └─ InitializeInGameComponents() ── 순서 중요!
     ├─ InGameVfxManager.Initialize()
     ├─ InGameHpBarViewPool.Initialize()
     ├─ InGameTextViewPool.InitializePool()
     ├─ InGameBuffDebuffPool.Initialize()
     ├─ InGameObjectManager.Initialize()
     ├─ InGameCommanderManager.Initialize()
     ├─ InGameSynergyManager.Initialize()
     └─ InGameSynergyUI.ClearPreviousSynergyStates()
        │
        ▼
┌──────────────────────────────────────────┐
│     FlowState: Ready (준비 단계)          │
│  - 적/장애물/중립 오브젝트 스폰             │
│  - 플레이어 덱 로딩 및 캐릭터 배치          │
│  - 타일 규칙 이펙트 적용                   │
│  - 카메라 설정, UI 초기화                  │
│  - 캐릭터 드래그 배치 가능                  │
│  - 타겟 예측 라인 표시                     │
└──────────────┬───────────────────────────┘
               │ 사용자가 전투 시작 버튼 누름
               ▼
┌──────────────────────────────────────────┐
│     FlowState: Combat (전투 단계)         │
│  - 시너지 효과 적용                       │
│  - OnCombatStart 이펙트코드 실행           │
│  - 1초 대기 후 모든 캐릭터 Unlock (Idle)    │
│  - 매 프레임: 승패 조건 체크               │
│    ├─ 적군 전멸 → 승리                    │
│    ├─ 아군 전멸 → 패배                    │
│    └─ 시간 초과 → 패배                    │
└──────────────┬───────────────────────────┘
               │ EndCombat(isWin)
               │ 속도 0.4x → 1.2초 대기 → 원래 속도
               ▼
┌──────────────────────────────────────────┐
│  FlowState: Clear 또는 Fail (결과 단계)   │
│  - 별점 계산 (Clear 시)                   │
│    ├─ 기본 1★                            │
│    ├─ 남은 시간 ≥ 30초 → +1★             │
│    └─ 아군 전원 생존 → +1★               │
│  - 서버에 결과 전송                       │
│  - InGameManager.EndInGame()              │
│  - 결과 팝업 표시                         │
└──────────────────────────────────────────┘
        │
        ▼
InGameManager.EndInGame()
  ├─ InGameMainFlowManager.StopInGameMainLoop()
  ├─ InGameCommanderManager.Clear()
  ├─ InGameObjectManager.Clear()
  ├─ InGameTextViewPool.ReleasePool()
  ├─ InGameHpBarViewPool.Clear()
  ├─ InGameBuffDebuffPool.Clear()
  ├─ InGameVfxManager.Clear()
  ├─ InGameStatistics.Clear()
  ├─ InGameSynergyManager.Clear()
  └─ TeamEcc.Clear()
```

---

## 2. StateBase 생명주기

```csharp
public abstract class StateBase
{
    public virtual void SetStateData(object data) { }  // 모드별 데이터 설정
    public abstract void StateInit(object owner);       // 초기화 (비동기 가능)
    public abstract void StateStart();                  // 시작 (동기)
    public abstract void StateRunning(float dt);        // 매 프레임 실행
    public abstract void StateEnd(bool isForced);       // 종료 (정리)
}
```

### 파생 클래스

| 클래스 | 용도 |
|--------|------|
| `StateReadyBase` | 준비 단계 베이스 - 타겟 예측 라인 그리기 (`StartDrawingLinesAsync`) |
| `StateCombatBase` | 전투 단계 베이스 - 현재 빈 클래스 (확장 포인트) |

---

## 3. InGameMainFlowManager 메인 루프

### 업데이트 흐름

```
Unity Update()
    │
    ├─ isPaused? → return
    │
    ├─ deltaTime = unscaledTime - prevProcessingTime
    │   └─ deltaTime > 0.5f → 모듈로 보정 (백그라운드 복귀 대응)
    │
    ├─ 핸들러 정렬 (priority 내림차순, dirty 시)
    │
    └─ 각 핸들러 호출: handler.Invoke(deltaTime × fastForwardRate)
```

### 업데이트 우선순위

| 상수 | 값 | 용도 |
|------|-----|------|
| `UpdatePriority_TopTier` | `int.MaxValue` | FlowState 관리 (ManagedUpdate) |
| `UpdatePriority_Objects` | `0` | 캐릭터/오브젝트 업데이트 |
| `UpdatePriority_Tween` | `-10000` | 트윈 애니메이션 |
| `UpdatePriority_BottomTier` | `int.MinValue` | 최하위 |

### FlowState 전환 로직

```csharp
private void ManagedUpdate(float dt)
{
    // 1. flowState가 없으면 큐에서 꺼내서 시작
    if (flowState == null)
    {
        flowState = nextStates.Dequeue();
        flowState.StateInit(this);
        flowState.StateStart();
        return;
    }

    // 2. 현재 flowState 실행
    flowState.StateRunning(dt);

    // 3. 한 틱에 여러 상태가 들어오면 마지막만 사용
    while (nextStates.Count > 1)
        StatePool.Instance.Return(nextStates.Dequeue());

    // 4. 다음 상태가 있으면 전환
    if (nextStates.Count > 0)
    {
        flowState.StateEnd(false);
        StatePool.Instance.Return(flowState);
        flowState = nextStates.Dequeue();
        flowState.StateInit(this);
        flowState.StateStart();
    }
}
```

### 게임 속도 관리

| 메서드 | 동작 |
|--------|------|
| `SetPlaySpeed(float)` | `fastForwardRate` 설정 + `Time.timeScale` 동기화 |
| `SetInGameSpeed(bool isSpeedUp)` | Preference에서 저장된 속도 로드 (기본 1.0x / 1.3x) |
| `Pause()` | `isPaused = true`, `Time.timeScale = 0` |
| `Resume()` | `isPaused = false`, `Time.timeScale = fastForwardRate` |

---

## 4. 모드별 FlowState 분석

### 4.1 스테이지 모드

```
FlowStateStageReady → FlowStateStageCombat → FlowStateStageClear / FlowStateStageFail
```

**FlowStateStageReady** (`GameFlowStates/Stage/FlowStateStageReady.cs`)
- `SetStateData`: StageInfo 수신, BGM 재생, 비네트 설정
- `StateInit`:
  - 스테이지 몬스터 스폰 (시너지 보너스 스탯 포함)
  - 장애물/중립 벽 스폰
  - 플레이어 덱 로딩 (ServerDataManager.Instance.Deck)
  - 캐릭터 배치 (저장된 좌표 사용)
  - 타일 규칙 이펙트 스폰
  - 카메라 초기화
- `StateStart`: UI 초기화, 타겟 라인 그리기 시작, 덱 저장

**FlowStateStageCombat** (`GameFlowStates/Stage/FlowStateStageCombat.cs`)
- `StateInit`: 시너지 FX 정리, 카메라 설정, 시작 캐릭터 기록
- `StateStart`: 시너지 효과 적용, OnCombatStart 이펙트코드, 1초 후 Unlock
- `StateRunning`: 매 프레임 승패 체크
  - 적군 전멸 → `AppEventResult = "clear"`, `EndCombat(true)`
  - 아군 전멸 → `AppEventResult = "fail"`, `AppEventReason = "dead"`, `EndCombat(false)`
  - 시간 초과 → `AppEventResult = "fail"`, `AppEventReason = "time_out"`, `EndCombat(false)`

**EndCombat 연출**:
```
EndCombat(bool isWin)
  → 속도 0.4x로 슬로우
  → 1.2초 대기 (극적 효과)
  → 원래 속도 복원
  → Clear 또는 Fail 상태로 전환
```

**FlowStateStageClear** (`GameFlowStates/Stage/FlowStateStageClear.cs`)
- 별점 계산: 기본 1★ + 시간 보너스(≥30초) + 생존 보너스(전원 생존)
- 클리어 시간 계산: `(60 - InGameTime) × 1000` ms
- 서버에 결과 전송 (별점, 클리어 시간)
- MVP 캐릭터 결정 (InGameStatistics)
- InGameResultPopup 표시

**FlowStateStageFail** (`GameFlowStates/Stage/FlowStateStageFail.cs`)
- 패배 결과 처리
- 패배 UI 표시

---

### 4.2 시련 던전 모드

```
FlowStateTrialDungeonReady → FlowStateTrialDungeonCombat → FlowStateTrialDungeonClear / FlowStateTrialDungeonFail
```

- `StartInGame<T>(DungeonBabelInfo)` 로 시작
- `IsBlockAmbush` = `dungeon_map_id == 1` 이면 true (매복 차단)
- 스테이지 모드와 유사한 흐름, 던전 전용 데이터(DungeonBabelInfo) 사용

---

### 4.3 프롤로그 모드

```
FlowStatePrologueReady → FlowStatePrologueCombat → FlowStatePrologueClear
```

- 튜토리얼/프롤로그 전용 흐름
- 프롤로그 전용 캐릭터 상태 (`CharacterStates/Prologue/`) 사용
- Fail 없음 (패배 불가)

---

### 4.4 로비 모드

```
FlowStateLobbyCombat (단일 상태)
```

- 로비에서 자동 전투 시연
- 몬스터 지속 스폰
- 승패 판정 없음

---

### 4.5 테스트 모드

```
FlowStateInGameTestReady → FlowStateInGameTestCombat
```

- `StartInGame<T>(InGameTestConfig)` 로 시작
- 무적 플래그 지원: `IsPlayerInvincible`, `IsEnemyInvincible`
- 테스트 프리셋 기반 전투

---

## 5. 서버 통신 흐름

### 전투 시작

```
1. 서버에서 BattleResponse 수신
   └─ SessionId, RandomSeed 포함
2. InGameManager.SetSessionIdAndRandomSeed(sessionId, seed)
   └─ InGameRandomManager에 시드 전파
3. InGameManager.StartInGame<FlowStateStageReady>(stageInfo)
```

### 전투 종료

```
1. FlowStateStageClear/Fail.StateStart()
2. 서버에 결과 전송:
   ├─ BattleSessionId
   ├─ Stars (1~3)
   ├─ ClearTime (ms)
   └─ IsVictory
3. 서버 응답으로 보상 수신
4. InGameManager.EndInGame()
```

---

## 6. 시간 관리

- **InGameTime**: `InGameMain.GetInGameMain().InGameTime` - 남은 시간 (초)
- **기본 시간 제한**: 60초 (스테이지 기준)
- **속도 배율**: `fastForwardRate` - 0.4x ~ 1.3x 범위
- **백그라운드 복귀**: deltaTime > 0.5f일 때 모듈로 보정으로 시간 점프 방지
- **OneTick**: `1f / Application.targetFrameRate` - 프레임 기준 틱

---

## 7. 이벤트 시스템

| 이벤트 | 시점 | 구독자 |
|--------|------|--------|
| `OnFlowStateChanged` | FlowState 전환 시 | UI, 카메라 등 |
| `OnCombatStart` (EffectCode) | 전투 시작 시 | 모든 이펙트코드 |
| `OnFlowStateStageReadyStart` (EffectCode) | Ready 상태 시작 시 | 팀 이펙트코드 |
