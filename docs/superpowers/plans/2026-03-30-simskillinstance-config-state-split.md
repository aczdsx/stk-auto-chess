# SimSkillInstance Config/State 분리 + Union 리팩토링

> **For agentic workers:** REQUIRED: Use superpowers:subagent-driven-development (if subagents available) or superpowers:executing-plans to implement this plan. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** SimSkillInstance의 초기화 후 불변 필드(Config)와 런타임 변경 필드(State)를 분리하고, 타입별 커스텀 State를 union으로 합쳐 메모리 낭비를 제거한다.

**Architecture:** CombatMatchState가 `SkillConfig[]`(초기화 후 읽기전용)와 `SkillState[]`(매 시전마다 Reset)를 별도 배열로 보유. 타입별 커스텀 런타임 필드는 `SkillState` 내 `SkillCustomState`를 `StructLayout(Explicit)`로 union 처리. `int[]` 참조 타입은 Config에 격리하여 State를 blittable에 가깝게 유지.

**Tech Stack:** Unity C#, `System.Runtime.InteropServices.StructLayout`

---

## 현재 구조 분석

### SimSkillInstance 필드 분류

**Config (초기화 후 불변) — 총 ~20개 필드 + 참조 3개:**
- Dispatch: `Type`, `IsInitialized`
- 공통: `SkillId`, `PowerPercent`, `DamageType`, `CastFrames`, `TargetType`, `CCType`, `CCDurationFrames`, `BuffStat`, `BuffValue`, `BuffDurationFrames`, `SecondaryPowerPercent`, `TargetCount`, `HitCount`, `FaceTarget`, `ExecutionType`, `HasProjectile`, `SkillClipFrames`, `WorldTickRate`
- 참조(힙): `Recipe` (class), `ParamValues[]`, `SkillHitFrames[]`
- Rukida Config: `FoxFireIncrease`, `AtkSpeedRatePercent`
- April Config: `Rate1`, `Rate2`, `Rate3`
- Enki Config: `HotDuration`, `HotInterval`
- Adria Config: `DefScaleValue`, `StunDurationFrames`

**State (런타임 변경) — 총 ~35개 필드:**
- 공통 타이머: `TickTimer`, `TickInterval`, `RemainingTicks`, `TickCount`, `StartDelay`, `ProjectileArrivalTimer`, `CurrentHitFrameIndex`, `HitFrameTimer`, `HasMultiHitFrames`, `PostCompleteTimer`, `CompleteFired`, `DelayTimer`
- 타겟/히트: `CachedTargetId`, `HitIds[]`(내용 변경), `HitIdCount`, `CurrentPower`, `BounceCount`, `DecayPercent`, `KnockbackHitWall`
- 위치: `SavedGridCol`, `SavedGridRow`
- April State: `DirCol`, `DirRow`, `Started`, `ClipEndTimer`, `HitIndex`
- Enki State: `PhaseTimer`, `ChannelFramesRemaining`, `Fired`, `Channeling`, `CachedCasterCombatId`, `CachedAttack`, `StartRow`, `CenterCol`, `HalfWidth`, `WaveDirRow`
- Adria State: `CurrentPhase`, `Done`, `HitMask`
- Dash State: `DashTilesRemaining`, `DashDirCol`, `DashDirRow`, `DashHitDamage`, `DashStunFrames`, `DashFramesPerTile`, `DashOvershootFrames`, `DashReturnFrames`, `DashOvershootEase`, `DashReturnEase`

### 문제점
1. 모든 타입의 필드가 flat → 사용하지 않는 필드가 대부분 (32개 인스턴스 × 낭비)
2. `int[]` 참조가 struct 안에 있어 struct의 의미 약화
3. 커스텀 스킬 추가 시마다 SimSkillInstance가 비대해짐
4. Reset()이 Config까지 건드림

### 파일 구조 (변경 대상)

| 파일 | 변경 | 역할 |
|---|---|---|
| `SimSkillBase.cs` | **전면 재작성** | `SkillConfig`, `SkillState`, `SkillCustomState` 정의 |
| `Components.cs:803` | 수정 | `Skills[]` → `SkillConfigs[]` + `SkillStates[]` |
| `SkillFactory.cs:42-49` | 수정 | `Create()` → `SkillConfig` 반환 |
| `SkillDispatcher.cs` | 수정 | 시그니처 `ref SkillConfig, ref SkillState` |
| `SkillSystem.cs` | 수정 | 두 배열 접근 패턴 |
| `SimSkillGeneric.cs` | 수정 | config/state 분리 접근 |
| `Custom/SimSkillRukidaFoxfire.cs` | 수정 | custom union 접근 |
| `Custom/SimSkillAprilBarrage.cs` | 수정 | custom union 접근 |
| `Custom/SimSkillEnkiWaveHeal.cs` | 수정 | custom union 접근 |
| `Custom/SimSkillAdriaExpand.cs` | 수정 | custom union 접근 |
| `DashSystem.cs` | 수정 | state.Dash 접근 |
| `Helpers/SkillCCHelper.cs:23` | 수정 | Skills[] → 분리된 배열 |

---

## Chunk 1: 데이터 구조 정의

### Task 1: SkillConfig struct 정의

**Files:**
- Modify: `Assets/_Project/Scripts/InGame_New/Simulation/Combat/Skills/SimSkillBase.cs`

- [ ] **Step 1: SkillConfig struct 작성**

`SimSkillBase.cs`에 `SkillConfig` struct 추가. 기존 `SimSkillInstance`에서 초기화 후 불변인 필드만 이동.

```csharp
/// <summary>
/// 스킬 설정 (초기화 후 읽기전용).
/// 참조 타입(Recipe, ParamValues, SkillHitFrames)은 여기에 격리.
/// </summary>
public struct SkillConfig
{
    // ── Dispatch ──
    public SkillImplType Type;
    public bool IsInitialized;

    // ── 공통 ──
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
    public bool FaceTarget;
    public SkillExecutionType ExecutionType;
    public bool HasProjectile;
    public int SkillClipFrames;
    public int WorldTickRate;

    // ── 참조 타입 (Config에 격리) ──
    public SkillRecipe Recipe;
    public int[] ParamValues;
    public int[] SkillHitFrames;

    // ── 읽기전용 프로퍼티 (기존 SimSkillInstance에서 이동) ──
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

    // ── Custom Config (타입별, 소수 필드라 union 불필요) ──
    // Rukida
    public int FoxFireIncrease;
    public int AtkSpeedRatePercent;
    // April
    public int Rate1, Rate2, Rate3;
    // Enki
    public int HotDuration, HotInterval;
    // Adria
    public int DefScaleValue, StunDurationFrames;
}
```

> **참고:** Custom Config 필드(Rukida 2개, April 3개, Enki 2개, Adria 2개 = 총 9개 int)는 union 처리 대비 복잡도 대비 절약이 미미하므로 flat으로 유지. State 쪽이 union의 핵심 대상.

- [ ] **Step 2: 컴파일 확인**

기존 `SimSkillInstance`는 아직 유지. `SkillConfig`만 추가된 상태에서 컴파일 에러 없는지 확인.

---

### Task 2: SkillCustomState union 정의

**Files:**
- Modify: `Assets/_Project/Scripts/InGame_New/Simulation/Combat/Skills/SimSkillBase.cs`

- [ ] **Step 1: 타입별 State struct 정의**

```csharp
public struct RukidaState { }  // Rukida는 Instant → 런타임 State 없음

public struct AprilState
{
    public int DirCol, DirRow;
    public bool Started;
    public int ClipEndTimer;
    public int HitIndex;
}

public struct EnkiState
{
    public int PhaseTimer;
    public int ChannelFramesRemaining;
    public bool Fired, Channeling;
    public int CachedCasterCombatId, CachedAttack;
    public int StartRow, CenterCol, HalfWidth, WaveDirRow;
}

public struct AdriaState
{
    public int CurrentPhase;
    public bool Done;
    public long HitMask;
}

public struct DashState
{
    public byte DashTilesRemaining;
    public sbyte DashDirCol, DashDirRow;
    public int DashHitDamage;
    public short DashStunFrames;
    public int DashFramesPerTile;
    public byte DashOvershootFrames, DashReturnFrames;
    public MoveEaseType DashOvershootEase, DashReturnEase;
}
```

- [ ] **Step 2: SkillCustomState union 정의**

```csharp
[System.Runtime.InteropServices.StructLayout(
    System.Runtime.InteropServices.LayoutKind.Explicit)]
public struct SkillCustomState
{
    [System.Runtime.InteropServices.FieldOffset(0)] public AprilState April;
    [System.Runtime.InteropServices.FieldOffset(0)] public EnkiState Enki;
    [System.Runtime.InteropServices.FieldOffset(0)] public AdriaState Adria;
    [System.Runtime.InteropServices.FieldOffset(0)] public DashState Dash;
}
```

> **주의:** `bool` 필드가 포함된 struct는 Explicit Layout에서 blittable 경고가 날 수 있음. `bool` → `byte` 전환이 필요하면 이 단계에서 처리.
> `EnkiState`가 가장 큼 (int ×10 + bool ×2 ≈ 44 bytes) → union 전체 = 44 bytes.
> 현재 flat: April(20) + Enki(44) + Adria(16) + Dash(20) ≈ 100 bytes → **56 bytes 절약/인스턴스**.

- [ ] **Step 3: 컴파일 확인**

---

### Task 3: SkillState struct 정의

**Files:**
- Modify: `Assets/_Project/Scripts/InGame_New/Simulation/Combat/Skills/SimSkillBase.cs`

- [ ] **Step 1: SkillState struct 작성**

```csharp
/// <summary>
/// 스킬 런타임 상태 (매 시전마다 Reset).
/// 참조 타입 없음 (HitIds는 fixed buffer).
/// </summary>
public struct SkillState
{
    // ── 공통 타이머/런타임 ──
    public int StartDelay;
    public int TickTimer;
    public int TickInterval;
    public int RemainingTicks;
    public int TickCount;
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
    public int DelayTimer;

    // ── 위치 저장 ──
    public byte SavedGridCol;
    public byte SavedGridRow;

    // ── 히트 추적 (고정 크기) ──
    public const int MaxHitIds = 8;
    // unsafe fixed는 Unity IL2CPP에서 주의 필요 — 일반 inline 필드로 대체
    public int HitId0, HitId1, HitId2, HitId3, HitId4, HitId5, HitId6, HitId7;
    public int HitIdCount;

    // ── 타입별 런타임 (union) ──
    public SkillCustomState Custom;

    public void Reset()
    {
        StartDelay = 0; TickTimer = 0; TickInterval = 0;
        RemainingTicks = 0; TickCount = 0;
        CachedTargetId = CombatUnit.InvalidId;
        KnockbackHitWall = false; ProjectileArrivalTimer = 0;
        CurrentPower = 0; BounceCount = 0; DecayPercent = 0;
        HitIdCount = 0; CurrentHitFrameIndex = 0; HitFrameTimer = 0;
        HasMultiHitFrames = false; PostCompleteTimer = 0; CompleteFired = false;
        DelayTimer = -1;
        HitId0 = HitId1 = HitId2 = HitId3 = CombatUnit.InvalidId;
        HitId4 = HitId5 = HitId6 = HitId7 = CombatUnit.InvalidId;
        Custom = default;
    }

    // ── HitIds 접근 헬퍼 ──
    public readonly int GetHitId(int index)
    {
        switch (index)
        {
            case 0: return HitId0; case 1: return HitId1;
            case 2: return HitId2; case 3: return HitId3;
            case 4: return HitId4; case 5: return HitId5;
            case 6: return HitId6; case 7: return HitId7;
            default: return CombatUnit.InvalidId;
        }
    }

    public void SetHitId(int index, int value)
    {
        switch (index)
        {
            case 0: HitId0 = value; break; case 1: HitId1 = value; break;
            case 2: HitId2 = value; break; case 3: HitId3 = value; break;
            case 4: HitId4 = value; break; case 5: HitId5 = value; break;
            case 6: HitId6 = value; break; case 7: HitId7 = value; break;
        }
    }
}
```

> **HitIds 결정:** `unsafe fixed int HitIds[8]`은 IL2CPP에서 안정적이나 unsafe 컨텍스트를 모든 호출부에 전파해야 함. inline 필드 8개 + switch 접근자가 현실적. 성능 차이 무시 가능 (최대 8회 switch).

- [ ] **Step 2: 컴파일 확인**

- [ ] **Step 3: Explicit Layout bool 호환성 테스트**

`SkillCustomState`의 `AprilState.Started`, `EnkiState.Fired`, `EnkiState.Channeling`, `AdriaState.Done`이 `bool` 타입. Explicit Layout에서 managed 타입과 겹치면 런타임 에러 발생 가능.

**만약 에러 발생 시:** `bool` → `byte` 전환 + 헬퍼 프로퍼티 (`public bool Started { get => _started != 0; set => _started = value ? (byte)1 : (byte)0; }`)

- [ ] **Step 4: 커밋**

```
feat: SkillConfig/SkillState/SkillCustomState 데이터 구조 정의
```

---

## Chunk 2: 저장소 + 팩토리 전환

### Task 4: CombatMatchState 배열 분리

**Files:**
- Modify: `Assets/_Project/Scripts/InGame_New/Simulation/Data/Components.cs:762-838`

- [ ] **Step 1: Skills[] → SkillConfigs[] + SkillStates[]**

```csharp
// 기존
// public SimSkillInstance[] Skills;  // [MaxCombatUnits]

// 변경
public SkillConfig[] SkillConfigs;    // [MaxCombatUnits]
public SkillState[] SkillStates;      // [MaxCombatUnits]
```

`Create()` 메서드에서:
```csharp
// 기존
// Skills = new SimSkillInstance[MaxCombatUnits],

// 변경
SkillConfigs = new SkillConfig[MaxCombatUnits],
SkillStates = new SkillState[MaxCombatUnits],
```

- [ ] **Step 2: 컴파일 — 에러 목록 수집**

이 시점에서 `state.Skills`를 참조하는 모든 곳에서 컴파일 에러 발생. 이것이 Task 5~8의 작업 범위를 정확히 보여줌.

---

### Task 5: SkillFactory.Create() + InitializeBase 전환

**Files:**
- Modify: `Assets/_Project/Scripts/InGame_New/Simulation/Combat/Skills/SkillFactory.cs:42-49`
- Modify: `Assets/_Project/Scripts/InGame_New/Simulation/Combat/Skills/SimSkillBase.cs` (InitializeBase)

- [ ] **Step 1: SkillFactory.Create() → SkillConfig 반환**

```csharp
public static SkillConfig Create(int skillId)
{
    var config = new SkillConfig();
    config.Type = _typeRegistry.TryGetValue(skillId, out var type) ? type : SkillImplType.Generic;
    if (config.Type == SkillImplType.Generic && _recipes.TryGetValue(skillId, out var recipe))
        config.Recipe = recipe;
    return config;
}
```

- [ ] **Step 2: InitializeBase를 SkillConfig의 메서드로 이동**

기존 `SimSkillInstance.InitializeBase(SkillParams p)` → `SkillConfig.InitializeBase(SkillParams p)`

HitIds 할당 로직 제거 (State.Reset()에서 처리):
```csharp
public void InitializeBase(SkillParams p)
{
    SkillId = p.SkillId;
    PowerPercent = p.PowerPercent;
    // ... (기존과 동일, HitIds = new int[8] 제거)
    IsInitialized = true;
}
```

- [ ] **Step 3: 컴파일 확인**

---

### Task 6: SkillDispatcher 시그니처 전환

**Files:**
- Modify: `Assets/_Project/Scripts/InGame_New/Simulation/Combat/Skills/SkillDispatcher.cs`

- [ ] **Step 1: 모든 메서드 시그니처 변경**

`ref SimSkillInstance skill` → `ref SkillConfig config, ref SkillState state`

```csharp
public static void InitializeFromSpec(ref SkillConfig config, ref SkillState state,
    SkillParams baseParams, List<SkillActive> specList, int tickRate)
{
    config.InitializeBase(baseParams);
    state.DelayTimer = -1;  // 초기값
    switch (config.Type)
    {
        case SkillImplType.Generic: GenericSkillLogic.InitializeFromSpec(ref config, ref state, specList, tickRate); break;
        case SkillImplType.Rukida:  RukidaSkillLogic.InitializeFromSpec(ref config, specList, tickRate); break;
        case SkillImplType.April:   AprilSkillLogic.InitializeFromSpec(ref config, specList, tickRate); break;
        case SkillImplType.Enki:    EnkiSkillLogic.InitializeFromSpec(ref config, specList, tickRate); break;
        case SkillImplType.Adria:   AdriaSkillLogic.InitializeFromSpec(ref config, specList, tickRate); break;
    }
}

public static int SelectTarget(ref SkillConfig config, CombatMatchState state, ref CombatUnit caster)
{ /* config.Type switch — 기존과 동일 */ }

public static void Execute(ref SkillConfig config, ref SkillState state,
    CombatMatchState matchState, ref CombatUnit caster, int targetCombatId, ref DeterministicRNG rng)
{ /* config.Type switch */ }

public static bool OnChannelTick(ref SkillConfig config, ref SkillState state,
    CombatMatchState matchState, ref CombatUnit caster, ref DeterministicRNG rng)
{ /* config.Type switch */ }
```

> **참고:** `SelectTarget`은 State 불필요 (config만 사용). `Rukida.InitializeFromSpec`도 state 불필요.

- [ ] **Step 2: 컴파일 — 하위 스킬 로직에서 에러 확인**

---

### Task 7: SkillSystem 전환

**Files:**
- Modify: `Assets/_Project/Scripts/InGame_New/Simulation/Combat/SkillSystem.cs`

- [ ] **Step 1: SetupSkills 전환**

```csharp
public static void SetupSkills(CombatMatchState state, GameWorld world)
{
    for (int i = 0; i < state.UnitCount; i++)
    {
        ref var unit = ref state.Units[i];
        if (unit.SkillSpecId <= 0) continue;

        state.SkillConfigs[i] = SkillFactory.Create(unit.SkillSpecId);
        ref var config = ref state.SkillConfigs[i];
        ref var skillState = ref state.SkillStates[i];
        skillState.Reset();

        if (SkillFactory.TryGetParams(unit.SkillSpecId, out var skillParams))
        {
            SkillFactory.TryGetSpecList(unit.SkillSpecId, out var specList);
            SkillDispatcher.InitializeFromSpec(ref config, ref skillState, skillParams, specList, world.TickRate);
            unit.MaxMana = (int)System.Math.Ceiling(skillParams.CooldownSeconds * world.Config.DefaultManaRegenPerSec);
        }
        else
        {
            config.InitializeBase(new SkillParams
            {
                SkillId = unit.SkillSpecId,
                PowerPercent = 200,
                DamageType = DamageType.Magical,
            });
        }
    }
    RegisterCharacterTraits(state);
}
```

- [ ] **Step 2: TryCast 전환**

`ref var skill = ref state.Skills[unitIndex]` → `ref var config = ref state.SkillConfigs[unitIndex]` + `ref var skillState = ref state.SkillStates[unitIndex]`

config에서 읽는 필드: `IsInitialized`, `GetCastFrames()`, `GetActionLockFrames()`, `FaceTarget`, `IsChanneling`, `HasProjectile`
state에서 읽는 필드: (TryCast 단계에서는 없음 — Execute에서 사용)

- [ ] **Step 3: TickCasting 전환**

동일 패턴. `skill.IsInitialized` → `config.IsInitialized`, `skill.IsChanneling` → `config.IsChanneling`.

- [ ] **Step 4: Cleanup 전환**

```csharp
public static void Cleanup(CombatMatchState state)
{
    if (state?.SkillConfigs == null) return;
    for (int i = 0; i < CombatMatchState.MaxCombatUnits; i++)
    {
        if (state.SkillConfigs[i].IsInitialized)
        {
            state.SkillStates[i].Reset();
            state.SkillConfigs[i] = default;
        }
    }
}
```

- [ ] **Step 5: SkillCCHelper.cs 전환**

`Helpers/SkillCCHelper.cs:23`: `ref var skill = ref state.Skills[idx]` → `ref var config = ref state.SkillConfigs[idx]` (CC에서 config만 참조하는지 확인 후 전환)

- [ ] **Step 6: 커밋**

```
refactor: CombatMatchState/SkillFactory/SkillDispatcher/SkillSystem Config/State 분리
```

---

## Chunk 3: 스킬 로직 전환

### Task 8: GenericSkillLogic 전환

**Files:**
- Modify: `Assets/_Project/Scripts/InGame_New/Simulation/Combat/Skills/SimSkillGeneric.cs`

이 파일이 가장 크고 복잡. `ref SimSkillInstance skill` → `ref SkillConfig config, ref SkillState state` 전환.

- [ ] **Step 1: InitializeFromSpec 전환**

Config 설정: `config.WorldTickRate`, `config.ExecutionType`, `config.HasProjectile`, `config.ParamValues`, `config.PowerPercent`

- [ ] **Step 2: Execute 전환**

`SkillExecuteContext` 생성 시 config와 state를 모두 전달해야 함.
현재 `ctx.GetParamValue()`는 `skill.ParamValues[]` 접근 → config에서 읽기.
`ctx.TargetCombatId` 설정 등 → state에서 쓰기.

> **핵심:** `SkillExecuteContext`도 config/state ref를 모두 보유하도록 수정 필요. 또는 context 생성 시 필요한 값만 복사.

- [ ] **Step 3: OnChannelTick 전환**

타이머 증감: state
DashPhase 확인: `caster.DashPhase`
Recipe dispatch: config

- [ ] **Step 4: DashForward 인라인 블록 전환**

`DashSystem.StartDash(ctx.State, ref caster, ref skill, ref action, ...)` → `DashSystem.StartDash(ctx.State, ref caster, ref state, ref action, ...)`

DashSystem은 config 접근 불필요 (타이밍은 action에서, 데미지/CC는 인자로 전달).

- [ ] **Step 5: 보조 메서드들 전환**

`InitChanneling`, `HandleProjectileArrival`, `DispatchActionsForHitFrame`, `DispatchActions`, `ExecuteActionWithSpecialHandling` — 모두 시그니처 변경.

- [ ] **Step 6: HitIds 접근 전환**

기존: `skill.HitIds[skill.HitIdCount++] = combatId`
변경: `state.SetHitId(state.HitIdCount++, combatId)`

기존: `for (int h = 0; h < skill.HitIdCount; h++) if (skill.HitIds[h] == id)`
변경: `for (int h = 0; h < state.HitIdCount; h++) if (state.GetHitId(h) == id)`

- [ ] **Step 7: 컴파일 확인**

- [ ] **Step 8: 커밋**

```
refactor: GenericSkillLogic config/state 분리 전환
```

---

### Task 9: Custom 스킬 로직 전환

**Files:**
- Modify: `Assets/_Project/Scripts/InGame_New/Simulation/Combat/Skills/Custom/SimSkillRukidaFoxfire.cs`
- Modify: `Assets/_Project/Scripts/InGame_New/Simulation/Combat/Skills/Custom/SimSkillAprilBarrage.cs`
- Modify: `Assets/_Project/Scripts/InGame_New/Simulation/Combat/Skills/Custom/SimSkillEnkiWaveHeal.cs`
- Modify: `Assets/_Project/Scripts/InGame_New/Simulation/Combat/Skills/Custom/SimSkillAdriaExpand.cs`

- [ ] **Step 1: Rukida 전환**

가장 간단. State 사용 없음 (Instant). config만 참조.

```csharp
public static void InitializeFromSpec(ref SkillConfig config, List<SkillActive> specList, int tickRate)
{
    config.FoxFireIncrease = SkillSpecHelper.GetInt(specList, 1, 2f);
    config.BuffDurationFrames = SkillSpecHelper.GetFrames(specList, 2, 3f, tickRate);
    config.AtkSpeedRatePercent = SkillSpecHelper.GetInt(specList, 3, 10f);
}

public static void Execute(ref SkillConfig config, CombatMatchState state,
    ref CombatUnit caster, int targetCombatId, ref DeterministicRNG rng)
{
    // config.FoxFireIncrease, config.BuffDurationFrames, config.AtkSpeedRatePercent 등
    // 기존 로직 그대로 — skill. → config.
}
```

- [ ] **Step 2: April 전환**

`skill.DirCol` → `state.Custom.April.DirCol`
`skill.Started` → `state.Custom.April.Started`
등.

InitializeFromSpec: config 설정 (ExecutionType, HitCount, Rate1/2/3)
Execute: state.Custom.April 설정 (DirCol, DirRow, Started, ...)
OnChannelTick: state.Custom.April 읽기/쓰기 + config 읽기 (HitCount, Rate1/2/3, SkillHitFrames, SkillClipFrames)

- [ ] **Step 3: Enki 전환**

`skill.WaveDirRow` → `state.Custom.Enki.WaveDirRow`
`skill.CachedAttack` → `state.Custom.Enki.CachedAttack`
등.

- [ ] **Step 4: Adria 전환**

`skill.CurrentPhase` → `state.Custom.Adria.CurrentPhase`
`skill.HitMask` → `state.Custom.Adria.HitMask`
등. config에서: `PowerPercent`, `DefScaleValue`, `StunDurationFrames`

- [ ] **Step 5: 컴파일 확인**

- [ ] **Step 6: 커밋**

```
refactor: Custom 스킬 로직 (Rukida/April/Enki/Adria) config/state 분리
```

---

### Task 10: DashSystem 전환

**Files:**
- Modify: `Assets/_Project/Scripts/InGame_New/Simulation/Combat/Systems/DashSystem.cs`

- [ ] **Step 1: 시그니처 변경**

`ref SimSkillInstance skill` → `ref SkillState state`

DashSystem은 config 접근 불필요:
- 타이밍: `ref SkillAction action`에서 받음
- 데미지/CC: 인자(`hitDamage`, `stunFrames`)로 받음
- 런타임 상태: `state.Custom.Dash.*`

```csharp
public static void StartDash(
    CombatMatchState matchState, ref CombatUnit caster, ref SkillState state,
    ref SkillAction action,
    int dirCol, int dirRow, int actualDist,
    int hitDamage, short stunFrames, int tickRate)
{
    ref var dash = ref state.Custom.Dash;
    // dash.DashTilesRemaining = ...
    // dash.DashDirCol = ...
}
```

- [ ] **Step 2: 내부 메서드 전환**

`ProcessTick`, `ProcessRushPhase`, `TransitionToOvershoot`, `TransitionToReturn`, `FinishDash`, `MoveToNextTile`, `ApplyHitOnCurrentTile` — 모두 `ref SkillState state` + `state.Custom.Dash` 접근.

- [ ] **Step 3: SavedGridCol/Row 접근**

`skill.SavedGridCol` → `state.SavedGridCol` (공통 State 필드, Dash union 밖)

- [ ] **Step 4: 컴파일 확인**

- [ ] **Step 5: 커밋**

```
refactor: DashSystem config/state 분리 전환
```

---

## Chunk 4: 정리 + 기존 SimSkillInstance 제거

### Task 11: SimSkillInstance 제거 + 최종 정리

**Files:**
- Modify: `Assets/_Project/Scripts/InGame_New/Simulation/Combat/Skills/SimSkillBase.cs`

- [ ] **Step 1: SimSkillInstance struct 삭제**

`SkillParams` struct는 유지 (팩토리 파라미터). `SimSkillInstance`와 관련 `SkillImplType` enum은 유지 (SkillConfig로 이동 완료 확인 후 삭제).

- [ ] **Step 2: 프로젝트 전체 컴파일 확인**

`SimSkillInstance`를 참조하는 곳이 0인지 확인.

- [ ] **Step 3: 최종 커밋**

```
refactor: SimSkillInstance 제거, Config/State 분리 완료
```

---

## 주의사항

### SkillExecuteContext 처리
`SimSkillGeneric.cs`의 `SkillExecuteContext`가 현재 `ref SimSkillInstance`를 캡처하는지 확인 필요. ref struct라면 config/state 둘 다 캡처 가능. 일반 struct/class라면 인덱스 기반 접근으로 전환.

### bool → byte 전환 가능성
Explicit Layout union에서 `bool`이 다른 필드와 offset이 겹칠 때 CLR이 거부할 수 있음. Task 2 Step 3에서 검증 후 필요시 `byte`로 전환.

### 테스트
현재 프로젝트에 유닛 테스트 프레임워크가 없으므로 TDD 단계 생략. 각 Task의 "컴파일 확인"이 검증 기준. 인게임 테스트는 전체 전환 완료 후 `InGameTestConfig`로 전투 실행하여 확인.
