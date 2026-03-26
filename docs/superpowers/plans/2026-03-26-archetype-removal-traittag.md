# 아키타입 제거 + TraitTag Implementation Plan

> **For agentic workers:** REQUIRED: Use superpowers:subagent-driven-development (if subagents available) or superpowers:executing-plans to implement this plan. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** SimSkillArchetype enum과 관련 switch문을 삭제하고, 모든 스킬을 개별 Recipe 선언으로 통일. TraitTag 비트마스크로 기능 쿼리 지원.

**Architecture:** 4-Phase 마이그레이션. Phase 1(인프라 추가, 비파괴) → Phase 2(아키타입 의존 스킬 개별화) → Phase 3(아키타입 제거) → Phase 4(Override 체인 정리). 각 Phase는 독립 커밋 가능하며, 중간 상태에서도 기존 동작 유지.

**Tech Stack:** Unity C# (IL2CPP), struct/enum 기반 GC-free 시뮬레이션

**Spec:** `docs/superpowers/specs/2026-03-26-skill-trait-system-design.md`

---

## Task 1: TraitTag enum 추가 + SkillRecipe.Tags 필드 (Phase 1)

**Files:**
- Modify: `Assets/_Project/Scripts/InGame_New/Simulation/Data/Enums.cs`
- Modify: `Assets/_Project/Scripts/InGame_New/Simulation/Combat/Skills/Recipe/SkillRecipe.cs`

- [ ] **Step 1: Enums.cs에 TraitTag enum 추가**

SimSkillArchetype enum 근처(264줄 이후)에 추가:

```csharp
[System.Flags]
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
}
```

- [ ] **Step 2: SkillRecipe.cs에 Tags 필드 추가**

SkillRecipe 클래스(13줄)에 HasProjectile 아래 추가:

```csharp
/// <summary>기능 태그 비트마스크 (시너지/아이템 쿼리용)</summary>
public TraitTag Tags;
```

- [ ] **Step 3: 컴파일 확인**

Run: Unity 에디터에서 컴파일 에러 없는지 확인.
기존 코드는 Tags를 참조하지 않으므로 동작 변경 없음.

- [ ] **Step 4: 커밋**

```
feat: add TraitTag [Flags] enum and SkillRecipe.Tags field
```

---

## Task 2: SkillRecipeBuilder에 WithTags, Apply, 자동 추론 추가 (Phase 1)

**Files:**
- Modify: `Assets/_Project/Scripts/InGame_New/Simulation/Combat/Skills/SkillFactory.cs` (SkillRecipeBuilder inner struct, 195~353줄)

- [ ] **Step 1: _explicitTags 필드 + WithTags() 메서드 추가**

SkillRecipeBuilder struct에 필드와 메서드 추가:

```csharp
private TraitTag _explicitTags;

public SkillRecipeBuilder WithTags(TraitTag tags)
{
    _explicitTags |= tags;
    return this;
}
```

- [ ] **Step 2: Apply() 메서드 추가**

```csharp
public SkillRecipeBuilder Apply(System.Func<SkillRecipeBuilder, SkillRecipeBuilder> preset)
    => preset(this);
```

- [ ] **Step 3: Build()에 자동 추론 로직 추가**

기존 Build() 메서드를 수정. return 전에 InferTags() 호출하여 Tags에 합산:

```csharp
public SkillRecipe Build()
{
    return new SkillRecipe
    {
        ExecutionType = _execType,
        TargetRule = _targetRule,
        HasProjectile = _hasProjectile,
        Tags = _explicitTags | InferTags(),
        ParamSlots = _params.Count > 0 ? _params.ToArray() : null,
        Actions = _actions.Count > 0 ? _actions.ToArray() : null,
    };
}

private TraitTag InferTags()
{
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
    if (_execType == SkillExecutionType.Channeling) inferred |= TraitTag.Channeling;
    return inferred;
}
```

- [ ] **Step 4: 컴파일 확인**

기존 Recipe 선언은 WithTags 호출 안 해도 자동 추론으로 Tags 세팅됨. 동작 변경 없음.

- [ ] **Step 5: 커밋**

```
feat: add WithTags, Apply, auto-infer to SkillRecipeBuilder
```

---

## Task 3: Custom 스킬 11개에 WithTags 추가 (Phase 1)

**Files:**
- Modify: `Assets/_Project/Scripts/InGame_New/Simulation/Combat/Skills/SkillFactory.Character.cs` (123~204줄, 커스텀 클래스 유지 섹션)

- [ ] **Step 1: Custom 스킬 Recipe에 .WithTags() 추가**

Custom 스킬은 Actions가 null이므로 자동 추론 불가. 수동 태그 부여:

```csharp
// 테토라: Damage + Knockback + CC
Skill(217413301, E.DelayedApply, T.NearestEnemy)
    .Param(1, P.Int, 200f)
    .Param(3, P.Int, 200f)
    .WithTags(TraitTag.Damage | TraitTag.Knockback | TraitTag.CC)
    .Register();

// 루키다: Buff
Skill(217263103, E.Instant, T.Self)
    .Param(1, P.Int, 2f)
    .Param(2, P.Frames, 3f)
    .Param(3, P.Int, 10f)
    .WithTags(TraitTag.Buff)
    .Register();

// 라키유: Projectile + Debuff
Skill(217353203, E.Channeling, T.NearestEnemy).Projectile()
    .Param(1, P.Frames, 3f)
    .Param(2, P.Int, 50f)
    .Param(3, P.Int, 30f)
    .WithTags(TraitTag.Projectile | TraitTag.Debuff | TraitTag.AoE)
    .Register();

// 미노: Damage + Projectile + MultiHit
Skill(217433302, E.Channeling, T.NearestEnemy).Projectile()
    .Param(1, P.Int, 200f)
    .WithTags(TraitTag.Damage | TraitTag.Projectile | TraitTag.MultiHit)
    .Register();

// 베인: Damage + Projectile + Buff
Skill(217363204, E.Channeling, T.NearestEnemy).Projectile()
    .Param(1, P.Int, 200f)
    .Param(2, P.Int, 20f)
    .Param(3, P.Frames, 3f)
    .Param(4, P.Int, 30f)
    .Param(5, P.Int, 5f)
    .WithTags(TraitTag.Damage | TraitTag.Projectile | TraitTag.Buff)
    .Register();

// 마리에: Damage + Teleport + Debuff
Skill(217563405, E.Channeling, T.HighestAttackEnemy)
    .Param(2, P.Int, 200f)
    .Param(1, P.Int, 4f)
    .Param(3, P.Frames, 3f)
    .Param(4, P.Int, 30f)
    .WithTags(TraitTag.Damage | TraitTag.Teleport | TraitTag.Debuff | TraitTag.MultiHit)
    .Register();

// 에이프릴: Damage + AoE
Skill(217333202, E.Channeling, T.NearestEnemy)
    .Param(2, P.Int, 100f)
    .Param(1, P.Int, 10f)
    .Param(3, P.Int, 75f)
    .Param(4, P.Int, 50f)
    .WithTags(TraitTag.Damage | TraitTag.AoE)
    .Register();

// 엔키: Heal
Skill(217653505, E.Channeling, T.Self)
    .Param(1, P.Int, 200f)
    .Param(2, P.Frames, 6f)
    .Param(3, P.Int, 50f)
    .WithTags(TraitTag.Heal)
    .Register();

// 오데트: Damage + Teleport + Debuff + AoE
Skill(217613501, E.Channeling, T.NearestEnemy)
    .Param(1, P.Int, 200f)
    .Param(2, P.Frames, 3f)
    .Param(3, P.Int, 30f)
    .WithTags(TraitTag.Damage | TraitTag.Teleport | TraitTag.Debuff | TraitTag.AoE)
    .Register();

// 아드리아: Damage + AoE
Skill(217523403, E.Channeling, T.Self)
    .Param(1, P.Int, 200f)
    .Param(2, P.Int, 100f)
    .Param(3, P.Frames, 2f)
    .WithTags(TraitTag.Damage | TraitTag.AoE)
    .Register();

// 시라유키: Damage + Teleport + Buff
Skill(217663506, E.Channeling, T.LowestHPEnemy)
    .Param(2, P.Int, 200f)
    .Param(1, P.Frames, 3f)
    .Param(3, P.Frames, 3f)
    .Param(4, P.Int, 30f)
    .WithTags(TraitTag.Damage | TraitTag.Teleport | TraitTag.Buff)
    .Register();
```

- [ ] **Step 2: 컴파일 확인**

- [ ] **Step 3: 커밋**

```
feat: add WithTags to all 11 custom skill recipes
```

---

## Task 4: 아키타입 의존 플레이어 스킬 개별 Recipe 추가 (Phase 2)

**Files:**
- Modify: `Assets/_Project/Scripts/InGame_New/Simulation/Combat/Skills/SkillFactory.Character.cs`
- Reference: `Assets/_Project/Scripts/InGame_New/Simulation/Combat/Skills/SkillFactory.SpecAdapter.cs` (ClassifySkill 136~139줄, ApplySkillSpecificParams 223~237줄)
- Reference: `Assets/_Project/Scripts/InGame_New/Simulation/Combat/Skills/SkillFactory.Archetypes.cs` (아키타입 Recipe 정의)

- [ ] **Step 1: 시이나 (215362202) 개별 Recipe 추가**

현재: ClassifySkill → DamageCC, ApplySkillSpecificParams → CCType=Silence, CCDuration=specList[2]
Character.cs의 RegisterPlayerRecipes()에 추가:

```csharp
// ── 시이나: Damage + CC(Silence) ──
Skill(215362202, E.Instant, T.NearestEnemy)
    .Param(1, P.Int, 200f)           // [0] 데미지 배율
    .Param(2, P.Frames, 3f)          // [1] 침묵 지속
    .OnCast(Damage(paramIndex: 0))
    .OnCast(CC(CrowdControlType.Silence, durationParamIndex: 1))
    .Register();
```

- [ ] **Step 2: 블린 (217243102) 개별 Recipe 추가**

현재: ClassifySkill → DiamondAoE, ApplySkillSpecificParams → Param0=2
Archetypes.cs의 DiamondAoE Recipe를 참고하여 range=2로:

```csharp
// ── 블린: Diamond AoE ──
Skill(217243102, E.DelayedApply, T.NearestEnemy)
    .Param(1, P.Int, 200f)
    .AtHit(Vfx(0, V.AtGridPos))
    .AtHit(AreaVfx(V.AreaEffect, 2))
    .AtHit(AreaVfx(V.PerTileInDiamond, 2, vfxIndex: 1))
    .AtHit(Damage(filter: F.EnemiesInArea, area: S.Diamond, range: 2))
    .Register();
```

- [ ] **Step 3: 아란 (1406031) 개별 Recipe 추가**

현재: ClassifySkill → Heal (단일 대상 힐)

```csharp
// ── 아란: 단일 대상 Heal ──
Skill(1406031, E.Instant, T.LowestHPAlly)
    .Param(1, P.Int, 200f)
    .OnCast(Heal(paramIndex: 0))
    .Register();
```

- [ ] **Step 4: 플레이어 fallthrough 전수 조사**

ClassifySkill()에서 Custom/DamageCC/DiamondAoE/Heal에 해당하지 않는 플레이어 스킬 중,
Character.cs에 개별 Recipe가 없는 스킬 식별. 해당 스킬에 SingleDamage Recipe 추가:

```csharp
Skill(id, E.Instant, T.NearestEnemy)
    .OnCast(Damage())
    .Register();
```

- [ ] **Step 5: 컴파일 + 인게임 검증**

시이나/블린/아란 스킬이 기존과 동일하게 동작하는지 확인.
(이 시점에서 이 3개는 개별 Recipe + 아키타입 Recipe 둘 다 등록됨. Initialize()에서 개별 Recipe가 우선이므로 정상 동작.)

- [ ] **Step 6: 커밋**

```
feat: add individual recipes for Siina, Blin, Aran (archetype-dependent players)
```

---

## Task 5: 몬스터 스킬 개별 Recipe 추가 + Preset (Phase 2)

**Files:**
- Modify: `Assets/_Project/Scripts/InGame_New/Simulation/Combat/Skills/SkillFactory.Monster.cs`
- Reference: `Assets/_Project/Scripts/InGame_New/Simulation/Combat/Skills/SkillFactory.SpecAdapter.cs` (ClassifyMonsterSkill 159~214줄)

- [ ] **Step 1: Preset 함수 추가**

Monster.cs 상단에 Preset 함수 정의:

```csharp
using static CookApps.AutoChess.SkillFactory.SkillRecipeBuilder;

// ── Preset 함수 (파라미터 없는 고정 패턴만) ──
static SkillRecipeBuilder DamageStun(SkillRecipeBuilder b)
    => b.OnCast(Damage()).OnCast(CC(CrowdControlType.Stun));

static SkillRecipeBuilder ApplySingleDamage(SkillRecipeBuilder b)
    => b.OnCast(Damage());

static SkillRecipeBuilder ApplyConeDamage(SkillRecipeBuilder b)
    => b.OnCast(Damage(filter: F.EnemiesInArea, area: S.Line, range: 2));

static SkillRecipeBuilder ApplyMultiHit(SkillRecipeBuilder b)
    => b.OnCast(MultiHit());

static SkillRecipeBuilder ApplyMultiTargetHeal(SkillRecipeBuilder b)
    => b.OnCast(Heal(filter: F.LowestHpAllies, range: 3));
```

- [ ] **Step 2: ClassifyMonsterSkill() case들을 개별 Recipe로 이동**

RegisterMonsterRecipes()에 추가 (기존 보스탱커 유지 + 새로운 몬스터들):

```csharp
// ── DamageCC ──
foreach (var id in new[] { 1102061, 230404002, 230505002, 230606002,
                            240107001, 240407301, 250208101 })
    Skill(id, E.Instant, T.NearestEnemy).Apply(DamageStun).Register();

// ── ConeDamage ──
foreach (var id in new[] { 230101002, 230404001, 230505001, 230606001, 280109001 })
    Skill(id, E.Instant, T.NearestEnemy).Apply(ApplyConeDamage).Register();

// ── DiamondAoE (맨허튼 1) ──
foreach (var id in new[] { 230202003, 230606003 })
    Skill(id, E.DelayedApply, T.NearestEnemy)
        .AtHit(Vfx(0, SkillVfxPlacement.AtGridPos))
        .AtHit(AreaVfx(SkillVfxPlacement.AreaEffect, 1))
        .AtHit(AreaVfx(SkillVfxPlacement.PerTileInDiamond, 1, vfxIndex: 1))
        .AtHit(Damage(filter: F.EnemiesInArea, area: S.Diamond, range: 1))
        .Register();

// ── PatternDamage ──
foreach (var id in new[] { 1103041, 1203021, 230505003, 250608501, 280109002 })
    Skill(id, E.Instant, T.BestAoETarget)
        .OnCast(Damage(filter: F.EnemiesInArea, area: S.Circle, range: 1))
        .OnCast(AreaCC(CrowdControlType.Stun, S.Circle, 1))
        .Register();

// ── LineDamage ──
foreach (var id in new[] { 1104081, 230404004, 230505004, 230606004, 240107002 })
    Skill(id, E.DelayedApply, T.NearestEnemy).Projectile()
        .AtHit(SpawnLinearProjectile())
        .Register();

// ── TeleportStrike ──
foreach (var id in new[] { 1202091, 240407302, 250108002, 250108003 })
    Skill(id, E.Instant, T.NearestEnemy)
        .OnCast(Damage(filter: F.EnemiesInArea, area: S.Circle, range: 1))
        .OnCast(AreaCC(CrowdControlType.Stun, S.Circle, 1))
        .Register();

// ── MultiHit ──
foreach (var id in new[] { 1105031, 230404005, 230505005 })
    Skill(id, E.Instant, T.NearestEnemy).Apply(ApplyMultiHit).Register();

// ── MultiTargetHeal ──
foreach (var id in new[] { 1106041, 230404006, 230505006, 230606006 })
    Skill(id, E.Instant, T.LowestHPAlly).Apply(ApplyMultiTargetHeal).Register();
```

- [ ] **Step 3: Fallthrough 몬스터 전수 조사**

Initialize() 실행 시 로그를 추가하여 아키타입 폴백 경로를 타는 몬스터 식별:

```csharp
// 임시 검증 코드 (Phase 3 전에 제거)
if (!TryGetRecipe(id, out _) && archetype != SimSkillArchetype.Custom)
    Debug.Log($"[SkillFactory] Fallthrough skill: {id} → {archetype}");
```

식별된 스킬에 SingleDamage Recipe 추가:
```csharp
foreach (var id in new[] { /* fallthrough IDs */ })
    Skill(id, E.Instant, T.NearestEnemy).Apply(ApplySingleDamage).Register();
```

- [ ] **Step 4: 아키타입 폴백 비활성화 테스트**

Initialize()에서 `TryGetByArchetype` 경로를 임시 주석 처리하고 인게임 검증.
모든 몬스터 스킬이 정상 동작하는지 확인.

- [ ] **Step 5: 커밋**

```
feat: add individual recipes for all monster skills with presets
```

---

## Task 6: 아키타입 제거 (Phase 3)

**Files:**
- Modify: `Assets/_Project/Scripts/InGame_New/Simulation/Combat/Skills/SkillFactory.cs`
- Modify: `Assets/_Project/Scripts/InGame_New/Simulation/Combat/Skills/SkillFactory.SpecAdapter.cs`
- Delete: `Assets/_Project/Scripts/InGame_New/Simulation/Combat/Skills/SkillFactory.Archetypes.cs` + `.meta`
- Modify: `Assets/_Project/Scripts/InGame_New/Simulation/Data/Enums.cs`

- [ ] **Step 1: SkillFactory.cs에서 아키타입 관련 코드 제거**

```csharp
// 삭제: _archetypeRecipes 딕셔너리 (27줄)
private static readonly Dictionary<SimSkillArchetype, SkillRecipe> _archetypeRecipes = new();

// 삭제: TryGetByArchetype (71~72줄)
private static bool TryGetByArchetype(...)

// 삭제: DefineArchetype (184~185줄)
private static void DefineArchetype(...)

// 삭제: ArchetypeBuilder (180~181줄)
private static SkillRecipeBuilder ArchetypeBuilder(...)

// 삭제: static constructor에서 RegisterArchetypeRecipes() 호출 (31줄)

// 수정: Initialize()에서 TryGetByArchetype 폴백 경로 제거 (131~140줄)
// archetype 변수 사용부 제거, ClassifySkill 호출 제거
```

Initialize() 수정 후:
```csharp
// 커스텀 스킬이 이미 등록되어 있으면 스킵
if (_registry.ContainsKey(id)) continue;

// 개별 Recipe로 등록
if (TryGetRecipe(id, out var recipe))
{
    var captured = recipe;
    Register(id, () =>
    {
        var skill = new SimSkillGeneric();
        skill.SetRecipe(captured);
        return skill;
    });
}
```

- [ ] **Step 2: SkillFactory.SpecAdapter.cs 대폭 축소**

```csharp
// 삭제: ClassifySkill() 전체 (112~148줄)
// 삭제: IsMonsterSkill() (154~157줄)
// 삭제: ClassifyMonsterSkill() 전체 (159~214줄)
// 삭제: ApplySkillSpecificParams() 전체 (220~239줄)

// BuildParams() 수정: archetype 파라미터 및 switch 분기 제거
// 유지: PowerPercent 추출, DamageType, CooldownSeconds, ExtractSkillHitTimes, FaceTarget
```

BuildParams 수정 후:
```csharp
private static SkillParams BuildParams(SkillActive spec, List<SkillActive> specList, int tickRate)
{
    var dmgType = spec.atk_type == AtkType.AP ? DamageType.Magical : DamageType.Physical;

    int powerPercent = 0;
    if (specList != null)
    {
        for (int i = 1; i < specList.Count; i++)
        {
            if (specList[i].skill_value_type == SkillValueType.PERCENT)
            {
                powerPercent = Mathf.RoundToInt(specList[i].base_rate);
                break;
            }
        }
    }

    var p = new SkillParams
    {
        SkillId = spec.skill_group_id,
        PowerPercent = powerPercent > 0 ? powerPercent : 200,
        DamageType = dmgType,
        CastFrames = 0,
        TargetCount = 1,
        HitCount = 1,
        TargetType = SkillTargetType.NearestEnemy,
        WorldTickRate = tickRate,
        CooldownSeconds = specList != null && specList.Count > 0
            ? specList[0].base_rate : 0f,
    };

    ExtractSkillHitTimes(ref p, spec.prefab_id, tickRate);

    // Recipe TargetRule로 FaceTarget 확정
    if (TryGetRecipe(spec.skill_group_id, out var recipe))
    {
        p.TargetType = recipe.TargetRule;
        p.FaceTarget = recipe.TargetRule != SkillTargetType.Self
            && recipe.TargetRule != SkillTargetType.LowestHPAlly;
    }

    return p;
}
```

- [ ] **Step 3: SkillFactory.Archetypes.cs 삭제**

파일 + .meta 삭제.

- [ ] **Step 4: SimSkillArchetype enum 삭제**

Enums.cs에서 SimSkillArchetype enum 제거.
다른 파일에서 SimSkillArchetype 참조가 있으면 제거.

- [ ] **Step 5: 안전 검증 추가**

Initialize()에 Debug.Assert 추가:
```csharp
#if UNITY_EDITOR
if (!_registry.ContainsKey(id) && !TryGetRecipe(id, out _))
    Debug.LogError($"[SkillFactory] Skill {id} has no recipe and no custom creator");
#endif
```

- [ ] **Step 6: 컴파일 + 인게임 전체 검증**

모든 캐릭터/몬스터 스킬이 정상 동작하는지 확인.

- [ ] **Step 7: 커밋**

```
refactor: remove SimSkillArchetype enum and all classification switch statements
```

---

## Task 7: SkillParams Override 체인 제거 (Phase 4)

**Files:**
- Modify: `Assets/_Project/Scripts/InGame_New/Simulation/Combat/Skills/SimSkillBase.cs` (SkillParams struct)
- Modify: `Assets/_Project/Scripts/InGame_New/Simulation/Combat/Skills/SimSkillGeneric.cs`
- Modify: `Assets/_Project/Scripts/InGame_New/Simulation/Combat/Skills/Recipe/ActionExecutor.cs`

- [ ] **Step 1: Custom 스킬의 SimSkillBase 필드 사용 여부 조사**

Grep으로 Custom 스킬 11개가 CCType, CCDurationFrames, BuffStat, BuffValue, BuffDurationFrames, SecondaryPowerPercent, Param0~3를 직접 참조하는지 확인.
참조하는 필드는 유지, 참조 없는 필드만 삭제 대상.

- [ ] **Step 2: SkillParams에서 아키타입 전용 필드 삭제**

Custom 스킬이 사용하지 않는 필드만 삭제:
```csharp
// 삭제 후보 (Custom 미사용 확인 후):
// Param0, Param1, Param2, Param3
// CCType, CCDurationFrames (Custom이 직접 사용하면 유지)
```

- [ ] **Step 3: SimSkillGeneric 정리**

```csharp
// 삭제: StoreParamOverrides() 메서드 전체
// 삭제: _areaRangeOverride, _ccDurationFramesOverride, _ccTypeOverride,
//       _targetCountOverride, _hitCountOverride 필드
// 수정: InitializeFromSpec()에서 StoreParamOverrides 호출 제거
// 수정: MakeContext()에서 Override 필드 제거
```

- [ ] **Step 4: SkillExecuteContext Override 필드 제거**

ActionExecutor.cs에서:
```csharp
// 삭제: CCDurationOverride, CCTypeOverride, AreaRangeOverride,
//       TargetCountOverride, HitCountOverride 필드
// 삭제: GetAreaRange() 헬퍼 (action.AreaRange 직접 사용)
```

- [ ] **Step 5: ActionExecutor 각 Execute 메서드에서 Override 해석 제거**

```csharp
// ExecuteCC: ctx.CCDurationOverride → action.SecondaryParamIndex만 사용
// ExecuteHeal(LowestHpAllies): ctx.TargetCountOverride → action.AreaRange 사용
// ExecuteMultiHit: ctx.HitCountOverride → action.RepeatCount 사용
// ExecuteDamage/etc: GetAreaRange() → action.AreaRange 직접 사용
```

- [ ] **Step 6: 폴백 값 검증**

모든 몬스터/플레이어 Recipe에서:
- MultiTargetHeal의 Heal action에 range=3이 명시되어 있는지
- MultiHit의 MultiHit action에 RepeatCount=3이 있거나 기본값이 올바른지
- AoE action의 AreaRange가 올바른지

- [ ] **Step 7: 컴파일 + 인게임 검증**

- [ ] **Step 8: 커밋**

```
refactor: remove SkillParams override chain (dead code after archetype removal)
```
