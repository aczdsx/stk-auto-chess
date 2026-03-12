# Idle Combat Mode (InGame_New) Design Spec

## 개요

BattleReady 씬에서 보여주던 idle 전투(배경 프리뷰 전투)를 레거시 InGame 시스템(`FlowStateLobbyCombat`)에서 InGame_New 시스템으로 이전한다.

### 목적
- 플레이어가 BattleReady 씬에서 스테이지 정보를 보는 동안, 보유 캐릭터들이 몬스터와 싸우는 시각적 프리뷰를 보여줌
- 실제 전투가 아닌 연출 용도 — 시너지 없음, 유닛이 죽지 않음

### 기존 동작 (FlowStateLobbyCombat)
- ServerDataManager에서 플레이어 캐릭터 최대 5명 가져와 배치
- 현재 챕터 몬스터 리스트에서 적을 1~4초 간격으로 랜덤 스폰
- 최대 적 수: `max_idle_battle_monster_count` 게임 설정값
- 캐릭터 사망 없음 (HP 0이 되어도 영구 사망 처리 안 함)
- 카메라 트윈 연출 있었으나 새 구현에서는 불필요

## 아키텍처: 경량 전투 러너

InGame_New의 전체 파이프라인(GameWorld, GameLoopSystem, Phase 관리, Shop/Economy)을 사용하지 않고, `CombatMatchState` + `CombatAISystem.Tick()`만 직접 구동하는 경량 러너를 만든다.

### 선택 근거
- idle 전투는 전투 비주얼만 필요 → 전체 게임 루프는 과도
- 기존 시스템에 idle 전용 분기를 추가하지 않음
- GameModeType 확장 불필요

### 참조 패턴
- `CombatSetupSystem.SpawnTutorialUnit()` — GameWorld 없이 동적으로 CombatUnit을 스폰하는 선례. AliveCount 갱신, MaxCombatUnits 방어 패턴을 동일하게 따름

## 새 파일 (3개)

### 1. `Scripts/InGame_New/Adapter/Local/IdleCombatRunner.cs`

경량 전투 러너 MonoBehaviour.

**책임:**
- `CombatMatchState` 생성 및 관리
- `DeterministicRNG` 관리
- 매 프레임 시간 누적 → `CombatAISystem.Tick()` 호출
- 틱 전 HP 보정 (사망 방지) + `IsFinished` 리셋
- 적 주기적 스폰 타이머 관리
- `OnTick` 이벤트 발행 → View 갱신

**주요 필드:**
```csharp
CombatMatchState _matchState;
DeterministicRNG _rng;
SimEventQueue _eventQueue;
int _tickRate = 30;
float _accumulatedTime;

// 적 스폰
float _enemySpawnTimer;
float _enemySpawnInterval; // 1~4초 랜덤
int _maxEnemyCount;
List<ChampionSpec> _enemyPool; // 챕터 몬스터 리스트

// 이벤트
event Action<CombatMatchState> OnTick;
```

**공개 API:**
```csharp
void StartIdleCombat(List<CharacterData> playerChars, List<ChampionSpec> chapterMonsters);
void StopIdleCombat();
```

**Update 루프 (매 프레임):**
1. 적 스폰 타이머 체크 → `IdleCombatSetup.TryAddEnemy()` (빈 타일에 새 적 추가, `AliveCountB` 갱신)
2. HP 보정: 모든 유닛 중 `CurrentHP ≤ MaxHP * 0.1` → `CurrentHP = MaxHP`, `State = Idle`
3. `state.IsFinished = false` 리셋 (종료 조건 무력화)
4. `CombatAISystem.Tick(matchState, ref rng, tickRate)` 호출
5. `OnTick` 이벤트 발행
6. (View 브릿지가 이벤트 처리 후) `EventQueue.Clear()`

### 2. `Scripts/InGame_New/View/IdleCombatViewBridge.cs`

SimEventQueue → View 디스패치 경량 브릿지.

**책임:**
- `IdleCombatRunner.OnTick` 구독
- `SimEventQueue`의 이벤트를 읽어서 `CombatViewManager`에 디스패치
- `UnitViewManager.SyncCombatUnits(matchState, boardIndex: 0)` 호출로 위치/상태 동기화
- `UnitDied` 이벤트 무시 (사망 방지와 이중 안전장치)

**초기화/정리:**
- `Start()`: `CombatViewManager.OnCombatStart()` 호출 → `_isCombatActive = true` 활성화
- `OnDestroy()`: `CombatViewManager.OnCombatEnd()` + `UnitViewManager.OnCombatEnd()` 호출 → VFX/투사체 정리

**기존 `AutoChessViewBridge` 재사용하지 않는 이유:**
- `AutoChessViewBridge`는 `GameWorld`에 의존 (Phase 전환, 보드 동기화, Shop UI 등)
- idle에 불필요한 처리가 많아 재사용 시 더미 객체나 분기 필요

### 3. `Scripts/InGame_New/Adapter/IdleCombatSetup.cs`

플레이어/적 유닛 데이터를 CombatUnit으로 변환하는 유틸리티.

**책임:**
- 캐릭터 데이터 → `CombatUnit` 변환 (스탯, AtkHitDelay, SkillSpecId 포함)
- 그리드 배치 (플레이어: row 0-3, 적: row 4-7)
- `CombatMatchState` 초기 생성
- `SkillSystem.SetupSkillsForUnit()` 호출하여 스킬 등록 (스킬 연출이 동작해야 함)
- 시너지(`SynergySystem.ApplyEffects`)는 호출하지 않음
- 적 추가 스폰 시 `CombatUnit` 생성 및 matchState에 삽입

**공개 API:**
```csharp
static CombatMatchState CreateMatchState(
    List<CharacterData> playerChars,
    SimEventQueue eventQueue);

static bool TryAddEnemy(
    CombatMatchState matchState,
    ChampionSpec enemySpec,
    ref DeterministicRNG rng);
```

**`TryAddEnemy` 필수 처리:**
- `state.UnitCount >= MaxCombatUnits` 방어
- 스폰 후 `state.AliveCountB` 갱신
- 빈 그리드 타일 확인 후 배치

**유닛 초기화 시 필요 데이터 (SpecDataManager 조회):**
- 기본 스탯 (HP, ATK, DEF 등)
- `AtkHitDelay` — `AnimKeyframeData.ExecuteTimes` 딕셔너리에서 추출
- `SkillSpecId` — 캐릭터 스펙에서 조회
- 아이템 스탯은 적용하지 않음 (idle 프리뷰 용도)

## 기존 파일 수정

**`BattleReadyMain.cs`:** 기존 `InGameManager.StartInGame<FlowStateLobbyCombat>()` 호출을 `IdleCombatRunner.StartIdleCombat()` 호출로 교체. 씬 떠날 때 `StopIdleCombat()` 호출.

## 데이터 흐름

```
BattleReadyMain
  └→ IdleCombatRunner.StartIdleCombat(playerChars, chapterMonsters)
       ├→ IdleCombatSetup.CreateMatchState(players, eventQueue)
       │    └→ CombatMatchState 생성 + 플레이어 유닛 배치 + 스킬 셋업
       └→ IdleCombatViewBridge 초기화
            └→ CombatViewManager.OnCombatStart()

매 프레임 (Update):
  IdleCombatRunner
    ├→ 적 스폰 타이머 → IdleCombatSetup.TryAddEnemy() + AliveCountB 갱신
    ├→ HP 보정 (사망 방지) + IsFinished 리셋
    ├→ CombatAISystem.Tick(matchState, ref rng, tickRate)
    ├→ OnTick 이벤트 발행
    └→ IdleCombatViewBridge
         ├→ SimEventQueue → CombatViewManager 디스패치 (UnitDied 무시)
         ├→ UnitViewManager.SyncCombatUnits(matchState, boardIndex: 0)
         └→ EventQueue.Clear()

BattleReady 씬 떠날 때:
  └→ IdleCombatRunner.StopIdleCombat()
       └→ IdleCombatViewBridge 정리
            ├→ CombatViewManager.OnCombatEnd()
            └→ UnitViewManager.OnCombatEnd()
```

## 시뮬레이션 규칙

| 항목 | 값 |
|------|-----|
| 플레이어 유닛 수 | 최대 5명 (보유 캐릭터, 스펙 순서). 0명이면 idle 전투 시작하지 않음 |
| 적 스폰 간격 | 1~4초 랜덤 |
| 적 최대 수 | `max_idle_battle_monster_count` 설정값 |
| 유닛 총합 상한 | `MaxCombatUnits (32)` 초과 시 스폰 중단 |
| 시너지 | 미적용 (`SynergySystem.ApplyEffects` 미호출) |
| 스킬 | 적용 (`SkillSystem.SetupSkillsForUnit` 호출) — 스킬 연출이 보여야 함 |
| 사망 처리 | 비활성화 (틱 전 HP 보정 + IsFinished 리셋) |
| HP 보정 임계값 | `MaxHP * 0.1` 이하 시 `MaxHP`로 복원 |
| 틱 레이트 | 30 FPS |
| 카메라 | 별도 처리 없음 |
| 아이템 스탯 | 미적용 (idle 프리뷰 용도) |

## View 처리

- InGame_New View 시스템(UnitView, CombatViewManager) 그대로 재사용
- `IdleCombatViewBridge`가 전투 이벤트만 처리 (Phase 전환, 보드 UI 등 무시)
- `UnitDied` 이벤트는 브릿지에서 무시 (이중 안전장치)
- 초기화 시 `CombatViewManager.OnCombatStart()` 필수 호출
- 정리 시 `CombatViewManager.OnCombatEnd()` + `UnitViewManager.OnCombatEnd()` 필수 호출

## 엣지 케이스

- **플레이어 유닛 0명**: idle 전투를 시작하지 않음 (방어 처리)
- **MaxCombatUnits 초과**: `TryAddEnemy()`에서 `UnitCount >= 32` 체크, 초과 시 스폰 중단
- **적 0명 상태**: `IsFinished` 매 틱 리셋으로 종료 방지. 적이 스폰될 때까지 플레이어 유닛은 Idle 상태 유지

## 범위 외 (Out of Scope)

- 카메라 연출/트윈
- 시너지 시스템 연동
- 전투 결과 처리 (승패 판정)
- 사운드 관리 (별도 처리)
- 아이템 스탯 적용
