# 그리드 시스템 분석

> 관련 파일:
> - `InGame/Grid/InGameGrid.cs` (768줄)
> - `InGame/Grid/InGameTile.cs` (97줄)
> - `InGame/Grid/InGameStage.cs` (57줄)

---

## 1. 그리드 구조

### InGameGrid

2D 타일 배열 기반의 전투 필드. `int2(x, y)` 좌표 시스템 사용.

```
Y
3 ┌───┬───┬───┬───┬───┬───┬───┬───┐
  │   │   │   │   │   │   │   │   │
2 ├───┼───┼───┼───┼───┼───┼───┼───┤  ← Guardian, Striker (Y=2)
  │ P │ P │ P │ P │ E │ E │ E │ E │
1 ├───┼───┼───┼───┼───┼───┼───┼───┤  ← Esper, Oracle (Y=1)
  │ P │ P │ P │ P │ E │ E │ E │ E │
0 ├───┼───┼───┼───┼───┼───┼───┼───┤  ← Sharpshooter, Ghost (Y=0)
  │ P │ P │ P │ P │ E │ E │ E │ E │
  └───┴───┴───┴───┴───┴───┴───┴───┘
  0   1   2   3   4   5   6   7    X
     Player 영역        Enemy 영역
```

### 핵심 프로퍼티

```csharp
public class InGameGrid
{
    public int Width { get; }           // 가로 타일 수
    public int Height { get; }          // 세로 타일 수
    private InGameTile[] tiles;         // Width × Height 1차원 배열
}
```

### 좌표 → 인덱스 변환

```
index = y × Width + x
tile = tiles[index]
```

---

## 2. InGameTile (개별 타일)

### 프로퍼티

| 프로퍼티 | 타입 | 설명 |
|----------|------|------|
| `X`, `Y` | `int` | 그리드 좌표 |
| `Int2Index` | `int2` | (X, Y) 벡터 |
| `View` | `InGameTileView` | 시각적 표현 |
| `OccupiedCharacter` | `CharacterController` | 점유 캐릭터 (null = 빈 칸) |
| `EffectCodeContainer` | `EffectCodeContainer` | 타일 레벨 이펙트코드 |
| `G`, `H`, `cameFrom` | A* 관련 | 경로 탐색용 |

### 타일 점유/해제 이벤트

```csharp
tile.SetOccupied(character)
  ├─ OccupiedCharacter = character
  ├─ 타일 이펙트코드 (EffectCodeType.Tile) → OnTileCharacterEnter
  ├─ 게임 이펙트코드 (EffectCodeType.Game) → OnTileCharacterEnter
  └─ 팀 이펙트코드 (InGameManager.TeamEcc) → OnTileCharacterEnter

tile.SetUnoccupied()
  ├─ 타일 이펙트코드 → OnTileCharacterExit
  ├─ 게임 이펙트코드 → OnTileCharacterExit
  ├─ 팀 이펙트코드 → OnTileCharacterExit
  └─ OccupiedCharacter = null
```

### 타일 유효성 체크

```csharp
CheckValidTile(AllianceType allianceType, bool isCheckSameAllianceType)
  ├─ 비어있으면 → false
  ├─ Wall → false (항상)
  ├─ Neutral → true (양쪽 모두 타겟 가능)
  ├─ isCheckSameAllianceType = true → 같은 진영만 유효
  └─ isCheckSameAllianceType = false → 다른 진영만 유효
```

---

## 3. InGameStage (씬 구성)

```csharp
public class InGameStage : MonoBehaviour
{
    [SerializeField] private int2 _gridSize;        // 그리드 크기
    [SerializeField] private InGameTileView[] _tileViews; // 타일 뷰 배열

    void ChangeBoardColor(tileViews, color);                    // 즉시 색상 변경
    async UniTask GraduallyChangeBoardColor(color, duration);   // 점진적 색상 변경
}
```

---

## 4. 거리 계산

### Manhattan 거리

모든 거리 계산은 **Manhattan 거리** 기반:

```
distance = |x₁ - x₂| + |y₁ - y₂|
```

### 관련 메서드

| 메서드 | 설명 |
|--------|------|
| `GetManhattanDistance(tile1, tile2)` | 두 타일 간 Manhattan 거리 |
| `GetTileListByManhattanDistance(tile, distance)` | 정확히 distance만큼 떨어진 타일들 |
| `GetTileListByManhattanDistanceInRange(tile, range)` | range 이내의 모든 타일 |
| `IsInRange(attacker, target, range, shape)` | 범위 내 판정 |

---

## 5. 공격 범위 형태 (AttackRangeShape)

### Manhattan (기본)

```
거리 2 기준:
      ○
    ○ ○ ○
  ○ ○ ★ ○ ○
    ○ ○ ○
      ○
```

### Plus (+자)

```
GetTileListByShapePlus(tile, range):
      ○
      ○
  ○ ○ ★ ○ ○
      ○
      ○
```

### X (대각선)

```
GetTileListByShapeX(tile, range):
  ○       ○
    ○   ○
      ★
    ○   ○
  ○       ○
```

### Square (사각형)

```
GetTileListByShapeSquare(tile, range):
  ○ ○ ○ ○ ○
  ○ ○ ○ ○ ○
  ○ ○ ★ ○ ○
  ○ ○ ○ ○ ○
  ○ ○ ○ ○ ○
```

### Column / Row (열/행)

```
GetTileListByColumn(tile):    GetTileListByRow(tile):
      ○                       ○ ○ ○ ★ ○ ○ ○
      ○
      ★
      ○
      ○
```

### 캐릭터 방향 기반

```
GetTileListByCharacterDirection(tile, range):
  캐릭터가 바라보는 방향의 타일만 포함
  (Player → 오른쪽, Enemy → 왼쪽)
```

---

## 6. 경로 탐색

### BFS (너비 우선 탐색)

```csharp
GetNextMovableTile(startTile, targetTile)
  │
  ├─ BFS로 targetTile까지의 경로 탐색
  ├─ 점유된 타일은 통과 불가 (Wall 포함)
  ├─ 경로의 첫 번째 타일 반환 (한 칸씩 이동)
  └─ 경로 없으면 null
```

### 4방향 이동

```
이동 방향: 상(0,1) 하(0,-1) 좌(-1,0) 우(1,0)
대각선 이동 불가
```

### 특수 이동

| 메서드 | 용도 |
|--------|------|
| `GetTileForKnockBack(tile, direction, distance)` | 넉백 목표 타일 (장애물 체크) |
| `GetTileForAssassin(target, distance)` | 암살자 진입 위치 (타겟 뒤쪽) |
| `GetOptimalDistanceByAttackRange(attacker, target, range)` | 공격 사거리에 맞는 최적 위치 |

---

## 7. 캐릭터 위치 추천

### 직업별 Y좌표 배치

```csharp
GetRecommandedTile(ISpecCharacterInfo specCharacter)
```

| 직업 (PositionType) | 추천 Y좌표 | 위치 |
|---------------------|-----------|------|
| Guardian | 2 | 후방 (상단) |
| Striker | 2 | 후방 (상단) |
| Esper | 1 | 중앙 |
| Oracle | 1 | 중앙 |
| Sharpshooter | 0 | 전방 (하단) |
| Ghost | 0 | 전방 (하단) |

### 빈 타일 검색 전략

| 메서드 | 전략 |
|--------|------|
| `GetRandomEmptyTile(AllianceType?)` | 랜덤 빈 타일 (진영 필터 선택) |
| `GetPriorityEmptyTile()` | Y 높은 순 → X 높은 순 |
| `GetEmptyTilePreferringNone()` | AllianceType.None 타일 우선 |
| `FindNearestEmptyTile(tile)` | Manhattan 거리 증가하며 검색 |

---

## 8. 그리드와 다른 시스템의 연동

### 이펙트코드 연동

```
타일 점유 → SetOccupied()
  → 타일의 EffectCodeContainer에 등록된 Tile/Game 이펙트코드 트리거
  → 함정, 버프 영역 등의 효과 발동

예: 독 타일에 캐릭터 진입 → 독 디버프 적용
```

### InGameObjectManager 연동

```
InGameObjectManager가 InGameGrid를 사용하여:
  ├─ 캐릭터 스폰 시 타일 배정
  ├─ 타겟 검색 시 거리 계산
  ├─ 범위 공격 시 대상 타일 검색
  ├─ 이동 시 경로 탐색
  └─ 넉백/강제이동 시 목표 타일 계산
```

### CharacterController 연동

```
CharacterController.CurrentTile → 현재 점유 타일
CharacterController.Position → 논리적 위치 (타일 좌표 기반)
CharacterController.MoveTile(tile) → 타일 이동 (해제 → 점유)
```

---

## 9. 그리드 시스템 특성 요약

| 특성 | 설명 |
|------|------|
| 좌표 시스템 | 2D 정수 좌표 (`int2`) |
| 거리 계산 | Manhattan 거리 |
| 이동 방향 | 4방향 (상하좌우) |
| 경로 탐색 | BFS |
| 타일 점유 | 1타일 1캐릭터 (단일 점유) |
| 이벤트 시스템 | 점유/해제 시 이펙트코드 트리거 |
| 범위 형태 | Manhattan, Plus, X, Square, Column, Row, Direction |
| A* 지원 | G, H, cameFrom 필드 존재 (현재 BFS 주로 사용) |
