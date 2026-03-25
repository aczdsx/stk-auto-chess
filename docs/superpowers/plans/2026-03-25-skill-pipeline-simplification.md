# Skill Pipeline Simplification Implementation Plan

> **For agentic workers:** REQUIRED: Use superpowers:subagent-driven-development (if subagents available) or superpowers:executing-plans to implement this plan. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** SkillRecipeBuilder, SkillRecipeRegistry, SkillFactory 3개 클래스를 하나의 SkillFactory partial class로 통합하여 스킬 파이프라인 중간 레이어를 단순화한다.

**Architecture:** SkillRecipeRegistry의 딕셔너리와 등록 로직을 SkillFactory로 흡수하고, SkillRecipeBuilder struct를 SkillFactory의 private inner struct로 이동. partial 파일 4개(Core, Archetypes, Character, Monster)로 구성.

**Spec:** `docs/superpowers/specs/2026-03-25-skill-pipeline-simplification-design.md`

---

## Chunk 1: SkillFactory Core 통합

### Task 1: SkillFactory.cs에 Recipe 딕셔너리와 Builder 흡수

**Files:**
- Modify: `Assets/_Project/Scripts/InGame_New/Simulation/Combat/Skills/SkillFactory.cs`
- Reference: `Assets/_Project/Scripts/InGame_New/Simulation/Combat/Skills/Recipe/SkillRecipeRegistry.cs`
- Reference: `Assets/_Project/Scripts/InGame_New/Simulation/Combat/Skills/Recipe/SkillRecipeBuilder.cs`

- [ ] **Step 1: SkillFactory.cs에 Recipe 딕셔너리 2개 추가**

`_registry` 선언 아래에 추가:

```csharp
private static readonly Dictionary<int, SkillRecipe> _recipes = new();
private static readonly Dictionary<SimSkillArchetype, SkillRecipe> _archetypeRecipes = new();
```

- [ ] **Step 2: Recipe 조회 메서드 추가**

`TryGetSpecList()` 아래에 추가:

```csharp
private static bool TryGetRecipe(int skillGroupId, out SkillRecipe recipe)
    => _recipes.TryGetValue(skillGroupId, out recipe);

private static bool TryGetByArchetype(SimSkillArchetype archetype, out SkillRecipe recipe)
    => _archetypeRecipes.TryGetValue(archetype, out recipe);
```

- [ ] **Step 3: SkillRecipeBuilder를 private inner struct로 이동**

SkillFactory 클래스 끝부분에 `SkillRecipeBuilder.cs`의 전체 struct 내용을 private inner struct로 추가. 차이점:
- `public struct` → `private struct`
- 나머지 필드, 생성자, 메서드, static 팩토리 메서드는 그대로 유지

```csharp
// SkillFactory 클래스 내부 마지막에 추가
private struct SkillRecipeBuilder
{
    // SkillRecipeBuilder.cs의 전체 내용 (public → 그대로, struct 자체만 private)
    // ... (기존 363줄 코드 그대로)
}
```

- [ ] **Step 4: Builder 헬퍼 메서드 추가**

inner struct 위에 추가:

```csharp
// ── Recipe Builder 헬퍼 ──

private static SkillRecipeBuilder Skill(int skillId, SkillExecutionType exec, SkillTargetType target)
    => new SkillRecipeBuilder(_recipes, skillId, exec, target);

private static SkillRecipeBuilder ArchetypeBuilder(SkillExecutionType exec, SkillTargetType target)
    => new SkillRecipeBuilder(null, 0, exec, target);

private static void DefineArchetype(SimSkillArchetype archetype, SkillRecipe recipe)
    => _archetypeRecipes[archetype] = recipe;
```

- [ ] **Step 5: static constructor 추가**

`_initialized` 필드 아래에 추가:

```csharp
static SkillFactory()
{
    RegisterArchetypeRecipes();
    RegisterPlayerRecipes();
    RegisterMonsterRecipes();
}
```

- [ ] **Step 6: Initialize() 내부 참조 변경**

83행 `SkillRecipeRegistry.TryGetByArchetype(archetype, out var archetypeRecipe)` →
`TryGetByArchetype(archetype, out var archetypeRecipe)`

- [ ] **Step 7: RegisterRecipeSkills() 내부 참조 변경**

147행 `SkillRecipeRegistry.TryGet(id, out var recipe)` →
`TryGetRecipe(id, out var recipe)`

- [ ] **Step 8: Clear() 메서드 - _recipes, _archetypeRecipes 제외 확인**

기존 Clear()는 `_registry`, `_paramsCache`, `_specListCache`만 비움. `_recipes`와 `_archetypeRecipes`는 static constructor에서 1회 초기화되는 불변 데이터이므로 비우지 않음. 주석 추가:

```csharp
public static void Clear()
{
    _registry.Clear();
    _paramsCache.Clear();
    _specListCache.Clear();
    _initialized = false;
    // _recipes, _archetypeRecipes는 static readonly 데이터 — Clear 대상 아님
}
```

- [ ] **Step 9: 기존 주석에서 SkillRecipeRegistry 참조 정리**

103행, 123행 주석에서 `SkillRecipeRegistry` → `SkillFactory` 또는 해당 주석 자체를 현행화.

---

### Task 2: Archetypes partial 파일 생성

**Files:**
- Create: `Assets/_Project/Scripts/InGame_New/Simulation/Combat/Skills/SkillFactory.Archetypes.cs`
- Reference: `Assets/_Project/Scripts/InGame_New/Simulation/Combat/Skills/Recipe/SkillRecipeRegistry.Archetypes.cs`

- [ ] **Step 1: SkillFactory.Archetypes.cs 작성**

`SkillRecipeRegistry.Archetypes.cs`를 복사하여 다음만 변경:
- `using static CookApps.AutoChess.SkillRecipeBuilder` → `using static CookApps.AutoChess.SkillFactory.SkillRecipeBuilder`
- `public static partial class SkillRecipeRegistry` → `public static partial class SkillFactory`
- 나머지 코드(RegisterArchetypeRecipes, DefineArchetype 호출 등) 그대로

---

### Task 3: Character partial 파일 생성

**Files:**
- Create: `Assets/_Project/Scripts/InGame_New/Simulation/Combat/Skills/SkillFactory.Character.cs`
- Reference: `Assets/_Project/Scripts/InGame_New/Simulation/Combat/Skills/Recipe/SkillRecipeRegistry.Character.cs`

- [ ] **Step 1: SkillFactory.Character.cs 작성**

`SkillRecipeRegistry.Character.cs`를 복사하여 다음만 변경:
- `using static CookApps.AutoChess.SkillRecipeBuilder` → `using static CookApps.AutoChess.SkillFactory.SkillRecipeBuilder`
- `public static partial class SkillRecipeRegistry` → `public static partial class SkillFactory`
- 나머지 코드(RegisterPlayerRecipes, 모든 Skill() 호출 등) 그대로

---

### Task 4: Monster partial 파일 생성

**Files:**
- Create: `Assets/_Project/Scripts/InGame_New/Simulation/Combat/Skills/SkillFactory.Monster.cs`
- Reference: `Assets/_Project/Scripts/InGame_New/Simulation/Combat/Skills/Recipe/SkillRecipeRegistry.Monster.cs`

- [ ] **Step 1: SkillFactory.Monster.cs 작성**

`SkillRecipeRegistry.Monster.cs`를 복사하여 다음만 변경:
- `using static CookApps.AutoChess.SkillRecipeBuilder` → `using static CookApps.AutoChess.SkillFactory.SkillRecipeBuilder`
- `public static partial class SkillRecipeRegistry` → `public static partial class SkillFactory`
- 나머지 코드(RegisterMonsterRecipes 등) 그대로

---

## Chunk 2: 구 파일 삭제 및 주석 정리

### Task 5: SkillRecipe.cs 주석 업데이트

**Files:**
- Modify: `Assets/_Project/Scripts/InGame_New/Simulation/Combat/Skills/Recipe/SkillRecipe.cs:8`

- [ ] **Step 1: 주석 업데이트**

8행 `/// 1. SkillRecipeRegistry에 static으로 정의` →
`/// 1. SkillFactory에 static으로 정의`

---

### Task 6: 구 파일 삭제

**Files:**
- Delete: `Assets/_Project/Scripts/InGame_New/Simulation/Combat/Skills/Recipe/SkillRecipeBuilder.cs`
- Delete: `Assets/_Project/Scripts/InGame_New/Simulation/Combat/Skills/Recipe/SkillRecipeBuilder.cs.meta`
- Delete: `Assets/_Project/Scripts/InGame_New/Simulation/Combat/Skills/Recipe/SkillRecipeRegistry.cs`
- Delete: `Assets/_Project/Scripts/InGame_New/Simulation/Combat/Skills/Recipe/SkillRecipeRegistry.cs.meta`
- Delete: `Assets/_Project/Scripts/InGame_New/Simulation/Combat/Skills/Recipe/SkillRecipeRegistry.Archetypes.cs`
- Delete: `Assets/_Project/Scripts/InGame_New/Simulation/Combat/Skills/Recipe/SkillRecipeRegistry.Archetypes.cs.meta`
- Delete: `Assets/_Project/Scripts/InGame_New/Simulation/Combat/Skills/Recipe/SkillRecipeRegistry.Character.cs`
- Delete: `Assets/_Project/Scripts/InGame_New/Simulation/Combat/Skills/Recipe/SkillRecipeRegistry.Character.cs.meta`
- Delete: `Assets/_Project/Scripts/InGame_New/Simulation/Combat/Skills/Recipe/SkillRecipeRegistry.Monster.cs`
- Delete: `Assets/_Project/Scripts/InGame_New/Simulation/Combat/Skills/Recipe/SkillRecipeRegistry.Monster.cs.meta`

- [ ] **Step 1: .cs 파일 5개 삭제**

```bash
rm Assets/_Project/Scripts/InGame_New/Simulation/Combat/Skills/Recipe/SkillRecipeBuilder.cs
rm Assets/_Project/Scripts/InGame_New/Simulation/Combat/Skills/Recipe/SkillRecipeRegistry.cs
rm Assets/_Project/Scripts/InGame_New/Simulation/Combat/Skills/Recipe/SkillRecipeRegistry.Archetypes.cs
rm Assets/_Project/Scripts/InGame_New/Simulation/Combat/Skills/Recipe/SkillRecipeRegistry.Character.cs
rm Assets/_Project/Scripts/InGame_New/Simulation/Combat/Skills/Recipe/SkillRecipeRegistry.Monster.cs
```

- [ ] **Step 2: .meta 파일 5개 삭제**

```bash
rm Assets/_Project/Scripts/InGame_New/Simulation/Combat/Skills/Recipe/SkillRecipeBuilder.cs.meta
rm Assets/_Project/Scripts/InGame_New/Simulation/Combat/Skills/Recipe/SkillRecipeRegistry.cs.meta
rm Assets/_Project/Scripts/InGame_New/Simulation/Combat/Skills/Recipe/SkillRecipeRegistry.Archetypes.cs.meta
rm Assets/_Project/Scripts/InGame_New/Simulation/Combat/Skills/Recipe/SkillRecipeRegistry.Character.cs.meta
rm Assets/_Project/Scripts/InGame_New/Simulation/Combat/Skills/Recipe/SkillRecipeRegistry.Monster.cs.meta
```

---

### Task 7: 컴파일 검증

- [ ] **Step 1: 프로젝트 전체에서 SkillRecipeRegistry 참조 잔존 여부 확인**

```bash
grep -r "SkillRecipeRegistry" Assets/_Project/Scripts/ --include="*.cs"
```

Expected: 결과 없음 (모든 참조 제거 완료)

- [ ] **Step 2: SkillRecipeBuilder 외부 참조 확인**

```bash
grep -r "SkillRecipeBuilder" Assets/_Project/Scripts/ --include="*.cs"
```

Expected: SkillFactory.cs 내부와 partial 파일의 `using static`만 남아야 함

- [ ] **Step 3: 커밋**

```bash
git add Assets/_Project/Scripts/InGame_New/Simulation/Combat/Skills/SkillFactory.cs
git add Assets/_Project/Scripts/InGame_New/Simulation/Combat/Skills/SkillFactory.Archetypes.cs
git add Assets/_Project/Scripts/InGame_New/Simulation/Combat/Skills/SkillFactory.Character.cs
git add Assets/_Project/Scripts/InGame_New/Simulation/Combat/Skills/SkillFactory.Monster.cs
git add Assets/_Project/Scripts/InGame_New/Simulation/Combat/Skills/Recipe/SkillRecipe.cs
git add Assets/_Project/Scripts/InGame_New/Simulation/Combat/Skills/Recipe/SkillRecipeBuilder.cs
git add Assets/_Project/Scripts/InGame_New/Simulation/Combat/Skills/Recipe/SkillRecipeRegistry.cs
git add Assets/_Project/Scripts/InGame_New/Simulation/Combat/Skills/Recipe/SkillRecipeRegistry.Archetypes.cs
git add Assets/_Project/Scripts/InGame_New/Simulation/Combat/Skills/Recipe/SkillRecipeRegistry.Character.cs
git add Assets/_Project/Scripts/InGame_New/Simulation/Combat/Skills/Recipe/SkillRecipeRegistry.Monster.cs
# .meta 파일도 포함
git add Assets/_Project/Scripts/InGame_New/Simulation/Combat/Skills/Recipe/SkillRecipeBuilder.cs.meta
git add Assets/_Project/Scripts/InGame_New/Simulation/Combat/Skills/Recipe/SkillRecipeRegistry.cs.meta
git add Assets/_Project/Scripts/InGame_New/Simulation/Combat/Skills/Recipe/SkillRecipeRegistry.Archetypes.cs.meta
git add Assets/_Project/Scripts/InGame_New/Simulation/Combat/Skills/Recipe/SkillRecipeRegistry.Character.cs.meta
git add Assets/_Project/Scripts/InGame_New/Simulation/Combat/Skills/Recipe/SkillRecipeRegistry.Monster.cs.meta
git commit -m "refactor: merge SkillRecipeBuilder+SkillRecipeRegistry into SkillFactory partial class"
```
