# 인게임 시스템 전체 개요

> 작성일: 2026-02-06
> 목적: 인게임 로직 개편을 위한 현행 아키텍처 분석

---

## 1. 폴더 구조

```
Assets/_Project/Scripts/InGame/
├── Character/                    # 캐릭터 엔티티 시스템
│   ├── Controller/               # CharacterController (Core, Combat, Stat 부분 클래스)
│   ├── CharacterStates/          # 상태 머신 구현체 (Idle, Attack, Skill, Move, CC, Dead 등)
│   │   └── Prologue/             # 프롤로그 전용 상태
│   ├── StatData/                 # CharacterStatData, MonsterStatData
│   └── CharacterStateBase.cs     # 상태 머신 베이스 클래스
│
├── EffectCode/                   # 이펙트코드 시스템 (스킬, 버프, 디버프, CC, 스탯)
│   ├── Character/                # 캐릭터 레벨 이펙트코드
│   │   ├── Impls/                # 구현체
│   │   │   ├── Skills/           # 플레이어/몬스터 스킬
│   │   │   ├── Buffs/            # 버프 (PermanentBuff 포함)
│   │   │   ├── Debuffs/          # 디버프 (PermanentDebuff 포함)
│   │   │   ├── CrowdControls/    # CC기 (에어본, 스턴, 빙결 등)
│   │   │   ├── Passive/          # 패시브 (JobPassive, SkillPassive)
│   │   │   ├── Stats/            # 스탯 변경 코드 (30+개)
│   │   │   ├── Synergy/          # 시너지 효과
│   │   │   └── NearlyBuff/       # 근접 버프
│   │   ├── EffectCodeCharacterBase.cs
│   │   ├── EffectCodeStatBase.cs
│   │   └── EffectCodeBuffDebuffBase.cs
│   ├── Game/                     # 게임 레벨 이펙트코드
│   │   └── Impls/
│   │       ├── ChapterRule/      # 챕터 규칙
│   │       └── CommanderSkill/   # 지휘관 스킬
│   ├── Tile/                     # 타일 레벨 이펙트코드
│   ├── EffectCodeBase.cs
│   ├── EffectCodeContainer.cs
│   ├── EffectCodeContainerTeam.cs
│   ├── EffectCodeHelper.cs
│   └── EffectCodePoolManager.cs
│
├── Grid/                         # 그리드 시스템
│   ├── InGameGrid.cs             # 핵심 그리드 로직 (768줄)
│   ├── InGameTile.cs             # 개별 타일
│   └── InGameStage.cs            # 스테이지 씬 구성
│
├── Managers/                     # 매니저 시스템
│   ├── InGameManager.cs          # 게임 오케스트레이터
│   ├── InGameMainFlowManager.cs  # 메인 루프 및 흐름 상태
│   ├── InGameObjectManager.cs    # 캐릭터 생명주기/타겟팅 (44K줄)
│   ├── InGameTouchManager.cs     # 터치 입력 처리 (49K줄)
│   ├── InGameSynergyManager.cs   # 시너지 계산
│   ├── InGameCommanderManager.cs # 지휘관 스킬
│   ├── InGameBattleItemComponent.cs # 전투 아이템
│   ├── InGameVfxManager.cs       # VFX 관리
│   ├── InGameRandomManager.cs    # 시드 기반 RNG
│   ├── InGameCalculator.cs       # 데미지/스탯 공식
│   ├── InGameStatistics.cs       # 전투 통계
│   └── InGameResourceHolder.cs   # 리소스 캐싱
│
├── GameFlowStates/               # 게임 흐름 상태 (모드별)
│   ├── Stage/                    # 스테이지, 던전, 프롤로그, 로비
│   └── Test/                     # 테스트 모드
│
├── StateMachine/                 # 상태 머신 인프라
│   ├── StateBase.cs              # StateBase, StateCombatBase, StateReadyBase
│   └── StatePool.cs              # 상태 오브젝트 풀링
│
├── Views/                        # 시각적 표현
│   ├── SpriteCharacterView.cs    # 캐릭터 스프라이트 렌더링
│   ├── HpBarView.cs              # 체력바 UI
│   └── InGameTextView.cs         # 플로팅 텍스트
│
├── VFX/                          # 시각 효과
│   ├── InGameVfxMovement/        # VFX 이동 애니메이션
│   └── PrologueImps/             # 프롤로그 VFX
│
├── Camera/                       # 카메라 제어
├── Common/                       # 공통 유틸리티 (Enums, Defines, Extensions)
└── Test/                         # 테스트 인프라
    └── Editor/Presets/
```

---

## 2. 시스템 맵 및 의존성

```
┌─────────────────────────────────────────────────────────────────────┐
│                        InGameManager                                │
│               (게임 세션 관리, 초기화/종료)                              │
│          StartInGame<T>() → EndInGame()                             │
└────────────────────────────┬────────────────────────────────────────┘
                             │ 초기화 순서
                             ▼
┌─────────────────────────────────────────────────────────────────────┐
│                    InGameMainFlowManager                            │
│              (메인 루프, 흐름 상태 전환)                                │
│    Update() → deltaTime × fastForwardRate → 핸들러 호출              │
│    FlowState: Ready → Combat → Clear/Fail                          │
└──────────┬──────────────────────────────┬───────────────────────────┘
           │ UpdatePriority_TopTier       │ FlowState 전환
           ▼                              ▼
┌──────────────────────┐    ┌─────────────────────────────────────────┐
│  InGameObjectManager │    │          GameFlowStates                 │
│  (캐릭터 생명주기)      │    │  Stage: Ready→Combat→Clear/Fail         │
│  - 스폰/제거           │    │  TrialDungeon: Ready→Combat→Clear/Fail  │
│  - 타겟 검색           │    │  Prologue: Ready→Combat→Clear           │
│  - 범위 판정           │    │  Lobby: Combat (자동전투)                 │
│  - 경로 탐색           │    │  Test: Ready→Combat                     │
└──────────┬───────────┘    └─────────────────────────────────────────┘
           │
           ▼
┌──────────────────────┐    ┌──────────────────────┐
│  CharacterController │◄──►│     InGameGrid       │
│  (캐릭터 엔티티)       │    │  (타일 기반 그리드)    │
│  - 상태 머신           │    │  - 타일 점유/해제      │
│  - 스탯 관리           │    │  - BFS 경로 탐색      │
│  - 데미지 처리         │    │  - Manhattan 거리     │
│  - EffectCode 연동    │    │  - 공격 범위 형태      │
└──────────┬───────────┘    └──────────────────────┘
           │
           ▼
┌──────────────────────┐    ┌──────────────────────┐
│  CharacterStateBase  │    │  EffectCodeContainer │
│  (상태 머신)           │    │  (이펙트코드 관리)     │
│  - Idle → 타겟 탐색    │    │  - 스킬/버프/디버프    │
│  - Attack → 일반 공격  │    │  - CC/스탯/패시브      │
│  - Skill → 스킬 사용   │    │  - 플래그 기반 이벤트   │
│  - Move → 타일 이동    │    │  - CalcOrder 스탯 계산 │
│  - CC → 상태이상       │    │                      │
│  - Dead → 사망 처리    │    │                      │
└──────────────────────┘    └──────────────────────┘

보조 시스템:
┌──────────────────────┐ ┌──────────────────────┐ ┌──────────────────────┐
│ InGameSynergyManager │ │InGameCommanderManager│ │  InGameRandomManager │
│ (속성/성좌 시너지)     │ │(지휘관 스킬, 카메라)   │ │ (시드 기반 RNG)       │
└──────────────────────┘ └──────────────────────┘ └──────────────────────┘
┌──────────────────────┐ ┌──────────────────────┐ ┌──────────────────────┐
│  InGameVfxManager    │ │  InGameStatistics    │ │  InGameCalculator    │
│ (VFX 풀링/관리)       │ │ (전투 통계/MVP)       │ │ (데미지/쿨타임 공식)   │
└──────────────────────┘ └──────────────────────┘ └──────────────────────┘
```

---

## 3. 핵심 클래스 역할 요약

### 게임 라이프사이클

| 클래스 | 파일 | 역할 |
|--------|------|------|
| `InGameManager` | `Managers/InGameManager.cs` | 게임 세션 관리, 컴포넌트 초기화/정리, 팀 이펙트코드 관리 |
| `InGameMainFlowManager` | `Managers/InGameMainFlowManager.cs` | 메인 루프 실행, FlowState 전환, 게임 속도/일시정지 |
| `StateBase` | `StateMachine/StateBase.cs` | 흐름 상태 베이스 (SetStateData → StateInit → StateStart → StateRunning → StateEnd) |
| `StatePool` | `StateMachine/StatePool.cs` | 상태 오브젝트 풀링 (GC 방지) |

### 캐릭터 시스템

| 클래스 | 파일 | 역할 |
|--------|------|------|
| `CharacterController` | `Character/Controller/CharacterController.cs` | 캐릭터 엔티티의 핵심 로직 (상태, 스탯, 데미지, EffectCode) |
| `CharacterStateBase` | `Character/CharacterStateBase.cs` | 캐릭터 상태 머신 베이스 (우선순위, RunningResult 플래그) |
| `CharacterStatData` | `Character/StatData/CharacterStatData.cs` | 캐릭터 스탯 데이터 컨테이너 |
| `InGameObjectManager` | `Managers/InGameObjectManager.cs` | 캐릭터 스폰/제거, 타겟 검색, 범위 판정 |

### 이펙트코드 시스템

| 클래스 | 파일 | 역할 |
|--------|------|------|
| `EffectCodeBase` | `EffectCode/EffectCodeBase.cs` | 이펙트코드 최상위 베이스 |
| `EffectCodeStatBase` | `EffectCode/Character/EffectCodeStatBase.cs` | 스탯 수정자 시스템 (CalcOrder, Fixed/Percent) |
| `EffectCodeCharacterBase` | `EffectCode/Character/EffectCodeCharacterBase.cs` | 캐릭터 스킬/패시브 베이스 (쿨타임, 활성화, 이벤트 콜백) |
| `EffectCodeBuffDebuffBase` | `EffectCode/Character/EffectCodeBuffDebuffBase.cs` | 버프/디버프 스택 관리 |
| `EffectCodeContainer` | `EffectCode/EffectCodeContainer.cs` | 이펙트코드 등록/제거/조회 컨테이너 |
| `EffectCodeContainerTeam` | `EffectCode/EffectCodeContainerTeam.cs` | 팀 전체 이펙트코드 (시너지 등) |
| `EffectCodePoolManager` | `EffectCode/EffectCodePoolManager.cs` | 이펙트코드 인스턴스 풀링 |

### 그리드 시스템

| 클래스 | 파일 | 역할 |
|--------|------|------|
| `InGameGrid` | `Grid/InGameGrid.cs` | 2D 타일 그리드 관리 (경로 탐색, 범위 계산, 위치 추천) |
| `InGameTile` | `Grid/InGameTile.cs` | 개별 타일 (점유 추적, 타일 이펙트코드) |
| `InGameStage` | `Grid/InGameStage.cs` | 스테이지 씬 컴포넌트 (그리드 크기, 타일 뷰) |

---

## 4. 네임스페이스 구조

| 네임스페이스 | 사용 영역 |
|-------------|----------|
| `CookApps.BattleSystem` | 인게임 핵심 (CharacterController, 상태 머신, 그리드, 매니저) |
| `CookApps.AutoBattler` | 상위 레벨 (InGameCalculator, EffectCode 등) |
| `CookApps.TeamBattle` | 공통 유틸리티 (SingletonMonoBehaviour 등) |
| `CookApps.Obfuscator` | 값 난독화 (ObfuscatorFloat, ObfuscatorInt) |

---

## 5. 주요 외부 의존성

| 패키지 | 용도 |
|--------|------|
| `UniTask` | 비동기 처리 (캐릭터 스폰, 상태 전환, 서버 통신) |
| `Unity.Mathematics` | `int2` 등 수학 타입 (그리드 좌표) |
| `ObfuscatorFloat/Int` | 메모리 값 난독화 (치팅 방지) |
| `R3` | 리액티브 패턴 (UI 바인딩) |

---

## 6. 문서 목차

| 번호 | 문서 | 내용 |
|------|------|------|
| 01 | [전투 흐름](01_BattleFlow.md) | GameFlowState 생명주기, 모드별 분석, 배틀 시퀀스 |
| 02 | [캐릭터 시스템](02_CharacterSystem.md) | CharacterController, 상태 머신, 상태 전환 규칙 |
| 03 | [이펙트코드 시스템](03_EffectCodeSystem.md) | 스킬/버프/CC, 플래그 시스템, 스탯/데미지 계산 |
| 04 | [그리드 시스템](04_GridSystem.md) | 타일, 경로 탐색, 공격 범위, 위치 추천 |
| 05 | [매니저 시스템](05_ManagerSystems.md) | 12개 매니저 클래스 상세 분석 |
| 06 | [문제점 및 개선 포인트](06_KnownIssues.md) | 현행 구조의 아키텍처적 문제점, 개편 시 고려사항 |
