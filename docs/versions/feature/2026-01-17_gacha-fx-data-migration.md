# Feature: Gacha FX 데이터 구조 마이그레이션

> 생성일: 2026-01-17
> 상태: 완료

---

## Feature Capsule

| 항목 | 내용 |
|------|------|
| 기능명 | Gacha FX 데이터 구조 마이그레이션 |
| 한줄 설명 | 변경된 RewardItem 데이터 구조에 맞게 가챠 연출 스크립트 수정 |
| 해결하는 문제 | 데이터 구조 변경으로 인해 가챠 연출이 동작하지 않는 문제 |
| 핵심 시나리오 | 가챠 뽑기 → 연출 팝업 → 캐릭터/조각 획득 연출 정상 표시 |
| Scope | GachaFxByTen.cs, GachaFxByOne.cs의 ID 추출 로직 수정 |
| Non-scope | 가챠 연출 VFX 변경, 새 기능 추가 |
| 성공 기준 | 가챠 연출 시 캐릭터 정보가 정상적으로 표시됨 |
| 관련 도메인 | UI |

---

## Decision Log

| ID | 항목 | 선택 | 근거 | 대안 |
|----|------|------|------|------|
| D-01 | ID 추출 방식 | `GetCharacterId()` 확장 메서드 사용 | 기존 코드베이스에 이미 구현된 메서드 활용 | 직접 `% 10000` 연산 |
| D-02 | Null 체크 | 각 `GetCharacterData()` 호출 후 null 체크 추가 | 런타임 에러 방지 | 없음 (위험) |

---

## Feature Spec

### 문제 분석

**기존 데이터 구조:**
- `CharacterInfo.id` 직접 사용 (예: `2101`, `3301`)

**변경된 데이터 구조:**
- `RewardItem.Id` = ItemId 타입 (예: `1053401`, `1032102`)
- 조각 ID에서 캐릭터 ID 추출 필요: `id % 10000`

**영향받는 로직:**
1. SSR 캐릭터 판별 (`grade_type == LEGENDARY`)
2. 캐릭터 정보 조회 (`SpecDataManager.GetCharacterData()`)
3. 획득 연출 표시 (`GetNewCharacter.SetChracater()`)

### 해결 방법

기존 `IdMap.cs`의 `GetCharacterId()` 확장 메서드 활용:

```csharp
// IdMap.cs:93-107
public static bool GetCharacterId(this ItemId id, out int charId)
{
    charId = -1;
    if (id.IsCharacter())
    {
        charId = id.Value;  // 캐릭터는 ID 그대로
        return true;
    }
    if (id.IsCharacterPiece())
    {
        charId = id.Value % 10000;  // 조각: 마지막 4자리 추출
        return true;
    }
    return false;
}
```

---

## Implementation Plan

### 변경 범위

#### Scripts (Client)

| 경로 | 변경 유형 | 설명 |
|------|----------|------|
| `Addressables/Remote/Gacha_STK/Script/GachaFxByTen.cs` | 수정 | ID 추출 로직 변경 |
| `Addressables/Remote/Gacha_STK/Script/GachaFxByOne.cs` | 수정 | 데이터 타입 및 ID 추출 로직 변경 |

### 매니저/시스템 의존성

| 매니저 | 용도 |
|--------|------|
| SpecDataManager | `GetCharacterData(int characterId)` |
| IdMap (확장 메서드) | `GetCharacterId(this ItemId id, out int charId)` |

---

## TASKS

### [TASK-001] GachaFxByTen.cs ID 추출 로직 수정
- **상태**: 완료
- **Files**: `Addressables/Remote/Gacha_STK/Script/GachaFxByTen.cs`
- **작업**:
  - [x] `SetItem()` 내 SSR 판별 로직 수정 (Line 137-174)
  - [x] `SetSkipList()` 내 캐릭터 조회 수정 (Line 325-328)
  - [x] `ShowSkipCharacterFX()` 내 캐릭터 조회 수정 (Line 415-419)
  - [x] `ShowGetFX()` 내 캐릭터 조회 수정 (Line 524-531)
  - [x] Null 체크 추가

### [TASK-002] GachaFxByOne.cs 데이터 타입 변경
- **상태**: 완료
- **Files**: `Addressables/Remote/Gacha_STK/Script/GachaFxByOne.cs`
- **작업**:
  - [x] `_datas` 타입 변경: `List<CharacterInfo>` → `List<RewardItem>`
  - [x] `SetItem()` 시그니처 변경
  - [x] SSR 포함 여부 판별 로직 수정
  - [x] `ShowGetFX()` 내 캐릭터 조회 수정
  - [x] Null 체크 추가

---

## 진행 로그

| 일시 | 작업 | 비고 |
|------|------|------|
| 2026-01-17 | TASK-001 완료 | GachaFxByTen.cs 수정 |
| 2026-01-17 | TASK-002 완료 | GachaFxByOne.cs 수정 |

---

## 코드 변경 상세

### GachaFxByTen.cs 주요 변경

```csharp
// Before
if (datas[i].Id.IsCharacterPiece())
{
    var specData = SpecDataManager.Instance.GetCharacterData(datas[i].Id);
    if (specData.grade_type == GradeType.LEGENDARY && datas[i].Count == 20)

// After
if (datas[i].Id.GetCharacterId(out int characterId))
{
    var specData = SpecDataManager.Instance.GetCharacterData(characterId);
    if (specData != null && specData.grade_type == GradeType.LEGENDARY && datas[i].Count == 20)
```

### GachaFxByOne.cs 주요 변경

```csharp
// Before
private List<CharacterInfo> _datas = null;
public void SetItem(List<CharacterInfo> datas)
{
    if (datas.Exists(x => x.grade_type == GradeType.LEGENDARY && x.need_piece == 20))

// After
private List<RewardItem> _datas = null;
public void SetItem(List<RewardItem> datas)
{
    foreach (var data in datas)
    {
        if (data.Id.GetCharacterId(out int charId))
        {
            var charInfo = SpecDataManager.Instance.GetCharacterData(charId);
            if (charInfo != null && charInfo.grade_type == GradeType.LEGENDARY && data.Count == 20)
```

---

## 테스트 포인트

1. **1회 뽑기**: 일반/SSR 캐릭터 연출 정상 표시
2. **10회 뽑기**: 모든 캐릭터 연출 순차 표시
3. **스킵 기능**: 스킵 시 결과 화면 정상 표시
4. **SSR 연출**: 레전더리 캐릭터 특수 연출 정상 동작
