# Feature: InGameBottomUI 터치/스크롤 개선

> 생성일: 2026-01-15
> 상태: 구현 완료 (테스트 대기)

---

## Feature Capsule

```
기능명: InGameBottomUI 터치/스크롤 개선
한줄 설명: 횡스크롤/종드래그 충돌 해결 및 캐릭터 반환 UX 개선
해결하는 문제: 터치 영역 충돌로 인한 조작 불편, 드롭 영역 제한
핵심 시나리오:
  1. 캐릭터 위에서 횡으로 드래그 → ScrollView 스크롤
  2. 캐릭터 위에서 종으로 드래그 → 보드에 배치
  3. 보드 캐릭터를 BottomUI로 드래그 → 하이라이트 표시 → 드롭 시 리스트로 반환 (CP순 정렬)
Scope: 횡스크롤 개선, 드롭 영역 확장, 반환 피드백, CP 정렬
Non-scope: 튜토리얼 로직 변경, 필터 로직 변경
성공 기준: 캐릭터 위 횡드래그로 스크롤 가능, BottomUI 어디든 드롭 시 반환
관련 도메인: [UI, InGame]
영향받는 기존 코드: InGameCharacterItem.cs, InGameTouchManager.cs, InGameBottomUI.cs
주요 리스크: IBeginDragHandler 이벤트 선점 → ScrollRect 이벤트 패스 처리 필요
```

---

## Decision Log

| ID | 항목 | 선택 | 근거 | 대안 |
|----|------|------|------|------|
| D-01 | 횡스크롤 처리 | ScrollRect로 이벤트 패스 | 캐릭터 위 시작해도 횡스크롤 필요 | 빈 영역에서만 스크롤 (기각) |
| D-02 | 드롭 영역 | ScrollRect 전체 | 넓은 드롭 영역으로 UX 개선 | ReturnObj 확장 (기각) |
| D-03 | 반환 피드백 | 영역 하이라이트 | 사용자에게 반환 가능 상태 명확히 표시 | 색상만 변경 (기각) |
| D-04 | 정렬 기준 | CP (GetAttrValueCP) | 전투력 순 정렬 요청 | Level/ID 순 (기존, 기각) |

---

## Feature Spec

### 상세 요구사항

- **REQ-F01**: 캐릭터 슬롯 위에서 드래그 시작 → 횡방향이면 ScrollRect 스크롤 동작
- **REQ-F02**: 캐릭터 슬롯 위에서 드래그 시작 → 종방향이면 캐릭터 배치 동작 (기존 유지)
- **REQ-F03**: 보드에서 캐릭터 드래그 중 BottomUI(ScrollRect) 영역 진입 시 하이라이트 피드백
- **REQ-F04**: 보드에서 캐릭터 드래그 종료 시 BottomUI 영역이면 리스트로 반환
- **REQ-F05**: 캐릭터 반환 시 CP(전투력) 기준 내림차순 정렬

### UI/UX 명세

**드래그 방향 판정**:
- DRAG_THRESHOLD: 10px 이동 후 판정
- |delta.x| > |delta.y| → 횡스크롤 모드 → ScrollRect에 이벤트 전달
- |delta.y| > |delta.x| → 종드래그 모드 → 캐릭터 배치 (기존)

**반환 피드백**:
- 드래그 중 ScrollRect 영역 진입 시: 하이라이트 (색상 또는 테두리)
- 드래그 중 ScrollRect 영역 이탈 시: 하이라이트 해제
- 드롭 성공 시: 캐릭터가 리스트에 CP순으로 삽입

### 엣지 케이스

| 케이스 | 처리 |
|--------|------|
| 대각선 드래그 | 10px 이후 더 큰 방향으로 결정, 이후 고정 |
| 빠른 스와이프 | 방향 판정 완료 전 끝나면 아무 동작 없음 |
| 드래그 중 영역 경계 왕복 | 진입/이탈마다 하이라이트 토글 |
| BattleItem 타입 캐릭터 | 반환 불가 (기존 로직 유지) |

---

## Implementation Plan

### 변경 범위

#### Scripts (Client)

| 경로 | 변경 유형 | 설명 |
|------|----------|------|
| `Scripts/UI/InGame/InGameCharacterItem.cs` | 수정 | 횡드래그 시 ScrollRect로 이벤트 패스 |
| `Scripts/UI/InGame/InGameBottomUI.cs` | 수정 | 하이라이트 피드백, CP정렬, 드롭 영역 체크 |
| `Scripts/UI/InGame/InGameMain.cs` | 수정 | IBottomScrollRectCheck 인터페이스 추가 |
| `Scripts/UI/InGame/InGameMainStateStage.cs` | 수정 | 인터페이스 구현 |
| `Scripts/UI/InGame/InGameMainStateTest.cs` | 수정 | 인터페이스 구현 |
| `Scripts/UI/InGame/InGameMainStatePrologue.cs` | 수정 | 인터페이스 구현 |
| `Scripts/UI/InGame/InGameMainStateTrialDungeon.cs` | 수정 | 인터페이스 구현 |
| `Scripts/InGame/Managers/InGameTouchManager.cs` | 수정 | 드롭 영역 판정을 BottomUI ScrollRect로 변경 |

### 매니저/시스템 의존성

| 매니저 | 용도 |
|--------|------|
| InGameTouchManager | 보드 드래그 처리, 드롭 영역 판정 |
| InGameBottomUI | 캐릭터 리스트 관리, 반환 처리 |
| EventSystem | UI Raycast로 ScrollRect 영역 체크 |

### 서버 API 변경

없음

### Spec 데이터 변경

없음

---

## TASKS

### 의존성 분석

| Layer | 작업 | 선행 조건 | 병렬 가능 |
|-------|------|----------|-----------|
| 3 | 횡스크롤 이벤트 패스 | - | O |
| 3 | 드롭 영역 판정 변경 | - | O |
| 3 | CP 정렬 변경 | - | O |
| 4 | 하이라이트 피드백 UI | Layer 3 | - |
| 5 | 통합 테스트 | Layer 4 | - |

---

## Layer 3: Logic (병렬 가능)

### [TASK-001] 횡스크롤 이벤트 패스 구현 ✅
- **의존**: 없음
- **Files**: `Assets/_Project/Scripts/UI/InGame/InGameCharacterItem.cs`
- **변경사항**:
  - [x] 1-1. OnDrag에서 횡방향 판정 시 ScrollRect로 이벤트 전달
  - [x] 1-2. ExecuteEvents.Execute로 ScrollRect에 드래그 이벤트 위임
  - [x] 1-3. OnBeginDrag에서 ScrollRect 참조 캐싱 (_cachedScrollRect)
  - [x] 1-4. OnEndDrag에서 횡스크롤 모드일 때 ScrollRect에 EndDrag 전달
- **완료 기준**: 캐릭터 위에서 횡드래그 시 스크롤 동작

### [TASK-002] 드롭 영역 판정 변경 ✅
- **의존**: 없음
- **Files**:
  - `Assets/_Project/Scripts/InGame/Managers/InGameTouchManager.cs`
  - `Assets/_Project/Scripts/UI/InGame/InGameBottomUI.cs`
  - `Assets/_Project/Scripts/UI/InGame/InGameMain.cs`
  - `Assets/_Project/Scripts/UI/InGame/InGameMainState*.cs`
- **변경사항**:
  - [x] 2-1. EndedMoveCharacter에서 기존 ReturnObj 태그 체크 대신 ScrollRect RectTransform 영역 체크
  - [x] 2-2. InGameBottomUI에 `IsPointInScrollRect(Vector2 screenPosition)` 메서드 추가
  - [x] 2-3. InGameMain에 IBottomScrollRectCheck 인터페이스 및 메서드 추가
  - [x] 2-4. 각 InGameMainState에서 인터페이스 구현
- **완료 기준**: BottomUI ScrollRect 영역 어디든 드롭 시 캐릭터 반환

### [TASK-003] CP 정렬 구현 ✅
- **의존**: 없음
- **Files**: `Assets/_Project/Scripts/UI/InGame/InGameBottomUI.cs`
- **변경사항**:
  - [x] 3-1. ReturnCharacter 메서드의 정렬 로직 변경 → `OrderByDescending(s => s.GetAttrValueCP())`
  - [x] 3-2. RefreshFilteredList의 정렬 로직도 동일하게 변경
  - [x] 3-3. InitData의 정렬 로직도 동일하게 변경
- **완료 기준**: 캐릭터 리스트가 CP 내림차순으로 정렬

---

## Layer 4: Presentation

### [TASK-004] 하이라이트 피드백 UI 구현 ✅
- **의존**: TASK-002 완료
- **Files**:
  - `Assets/_Project/Scripts/UI/InGame/InGameBottomUI.cs`
  - `Assets/_Project/Scripts/InGame/Managers/InGameTouchManager.cs`
- **변경사항**:
  - [x] 4-1. InGameBottomUI에 `SetDropHighlight(bool active)` 메서드 추가
  - [x] 4-2. 기존 _returnImage 활용하여 하이라이트 효과 (녹색 반투명)
  - [x] 4-3. InGameTouchManager.MoveCharacter에서 `UpdateDropHighlight` 호출
  - [x] 4-4. EndedMoveCharacter에서 하이라이트 해제
- **완료 기준**: 드래그 중 BottomUI 영역 진입 시 시각적 피드백 표시

---

## Layer 5: Integration

### [TASK-005] 통합 테스트
- **의존**: Layer 4 완료
- **Subtasks**:
  - [ ] 5-1. 횡스크롤 테스트: 캐릭터 위에서 좌우 드래그 → 스크롤 동작 확인
  - [ ] 5-2. 종드래그 테스트: 캐릭터 위에서 위로 드래그 → 보드 배치 동작 확인
  - [ ] 5-3. 드롭 반환 테스트: 보드 캐릭터를 BottomUI 여러 위치에 드롭 → 반환 확인
  - [ ] 5-4. 하이라이트 테스트: 드래그 중 영역 진입/이탈 시 피드백 확인
  - [ ] 5-5. CP 정렬 테스트: 반환된 캐릭터가 CP순으로 정렬되는지 확인
  - [ ] 5-6. 튜토리얼 테스트: 튜토리얼 배치 동작 영향 없는지 확인
  - [ ] 5-7. BattleItem 테스트: BattleItem 타입은 반환 불가 확인
- **완료 기준**: 모든 시나리오 통과

---

## Golden Rules 체크리스트

**필수 준수**:
- [x] UniTask 사용 (코루틴 X) - 해당 없음 (동기 로직)
- [x] State machine 패턴 사용 - 해당 없음
- [x] LINQ 지양 - CP 정렬 시 OrderByDescending 1회 사용 (허용 범위)

**파일 명명 규칙**:
- 기존 파일 수정만 있음, 신규 파일 없음

---

## 진행 로그

| 일시 | 상태 | 내용 |
|------|------|------|
| 2026-01-15 | 설계 완료 | Feature Spec, Implementation Plan, TASKS 작성 |
| 2026-01-15 | 구현 완료 | TASK-001~004 구현 완료, 테스트 대기 |

---

## 변경된 파일 목록

| 파일 | 주요 변경 |
|------|----------|
| `InGameCharacterItem.cs` | _cachedScrollRect 캐싱, 횡방향 시 ExecuteEvents로 ScrollRect에 이벤트 전달 |
| `InGameBottomUI.cs` | IsPointInScrollRect, SetDropHighlight, GetScrollRectTransform 추가, CP 정렬 |
| `InGameMain.cs` | IBottomScrollRectCheck 인터페이스, IsPointInBottomScrollRect, SetDropHighlight 추가 |
| `InGameMainStateStage.cs` | IBottomScrollRectCheck 구현 |
| `InGameMainStateTest.cs` | IBottomScrollRectCheck 구현 |
| `InGameMainStatePrologue.cs` | IBottomScrollRectCheck 구현 |
| `InGameMainStateTrialDungeon.cs` | IBottomScrollRectCheck 구현 |
| `InGameTouchManager.cs` | EndedMoveCharacter ScrollRect 영역 체크, UpdateDropHighlight 추가 |
