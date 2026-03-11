# 멀티보드 뷰 전환 설계

## 요약

InGame_New 경쟁 모드에서 여러 보드(매치)의 전투를 전환하며 볼 수 있도록 뷰 레이어를 설계한다. 시뮬레이션은 이미 멀티보드(`CombatMatchState[]`)를 지원하므로, 뷰 전환 로직만 추가한다.

## 결정 사항

| 항목 | 결정 |
|---|---|
| 전환 방식 | 즉시 전환 (스냅) + 짧은 페이드 트랜지션 (0.3초) |
| 이전 보드 상태 보존 | 불필요 — 전환 시점의 시뮬레이션 스냅샷으로 새로 스폰 |
| 진행 중 투사체 | 무시 — 전환 후 새 이벤트만 처리 |
| 전환 가능 페이즈 | 전투 + 준비 모두 가능 |
| UI | 탭 버튼 또는 플레이어 초상화 클릭 |

## 접근 방식

**싱글 보드 + 데이터 소스 교체** — 씬에 `BoardGridView` 1개만 유지하고, 전환 시 UnitView를 풀 회수 후 새 보드 데이터로 재스폰. 기존 `UnitViewManager.SetActiveBoard()` 패턴을 확장.

### 기각된 대안

- **멀티 보드 프리로드**: 보드별 UnitView 세트 유지 → 메모리 오버헤드 과다 (Spine 리소스 등)
- **스냅샷 캐시**: 전환 시 뷰 상태 캐싱 → 보존 불필요 방침과 불일치, 과잉 설계

## 전체 흐름

```
[UI 탭/초상화 클릭]
  → BoardSwitchRequest(targetPlayerIndex)
  → ViewBridge.SwitchBoard(targetPlayerIndex)
    → _isSwitching = true                   // 틱 처리 가드 (Sync/ProcessEvents 차단)
    → 페이드아웃 (0.15s)
    → UnitViewManager.ClearAllViews()       // 풀 회수
    → CombatViewManager.ClearEffects()      // VFX + pending 큐 정리
    → TargetLineManager.ClearAll()          // 타겟 라인 정리
    → BoardInputHandler.CancelDrag()        // 드래그 상태 취소
    → _activeBoardIndex = targetPlayerIndex
    → _isSwitching = false                  // 가드 해제
    → 페이드인 (0.15s)
    → 다음 틱에서 Sync 메서드가 새 보드 데이터로 UnitView 재스폰
```

> **트랜지션 중 틱 가드**: `HandleTick()`에서 `_isSwitching == true`이면 `ProcessEvents()`와 `SyncCombatViews()`/`SyncBoardUnits()` 호출을 스킵한다. 이는 페이드아웃 중 이전 보드 유닛이 재스폰되거나, 이전 보드 이벤트로 VFX가 재생되는 것을 방지한다.

## 컴포넌트별 역할

### AutoChessViewBridge — 전환 오케스트레이션

- `SwitchBoard(int targetPlayerIndex)` 메서드 추가
- 현재 페이즈에 따라 적절한 매치/보드를 찾아 `_activeBoardIndex` 설정
  - 전투 페이즈: `targetPlayerIndex`가 참여 중인 `CombatMatchState`를 찾아 동기화
  - 준비 페이즈: `targetPlayerIndex`의 `BoardSlots[]` 데이터를 동기화
- 트랜지션 중 입력 차단
- `_isSwitching` 가드 플래그: `HandleTick()`에서 Sync/ProcessEvents 차단
- **`SyncCombatViews()` 변경**: 현재 모든 매치를 순회하며 스폰하는 로직을 `_activeBoardIndex`에 해당하는 단일 매치만 처리하도록 수정
- **이벤트 필터링**: `ProcessEvents()`에서 `SimEvent`의 매치 귀속을 확인하여 비활성 보드 이벤트 스킵

### UnitViewManager — 뷰 생명주기

- `ClearAllViews()` — 모든 활성 UnitView를 풀로 회수, `_boardUnitViews`/`_combatUnitViews` 딕셔너리 클리어
- `SyncBoardUnits(world)` / `SyncCombatUnits(matchState, boardIndex)` — 기존 로직 유지, `_activeBoardIndex` 기준으로 표시할 유닛 필터링

### CombatViewManager — VFX 정리

- `ClearEffects()` — 활성 프로젝타일, 데미지 텍스트, 타일 이펙트 즉시 회수
- **pending 큐도 정리**: `_pendingProjectiles.Clear()`, `_pendingMeleeAttacks.Clear()`, `_pendingMeleeTargetIds.Clear()`
- 전환 후 새 보드의 이벤트만 처리 (과거 틱 이벤트 무시)

### BoardTransitionEffect (신규)

- 페이드아웃/인 연출 담당
- `PlayTransition(Action onMidpoint)` — 페이드아웃 → `onMidpoint` 콜백(뷰 교체) → 페이드인
- CanvasGroup alpha 또는 카메라 효과, LitMotion 사용

## 입력 제어

- 자기 보드(`_activeBoardIndex == _localPlayerIndex`): 준비 페이즈에서 모든 조작 가능
- 다른 플레이어 보드: 조회만 가능, `ViewBridge.SendCommand()` 진입점에서 명령 무시
- 트랜지션 중(0.3초): 입력 차단

## 엣지 케이스

| 상황 | 처리 |
|---|---|
| 다른 보드 구경 중 페이즈 전환 (준비→전투) | 자동으로 내 보드로 복귀 |
| 전투 중 보고 있던 플레이어가 패배/탈락 | 내 보드로 자동 복귀 |
| 전환 트랜지션 중 다시 전환 요청 | 무시 (트랜지션 완료까지 차단) |
| 진행 중 투사체 | 무시, 전환 후 새 이벤트만 처리 |
| 준비 페이즈 타 보드 구경 시 PvE 적 프리뷰 | 미표시 — 타 플레이어 보드 구경 시 PvE 프리뷰 영역은 비워둠 |

## 메모리/성능

- UnitView 풀 크기 64개 유지 (한 보드 최대 32유닛)
- Spine 캐릭터 비주얼은 Addressables 캐시에서 즉시 반환 (기로드 캐릭터), 미로드만 비동기 로드
- VFX 풀은 보드 간 공유 (CombatViewManager 1개)

## 시뮬레이션 레이어 변경

### SimEvent에 MatchIndex 태깅

현재 `SimEvent`에는 매치 귀속 정보가 없어 뷰에서 보드별 필터링이 불가능하다. 전투 이벤트(`UnitAttacked`, `ProjectileSpawned`, `UnitDamaged` 등) 발행 시 `MatchIndex` 필드를 설정하도록 변경한다.

```
SimEvent {
    ...
    byte MatchIndex;  // 추가: 이 이벤트가 발생한 매치 인덱스
}
```

이벤트 발행 시점(`CombatAISystem.Tick()` 등)에서 현재 처리 중인 `matchIndex`를 `SimEvent.MatchIndex`에 기록한다.

## 수정 대상 파일

| 파일 | 변경 |
|---|---|
| `Simulation/Data/SimEvent.cs` (또는 해당 구조체) | `MatchIndex` 필드 추가 |
| `Simulation/Combat/CombatAISystem.cs` | 이벤트 발행 시 `MatchIndex` 설정 |
| `View/AutoChessViewBridge.cs` | `SwitchBoard()` 추가, `_isSwitching` 가드, `SyncCombatViews()` 단일 매치 처리, `ProcessEvents()` 매치 필터링, 페이즈 전환 시 자동 복귀 |
| `View/Unit/UnitViewManager.cs` | `ClearAllViews()` 추가, 준비 페이즈 타 플레이어 보드 동기화 |
| `View/Combat/CombatViewManager.cs` | `ClearEffects()` 추가 (pending 큐 포함) |
| `View/Board/BoardTransitionEffect.cs` (신규) | 페이드 트랜지션 컴포넌트 |
| (신규 또는 기존 UI 확장) | 보드 전환 탭/초상화 UI |
