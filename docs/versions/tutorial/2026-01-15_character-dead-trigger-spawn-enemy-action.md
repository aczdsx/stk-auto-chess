# Tutorial Feature: CHARACTER_DEAD 트리거 + SPAWN_ENEMY 액션

> 생성일: 2026-01-15
> 상태: 구현 완료
> 유형: 새 TriggerType 호출부 구현 + 새 ActionType 전략 구현

---

## Feature Capsule

```
기능명: CHARACTER_DEAD 트리거 + SPAWN_ENEMY 액션
한줄 설명: 특정 캐릭터 사망 시 튜토리얼 트리거 발동 + 몬스터 3개 순차 스폰 액션
유형: 새 TriggerType 호출부 구현 + 새 ActionType 전략 구현
해결하는 문제: 튜토리얼에서 캐릭터 사망 이벤트 기반 흐름 제어 및 적 스폰 연출
사용 시나리오: 특정 캐릭터 사망 → CHARACTER_DEAD 트리거 → SPAWN_ENEMY 액션 → 몬스터 3개 생성 → 다음 튜토리얼
타겟 오브젝트: TutorialTarget이 부착된 캐릭터 (사망 감지용)
완료 조건:
  - CHARACTER_DEAD: 트리거 발동만 (사망 시점에 HandleTutorialAction 호출)
  - SPAWN_ENEMY: 3개 스폰 완료 시 콜백으로 다음 진행
Scope:
  - CHARACTER_DEAD 트리거 호출부 구현 (CharacterController.GetDamaged)
  - SPAWN_ENEMY 전략 클래스 구현
  - TutorialController에 전략 등록
Non-scope:
  - 스폰 위치 직접 지정 (향후 확장)
  - 몬스터 개수/레벨 파라미터화 (현재 3개, 레벨1 고정)
성공 기준:
  - TutorialTarget ID로 지정된 캐릭터 사망 시 튜토리얼 트리거 발동
  - SPAWN_ENEMY 액션으로 몬스터 3개 0.5~1초 간격 스폰
  - 스폰 완료 후 다음 튜토리얼로 자동 진행
관련 전략: TutorialActionSpawnEnemy (신규)
영향받는 기존 코드:
  - CharacterController.GetDamaged() - 트리거 호출 추가
  - TutorialController._strategies - 전략 등록
주요 리스크: 캐릭터 사망 시점에 TutorialTarget 조회 타이밍
```

---

## Decision Log

| ID | 항목 | 선택 | 근거 | 대안 |
|----|------|------|------|------|
| D-01 | tutorial_trigger_key 형식 | TutorialTarget ID | 캐릭터에 TutorialTarget 컴포넌트로 ID 부여, 기존 시스템과 일관성 | 몬스터 ID 직접 사용 |
| D-02 | 스폰 위치 | 랜덤 (Enemy 타일 중 빈 곳) | 단순 구현, 향후 직접 지정 가능하게 확장 예정 | 고정 좌표 지정 |
| D-03 | 스폰 간격 | 0.5~1초 랜덤 | 연출 자연스러움 | 고정 간격 |
| D-04 | 스폰 개수 | 3개 고정 | 단순화, 튜토리얼 용도로 충분 | action_key 파라미터화 |
| D-05 | 몬스터 레벨 | 1 고정 | 튜토리얼 용도, 단순화 | action_key에 레벨 포함 |
| D-06 | tutorial_action_key 형식 | 몬스터 ID만 | 레벨 고정이므로 ID만 필요 | "몬스터ID_레벨" 형식 |

---

## Feature Spec: CHARACTER_DEAD 트리거 + SPAWN_ENEMY 액션

### 상세 요구사항

#### CHARACTER_DEAD 트리거
- **REQ-T01**: 트리거 조건 - TutorialTarget ID와 일치하는 캐릭터가 사망할 때 발동
- **REQ-T02**: 트리거 키 - `tutorial_trigger_key`에 TutorialTarget ID 지정
- **REQ-T03**: 호출 위치 - `CharacterController.GetDamaged()`에서 사망 처리 시

#### SPAWN_ENEMY 액션
- **REQ-A01**: 스폰 대상 - `tutorial_action_key`에 지정된 몬스터 ID
- **REQ-A02**: 스폰 개수 - 3개 고정
- **REQ-A03**: 스폰 간격 - 0.5~1초 랜덤 간격으로 순차 생성
- **REQ-A04**: 스폰 위치 - Enemy 영역 빈 타일 중 랜덤
- **REQ-A05**: 몬스터 레벨 - 1 고정
- **REQ-A06**: 완료 조건 - 3개 모두 스폰 완료 시 콜백 호출

### 전략 설계: TutorialActionSpawnEnemy

```csharp
public class TutorialActionSpawnEnemy : ITutorialActionStrategy
{
    public static System.Action OnSpawnEnemyCompleted;

    private const int SPAWN_COUNT = 3;
    private const float MIN_SPAWN_INTERVAL = 0.5f;
    private const float MAX_SPAWN_INTERVAL = 1.0f;

    public void OnShow(TutorialActionContext context)
    {
        // 1. tutorial_action_key에서 몬스터 ID 파싱
        // 2. 스폰 코루틴/UniTask 시작
        // 3. 튜토리얼 UI 숨김 (스폰 중에는 UI 표시 안함)
    }

    public void OnNext(TutorialActionContext context)
    {
        // 다음 버튼 비활성화 (콜백으로만 진행)
        context.NextObj.SetActive(false);
    }

    public bool CanProceedOnDimmedClick(TutorialActionContext context)
    {
        return false; // 스폰 완료 콜백으로만 진행
    }

    public void OnClear(TutorialActionContext context)
    {
        OnSpawnEnemyCompleted = null;
    }
}
```

### 콜백 구조

```
TutorialActionSpawnEnemy.OnSpawnEnemyCompleted
  ↓
TutorialController.OnSpawnEnemyCompleted()
  ↓
ProceedToNext()
```

### Spec 데이터 설계

| 필드 | 값 예시 | 설명 |
|------|---------|------|
| tutorial_trigger_type | CHARACTER_DEAD (2) | 캐릭터 사망 트리거 |
| tutorial_trigger_key | "TargetEnemy_01" | 사망 감지할 캐릭터의 TutorialTarget ID |
| tutorial_action_type | SPAWN_ENEMY (4) | 적 스폰 액션 |
| tutorial_action_key | "1001001" | 스폰할 몬스터 ID |
| hole_radius | 0 | 마스크 홀 미사용 |
| desc_key | "" | 설명 텍스트 미사용 (스폰 중 UI 숨김) |

### 엣지 케이스

| 케이스 | 처리 |
|--------|------|
| 빈 타일 부족 (3개 미만) | 가능한 만큼만 스폰 후 완료 콜백 호출 |
| 몬스터 ID 유효하지 않음 | 로그 경고 후 즉시 완료 콜백 호출 |
| 튜토리얼 중단 | OnClear에서 스폰 취소 및 콜백 정리 |
| TutorialTarget 미존재 (사망 시) | 사망 전에 ID 캐싱 또는 컴포넌트에서 직접 조회 |

---

## Implementation Plan

### 변경 범위

#### Scripts (전략 패턴)

| 경로 | 변경 유형 | 설명 |
|------|----------|------|
| `Scripts/Common/Tutorial/Strategies/TutorialActionSpawnEnemy.cs` | **추가** | 새 전략 클래스 |
| `Scripts/Common/TutorialController.cs` | **수정** | 전략 등록 + 콜백 핸들러 |
| `Scripts/InGame/Character/CharacterController.cs` | **수정** | CHARACTER_DEAD 트리거 호출 추가 |

#### 타겟 오브젝트 (TutorialTarget)

| 대상 | 변경 유형 | 설명 |
|------|----------|------|
| 튜토리얼용 캐릭터 프리팹/스폰 로직 | 확인 | TutorialTarget 컴포넌트 부착 확인 (이미 있음) |

#### Spec 데이터

| Spec | 변경 유형 | 설명 |
|------|----------|------|
| TutorialDialogue | 데이터 추가 | CHARACTER_DEAD 트리거 + SPAWN_ENEMY 액션 항목 |

### 트리거 호출부

| 위치 | 호출 코드 |
|------|----------|
| `CharacterController.GetDamaged()` (사망 처리 부분) | `TutorialManager.Instance.HandleTutorialAction(TutorialTriggerType.CHARACTER_DEAD, tutorialTargetId)` |

### 의존성

- **선행 작업**: 없음 (Enum 이미 존재)
- **후행 작업**: Spec 데이터 추가, 통합 테스트

---

## TASKS

### 의존성 분석

| Layer | 작업 | 선행 조건 | 병렬 가능 |
|-------|------|----------|-----------|
| 1 | 전략 클래스 구현 | - | - |
| 2 | Controller 등록, 트리거 호출부 | Layer 1 | O |
| 3 | Spec 데이터, 통합 테스트 | Layer 2 | O |

---

### Layer 1: 전략 클래스 구현

#### [TASK-001] TutorialActionSpawnEnemy 전략 클래스 생성 ✅
- **의존**: 없음 (TutorialActionType.SPAWN_ENEMY 이미 존재)
- **Files**: `Assets/_Project/Scripts/Common/Tutorial/Strategies/TutorialActionSpawnEnemy.cs`
- **Subtasks**:
  - [x] 1-1. 클래스 생성 및 ITutorialActionStrategy 구현
  - [x] 1-2. OnShow 구현
    - tutorial_action_key에서 몬스터 ID 파싱
    - 스폰 UniTask 시작 (SpawnEnemiesAsync)
    - 튜토리얼 UI 요소 숨김
  - [x] 1-3. SpawnEnemiesAsync 구현
    - 3개 몬스터 순차 스폰
    - 0.5~1초 랜덤 간격
    - InGameObjectManager.Instance.AddCharacterToField 사용
    - 완료 시 OnSpawnEnemyCompleted 콜백 호출
  - [x] 1-4. OnNext 구현 (NextObj 비활성화)
  - [x] 1-5. CanProceedOnDimmedClick 구현 (return false)
  - [x] 1-6. OnClear 구현 (콜백 정리, 스폰 취소 토큰)
  - [x] 1-7. 정적 콜백 정의: `public static System.Action OnSpawnEnemyCompleted`
- **완료 기준**: 전략 클래스 컴파일 성공
- **참조**:
  - `TutorialActionCharacterPlacement.cs` (콜백 패턴)
  - `FlowStateLobbyCombat.SpawnEnemy()` (스폰 로직)

---

### Layer 2: Controller 등록 & 트리거 호출부 (병렬 가능)

#### [TASK-002] TutorialController에 전략 등록 ✅
- **의존**: TASK-001 완료
- **Files**: `Assets/_Project/Scripts/Common/TutorialController.cs`
- **Subtasks**:
  - [x] 2-1. _strategies 딕셔너리에 SPAWN_ENEMY 전략 추가
    ```csharp
    { TutorialActionType.SPAWN_ENEMY, new TutorialActionSpawnEnemy() }
    ```
  - [x] 2-2. ShowNextTutorial()에 콜백 등록 추가
    ```csharp
    if (CurrentSpecTutorial.tutorial_action_type == TutorialActionType.SPAWN_ENEMY)
    {
        TutorialActionSpawnEnemy.OnSpawnEnemyCompleted = OnSpawnEnemyCompleted;
    }
    ```
  - [x] 2-3. OnSpawnEnemyCompleted() 핸들러 추가
    ```csharp
    private void OnSpawnEnemyCompleted()
    {
        TutorialActionSpawnEnemy.OnSpawnEnemyCompleted = null;
        ProceedToNext();
    }
    ```
- **완료 기준**: GetStrategy()에서 TutorialActionSpawnEnemy 반환

#### [TASK-003] CharacterController에 CHARACTER_DEAD 트리거 호출 추가 ✅
- **의존**: TASK-001 완료
- **Files**: `Assets/_Project/Scripts/InGame/Character/CharacterController.cs`
- **Subtasks**:
  - [x] 3-1. GetDamaged() 메서드에서 사망 처리 부분 찾기 (라인 1320~1354)
  - [x] 3-2. TutorialTarget 컴포넌트 조회 및 ID 획득
    ```csharp
    var tutorialTarget = GetComponent<TutorialTarget>();
    if (tutorialTarget != null && !string.IsNullOrEmpty(tutorialTarget.TargetId))
    {
        TutorialManager.Instance.HandleTutorialAction(
            TutorialTriggerType.CHARACTER_DEAD,
            tutorialTarget.TargetId
        );
    }
    ```
  - [x] 3-3. ForceSetNextState<CharacterStateDead>() 호출 전에 트리거 호출 배치
- **완료 기준**: TutorialTarget이 있는 캐릭터 사망 시 트리거 발동
- **주의**: TutorialTarget이 없는 일반 캐릭터는 영향 없음

---

### Layer 3: Spec 데이터 & 통합 테스트 (병렬 가능)

#### [TASK-004] Spec 데이터 추가
- **의존**: Layer 2 완료
- **Files**: TutorialDialogue Spec 시트 (Google Sheets 또는 Excel)
- **예시 데이터**:
  | id | tutorial_id | seq | trigger_type | trigger_key | action_type | action_key |
  |----|-------------|-----|--------------|-------------|-------------|------------|
  | 100 | 10 | 1 | 2 (CHARACTER_DEAD) | "TargetEnemy_01" | 4 (SPAWN_ENEMY) | "1001001" |
- **완료 기준**: SpecDataManager에서 데이터 로드 가능

#### [TASK-005] 통합 테스트
- **의존**: Layer 2 완료
- **Subtasks**:
  - [ ] 5-1. TutorialTarget이 붙은 캐릭터 사망 시 CHARACTER_DEAD 트리거 발동 확인
  - [ ] 5-2. SPAWN_ENEMY 액션으로 몬스터 3개 순차 스폰 확인
  - [ ] 5-3. 스폰 간격 0.5~1초 확인
  - [ ] 5-4. 3개 스폰 완료 후 다음 튜토리얼로 진행 확인
  - [ ] 5-5. 엣지 케이스: 빈 타일 부족 시 처리 확인
  - [ ] 5-6. 엣지 케이스: 튜토리얼 중단 시 스폰 취소 확인
- **완료 기준**: 전체 시나리오 통과

---

## Golden Rules 체크리스트

- [x] ITutorialActionStrategy 인터페이스 완전 구현
- [x] TutorialController._strategies에 등록
- [x] 콜백 사용 시 정리(= null) 필수 (OnClear에서)
- [x] TutorialTargetRegistry 사용 (GameObject.Find 금지)
- [x] 스폰 실패 시 fallback 처리 (가능한 만큼 스폰 후 완료)
- [x] UniTask 취소 토큰 사용 (OnClear에서 취소)

---

## 진행 로그

| 일시 | 상태 | 내용 |
|------|------|------|
| 2026-01-15 | 설계 완료 | Feature Capsule, Decision Log, Implementation Plan, TASKS 작성 |
| 2026-01-15 | 구현 완료 | TASK-001~003 완료 (전략 클래스, Controller 등록, 트리거 호출부) |
