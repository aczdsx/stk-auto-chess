# 게임 루프 & 페이즈 시스템 설계

> Quantum 3 기반의 전체 게임 흐름과 라운드/페이즈 구조를 정의한다.
> 다양한 게임 모드를 단일 아키텍처로 수용하기 위한 GameMode 추상화를 포함한다.

---

## 0. 게임 모드 추상화 (GameMode)

### 0.1 개요

```
단일 시스템 아키텍처가 여러 게임 모드를 수용한다.
모드별로 활성화되는 서브시스템과 게임 흐름이 달라진다.

핵심 원칙:
  - 전투 시스템(Combat)은 모든 모드의 공통 기반
  - 경제/상점/매칭 등은 모드별 선택적 활성화
  - GameModeConfig 에셋으로 모드 정의 → 코드 분기 최소화
  - 새 모드 추가 시 Config만 작성하면 됨 (시스템 코드 수정 불필요)
```

### 0.2 모드 타입 정의

```qtn
enum GameModeType {
    ClassicBattle,     // 현행 싱글 배틀 (보유 캐릭터 1회 전투)
    PvECampaign,       // PvE 캠페인 (상점O, 다수 크립 라운드)
    Competitive        // 4인 경쟁 오토체스 (풀 스펙)
}
```

### 0.3 모드별 비교

```
┌────────────────┬──────────────┬──────────────┬──────────────┐
│ 기능            │ ClassicBattle│ PvECampaign  │ Competitive  │
├────────────────┼──────────────┼──────────────┼──────────────┤
│ 플레이어 수     │ 1            │ 1            │ 4            │
│ 네트워크        │ 로컬         │ 로컬         │ Quantum 멀티 │
│ 라운드 수       │ 1            │ 다수 (설정)   │ 다수 (탈락)  │
│ 상점/리롤       │ ✗            │ ✓            │ ✓            │
│ 챔피언 풀       │ ✗            │ 솔로 풀      │ 공유 풀      │
│ 경제 (골드/XP)  │ ✗            │ ✓            │ ✓            │
│ 유닛 소스       │ 보유 캐릭터   │ 상점 구매     │ 상점 구매    │
│ 합성 (별 승급)  │ ✗            │ ✓            │ ✓            │
│ 시너지          │ 레거시/비활성 │ ✓            │ ✓            │
│ 아이템          │ 기존 장비     │ PvE 드롭      │ PvE 드롭    │
│ 상대            │ 사전정의 AI   │ 크립 웨이브   │ 플레이어/크립│
│ HP 시스템       │ ✗ (1회 승패)  │ ✓            │ ✓            │
│ 탈락/순위       │ ✗            │ ✗ (HP=0→실패) │ ✓            │
│ 매치메이킹      │ ✗            │ ✗            │ ✓            │
│ 커맨더 스킬     │ ✓            │ ✓            │ ✓            │
│ 공유 드래프트   │ ✗            │ 선택적        │ ✓            │
└────────────────┴──────────────┴──────────────┴──────────────┘
```

### 0.4 GameModeConfig 에셋

```csharp
public class GameModeConfigAsset : AssetObject
{
    public GameModeType ModeType;

    // --- 플레이어 ---
    public int PlayerCount;           // 1 또는 4
    public int StartingHP;            // 0 = HP 시스템 비활성 (ClassicBattle)
    public int StartingGold;          // 0 = 경제 비활성
    public int StartingLevel;         // ClassicBattle: 보유 캐릭터 수에 맞춰 설정

    // --- 유닛 소스 ---
    public UnitSourceType UnitSource; // OwnedCharacters | ShopPurchase

    // --- 시스템 활성화 플래그 ---
    public bool EnableShop;
    public bool EnableEconomy;        // 골드 수입, 이자, XP 구매
    public bool EnableChampionPool;   // false면 솔로 무한 풀
    public bool SharedPool;           // true: 4인 공유, false: 개인 풀
    public bool EnableCombine;        // 별 승급
    public bool EnableSynergy;
    public bool EnableItems;
    public bool EnableMatchmaking;    // PvP 매칭
    public bool EnableElimination;    // HP 탈락 시스템
    public bool EnableSharedDraft;

    // --- 라운드 ---
    public RoundEndCondition RoundEndCondition;  // FixedCount | Elimination | SingleBattle
    public int MaxRounds;             // FixedCount일 때 최대 라운드 수 (0=무제한)
    public AssetRef<RoundConfigAsset> RoundConfig;

    // --- 전투 ---
    public AssetRef<BoardConfigAsset> BoardConfig; // 보드 설정 (공통)
}

enum UnitSourceType {
    OwnedCharacters,   // 서버 보유 캐릭터 → 벤치에 로드
    ShopPurchase        // 상점에서 구매
}

enum RoundEndCondition {
    SingleBattle,       // 1회 전투 후 종료
    FixedCount,         // 정해진 라운드 수 클리어
    Elimination         // HP=0 탈락, 최후 1인까지
}
```

### 0.5 모드별 게임 흐름

```
── ClassicBattle (현행 싱글 배틀) ──

[게임 시작]
  │ 보유 캐릭터 → 벤치 로드
  ▼
[Preparation] ─── 유닛 배치 (상점 없음)
  │
  ▼
[Combat] ─── 사전정의 적과 1회 전투
  │
  ▼
[결과] ─── 승/패 판정 → 즉시 종료


── PvECampaign (크립 라운드 연속) ──

[게임 시작]
  │ 초기 골드/레벨 설정, 솔로 챔피언 풀 초기화
  ▼
[라운드 반복] ◄──────────────────────┐
  │                                  │
  ├─ [Preparation] ─── 상점/배치/합성  │
  │                                  │
  ├─ [Combat] ─── 크립 웨이브 전투     │
  │                                  │
  ├─ [Result] ─── 보상/HP감소/경제정산  │
  │                                  │
  ├─ HP > 0 && 라운드 남음? ──────────┘
  │
  ▼
[종료] ─── 클리어 or 실패


── Competitive (4인 경쟁 오토체스) ──

(기존 섹션 1의 흐름과 동일)
```

### 0.6 시스템 활성화 로직

```csharp
// 각 시스템은 Update 진입 시 GameModeConfig를 체크
public unsafe class ShopSystem : SystemMainThread
{
    public override void Update(Frame f)
    {
        var config = f.FindAsset<GameModeConfigAsset>(f.RuntimeConfig.GameModeConfig);
        if (!config.EnableShop) return; // 이 모드에서 상점 비활성
        // ... 상점 로직
    }
}

public unsafe class EconomySystem : SystemMainThread
{
    public override void Update(Frame f)
    {
        var config = f.FindAsset<GameModeConfigAsset>(f.RuntimeConfig.GameModeConfig);
        if (!config.EnableEconomy) return;
        // ... 경제 로직
    }
}

public unsafe class MatchmakerSystem : SystemMainThread
{
    public override void Update(Frame f)
    {
        var config = f.FindAsset<GameModeConfigAsset>(f.RuntimeConfig.GameModeConfig);
        if (!config.EnableMatchmaking)
        {
            // 싱글/PvE 모드: 모든 플레이어가 각자 CreepWave와 전투
            SetupPvEMatches(f);
            return;
        }
        // ... PvP 매칭 로직
    }
}
```

### 0.7 유닛 소스 분기 - ClassicBattle 초기화

```csharp
// ClassicBattle 모드: 보유 캐릭터를 벤치에 로드
public unsafe class GameInitSystem : SystemSignalsOnly, ISignalOnGameStart
{
    public void OnGameStart(Frame f)
    {
        var config = f.FindAsset<GameModeConfigAsset>(f.RuntimeConfig.GameModeConfig);

        switch (config.UnitSource)
        {
            case UnitSourceType.OwnedCharacters:
                LoadOwnedCharacters(f);
                break;
            case UnitSourceType.ShopPurchase:
                InitializeShopAndPool(f, config);
                break;
        }
    }

    private void LoadOwnedCharacters(Frame f)
    {
        // RuntimeConfig에 포함된 보유 캐릭터 목록을 벤치에 생성
        // 캐릭터 스탯은 서버 데이터 기반 (ChampionSpec과 별도)
        var roster = f.RuntimeConfig.PlayerRoster; // 사전 직렬화된 보유 캐릭터
        for (int i = 0; i < roster.Length && i < 9; i++) // 벤치 9슬롯 제한
        {
            var entity = f.Create();
            var unit = f.Add<UnitData>(entity);
            unit->Owner = PlayerRef.None; // 싱글이므로 Player 0
            unit->ChampionSpecId = roster[i].SpecId;
            unit->StarLevel = roster[i].StarLevel;
            unit->Location = UnitLocation.Bench;
            unit->BenchIndex = (byte)i;
            // ... 기존 캐릭터 스탯 매핑
        }
    }
}
```

### 0.8 GameLoopSystem 모드 인식

```csharp
private void UpdateResult(Frame f)
{
    var config = f.FindAsset<GameModeConfigAsset>(f.RuntimeConfig.GameModeConfig);

    switch (config.RoundEndCondition)
    {
        case RoundEndCondition.SingleBattle:
            // 1회 전투 결과 → 즉시 GameOver
            TransitionTo(f, GamePhase.GameOver);
            break;

        case RoundEndCondition.FixedCount:
            // 최대 라운드 도달 or HP=0 → 종료
            if (global->TotalRoundCount >= config.MaxRounds || global->AlivePlayerCount <= 0)
                TransitionTo(f, GamePhase.GameOver);
            else
                TransitionTo(f, GamePhase.Preparation);
            break;

        case RoundEndCondition.Elimination:
            // 생존자 1인 이하 → 종료 (기존 Competitive 로직)
            if (global->AlivePlayerCount <= 1)
                TransitionTo(f, GamePhase.GameOver);
            else
                TransitionTo(f, GamePhase.Preparation);
            break;
    }
}
```

### 0.9 RuntimeConfig 확장

```qtn
// 기존 RuntimeConfig에 GameMode 참조 추가
partial RuntimeConfig {
    AssetRef<GameModeConfigAsset> GameModeConfig;

    // ClassicBattle용: 보유 캐릭터 목록
    array<OwnedUnit>[9] PlayerRoster;
}

struct OwnedUnit {
    Int32 SpecId;           // 캐릭터 스펙 ID
    Byte StarLevel;         // 별 등급
    Byte Level;             // 캐릭터 레벨
    array<Int32>[3] ItemIds; // 장착 아이템
}
```

### 0.10 확장 가능성

```
이 구조로 추가 가능한 모드 예시:

- 튜토리얼 모드: ClassicBattle + 강제 배치 가이드
- 무한 웨이브: PvECampaign + MaxRounds=0 (무제한)
- 듀오 모드: Competitive + PlayerCount=2
- 코옵 PvE: 2인이 같은 크립 웨이브에 도전
- 토너먼트: Competitive + 커스텀 RoundConfig (짧은 라운드)

새 모드 추가 절차:
  1. GameModeConfigAsset 에셋 생성 (Inspector에서 설정)
  2. 필요 시 RoundConfigAsset 작성
  3. (선택) 새 UnitSourceType/RoundEndCondition 추가
  4. 시스템 코드 변경 불필요 (플래그 기반 분기)
```

---

## 1. 전체 게임 흐름 (Competitive 모드)

```
[로비]
  │
  ▼
[매칭] ─── 4인 매칭 완료
  │
  ▼
[게임 시작] ─── Quantum 세션 생성, 초기 상태 설정
  │
  ▼
[라운드 반복] ◄─────────────────────────────┐
  │                                         │
  ├─ [계획 페이즈] ─── 샵/배치/합성          │
  │                                         │
  ├─ [전투 페이즈] ─── 자동 전투             │
  │                                         │
  ├─ [결과 페이즈] ─── 데미지/경제/탈락 처리  │
  │                                         │
  ├─ 생존자 2명 이상? ─── Yes ──────────────┘
  │
  ▼ No
[게임 종료] ─── 최종 순위 확정
  │
  ▼
[결과 화면] ─── 보상 지급 → 로비 복귀
```

---

## 2. 스테이지 구조

### 스테이지-라운드 넘버링

```
Stage 1: 1-1, 1-2, 1-3, 1-4
Stage 2: 2-1, 2-2, 2-3, 2-4, 2-5
Stage 3: 3-1, 3-2, 3-3, 3-4, 3-5
Stage 4: 4-1, 4-2, 4-3, 4-4, 4-5
...
```

### 라운드 타입

| 라운드 | 타입 | 설명 |
|--------|------|------|
| 1-1, 1-2, 1-3 | **PvE** | 크립(몬스터) 라운드. 아이템 드롭. |
| 1-4 | **공유 드래프트** | 캐러셀. 챔피언+아이템 선택. |
| 2-1 ~ 2-5 | **PvP** | 플레이어 간 대전. |
| 3-1 | **PvE** | 중간 크립 라운드. |
| 3-2 ~ 3-5 | **PvP** | 플레이어 간 대전. |
| 4-1 | **공유 드래프트** | 캐러셀. |
| 이후 | **PvP** | 게임 종료까지. |

> Competitive 모드 기준 (4인). PvECampaign 모드에서는 PvP 라운드가 모두 PvE로 대체된다.
> 스테이지 구성은 RoundConfigAsset으로 모드별 설정 가능.

---

## 3. 페이즈 시스템

### 3.1 페이즈 정의

```
enum GamePhase {
    None,
    Preparation,      // 계획 페이즈: 샵/배치/합성
    Combat,           // 전투 페이즈: 자동 전투
    Result,           // 결과 페이즈: 데미지/경제 정산
    SharedDraft,      // 공유 드래프트: 캐러셀
    GameOver          // 게임 종료
}
```

### 3.2 페이즈 전환 흐름

```
┌──────────────┐     타이머 종료      ┌──────────────┐
│ Preparation  │ ──────────────────→ │   Combat     │
│              │     또는 전원 레디    │              │
│ - 샵 이용     │                     │ - 자동 전투   │
│ - 유닛 배치   │                     │ - 타겟팅      │
│ - 합성       │                     │ - 스킬 발동   │
│ - 아이템 장착  │                     │ - 버프/디버프  │
└──────────────┘                     └──────┬───────┘
       ▲                                    │ 한쪽 전멸
       │                                    │ 또는 타임아웃
       │         ┌──────────────┐           │
       └──────── │   Result     │ ◄─────────┘
                 │              │
                 │ - 승패 판정   │
                 │ - HP 감소     │
                 │ - 골드 지급   │
                 │ - XP 지급    │
                 │ - 탈락 체크   │
                 │ - 다음 라운드  │
                 └──────────────┘
```

### 3.3 페이즈별 타이머

| 페이즈 | 기본 시간 | 비고 |
|--------|----------|------|
| Preparation | 30초 | 라운드 진행에 따라 조정 가능 |
| Combat | 최대 45초 | 한쪽 전멸 시 즉시 종료 |
| Result | 5초 | 연출 + 정산 시간 |
| SharedDraft | 20초 | 선택 완료 시 단축 가능 |

> 타이머 값은 스펙 데이터(`RoundSpec`)로 관리.

---

## 4. Quantum 게임 상태 설계

### 4.1 전역 게임 상태 (Global)

```qtn
// Quantum DSL 정의
global {
    GamePhase CurrentPhase;
    Int32 CurrentStage;        // 1, 2, 3, ...
    Int32 CurrentRound;        // 스테이지 내 라운드 (1, 2, 3, ...)
    Int32 TotalRoundCount;     // 전체 라운드 번호 (1, 2, 3, ...)
    FP PhaseTimer;             // 현재 페이즈 남은 시간
    FP PhaseElapsed;           // 현재 페이즈 경과 시간
    RoundType CurrentRoundType; // PvE, PvP, SharedDraft

    // 4인 플레이어 매칭 정보
    array<MatchPair>[2] CombatMatches; // 2개의 1v1 매치

    // 게임 모드
    AssetRef<GameModeConfigAsset> GameModeConfig;

    // 게임 진행 상태
    Int32 AlivePlayerCount;
    Boolean IsGameOver;
}

enum RoundType {
    PvE,
    PvP,
    SharedDraft
}

struct MatchPair {
    player_ref Player1;
    player_ref Player2;
    Boolean IsGhostMatch;     // 고스트 매치 여부
    player_ref GhostSource;   // 고스트의 원본 플레이어
}
```

### 4.2 라운드 설정 데이터 (Asset)

```csharp
// Quantum Asset - 스펙 데이터
public class RoundConfigAsset : AssetObject {
    public RoundEntry[] Rounds;
}

public struct RoundEntry {
    public int Stage;           // 1, 2, 3...
    public int Round;           // 스테이지 내 순서
    public RoundType Type;      // PvE, PvP, SharedDraft
    public FP PreparationTime;  // 계획 페이즈 시간
    public FP CombatTimeout;    // 전투 최대 시간

    // PvE 전용
    public AssetRef<CreepWaveAsset> CreepWave; // 몬스터 웨이브 스펙
    public int ItemDropCount;   // 아이템 드롭 수
}
```

---

## 5. 페이즈별 상세 설계

### 5.1 Preparation (계획 페이즈)

```
페이즈 시작:
  1. 골드 지급 (기본 수입 + 이자 + 연승/연패 보너스)
  2. XP 지급 (라운드별 자동 획득)
  3. 샵 리프레시 (새로운 5개 챔피언 표시)
  4. 타이머 시작

플레이어 가능 행동:
  - 샵에서 챔피언 구매       → BuyUnitCommand
  - 챔피언 판매              → SellUnitCommand
  - 샵 리롤                 → RerollShopCommand
  - XP 구매 (레벨업)         → BuyXPCommand
  - 유닛 보드 배치/이동       → MoveUnitCommand
  - 유닛 보드 ↔ 벤치 이동    → SwapUnitCommand
  - 아이템 장착/해제          → EquipItemCommand
  - 레디 (준비 완료)         → ReadyCommand

페이즈 종료 조건:
  - 타이머 종료
  - 또는 전원 레디
```

### 5.2 Combat (전투 페이즈)

```
페이즈 시작:
  1. 매치메이킹 실행 (4인 → 2개의 1v1 매치 생성)
  2. 각 매치별:
     a. 양쪽 보드의 유닛을 복제 (원본 유닛에 영향 없음)
     b. 복제 유닛을 전투 보드에 배치
     c. 시너지 효과 적용
     d. 전투 시작

전투 진행:
  - 유닛 상태 머신에 따라 자동 전투
  - 다른 플레이어의 전투 관전 가능 → SpectateCommand

플레이어 가능 행동 (전투 중):
  - 아이템 장착              → EquipItemCommand (해제는 불가)
  - 커맨더 스킬 사용          → UseCommanderSkillCommand
  - 관전 대상 변경            → SpectateCommand

전투 종료 조건:
  - 한쪽 유닛 전멸
  - 또는 타임아웃 (남은 HP 합산으로 판정)
```

### 5.2.1 커맨더 스킬 (Commander Skill)

```
플레이어가 전투 중 발동하는 특수 스킬.
전투의 흐름에 직접 영향을 줌.

특성:
  - 플레이어당 1~2개의 커맨더 스킬 보유
  - 쿨타임 기반 (전투 내 또는 라운드 단위)
  - 타겟 지정형 / 즉시 발동형
  - 결정론적 처리 (Quantum Command로 전달)

예시:
  - 즉시 발동형: 아군 전체 HP 일정량 회복
  - 타겟 지정형: 특정 영역에 데미지
  - 버프형: 아군 전체 공격속도 N초간 증가

Command 흐름:
  Player Touch → UseCommanderSkillCommand(skillIndex, targetPos)
  → Quantum Simulation에서 처리
  → 전투 유닛에 즉시 효과 적용
```

### 5.3 Result (결과 페이즈)

```
페이즈 시작:
  1. 각 매치 승패 판정
  2. 패배 플레이어 HP 감소 (생존 적 유닛 수 + 별 등급 기반)
  3. 연승/연패 카운트 갱신
  4. 탈락 체크 (HP ≤ 0)
  5. 탈락자 순위 확정
  6. 생존자 수 체크
     - 1명 이하 → GameOver
     - 2명 이상 → 다음 라운드로
  7. 라운드/스테이지 번호 증가
  8. 다음 라운드 타입 결정 (RoundConfig에서 조회)
```

### 5.4 PvE (크립 라운드)

```
페이즈 시작:
  1. CreepWaveAsset에 정의된 몬스터 스폰
  2. 모든 플레이어가 각자의 보드에서 동일 몬스터와 전투

전투 종료:
  - 몬스터 전멸 → 승리: 아이템 드롭
  - 플레이어 유닛 전멸 → 패배: 아이템 드롭 없음, HP 감소
  - 타임아웃 → 남은 몬스터에 비례한 약간의 HP 감소
```

### 5.5 SharedDraft (공유 드래프트 / 캐러셀)

```
4인 게임 적응:
  - 중앙에 챔피언+아이템 조합 N개 배치
  - HP가 낮은 플레이어부터 순서대로 선택
  - 선택 순서: 4위 → 3위 → 2위 → 1위
  - 제한 시간 내 선택하지 않으면 자동 배정

플레이어 행동:
  - 캐러셀에서 원하는 챔피언 선택 → SelectDraftCommand
```

---

## 6. Quantum System 구조

### 6.1 GameLoopSystem (메인 루프)

```csharp
public unsafe class GameLoopSystem : SystemMainThread,
    ISignalOnCombatEnd,
    ISignalOnPlayerEliminated
{
    public override void Update(Frame f)
    {
        var global = f.Global;

        // 페이즈 타이머 갱신
        global->PhaseTimer -= f.DeltaTime;
        global->PhaseElapsed += f.DeltaTime;

        // 페이즈별 종료 조건 체크
        switch (global->CurrentPhase)
        {
            case GamePhase.Preparation:
                UpdatePreparation(f);
                break;
            case GamePhase.Combat:
                UpdateCombat(f);
                break;
            case GamePhase.Result:
                UpdateResult(f);
                break;
            case GamePhase.SharedDraft:
                UpdateSharedDraft(f);
                break;
        }
    }

    private void TransitionTo(Frame f, GamePhase nextPhase)
    {
        var global = f.Global;
        global->CurrentPhase = nextPhase;
        global->PhaseElapsed = FP._0;
        global->PhaseTimer = GetPhaseTime(f, nextPhase);

        // 페이즈 시작 Signal 발생
        f.Signals.OnPhaseStarted(nextPhase);

        // View에 이벤트 전달
        f.Events.PhaseChanged(nextPhase, global->CurrentStage, global->CurrentRound);
    }
}
```

### 6.2 Signal & Event 정의

```qtn
// Simulation 내부 통신 (System 간)
signal OnPhaseStarted(GamePhase phase);
signal OnRoundStarted(Int32 stage, Int32 round, RoundType type);
signal OnCombatEnd(player_ref winner, player_ref loser, Int32 matchIndex);
signal OnPlayerEliminated(player_ref player, Int32 rank);
signal OnPlayerDamaged(player_ref player, Int32 damage, Int32 remainingHP);

// View로 전달 (Unity에서 수신)
event PhaseChanged {
    GamePhase Phase;
    Int32 Stage;
    Int32 Round;
}

event CombatResult {
    player_ref Winner;
    player_ref Loser;
    Int32 Damage;
}

event PlayerEliminated {
    player_ref Player;
    Int32 Rank;
}

event GameOver {
    player_ref Winner;
}
```

### 6.3 System 실행 순서

```csharp
// SystemSetup에서 등록 순서 = 실행 순서
public static class SystemSetup
{
    public static SystemBase[] CreateSystems()
    {
        return new SystemBase[]
        {
            // 0. 초기화 (모드별 유닛 소스 로드)
            new GameInitSystem(),

            // 1. 코어 루프
            new GameLoopSystem(),

            // 2. 계획 페이즈 시스템 (모드별 비활성화 가능)
            new ShopSystem(),            // EnableShop 체크
            new BoardSystem(),
            new UnitCombineSystem(),
            new BenchSystem(),
            new ItemSystem(),

            // 3. 전투 페이즈 시스템
            new MatchmakerSystem(),
            new CombatSetupSystem(),      // 전투 초기화 (유닛 복제, 배치)
            new CommanderSkillSystem(),   // 커맨더 스킬 처리 (플레이어 입력)
            new UnitStateMachineSystem(), // 유닛 상태 전환
            new TargetingSystem(),        // 타겟 선택
            new MovementSystem(),         // 이동
            new AttackSystem(),           // 공격
            new SkillSystem(),            // 스킬
            new DamageSystem(),           // 데미지 적용
            new ManaSystem(),             // 마나 관리
            new EffectSystem(),           // 버프/디버프/CC
            new CombatResultSystem(),     // 전투 종료 판정

            // 4. 결과 페이즈 시스템
            new EconomySystem(),          // 골드/XP 정산
            new PlayerDamageSystem(),     // 플레이어 HP 감소
            new EliminationSystem(),      // 탈락 처리

            // 5. 시너지
            new SynergySystem(),          // 시너지 계산/적용

            // 6. 공유 드래프트
            new SharedDraftSystem(),
        };
    }
}
```

---

## 7. 매치메이킹 (라운드별 상대 배정)

### 4인 PvP 매칭 규칙

```
4인 중 생존자 수에 따라:

4인 생존: 2개 매치 (A vs B, C vs D)
3인 생존: 1개 매치 + 1개 고스트 매치 (A vs B, C vs Ghost(A or B))
2인 생존: 1개 매치 (A vs B) → 이 라운드 결과로 게임 종료 가능
1인 생존: 게임 종료

고스트 매치:
  - 홀수 인원일 때 이미 매칭된 플레이어의 보드를 복제하여 대전
  - 고스트전 패배 시에도 정상적으로 HP 감소
```

### 매칭 알고리즘

```
1. 생존 플레이어 목록 수집
2. 직전 라운드 매칭 기록 참조
3. 가능하면 직전 상대와 다른 상대 배정 (중복 최소화)
4. 무작위 셔플 후 순서대로 2인씩 매칭
5. 홀수면 마지막 1인에게 고스트 매치 배정
```

---

## 8. Command 정의 (플레이어 입력)

### 계획 페이즈 Command

```csharp
// 챔피언 구매
public class BuyUnitCommand : DeterministicCommand {
    public Int32 ShopSlotIndex;  // 0~4

    public override void Serialize(BitStream stream) {
        stream.Serialize(ref ShopSlotIndex);
    }
}

// 챔피언 판매
public class SellUnitCommand : DeterministicCommand {
    public EntityRef UnitEntity;

    public override void Serialize(BitStream stream) {
        stream.Serialize(ref UnitEntity);
    }
}

// 샵 리롤
public class RerollShopCommand : DeterministicCommand {
    public override void Serialize(BitStream stream) { }
}

// XP 구매
public class BuyXPCommand : DeterministicCommand {
    public override void Serialize(BitStream stream) { }
}

// 유닛 이동 (보드 내, 보드↔벤치)
public class MoveUnitCommand : DeterministicCommand {
    public EntityRef UnitEntity;
    public Int32 TargetSlotType;  // 0=Board, 1=Bench
    public Int32 TargetX;
    public Int32 TargetY;

    public override void Serialize(BitStream stream) {
        stream.Serialize(ref UnitEntity);
        stream.Serialize(ref TargetSlotType);
        stream.Serialize(ref TargetX);
        stream.Serialize(ref TargetY);
    }
}

// 아이템 장착 (계획 + 전투 페이즈 모두 가능, 해제는 계획 페이즈만)
public class EquipItemCommand : DeterministicCommand {
    public EntityRef ItemEntity;
    public EntityRef TargetUnit;

    public override void Serialize(BitStream stream) {
        stream.Serialize(ref ItemEntity);
        stream.Serialize(ref TargetUnit);
    }
}

// 아이템 해제 (계획 페이즈만 가능)
public class UnequipItemCommand : DeterministicCommand {
    public EntityRef ItemEntity;

    public override void Serialize(BitStream stream) {
        stream.Serialize(ref ItemEntity);
    }
}

// 레디 (준비 완료)
public class ReadyCommand : DeterministicCommand {
    public override void Serialize(BitStream stream) { }
}
```

### 전투 페이즈 Command

```csharp
// 커맨더 스킬 사용
public class UseCommanderSkillCommand : DeterministicCommand {
    public Int32 SkillIndex;       // 보유 스킬 인덱스 (0, 1)
    public FPVector2 TargetPos;    // 타겟 지정형일 경우 위치 (즉시 발동형은 무시)

    public override void Serialize(BitStream stream) {
        stream.Serialize(ref SkillIndex);
        stream.Serialize(ref TargetPos);
    }
}

// 관전 대상 변경
public class SpectateCommand : DeterministicCommand {
    public player_ref TargetPlayer;

    public override void Serialize(BitStream stream) {
        stream.Serialize(ref TargetPlayer);
    }
}
```

### 기타 Command

```csharp
// 공유 드래프트 선택
public class SelectDraftCommand : DeterministicCommand {
    public Int32 DraftSlotIndex;

    public override void Serialize(BitStream stream) {
        stream.Serialize(ref DraftSlotIndex);
    }
}
```

### Command 허용 페이즈 매트릭스

| Command | Preparation | Combat | SharedDraft |
|---------|:-----------:|:------:|:-----------:|
| BuyUnitCommand | O | | |
| SellUnitCommand | O | | |
| RerollShopCommand | O | | |
| BuyXPCommand | O | | |
| MoveUnitCommand | O | | |
| EquipItemCommand | O | **O** | |
| UnequipItemCommand | O | | |
| ReadyCommand | O | | |
| UseCommanderSkillCommand | | **O** | |
| SpectateCommand | | O | |
| SelectDraftCommand | | | O |

---

## 9. 현행 시스템 대비 개선

| 현행 | 개선 |
|------|------|
| FlowState 5종류 (Stage, Dungeon, PVP...) 코드 중복 | **단일 GameLoopSystem** + GameModeConfig + RoundConfig 데이터 |
| InGameMainFlowManager (11KB) + 모드별 FlowState | **GamePhase enum** + Signal 기반 전환 + 모드별 시스템 비활성화 |
| `Time.timeScale`로 속도 조절 | Quantum `DeltaTime` 기반 (네트워크 안전) |
| 배경 복귀 시 `deltaTime > 0.5f` 보정 | Quantum 프레임워크가 자동 처리 |
| 싱글턴 `InGameManager.Instance` | Quantum `Frame.Global` |

---

## 10. View 이벤트 처리 예시

```csharp
// Unity 측 - 페이즈 변경 이벤트 수신
public class GamePhaseView : MonoBehaviour
{
    private void OnEnable()
    {
        QuantumEvent.Subscribe<EventPhaseChanged>(this, OnPhaseChanged);
        QuantumEvent.Subscribe<EventCombatResult>(this, OnCombatResult);
        QuantumEvent.Subscribe<EventGameOver>(this, OnGameOver);
    }

    private void OnPhaseChanged(EventPhaseChanged e)
    {
        // 페이즈 UI 갱신
        _phaseLabel.text = e.Phase switch
        {
            GamePhase.Preparation => $"라운드 {e.Stage}-{e.Round} 준비",
            GamePhase.Combat => "전투 중",
            GamePhase.SharedDraft => "공유 드래프트",
            _ => ""
        };

        // 페이즈별 UI 표시/숨기기
        _shopPanel.SetActive(e.Phase == GamePhase.Preparation);
        _combatOverlay.SetActive(e.Phase == GamePhase.Combat);
    }
}
```
