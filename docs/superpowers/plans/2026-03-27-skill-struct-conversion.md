# Skill System Class → Struct Conversion Plan

> **For agentic workers:** REQUIRED: Use superpowers:subagent-driven-development (if subagents available) or superpowers:executing-plans to implement this plan. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** SimSkillBase 계층(5개 class)을 `struct SimSkillInstance` + `enum SkillType` + `static SkillDispatcher`로 전환하여 GC-free, 캐시 친화적 스킬 시스템 구축.

**Architecture:** 현재 abstract class + virtual dispatch 패턴을 tagged union struct + static switch dispatch로 전환. Custom 4개의 로직은 각각 static class로 분리. SkillRecipe/int[] 등 불변 데이터는 managed 참조로 struct 내에 유지.

**Tech Stack:** Unity C#, IL2CPP compatible, no unsafe code

---

## File Structure

| 파일 | 역할 | 변경 |
|------|------|------|
| `SimSkillBase.cs` | struct 정의 + enum | **전면 재작성** → `SimSkillInstance` struct + `SkillType` enum |
| `SimSkillGeneric.cs` | Generic 스킬 로직 | **전면 재작성** → `static class GenericSkillLogic` |
| `Custom/SimSkillRukidaFoxfire.cs` | 루키다 로직 | **전면 재작성** → `static class RukidaSkillLogic` |
| `Custom/SimSkillAprilBarrage.cs` | 에이프릴 로직 | **전면 재작성** → `static class AprilSkillLogic` |
| `Custom/SimSkillEnkiWaveHeal.cs` | 엔키 로직 | **전면 재작성** → `static class EnkiSkillLogic` |
| `Custom/SimSkillAdriaExpand.cs` | 아드리아 로직 | **전면 재작성** → `static class AdriaSkillLogic` |
| `SkillDispatcher.cs` | static dispatch 허브 | **신규 생성** |
| `SkillFactory.cs` | 팩토리 (core) | **수정** — `Func<SimSkillBase>` → enum 기반 초기화 |
| `SkillSystem.cs` | 오케스트레이션 | **수정** — `var skill` → `ref var skill`, dispatch 호출 변경 |
| `Components.cs` | CombatMatchState | **수정** — `SimSkillBase[]` → `SimSkillInstance[]` |
| `IdleCombatSetup.cs` | Idle 모드 스킬 셋업 | **수정** — Create → Initialize 패턴 변경 |
| `Helpers/SkillCCHelper.cs` | CC 헬퍼 | **수정** — `var skill` → `ref var skill` |

---

## Chunk 1: Core Types (SimSkillInstance struct + SkillType enum + SkillDispatcher)

### Task 1: SkillType enum 정의

**Files:**
- Modify: `Assets/_Project/Scripts/InGame_New/Simulation/Combat/Skills/SimSkillBase.cs`

- [ ] **Step 1: SimSkillBase.cs 상단에 SkillType enum 추가**

```csharp
namespace CookApps.AutoChess
{
    /// <summary>스킬 구현 타입 (static dispatch key)</summary>
    public enum SkillType : byte
    {
        None = 0,
        Generic,
        Rukida,
        April,
        Enki,
        Adria,
    }
}
```

기존 `SkillParams` struct는 그대로 유지. `abstract class SimSkillBase`는 아직 삭제하지 않음 (Task 7에서 삭제).

- [ ] **Step 2: 컴파일 확인**

---

### Task 2: SimSkillInstance struct 작성

**Files:**
- Modify: `Assets/_Project/Scripts/InGame_New/Simulation/Combat/Skills/SimSkillBase.cs`

- [ ] **Step 1: SkillParams struct 아래에 SimSkillInstance struct 추가**

```csharp
/// <summary>
/// 스킬 인스턴스 (값 타입). 모든 스킬의 공통 상태 + Custom union 필드를 flat으로 보유.
/// 행위는 SkillDispatcher에서 SkillType 기반 static dispatch.
/// </summary>
public struct SimSkillInstance
{
    // ── Dispatch key ──
    public SkillType Type;
    public bool IsInitialized; // None과 초기화된 Generic 구분

    // ── 공통 (현 SimSkillBase 필드) ──
    public int SkillId;
    public int PowerPercent;
    public DamageType DamageType;
    public int CastFrames;
    public SkillTargetType TargetType;
    public CrowdControlType CCType;
    public int CCDurationFrames;
    public StatModType BuffStat;
    public int BuffValue;
    public int BuffDurationFrames;
    public int SecondaryPowerPercent;
    public int TargetCount;
    public int HitCount;
    public SkillExecutionType ExecutionType;
    public bool FaceTarget;
    public bool HasProjectile;
    public int[] SkillHitFrames;     // managed 참조 (유닛별 다른 길이)
    public int SkillClipFrames;

    // ── Generic 런타임 상태 ──
    public SkillRecipe Recipe;        // managed 참조 (불변, 공유)
    public int[] ParamValues;         // managed 참조 (인스턴스별)
    public int StartDelay;
    public int TickTimer;
    public int TickInterval;
    public int RemainingTicks;
    public int TickCount;
    public int WorldTickRate;
    public int CachedTargetId;
    public bool KnockbackHitWall;
    public int ProjectileArrivalTimer;
    public int CurrentPower;
    public int BounceCount;
    public int DecayPercent;
    public int CurrentHitFrameIndex;
    public int HitFrameTimer;
    public bool HasMultiHitFrames;
    public int PostCompleteTimer;
    public bool CompleteFired;
    public int DelayTimer;            // base의 _delayTimer

    // ── 히트 추적 ──
    public int[] HitIds;              // managed 참조 (new int[8])
    public int HitIdCount;

    // ── Custom: Rukida ──
    public int FoxFireIncrease;
    public int AtkSpeedRatePercent;

    // ── Custom: April ──
    public int Rate1, Rate2, Rate3;
    public int DirCol, DirRow;
    public bool Started;
    public int ClipEndTimer;
    public int HitIndex;

    // ── Custom: Enki ──
    public int HotDuration, HotInterval;
    public int PhaseTimer;
    public int ChannelFramesRemaining;
    public bool Fired, Channeling;
    public int CachedCasterCombatId, CachedAttack;
    public int StartRow, CenterCol, HalfWidth, WaveDirRow;

    // ── Custom: Adria ──
    public int DefScaleValue, StunDurationFrames;
    public int CurrentPhase;
    public bool Done;
    public long HitMask;              // 비트마스크 중복 방지

    // ── 읽기 전용 프로퍼티 ──
    public readonly bool IsChanneling => ExecutionType != SkillExecutionType.Instant;
    public readonly int FirstEffectFrame => SkillHitFrames != null && SkillHitFrames.Length > 0
        ? SkillHitFrames[0] : 0;

    public readonly int GetCastFrames()
    {
        if (ExecutionType == SkillExecutionType.DelayedApply ||
            ExecutionType == SkillExecutionType.Channeling)
            return 0;
        if (CastFrames > 0) return CastFrames;
        if (SkillHitFrames != null && SkillHitFrames.Length > 0) return SkillHitFrames[0];
        return 0;
    }

    public readonly int GetActionLockFrames()
    {
        if (SkillClipFrames > 0) return SkillClipFrames;
        int cf = GetCastFrames();
        if (cf > 0) return cf;
        return FirstEffectFrame > 0 ? FirstEffectFrame : 1;
    }

    /// <summary>공통 파라미터 초기화 (SkillParams → struct 필드)</summary>
    public void InitializeBase(SkillParams p)
    {
        SkillId = p.SkillId;
        PowerPercent = p.PowerPercent;
        DamageType = p.DamageType;
        CastFrames = p.CastFrames;
        CCType = p.CCType;
        CCDurationFrames = p.CCDurationFrames;
        BuffStat = p.BuffStat;
        BuffValue = p.BuffValue;
        BuffDurationFrames = p.BuffDurationFrames;
        SecondaryPowerPercent = p.SecondaryPowerPercent;
        TargetType = p.TargetType;
        TargetCount = p.TargetCount <= 0 ? 1 : p.TargetCount;
        HitCount = p.HitCount <= 0 ? 1 : p.HitCount;
        SkillHitFrames = p.SkillHitFrames;
        SkillClipFrames = p.SkillClipFrames;
        FaceTarget = p.FaceTarget;
        IsInitialized = true;
        DelayTimer = -1;
        HitIds = new int[8];
    }

    /// <summary>풀 반환 시 런타임 상태 초기화</summary>
    public void Reset()
    {
        // Generic
        StartDelay = 0; TickTimer = 0; TickInterval = 0;
        RemainingTicks = 0; TickCount = 0;
        CachedTargetId = CombatUnit.InvalidId;
        KnockbackHitWall = false; ProjectileArrivalTimer = 0;
        CurrentPower = 0; BounceCount = 0; DecayPercent = 0;
        HitIdCount = 0; CurrentHitFrameIndex = 0; HitFrameTimer = 0;
        HasMultiHitFrames = false; PostCompleteTimer = 0; CompleteFired = false;
        DelayTimer = -1;
        if (HitIds != null)
            for (int i = 0; i < HitIds.Length; i++) HitIds[i] = CombatUnit.InvalidId;

        // April
        Started = false; ClipEndTimer = 0; HitIndex = 0; DirCol = 0; DirRow = 0;
        // Enki
        Channeling = false; Fired = false; PhaseTimer = 0; ChannelFramesRemaining = 0;
        // Adria
        CurrentPhase = 0; Done = false; HitMask = 0;
    }
}
```

- [ ] **Step 2: 컴파일 확인**

---

### Task 3: SkillDispatcher 생성

**Files:**
- Create: `Assets/_Project/Scripts/InGame_New/Simulation/Combat/Skills/SkillDispatcher.cs`

- [ ] **Step 1: static dispatch 허브 작성**

```csharp
using System.Collections.Generic;
using CookApps.AutoBattler;

namespace CookApps.AutoChess
{
    /// <summary>
    /// 스킬 static dispatch 허브.
    /// SkillType enum 기반 switch로 각 스킬 로직의 static 메서드 호출.
    /// </summary>
    public static class SkillDispatcher
    {
        public static void InitializeFromSpec(ref SimSkillInstance skill, SkillParams baseParams,
            List<SkillActive> specList, int tickRate)
        {
            skill.InitializeBase(baseParams);
            switch (skill.Type)
            {
                case SkillType.Generic: GenericSkillLogic.InitializeFromSpec(ref skill, specList, tickRate); break;
                case SkillType.Rukida:  RukidaSkillLogic.InitializeFromSpec(ref skill, specList, tickRate); break;
                case SkillType.April:   AprilSkillLogic.InitializeFromSpec(ref skill, specList, tickRate); break;
                case SkillType.Enki:    EnkiSkillLogic.InitializeFromSpec(ref skill, specList, tickRate); break;
                case SkillType.Adria:   AdriaSkillLogic.InitializeFromSpec(ref skill, specList, tickRate); break;
            }
        }

        public static int SelectTarget(ref SimSkillInstance skill, CombatMatchState state, ref CombatUnit caster)
        {
            switch (skill.Type)
            {
                case SkillType.Generic: return GenericSkillLogic.SelectTarget(ref skill, state, ref caster);
                case SkillType.Rukida:  return caster.CombatId;
                case SkillType.April:   return TargetingSystem.FindNearestEnemy(state, ref caster);
                case SkillType.Enki:    return EnkiSkillLogic.SelectTarget(ref skill, state, ref caster);
                case SkillType.Adria:   return caster.CombatId;
                default:                return CombatUnit.InvalidId;
            }
        }

        public static void Execute(ref SimSkillInstance skill, CombatMatchState state,
            ref CombatUnit caster, int targetCombatId, ref DeterministicRNG rng)
        {
            switch (skill.Type)
            {
                case SkillType.Generic: GenericSkillLogic.Execute(ref skill, state, ref caster, targetCombatId, ref rng); break;
                case SkillType.Rukida:  RukidaSkillLogic.Execute(ref skill, state, ref caster, targetCombatId, ref rng); break;
                case SkillType.April:   AprilSkillLogic.Execute(ref skill, state, ref caster, targetCombatId, ref rng); break;
                case SkillType.Enki:    EnkiSkillLogic.Execute(ref skill, state, ref caster, targetCombatId, ref rng); break;
                case SkillType.Adria:   AdriaSkillLogic.Execute(ref skill, state, ref caster, targetCombatId, ref rng); break;
            }
        }

        public static bool OnChannelTick(ref SimSkillInstance skill, CombatMatchState state,
            ref CombatUnit caster, ref DeterministicRNG rng)
        {
            switch (skill.Type)
            {
                case SkillType.Generic: return GenericSkillLogic.OnChannelTick(ref skill, state, ref caster, ref rng);
                case SkillType.April:   return AprilSkillLogic.OnChannelTick(ref skill, state, ref caster, ref rng);
                case SkillType.Enki:    return EnkiSkillLogic.OnChannelTick(ref skill, state, ref caster, ref rng);
                case SkillType.Adria:   return AdriaSkillLogic.OnChannelTick(ref skill, state, ref caster, ref rng);
                default:                return false; // Rukida는 Instant → 채널링 없음
            }
        }

        /// <summary>DelayedApply 기본 구현 (base.OnChannelTick 대체)</summary>
        public static bool OnChannelTickDelayedApply(ref SimSkillInstance skill, CombatMatchState state,
            ref CombatUnit caster, ref DeterministicRNG rng)
        {
            if (skill.ExecutionType != SkillExecutionType.DelayedApply) return false;

            if (skill.DelayTimer <= 0)
                skill.DelayTimer = skill.SkillHitFrames != null && skill.SkillHitFrames.Length > 0
                    ? skill.SkillHitFrames[0] : 10;

            skill.DelayTimer--;
            if (skill.DelayTimer > 0) return true;

            // Generic만 DelayedApply를 사용 (ApplySkillEffect 호출)
            GenericSkillLogic.ApplySkillEffect(ref skill, state, ref caster, ref rng);
            return false;
        }
    }
}
```

- [ ] **Step 2: 컴파일은 Task 4~6 완료 후 (참조하는 static class가 아직 없으므로)**

---

## Chunk 2: Custom 스킬 로직 → static class 전환

### Task 4: GenericSkillLogic (SimSkillGeneric → static)

**Files:**
- Rewrite: `Assets/_Project/Scripts/InGame_New/Simulation/Combat/Skills/SimSkillGeneric.cs`

- [ ] **Step 1: 전면 재작성**

기계적 전환 규칙:
1. `class SimSkillGeneric : SimSkillBase` → `static class GenericSkillLogic`
2. 모든 인스턴스 메서드 → `static` + 첫 파라미터 `ref SimSkillInstance skill`
3. 모든 `this.필드` 접근 → `skill.필드`
4. `_recipe` → `skill.Recipe`
5. `_paramValues` → `skill.ParamValues`
6. `_cachedTargetId` → `skill.CachedTargetId`
7. `_knockbackHitWall` → `skill.KnockbackHitWall`
8. 나머지 `_필드` → `skill.대응필드` (SimSkillInstance의 필드명 참조)
9. `base.OnChannelTick()` → `SkillDispatcher.OnChannelTickDelayedApply()`
10. `MakeContext` 내 `HitIds = _hitIds` → `HitIds = skill.HitIds`
11. `PowerPercent` → `skill.PowerPercent` 등 protected 필드도 동일

주의: `DispatchActions`, `ExecuteActionWithSpecialHandling`, `DispatchActionsForHitFrame`, `HandleProjectileArrival`, `InitChanneling`, `MakeContext`, `CheckCondition`, `FindAoERadius` — 모든 내부 메서드에 `ref SimSkillInstance skill` 전파.

`SetRecipe` → `GenericSkillLogic.SetRecipe(ref skill, recipe)` 또는 직접 `skill.Recipe = recipe`.

- [ ] **Step 2: 컴파일 확인 (Task 5~6과 함께)**

---

### Task 5: Custom 4개 → static class 전환

**Files:**
- Rewrite: `Custom/SimSkillRukidaFoxfire.cs` → `static class RukidaSkillLogic`
- Rewrite: `Custom/SimSkillAprilBarrage.cs` → `static class AprilSkillLogic`
- Rewrite: `Custom/SimSkillEnkiWaveHeal.cs` → `static class EnkiSkillLogic`
- Rewrite: `Custom/SimSkillAdriaExpand.cs` → `static class AdriaSkillLogic`

- [ ] **Step 1: 각 파일에 동일한 기계적 전환 적용**

규칙 (Task 4와 동일):
1. `class SimSkillXxx : SimSkillBase` → `static class XxxSkillLogic`
2. 모든 인스턴스 메서드 → `static` + `ref SimSkillInstance skill`
3. `_필드` → `skill.대응필드`
4. `base.Initialize(baseParams)` 호출 제거 (SkillDispatcher가 InitializeBase 호출)
5. 상수(`MaxFoxFires`, `DefaultMoveInterval` 등)는 각 static class 내에 유지

각 Custom의 필드 매핑:

**Rukida:**
- `_foxFireIncrease` → `skill.FoxFireIncrease`
- `_buffDurationFrames` → `skill.BuffDurationFrames`
- `_atkSpeedRatePercent` → `skill.AtkSpeedRatePercent`

**April:**
- `_rate1/2/3` → `skill.Rate1/2/3`
- `_remainingHits` → `skill.RemainingTicks` (재사용)
- `_tickInterval` → `skill.TickInterval`
- `_tickTimer` → `skill.TickTimer`
- `_dirCol/_dirRow` → `skill.DirCol/DirRow`
- `_started` → `skill.Started`
- `_startDelay` → `skill.StartDelay`
- `_clipEndTimer` → `skill.ClipEndTimer`
- `_hitIndex` → `skill.HitIndex`

**Enki:**
- `_hotDuration/_hotInterval` → `skill.HotDuration/HotInterval`
- `_phaseTimer` → `skill.PhaseTimer`
- `_channelFramesRemaining` → `skill.ChannelFramesRemaining`
- `_fired/_channeling` → `skill.Fired/Channeling`
- `_cachedCasterCombatId/_cachedAttack` → `skill.CachedCasterCombatId/CachedAttack`
- `_startRow/_centerCol/_halfWidth/_waveDirRow` → 대응 필드

**Adria:**
- `_defScaleValue` → `skill.DefScaleValue`
- `_stunDurationFrames` → `skill.StunDurationFrames`
- `_currentPhase` → `skill.CurrentPhase`
- `_phaseTimer` → `skill.PhaseTimer`
- `_done` → `skill.Done`
- `_hitMask` → `skill.HitMask`

- [ ] **Step 2: 전체 컴파일 확인**

---

## Chunk 3: 호출부 전환 (Factory, System, State)

### Task 6: SkillFactory 전환

**Files:**
- Modify: `Assets/_Project/Scripts/InGame_New/Simulation/Combat/Skills/SkillFactory.cs`

- [ ] **Step 1: 팩토리 패턴 변경**

```csharp
// 변경 전
private static readonly Dictionary<int, System.Func<SimSkillBase>> _registry = new();
public static void Register(int skillId, System.Func<SimSkillBase> creator) { ... }
public static SimSkillBase Create(int skillId) { ... }

// 변경 후
private static readonly Dictionary<int, SkillType> _typeRegistry = new();
public static void RegisterType(int skillId, SkillType type) { _typeRegistry[skillId] = type; }

public static SimSkillInstance Create(int skillId)
{
    var skill = new SimSkillInstance();
    skill.Type = _typeRegistry.TryGetValue(skillId, out var type) ? type : SkillType.Generic;
    return skill;
}
```

`RegisterCustomSkills` 변경:
```csharp
private static void RegisterCustomSkills()
{
    RegisterType(217653505, SkillType.Enki);
    RegisterType(217333202, SkillType.April);
    RegisterType(217523403, SkillType.Adria);
    RegisterType(217263103, SkillType.Rukida);
}
```

`Initialize` 내부의 `Register(id, () => ...)` 루프 변경:
- 커스텀이 아닌 스킬은 전부 `SkillType.Generic`으로 등록
- `_registry` 딕셔너리 삭제, `_typeRegistry`로 대체
- Recipe 설정은 Create 후 별도로: `skill.Recipe = captured;`

- [ ] **Step 2: 컴파일 확인**

---

### Task 7: CombatMatchState + SkillSystem + 참조부 전환

**Files:**
- Modify: `Assets/_Project/Scripts/InGame_New/Simulation/Data/Components.cs:803-824`
- Modify: `Assets/_Project/Scripts/InGame_New/Simulation/Combat/SkillSystem.cs` (전체)
- Modify: `Assets/_Project/Scripts/InGame_New/Adapter/IdleCombatSetup.cs:284-307`
- Modify: `Assets/_Project/Scripts/InGame_New/Simulation/Combat/Skills/Helpers/SkillCCHelper.cs:20-26`

- [ ] **Step 1: CombatMatchState 배열 타입 변경**

```csharp
// Components.cs:803
// 변경 전
public SimSkillBase[] Skills;

// 변경 후
public SimSkillInstance[] Skills;

// Components.cs:824 Create()
// 변경 전
Skills = new SimSkillBase[MaxCombatUnits],

// 변경 후
Skills = new SimSkillInstance[MaxCombatUnits],
```

- [ ] **Step 2: SkillSystem.cs — 핵심 var → ref 전환**

`SetupSkills` (line 19):
```csharp
// 변경 전
var skill = SkillFactory.Create(unit.SkillSpecId);
if (skill == null) continue;
// ...
state.Skills[i] = skill;

// 변경 후
state.Skills[i] = SkillFactory.Create(unit.SkillSpecId);
ref var skill = ref state.Skills[i];
if (!skill.IsInitialized && skill.Type == SkillType.None) continue;

if (SkillFactory.TryGetParams(unit.SkillSpecId, out var skillParams))
{
    SkillFactory.TryGetSpecList(unit.SkillSpecId, out var specList);
    SkillDispatcher.InitializeFromSpec(ref skill, skillParams, specList, world.TickRate);
    unit.MaxMana = (int)System.Math.Ceiling(skillParams.CooldownSeconds * world.Config.DefaultManaRegenPerSec);
}
else
{
    skill.InitializeBase(new SkillParams
    {
        SkillId = unit.SkillSpecId,
        PowerPercent = 200,
        DamageType = DamageType.Magical,
    });
}
```

`TryCast` (line 66):
```csharp
// 변경 전
var skill = state.Skills[unitIndex];
if (skill == null) { ... }

// 변경 후
ref var skill = ref state.Skills[unitIndex];
if (!skill.IsInitialized) { unit.CurrentMana = 0; return false; }
```

모든 `skill.Execute(...)` → `SkillDispatcher.Execute(ref skill, ...)`.
모든 `skill.SelectTarget(...)` → `SkillDispatcher.SelectTarget(ref skill, ...)`.
`skill.IsChanneling` → `skill.IsChanneling` (struct 프로퍼티, 변경 없음).
`skill.HasProjectile` → `skill.HasProjectile` (struct 필드, 변경 없음).
`skill.GetCastFrames()` → `skill.GetCastFrames()` (readonly 메서드, 변경 없음).
`skill.FaceTarget` → `skill.FaceTarget` (변경 없음).

`TickCasting` (line 141):
```csharp
// 변경 전
var skill = state.Skills[unitIndex];

// 변경 후
ref var skill = ref state.Skills[unitIndex];
```

`skill.OnChannelTick(state, ref unit, ref rng)` → `SkillDispatcher.OnChannelTick(ref skill, state, ref unit, ref rng)`.

`Cleanup` (line 216-227):
```csharp
// 변경 전
if (state.Skills[i] != null) { state.Skills[i].Reset(); state.Skills[i] = null; }

// 변경 후
if (state.Skills[i].IsInitialized) { state.Skills[i].Reset(); state.Skills[i] = default; }
```

- [ ] **Step 3: IdleCombatSetup.cs 전환**

```csharp
// 변경 전 (line 291-307)
var skill = SkillFactory.Create(unit.SkillSpecId);
if (skill == null) return;
// ...
state.Skills[unitIndex] = skill;

// 변경 후
state.Skills[unitIndex] = SkillFactory.Create(unit.SkillSpecId);
ref var skill = ref state.Skills[unitIndex];
if (!skill.IsInitialized && skill.Type == SkillType.None) return;

if (SkillFactory.TryGetParams(unit.SkillSpecId, out var skillParams))
{
    SkillFactory.TryGetSpecList(unit.SkillSpecId, out var specList);
    SkillDispatcher.InitializeFromSpec(ref skill, skillParams, specList, tickRate);
}
else
{
    skill.InitializeBase(new SkillParams { ... });
}
```

- [ ] **Step 4: SkillCCHelper.cs 전환**

```csharp
// 변경 전 (line 20)
var skill = idx >= 0 ? state.Skills[idx] : null;
if (skill != null && skill.IsChanneling)
{
    shouldRestoreMana = skill.FirstEffectFrame <= 0
        || target.SkillCastTimer < skill.FirstEffectFrame;
}

// 변경 후
if (idx >= 0 && state.Skills[idx].IsInitialized)
{
    ref var skill = ref state.Skills[idx];
    if (skill.IsChanneling)
    {
        shouldRestoreMana = skill.FirstEffectFrame <= 0
            || target.SkillCastTimer < skill.FirstEffectFrame;
    }
}
```

- [ ] **Step 5: 컴파일 확인**

---

### Task 8: abstract class SimSkillBase 삭제

**Files:**
- Modify: `Assets/_Project/Scripts/InGame_New/Simulation/Combat/Skills/SimSkillBase.cs`

- [ ] **Step 1: `abstract class SimSkillBase` 전체 삭제**

파일에는 `SkillParams` struct + `SkillType` enum + `SimSkillInstance` struct만 남김.

- [ ] **Step 2: 전체 컴파일 확인 — 모든 SimSkillBase 참조가 제거되었는지 확인**

잔여 참조가 있다면 이 단계에서 컴파일 에러로 발견됨.

---

## Chunk 4: 검증

### Task 9: 동작 검증

- [ ] **Step 1: Unity Editor에서 InGame_New 전투 실행**

확인 항목:
1. Instant 스킬 (시이나, 아란) — 즉시 발동 후 Idle 복귀
2. DelayedApply 스킬 (필리아, 하티, 블린) — 딜레이 후 효과 적용
3. Channeling 스킬 (클레이, 엘리스, 마리에) — 틱 반복 + 종료
4. Custom 4개 (루키다/에이프릴/엔키/아드리아) — 각각 고유 로직 정상
5. Multi-hitframe (오데트, 시라유키) — 복수 키프레임 순차 발동
6. 투사체 (아트레시아, 라키유, 베인, 미노) — 발사 + 도착 + 체이닝

- [ ] **Step 2: CombatLogger 활성화 후 로그 비교**

전환 전/후 동일 시드로 전투 실행 → 로그 diff 없어야 함 (결정론적 시뮬레이션).

---

## 변경 요약

| 카테고리 | 변경 전 | 변경 후 |
|----------|---------|---------|
| 스킬 타입 | `abstract class` + 5 서브클래스 | `struct` 1개 + `enum` |
| 디스패치 | virtual (vtable) | static switch |
| 메모리 | 힙 할당, GC 대상 | 값 타입 배열, GC-free |
| 팩토리 | `Func<SimSkillBase>` → `new` | `SkillType` enum → struct 초기화 |
| 참조 패턴 | `var skill = Skills[i]` (class 참조) | `ref var skill = ref Skills[i]` (struct ref) |
| null 체크 | `skill == null` | `skill.IsInitialized` |
