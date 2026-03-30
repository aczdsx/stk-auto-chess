# DashForward 레시피 기반 3페이즈 리팩토링

> **For agentic workers:** REQUIRED: Use superpowers:subagent-driven-development (if subagents available) or superpowers:executing-plans to implement this plan. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** DashSystem 내부 페이즈 관리를 제거하고, 각 Execute 이벤트에 독립적인 DashForward 액션을 배치하여 레시피가 대쉬 흐름을 완전히 제어하게 한다.

**Architecture:** DashForward(DashPhase, ...) 하나로 Rush/Overshoot/Return 세 가지 이동을 표현. DashSystem은 단일 페이즈 실행+완료 감지만 담당. 페이즈 완료 시 SimSkillGeneric이 다음 hitframe을 디스패치하여 레시피의 다음 DashForward를 발동.

**Tech Stack:** Unity C#, struct 기반 시뮬레이션

---

## Before → After

### Before (현재)
```csharp
// 레시피: DashForward 1번, 3페이즈 타이밍을 전부 담음
.On(Evt.Execute1)
    .Do(DashForward(3, rushMs:500, overshootMs:300, returnMs:100, ...))
.On(Evt.Execute2)
    .Do(Vfx(...))  // VFX만 (DashSystem이 내부에서 페이즈 전환)

// DashSystem: 내부에서 Rush→Overshoot→Return 3페이즈 관리
// SkillAction: DashRushMs/OvershootMs/ReturnMs/RushEase/OvershootEase/ReturnEase 6개 필드
// DashState: DashOvershootFrames/ReturnFrames/OvershootEase/ReturnEase 4개 필드
```

### After (목표)
```csharp
// 레시피: DashForward 3번, 각 Execute에 독립적
.On(Evt.Execute1)
    .Do(Vfx(0, V.AtTargetWithDir))
    .Do(Vfx(1, V.AtCasterWithDir))
    .Do(DashForward(DashPhase.Rush, distance: 3, durationMs: 500, ease: MoveEaseType.OutQuad,
        power: Spec(1, 200f), cc: CrowdControlType.Stun, ccDuration: Spec(2, P.Frames, 3f)))
.On(Evt.Execute2)
    .Do(Vfx(0, V.AtCasterToSaved, vfxDirOffset: 18))
    .Do(DashForward(DashPhase.Overshoot, durationMs: 300, ease: MoveEaseType.Linear))
.On(Evt.Execute3)
    .Do(DashForward(DashPhase.Return, durationMs: 100, ease: MoveEaseType.InExpo))

// DashSystem: 단일 페이즈 실행만 (StartPhase/OnMoveComplete)
// SkillAction: DashPhaseType + DashDurationMs + DashEase (3개 필드로 축소)
// DashState: TilesRemaining/Dir/HitDamage/StunFrames/FramesPerTile (5개로 축소)
```

---

## 파일 구조

| 파일 | 변경 | 설명 |
|------|------|------|
| `Enums.cs` | 수정 없음 | DashPhase enum 이미 존재 |
| `SkillRecipe.cs` | 수정 | SkillAction: 6개 필드 → DashPhaseType + DashDurationMs + DashEase 3개 |
| `SkillFactory.cs` | 수정 | DashForward 시그니처 변경 |
| `SkillFactory.Monster.cs` | 수정 | 빅마우스 레시피 3×DashForward |
| `SimSkillBase.cs` | 수정 | DashState 간소화 (OvershootFrames/ReturnFrames/Ease 제거) |
| `DashSystem.cs` | 전면 재작성 | StartPhase + OnMoveComplete (페이즈 관리 제거) |
| `SimSkillGeneric.cs` | 수정 | DashForward 특수 처리 + 채널링 틱 페이즈 완료→다음 hitframe |
| `CombatAISystem.cs` | 수정 없음 | DashPhase != None 분기 이미 존재 |
| `UnitViewManager.cs` | 수정 없음 | DashEase/DashPhase 보간 이미 존재 |

---

## Task 1: SkillAction 필드 교체

**Files:** `SkillRecipe.cs`

- [ ] **Step 1: 기존 6개 필드 제거, 3개 필드로 교체**

```csharp
// 제거:
// public short DashRushMs, DashOvershootMs, DashReturnMs;
// public MoveEaseType DashRushEase, DashOvershootEase, DashReturnEase;

// 추가:
/// <summary>DashForward: 이 액션이 수행할 대쉬 페이즈</summary>
public DashPhase DashPhaseType;
/// <summary>DashForward: 이 페이즈 지속 시간 (ms)</summary>
public short DashDurationMs;
/// <summary>DashForward: 이 페이즈 Ease 타입</summary>
public MoveEaseType DashEase;
```

---

## Task 2: DashForward 팩토리 변경

**Files:** `SkillFactory.cs`

- [ ] **Step 1: DashForward 시그니처를 페이즈 기반으로 변경**

```csharp
/// <summary>대쉬 단일 페이즈. Rush/Overshoot/Return을 각각 호출.</summary>
public static ActionTemplate DashForward(DashPhase phase,
    byte distance = 0,
    short durationMs = 500,
    MoveEaseType ease = MoveEaseType.OutQuad,
    ValueRef power = default,
    CrowdControlType cc = default,
    ValueRef ccDuration = default)
    => new ActionTemplate(
        new SkillAction
        {
            Effect = SkillEffectType.DashForward,
            DashPhaseType = phase,
            AreaRange = distance,
            DashDurationMs = durationMs,
            DashEase = ease,
            CCType = cc,
        },
        primary: power, secondary: ccDuration);
```

---

## Task 3: 빅마우스 레시피 업데이트

**Files:** `SkillFactory.Monster.cs`

- [ ] **Step 1: 3×DashForward 레시피**

```csharp
Skill(240107002, E.Channeling, T.NearestEnemy)
    .On(Evt.Execute1)  // 돌진: 포탈VFX + 3타일 대쉬 (타일별 데미지+스턴)
        .Do(Vfx(0, V.AtTargetWithDir))
        .Do(Vfx(1, V.AtCasterWithDir))
        .Do(DashForward(DashPhase.Rush, distance: 3, durationMs: 500, ease: MoveEaseType.OutQuad,
            power: Spec(1, 200f), cc: CrowdControlType.Stun, ccDuration: Spec(2, P.Frames, 3f)))
    .On(Evt.Execute2)  // 오버슈트: 복귀 포탈VFX + 관성 미끄러짐
        .Do(Vfx(0, V.AtCasterToSaved, vfxDirOffset: 18))
        .Do(DashForward(DashPhase.Overshoot, durationMs: 300, ease: MoveEaseType.Linear))
    .On(Evt.Execute3)  // 복귀: 원위치 텔레포트 + 착지 슬라이드
        .Do(DashForward(DashPhase.Return, durationMs: 100, ease: MoveEaseType.InExpo))
    .Register();
```

---

## Task 4: DashState 간소화

**Files:** `SimSkillBase.cs`

- [ ] **Step 1: DashState에서 페이즈 전환용 필드 제거**

```csharp
public struct DashState
{
    // Rush용 런타임
    public byte DashTilesRemaining;
    public sbyte DashDirCol, DashDirRow;
    public int DashHitDamage;
    public short DashStunFrames;
    public int DashFramesPerTile;
    // 제거: DashOvershootFrames, DashReturnFrames, DashOvershootEase, DashReturnEase
}
```

---

## Task 5: DashSystem 전면 재작성

**Files:** `DashSystem.cs`

- [ ] **Step 1: StartPhase — 페이즈별 시작 분기**

```csharp
public static void StartPhase(CombatMatchState state, ref CombatUnit caster,
    ref SkillState skillState, ref SkillAction action, SkillExecuteContext ctx, int tickRate)
{
    switch (action.DashPhaseType)
    {
        case DashPhase.Rush:
            StartRush(state, ref caster, ref skillState, ref action, ctx, tickRate);
            break;
        case DashPhase.Overshoot:
            StartOvershoot(ref caster, ref action, ref skillState, tickRate);
            break;
        case DashPhase.Return:
            StartReturn(state, ref caster, ref skillState, ref action, tickRate);
            break;
    }
}
```

- [ ] **Step 2: StartRush — 방향 계산 + 첫 타일 이동**

기존 ExecuteActionWithSpecialHandling의 방향/거리/데미지 계산 로직을 여기로 이동.

```csharp
private static void StartRush(CombatMatchState state, ref CombatUnit caster,
    ref SkillState skillState, ref SkillAction action, SkillExecuteContext ctx, int tickRate)
{
    // 타겟 방향 계산 (기존 SimSkillGeneric의 DashForward 블록에서 이동)
    int targetIdx = state.FindUnitIndex(ctx.TargetCombatId);
    if (targetIdx < 0) return;
    ref var target = ref state.Units[targetIdx];
    int maxDist = action.AreaRange > 0 ? action.AreaRange : 3;

    int dirCol = target.GridCol - caster.GridCol;
    int dirRow = target.GridRow - caster.GridRow;
    if (dirCol != 0) dirCol = dirCol > 0 ? 1 : -1;
    if (dirRow != 0) dirRow = dirRow > 0 ? 1 : -1;
    if (dirCol != 0 && dirRow != 0) dirCol = 0;
    if (dirCol == 0 && dirRow == 0) return;

    int actualDist = 0;
    for (int step = 1; step <= maxDist; step++)
    {
        int nc = caster.GridCol + dirCol * step;
        int nr = caster.GridRow + dirRow * step;
        if (!BoardHelper.IsValidCombatPosition(nc, nr)) break;
        int occ = state.GetUnitAtGrid(nc, nr);
        if (occ != CombatUnit.InvalidId)
        {
            int oi = state.FindUnitIndex(occ);
            if (oi >= 0 && state.Units[oi].TeamIndex == caster.TeamIndex) break;
        }
        actualDist = step;
    }
    if (actualDist <= 0) return;

    int durationMs = action.DashDurationMs > 0 ? action.DashDurationMs : 500;
    int totalFrames = MsToFrames(durationMs, tickRate);
    int framesPerTile = totalFrames / actualDist;
    if (framesPerTile < 1) framesPerTile = 1;

    int power = ctx.GetParamValue(action.ParamIndex);
    int damage = caster.Attack * power / 100;
    if (damage < 1) damage = 1;
    short stunFrames = action.CCType != CrowdControlType.None
        ? (short)ctx.GetParamValue(action.SecondaryParamIndex) : (short)0;

    ref var dash = ref skillState.Custom.Dash;
    dash.DashTilesRemaining = (byte)actualDist;
    dash.DashDirCol = (sbyte)dirCol;
    dash.DashDirRow = (sbyte)dirRow;
    dash.DashHitDamage = damage;
    dash.DashStunFrames = stunFrames;
    dash.DashFramesPerTile = framesPerTile;

    caster.DashPhase = DashPhase.Rush;
    caster.DashEase = action.DashEase != MoveEaseType.None ? action.DashEase : MoveEaseType.OutQuad;
    MoveToNextTile(state, ref caster, ref dash);
}
```

- [ ] **Step 3: StartOvershoot — 비주얼 오프셋 이동**

```csharp
private static void StartOvershoot(ref CombatUnit caster, ref SkillAction action,
    ref SkillState skillState, int tickRate)
{
    ref var dash = ref skillState.Custom.Dash;
    int durationMs = action.DashDurationMs > 0 ? action.DashDurationMs : 300;

    caster.DashPhase = DashPhase.Overshoot;
    caster.DashEase = action.DashEase != MoveEaseType.None ? action.DashEase : MoveEaseType.Linear;
    caster.MoveFromCol = (byte)(caster.GridCol - dash.DashDirCol);
    caster.MoveFromRow = (byte)(caster.GridRow - dash.DashDirRow);
    caster.MoveDuration = MsToFrames(durationMs, tickRate);
    caster.MoveTimer = caster.MoveDuration;
}
```

- [ ] **Step 4: StartReturn — 텔레포트 + 착지 슬라이드**

```csharp
private static void StartReturn(CombatMatchState state, ref CombatUnit caster,
    ref SkillState skillState, ref SkillAction action, int tickRate)
{
    ref var dash = ref skillState.Custom.Dash;
    int durationMs = action.DashDurationMs > 0 ? action.DashDurationMs : 100;

    state.ClearGrid(caster.GridCol, caster.GridRow);
    caster.GridCol = skillState.SavedGridCol;
    caster.GridRow = skillState.SavedGridRow;
    int occupant = state.GetUnitAtGrid(skillState.SavedGridCol, skillState.SavedGridRow);
    if (occupant == CombatUnit.InvalidId)
        state.SetGrid(skillState.SavedGridCol, skillState.SavedGridRow, caster.CombatId);

    caster.DashPhase = DashPhase.Return;
    caster.DashEase = action.DashEase != MoveEaseType.None ? action.DashEase : MoveEaseType.InExpo;
    caster.MoveFromCol = (byte)(skillState.SavedGridCol - dash.DashDirCol);
    caster.MoveFromRow = (byte)(skillState.SavedGridRow - dash.DashDirRow);
    caster.MoveDuration = MsToFrames(durationMs, tickRate);
    caster.MoveTimer = caster.MoveDuration;

    state.EventQueue?.PushUnitMoved(caster.CombatId, skillState.SavedGridCol, skillState.SavedGridRow);
}
```

- [ ] **Step 5: OnMoveComplete — Rush 타일 진행 or 페이즈 완료 신호**

```csharp
/// <summary>MoveTimer=0 도달 시 호출. true=아직 이동 중, false=페이즈 완료(다음 hitframe 디스패치 필요)</summary>
public static bool OnMoveComplete(CombatMatchState state, ref CombatUnit unit, ref SkillState skillState)
{
    if (unit.DashPhase == DashPhase.Rush)
    {
        ref var dash = ref skillState.Custom.Dash;
        ApplyHitOnCurrentTile(state, ref unit, ref dash);
        dash.DashTilesRemaining--;

        if (dash.DashTilesRemaining > 0)
        {
            int nc = unit.GridCol + dash.DashDirCol;
            int nr = unit.GridRow + dash.DashDirRow;
            if (BoardHelper.IsValidCombatPosition(nc, nr))
            {
                MoveToNextTile(state, ref unit, ref dash);
                return true; // 아직 Rush 진행 중
            }
        }
    }

    // Rush 완료 or Overshoot/Return 완료
    unit.DashPhase = DashPhase.None;
    unit.DashEase = MoveEaseType.None;
    return false; // 페이즈 완료 → 다음 hitframe 디스패치 필요
}
```

- [ ] **Step 6: 유틸리티 유지 (MoveToNextTile, ApplyHitOnCurrentTile, MsToFrames, IsActive)**

기존 그대로.

---

## Task 6: SimSkillGeneric 수정

**Files:** `SimSkillGeneric.cs`

- [ ] **Step 1: ExecuteActionWithSpecialHandling — DashForward 블록 간소화**

방향/거리/데미지 계산을 DashSystem.StartPhase로 이동했으므로:

```csharp
if (action.Effect == SkillEffectType.DashForward)
{
    ActionExecutor.Execute(ref action, ctx); // VFX 처리
    ref var caster = ref ctx.GetCaster();
    DashSystem.StartPhase(ctx.State, ref caster, ref state, ref action, ctx, config.WorldTickRate);
    return;
}
```

- [ ] **Step 2: OnChannelTick — 페이즈 완료 시 다음 hitframe 디스패치**

```csharp
if (caster.DashPhase != DashPhase.None)
{
    if (!caster.IsMoving)
    {
        bool stillMoving = DashSystem.OnMoveComplete(matchState, ref caster, ref state);
        if (!stillMoving)
        {
            // 페이즈 완료 → 다음 hitframe 디스패치
            state.DashHitFrameIndex++;
            DispatchActionsForHitFrame(ref config, ref state, state.DashHitFrameIndex, ctx);
        }
    }
    // DashPhase가 다시 설정됐으면 계속, None이면 채널링 종료
    return DashSystem.IsActive(ref caster);
}
```

**주의:** `state.DashHitFrameIndex`는 새 필드. DashForward 시작 시 현재 hitframe 인덱스를 기억해야 다음 hitframe을 디스패치할 수 있다.

---

## Task 7: SkillState에 DashHitFrameIndex 추가

**Files:** `SimSkillBase.cs`

- [ ] **Step 1: DashState에 현재 hitframe 인덱스 추가**

```csharp
public struct DashState
{
    public byte DashTilesRemaining;
    public sbyte DashDirCol, DashDirRow;
    public int DashHitDamage;
    public short DashStunFrames;
    public int DashFramesPerTile;
    public byte DashHitFrameIndex;  // 현재 대쉬가 시작된 hitframe 인덱스
}
```

DashSystem.StartPhase에서 설정:
```csharp
// StartRush 시작 시 (또는 StartPhase 공통):
dash.DashHitFrameIndex = action.HitFrameIndex;
```

OnMoveComplete 후 SimSkillGeneric에서:
```csharp
int nextHitFrame = skillState.Custom.Dash.DashHitFrameIndex + 1;
DispatchActionsForHitFrame(ref config, ref state, nextHitFrame, ctx);
```

---

## 흐름 요약

```
Execute1 hitframe → DashSystem.StartPhase(Rush)
  → Rush 타일 이동 (OnMoveComplete returns true)
  → Rush 마지막 타일 (OnMoveComplete returns false, DashPhase=None)
  → SimSkillGeneric: hitFrameIndex+1 → DispatchActionsForHitFrame(1)
  → Execute2 발동 → VFX + DashSystem.StartPhase(Overshoot)
  → Overshoot MoveTimer=0 (OnMoveComplete returns false, DashPhase=None)
  → SimSkillGeneric: hitFrameIndex+1 → DispatchActionsForHitFrame(2)
  → Execute3 발동 → DashSystem.StartPhase(Return)
  → Return MoveTimer=0 (OnMoveComplete returns false, DashPhase=None)
  → SimSkillGeneric: hitFrameIndex+1 → DispatchActionsForHitFrame(3)
  → Execute3에 더 이상 액션 없음 → DashPhase=None → 채널링 종료
```
