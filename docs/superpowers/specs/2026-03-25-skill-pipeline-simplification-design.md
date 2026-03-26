# Skill Pipeline Simplification Design

## 목적

스킬 시스템의 중간 레이어(SkillRecipeBuilder, SkillRecipeRegistry, SkillFactory)를 하나의 `SkillFactory` 클래스로 통합하여 파일/클래스 수를 줄이고, 스킬 추가 시 건드리는 곳을 최소화한다.

## 현재 구조 (Before)

```
Skills/Recipe/
  SkillRecipeBuilder.cs              (363줄, struct)
  SkillRecipeRegistry.cs             (56줄, static partial class)
  SkillRecipeRegistry.Archetypes.cs  (92줄)
  SkillRecipeRegistry.Character.cs   (204줄)
  SkillRecipeRegistry.Monster.cs     (24줄)
  SkillRecipe.cs                     (130줄, 유지)
  ActionExecutor.cs                  (656줄, 유지)
Skills/
  SkillFactory.cs                    (168줄, static class)
```

- 클래스 3개: SkillRecipeBuilder, SkillRecipeRegistry, SkillFactory
- 파일 7개 (Recipe 데이터/실행기 제외)
- 스킬 실행 경로: SkillFactory → SkillRecipeRegistry → SkillRecipeBuilder → SkillRecipe → SimSkillGeneric

## 변경 후 구조 (After)

```
Skills/
  SkillFactory.cs                    (Core + Builder + 딕셔너리)
  SkillFactory.Archetypes.cs         (아키타입 Recipe 정의)
  SkillFactory.Character.cs          (플레이어 스킬 Recipe 정의)
  SkillFactory.Monster.cs            (몬스터 스킬 Recipe 정의)
  Recipe/
    SkillRecipe.cs                   (유지)
    ActionExecutor.cs                (유지)
```

- 클래스 1개: SkillFactory (partial)
- 파일 4개
- 스킬 실행 경로: SkillFactory → SkillRecipe → SimSkillGeneric

## 상세 변경

### 1. SkillFactory.cs (Core)

SkillRecipeRegistry의 딕셔너리 2개와 조회 메서드를 흡수:
- `_recipes` (int → SkillRecipe)
- `_archetypeRecipes` (SimSkillArchetype → SkillRecipe)
- `TryGetRecipe()`, `TryGetByArchetype()` → private

SkillRecipeBuilder struct를 private inner struct로 이동:
- 액션 팩토리 메서드(Damage, Heal, CC 등)는 inner struct의 static 메서드로 유지
- partial 파일에서 `using static SkillFactory.SkillRecipeBuilder;` 패턴 유지

Builder 헬퍼를 private static 메서드로 추가:
- `Skill(id, exec, target)` → SkillRecipeBuilder 반환
- `ArchetypeBuilder(exec, target)` → SkillRecipeBuilder 반환
- `DefineArchetype(archetype, recipe)` → _archetypeRecipes에 등록

Initialize() 내부 변경:
- `SkillRecipeRegistry.TryGetByArchetype()` → `TryGetByArchetype()`
- `RegisterRecipeSkills()` 내부 `SkillRecipeRegistry.TryGet()` → `TryGetRecipe()`

static constructor 추가:
```csharp
static SkillFactory()
{
    RegisterArchetypeRecipes();
    RegisterPlayerRecipes();
    RegisterMonsterRecipes();
}
```

**Clear() 메서드 변경**: `_recipes`와 `_archetypeRecipes`는 static constructor에서 1회만 초기화되는 불변 데이터이므로, `Clear()`에서 비우지 않는다. Clear()는 기존처럼 `_registry`, `_paramsCache`, `_specListCache`만 초기화.

```csharp
public static void Clear()
{
    _registry.Clear();
    _paramsCache.Clear();
    _specListCache.Clear();
    _initialized = false;
    // _recipes, _archetypeRecipes는 비우지 않음 (static constructor에서 1회 초기화)
}
```

### 2. SkillFactory.Archetypes.cs

SkillRecipeRegistry.Archetypes.cs를 그대로 이동. 네임스페이스와 클래스명만 변경:
- `partial class SkillRecipeRegistry` → `partial class SkillFactory`

### 3. SkillFactory.Character.cs

SkillRecipeRegistry.Character.cs를 그대로 이동. 동일하게 클래스명 변경.

### 4. SkillFactory.Monster.cs

SkillRecipeRegistry.Monster.cs를 그대로 이동. 동일하게 클래스명 변경.

### 5. 삭제 파일

- `Skills/Recipe/SkillRecipeBuilder.cs` (→ SkillFactory inner struct로 이동)
- `Skills/Recipe/SkillRecipeRegistry.cs` (→ SkillFactory.cs에 흡수)
- `Skills/Recipe/SkillRecipeRegistry.Archetypes.cs` (→ SkillFactory.Archetypes.cs)
- `Skills/Recipe/SkillRecipeRegistry.Character.cs` (→ SkillFactory.Character.cs)
- `Skills/Recipe/SkillRecipeRegistry.Monster.cs` (→ SkillFactory.Monster.cs)

### 6. .meta 파일

Unity에서 삭제된 .cs 파일의 .meta도 함께 삭제. 새로 생성되는 파일의 .meta는 Unity Editor가 자동 생성.

## 외부 영향도

- **외부 API 변경 없음**: `SkillFactory.Create()`, `TryGetParams()`, `TryGetSpecList()` 시그니처 유지
- **SimSkillGeneric**: Recipe를 `SetRecipe()`로 주입받으므로 변경 없음
- **SkillSystem**: `SkillFactory.Create()` 호출만 하므로 변경 없음
- **SkillSpecAdapter**: SkillFactory/Registry를 직접 참조하지 않으므로 변경 없음
- **using static 변경**: 내부 partial 파일에서만 `using static SkillRecipeBuilder` → `using static SkillFactory.SkillRecipeBuilder` (외부 참조 없음)
- **주석 업데이트**: `SkillRecipe.cs`, `SkillFactory.cs` 내부 주석에서 `SkillRecipeRegistry` 참조를 `SkillFactory`로 변경

## 제약조건

- GC-free 설계 유지 (lambda 캡처 패턴 변경 없음)
- 결정론적 실행 보장 (static readonly Recipe)
- 커스텀 스킬 11개 및 헬퍼 5개는 이번 범위에서 제외
