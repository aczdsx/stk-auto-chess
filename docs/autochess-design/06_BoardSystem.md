# 보드 & 유닛 관리 시스템 설계

> 각 플레이어의 보드, 벤치, 유닛 배치/이동, 별 승급(합성) 로직을 정의한다.
>
> **GameMode 적용**: 보드/벤치 시스템은 **모든 모드**에서 활성화된다.
> - 유닛 소스가 모드별로 다름: `OwnedCharacters`(ClassicBattle) vs `ShopPurchase`(PvECampaign, Competitive)
> - 별 승급(합성)은 `EnableCombine=true`인 모드에서만 활성화 (ClassicBattle에서는 비활성)

---

## 1. 보드 구조

### 1.1 보드 개요

```
각 플레이어는 독립된 보드를 소유한다.

보드 구성:
  - 전투 영역 (Battle Area): 유닛을 배치하여 전투에 참여시키는 그리드
  - 벤치 (Bench): 유닛을 보관하는 대기석
  - 보드는 Quantum의 ECS 컴포넌트로 관리 (결정론적)
```

### 1.2 그리드 사양

```
전투 영역: 7열(Column) × 4행(Row) = 28타일
벤치: 9슬롯 (1행)

        ── 상대 진영 ──
  ┌─┬─┬─┬─┬─┬─┬─┐
  │ │ │ │ │ │ │ │ Row 3 (최전방)
  ├─┼─┼─┼─┼─┼─┼─┤
  │ │ │ │ │ │ │ │ Row 2
  ├─┼─┼─┼─┼─┼─┼─┤
  │ │ │ │ │ │ │ │ Row 1
  ├─┼─┼─┼─┼─┼─┼─┤
  │ │ │ │ │ │ │ │ Row 0 (최후방)
  └─┴─┴─┴─┴─┴─┴─┘
        ── 자기 진영 ──

  ┌─┬─┬─┬─┬─┬─┬─┬─┬─┐
  │0│1│2│3│4│5│6│7│8│ Bench (9슬롯)
  └─┴─┴─┴─┴─┴─┴─┴─┴─┘

좌표계:
  - 전투 영역: (col, row) → col: 0~6, row: 0~3
  - 벤치: benchIndex: 0~8
  - Quantum FP 좌표 변환: worldPos = gridOrigin + (col * tileSize, row * tileSize)
```

### 1.3 전투 시 보드 미러링

```
PvP 전투 시 두 플레이어의 보드를 하나의 전투장으로 합침:

  ┌─┬─┬─┬─┬─┬─┬─┐
  │ │ │ │ │ │ │ │ Row 7  상대 최후방
  ├─┼─┼─┼─┼─┼─┼─┤
  │ │ │ │ │ │ │ │ Row 6
  ├─┼─┼─┼─┼─┼─┼─┤
  │ │ │ │ │ │ │ │ Row 5
  ├─┼─┼─┼─┼─┼─┼─┤
  │ │ │ │ │ │ │ │ Row 4  상대 최전방
  ├─┼─┼─┼─┼─┼─┼─┤ ── 중앙선 ──
  │ │ │ │ │ │ │ │ Row 3  자기 최전방
  ├─┼─┼─┼─┼─┼─┼─┤
  │ │ │ │ │ │ │ │ Row 2
  ├─┼─┼─┼─┼─┼─┼─┤
  │ │ │ │ │ │ │ │ Row 1
  ├─┼─┼─┼─┼─┼─┼─┤
  │ │ │ │ │ │ │ │ Row 0  자기 최후방
  └─┴─┴─┴─┴─┴─┴─┘

미러링 규칙:
  - 자기 유닛: 원래 좌표 그대로 (row 0~3)
  - 상대 유닛: 좌우 반전 + 상단 배치
    → 상대 (col, row) → 전투장 (6-col, 7-row)
  - 유닛 데이터는 복제(Clone)하여 전투 전용 엔티티 생성
  - 원본 보드의 유닛은 전투 중 변경되지 않음
```

---

## 2. 벤치 시스템

### 2.1 벤치 사양

```
슬롯 수: 9칸
용도: 유닛 보관 (전투에 참여하지 않음)

벤치 유닛은:
  - 전투에 참여하지 않음
  - 합성 대상에 포함됨 (보드 + 벤치 합산)
  - 시너지에 기여하지 않음 (보드 위 유닛만 시너지 계산)
```

### 2.2 벤치 가득 참 처리

```
벤치 + 보드가 모두 꽉 찬 상태에서:
  - 챔피언 구매 → 불가 (버튼 비활성화)
  - 캐러셀 챔피언 획득 → 특수 처리
    → 임시 슬롯에 보관 or 자동 판매 (TFT 방식: 임시 보관)
  - 합성 결과 유닛 발생 → 합성은 항상 성공 (슬롯 2개 비움, 결과 1개)
```

---

## 3. 유닛 배치 & 이동

### 3.1 Command 정의

```csharp
// 보드 ↔ 보드 이동 (위치 변경)
public class MoveUnitCommand : DeterministicCommand
{
    public EntityRef UnitEntity;
    public Byte TargetCol;       // 0~6
    public Byte TargetRow;       // 0~3

    public override void Serialize(BitStream stream)
    {
        stream.Serialize(ref UnitEntity);
        stream.Serialize(ref TargetCol);
        stream.Serialize(ref TargetRow);
    }
}

// 벤치 → 보드 배치
public class PlaceUnitCommand : DeterministicCommand
{
    public EntityRef UnitEntity;
    public Byte TargetCol;
    public Byte TargetRow;

    public override void Serialize(BitStream stream)
    {
        stream.Serialize(ref UnitEntity);
        stream.Serialize(ref TargetCol);
        stream.Serialize(ref TargetRow);
    }
}

// 보드 → 벤치 회수
public class WithdrawUnitCommand : DeterministicCommand
{
    public EntityRef UnitEntity;
    public Byte TargetBenchSlot;   // 0xFF = 자동 배정

    public override void Serialize(BitStream stream)
    {
        stream.Serialize(ref UnitEntity);
        stream.Serialize(ref TargetBenchSlot);
    }
}

// 유닛 스왑 (보드↔보드, 보드↔벤치, 벤치↔벤치)
public class SwapUnitsCommand : DeterministicCommand
{
    public EntityRef UnitA;
    public EntityRef UnitB;

    public override void Serialize(BitStream stream)
    {
        stream.Serialize(ref UnitA);
        stream.Serialize(ref UnitB);
    }
}
```

### 3.2 배치 규칙

```
PlaceUnit (벤치 → 보드):
  1. 현재 보드 위 유닛 수 확인
  2. 보드 유닛 수 < 플레이어 레벨인지 체크
  3. 대상 타일이 비었는지 체크
  4. 대상 타일에 유닛이 있으면 → 스왑 처리
  5. 벤치에서 제거, 보드에 배치
  6. 시너지 재계산 트리거

MoveUnit (보드 내 이동):
  1. 대상 타일이 비었으면 → 단순 이동
  2. 대상 타일에 유닛이 있으면 → 위치 스왑
  3. 시너지 변경 없음 (보드 내 이동이므로)

WithdrawUnit (보드 → 벤치):
  1. 벤치에 빈 슬롯 있는지 체크
  2. 보드에서 제거, 벤치에 배치
  3. 시너지 재계산 트리거

허용 조건:
  - Preparation 페이즈에서만 가능 (전투 중 배치 변경 불가)
  - 자기 보드의 유닛만 조작 가능
  - 같은 챔피언 복수 배치 허용 (예: 1★ 이즈리얼 2개 + 2★ 이즈리얼 1개 동시 배치 가능)
  - 합성은 3개가 모여야 발동되므로 그 전까지 동일 챔피언을 자유롭게 배치
```

### 3.3 보드 유닛 상한

```
플레이어 레벨 = 보드에 배치 가능한 최대 유닛 수

레벨 1 → 최대 1유닛
레벨 2 → 최대 2유닛
...
레벨 8 → 최대 8유닛

레벨 초과 시:
  - 추가 배치 불가
  - 레벨 다운은 없으므로 초과 상태는 발생하지 않음
  - 단, 합성으로 인한 일시적 초과는 허용
    → 합성 결과 유닛이 보드에 생성될 때 소재 3개 → 결과 1개이므로 감소
```

---

## 4. 유닛 합성 (별 승급)

### 4.1 합성 규칙

```
동일 챔피언 3개를 모으면 자동으로 별 승급:

1★ × 3 → 2★ × 1
2★ × 3 → 3★ × 1

합성 조건:
  - 동일 ChampionSpecId
  - 동일 별 등급 (Star Level)
  - 보드 + 벤치 전체에서 검색
  - 아이템은 결과 유닛에 이전 (아래 상세)

3★이 최대 등급. 3★ × 3 → 합성 불가.
```

### 4.2 자동 합성 트리거

```
자동 합성이 발생하는 시점:
  1. 챔피언 구매 시 (샵에서 구매 직후)
  2. 캐러셀에서 챔피언 획득 시
  3. 합성 결과로 2★ 3개가 모였을 때 (연쇄 합성)

합성 체크 순서:
  CheckCombine(player, championSpecId, starLevel):
    1. 해당 스펙 & 등급의 유닛 3개 이상 있는지 확인
    2. 3개 선택 (우선순위: 보드 유닛 > 벤치 유닛)
    3. 결과 유닛 생성 (starLevel + 1)
    4. 소재 3개 제거
    5. 결과 유닛 배치 (아래 규칙)
    6. 아이템 이전 처리
    7. 연쇄 합성 체크 → 재귀 호출
```

### 4.3 합성 결과 유닛 배치

```
결과 유닛의 위치 결정:

우선순위:
  1. 소재 중 보드에 있던 유닛이 있으면 → 그 위치에 결과 유닛 배치
     (여러 개면 가장 앞줄(row 높은) 유닛의 위치)
  2. 소재 모두 벤치에 있었으면 → 벤치에 결과 유닛 배치

이유:
  - 플레이어가 의도한 배치를 최대한 유지
  - 전투 중 보드 위 유닛이 합성되는 경우는 없음 (구매 시에만 발생)
```

### 4.4 합성 시 아이템 이전

```
소재 유닛이 장착한 아이템 처리:

규칙:
  1. 결과 유닛은 최대 3개 아이템 장착 가능
  2. 소재 유닛들의 아이템을 결과 유닛에 순서대로 이전
  3. 결과 유닛의 아이템 슬롯이 가득 차면
     → 남은 아이템은 보드/벤치 빈 자리에 임시 유닛 없이 반환
     → 또는 아이템 전용 큐에 보관 (플레이어가 수동 배치)

이전 순서:
  보드 유닛의 아이템 > 벤치 유닛의 아이템
  같은 위치라면 엔티티 생성 순서대로
```

### 4.5 합성 시 스탯 변화

```
별 승급에 따른 스탯 배율:

1★: 기본 스탯 (BaseHP, BaseAttack, ...)
2★: 기본 스탯 × Star2Multiplier (1.8x)
3★: 기본 스탯 × Star3Multiplier (3.2x)

적용 스탯:
  - HP (최대 HP)
  - 공격력 (Attack)
  - 방어력 (Armor)
  - 마법 저항 (MagicResist)

미적용:
  - 공격 속도 (고정)
  - 공격 사거리 (고정)
  - 이동 속도 (고정)
  - 마나 (고정)

배율은 ChampionSpecAsset에 정의되어 밸런스 조정 가능.
```

---

## 5. Quantum 컴포넌트 설계

### 5.1 보드 컴포넌트

```qtn
component PlayerBoard {
    // 전투 영역: 7×4 = 28타일
    // 각 타일에 유닛 엔티티 참조 (None이면 빈 타일)
    array<EntityRef>[28] Tiles;

    // 벤치: 9슬롯
    array<EntityRef>[9] Bench;
}
```

### 5.2 유닛 컴포넌트

```qtn
component UnitData {
    // 기본 정보
    Int32 ChampionSpecId;
    Byte StarLevel;              // 1, 2, 3
    player_ref Owner;

    // 위치
    UnitLocation Location;       // Board 또는 Bench
    Byte BoardCol;               // 0~6 (보드 위일 때)
    Byte BoardRow;               // 0~3 (보드 위일 때)
    Byte BenchIndex;             // 0~8 (벤치 위일 때)

    // 전투 스탯 (별 등급 적용 후)
    FP MaxHP;
    FP CurrentHP;
    FP Attack;
    FP Armor;
    FP MagicResist;
    FP AttackSpeed;
    FP AttackRange;
    FP MoveSpeed;
    FP MaxMana;
    FP CurrentMana;

    // 아이템 슬롯 (최대 3개)
    array<EntityRef>[3] Items;
}

enum UnitLocation {
    None,
    Board,
    Bench
}
```

### 5.3 전투 전용 유닛 컴포넌트

```qtn
// 전투 시뮬레이션에서만 사용하는 컴포넌트
// 전투 시작 시 UnitData를 기반으로 복제 생성
component CombatUnit {
    EntityRef SourceUnit;        // 원본 UnitData 엔티티
    player_ref Owner;
    Int32 ChampionSpecId;
    Byte StarLevel;

    // 전투 위치 (그리드 좌표 - 칸 단위 이동)
    Byte GridCol;                // 0~6
    Byte GridRow;                // 0~7 (전투장 8행)
    FP MoveCooldown;             // 다음 이동까지 남은 시간
    FP MoveInterval;             // 이동 간격 (1/MoveSpeed 초)

    // 특수 이동
    Boolean HasBacklineJump;     // 전투 시작 시 후방 점프 능력 (암살자 등)
    Boolean BacklineJumpDone;    // 점프 완료 여부

    // 전투 스탯 (아이템/시너지 버프 적용 후)
    FP MaxHP;
    FP CurrentHP;
    FP Attack;
    FP Armor;
    FP MagicResist;
    FP AttackSpeed;
    Byte AttackRange;            // 사거리 (칸 단위, 1=인접, 2~4=원거리)
    FP MoveSpeed;                // 이동 속도 (칸/초)
    FP MaxMana;
    FP CurrentMana;

    // 전투 상태
    CombatState State;
    EntityRef Target;
    FP AttackCooldown;

    // 시너지/아이템 효과 참조
    Int32 AppliedSynergyFlags;   // 비트 플래그
}

enum CombatState {
    Idle,
    Moving,
    Attacking,
    CastingSkill,
    CrowdControlled,
    Dead
}

// 전투장 그리드 점유 상태 (매치당 1개)
component CombatGrid {
    // 7×8 = 56칸, 각 칸에 유닛 엔티티 (None이면 빈 칸)
    array<EntityRef>[56] Tiles;
    Byte MatchIndex;
}
```

### 5.4 보드 설정 (Asset)

```csharp
public class BoardConfigAsset : AssetObject
{
    // 전투 영역 크기
    public int Columns = 7;
    public int Rows = 4;

    // 벤치 크기
    public int BenchSlots = 9;

    // 타일 크기 (Quantum FP 좌표)
    public FP TileSize = FP._1;

    // 보드 원점 (FP 좌표)
    public FPVector2 BoardOrigin;

    // 이동 옵션
    public bool AllowDiagonalMovement = true;  // false: 상하좌우 4방향만, true: 대각선 포함 8방향

    // 최대 아이템 슬롯
    public int MaxItemSlots = 3;

    // 별 승급 배율 (기본값, ChampionSpec에서 오버라이드 가능)
    public FP DefaultStar2Multiplier = FP._1 + FP._0_50 + FP._0_33; // ≈1.8
    public FP DefaultStar3Multiplier = FP._3 + FP._0_20;             // ≈3.2
}
```

---

## 6. BoardSystem 설계

### 6.1 시스템 구조

```csharp
public unsafe class BoardSystem : SystemMainThread
{
    public override void Update(Frame f)
    {
        // Preparation 페이즈에서만 배치 커맨드 처리
        var global = f.Global;
        if (global->CurrentPhase != GamePhase.Preparation) return;

        for (int p = 0; p < f.PlayerCount; p++)
        {
            var player = (PlayerRef)p;

            foreach (var cmd in f.GetPlayerCommands<PlaceUnitCommand>(player))
                ProcessPlace(f, player, cmd);

            foreach (var cmd in f.GetPlayerCommands<WithdrawUnitCommand>(player))
                ProcessWithdraw(f, player, cmd);

            foreach (var cmd in f.GetPlayerCommands<MoveUnitCommand>(player))
                ProcessMove(f, player, cmd);

            foreach (var cmd in f.GetPlayerCommands<SwapUnitsCommand>(player))
                ProcessSwap(f, player, cmd);
        }
    }
}
```

### 6.2 배치 처리

```csharp
private void ProcessPlace(Frame f, PlayerRef player, PlaceUnitCommand cmd)
{
    var unit = f.Get<UnitData>(cmd.UnitEntity);
    if (unit->Owner != player) return;
    if (unit->Location != UnitLocation.Bench) return;

    var board = GetPlayerBoard(f, player);
    var economy = GetPlayerEconomy(f, player);

    // 보드 유닛 수 체크 (레벨 상한)
    int boardUnitCount = CountBoardUnits(f, board);
    int tileIndex = cmd.TargetCol + cmd.TargetRow * BoardColumns;

    EntityRef existing = board->Tiles[tileIndex];

    if (existing != EntityRef.None)
    {
        // 대상 타일에 유닛이 있으면 → 스왑
        SwapBenchAndBoard(f, unit, cmd.UnitEntity,
                          existing, board, tileIndex, unit->BenchIndex);
    }
    else
    {
        // 빈 타일 & 레벨 상한 미초과
        if (boardUnitCount >= economy->Level) return;

        // 벤치에서 제거
        board->Bench[unit->BenchIndex] = EntityRef.None;

        // 보드에 배치
        board->Tiles[tileIndex] = cmd.UnitEntity;
        unit->Location = UnitLocation.Board;
        unit->BoardCol = cmd.TargetCol;
        unit->BoardRow = cmd.TargetRow;
    }

    // 시너지 재계산 시그널
    f.Signals.OnBoardChanged(player);
}
```

---

## 7. UnitCombineSystem 설계

### 7.1 자동 합성 시스템

```csharp
public unsafe class UnitCombineSystem : SystemSignalsOnly,
    ISignalOnUnitAcquired
{
    // 유닛 획득 시 합성 체크
    public void OnUnitAcquired(Frame f, PlayerRef player,
                               EntityRef unit, Int32 championSpecId)
    {
        CheckAndCombine(f, player, championSpecId, starLevel: 1);
    }

    private void CheckAndCombine(Frame f, PlayerRef player,
                                  int specId, int starLevel)
    {
        if (starLevel >= 3) return; // 3★은 최대

        var board = GetPlayerBoard(f, player);
        var candidates = CollectCandidates(f, board, specId, starLevel);

        if (candidates.Count < 3) return;

        // 3개 선택 (보드 우선)
        var selected = SelectThree(candidates);

        // 결과 유닛 생성
        int newStarLevel = starLevel + 1;
        var resultEntity = CreateCombinedUnit(f, player, specId, newStarLevel);

        // 결과 유닛 위치 결정
        PlaceCombinedUnit(f, board, player, resultEntity, selected);

        // 아이템 이전
        TransferItems(f, resultEntity, selected);

        // 소재 제거
        foreach (var src in selected)
        {
            RemoveFromBoard(f, board, src);
            f.Destroy(src);
        }

        // View 이벤트
        f.Events.UnitCombined(player, resultEntity, specId, newStarLevel);

        // 시너지 재계산
        f.Signals.OnBoardChanged(player);

        // 연쇄 합성 체크
        CheckAndCombine(f, player, specId, newStarLevel);
    }
}
```

### 7.2 합성 후보 수집

```csharp
private List<CombineCandidate> CollectCandidates(
    Frame f, PlayerBoard* board, int specId, int starLevel)
{
    var result = new List<CombineCandidate>();

    // 보드 유닛 검색
    for (int i = 0; i < 28; i++)
    {
        if (board->Tiles[i] == EntityRef.None) continue;
        var unit = f.Get<UnitData>(board->Tiles[i]);
        if (unit->ChampionSpecId == specId && unit->StarLevel == starLevel)
        {
            result.Add(new CombineCandidate {
                Entity = board->Tiles[i],
                IsOnBoard = true,
                BoardIndex = i,
                Row = i / 7  // 앞줄 우선 정렬용
            });
        }
    }

    // 벤치 유닛 검색
    for (int i = 0; i < 9; i++)
    {
        if (board->Bench[i] == EntityRef.None) continue;
        var unit = f.Get<UnitData>(board->Bench[i]);
        if (unit->ChampionSpecId == specId && unit->StarLevel == starLevel)
        {
            result.Add(new CombineCandidate {
                Entity = board->Bench[i],
                IsOnBoard = false,
                BenchIndex = i,
                Row = -1
            });
        }
    }

    return result;
}
```

---

## 8. 전투 준비 (보드 → 전투장)

### 8.1 CombatSetup 과정

```csharp
// CombatSystem에서 전투 시작 시 호출
public void SetupCombatBoard(Frame f, PlayerRef playerA, PlayerRef playerB,
                              int matchIndex)
{
    // 전투 그리드 생성 (7×8 = 56칸)
    var gridEntity = f.Create();
    var grid = f.Add<CombatGrid>(gridEntity);
    grid->MatchIndex = (byte)matchIndex;
    // 모든 Tiles는 EntityRef.None으로 초기화됨

    var boardA = GetPlayerBoard(f, playerA);
    var boardB = GetPlayerBoard(f, playerB);

    // 각 플레이어의 보드 유닛을 전투 유닛으로 복제 → 그리드에 배치
    CloneUnitsForCombat(f, boardA, playerA, grid, isMirrored: false);
    CloneUnitsForCombat(f, boardB, playerB, grid, isMirrored: true);
}

private void CloneUnitsForCombat(Frame f, PlayerBoard* board,
                                  PlayerRef player, CombatGrid* grid,
                                  bool isMirrored)
{
    for (int i = 0; i < 28; i++)
    {
        if (board->Tiles[i] == EntityRef.None) continue;

        var source = f.Get<UnitData>(board->Tiles[i]);
        int col = i % 7;
        int row = i / 7;

        // 미러링 적용 (상대 유닛은 반대편에 배치)
        if (isMirrored)
        {
            col = 6 - col;
            row = 7 - row;
        }

        // CombatUnit 엔티티 생성
        var combatEntity = f.Create();
        var combat = f.Add<CombatUnit>(combatEntity);

        combat->SourceUnit = board->Tiles[i];
        combat->Owner = player;
        combat->ChampionSpecId = source->ChampionSpecId;
        combat->StarLevel = source->StarLevel;

        // 그리드 좌표 설정
        combat->GridCol = (byte)col;
        combat->GridRow = (byte)row;
        combat->MoveInterval = FP._1 / source->MoveSpeed; // 이동 간격
        combat->MoveCooldown = FP._0;

        // 특수 이동 능력 (ChampionSpec에서 결정)
        var spec = f.FindAsset<ChampionSpecAsset>(source->ChampionSpecId);
        combat->HasBacklineJump = spec.HasBacklineJump;
        combat->BacklineJumpDone = false;

        // 스탯 복사 (시너지/아이템 버프는 별도 적용)
        combat->MaxHP = source->MaxHP;
        combat->CurrentHP = source->MaxHP;
        combat->Attack = source->Attack;
        combat->Armor = source->Armor;
        combat->MagicResist = source->MagicResist;
        combat->AttackSpeed = source->AttackSpeed;
        combat->AttackRange = (byte)FPMath.RoundToInt(source->AttackRange);
        combat->MoveSpeed = source->MoveSpeed;
        combat->MaxMana = source->MaxMana;
        combat->CurrentMana = source->CurrentMana;

        combat->State = CombatState.Idle;
        combat->Target = EntityRef.None;

        // 그리드 점유 등록
        int gridIndex = col + row * 7;
        grid->Tiles[gridIndex] = combatEntity;
    }
}
```

### 8.2 전투 종료 후 정리

```
전투 종료 시:
  1. 모든 CombatUnit 엔티티 삭제
  2. CombatGrid 엔티티 삭제 (점유 배열 해제)
  3. 원본 보드의 UnitData는 변경 없음
  4. 전투 결과만 기록 (승패, 잔여 유닛 수, 피해량)

→ 보드 배치는 전투 전후로 항상 보존됨
→ 플레이어는 다음 Preparation에서 배치를 조정
```

---

## 9. View 이벤트

```qtn
// 유닛 배치 변경
event UnitPlaced {
    player_ref Player;
    entity_ref Unit;
    Int32 ChampionSpecId;
    Byte Col;
    Byte Row;
}

// 유닛 벤치로 이동
event UnitWithdrawn {
    player_ref Player;
    entity_ref Unit;
    Byte BenchSlot;
}

// 유닛 스왑
event UnitsSwapped {
    player_ref Player;
    entity_ref UnitA;
    entity_ref UnitB;
}

// 유닛 합성 (별 승급)
event UnitCombined {
    player_ref Player;
    entity_ref ResultUnit;
    Int32 ChampionSpecId;
    Byte NewStarLevel;
}

// 보드 가득 참 (배치 불가 알림)
event BoardFull {
    player_ref Player;
    Byte CurrentCount;
    Byte MaxCount;
}
```

---

## 10. 좌표 변환 유틸리티

```csharp
public static class BoardHelper
{
    public const int Columns = 7;
    public const int Rows = 4;
    public const int CombatRows = 8;
    public const int BenchSlots = 9;
    public const int TotalTiles = Columns * Rows; // 28

    // 그리드 → 배열 인덱스
    public static int ToIndex(int col, int row) => col + row * Columns;

    // 배열 인덱스 → 그리드
    public static (int col, int row) FromIndex(int index)
        => (index % Columns, index / Columns);

    // 그리드 → 월드 좌표 (FP)
    public static FPVector2 GridToWorld(int col, int row, FP tileSize, FPVector2 origin)
        => origin + new FPVector2(col * tileSize, row * tileSize);

    // 월드 좌표 → 그리드 (반올림)
    public static (int col, int row) WorldToGrid(FPVector2 pos, FP tileSize, FPVector2 origin)
    {
        var local = pos - origin;
        return (
            FPMath.RoundToInt(local.X / tileSize),
            FPMath.RoundToInt(local.Y / tileSize)
        );
    }

    // 미러링 (상대 유닛 좌표 변환)
    public static (int col, int row) Mirror(int col, int row)
        => (Columns - 1 - col, CombatRows - 1 - row);

    // Manhattan 거리 (타겟팅, 사거리 계산)
    public static int ManhattanDistance(int col1, int row1, int col2, int row2)
        => Math.Abs(col1 - col2) + Math.Abs(row1 - row2);
}
```

---

## 11. View 레이어 (Unity)

### 11.1 드래그 & 드롭

```
View 레이어에서 처리 (Quantum 시뮬레이션 외부):

DragDropHandler:
  1. 유닛 터치/클릭 → 드래그 시작
  2. 유닛 비주얼을 손가락/마우스 따라 이동 (Presentation Only)
  3. 대상 타일 위에서 놓으면 → 적절한 Command 전송
     - 벤치 → 보드 타일: PlaceUnitCommand
     - 보드 → 다른 보드 타일: MoveUnitCommand
     - 보드 → 벤치 영역: WithdrawUnitCommand
     - 유닛 위에 놓기: SwapUnitsCommand
  4. 유효하지 않은 위치에 놓으면 → 원래 위치로 스냅백

View 동기화:
  - Command 전송 후 Quantum 시뮬레이션이 처리
  - 다음 Verified Frame에서 실제 위치 반영
  - 예측(Prediction) 적용 가능: 즉시 비주얼 이동 후 Rollback 시 되돌림
```

### 11.2 보드 시각화

```
BoardView:
  - 7×4 타일 그리드 시각화
  - 배치 가능 영역 하이라이트
  - 드래그 중 유효/무효 타일 표시
  - 상대 보드 미니맵 (간략화된 유닛 위치)

UnitView:
  - Spine 애니메이션 (Idle, 합성 이펙트)
  - 별 등급 표시 (1★, 2★, 3★)
  - 장착 아이템 아이콘
  - HP바 (전투 중)
  - 시너지 하이라이트 (해당 유닛의 시너지 활성화 시)
```

---

## 12. 4인 게임 보드 밸런스

### 설계 가이드

```
TFT(8인) 대비 조정:

그리드 크기:
  - TFT: 7×4 = 28타일 → 동일 유지
  - 4인이라 유닛 수가 적으므로 동일 크기에서 밀도가 낮아짐
  - 전투가 더 개방적 → 포지셔닝 전략에 여유

벤치 크기:
  - TFT: 9슬롯 → 동일 유지
  - 4인 게임에서 챔피언 종류가 적으므로 합성 확률 상승
  - 벤치 관리의 중요도 유지

최대 유닛 수:
  - TFT: 최대 10 (레벨 9 + 특성 보너스)
  - 4인: 최대 8 (레벨 8)
  - 보드 밀도: 8/28 = 28.6% (TFT: 10/28 = 35.7%)
  - 적절한 공간 활용과 포지셔닝 여지
```
