# Feature: 스테이지 클리어 후 나니노벨 트리거

> 생성일: 2026-01-17
> 상태: 설계 완료

---

## Feature Capsule

| 항목 | 내용 |
|------|------|
| **기능명** | 스테이지 클리어 후 나니노벨 트리거 |
| **한줄 설명** | 스테이지 클리어 시 STAGE_CLEAR_NANI 타입 트리거를 확인하고 나니노벨 재생 |
| **해결하는 문제** | 특정 스테이지 클리어 후 스토리 씬 자동 재생 |
| **핵심 시나리오** | 스테이지 클리어 → InGameResultPopup 버튼 클릭 → 씬 전환 전 트리거 확인 → 나니노벨 재생 → 원래 목적지 씬 이동 |
| **Scope** | InGameResultPopup의 모든 버튼(나가기/다음/재시도)에서 STAGE_CLEAR_NANI 트리거 확인 |
| **Non-scope** | - |
| **관련 도메인** | InGame, Naninovel |

---

## Decision Log

| ID | 항목 | 선택 | 근거 | 대안 |
|----|------|------|------|------|
| D-01 | 구현 방식 | SceneLoading에 새 메서드 추가 | 기존 GoToNextSceneWithSpecialTrigger 패턴 일관성 | InGameResultPopup에서 직접 처리 |
| D-02 | 트리거 적용 범위 | 모든 버튼 (나가기/다음/재시도) | 사용자 요청 | 나가기 버튼만 |

---

## Feature Spec

### 상세 요구사항

- **REQ-F01**: 스테이지 클리어 후 InGameResultPopup에서 버튼 클릭 시 STAGE_CLEAR_NANI 트리거 확인
- **REQ-F02**: 트리거가 있으면 나니노벨 재생 후 원래 목적지로 이동
- **REQ-F03**: 트리거가 없으면 기존처럼 바로 씬 전환

### 트리거 데이터 구조

| 필드 | 값 예시 | 설명 |
|------|---------|------|
| naninovel_trigger_type | STAGE_CLEAR_NANI | 트리거 타입 |
| trigger_key | "1001", "1002", "1003" | 스테이지 ID (문자열) |
| naninovel_name | "Chapter0_04" | 재생할 나니노벨 스크립트 |

### 플로우

```
[스테이지 클리어]
       ↓
[InGameResultPopup 표시]
       ↓
[버튼 클릭 (나가기/다음/재시도)]
       ↓
[GoToNextSceneWithStageClearTrigger 호출]
       ↓
[NaninovelTriggerManager.GetTriggerOnStageClear(stageId)]
       ↓
    트리거 있음? ─Yes→ [Naninovel 재생] → [원래 목적지로 이동]
       ↓ No
[바로 씬 전환]
```

---

## Implementation Plan

### 변경 범위

#### Scripts (Client)

| 경로 | 변경 유형 | 설명 |
|------|----------|------|
| `Assets/_Project/Scripts_Libs/CookApps/UIManagements/SceneLoading.cs` | 수정 | 델리게이트 + 메서드 추가 |
| `Assets/_Project/Scripts/NaninovelExtension/NaninovelTriggerManager.cs` | 수정 | Initialize()에서 델리게이트 연동 |
| `Assets/_Project/Scripts/UI/Popup/InGameResultPopup.cs` | 수정 | 씬 전환 메서드 변경 |

### 매니저/시스템 의존성

| 매니저 | 용도 |
|--------|------|
| NaninovelTriggerManager | STAGE_CLEAR_NANI 트리거 검색 |
| SceneLoading | 씬 전환 + 나니노벨 경유 처리 |
| InGameManager | 현재 스테이지 ID 조회 |

### 서버 API 변경

없음 (클라이언트 로직만 변경)

---

## TASKS

### 의존성 레이어 구조

```
Layer 1: SceneLoading 델리게이트/메서드 추가
    ↓
Layer 2: NaninovelTriggerManager 델리게이트 연동
    ↓
Layer 3: InGameResultPopup 씬 전환 로직 변경
```

---

### Layer 1: SceneLoading 확장

#### [TASK-001] SceneLoading에 StageClear 트리거 지원 추가

- **의존**: 없음
- **병렬**: -
- **Files**: `Assets/_Project/Scripts_Libs/CookApps/UIManagements/SceneLoading.cs`
- **작업**:
  - [ ] `OnGetStageClearNaninovelTrigger` 델리게이트 추가 (Func<int, string>)
  - [ ] `GoToNextSceneWithStageClearTrigger` 메서드 추가
- **완료 기준**: 메서드 호출 시 델리게이트를 통해 트리거 검색 가능
- **참조**: `GoToNextSceneWithSpecialTrigger` 패턴 (라인 77-82)

---

### Layer 2: NaninovelTriggerManager 연동

#### [TASK-002] NaninovelTriggerManager 델리게이트 연동

- **의존**: TASK-001
- **병렬**: -
- **Files**: `Assets/_Project/Scripts/NaninovelExtension/NaninovelTriggerManager.cs`
- **작업**:
  - [ ] `Initialize()`에서 `SceneLoading.OnGetStageClearNaninovelTrigger` 연동
- **완료 기준**: 델리게이트 호출 시 `GetTriggerOnStageClear` 실행
- **참조**: 기존 `OnGetNaninovelTrigger`, `OnGetSpecialNaninovelTrigger` 연동 패턴 (라인 23-24)

---

### Layer 3: InGameResultPopup 수정

#### [TASK-003] InGameResultPopup 씬 전환 로직 변경

- **의존**: TASK-002
- **병렬**: -
- **Files**: `Assets/_Project/Scripts/UI/Popup/InGameResultPopup.cs`
- **작업**:
  - [ ] `OnExitButtonClickedAsync`: `GoToNextScene` → `GoToNextSceneWithStageClearTrigger` 변경
  - [ ] `OnNextStageButtonClickedAsync`: `GoToNextScene` → `GoToNextSceneWithStageClearTrigger` 변경
  - [ ] `OnClickRetryStageButtonAsync`: `GoToNextScene` → `GoToNextSceneWithStageClearTrigger` 변경
  - [ ] 현재 클리어한 스테이지 ID 전달 (`InGameManager.Instance.SpecStage.stage_id`)
- **완료 기준**: 버튼 클릭 시 STAGE_CLEAR_NANI 트리거 확인 후 씬 전환
- **참조**: 기존 `SceneLoading.GoToNextScene` 호출 위치 (라인 209, 252, 278)

---

## 진행 로그

| 일시 | 단계 | 내용 |
|------|------|------|
| 2026-01-17 | Phase 1 | Feature Capsule 확정 |
| 2026-01-17 | Phase 2 | 영향 분석 완료 |
| 2026-01-17 | Phase 3 | 문서 생성 완료 |
