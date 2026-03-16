# Synergy Effect Mapping Implementation Plan

> **For agentic workers:** REQUIRED: Use superpowers:subagent-driven-development (if subagents available) or superpowers:executing-plans to implement this plan. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** InGame_New SynergySystem에 원소 시너지 스탯 매핑을 완성하고, asterism 시너지를 위한 행동 프레임워크(ISynergyBehavior)를 구축한다.

**Architecture:** 2계층 구조. 1계층은 `AutoChessSpecAdapter.BuildSynergySpecs()`에서 `ISpecSynergyData` → `SynergyEffect[]` 변환으로 원소 시너지 완성. 2계층은 `ISynergyBehavior` 인터페이스 + `SynergyBehaviorFactory`로 asterism의 복잡한 행동(쿨감, 쉴드, 흡수 등)을 확장 가능하게 설계. 기존 `CombatTraitBase`/`TraitSystem` 패턴을 시너지 전용으로 미러링.

**Tech Stack:** Unity C#, InGame_New Simulation layer (namespace: CookApps.AutoChess)

**Spec:** `docs/superpowers/specs/2026-03-16-synergy-effect-mapping-design.md`

---

## File Map

| 파일 | 변경 | 역할 |
|---|---|---|
| `Adapter/AutoChessSpecAdapter.cs` | 수정 | `BuildSynergySpecs()` → Effects 매핑 로직 추가 |
| `Simulation/Data/Components.cs` | 수정 | `SynergySpec.HasBehavior` 필드, `CombatMatchState` 행동 배열 |
| `Simulation/Synergy/ISynergyBehavior.cs` | 신규 | 시너지 행동 인터페이스 |
| `Simulation/Synergy/SynergyBehaviorFactory.cs` | 신규 | SynergyType → 행동 클래스 팩토리 |
| `Simulation/Synergy/SynergySystem.cs` | 수정 | `ApplyBehaviors()` 메서드 추가 |
| `Simulation/Core/GameLoopSystem.cs` | 수정 | `ApplyBehaviors()` 호출 삽입 |

---

## Chunk 1: 원소 시너지 스탯 매핑

### Task 1: AutoChessSpecAdapter — BuildEffects 헬퍼 + 스위치 매핑

**Files:**
- Modify: `Assets/_Project/Scripts/InGame_New/Adapter/AutoChessSpecAdapter.cs:118-165`

- [ ] **Step 1: MapCoverType 헬퍼 추가**

`BuildSynergySpecs()` 아래에 다음 메서드 추가:

```csharp
private static SynergyTarget MapCoverType(SynergyCoverType cover)
{
    switch (cover)
    {
        case SynergyCoverType.SYNERGY_ELEMENTAL:
        case SynergyCoverType.SYNERGY_STELLA:
            return SynergyTarget.TraitUnits;
        case SynergyCoverType.SQUAD_ALL:
        case SynergyCoverType.KNIGHT_ALL:
            return SynergyTarget.AllAllies;
        default:
            return SynergyTarget.AllAllies;
    }
}
```

- [ ] **Step 2: BuildEffects 메서드 추가**

`MapCoverType()` 아래에 다음 메서드 추가.
핵심 설계: **SynergyType별 스위치 1개**에서 "어떤 EffectType에 어떤 값을" 결정.
스펙 테이블이 바뀌면 이 스위치만 수정.

```csharp
private static SynergyEffect[] BuildEffects(SynergyType type, ISpecSynergyData data)
{
    var target = MapCoverType(data.synergy_cover_type);
    int v1 = data.effect_stat_value_1;
    int v2 = data.effect_stat_value_2;

    switch (type)
    {
        case SynergyType.FIRE:
            // 공격력 {v1}% 상승
            return new[]
            {
                new SynergyEffect { Type = SynergyEffectType.BonusAttackPercent, Target = target, ValuePercent = v1 },
            };

        case SynergyType.WIND:
            // 공격속도 {v1}%, 회피율 {v2}%
            return new[]
            {
                new SynergyEffect { Type = SynergyEffectType.BonusAttackSpeedPercent, Target = target, ValuePercent = v1 },
                new SynergyEffect { Type = SynergyEffectType.DodgeChance, Target = target, Value = v2 },
            };

        case SynergyType.LIGHTNING:
            // 크리티컬 확률 {v1}%, 크리티컬 데미지 {v2}%
            return new[]
            {
                new SynergyEffect { Type = SynergyEffectType.BonusCritChance, Target = target, Value = v1 },
                new SynergyEffect { Type = SynergyEffectType.BonusCritMultiplier, Target = target, Value = v2 },
            };

        case SynergyType.EARTH:
            // 관통력 {v1}% (물리+마법 동일), 블록률 {v2}%
            return new[]
            {
                new SynergyEffect { Type = SynergyEffectType.BonusAdReduce, Target = target, Value = v1 },
                new SynergyEffect { Type = SynergyEffectType.BonusApReduce, Target = target, Value = v1 },
            };

        case SynergyType.WATER:
            // HP {v1}%, 방어력 {v2}
            return new[]
            {
                new SynergyEffect { Type = SynergyEffectType.BonusHPPercent, Target = target, ValuePercent = v1 },
                new SynergyEffect { Type = SynergyEffectType.BonusDef, Target = target, Value = v2 },
            };

        default:
            // asterism 등: 스탯 매핑 없음 (행동 클래스에서 처리)
            return System.Array.Empty<SynergyEffect>();
    }
}
```

- [ ] **Step 3: BuildSynergySpecs에서 BuildEffects 호출**

기존 TODO 라인을 교체:

```csharp
// 변경 전 (148-149행):
// TODO: 시너지 효과 매핑 (기존 EffectCode 기반 → 데이터 기반 전환 필요)
Effects = System.Array.Empty<SynergyEffect>(),

// 변경 후:
Effects = BuildEffects(synergyType, data),
```

- [ ] **Step 4: 컴파일 확인**

Unity Editor 또는 IDE에서 컴파일 에러 없는지 확인.
`SynergyCoverType`이 `CookApps.AutoBattler` 네임스페이스에 있으므로 using 추가 필요 여부 확인.

- [ ] **Step 5: 커밋**

```bash
git add Assets/_Project/Scripts/InGame_New/Adapter/AutoChessSpecAdapter.cs
git commit -m "feat(synergy): 원소 시너지 스탯 매핑 — BuildEffects 구현"
```

---

## Chunk 2: ISynergyBehavior 프레임워크

### Task 2: ISynergyBehavior 인터페이스 생성

**Files:**
- Create: `Assets/_Project/Scripts/InGame_New/Simulation/Synergy/ISynergyBehavior.cs`

- [ ] **Step 1: 인터페이스 파일 작성**

`CombatTraitBase`의 콜백 패턴을 시너지 전용으로 미러링.
차이점: 유닛 단위가 아니라 **팀 단위**로 동작 (시너지는 팀 시너지).

```csharp
namespace CookApps.AutoChess
{
    /// <summary>
    /// Asterism 시너지 행동 인터페이스.
    /// 원소 시너지(스탯만)와 달리 복잡한 전투 중 행동을 정의.
    /// 팀 단위로 동작 (유닛 단위 X → CombatTraitBase와 구분).
    /// </summary>
    public abstract class SynergyBehaviorBase
    {
        public int TraitId;
        public byte Tier;
        public byte TeamIndex;

        /// <summary>전투 시작 시 1회 (스탯 적용 후)</summary>
        public virtual void OnCombatStart(CombatMatchState state) { }

        /// <summary>매 전투 틱</summary>
        public virtual void OnTick(CombatMatchState state) { }

        /// <summary>아군 유닛이 기본공격 시</summary>
        public virtual void OnAllyAttack(CombatMatchState state,
            ref CombatUnit attacker, ref CombatUnit target) { }

        /// <summary>아군 유닛이 피격 시</summary>
        public virtual void OnAllyDamaged(CombatMatchState state,
            ref CombatUnit victim, ref CombatUnit attacker, int damage) { }

        /// <summary>아군 유닛이 적 처치 시</summary>
        public virtual void OnAllyKill(CombatMatchState state,
            ref CombatUnit killer, ref CombatUnit victim) { }

        /// <summary>나가는 데미지 보정 (해당 특성 유닛만)</summary>
        public virtual int ModifyOutgoingDamage(CombatMatchState state,
            ref CombatUnit attacker, ref CombatUnit target, int damage, DamageType damageType) => damage;

        /// <summary>들어오는 데미지 보정 (해당 특성 유닛만)</summary>
        public virtual int ModifyIncomingDamage(CombatMatchState state,
            ref CombatUnit attacker, ref CombatUnit target, int damage, DamageType damageType) => damage;

        /// <summary>리셋</summary>
        public virtual void Reset() { }

        /// <summary>해당 유닛이 이 시너지의 특성을 가지고 있는지</summary>
        protected bool HasTrait(ref CombatUnit unit)
        {
            return (unit.TraitFlags & (1 << TraitId)) != 0;
        }

        /// <summary>해당 유닛이 이 시너지의 팀인지</summary>
        protected bool IsMyTeam(ref CombatUnit unit)
        {
            return unit.TeamIndex == TeamIndex;
        }
    }
}
```

- [ ] **Step 2: meta 파일 자동 생성 확인**

Unity Editor가 실행 중이면 자동 생성. 아니면 수동 커밋에서 제외.

---

### Task 3: SynergyBehaviorFactory 생성

**Files:**
- Create: `Assets/_Project/Scripts/InGame_New/Simulation/Synergy/SynergyBehaviorFactory.cs`

- [ ] **Step 1: 팩토리 파일 작성**

```csharp
namespace CookApps.AutoChess
{
    /// <summary>
    /// SynergyType + Tier → SynergyBehaviorBase 인스턴스 생성.
    /// asterism 시너지 구현 시 여기에 케이스 추가.
    /// </summary>
    public static class SynergyBehaviorFactory
    {
        public static SynergyBehaviorBase Create(SynergyType type, byte tier, int traitId, byte teamIndex)
        {
            SynergyBehaviorBase behavior = type switch
            {
                // === asterism 시너지 구현 시 여기에 추가 ===
                // SynergyType.NOBLESSE => new SynergyBehaviorNoblesse(),
                // SynergyType.TROUBLESHOOTER => new SynergyBehaviorTroubleShooter(),
                // SynergyType.SUPERNOVA => new SynergyBehaviorSupernova(),
                _ => null,
            };

            if (behavior != null)
            {
                behavior.TraitId = traitId;
                behavior.Tier = tier;
                behavior.TeamIndex = teamIndex;
            }

            return behavior;
        }

        /// <summary>해당 SynergyType이 행동 클래스를 필요로 하는지</summary>
        public static bool NeedsBehavior(SynergyType type)
        {
            return type switch
            {
                // 원소(1-6): 스탯만, 행동 없음
                SynergyType.NORMAL or
                SynergyType.FIRE or
                SynergyType.WIND or
                SynergyType.LIGHTNING or
                SynergyType.EARTH or
                SynergyType.WATER => false,
                // asterism(7+): 행동 필요
                _ => true,
            };
        }
    }
}
```

---

### Task 4: Components.cs — SynergySpec.HasBehavior + CombatMatchState 확장

**Files:**
- Modify: `Assets/_Project/Scripts/InGame_New/Simulation/Data/Components.cs:341-349, 735-765`

- [ ] **Step 1: SynergySpec에 HasBehavior 추가**

```csharp
// 변경 전 (341-349):
public struct SynergySpec
{
    public int TraitId;
    public TraitCategory Category;
    public SynergyTier[] Tiers;

    public bool IsValid => Tiers != null && Tiers.Length > 0;
}

// 변경 후:
public struct SynergySpec
{
    public int TraitId;
    public TraitCategory Category;
    public SynergyTier[] Tiers;
    public bool HasBehavior;       // asterism처럼 행동 클래스가 필요한 시너지

    public bool IsValid => Tiers != null && Tiers.Length > 0;
}
```

- [ ] **Step 2: CombatMatchState에 시너지 행동 배열 추가**

`CombatMatchState` 클래스의 상태효과 블록 아래에 추가:

```csharp
// 시너지 행동 (asterism 전용)
public const int MaxSynergyBehaviors = 8;
public SynergyBehaviorBase[] SynergyBehaviors;  // [MaxSynergyBehaviors]
public int SynergyBehaviorCount;
```

- [ ] **Step 3: CombatMatchState 생성 시 배열 초기화 확인**

`CombatSetupSystem` 또는 `CombatMatchState` 생성자에서 `SynergyBehaviors = new SynergyBehaviorBase[MaxSynergyBehaviors]` 초기화가 필요한지 확인하고 추가.

---

### Task 5: AutoChessSpecAdapter — HasBehavior 설정

**Files:**
- Modify: `Assets/_Project/Scripts/InGame_New/Adapter/AutoChessSpecAdapter.cs:156-161`

- [ ] **Step 1: BuildSynergySpecs에서 HasBehavior 설정**

```csharp
// 변경 전:
result.Add(new SynergySpec
{
    TraitId = synergyTypeInt,
    Category = category,
    Tiers = tiers,
});

// 변경 후:
result.Add(new SynergySpec
{
    TraitId = synergyTypeInt,
    Category = category,
    Tiers = tiers,
    HasBehavior = SynergyBehaviorFactory.NeedsBehavior(synergyType),
});
```

---

### Task 6: SynergySystem — ApplyBehaviors 메서드

**Files:**
- Modify: `Assets/_Project/Scripts/InGame_New/Simulation/Synergy/SynergySystem.cs:133` (ApplyEffects 뒤)

- [ ] **Step 1: ApplyBehaviors 추가**

`ApplyEffects()` 메서드 아래에 추가:

```csharp
// ── 전투 시작 시 시너지 행동 등록 ──

/// <summary>
/// HasBehavior인 시너지의 행동 클래스를 생성하여 CombatMatchState에 등록.
/// ApplyEffects() 이후 호출.
/// </summary>
public static void ApplyBehaviors(GameWorld world, CombatMatchState state,
    byte playerIndex, byte teamIndex)
{
    if (!world.Config.EnableSynergy) return;
    if (world.SynergySpecs == null) return;

    var synergy = world.Synergies[playerIndex];

    for (int t = 0; t < world.SynergySpecCount; t++)
    {
        ref var spec = ref world.SynergySpecs[t];
        if (!spec.IsValid || !spec.HasBehavior) continue;

        int traitId = spec.TraitId;
        byte tier = synergy.GetTraitTier(traitId);
        if (tier == 0) continue;

        var behavior = SynergyBehaviorFactory.Create(
            (SynergyType)traitId, tier, traitId, teamIndex);
        if (behavior == null) continue;

        if (state.SynergyBehaviorCount < CombatMatchState.MaxSynergyBehaviors)
        {
            state.SynergyBehaviors[state.SynergyBehaviorCount++] = behavior;
        }
    }

    // 등록된 행동의 OnCombatStart 호출
    for (int i = 0; i < state.SynergyBehaviorCount; i++)
    {
        state.SynergyBehaviors[i].OnCombatStart(state);
    }
}

// ── 시너지 행동 콜백 디스패치 ──

public static void InvokeOnTick(CombatMatchState state)
{
    for (int i = 0; i < state.SynergyBehaviorCount; i++)
        state.SynergyBehaviors[i].OnTick(state);
}

public static void InvokeOnAllyAttack(CombatMatchState state,
    ref CombatUnit attacker, ref CombatUnit target)
{
    for (int i = 0; i < state.SynergyBehaviorCount; i++)
    {
        var b = state.SynergyBehaviors[i];
        if (b.TeamIndex == attacker.TeamIndex)
            b.OnAllyAttack(state, ref attacker, ref target);
    }
}

public static void InvokeOnAllyDamaged(CombatMatchState state,
    ref CombatUnit victim, ref CombatUnit attacker, int damage)
{
    for (int i = 0; i < state.SynergyBehaviorCount; i++)
    {
        var b = state.SynergyBehaviors[i];
        if (b.TeamIndex == victim.TeamIndex)
            b.OnAllyDamaged(state, ref victim, ref attacker, damage);
    }
}

public static void InvokeOnAllyKill(CombatMatchState state,
    ref CombatUnit killer, ref CombatUnit victim)
{
    for (int i = 0; i < state.SynergyBehaviorCount; i++)
    {
        var b = state.SynergyBehaviors[i];
        if (b.TeamIndex == killer.TeamIndex)
            b.OnAllyKill(state, ref killer, ref victim);
    }
}
```

---

### Task 7: GameLoopSystem — ApplyBehaviors 호출 삽입

**Files:**
- Modify: `Assets/_Project/Scripts/InGame_New/Simulation/Core/GameLoopSystem.cs:246, 266-267`

- [ ] **Step 1: PvE 경로에 ApplyBehaviors 추가 (246행 뒤)**

```csharp
// 변경 전:
SynergySystem.ApplyEffects(world, matchState, 0, 0);
SkillSystem.SetupSkills(matchState, world);

// 변경 후:
SynergySystem.ApplyEffects(world, matchState, 0, 0);
SynergySystem.ApplyBehaviors(world, matchState, 0, 0);
SkillSystem.SetupSkills(matchState, world);
```

- [ ] **Step 2: PvP 경로에 ApplyBehaviors 추가 (266-268행 뒤)**

```csharp
// 변경 전:
SynergySystem.ApplyEffects(world, matchState, match.PlayerA, 0);
SynergySystem.ApplyEffects(world, matchState, match.PlayerB, 1);
SkillSystem.SetupSkills(matchState, world);

// 변경 후:
SynergySystem.ApplyEffects(world, matchState, match.PlayerA, 0);
SynergySystem.ApplyEffects(world, matchState, match.PlayerB, 1);
SynergySystem.ApplyBehaviors(world, matchState, match.PlayerA, 0);
SynergySystem.ApplyBehaviors(world, matchState, match.PlayerB, 1);
SkillSystem.SetupSkills(matchState, world);
```

- [ ] **Step 3: 커밋**

```bash
git add Assets/_Project/Scripts/InGame_New/Simulation/Synergy/ISynergyBehavior.cs
git add Assets/_Project/Scripts/InGame_New/Simulation/Synergy/SynergyBehaviorFactory.cs
git add Assets/_Project/Scripts/InGame_New/Simulation/Synergy/SynergySystem.cs
git add Assets/_Project/Scripts/InGame_New/Simulation/Data/Components.cs
git add Assets/_Project/Scripts/InGame_New/Adapter/AutoChessSpecAdapter.cs
git add Assets/_Project/Scripts/InGame_New/Simulation/Core/GameLoopSystem.cs
git commit -m "feat(synergy): ISynergyBehavior 프레임워크 + ApplyBehaviors 파이프라인"
```

---

## Chunk 3: 컴파일 및 통합 검증

### Task 8: 전체 컴파일 + 런타임 검증

- [ ] **Step 1: Unity 컴파일 확인**

모든 파일 저장 후 Unity Editor에서 컴파일 에러/경고 확인.
특히 `SynergyCoverType` 네임스페이스 접근 확인 (`CookApps.AutoBattler` vs 전역).

- [ ] **Step 2: 시너지 UI 확인**

게임 실행 → ClassicBattle 모드 → 같은 원소 캐릭터 2-3명 배치.
시너지 아이콘 활성화 확인 + 전투 시작 시 스탯 변동 확인.

- [ ] **Step 3: 디버그 로그 추가 (임시)**

`SynergySystem.ApplyEffects()`의 `ApplyStatEffect()` 안에 임시 로그:
```csharp
Debug.Log($"[Synergy] {effect.Type} → unit[{unitIndex}] value={effect.Value} percent={effect.ValuePercent}");
```
전투 시작 시 원소 시너지 효과가 올바르게 적용되는지 확인 후 제거.

- [ ] **Step 4: 최종 커밋**

```bash
git add -u
git commit -m "feat(synergy): 원소 시너지 스탯 매핑 완성 + asterism 행동 프레임워크"
```
