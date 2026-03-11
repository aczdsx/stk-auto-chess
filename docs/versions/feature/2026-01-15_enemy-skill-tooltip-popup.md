# Feature: EnemySkillTooltipPopup

> 생성일: 2026-01-15
> 상태: 완료

---

## Feature Capsule

```
기능명: EnemySkillTooltipPopup
한줄 설명: 보스 몬스터 클릭 시 스킬 정보를 보여주는 팝업
해결하는 문제: 유저가 상대방 보스 스킬 정보를 쉽게 확인
핵심 시나리오: Ready 단계 → 보스 몬스터 클릭 → 스킬 툴팁 팝업 표시
Scope: 보스 몬스터 스킬 툴팁 전체 구현
Non-scope: 일반 몬스터, Combat 단계
성공 기준: Ready 단계에서 보스 클릭 시 스킬 정보 정상 표시
관련 도메인: [InGame, UI]
영향받는 기존 코드: InGameTouchManager.cs, InGameMain.cs
주요 리스크: 없음 (기존 패턴 확장)
```

---

## Decision Log

| ID | 항목 | 선택 | 근거 | 대안 |
|----|------|------|------|------|
| D-01 | 대상 범위 | 보스 몬스터만 | `character_type == BOSS` 체크 | 모든 Enemy (복잡도 증가) |
| D-02 | 동작 단계 | Ready 단계만 | 기존 TouchManager가 Ready에서만 동작 | Combat 포함 (추후 확장) |
| D-03 | 팝업 위치 | 기존 프리팹 사용 | `EnemySkillTooltipPopup.prefab` 이미 존재 | 새 프리팹 생성 |
| D-04 | 닫기 방식 | X버튼 + Dim 터치 | UI 이미지 확인 | X버튼만 |
| D-05 | 스킬 데이터 | MonsterInfo.skill_ids[0] | 첫 번째 스킬이 메인 스킬 | 모든 스킬 표시 |
| D-06 | 팝업 패턴 | UILayer + PushUILayerAsync | 프로젝트 표준 패턴 | SetActive 방식 (비표준) |

---

## Feature Spec

### 상세 요구사항

- **REQ-F01**: Ready 단계에서 보스 몬스터(CharacterType.BOSS) 터치 시 스킬 툴팁 팝업 표시
- **REQ-F02**: 팝업에 스킬 이름, 스킬 설명, 스킬 이미지 표시
- **REQ-F03**: X버튼 또는 Dim 영역 터치 시 팝업 닫기
- **REQ-F04**: 몬스터 skill_ids[0]을 SkillActive 데이터와 매핑하여 정보 표시

### UI/UX 명세

- **UILayerType**: Modal (기존 `SkillTooltipPopup_1` 매핑 사용)
- **화면 구성**:
  - 상단: 스킬 이름 (TextMeshProUGUI)
  - 우측 상단: X 닫기 버튼 (CAButton)
  - 좌측: 스킬 이미지 (SpriteLoader)
  - 우측: 스킬 설명 (TextMeshProUGUI)
  - 배경: Dim 처리 (CAButton)

### 엣지 케이스

| 케이스 | 처리 |
|--------|------|
| skill_ids가 비어있는 경우 | 팝업 표시하지 않음 |
| SkillActive 데이터가 없는 경우 | 팝업 표시하지 않음 |
| 일반 몬스터 터치 | 무시 (보스만 대상) |
| Combat 단계에서 터치 | 기존 로직대로 무시 (Ready만 동작) |

---

## Implementation Plan

### 변경 범위

#### Scripts (Client)

| 경로 | 변경 유형 | 설명 |
|------|----------|------|
| `Scripts/UI/Popup/EnemySkillTooltipPopup.cs` | **추가** | 적 스킬 툴팁 팝업 스크립트 (UILayer 상속) |
| `Scripts/InGame/Managers/InGameTouchManager.cs` | 수정 | Enemy 보스 클릭 분기 추가 |
| `Scripts/UI/InGame/InGameMain.cs` | 수정 | ShowEnemySkillTooltip 메서드 추가 (PushUILayerAsync) |

#### Addressables (Assets)

| 경로 | 변경 유형 | 설명 |
|------|----------|------|
| `Prefabs/UI/InGame/EnemySkillTooltipPopup.prefab` | 수정 | 스크립트 연결 |

### 매니저/시스템 의존성

| 매니저 | 용도 |
|--------|------|
| SceneUILayerManager | 팝업 Push/Pop |
| SpecDataManager | MonsterInfo, SkillActive 데이터 조회 |
| LanguageManager | 스킬 이름/설명 로컬라이즈 |
| SpriteNameParser | 스킬 아이콘 스프라이트 경로 |

### 서버 API 변경

없음 (클라이언트 전용 기능)

### Spec 데이터 변경

없음 (기존 MonsterInfo.skill_ids, SkillActive 사용)

---

## TASKS

### 의존성 분석

| Layer | 작업 | 선행 조건 | 병렬 가능 |
|-------|------|----------|-----------|
| 1 | Spec 확인 | - | - |
| 2 | UI 스크립트 | Layer 1 | - |
| 3 | TouchManager 수정, InGameMain 수정 | Layer 2 | O (서로 독립적) |
| 4 | 프리팹 연결 | Layer 2 | O (Layer 3과 병렬) |
| 5 | 통합 테스트 | Layer 3, 4 | - |

---

### Layer 1: Spec 데이터 확인

#### [TASK-001] Spec 데이터 구조 확인
- **의존**: 없음
- **상태**: [x] 완료
- **확인사항**:
  - [x] MonsterInfo.skill_ids 필드 확인 (`int[]`)
  - [x] MonsterInfo.character_type 필드 확인 (`CharacterType.BOSS == 3`)
  - [x] SkillActive 구조 확인 (skill_name_token, skill_desc_token, skill_group_id)
  - [x] SpecDataManager.GetSkillDataList(skillID) 메서드 확인
- **완료 기준**: 데이터 매핑 방법 확정
- **후속**: Layer 2 작업 시작 가능

---

### Layer 2: UI 스크립트

#### [TASK-002] EnemySkillTooltipPopup.cs 생성
- **의존**: TASK-001 완료
- **상태**: [x] 완료
- **Files**: `Assets/_Project/Scripts/UI/Popup/EnemySkillTooltipPopup.cs`
- **최종 구현**:
  ```csharp
  public class EnemySkillTooltipPopup : UILayer
  {
      [SerializeField] private CAButton _closeButton;
      [SerializeField] private CAButton _dimButton;
      [SerializeField] private SpriteLoader _skillIconSpriteLoader;
      [SerializeField] private TextMeshProUGUI _skillNameText;
      [SerializeField] private TextMeshProUGUI _skillDescText;

      protected override void OnPreEnter(object param)
      {
          base.OnPreEnter(param);
          if (param is MonsterInfo monsterInfo)
              SetSkillTooltip(monsterInfo);
      }

      private void OnClickClose()
      {
          SceneUILayerManager.Instance.PopUILayer(this);
      }
  }
  ```
- **Subtasks**:
  - [x] 2-1. UILayer 상속 클래스 생성 및 SerializeField 정의
  - [x] 2-2. Awake에서 버튼 이벤트 바인딩
  - [x] 2-3. OnPreEnter에서 데이터 초기화 (MonsterInfo → SkillActive 매핑)
  - [x] 2-4. OnClickClose에서 PopUILayer 호출
- **완료 기준**: 컴파일 성공, 프로젝트 표준 패턴 준수

---

### Layer 3: Logic (병렬 가능)

#### [TASK-003] InGameTouchManager 수정
- **의존**: TASK-002 완료
- **상태**: [x] 완료
- **Files**: `Assets/_Project/Scripts/InGame/Managers/InGameTouchManager.cs`
- **최종 구현** (line 160-170):
  ```csharp
  // Enemy 보스 몬스터 클릭 시 스킬 툴팁 표시
  if (tile.OccupiedCharacter != null &&
      tile.OccupiedCharacter.AllianceType == AllianceType.Enemy)
  {
      var specMonster = tile.OccupiedCharacter.GetCharacterStat()?.Spec as MonsterInfo;
      if (specMonster != null && specMonster.character_type == CharacterType.BOSS)
      {
          InGameMain.GetInGameMain().ShowEnemySkillTooltip(specMonster);
          return;
      }
  }
  ```
- **Subtasks**:
  - [x] 3-1. Enemy Alliance 체크 조건 추가
  - [x] 3-2. CharacterType.BOSS 체크 조건 추가
  - [x] 3-3. ShowEnemySkillTooltip 호출
- **완료 기준**: 보스 클릭 시 분기 진입 확인

#### [TASK-004] InGameMain 수정
- **의존**: TASK-002 완료
- **상태**: [x] 완료
- **Files**: `Assets/_Project/Scripts/UI/InGame/InGameMain.cs`
- **최종 구현** (line 193-199):
  ```csharp
  public void ShowEnemySkillTooltip(MonsterInfo monsterInfo)
  {
      if (monsterInfo == null) return;
      if (monsterInfo.skill_ids == null || monsterInfo.skill_ids.Length == 0) return;

      SceneUILayerManager.Instance.PushUILayerAsync<EnemySkillTooltipPopup>(monsterInfo).Forget();
  }
  ```
- **Subtasks**:
  - [x] 4-1. ShowEnemySkillTooltip 메서드 구현 (PushUILayerAsync 방식)
- **완료 기준**: 메서드 호출 시 팝업 표시

---

### Layer 4: Presentation

#### [TASK-005] 프리팹 스크립트 연결
- **의존**: TASK-002 완료
- **상태**: [x] 완료
- **Files**:
  - `Addressables/Remote/Prefabs/UI/InGame/EnemySkillTooltipPopup.prefab`
- **Subtasks**:
  - [x] 5-1. EnemySkillTooltipPopup 프리팹에 스크립트 연결
  - [x] 5-2. SerializeField 바인딩 (closeButton, dimButton, skillIcon, skillName, skillDesc)
- **완료 기준**: Inspector에서 모든 참조 연결 확인

---

### Layer 5: Integration

#### [TASK-006] 통합 테스트
- **의존**: Layer 3, 4 완료
- **상태**: [x] 완료
- **Subtasks**:
  - [x] 6-1. Ready 단계에서 보스 몬스터 클릭 → 팝업 표시 확인
  - [x] 6-2. 스킬 이름, 설명, 이미지 정상 표시 확인
  - [x] 6-3. X버튼 클릭 → 팝업 닫힘 확인
  - [x] 6-4. Dim 영역 터치 → 팝업 닫힘 확인
  - [x] 6-5. 일반 몬스터(non-Boss) 클릭 → 팝업 미표시 확인
  - [x] 6-6. Combat 단계에서 클릭 → 동작 안 함 확인
- **완료 기준**: 전체 시나리오 통과

---

## Golden Rules 체크리스트

**필수 준수**:
- [x] UniTask 사용 (코루틴 X)
- [x] State machine 패턴 사용 - 해당 없음
- [x] 오브젝트 풀링 사용 - 해당 없음 (단일 팝업)
- [x] LINQ 지양 - 사용하지 않음

**UI 팝업 패턴**:
- [x] UILayer 상속
- [x] OnPreEnter에서 데이터 초기화
- [x] PopUILayer로 닫기
- [x] PushUILayerAsync로 열기

**파일 명명 규칙**:
- [x] UI: `EnemySkillTooltipPopup.cs` ✓

---

## 진행 로그

| 일시 | 상태 | 내용 |
|------|------|------|
| 2026-01-15 | 설계 완료 | Feature Spec, Implementation Plan, TASKS 생성 |
| 2026-01-15 | 구현 완료 | EnemySkillTooltipPopup.cs, InGameTouchManager.cs, InGameMain.cs 수정 |
| 2026-01-15 | 패턴 수정 | SetActive → UILayer + PushUILayerAsync 패턴으로 변경 |
| 2026-01-15 | 테스트 완료 | 전체 기능 동작 확인 |
