# SkillSpecAdapter → SkillFactory 통합 Design

## 목적

SkillSpecAdapter를 SkillFactory에 합쳐서:
1. Factory → Adapter → Factory 왕복 플로우 제거
2. BuildParams 안에서 Recipe 직접 접근 → TargetType/FaceTarget 한 번에 확정
3. 클래스 1개 추가 제거
4. 죽은 코드 정리

## 현재 문제

```
Factory.Initialize()
  → Adapter.ClassifySkill()     // Factory → Adapter
  → Adapter.BuildParams()       // Factory → Adapter (TargetType = NearestEnemy 기본값)
  → Factory: Recipe로 TargetType 덮어쓰기  // 다시 Factory
  → Factory: _registry 등록
```

- SkillSpecAdapter는 SkillFactory에서만 호출 (외부 사용자 0)
- BuildParams에서 Recipe에 접근 못해서 Factory에서 이중 처리
- RegisterRecipeSkills()와 Initialize() 루프가 중복 등록 경로
- CreateFromArchetype()은 항상 null 반환 (죽은 코드)

## 변경 후 플로우

```
Factory.Initialize()
  → Factory.ClassifySkill()     // 내부 호출
  → Factory.BuildParams()       // 내부 호출, Recipe 직접 접근
     + TargetType/FaceTarget 한 번에 확정
  → Factory: _registry 등록 (개별 Recipe > 아키타입 Recipe)
```

## 상세 변경

### 1. SkillSpecAdapter 전체를 SkillFactory.SpecAdapter.cs partial로 이동

- `ClassifySkill()` → private static
- `BuildParams()` → private static (내부에서 TryGetRecipe/TryGetByArchetype 직접 호출)
- `ApplySkillSpecificParams()` → private static
- `ExtractSkillHitTimes()` → private static
- `SecondsToFrames()`, `GetSpecRate()` → private static
- `CreateFromArchetype()` → **삭제** (항상 null 반환)

### 2. BuildParams() 안에서 FaceTarget 한 번에 확정

```csharp
// BuildParams 마지막에서:
SkillTargetType resolvedTarget = p.TargetType;
if (TryGetRecipe(spec.skill_group_id, out var idRecipe))
    resolvedTarget = idRecipe.TargetRule;
else if (TryGetByArchetype(archetype, out var archRecipe))
    resolvedTarget = archRecipe.TargetRule;
p.TargetType = resolvedTarget;
p.FaceTarget = resolvedTarget != SkillTargetType.Self
    && resolvedTarget != SkillTargetType.LowestHPAlly;
```

→ Initialize()의 TargetType/FaceTarget 동기화 코드 삭제.

### 3. RegisterRecipeSkills() 삭제

대신 Initialize() 루프에서 개별 Recipe 우선 등록 경로 추가:

```csharp
// 기존
if (TryGetByArchetype(archetype, out var archetypeRecipe)) { ... }
else { CreateFromArchetype (삭제) }

// 변경
if (TryGetRecipe(id, out var individualRecipe))
{
    var captured = individualRecipe;
    Register(id, () => { var s = new SimSkillGeneric(); s.SetRecipe(captured); return s; });
}
else if (TryGetByArchetype(archetype, out var archetypeRecipe))
{
    var captured = archetypeRecipe;
    Register(id, () => { var s = new SimSkillGeneric(); s.SetRecipe(captured); return s; });
}
```

### 4. ClassifySkill() Custom 목록 축소

Recipe 기반 스킬 11개를 Custom에서 제거:

**제거 (Initialize 루프에서 개별 Recipe로 자동 등록):**
- 215532401 (필리아), 217433303 (하티), 215252102 (유니)
- 215422301 (멘샤), 217323201 (미사), 217553404 (클레이)
- 215642501 (엘리스), 215322201 (메이)
- 230101005, 230202004 (몬스터 투사체), 250108001 (보스탱커)

**유지 (전용 커스텀 클래스 필요):**
- 217433302 (미노), 217363204 (베인), 217413301 (테토라)
- 217563405 (마리에), 217653505 (엔키), 217333202 (에이프릴)
- 217613501 (오데트), 217523403 (아드리아), 217663506 (시라유키)
- 217263103 (루키다), 217353203 (라키유)

### 5. 삭제 파일

- `Assets/_Project/Scripts/InGame_New/Adapter/SkillSpecAdapter.cs` + `.meta`

### 6. 생성 파일

- `Assets/_Project/Scripts/InGame_New/Simulation/Combat/Skills/SkillFactory.SpecAdapter.cs` (partial)

## 검증 결과

| 스킬 | Custom 제거 후 분류 | TryGetRecipe | 등록 Recipe | 결과 |
|------|-------------------|-------------|-----------|------|
| 필리아 | SingleDamage | 있음 → 개별 Recipe | DelayedApply+마커 | 정확 |
| 유니 | SingleDamage | 있음 → 개별 Recipe | DelayedApply+Heal | 정확 |
| 아트레시아 | SingleDamage | 있음 → 개별 Recipe | DelayedApply+LinearProjectile | 정확 |
| 보스탱커 | SingleDamage | 있음 → 개별 Recipe | Channeling+SequentialLine | 정확 |
| 시이나 | DamageCC (기존) | 없음 → 아키타입 Recipe | DamageCC | 정확 |
| 일반 몬스터 | 각 아키타입 | 없음 → 아키타입 Recipe | 각 아키타입 | 정확 |

## 제약조건

- 상속 구조 변경 없음
- RegisterCustomSkills()의 11개 전용 클래스 등록은 유지
- 아키타입별 BuildParams 기본값 로직은 그대로 이동
