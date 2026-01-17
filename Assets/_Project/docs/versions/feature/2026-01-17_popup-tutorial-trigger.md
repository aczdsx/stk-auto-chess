# Feature: 팝업 열림/닫힘 튜토리얼 트리거

> 생성일: 2026-01-17
> 상태: 설계 완료

---

## Feature Capsule

| 항목 | 내용 |
|------|------|
| 기능명 | 팝업 열림/닫힘 튜토리얼 트리거 |
| 한줄 설명 | 팝업이 완전히 열리거나 닫힐 때 튜토리얼을 트리거하는 기능 |
| 해결하는 문제 | 특정 팝업의 열림/닫힘 시점에 튜토리얼 가이드를 표시할 수 있도록 |
| 핵심 시나리오 | 1. 팝업 열림 완료 → SHOW_POP_COMPLETE 트리거<br>2. 팝업 닫힘 완료 → CLOSE_POP_COMPLETE 트리거 |
| Scope | UILayerType.Popup만 처리 |
| Non-scope | UILayerType.Modal, Cover, Overlay 제외 |
| 관련 도메인 | UI, Common |

---

## Decision Log

| ID | 항목 | 선택 | 근거 | 대안 |
|----|------|------|------|------|
| D-01 | 구현 위치 | 중간 클래스 (UILayerPopupBase) | Scripts_Libs 수정 불필요, 깔끔한 상속 구조 | UILayer 직접 수정, SceneUILayerManager 수정 |
| D-02 | 클래스명 | UILayerPopupBase | UILayer에서 파생됨을 명시 | PopupBase |
| D-03 | 대상 UILayerType | Popup만 | 우선 Popup만 처리, Modal은 추후 필요시 | Popup + Modal |
| D-04 | key 값 | 팝업 클래스명 (GetType().Name) | 명확한 식별, 스펙 데이터와 매칭 용이 | - |

---

## Feature Spec

### 상세 요구사항
- REQ-F01: 팝업이 완전히 열린 후 (OnPostEnter) `SHOW_POP_COMPLETE` 트리거 발동
- REQ-F02: 팝업이 완전히 닫힌 후 (OnPostExit) `CLOSE_POP_COMPLETE` 트리거 발동
- REQ-F03: key로 팝업 클래스명 전달 (예: "GachaPopup")
- REQ-F04: TutorialManager가 없거나 튜토리얼 진행 중이 아니면 무시 (기존 로직)

### 동작 흐름
```
[팝업 열림]
SceneUILayerManager.PushUILayerAsync()
  → UILayerPopupBase.OnPostEnter()
    → TutorialManager.HandleTutorialAction(SHOW_POP_COMPLETE, "GachaPopup")

[팝업 닫힘]
SceneUILayerManager.PopUILayer()
  → UILayerPopupBase.OnPostExit()
    → TutorialManager.HandleTutorialAction(CLOSE_POP_COMPLETE, "GachaPopup")
```

### 엣지 케이스
| 케이스 | 처리 |
|--------|------|
| TutorialManager 없음 | HandleTutorialAction에서 IsTutorial 체크로 무시 |
| 튜토리얼 진행 중 아님 | 스펙 리스트가 비어있으면 무시 |
| 해당 팝업이 스펙에 없음 | 트리거 key 매칭 실패로 무시 |

---

## Implementation Plan

### 변경 범위

#### Scripts (추가)
| 경로 | 설명 |
|------|------|
| `Scripts/UI/Common/UILayerPopupBase.cs` | UILayer 상속, OnPostEnter/OnPostExit 오버라이드 |

#### Scripts (수정 - 상속 변경)
| 경로 | 변경 내용 |
|------|----------|
| `Scripts/UI/Popup/**/*Popup.cs` (39개) | `UILayer` → `UILayerPopupBase` 상속 변경 |
| `Scripts/UI/Popup/ImageInfoPop.cs` | 동일 |

### 매니저/시스템 의존성
| 매니저 | 용도 |
|--------|------|
| TutorialManager | HandleTutorialAction 호출 |

### Spec 데이터 (이미 추가됨)
| Enum | 값 |
|------|-----|
| SHOW_POP_COMPLETE | 22 |
| CLOSE_POP_COMPLETE | (사용자가 추가) |

---

## TASKS

### 의존성 레이어 구조

```
Layer 1: UILayerPopupBase 클래스 생성
    ↓
Layer 2: 기존 Popup 상속 변경 (병렬 가능 - 파일별 독립)
    ↓
Layer 3: 테스트
```

---

### Layer 1: Base 클래스 생성

#### [TASK-001] UILayerPopupBase 클래스 생성
- **의존**: 없음
- **병렬**: -
- **Files**: `Scripts/UI/Common/UILayerPopupBase.cs`
- **작업**:
  - [ ] UILayerPopupBase 클래스 생성 (UILayer 상속)
  - [ ] OnPostEnter 오버라이드 → SHOW_POP_COMPLETE 트리거
  - [ ] OnPostExit 오버라이드 → CLOSE_POP_COMPLETE 트리거
  - [ ] key로 GetType().Name 전달
- **완료 기준**: 클래스 생성 완료
- **후속**: Layer 2 시작

---

### Layer 2: 상속 변경

#### [TASK-002] Popup 파일 상속 변경
- **의존**: TASK-001
- **병렬**: -
- **Files**: `Scripts/UI/Popup/` 내 40개 파일
- **작업**:
  - [ ] 모든 Popup 파일에서 `: UILayer` → `: UILayerPopupBase` 변경
  - [ ] using 문 추가 필요시 추가
- **완료 기준**: 모든 Popup이 UILayerPopupBase 상속
- **후속**: Layer 3 시작

---

### Layer 3: 검증

#### [TASK-003] 빌드 검증
- **의존**: TASK-002
- **병렬**: -
- **작업**:
  - [ ] 컴파일 에러 없음 확인
- **완료 기준**: 빌드 성공

---

## 파일 목록 (상속 변경 대상)

```
Scripts/UI/Popup/
├── AccountLevelUpWindowPopup.cs
├── AttendancePopup.cs
├── BattleStatisticsPopup.cs
├── ChapterClearWindowPopup.cs
├── ChapterListPopup.cs
├── CharacterCollectionPopup.cs
├── CommanderSkillPopup.cs
├── DialoguePopup.cs
├── DungeonTrialPopup.cs
├── Elpis/
│   ├── ElpisBuildingPopup.cs
│   └── CommandCenter/
│       ├── ElpisCommandCenterPopup.cs
│       └── ElpisCommandCenterResultPopup.cs
├── EndTestgamePopup.cs
├── EnemySkillTooltipPopup.cs
├── GachaPopup.cs
├── IdleRewardIncreasedPopup.cs
├── IdleRewardPopup.cs
├── ImageInfoPop.cs
├── InGameDungeonResultPopup.cs
├── InGameDungeonTrialResultPopup.cs
├── InGameExitPopup.cs
├── InGameResultPopup.cs
├── InfoDetailTooltipPopup.cs
├── ItemConsumeEventPopup.cs
├── ItemTooltipPopup.cs
├── LoadingPopup.cs
├── NicknamePopup.cs
├── QuestPopup.cs
├── RewardResultPopup.cs
├── SessionTimeEventPopup.cs
├── SettingPopup.cs
├── ShopBannerPopup.cs
├── SkillTooltipPopup.cs
├── StageDetailPopup.cs
├── SynergyTooltipInGamePopup.cs
├── SynergyTooltipIngameMiniPopup.cs
├── SynergyTooltipPopup.cs
├── SystemConfirmPopup.cs
├── ToastSystemPopup.cs
```

총 40개 파일
