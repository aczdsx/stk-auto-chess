# 아키타입 제거 + TraitTag 기반 스킬 분류 시스템

## 한 줄 요약

SimSkillArchetype(완성품 라벨)을 삭제하고, 모든 스킬을 개별 Recipe 선언으로 통일.
Recipe에 `TraitTag` 비트마스크를 추가하여 기능 쿼리와 시너지 연동을 지원.

## 핵심 설계 원칙

### 왜 기능별 파일 분리를 하지 않는가

논의 과정에서 "기능별 파일 분리" (DamageCC.cs, AreaHeal.cs 등)를 검토했으나 **기각함.**

이유: 스킬은 여러 기능의 조합이므로 단일 분류에 강제할 수 없음.
- 클레이 = Heal + Damage + Debuff + RemoveDebuffs + AoE → 어느 파일?
- 메이 = Damage + Knockback + Buff → 어느 파일?
- 결국 "주 기능"이라는 주관적 판단 → 아키타입 라벨과 같은 문제 재발

**결론: "기능별 묶음"은 파일 구조가 아니라 TraitTag 쿼리로 달성.**

```csharp
// "DamageCC 스킬 뭐 있지?" → 파일을 뒤질 필요 없음
var damageCCSkills = allRecipes
    .Where(r => (r.Tags & (TraitTag.Damage | TraitTag.CC)) == (TraitTag.Damage | TraitTag.CC));

// 클레이 = 복합 기능이어도 전부 표현됨
clay.Tags == Heal | Damage | Debuff | RemoveDebuffs | AoE | Channeling
```

### 파일 조직은 현행 유지

| 파일 | 역할 | 변경 |
|------|------|------|
| `SkillFactory.Character.cs` | 플레이어 스킬 선언 | 시이나 등 아키타입 기반 스킬 → 개별 Recipe로 전환 |
| `SkillFactory.Monster.cs` | 몬스터 스킬 선언 | 모든 몬스터 개별 Recipe 추가 |
| `SkillFactory.Archetypes.cs` | 아키타입 Recipe | **삭제** |

파일을 기능별로 나누는 대신, **한 파일 안에서 Preset 함수로 공통 패턴을 재사용.**

## 두 가지 변경

### 변경 1: 아키타입 제거 → 모든 스킬 개별 Recipe

**문제**: 새 스킬 추가 시 switch 2~3개 수정 필요

```
ClassifySkill()           — switch(id) → 아키타입 분류
BuildParams()             — switch(archetype) → 기본값 설정
ApplySkillSpecificParams() — switch(id) → 개별 오버라이드
```

**해결**: 위 3개 함수 전부 삭제. 모든 스킬(몬스터 포함)이 개별 Recipe 선언을 가짐.

### 변경 2: TraitTag 추가 → 기능 쿼리 지원

**문제**: "이 스킬이 AoE인가?" "CC를 가진 스킬인가?" 를 코드에서 판단할 방법 없음.
아키타입은 단순 라벨이라 실제 기능과 1:1 대응이 아님.

**해결**: Recipe에 `TraitTag` [Flags] 비트마스크 추가. Actions에서 자동 추론 + 수동 보충.

## 현재 문제 (상세)

### 아키타입이 "라벨"에 불과

```
SimSkillArchetype.DamageCC
  → BuildParams: CCType=Stun, CCDuration=60  (기본값 하드코딩)
  → Archetype Recipe: Damage() + CC(Stun)     (같은 걸 Recipe로도 정의)
  → 개별 Recipe가 있으면? → 아키타입은 완전 무시됨
```

13개 아키타입 중:
- **몬스터**: 아키타입 Recipe로 동작 (개별 Recipe 없는 대부분)
- **플레이어**: 거의 전부 개별 Recipe 또는 Custom 클래스 → 아키타입 무의미

### BuildParams 아키타입별 기본값이 Recipe와 이중 정의

| 아키타입 | BuildParams 기본값 | Archetype Recipe |
|----------|-------------------|-----------------|
| DamageCC | CCType=Stun, CCDuration=60 | Damage() + CC(Stun) |
| ConeDamage | Param0=2 | Damage(EnemiesInArea, Line, 2) |
| MultiHit | HitCount=3 | MultiHit() |
| MultiTargetHeal | TargetCount=3 | Heal(LowestHpAllies, 3) |

같은 정보를 SkillParams(C# 필드)와 Recipe(선언형)에 중복 저장.

## 변경 후 구조

### TraitTag 정의

```csharp
[Flags]
public enum TraitTag : ulong
{
    None          = 0,
    Damage        = 1 << 0,
    AoE           = 1 << 1,
    CC            = 1 << 2,
    Heal          = 1 << 3,
    Shield        = 1 << 4,
    Buff          = 1 << 5,
    Debuff        = 1 << 6,
    Knockback     = 1 << 7,
    Projectile    = 1 << 8,
    Teleport      = 1 << 9,
    MultiHit      = 1 << 10,
    Channeling    = 1 << 11,
    RemoveDebuffs = 1 << 12,
    // 확장 가능 (최대 64개)
}
```

- ulong: 64비트, 장기 운영 대비
- 메타데이터 전용: 전투 틱 루프에서 사용 안 함, 성능 영향 없음
- 퀀텀 안전: enum, 힙 할당 없음

### SkillRecipe 변경

```csharp
public class SkillRecipe
{
    public SkillExecutionType ExecutionType;
    public SkillTargetType TargetRule;
    public bool HasProjectile;
    public TraitTag Tags;           // ← 추가: 기능 태그 비트마스크
    public SkillAction[] Actions;   // 기존과 동일
    public ParamSlot[] ParamSlots;  // 기존과 동일
}
```

SkillAction struct (17필드)는 변경 없음.

### 스킬 선언 (Before → After)

**Before (시이나 — 3곳 수정 필요):**
```csharp
// 1. ClassifySkill()
case 215362202: return SimSkillArchetype.DamageCC;

// 2. ApplySkillSpecificParams()
case 215362202:
    p.CCType = CrowdControlType.Silence;
    p.CCDurationFrames = SecondsToFrames(GetSpecRate(specList, 2, 3f), tickRate);
    break;

// 3. Archetype Recipe (이미 존재)
DefineArchetype(SimSkillArchetype.DamageCC, ...);
```

**After (시이나 — 1곳에서 완결):**
```csharp
Skill(215362202, E.Instant, T.NearestEnemy)        // 시이나
    .Param(1, P.Int, 200f)
    .Param(2, P.Frames, 3f)
    .OnCast(Damage(paramIndex: 0))
    .OnCast(CC(CrowdControlType.Silence, durationParamIndex: 1))
    .Register();
// Tags는 Build()에서 자동 추론: Damage | CC
```

### 몬스터 스킬 — Preset으로 공통 패턴 재사용

```csharp
// ── SkillFactory.Monster.cs ──

// 파라미터 없는 고정 Preset (공통 패턴 재사용)
static SkillRecipeBuilder DamageStun(SkillRecipeBuilder b)
    => b.OnCast(Damage()).OnCast(CC(CrowdControlType.Stun));

static SkillRecipeBuilder SingleDamage(SkillRecipeBuilder b)
    => b.OnCast(Damage());

static SkillRecipeBuilder ConeDamage(SkillRecipeBuilder b)
    => b.OnCast(Damage(filter: F.EnemiesInArea, area: S.Line, range: 2));

private static void RegisterMonsterRecipes()
{
    // ── DamageCC 패턴 몬스터 ──
    foreach (var id in new[] { 1102061, 230404002, 230505002, 230606002,
                                240107001, 240407301, 250208101 })
        Skill(id, E.Instant, T.NearestEnemy).Apply(DamageStun).Register();

    // ── ConeDamage 패턴 몬스터 ──
    foreach (var id in new[] { 230101002, 230404001, 230505001, 230606001, 280109001 })
        Skill(id, E.Instant, T.NearestEnemy).Apply(ConeDamage).Register();

    // ── SingleDamage 패턴 (fallthrough 몬스터 포함) ──
    foreach (var id in fallThroughMonsterIds)
        Skill(id, E.Instant, T.NearestEnemy).Apply(SingleDamage).Register();

    // ── 개별 선언이 필요한 몬스터 ──
    Skill(250108001, E.Channeling, T.NearestEnemy)  // 보스 탱커
        .Param(1, P.Int, 200f)
        .OnTick(SequentialLine(0, lineLength: 10,
            intervalMs: 200, repeatCount: 10))
        .Register();
}
```

**Preset 원칙**: 파라미터를 받지 않는 고정 패턴만.
`ApplyConeDamage(range: 2)` 같은 파라미터화 Preset은 만들지 않음 — inline이 더 명확.

### Tags 자동 추론

Build()에서 Actions 내용을 스캔하여 자동으로 Tags 추론:

```csharp
// SkillRecipeBuilder.Build() 내부:
TraitTag inferred = TraitTag.None;
for (int i = 0; i < _actions.Count; i++)
{
    switch (_actions[i].Effect)
    {
        case SkillEffectType.Damage: inferred |= TraitTag.Damage; break;
        case SkillEffectType.Heal: inferred |= TraitTag.Heal; break;
        case SkillEffectType.ApplyCC: inferred |= TraitTag.CC; break;
        case SkillEffectType.Shield: inferred |= TraitTag.Shield; break;
        case SkillEffectType.Knockback: inferred |= TraitTag.Knockback; break;
        case SkillEffectType.SpawnProjectile:
        case SkillEffectType.SpawnLinearProjectile: inferred |= TraitTag.Projectile; break;
        case SkillEffectType.MultiHit: inferred |= TraitTag.MultiHit; break;
        case SkillEffectType.ApplyBuff: inferred |= TraitTag.Buff; break;
        case SkillEffectType.ApplyDebuff: inferred |= TraitTag.Debuff; break;
        case SkillEffectType.RemoveDebuffs: inferred |= TraitTag.RemoveDebuffs; break;
    }
    if (_actions[i].AreaShape != SkillAreaShape.None) inferred |= TraitTag.AoE;
}
// ExecutionType 기반 추론
if (_execType == SkillExecutionType.Channeling) inferred |= TraitTag.Channeling;

recipe.Tags = _explicitTags | inferred;  // 수동 + 자동 합산
```

- Actions가 있는 Recipe → 자동 추론 (`.WithTags()` 없어도 동작)
- Actions가 null인 Custom 스킬 → 수동 `.WithTags()` 필수
- 수동 + 자동은 OR 합산
- **수동 보충이 필요한 태그**: `Teleport` (SkillEffectType에 대응 없음)

### Custom 스킬 Tags

Custom 스킬은 Recipe에 Actions 없이 ParamSlots만 있음. `.WithTags()`로 수동 부여:

```csharp
Skill(217433302, E.Channeling, T.NearestEnemy).Projectile()  // 미노
    .Param(1, P.Int, 200f)
    .WithTags(TraitTag.Damage | TraitTag.Projectile | TraitTag.MultiHit)
    .Register();

Skill(217263103, E.Instant, T.Self)                           // 루키다
    .Param(1, P.Int, 2f)
    .Param(2, P.Frames, 3f)
    .Param(3, P.Int, 10f)
    .WithTags(TraitTag.Buff)
    .Register();
```

### 기능 쿼리

```csharp
// 시너지: "AoE 스킬이면 데미지 20% 증가"
if (recipe.Tags.HasFlag(TraitTag.AoE)) { ... }

// 아이템: "CC 스킬의 지속시간 +1초"
if ((recipe.Tags & TraitTag.CC) != 0) { ... }

// 복합 쿼리: "데미지+CC 조합인 스킬"
var mask = TraitTag.Damage | TraitTag.CC;
if ((recipe.Tags & mask) == mask) { ... }
```

## 삭제 대상

| 대상 | 이유 |
|------|------|
| `SimSkillArchetype` enum | TraitTag로 대체 |
| `ClassifySkill()` (~40줄) | 개별 Recipe로 대체 |
| `ClassifyMonsterSkill()` (~55줄) | 개별 Recipe로 대체 |
| `BuildParams()` 아키타입 switch (~30줄) | Recipe가 모든 정보 포함 |
| `ApplySkillSpecificParams()` (~15줄) | Recipe 선언에 직접 포함 |
| `RegisterArchetypeRecipes()` (85줄) | Preset 함수로 대체 |
| `_archetypeRecipes` 딕셔너리 | 아키타입 폴백 경로 제거 |
| `SkillFactory.Archetypes.cs` 파일 | 전체 삭제 |
| `SkillParams.Param0~3, CCType, CCDurationFrames` | Recipe에서 직접 관리 (Phase 4) |
| `SimSkillGeneric.StoreParamOverrides()` | 위와 동일 (Phase 4) |
| `SkillExecuteContext` Override 필드 6개 | 위와 동일 (Phase 4) |
| `ActionExecutor.GetAreaRange()` override 로직 | Recipe 값 직접 사용 (Phase 4) |

## 유지 대상

| 대상 | 이유 |
|------|------|
| `SkillAction` struct (17필드) | 실행 단위로서 잘 동작, struct라서 미사용 필드 비용 없음 |
| `ActionExecutor` switch(effect) | 정당한 디스패치 패턴 |
| `SimSkillGeneric` | Recipe 소비자 역할 유지 |
| Custom 스킬 11개 | 복잡한 상태 로직은 Recipe로 표현 불가 |
| `SkillRecipeBuilder` | `.WithTags()`, `.Apply()` 메서드 추가 확장 |
| `BuildParams()` 공통 부분 | PowerPercent, DamageType, CooldownSeconds, ExtractSkillHitTimes, FaceTarget |
| `SkillFactory.Character.cs` | 플레이어 스킬 선언 (현행 파일 구조 유지) |
| `SkillFactory.Monster.cs` | 몬스터 스킬 선언 (Preset + 개별 선언 추가) |

## Initialize() 플로우 변경

**Before:**
```
Initialize()
  → RegisterCustomSkills() — 11개
  → for each SkillActive:
      → ClassifySkill(spec) → archetype           ← 삭제
      → BuildParams(spec, specList)
        → switch(archetype) 기본값                  ← 삭제
        → ApplySkillSpecificParams()               ← 삭제
        → Recipe.TargetRule로 FaceTarget
      → TryGetRecipe(id) → 개별 Recipe
      → TryGetByArchetype(archetype) → 폴백        ← 삭제
      → Register(id, SimSkillGeneric + recipe)
```

**After:**
```
Initialize()
  → RegisterCustomSkills() — 11개 (유지)
  → for each SkillActive:
      → BuildParams(spec, specList)
        → PowerPercent, DamageType, CooldownSeconds 추출
        → ExtractSkillHitTimes()
        → Recipe.TargetRule로 FaceTarget
      → TryGetRecipe(id) → 개별 Recipe (유일한 경로)
      → Register(id, SimSkillGeneric + recipe)
```

## 마이그레이션 계획

### Phase 1: 인프라 추가 (비파괴, 동작 변경 없음)

1. `TraitTag` [Flags] ulong enum 추가
2. `SkillRecipe`에 `Tags` 필드 추가
3. `SkillRecipeBuilder`에 `.WithTags()`, `.Apply()` 추가
4. `Build()`에 Tags 자동 추론 로직 추가
5. Custom 스킬 11개에 `.WithTags()` 추가 (Actions 없어서 자동 추론 불가, 수동 필수)
6. 나머지 기존 Recipe는 자동 추론으로 동작하므로 `.WithTags()` 선택적

### Phase 2: 아키타입 의존 스킬 전체 개별화

**플레이어** — 아키타입으로 분류되지만 개별 Recipe가 없는 3개:
1. 시이나 (215362202) — DamageCC 아키타입 + CCType=Silence override → 개별 Recipe 추가
2. 블린 (217243102) — DiamondAoE 아키타입 + Param0=2 override → 개별 Recipe 추가
3. 아란 (1406031) — Heal 아키타입 → 개별 Recipe 추가

**플레이어 fallthrough** — ClassifySkill() default → SingleDamage로 빠지는 스킬 전수 조사.
Character.cs에 개별 Recipe가 있으면 OK, 없으면 추가.

**몬스터:**
1. `ClassifyMonsterSkill()` 명시적 case들 → `SkillFactory.Monster.cs`에 개별 Recipe 선언으로 이동
2. Fallthrough 몬스터 전수 조사 — default → SingleDamage로 빠지는 스킬 식별 후 개별 Recipe 추가
3. Preset 함수로 공통 패턴 재사용 (DamageStun, SingleDamage, ConeDamage 등)

**검증**: 아키타입 Recipe 폴백 경로 비활성화 → 모든 스킬 동작 확인

### Phase 3: 아키타입 제거

1. `ClassifySkill()` 삭제
2. `BuildParams()` 아키타입 switch 삭제
3. `ApplySkillSpecificParams()` 삭제
4. `SimSkillArchetype` enum 삭제
5. `_archetypeRecipes` 딕셔너리 + `TryGetByArchetype()` 삭제
6. `SkillFactory.Archetypes.cs` 파일 삭제
7. 안전 검증: `Debug.Assert(_registry.ContainsKey(id) || TryGetRecipe(id, out _))`

### Phase 4: SkillParams Override 체인 제거

아키타입 제거 후, BuildParams가 Param0/CCType/등을 설정하는 경로가 사라짐.
결과적으로 Override 체인 전체가 죽은 코드:

| 죽은 코드 | 위치 |
|-----------|------|
| `SkillParams.Param0~3, CCType, CCDurationFrames` | SimSkillBase.cs |
| `StoreParamOverrides()` | SimSkillGeneric.cs |
| `_areaRangeOverride` 등 6개 필드 | SimSkillGeneric.cs |
| `SkillExecuteContext` Override 필드 6개 | ActionExecutor.cs |
| `GetAreaRange()` override 해석 | ActionExecutor.cs |

Phase 4 범위:
1. SkillParams에서 위 필드 삭제
2. SimSkillGeneric.StoreParamOverrides() 삭제
3. SkillExecuteContext Override 필드 삭제
4. ActionExecutor override 해석 → Recipe 값 직접 사용
5. **폴백 검증 필수**: Override 제거 후 Recipe의 action.AreaRange/action.RepeatCount가 올바른 값인지 확인 (예: MultiTargetHeal의 range=3, MultiHit의 count=3)
6. SimSkillBase의 CCType/CCDurationFrames/BuffStat 등 필드 → Custom 스킬 사용 여부 확인 후 정리

## SkillRecipeBuilder 변경사항

```csharp
// 추가 필드
private TraitTag _explicitTags;

// 추가 메서드
public SkillRecipeBuilder WithTags(TraitTag tags)
{
    _explicitTags |= tags;
    return this;
}

// ※ struct이지만 List 필드는 참조 공유되므로 동작 OK.
// Func 사용 이유: Action<T>은 반환값이 없어 체이닝 불가.
public SkillRecipeBuilder Apply(System.Func<SkillRecipeBuilder, SkillRecipeBuilder> preset)
    => preset(this);

// Build()에서 자동 추론 + 수동 합산
public SkillRecipe Build()
{
    TraitTag inferred = InferTags();
    return new SkillRecipe
    {
        ExecutionType = _execType,
        TargetRule = _targetRule,
        HasProjectile = _hasProjectile,
        Tags = _explicitTags | inferred,
        ParamSlots = _params.Count > 0 ? _params.ToArray() : null,
        Actions = _actions.Count > 0 ? _actions.ToArray() : null,
    };
}
```

## 예상 결과

### 코드량

| 항목 | Before | After |
|------|--------|-------|
| switch문 (ClassifySkill + BuildParams + ApplySpecific) | ~145줄 | 삭제 |
| RegisterArchetypeRecipes | 85줄 | 삭제 |
| 몬스터 개별 Recipe | 0 | ~80줄 (선언형) |
| TraitTag enum + 자동추론 | 0 | ~40줄 |
| Preset 함수 | 0 | ~20줄 |
| Phase 4 Override 제거 | - | ~-50줄 |

순감소 ~140줄, switch → 선언형 전환.

### 개발 경험

```
새 스킬 추가:
  Before: switch 2~3개 case 추가 + Recipe 선언
  After:  Recipe 선언 1곳. 끝.

기능 쿼리:
  Before: 불가능
  After:  recipe.Tags.HasFlag(TraitTag.AoE) — O(1)

기능별 검색:
  Before: 아키타입 enum으로 대략 분류 (실제 기능과 불일치)
  After:  TraitTag 비트 쿼리로 정확한 기능 조합 검색
```

## 제약조건

- Custom 스킬 11개의 실행 로직 변경 없음
- ActionExecutor switch(effect) 유지
- GC-free / 퀀텀 안전 유지 (TraitTag는 ulong enum)
- SkillAction struct 17필드 변경 없음
- 기존 스킬 동작 변경 없음 (순수 리팩토링)
- Preset은 파라미터 없는 고정 패턴만 (파라미터 있으면 inline 선언)
- 파일 조직은 현행 유지 (Character.cs / Monster.cs) — 기능별 분류는 TraitTag로

## 검토했으나 기각한 대안

### 기능별 파일 분리 (SkillFactory.DamageCC.cs 등)

**기각 이유**: 스킬은 여러 기능의 조합. 복합 스킬(Heal+Damage+Debuff)이 어느 파일에도 자연스럽게 속하지 않음. "주 기능"으로 분류하면 아키타입 라벨과 같은 문제 재발.

### 전면 데이터화 (SkillActive 스펙에 액션 구조 포함)

**기각 이유**: 기획자가 관리할 데이터 복잡도 폭증. C# Recipe 선언이 이미 선언형이고 컴파일 타임 검증까지 됨. Custom 스킬 11개는 어차피 코드. VFX 타이밍은 애니메이션 종속이라 데이터화해도 자동화 못함.

### SkillTrait struct (기능 블록 1급 객체)

**기각 이유**: 실제 설계에서 Trait struct는 사용되지 않음. Recipe.Tags 필드 + Build() 자동 추론이 같은 역할을 더 단순하게 수행. 불필요한 추상화 레이어.
