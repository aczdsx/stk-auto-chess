# Feature: 인게임 캐릭터 필터링

> 생성일: 2026-01-15
> 상태: 대기 중

---

## Feature Capsule

```
기능명: 인게임 캐릭터 필터링
한줄 설명: 하단 배치 캐릭터 목록을 속성/성군으로 필터링
해결하는 문제: 캐릭터가 많아졌을 때 원하는 캐릭터 찾기 어려움
핵심 시나리오: 필터 버튼 → 팝업에서 속성/성군 선택 → 실시간 필터링
Scope: 속성 필터(5종), 성군 필터(3종), 복수선택, AND조건, 실시간적용, 상태유지
Non-scope: 없음
성공 기준: 필터 선택 시 하단 목록이 실시간으로 필터링됨
관련 도메인: [UI, InGame]
영향받는 기존 코드: InGameBottomUI.cs, UILayerConstants.cs
주요 리스크: 필터 상태 관리 (InitData 호출 시 리셋 방지)
```

---

## Decision Log

| ID | 항목 | 선택 | 근거 | 대안 |
|----|------|------|------|------|
| D-01 | 필터 선택 방식 | 복수 선택 | 사용자 편의성 | 단일 선택 |
| D-02 | ALL 버튼 동작 | 필터 해제 | 직관적 UX | 모든 타입 선택 |
| D-03 | 속성+성군 조합 | AND 조건 | 정확한 필터링 | OR 조건 |
| D-04 | 적용 시점 | 실시간 (팝업 유지) | 즉각적 피드백 | 확인 버튼 |
| D-05 | 상태 유지 | 라운드 전환 시 유지 | 사용자 편의 | 매 라운드 초기화 |
| D-06 | 빈 결과 처리 | 빈 목록 표시 | 간단한 처리 | 안내 메시지/자동 해제 |

---

## Feature Spec

### 상세 요구사항

- **REQ-F01**: 속성(Element) 필터 - FIRE, WIND, LIGHTNING, EARTH, WATER (5종)
- **REQ-F02**: 성군(Stella) 필터 - NOBLESSE, TROUBLESHOOTER, SUPERNOVA (3종)
- **REQ-F03**: 복수 선택 가능 (속성 내, 성군 내 각각)
- **REQ-F04**: 속성 + 성군 = AND 조건 적용
- **REQ-F05**: ALL 버튼 = 해당 카테고리 필터 해제
- **REQ-F06**: 선택 즉시 실시간 적용 (팝업 유지)
- **REQ-F07**: 라운드 전환 시에도 필터 상태 유지
- **REQ-F08**: 필터 결과 0개여도 빈 목록 표시

### UI/UX 명세

- **UILayerType**: Popup (딤드 배경, 외부 터치로 닫기)
- **화면 구성**:
  - 속성 섹션: ALL + 5개 속성 아이콘 (토글)
  - 성군 섹션: ALL + 3개 성군 아이콘 (토글)
  - X 닫기 버튼
- **사용자 플로우**:
  1. 하단 UI "필터" 버튼 터치
  2. FilterTooltipInIngamePopup 표시
  3. 속성/성군 아이콘 터치 → 토글 ON/OFF
  4. 선택 즉시 하단 캐릭터 목록 갱신
  5. 딤드 영역 또는 X 버튼 터치 → 팝업 닫힘

### 데이터 구조

```csharp
// ISpecCharacterInfo 필터 대상 필드
SynergyType character_element_type  // 속성 시너지
SynergyType character_stella_type   // 성군 시너지

// 필터 상태 (InGameBottomUI에서 관리)
HashSet<SynergyType> _selectedElementFilters  // 선택된 속성
HashSet<SynergyType> _selectedStellaFilters   // 선택된 성군
```

### 필터링 로직

```csharp
// AND 조건 적용
bool PassFilter(CharacterStatData stat)
{
    // 속성 필터가 비어있으면 통과, 아니면 포함 여부 확인
    bool passElement = _selectedElementFilters.Count == 0
        || _selectedElementFilters.Contains(stat.Spec.character_element_type);

    // 성군 필터가 비어있으면 통과, 아니면 포함 여부 확인
    bool passStella = _selectedStellaFilters.Count == 0
        || _selectedStellaFilters.Contains(stat.Spec.character_stella_type);

    return passElement && passStella;
}
```

---

## Implementation Plan

### 변경 범위

#### Scripts (Client)

| 경로 | 변경 유형 | 설명 |
|------|----------|------|
| `Scripts/UI/InGame/FilterTooltipInIngamePopup.cs` | **추가** | 필터 팝업 UI Layer |
| `Scripts/UI/InGame/InGameBottomUI.cs` | **수정** | 필터링 로직 + 상태 관리 |
| `Scripts_Libs/.../UILayerConstants.cs` | **수정** | 팝업 등록 이름 변경 |

#### Addressables (Assets)

| 경로 | 변경 유형 | 설명 |
|------|----------|------|
| `Addressables/Remote/Prefabs/UI/InGame/FilterTooltipInIngamePopup.prefab` | **기존** | 이미 존재, 스크립트 연결만 |

### 매니저/시스템 의존성

| 매니저 | 용도 |
|--------|------|
| SceneUILayerManager | 필터 팝업 Push/Pop |
| SpecDataManager | SynergyType enum 참조 |

### Spec 데이터 변경

없음 (기존 ISpecCharacterInfo 활용)

### 서버 API 변경

없음 (클라이언트 전용 필터링)

---

## TASKS

### 의존성 분석

| Layer | 작업 | 선행 조건 | 병렬 가능 |
|-------|------|----------|-----------|
| 1 | UILayerConstants 등록 | - | - |
| 2 | FilterTooltipInIngamePopup.cs 생성 | Layer 1 | - |
| 3 | InGameBottomUI 필터링 로직 | Layer 2 | - |
| 4 | 프리팹 스크립트 연결 + 테스트 | Layer 3 | - |

---

### Layer 1: 설정

#### [TASK-001] UILayerConstants 팝업 등록 확인/수정
- **의존**: 없음
- **Files**: `Assets/_Project/Scripts_Libs/CookApps/UIManagements/Generated_UILayer/UILayerConstants.cs`
- **변경사항**:
  - 현재 `"SynergyTooltipInGamePopup_1"` → `FilterTooltipInIngamePopup.prefab` 매핑됨
  - `"FilterTooltipInIngamePopup"` 키로 변경 또는 신규 추가
- **Subtasks**:
  - [ ] 1-1. 기존 매핑 확인
  - [ ] 1-2. FilterTooltipInIngamePopup 키로 등록
- **완료 기준**: `SceneUILayerManager.Instance.PushUILayerAsync<FilterTooltipInIngamePopup>()` 호출 가능

---

### Layer 2: UI Layer 구현

#### [TASK-002] FilterTooltipInIngamePopup.cs 생성
- **의존**: TASK-001 완료
- **Files**: `Assets/_Project/Scripts/UI/InGame/FilterTooltipInIngamePopup.cs`
- **변경사항**:
  - UILayer 상속
  - 속성/성군 필터 토글 UI
  - 선택 변경 시 콜백으로 InGameBottomUI에 전달
- **Subtasks**:
  - [ ] 2-1. 클래스 기본 구조 작성 (UILayer 상속)
  - [ ] 2-2. SerializeField 바인딩 (속성 버튼들, 성군 버튼들, 닫기 버튼)
  - [ ] 2-3. 토글 로직 구현 (복수 선택)
  - [ ] 2-4. ALL 버튼 로직 (필터 해제)
  - [ ] 2-5. 선택 변경 시 콜백 호출
  - [ ] 2-6. OnPreEnter에서 현재 필터 상태 반영
- **완료 기준**: 팝업 열림, 토글 동작, 콜백 호출 확인
- **참조**: `SynergyTooltipInGamePopup.cs` 패턴 참고

**핵심 구현 코드**:
```csharp
public class FilterTooltipInIngamePopup : UILayer
{
    public struct FilterParam
    {
        public HashSet<SynergyType> SelectedElements;
        public HashSet<SynergyType> SelectedStellas;
        public Action<HashSet<SynergyType>, HashSet<SynergyType>> OnFilterChanged;
    }

    // 속성 버튼: FIRE, WIND, LIGHTNING, EARTH, WATER
    // 성군 버튼: NOBLESSE, TROUBLESHOOTER, SUPERNOVA
    // ALL 버튼: 해당 카테고리 필터 해제
}
```

---

### Layer 3: 필터링 로직

#### [TASK-003] InGameBottomUI 필터링 기능 추가
- **의존**: TASK-002 완료
- **Files**: `Assets/_Project/Scripts/UI/InGame/InGameBottomUI.cs`
- **변경사항**:
  - 필터 상태 필드 추가
  - ApplyFilter() 메서드 추가
  - 필터 팝업 열기 버튼 연결
  - InitData() 수정 (필터 상태 유지)
- **Subtasks**:
  - [ ] 3-1. 필터 상태 필드 추가 (`HashSet<SynergyType>`)
  - [ ] 3-2. `ApplyFilter()` 메서드 구현
  - [ ] 3-3. `GetFilteredCharacterStats()` 메서드 구현
  - [ ] 3-4. `OpenFilterPopup()` 메서드 추가
  - [ ] 3-5. 필터 버튼 SerializeField + 바인딩
  - [ ] 3-6. InitData() 수정 - 필터 상태 유지하면서 데이터 갱신
  - [ ] 3-7. UpdateData() 수정 - 필터된 목록 사용
- **완료 기준**: 필터 선택 시 하단 목록 실시간 갱신

**핵심 구현 코드**:
```csharp
// 필터 상태 (라운드 전환 시에도 유지)
private HashSet<SynergyType> _selectedElementFilters = new();
private HashSet<SynergyType> _selectedStellaFilters = new();
private List<CharacterStatData> _allCharacterStats = new(); // 전체 목록 보관

public void ApplyFilter(HashSet<SynergyType> elements, HashSet<SynergyType> stellas)
{
    _selectedElementFilters = elements;
    _selectedStellaFilters = stellas;
    RefreshFilteredList();
}

private void RefreshFilteredList()
{
    _characterStats = _allCharacterStats
        .Where(stat => PassFilter(stat))
        .OrderByDescending(stat => stat.Level)
        .ThenByDescending(stat => stat.CharacterID)
        .ToList();
    UpdateData();
}

private bool PassFilter(CharacterStatData stat)
{
    bool passElement = _selectedElementFilters.Count == 0
        || _selectedElementFilters.Contains(stat.Spec.character_element_type);
    bool passStella = _selectedStellaFilters.Count == 0
        || _selectedStellaFilters.Contains(stat.Spec.character_stella_type);
    return passElement && passStella;
}
```

---

### Layer 4: 통합

#### [TASK-004] 프리팹 연결 및 테스트
- **의존**: TASK-003 완료
- **Files**:
  - `FilterTooltipInIngamePopup.prefab`
  - Unity Editor
- **Subtasks**:
  - [ ] 4-1. 프리팹에 FilterTooltipInIngamePopup.cs 컴포넌트 추가
  - [ ] 4-2. SerializeField 바인딩 (버튼들 연결)
  - [ ] 4-3. InGameBottomUI 프리팹에 필터 버튼 연결
  - [ ] 4-4. 테스트: 필터 팝업 열기/닫기
  - [ ] 4-5. 테스트: 속성 필터 단일/복수 선택
  - [ ] 4-6. 테스트: 성군 필터 단일/복수 선택
  - [ ] 4-7. 테스트: 속성 + 성군 AND 조건
  - [ ] 4-8. 테스트: ALL 버튼 (필터 해제)
  - [ ] 4-9. 테스트: 라운드 전환 시 필터 유지
  - [ ] 4-10. 테스트: 빈 결과 표시
- **완료 기준**: 전체 시나리오 통과

---

## Golden Rules 체크리스트

### 필수 준수
- [ ] UniTask 사용 (코루틴 X)
- [ ] State machine 패턴 해당 없음 (UI 팝업)
- [ ] LINQ 최소화 (필터링 시 Where 사용은 OK, 매 프레임 X)

### 파일 명명 규칙
- [x] UI: `FilterTooltipInIngamePopup.cs` (기존 프리팹명 따름)

---

## 진행 로그

| 일시 | 상태 | 내용 |
|------|------|------|
| 2026-01-15 | 설계 완료 | Feature Spec, Implementation Plan, TASKS 생성 |
| 2026-01-15 | 구현 완료 | TASK-001~003 완료, TASK-004 Unity 작업 필요 |
