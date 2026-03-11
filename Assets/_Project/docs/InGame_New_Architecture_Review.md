# InGame_New 아키텍처 점검 보고서

> 작성일: 2026-03-04
> 대상: `Assets/_Project/Scripts/InGame_New/`

---

## 1. 전체 구조

```
InGame_New/
├── Simulation/    (36파일) ← 순수 게임 로직, namespace: CookApps.AutoChess
│   ├── Core/      GameLoopSystem, CommandProcessor
│   ├── Data/      GameWorld, Components, Commands, SimulationEvents, Enums, GameConfig
│   ├── Math/      DeterministicRNG (xorshift64)
│   ├── Board/     BoardSystem, BoardHelper, CombineSystem
│   ├── Combat/    CombatAI, Damage, Targeting, Movement, Projectile, Skill, StatusEffect
│   │   └── Skills/  SimSkillBase + 7개 구현체 + SkillFactory + SkillHelpers
│   ├── Economy/   EconomySystem
│   ├── Shop/      ShopSystem
│   ├── Synergy/   SynergySystem
│   ├── Item/      ItemSystem
│   └── Player/    PlayerDamageSystem
├── Adapter/       (3파일) ← Simulation ↔ Unity 경계
│   ├── ISimulationRunner.cs          (인터페이스)
│   ├── AutoChessSpecAdapter.cs       (SpecDataManager → 시뮬레이션 struct 변환)
│   └── Local/LocalSimulationRunner.cs (MonoBehaviour, Update 기반 틱 구동)
└── View/          (16파일) ← 프레젠테이션, namespace: CookApps.AutoChess.View
    ├── AutoChessViewBridge.cs    (중앙 브릿지)
    ├── AutoChessViewRoot.cs      (리소스 로드)
    ├── Board/  Unit/  Combat/  UI/
```

- **총 55개 .cs 파일**: Simulation 36, View 16, Adapter 3

---

## 2. 핵심 설계 패턴

### 2.1 데이터 흐름

```
┌─── Simulation ──────────────────────────┐
│  GameLoopSystem.Tick(world, commands[])  │
│      ↓                                   │
│  CommandProcessor → BoardSystem          │
│  CombatAISystem → DamageSystem           │
│      ↓                                   │
│  GameWorld (상태 갱신)                    │
│  SimEventQueue (이벤트 push)             │
└──────────┬──────────────────────────────┘
           │ OnTick(GameWorld) + EventQueue
           ▼
┌─── AutoChessViewBridge ─────────────────┐
│  ProcessEvents() → CombatViewManager    │
│  SyncBoardUnits() → UnitViewManager     │
│  SyncState() → AutoChessUIBase          │
└──────────────────────────────────────────┘
           ↑ GameCommand
     View 입력 (BoardInputHandler, UI)
```

### 2.2 3가지 통신 채널

| 방향 | 채널 | 메커니즘 |
|------|------|---------|
| Sim → View | 상태 스냅샷 | `GameWorld` 직접 읽기 (매 틱) |
| Sim → View | 일회성 이벤트 | `SimEventQueue` (flat struct, GC 회피) |
| View → Sim | 입력 | `GameCommand` struct (팩토리 메서드) |

### 2.3 GameWorld — 단일 진실 소스

- 모든 게임 상태를 한 곳에 보유
- 고정 크기 배열: `UnitData[128]`, `CombatUnit[32]`, `Projectile[32]`, `StatusEffect[128]`
- struct 기반 데이터, `ref` 반환으로 수정
- ID 기반 참조 (EntityId, CombatId)
- 주석에 "직렬화하면 게임 상태 복원 가능" 명시

### 2.4 결정론적 시뮬레이션

- `DeterministicRNG` (xorshift64, 모듈러 바이어스 제거)
- 모든 랜덤 결정이 `world.RNG`를 통해 수행
- `GameCommand` 커맨드 패턴 → 동일 시드 + 동일 커맨드 시퀀스 = 동일 결과

---

## 3. 로직/뷰 분리 상태 — 평가: A

| 항목 | 상태 |
|------|------|
| Simulation → View 참조 | **0건** |
| Unity 의존성 (Simulation 내) | **0건** (수정 완료) |
| 네임스페이스 분리 | `CookApps.AutoChess` vs `CookApps.AutoChess.View` |
| View에 게임 로직 | **없음** (데미지 계산, 상태 전환 모두 Simulation에서 수행) |
| 단방향 의존성 | View → Simulation 방향만 참조 |

### 로직 단독 실행

```csharp
var config = GameConfig.Competitive();
var world = GameWorld.Create(config);
world.RNG = new DeterministicRNG(12345);
// world.SetChampionPool(...) 로 스펙 직접 주입
GameLoopSystem.Initialize(world, config);

for (int i = 0; i < 1000; i++)
    GameLoopSystem.Tick(world, commands, commandCount);
// → Unity 없이 실행 가능
```

---

## 4. 시스템 간 의존성

```
GameLoopSystem (오케스트레이터)
  ├── CommandProcessor → BoardSystem, ShopSystem, EconomySystem, ItemSystem, SynergySystem
  ├── EconomySystem (독립)
  ├── ShopSystem → BoardSystem, CombineSystem, EconomySystem, ChampionPool
  ├── CombatSetupSystem → BoardSystem, BoardHelper, ItemSystem, SynergySystem
  ├── CombatAISystem → TargetingSystem, MovementSystem, DamageSystem, ProjectileSystem, SkillSystem
  ├── DamageSystem → StatusEffectSystem, CombatSetupSystem(CountAlive), BoardHelper
  ├── SkillSystem → SkillFactory → SimSkillBase 서브클래스들
  ├── SynergySystem → StatusEffectSystem
  ├── PlayerDamageSystem → ShopSystem, EconomySystem
  └── CombineSystem → BoardSystem, BoardHelper, ItemSystem
```

모든 시스템이 `static class`로 구현 (상태 없는 함수형 스타일, `GameWorld`를 매개변수로 전달).

---

## 5. 스킬 시스템 분석

### 5.1 구조

```
SimSkillBase (abstract)
  ├── SimSkillSingleDamage   — 단일 타겟 배율 데미지
  ├── SimSkillAoEDamage      — 원형 범위 데미지 (radius)
  ├── SimSkillLineDamage     — 직선 관통 데미지 (length)
  ├── SimSkillHeal           — 최저HP 아군 회복
  ├── SimSkillBuff           — 자기 버프 (statType + value)
  ├── SimSkillDebuff         — 적 디버프 (statType + value)
  └── SimSkillStun           — 데미지 + CC (stunFrames)

SkillFactory     — Dictionary<int, Func<SimSkillBase>> 수동 등록 (현재 비어있음)
SkillHelpers     — static 유틸리티 (DamageHelper, AreaHelper, CCHelper, BuffHelper)
```

### 5.2 타겟팅 전략 (4가지)

1. `TargetingSystem.FindNearestEnemy` — SingleDamage, LineDamage, Debuff, Stun
2. `SkillAreaHelper.FindBestAoETarget` — AoEDamage (최대 적 포함 중심)
3. `SkillAreaHelper.FindLowestHPAlly` — Heal (최저 HP 아군)
4. `caster.CombatId` (자기 자신) — Buff

### 5.3 데이터 주도 평가

| 스킬 | 커스텀 상태 | Execute 복잡도 | 데이터 주도 |
|------|-----------|---------------|------------|
| SingleDamage | 없음 | 헬퍼 1줄 | 완전 |
| Heal | 없음 | 헬퍼 1줄 | 완전 |
| Buff | statType, value | 헬퍼 1줄 | 완전 |
| Debuff | statType, value | 헬퍼 1줄 | 완전 |
| AoEDamage | radius | 범위 순회 | 높음 |
| Stun | stunFrames | 데미지 + CC 조건분기 | 혼합 |
| LineDamage | length | 방향 계산 ~5줄 | 혼합 |

**결론**: 7개 중 5개가 헬퍼 함수 1줄 호출 수준. ECS 전환 시 `SkillType` enum + switch로 기계적 매핑 가능. `SkillHelpers`의 static 함수들은 그대로 재사용 가능.

---

## 6. Photon Quantum 결합 용이성 — 평가: B+

### 6.1 Quantum에 유리한 설계 (이미 준비됨)

| 항목 | Quantum 대응 | 상태 |
|------|-------------|------|
| 결정론적 RNG (xorshift64) | Quantum 필수 요건 | 준비됨 |
| Command 패턴 (`GameCommand` struct) | Quantum Input/Command 동일 패턴 | 준비됨 |
| 고정 틱레이트 (30fps) | Quantum 틱 기반과 동일 | 준비됨 |
| 전체 상태 컨테이너 (`GameWorld`) | Quantum Frame/State 매칭 | 준비됨 |
| Static System 패턴 | Quantum System과 동일 | 준비됨 |
| 이벤트 큐 (`SimEventQueue`) | Quantum Events 유사 | 준비됨 |
| `ISimulationRunner` 어댑터 | Quantum 어댑터 교체 가능 | 준비됨 |

### 6.2 마이그레이션 시 수정 필요 사항

#### 필수 수정 (Quantum SDK 도입 시점에 수행)

| # | 항목 | 설명 | 예상 작업량 |
|---|------|------|-----------|
| 1 | **고정소수점(FP) 변환** | int→FP. 현재 정수 기반(100=1.0x)이라 변환 수월 | 2-3일 |
| 2 | **class→struct 변환** | GameWorld, ChampionPool, CombatMatchState, PlayerSynergy. Quantum DSL로 재작성 | 3-5일 |
| 3 | **참조 타입 배열 제거** | SynergySpec.Tiers[], SynergyTier.Effects[] → FixedArray | 1-2일 |
| 4 | **SkillSystem OOP→ECS** | SimSkillBase 상속 → SkillType enum + switch. SkillHelpers는 재사용 | 1-2일 |

> **주의**: 위 항목들은 Quantum SDK 없이 선행 작업하면 이중 작업이 됨.
> Quantum은 자체 DSL(`qtn` 파일)로 데이터 구조를 정의하고 코드 생성하므로,
> C#에서 미리 struct로 바꿔도 다시 작성해야 함.

#### 경미한 수정

| # | 항목 | 설명 |
|---|------|------|
| 5 | 이벤트 시스템 | SimEventQueue → Quantum EventBase 매핑 (기계적) |
| 6 | Adapter 교체 | LocalSimulationRunner → QuantumCallbacks 기반 |
| 7 | Spec 주입 | AutoChessSpecAdapter → Quantum AssetDB/RuntimeConfig |

### 6.3 예상 마이그레이션 일정

```
Phase 1: Quantum SDK 도입 + DSL 작성 (GameWorld, Components)    3-5일
Phase 2: System 포팅 (static 함수 → Quantum System)             3-5일
Phase 3: FP 수학 변환                                           2-3일
Phase 4: Skill ECS 전환 + SkillHelpers 재사용                   1-2일
Phase 5: Quantum Adapter + View 연결                            2-3일
                                                        Total: 약 2-3주
```

---

## 7. 수행된 개선 조치 (2026-03-04)

| # | 조치 | 커밋 |
|---|------|------|
| 1 | `GameLoopSystem`의 `UnityEngine.Debug.Log` 2건 제거 → `CombatLogger.Flush()` 콜백 방식 | dc3c935c7 |
| 2 | `BoardHelper.Setup()`을 `GameLoopSystem.Initialize()`에서 Config 기준 동기화 | dc3c935c7 |
| 3 | `SkillSystem._skillCache` static → `CombatMatchState.Skills[]` 매치별 인스턴스로 이동 | dc3c935c7 |
| 4 | `ISimulationRunner` 인터페이스 추출. View가 구체 구현에 의존하지 않도록 변경 | dc3c935c7 |

---

## 8. 남은 개선 사항 (급하지 않음)

| # | 항목 | 우선순위 | 설명 |
|---|------|---------|------|
| 1 | `CombatLogger` static 상태 | 낮음 | 디버그용. 다중 매치 로깅 혼재 가능하나 실무 영향 없음 |
| 2 | `BoardHelper.Width/Height` 매개변수화 | 낮음 | 현재 Setup()으로 동기화됨. 완전 제거 시 20개 참조 수정 필요 |
| 3 | `SkillFactory.Initialize()` 구현 | 중간 | 스킬 ID 등록 미구현. SkillActive 테이블 연동 시 작업 |
| 4 | `GameWorld` 직렬화/스냅샷 | 중간 | 리플레이/서버 검증에 필요. 마이그레이션 전까지는 불필요 |
| 5 | `DeterministicRNG.State` setter | 낮음 | 스냅샷 복원 시 필요 |
| 6 | `AutoChessViewBridge`의 `SpecDataManager` 직접 참조 | 낮음 | Adapter 분리하면 더 깔끔하지만 동작에 문제 없음 |

---

## 9. 종합 평가

| 항목 | 등급 |
|------|------|
| 로직/뷰 분리 | **A** |
| 단독 실행 가능 | **A** |
| 결정론적 시뮬레이션 | **A** |
| GC 최적화 | **A** |
| Quantum 결합 용이성 | **B+** |
| 테스트 용이성 | **B+** |