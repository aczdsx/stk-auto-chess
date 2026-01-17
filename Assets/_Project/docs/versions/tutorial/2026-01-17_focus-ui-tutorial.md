# Tutorial Feature: FOCUS_UI

> 생성일: 2026-01-17
> 상태: 구현 완료
> 유형: 새 ActionType 전략 추가

---

## Feature Capsule

| 항목 | 내용 |
|------|------|
| 기능명 | FOCUS_UI |
| 한줄 설명 | UI RectTransform 기준으로 MaskHole을 표시하여 UI 요소를 포커싱하는 튜토리얼 액션 |
| 유형 | 새 ActionType 전략 추가 |
| 사용 시나리오 | tutorial_action_key로 지정된 UI 타겟에 마스크 홀 표시 |
| 타겟 오브젝트 | UI (RectTransform) |
| 완료 조건 | 딤드 클릭으로 다음 진행 |
| Scope | TutorialActionFocusUI 전략 클래스 생성, TutorialController 등록 |
| Non-scope | FOCUS_OBJECT 수정 (기존 유지) |
| 성공 기준 | FOCUS_UI ActionType으로 UI 요소에 마스크 홀 표시되고 딤드 클릭 시 진행 |
| 관련 전략 | TutorialActionFocusObject (참조), TutorialActionNone (패턴 유사) |

---

## Decision Log

| ID | 항목 | 선택 | 근거 | 대안 |
|----|------|------|------|------|
| D-01 | 전략 분리 | FOCUS_UI 별도 생성 | UI/3D 타겟 명확히 구분 | FOCUS_OBJECT 확장 (복잡도 증가) |
| D-02 | 좌표 변환 | GetNormalizedPosition() | RectTransform 기반 UI 좌표 | CalculateWorldPositionUV() (3D용) |
| D-03 | 완료 조건 | 딤드 클릭 | TutorialActionNone과 동일한 UX | 버튼 클릭 (불필요한 복잡도) |

---

## Feature Spec

### 상세 요구사항

- **REQ-T01**: tutorial_action_key로 TutorialTargetRegistry에서 UI 타겟 검색
- **REQ-T02**: 타겟 RectTransform 기준으로 MaskHole 위치 계산
- **REQ-T03**: 딤드 클릭 시 다음 튜토리얼로 진행
- **REQ-T04**: 화살표 비활성화 (FOCUS_OBJECT와 동일)

### 전략 설계

```csharp
public class TutorialActionFocusUI : ITutorialActionStrategy
{
    // OnShow: 타겟 검색 → TargetUnmaskObj 설정
    // OnNext: 다음 버튼 활성화
    // CanProceedOnDimmedClick: true (딤드 클릭 허용)
    // OnClear: 정리 (TargetUnmaskObj = null)
}
```

### Spec 데이터 설계

| 필드 | 값 예시 | 설명 |
|------|---------|------|
| tutorial_action_type | FOCUS_UI (10) | 액션 타입 |
| tutorial_action_key | "BtnStart" | TutorialTarget ID |
| hole_radius | 0.1 | 마스크 홀 반지름 (정규화) |

### FOCUS_UI vs FOCUS_OBJECT 차이점

| 항목 | FOCUS_UI | FOCUS_OBJECT |
|------|----------|--------------|
| 타겟 | UI (RectTransform) | 3D 오브젝트 (Transform) |
| 좌표 변환 | GetNormalizedPosition() | CalculateWorldPositionUV() |
| Enum 값 | 10 | 5 |

---

## Implementation Plan

### 변경 범위

#### Scripts (전략 패턴)

| 경로 | 변경 유형 | 설명 |
|------|----------|------|
| `Scripts/Common/Tutorial/Strategies/TutorialActionFocusUI.cs` | **추가** | 새 전략 클래스 |
| `Scripts/Common/TutorialController.cs` | 수정 | _strategies 등록 + CalculateTargetUvPosition() 조건 추가 |

#### Enum (변경 없음)

| 항목 | 상태 |
|------|------|
| TutorialActionType.FOCUS_UI | ✅ 이미 존재 (값 10) |

---

## TASKS

### 의존성 레이어 구조

```
Layer 1: 전략 클래스 생성
    ↓
Layer 2: TutorialController 수정 (등록 + 좌표 변환)
    ↓
Layer 3: 테스트
```

---

### Layer 1: 전략 클래스 생성

#### [TASK-001] TutorialActionFocusUI.cs 생성
- **의존**: 없음
- **Files**: `Assets/_Project/Scripts/Common/Tutorial/Strategies/TutorialActionFocusUI.cs`
- **작업**:
  - [x] ITutorialActionStrategy 구현
  - [x] OnShow: TutorialTargetRegistry로 타겟 검색, TargetUnmaskObj 설정
  - [x] OnNext: NextObj 활성화
  - [x] CanProceedOnDimmedClick: return true
  - [x] OnClear: NextObj 비활성화, TargetUnmaskObj = null
- **완료 기준**: 컴파일 성공
- **참조**: TutorialActionFocusObject.cs

---

### Layer 2: TutorialController 수정

#### [TASK-002] TutorialController._strategies에 FOCUS_UI 등록
- **의존**: TASK-001
- **Files**: `Assets/_Project/Scripts/Common/TutorialController.cs`
- **작업**:
  - [x] _strategies 딕셔너리에 FOCUS_UI 등록
- **완료 기준**: GetStrategy(FOCUS_UI) 반환 성공

#### [TASK-003] CalculateTargetUvPosition()에 FOCUS_UI 조건 추가
- **의존**: TASK-001
- **Files**: `Assets/_Project/Scripts/Common/TutorialController.cs`
- **작업**:
  - [x] FOCUS_UI 타입일 때 GetNormalizedPosition() 사용하도록 조건 추가
- **완료 기준**: UI 타겟에 대해 올바른 좌표 변환

---

### Layer 3: 테스트

#### [TASK-004] 통합 테스트
- **의존**: Layer 2 완료
- **작업**:
  - [ ] FOCUS_UI ActionType으로 튜토리얼 데이터 설정
  - [ ] UI 타겟에 TutorialTarget 컴포넌트 부착
  - [ ] 마스크 홀 위치 확인
  - [ ] 딤드 클릭 시 진행 확인
- **완료 기준**: 전체 시나리오 통과

---

## 진행 로그

| 일시 | 작업 | 상태 |
|------|------|------|
| 2026-01-17 | Feature Spec 작성 | 완료 |
| 2026-01-17 | Implementation Plan 작성 | 완료 |
| 2026-01-17 | TASK-001: TutorialActionFocusUI.cs 생성 | 완료 |
| 2026-01-17 | TASK-002: _strategies에 FOCUS_UI 등록 | 완료 |
| 2026-01-17 | TASK-003: CalculateTargetUvPosition() 조건 추가 | 완료 |
