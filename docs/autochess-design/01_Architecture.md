# 전체 아키텍처 설계

> 4인 경쟁 TFT 스타일 오토체스의 시스템 아키텍처를 정의한다.

---

## 1. 기술 스택

| 항목 | 기술 | 비고 |
|------|------|------|
| 엔진 | Unity 6 (6000.x) | URP |
| 플랫폼 | iOS / Android | 모바일 우선 |
| 네트워크 | **Photon Quantum 3** (권장) | 결정론적 ECS |
| 비동기 | UniTask | `Cysharp.Threading.Tasks` |
| 리액티브 | R3 | UI 바인딩 |
| 트윈 | LitMotion | 연출/애니메이션 |
| 직렬화 | MemoryPack | 로컬 캐싱 |
| 2D 애니메이션 | Spine | 캐릭터 |

---

## 2. Photon 제품 비교 및 권장안

### 비교표

| 항목 | Quantum 3 | Fusion 2 | Realtime/PUN2 |
|------|-----------|----------|---------------|
| **동기화 모델** | 입력만 동기화 (결정론적) | 상태 스냅샷 동기화 | RPC/이벤트 수동 동기화 |
| **시뮬레이션** | 서버 검증 + 클라이언트 예측/롤백 | 호스트 또는 서버 권위 | 권위 모델 직접 구현 |
| **대역폭** | 낮음 (입력만 전송) | 높음 (상태 스냅샷) | 가변 (구현에 따라) |
| **치트 방지** | 강함 (서버 검증) | 호스트 모드: 약함 / 서버 모드: 강함 | 약함 |
| **프로그래밍 모델** | ECS (순수 C#, Unity 분리) | MonoBehaviour 친화적 | MonoBehaviour 친화적 |
| **오토체스 적합도** | **최적** (공식 샘플 존재) | 적합 | 부적합 |
| **학습 비용** | 높음 (ECS 패러다임) | 중간 | 낮음 |
| **봇 지원** | 내장 Bot SDK | 직접 구현 | 직접 구현 |
| **리플레이** | 내장 (입력 로그만으로 재현) | 직접 구현 | 직접 구현 |

### 권장: Photon Quantum 3

**이유:**

1. **공식 Auto Chess 샘플 존재**
   - Photon이 Bitcake 스튜디오와 함께 개발한 오토체스 샘플이 존재
   - 8인 매치, 유닛 합성, 공유 풀 샵, 경제 시스템 등 핵심 메커니즘 구현 완료
   - Turn Based Framework 기반으로 오토체스의 페이즈 전환에 최적화

2. **결정론적 시뮬레이션**
   - 동일 입력 + 동일 시드 → 모든 클라이언트에서 동일 결과 보장
   - 전투 시뮬레이션의 결과가 100% 일치 (치팅 방지 + 일관성)
   - 현행 시스템의 `InGameRandomManager` (시드 기반 RNG) 개념과 동일하되, 프레임워크 레벨에서 보장

3. **낮은 대역폭**
   - 상태가 아닌 입력만 전송하므로 유닛이 많아도 대역폭 일정
   - 오토체스는 보드 위에 다수 유닛이 동시 전투하므로 이점이 큼
   - 모바일 환경에서 특히 유리

4. **서버 검증**
   - Photon 클라우드에서 게임 무결성 검증
   - 호스트 조작/핵 방지
   - 경쟁 게임에서 필수적인 공정성 보장

5. **내장 봇 SDK**
   - 매칭 인원 부족 시 자동 봇 투입
   - 4인 게임에서 이탈자 대체 가능

**주의 사항:**

- ECS 패러다임 학습 필요 (기존 MonoBehaviour 방식과 다름)
- 게임 로직(Simulation)과 비주얼(View)이 완전 분리됨
- 결정론적 수학 라이브러리 사용 (`FP` 고정소수점, `FPVector2` 등)
- Unity 물리 대신 Quantum 내장 물리 사용

### 대안: Photon Fusion 2 (Shared 모드)

ECS 도입이 부담되는 경우의 대안:

- Unity MonoBehaviour 기반으로 친숙한 개발 환경
- Shared Authority 모드로 각 플레이어가 자신의 보드만 권위 보유
- 전투 시뮬레이션은 결정론적 RNG로 로컬 실행, 결과만 검증
- 단점: 대역폭 높음, 치트 방지 약함, 직접 구현할 부분 많음

---

## 3. 아키텍처 레이어

```
┌─────────────────────────────────────────────────────┐
│                  Presentation Layer                   │
│  (Unity MonoBehaviour, UI, VFX, Spine Animation)     │
│  - BoardView, UnitView, ShopUI, SynergyPanel        │
│  - 입력 처리 → Quantum 커맨드로 변환                    │
│  - Quantum 상태 → 비주얼 동기화                        │
├─────────────────────────────────────────────────────┤
│                  Simulation Layer                     │
│  (Quantum ECS - 순수 C#, Unity 비의존)                │
│  - GameLoopSystem, CombatSystem, EconomySystem       │
│  - ShopSystem, SynergySystem, BoardSystem            │
│  - 결정론적 로직만 포함                                 │
├─────────────────────────────────────────────────────┤
│                  Network Layer                        │
│  (Photon Quantum 3 Runtime)                          │
│  - 입력 동기화, 예측/롤백, 서버 검증                     │
│  - 매칭, 룸 관리                                      │
├─────────────────────────────────────────────────────┤
│                  Data Layer                           │
│  - Spec 데이터 (챔피언, 아이템, 시너지 테이블)            │
│  - Quantum DSL로 정의된 게임 상태                       │
└─────────────────────────────────────────────────────┘
```

### Quantum의 Simulation ↔ View 분리

```
Quantum Simulation (순수 C#)          Unity View (MonoBehaviour)
┌──────────────────────┐          ┌──────────────────────┐
│ UnitData (ECS 컴포넌트) │  ──→  │ UnitView (시각 표현)    │
│ - HP, Attack, Mana     │  읽기  │ - Spine 애니메이션      │
│ - Position (FPVector2) │  전용  │ - HP바, 버프 아이콘     │
│ - State (Idle/Attack)  │        │ - LitMotion 트윈       │
│ - TargetRef            │        │ - 파티클/VFX           │
├──────────────────────┤          ├──────────────────────┤
│ PlayerData            │  ──→  │ PlayerView             │
│ - Gold, XP, Level      │        │ - 샵 UI               │
│ - HP, Streak           │        │ - 경제 UI             │
│ - Board[]              │        │ - 미니맵              │
├──────────────────────┤          ├──────────────────────┤
│ GameState             │  ──→  │ GameView               │
│ - Phase, Timer         │        │ - 페이즈 UI           │
│ - Round                │        │ - 타이머              │
└──────────────────────┘          └──────────────────────┘

입력 흐름:
  Unity Input → Quantum Command → Simulation 처리 → View 갱신
```

---

## 4. 시스템 의존성 다이어그램

```
                        ┌─────────────┐
                        │ GameLoop    │
                        │ System      │
                        └──────┬──────┘
                               │ 페이즈 전환 제어
               ┌───────────────┼───────────────┐
               ▼               ▼               ▼
      ┌──────────────┐ ┌────────────┐ ┌──────────────┐
      │ Planning     │ │ Combat     │ │ Result       │
      │ Phase        │ │ Phase      │ │ Phase        │
      └──────┬───────┘ └─────┬──────┘ └──────┬───────┘
             │               │               │
     ┌───────┼────────┐     │         ┌──────┴──────┐
     ▼       ▼        ▼     ▼         ▼             ▼
┌────────┐┌──────┐┌───────┐┌──────┐┌────────┐┌──────────┐┌─────────┐
│ Shop   ││Board ││Economy││Combat││Player  ││Matchmaker││Commander│
│ System ││System││System ││System││System  ││System    ││System   │
└───┬────┘└──┬───┘└───┬───┘└──┬───┘└───┬────┘└──────────┘└────┬────┘
    │        │        │       │        │                      │
    ▼        ▼        ▼       ▼        ▼                      ▼
┌─────────────────────────────────────────────────────────────────┐
│              Champion Pool (공유 데이터)                           │
├─────────────────────────────────────────────────────────────────┤
│              Synergy System                                      │
├─────────────────────────────────────────────────────────────────┤
│              Item System                                         │
├─────────────────────────────────────────────────────────────────┤
│              Spec Data (챔피언/아이템/시너지/커맨더 스킬 테이블)       │
└─────────────────────────────────────────────────────────────────┘
```

### 의존 방향 규칙

```
GameLoop → Phase Systems → Domain Systems → Data/Spec

위에서 아래로만 의존. 역방향 의존 금지.
- ✅ CombatSystem → UnitData 읽기
- ✅ ShopSystem → ChampionPool 읽기/쓰기
- ❌ UnitData → CombatSystem 호출 (금지)
- ❌ ChampionPool → ShopSystem 호출 (금지)
```

### 게임 모드에 따른 시스템 활성화

```
GameModeConfigAsset(불변 에셋)가 각 시스템의 활성 여부를 결정한다.
모든 시스템은 Update() 진입 시 해당 플래그를 체크한다.

                    ClassicBattle  PvECampaign  Competitive
GameLoopSystem         ✓             ✓            ✓
BoardSystem            ✓             ✓            ✓
CombatSystem           ✓             ✓            ✓
CommanderSystem        ✓             ✓            ✓
ShopSystem             ✗             ✓            ✓
EconomySystem          ✗             ✓            ✓
UnitCombineSystem      ✗             ✓            ✓
SynergySystem          ✗             ✓            ✓
ItemSystem             △ (기장착)     ✓            ✓
MatchmakerSystem       ✗             ✗ (PvE전용)   ✓
EliminationSystem      ✗             ✗ (HP→실패)   ✓
SharedDraftSystem      ✗             △ (선택적)     ✓

GameModeConfig는 세션 시작 시 결정되는 불변 에셋이다.
시스템 간 역방향 의존을 만들지 않는다.
상세: 02_GameLoop.md §0 참조.
```

---

## 5. Quantum ECS 핵심 개념 매핑

### 현행 시스템 → Quantum ECS 매핑

| 현행 (MonoBehaviour) | Quantum ECS | 설명 |
|---------------------|-------------|------|
| `CharacterController` (God Object) | `Unit` 컴포넌트 + 여러 System | 데이터와 로직 분리 |
| `InGameObjectManager` (44KB) | `BoardSystem` + `UnitSpawnSystem` | 역할별 System 분리 |
| `InGameTouchManager` (49KB) | Unity 입력 → `Command` | 시뮬레이션 밖에서 처리 |
| `CharacterStateBase` (상태 머신) | `UnitStateMachine` System | ECS 시스템으로 상태 전환 |
| `EffectCodeBase` (스킬/버프) | `EffectSystem` + 데이터 컴포넌트 | 데이터 드리븐 |
| `InGameGrid` | `Board` 컴포넌트 + `BoardSystem` | 그리드 데이터/로직 분리 |
| `InGameRandomManager` | Quantum 내장 `RNGSession` | 프레임워크 레벨 결정론 |
| `InGameSynergyManager` | `SynergySystem` | 독립 System |
| `SingletonMonoBehaviour<T>` | Quantum `Frame.Global` | ECS 전역 상태 |
| `SpecDataManager` | Quantum `RuntimeConfig` / `AssetDB` | Quantum 에셋 시스템 |

### Quantum 핵심 개념

```
Component (데이터)     → 순수 데이터 구조체. 로직 없음.
System (로직)          → Component를 읽고 쓰는 로직. 순수 함수.
Entity                → Component의 묶음 (ID).
Frame                 → 한 틱의 전체 게임 상태 스냅샷.
Command               → 플레이어 입력 (비결정론적 → 결정론적 처리).
Signal                → System 간 이벤트 통신.
Asset                 → 읽기 전용 설정 데이터 (스펙 테이블 등).
```

---

## 6. 폴더 구조 설계

```
Assets/_Project/Scripts/InGame
├── Quantum/                          # Quantum 시뮬레이션 (순수 C#)
│   ├── Core/                         # 핵심 게임 루프
│   │   ├── GameLoopSystem.cs         # 페이즈 전환, 라운드 관리
│   │   ├── GameInitSystem.cs         # 모드별 초기화 (유닛 소스 분기)
│   │   ├── GameModeConfigAsset.cs    # 모드 정의 에셋 (02_GameLoop.md §0)
│   │   ├── PhaseTimer.cs             # 페이즈 타이머
│   │   └── MatchmakerSystem.cs       # 라운드별 상대 배정
│   ├── Combat/                       # 전투 시뮬레이션
│   │   ├── CombatSystem.cs           # 전투 메인 루프
│   │   ├── TargetingSystem.cs        # 타겟 선택
│   │   ├── DamageSystem.cs           # 데미지 계산
│   │   ├── ManaSystem.cs             # 마나 관리
│   │   ├── UnitStateMachine.cs       # 유닛 상태 전환
│   │   └── SkillSystem.cs            # 스킬 발동
│   ├── Economy/                      # 경제
│   │   ├── EconomySystem.cs          # 골드/XP/레벨
│   │   ├── ShopSystem.cs             # 샵/리롤
│   │   └── ChampionPoolSystem.cs     # 공유 풀 관리
│   ├── Board/                        # 보드 관리
│   │   ├── BoardSystem.cs            # 유닛 배치/이동
│   │   ├── BenchSystem.cs            # 대기석
│   │   └── UnitCombineSystem.cs      # 별 승급 합성
│   ├── Synergy/                      # 시너지
│   │   └── SynergySystem.cs          # 시너지 계산/적용
│   ├── Item/                         # 아이템
│   │   ├── ItemSystem.cs             # 아이템 장착/해제
│   │   └── ItemCombineSystem.cs      # 아이템 조합
│   ├── Player/                       # 플레이어
│   │   ├── PlayerSystem.cs           # HP, 탈락, 순위
│   │   └── PlayerDamageSystem.cs     # 패배 시 데미지
│   ├── Commander/                    # 커맨더 스킬
│   │   └── CommanderSkillSystem.cs   # 전투 중 플레이어 특수 스킬
│   ├── Effect/                       # 버프/디버프/CC
│   │   ├── EffectSystem.cs           # 이펙트 적용/제거
│   │   └── StatModifierSystem.cs     # 스탯 변경
│   ├── Components/                   # ECS 컴포넌트 (DSL)
│   │   ├── Unit.qtn                  # 유닛 데이터
│   │   ├── Player.qtn               # 플레이어 데이터
│   │   ├── Board.qtn                # 보드 데이터
│   │   ├── Shop.qtn                 # 샵 데이터
│   │   ├── Item.qtn                 # 아이템 데이터
│   │   ├── Commander.qtn            # 커맨더 스킬 데이터
│   │   └── Effect.qtn               # 이펙트 데이터
│   └── Assets/                       # Quantum 에셋 (스펙 데이터)
│       ├── ChampionSpec.cs           # 챔피언 스펙
│       ├── ItemSpec.cs               # 아이템 스펙
│       ├── SynergySpec.cs            # 시너지 스펙
│       └── CommanderSkillSpec.cs     # 커맨더 스킬 스펙
│
├── View/                             # Unity Presentation Layer
│   ├── Board/                        # 보드 비주얼
│   │   ├── BoardView.cs             # 보드 렌더링
│   │   ├── TileView.cs              # 타일 비주얼
│   │   └── UnitView.cs              # 유닛 비주얼 (Spine)
│   ├── UI/                           # UI
│   │   ├── ShopView.cs              # 샵 UI
│   │   ├── SynergyPanel.cs          # 시너지 패널
│   │   ├── EconomyHUD.cs            # 골드/XP/레벨 HUD
│   │   ├── CommanderSkillView.cs     # 커맨더 스킬 버튼/쿨타임
│   │   ├── PhaseTimerView.cs        # 페이즈 타이머
│   │   ├── PlayerHPBar.cs           # 플레이어 HP
│   │   ├── MinimapView.cs           # 상대 보드 미니맵
│   │   └── ResultScreen.cs          # 순위/결과
│   ├── VFX/                          # 이펙트
│   │   ├── CombatVFXManager.cs      # 전투 VFX
│   │   └── SkillVFXPlayer.cs        # 스킬 이펙트
│   ├── Input/                        # 입력 처리
│   │   ├── DragDropHandler.cs       # 드래그 앤 드롭
│   │   └── InputCommandBridge.cs    # 입력 → Quantum Command
│   └── Camera/                       # 카메라
│       ├── GameCamera.cs            # 메인 카메라
│       └── SpectateCamera.cs        # 관전 카메라
│
├── Network/                          # 네트워크 연결
│   ├── PhotonLauncher.cs            # Photon 연결/룸 관리
│   ├── MatchmakingManager.cs        # 매칭 UI/로직
│   └── ReconnectHandler.cs          # 재접속 처리
│
├── Data/                             # 로컬 데이터/설정
│   ├── GameConfig.cs                # 게임 설정값
│   └── SpecLoader.cs               # 스펙 데이터 로드
│
└── Common/                           # 공용 유틸리티
    ├── Pool/                         # 오브젝트 풀링
    └── Extensions/                   # 확장 메서드
```

---

## 7. 핵심 설계 원칙

### 7.1 Simulation ↔ View 완전 분리

```
원칙: 게임 로직은 Unity에 의존하지 않는다.

Quantum Simulation:
  - 순수 C#, Unity API 호출 금지
  - MonoBehaviour, Transform, GameObject 사용 금지
  - 결정론적 수학만 사용 (FP, FPVector2)
  - 모든 게임 상태는 Frame에 존재

Unity View:
  - Simulation 데이터를 읽어서 비주얼 반영
  - 입력을 Command로 변환하여 Simulation에 전달
  - View → Simulation 직접 쓰기 금지
```

### 7.2 단방향 의존

```
현행 문제: CharacterController ↔ InGameObjectManager 순환 의존
개선: System → Component (읽기/쓰기), System → System (Signal)

┌──────────┐     ┌──────────┐     ┌──────────┐
│ SystemA  │ ──→ │ Component│ ←── │ SystemB  │
└──────────┘     └──────────┘     └──────────┘
      │                                 ▲
      └────── Signal ──────────────────┘

System은 다른 System을 직접 호출하지 않는다.
Component(데이터)를 통해 간접 통신하거나 Signal을 사용한다.
```

### 7.3 데이터 드리븐

```
현행 문제: EffectCode 100+ 클래스, 인덱스 기반 파라미터
개선: 스펙 데이터(Asset)로 행동 정의, System은 범용 로직만 처리

현행:
  class EffectCodeSkill1101001 : EffectCodeCharacterBase  // 스킬마다 별도 클래스
  {
      cooltime = codeInfo.GetCodeStatToFloat(0);  // 인덱스 0이 뭔지 모름
  }

개선:
  // ChampionSpec.asset (데이터)
  {
      SkillId: 1101001,
      ManaCost: 80,
      DamageType: AP,
      DamageMultiplier: 3.5,
      TargetType: NearestEnemy,
      AreaShape: Circle,
      AreaRange: 2,
      Effects: [{ Type: Stun, Duration: 1.5 }]
  }

  // SkillSystem.cs (범용 로직)
  void ExecuteSkill(Frame f, EntityRef unit, SkillSpec spec) {
      var targets = FindTargets(f, unit, spec.TargetType, spec.AreaShape, spec.AreaRange);
      foreach (var target in targets) {
          ApplyDamage(f, unit, target, spec.DamageType, spec.DamageMultiplier);
          foreach (var effect in spec.Effects) {
              ApplyEffect(f, target, effect);
          }
      }
  }
```

### 7.4 역할 분리 (SRP)

```
현행 문제: InGameObjectManager 하나에 6개 이상의 역할
개선: 시스템별 단일 책임

현행 InGameObjectManager (44KB):
  ├─ 캐릭터 스폰/제거     → UnitSpawnSystem
  ├─ 캐릭터 컬렉션 관리   → Board 컴포넌트
  ├─ 타겟 검색            → TargetingSystem
  ├─ 범위 판정            → BoardSystem (범위 쿼리)
  ├─ 프레임 업데이트       → Quantum SystemMainThread (자동)
  └─ 드래그 로직          → DragDropHandler (View)
```

### 7.5 상태 머신 개선

```
현행 문제: StatePriority 갭 (0~10, 96~99), 병렬/직렬 혼재
개선: 명확한 레이어 분리

Primary Layer (한 번에 하나):
  Idle → Move → Attack → Skill → Dead

  전환 규칙:
  - Dead는 최고 우선순위, 항상 전환
  - Skill > Attack > Move > Idle
  - 상위 상태에서 하위로 직접 전환 금지 (Idle 경유)

Overlay Layer (병렬 적용):
  CC: Stun, Silence, Airborne, Knockback
  Buff/Debuff: 독립적으로 적용, Primary와 공존

  CC → Primary 영향:
  - Stun: Primary를 강제 Idle
  - Silence: Skill 사용 불가
  - Airborne: Primary를 강제 Idle + 이동 불가
  - Knockback: 강제 이동 후 복귀
```

---

## 8. 현행 문제점 대비 개선 요약

| 현행 문제 | 원인 | 개선 방향 |
|----------|------|----------|
| God Object (44KB, 49KB) | 역할 분리 안 됨 | ECS System별 단일 책임 |
| 순환 의존 | Controller ↔ Manager | System → Component 단방향 |
| 싱글턴 남용 | 전역 접근 편의 | Quantum Frame (전역 상태 관리) |
| object 캐스팅 | `SetStateData(object)` | ECS 컴포넌트 타입 안전 |
| 인덱스 파라미터 | `GetCodeStatToFloat(0)` | 명명된 Asset 필드 |
| 100+ EffectCode 클래스 | 스킬마다 클래스 | 데이터 드리븐 + 범용 System |
| FlowState 중복 | 모드별 복붙 | 단일 GameLoopSystem + GameModeConfig |
| StatePriority 갭 | 설계 혼란 | Primary/Overlay 레이어 분리 |
| ObfuscatorFloat 오버헤드 | 매 접근 XOR | 서버 검증으로 대체 (Quantum) |
| new List 할당 | 임시 리스트 GC | ECS 네이티브 리스트 (풀링 불필요) |

---

## 9. 데이터 흐름 전체도

```
[플레이어 입력]
     │
     ▼
[Unity Input Layer]
  - 터치/드래그 감지
  - UI 버튼 클릭
     │
     ▼ (Command)
[Quantum Simulation]
  - GameLoopSystem: 페이즈 관리
  - ShopSystem: 구매/판매/리롤
  - BoardSystem: 유닛 배치/이동
  - UnitCombineSystem: 합성
  - CombatSystem: 자동 전투
  - EconomySystem: 골드/XP
  - SynergySystem: 시너지 계산
  - PlayerSystem: HP/탈락
     │
     ▼ (State Update)
[Quantum Frame - 게임 상태]
  - 4인 플레이어 데이터
  - 4개 보드 + 벤치
  - 모든 유닛 상태
  - 경제 상태
  - 전투 상태
     │
     ├──→ [Photon Cloud] → 다른 클라이언트 동기화
     │
     ▼ (View Update)
[Unity View Layer]
  - UnitView: Spine 애니메이션, HP바
  - BoardView: 타일 하이라이트
  - ShopView: 샵 갱신
  - CombatVFX: 전투 이펙트
  - UI: 골드, XP, 시너지, 타이머
```

---

## 10. 다음 단계

이 아키텍처를 기반으로 다음 문서에서 각 시스템의 상세 설계를 진행한다:

1. **02_GameLoop.md** - 페이즈 시스템, 라운드 구조, 스테이지 진행
2. **03_NetworkSync.md** - Quantum 통합, 동기화 포인트, 재접속

### Photon 제품 결정 요청

아키텍처 설계를 더 구체화하기 위해 Photon 제품 결정이 필요하다:

- **Quantum 3**: 이 문서에서 권장. 공식 오토체스 샘플 기반으로 빠른 프로토타이핑 가능.
- **Fusion 2**: 대안. MonoBehaviour 기반으로 기존 팀 경험 활용 가능.

결정에 따라 이후 문서의 구체적인 구현 패턴이 달라진다.
