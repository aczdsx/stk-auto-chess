# Recipe 조건부 체이닝 Implementation Plan

> **For agentic workers:** REQUIRED: Use superpowers:subagent-driven-development (if subagents available) or superpowers:executing-plans to implement this plan. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Custom 스킬 7개를 Recipe 선언으로 전환. Trigger 2개 + EffectType 3개 + AreaShape 1개 추가.

**Architecture:** SimSkillGeneric에 바운스/히트 추적 + multi-hitframe 상태를 추가하고, ActionExecutor에 Teleport/Retarget/ApplyStatusEffect 메서드를 추가. Knockback 결과는 DispatchActions에서 직접 처리하여 OnKnockbackWall 트리거 디스패치. OnProjectileArrive는 타이머 기반.

**Tech Stack:** Unity C# (IL2CPP), struct 기반 GC-free 시뮬레이션

**Spec:** `docs/superpowers/specs/2026-03-26-recipe-conditional-chaining-design.md`

---

## Task 1: Enum + SkillAction 필드 추가

**Files:**
- Modify: `Assets/_Project/Scripts/InGame_New/Simulation/Data/Enums.cs`
- Modify: `Assets/_Project/Scripts/InGame_New/Simulation/Combat/Skills/Recipe/SkillRecipe.cs`

- [ ] **Step 1: SkillEffectType에 3개 추가**

Enums.cs의 SkillEffectType enum 끝에:
```csharp
Teleport,              // GridSystem으로 순간이동
Retarget,              // TargetingSystem으로 타겟 재선택
ApplyStatusEffect,     // StatusEffectSystem으로 상태이상 적용 (CC와 별도)
```

- [ ] **Step 2: SkillTriggerType에 2개 추가**

```csharp
OnKnockbackWall,       // 넉백 벽 충돌 시
OnProjectileArrive,    // 투사체 도착 시
```

- [ ] **Step 3: SkillAreaShape에 1개 추가**

```csharp
Rect,                  // 방향 기반 직사각형 (AreaRange=좌우, RectDepth=전방)
```

- [ ] **Step 4: SkillTargetFilter에 1개 추가**

```csharp
NearestEnemy,          // 가장 가까운 적 (Retarget에서 베인 바운스용)
```

- [ ] **Step 5: SkillAction struct에 필드 7개 추가**

SkillRecipe.cs의 SkillAction struct에:
```csharp
// ── 체이닝 확장 ──
/// <summary>Teleport: 전방 이동 거리 (0이면 타겟 뒤로)</summary>
public byte TeleportDistance;
/// <summary>Rect: 전방 깊이 (시전자 행 기준 추가 행 수)</summary>
public byte RectDepth;
/// <summary>Knockback: 고정 거리 (0이면 SecondaryParamIndex 사용)</summary>
public byte KnockbackDistance;
/// <summary>Retarget: 이미 히트한 타겟 제외</summary>
public bool ExcludeHit;
/// <summary>Damage(Area): 메인 타겟 제외</summary>
public bool ExcludePrimary;
/// <summary>Buff: 값에 히트수를 곱함</summary>
public bool ScaleByHitCount;
/// <summary>Damage: 바운스 감쇠율 ParamSlots 인덱스 (-1=없음)</summary>
public sbyte DecayParamIndex;
```

- [ ] **Step 6: 컴파일 확인**

- [ ] **Step 7: 커밋**
```
feat: add enums and SkillAction fields for conditional chaining
```

---

## Task 2: SkillRecipeBuilder 팩토리 + 빌더 메서드 추가

**Files:**
- Modify: `Assets/_Project/Scripts/InGame_New/Simulation/Combat/Skills/SkillFactory.cs` (SkillRecipeBuilder inner struct)

- [ ] **Step 1: Trigger 빌더 메서드 2개 추가**

OnComplete 메서드 아래에:
```csharp
public SkillRecipeBuilder OnKnockbackWall(SkillAction action)
{
    action.Trigger = SkillTriggerType.OnKnockbackWall;
    _actions.Add(action);
    return this;
}

public SkillRecipeBuilder OnProjectileArrive(SkillAction action)
{
    action.Trigger = SkillTriggerType.OnProjectileArrive;
    _actions.Add(action);
    return this;
}
```

- [ ] **Step 2: EffectType 팩토리 메서드 추가**

액션 팩토리 섹션에:
```csharp
public static SkillAction Teleport(byte distance = 0)
    => new SkillAction { Effect = SkillEffectType.Teleport, TeleportDistance = distance };

public static SkillAction Retarget(SkillTargetFilter filter, bool excludeHit = false)
    => new SkillAction { Effect = SkillEffectType.Retarget, TargetFilter = filter, ExcludeHit = excludeHit };

public static SkillAction ApplyStatusEffect(StatusEffectType statusType, SkillTargetFilter filter,
    sbyte durationParamIndex = -1, sbyte valueParamIndex = -1)
    => new SkillAction { Effect = SkillEffectType.ApplyStatusEffect, TargetFilter = filter,
        StatusEffect = statusType, SecondaryParamIndex = durationParamIndex, ParamIndex = valueParamIndex };

public static SkillAction DamageWithDecay(sbyte paramIndex = -1, sbyte decayParamIndex = -1)
    => new SkillAction { Effect = SkillEffectType.Damage, ParamIndex = paramIndex, DecayParamIndex = decayParamIndex };

public static SkillAction BuffScaled(StatModType stat, sbyte valueIdx, sbyte durIdx, bool scaleByHitCount = false)
    => new SkillAction { Effect = SkillEffectType.ApplyBuff, BuffStat = stat,
        ParamIndex = valueIdx, SecondaryParamIndex = durIdx, ScaleByHitCount = scaleByHitCount };
```

- [ ] **Step 3: 기존 Knockback/Damage 팩토리 확장**

기존 Knockback 팩토리 수정:
```csharp
public static SkillAction Knockback(sbyte distParamIndex = -1, byte fixedDistance = 0)
    => new SkillAction { Effect = SkillEffectType.Knockback, TargetFilter = SkillTargetFilter.PrimaryTarget,
        SecondaryParamIndex = distParamIndex, KnockbackDistance = fixedDistance };
```

Damage 팩토리에 excludePrimary 옵션 추가 — 기존 시그니처 유지하면서 오버로드:
```csharp
public static SkillAction Damage(sbyte paramIndex = -1,
    SkillTargetFilter filter = SkillTargetFilter.PrimaryTarget,
    SkillAreaShape area = SkillAreaShape.None, byte range = 0,
    bool excludePrimary = false, byte rectDepth = 0)
    => new SkillAction { Effect = SkillEffectType.Damage, TargetFilter = filter,
        AreaShape = area, AreaRange = range, ParamIndex = paramIndex,
        ExcludePrimary = excludePrimary, RectDepth = rectDepth };
```

- [ ] **Step 4: 컴파일 확인**

- [ ] **Step 5: 커밋**
```
feat: add chaining factory and builder methods to SkillRecipeBuilder
```

---

## Task 3: ActionExecutor — Teleport, Retarget, ApplyStatusEffect, Rect, ExcludePrimary

**Files:**
- Modify: `Assets/_Project/Scripts/InGame_New/Simulation/Combat/Skills/Recipe/ActionExecutor.cs`
- Modify: `Assets/_Project/Scripts/InGame_New/Simulation/Combat/Skills/Helpers/SkillAreaHelper.cs` (IsInArea에 Rect case)

- [ ] **Step 1: Execute dispatch에 3개 case 추가**

ActionExecutor.Execute의 switch에:
```csharp
case SkillEffectType.Teleport: ExecuteTeleport(ref action, ctx); break;
case SkillEffectType.Retarget: ExecuteRetarget(ref action, ctx); break;
case SkillEffectType.ApplyStatusEffect: ExecuteApplyStatusEffect(ref action, ctx); break;
```

- [ ] **Step 2: ExecuteTeleport 구현**

Custom 스킬의 TeleportBehindTarget/TryTeleport 로직을 ActionExecutor static 메서드로 이동:

```csharp
private static void ExecuteTeleport(ref SkillAction action, SkillExecuteContext ctx)
{
    ref var caster = ref ctx.GetCaster();

    if (action.TeleportDistance > 0)
    {
        // 전방 N칸 (오데트)
        TryTeleportForward(ctx.State, ref caster, action.TeleportDistance, ctx);
    }
    else
    {
        // 타겟 뒤로 (마리에/시라유키)
        int targetIdx = ctx.State.FindUnitIndex(ctx.TargetCombatId);
        if (targetIdx >= 0)
            TeleportBehindTarget(ctx.State, ref caster, ref ctx.State.Units[targetIdx]);
    }
}

private static void TeleportBehindTarget(CombatMatchState state, ref CombatUnit caster, ref CombatUnit target)
{
    int dirCol = target.GridCol - caster.GridCol;
    int dirRow = target.GridRow - caster.GridRow;
    if (dirCol != 0) dirCol = dirCol > 0 ? 1 : -1;
    if (dirRow != 0) dirRow = dirRow > 0 ? 1 : -1;

    int behindCol = target.GridCol + dirCol;
    int behindRow = target.GridRow + dirRow;
    if (TryTeleport(state, ref caster, behindCol, behindRow)) return;

    for (int d = 1; d <= 2; d++)
        for (int dc = -d; dc <= d; dc++)
            for (int dr = -d; dr <= d; dr++)
            {
                if (dc == 0 && dr == 0) continue;
                if (TryTeleport(state, ref caster, target.GridCol + dc, target.GridRow + dr))
                    return;
            }
}

private static bool TryTeleport(CombatMatchState state, ref CombatUnit caster, int col, int row)
{
    if (!BoardHelper.IsValidCombatPosition(col, row)) return false;
    if (state.GetUnitAtGrid(col, row) != CombatUnit.InvalidId) return false;
    state.ClearGrid(caster.GridCol, caster.GridRow);
    caster.GridCol = (byte)col;
    caster.GridRow = (byte)row;
    state.SetGrid(col, row, caster.CombatId);
    state.EventQueue?.PushUnitMoved(caster.CombatId, (byte)col, (byte)row);
    return true;
}

private static void TryTeleportForward(CombatMatchState state, ref CombatUnit caster,
    int distance, SkillExecuteContext ctx)
{
    // 타겟 방향 계산
    int targetIdx = ctx.State.FindUnitIndex(ctx.TargetCombatId);
    int dirCol, dirRow;
    if (targetIdx >= 0)
    {
        ref var t = ref state.Units[targetIdx];
        int dc = t.GridCol - caster.GridCol;
        int dr = t.GridRow - caster.GridRow;
        dirCol = dc > 0 ? 1 : dc < 0 ? -1 : 0;
        dirRow = dr > 0 ? 1 : dr < 0 ? -1 : 0;
    }
    else
    {
        dirCol = 0;
        dirRow = caster.TeamIndex == 0 ? 1 : -1;
    }

    int destCol = caster.GridCol + dirCol * distance;
    int destRow = caster.GridRow + dirRow * distance;
    if (!BoardHelper.IsValidCombatPosition(destCol, destRow)) return;
    if (state.GetUnitAtGrid(destCol, destRow) != CombatUnit.InvalidId) return;

    state.ClearGrid(caster.GridCol, caster.GridRow);
    caster.GridCol = (byte)destCol;
    caster.GridRow = (byte)destRow;
    state.SetGrid(destCol, destRow, caster.CombatId);
    state.EventQueue?.PushUnitMoved(caster.CombatId, (byte)destCol, (byte)destRow);
}
```

- [ ] **Step 3: ExecuteRetarget 구현**

```csharp
private static void ExecuteRetarget(ref SkillAction action, SkillExecuteContext ctx)
{
    ref var caster = ref ctx.GetCaster();
    int newTarget;

    switch (action.TargetFilter)
    {
        case SkillTargetFilter.LowestHpAllies:
        {
            // "LowestHpAllies"를 적 대상으로 사용 (네이밍 재활용)
            byte enemyTeam = (byte)(1 - ctx.CasterTeam);
            newTarget = FindNextTarget(ctx.State, enemyTeam, action.ExcludeHit,
                ctx.HitIds, ctx.HitIdCount, findLowestHp: true);
            break;
        }
        case SkillTargetFilter.NearestEnemy:
        {
            newTarget = FindNextTarget(ctx.State, (byte)(1 - ctx.CasterTeam),
                action.ExcludeHit, ctx.HitIds, ctx.HitIdCount, findLowestHp: false,
                refCol: caster.GridCol, refRow: caster.GridRow);
            break;
        }
        default:
            newTarget = TargetingSystem.FindNearestEnemy(ctx.State, ref caster);
            break;
    }

    ctx.TargetCombatId = newTarget;
}

private static int FindNextTarget(CombatMatchState state, byte enemyTeam, bool excludeHit,
    int[] hitIds, int hitIdCount, bool findLowestHp,
    int refCol = 0, int refRow = 0)
{
    int bestId = CombatUnit.InvalidId;
    int bestScore = findLowestHp ? int.MaxValue : int.MaxValue; // HP or distance

    for (int i = 0; i < state.UnitCount; i++)
    {
        ref var u = ref state.Units[i];
        if (!u.IsAlive || u.TeamIndex != enemyTeam) continue;

        if (excludeHit && hitIds != null)
        {
            bool hit = false;
            for (int j = 0; j < hitIdCount; j++)
                if (hitIds[j] == u.CombatId) { hit = true; break; }
            if (hit) continue;
        }

        int score = findLowestHp
            ? u.CurrentHP
            : (System.Math.Abs(u.GridCol - refCol) + System.Math.Abs(u.GridRow - refRow));

        if (score < bestScore) { bestScore = score; bestId = u.CombatId; }
    }
    return bestId;
}
```

- [ ] **Step 4: ExecuteApplyStatusEffect 구현**

```csharp
private static void ExecuteApplyStatusEffect(ref SkillAction action, SkillExecuteContext ctx)
{
    int duration = ctx.GetParamValue(action.SecondaryParamIndex);
    int value = action.ParamIndex >= 0 ? ctx.GetParamValue(action.ParamIndex) : 0;

    int targetIdx;
    if (action.TargetFilter == SkillTargetFilter.Self)
        targetIdx = ctx.State.FindUnitIndex(ctx.CasterCombatId);
    else
        targetIdx = ctx.State.FindUnitIndex(ctx.TargetCombatId);

    if (targetIdx >= 0)
        StatusEffectSystem.AddEffect(ctx.State, targetIdx, action.StatusEffect, value, duration);
}
```

- [ ] **Step 5: ExecuteDamage에 ExcludePrimary + DecayParamIndex 지원**

ExecuteDamage 내부, EnemiesInArea 순회에서:
```csharp
// ExcludePrimary: 메인 타겟 제외 (미노 스플래시)
if (action.ExcludePrimary && unit.CombatId == ctx.TargetCombatId) continue;
```

DamageWithDecay는 일반 Damage와 같은 ExecuteDamage에서 처리.
DecayParamIndex가 >=0이면 ctx.CurrentPower 사용 (BasePowerPercent 대신):
```csharp
int power;
if (action.DecayParamIndex >= 0)
    power = ctx.CurrentPower; // SimSkillGeneric이 감쇠 적용한 값
else
    power = ctx.GetParamValue(action.ParamIndex);
```

- [ ] **Step 6: ExecuteBuff에 ScaleByHitCount 지원**

```csharp
int value = ctx.GetParamValue(action.ParamIndex);
if (action.ScaleByHitCount)
    value *= ctx.BounceCount;
```

- [ ] **Step 7: SkillAreaHelper.IsInArea에 Rect case 추가**

```csharp
case SkillAreaShape.Rect:
{
    // 방향 기반 직사각형: 좌우 range, 전방 rectDepth
    // dirCol/dirRow는 ctx에서 전달 필요 — 일단 시전자→타겟 방향 사용
    // 호출 시 rectDepth를 별도 파라미터로 전달
    break;
}
```

※ Rect 판정은 오데트 전용이라 ExecuteDamage 내부에서 직접 처리하는 게 더 단순할 수 있음. 구현 시 판단.

- [ ] **Step 8: SkillExecuteContext에 체이닝 필드 추가**

```csharp
// SkillExecuteContext에 추가
public int CurrentPower;         // 감쇠 적용된 현재 파워 (베인)
public int BounceCount;          // 현재 바운스/히트 횟수
public int[] HitIds;             // 히트한 타겟 ID 배열 참조
public int HitIdCount;           // HitIds 유효 개수
```

- [ ] **Step 9: 컴파일 확인**

- [ ] **Step 10: 커밋**
```
feat: add ExecuteTeleport, ExecuteRetarget, ExecuteApplyStatusEffect to ActionExecutor
```

---

## Task 4: SimSkillGeneric — 상태 확장 + OnKnockbackWall + OnProjectileArrive + multi-hitframe

**Files:**
- Modify: `Assets/_Project/Scripts/InGame_New/Simulation/Combat/Skills/SimSkillGeneric.cs`

- [ ] **Step 1: 상태 필드 추가**

```csharp
// ── 체이닝 상태 ──
private bool _knockbackHitWall;
private int _projectileArrivalTimer;
private int _currentPower;
private int _bounceCount;
private int _decayPercent;
private readonly int[] _hitIds = new int[8];
private int _hitIdCount;

// ── 복수 HitFrame (오데트 2페이즈) ──
private int _currentHitFrameIndex;
private int _hitFrameTimer;
private bool _hasMultiHitFrames;
```

- [ ] **Step 2: InitChanneling에서 multi-hitframe 초기화**

```csharp
// InitChanneling 끝에 추가:
// Recipe에 AtHitFrame + hitFrameIndex > 0인 액션이 있으면 multi-hitframe 모드
_hasMultiHitFrames = false;
if (_recipe.Actions != null)
{
    for (int i = 0; i < _recipe.Actions.Length; i++)
    {
        if (_recipe.Actions[i].Trigger == SkillTriggerType.AtHitFrame && _recipe.Actions[i].HitFrameIndex > 0)
        {
            _hasMultiHitFrames = true;
            break;
        }
    }
}
if (_hasMultiHitFrames && SkillHitFrames != null && SkillHitFrames.Length > 0)
{
    _currentHitFrameIndex = 0;
    _hitFrameTimer = SkillHitFrames[0];
}
```

- [ ] **Step 3: OnChannelTick에 multi-hitframe + 투사체 도착 처리 추가**

기존 OnChannelTick의 `_startDelay` 처리 이후, 다음 로직 추가:

```csharp
// Multi-hitframe 처리 (오데트 2페이즈)
if (_hasMultiHitFrames)
{
    if (_hitFrameTimer > 0)
    {
        _hitFrameTimer--;
        if (_hitFrameTimer <= 0)
        {
            var hfCtx = MakeContext(state, ref caster, _cachedTargetId, ref rng);
            DispatchActionsForHitFrame(_currentHitFrameIndex, hfCtx);
            rng = hfCtx.Rng;
            _currentHitFrameIndex++;

            if (SkillHitFrames != null && _currentHitFrameIndex < SkillHitFrames.Length)
                _hitFrameTimer = SkillHitFrames[_currentHitFrameIndex] - SkillHitFrames[_currentHitFrameIndex - 1];
        }
    }
}

// 투사체 도착 타이머 (라키유/미노/베인)
if (_projectileArrivalTimer > 0)
{
    _projectileArrivalTimer--;
    if (_projectileArrivalTimer <= 0)
    {
        _bounceCount++;
        if (_hitIdCount < _hitIds.Length)
            _hitIds[_hitIdCount++] = _cachedTargetId;

        if (_decayPercent > 0)
            _currentPower = _currentPower * (100 - _decayPercent) / 100;

        var paCtx = MakeContext(state, ref caster, _cachedTargetId, ref rng);
        DispatchActions(SkillTriggerType.OnProjectileArrive, _bounceCount, paCtx);
        rng = paCtx.Rng;
        _cachedTargetId = paCtx.TargetCombatId; // Retarget 결과 동기화
    }
}
```

- [ ] **Step 4: DispatchActions에서 Knockback 특별 처리**

DispatchActions 메서드 내부, ActionExecutor.Execute 호출 전에:

```csharp
// Knockback 특별 처리: 결과를 직접 저장 + OnKnockbackWall 디스패치
if (action.Effect == SkillEffectType.Knockback)
{
    int dist = action.KnockbackDistance > 0
        ? action.KnockbackDistance
        : ctx.GetParamValue(action.SecondaryParamIndex);
    if (dist <= 0) dist = 2;

    int targetIdx = ctx.State.FindUnitIndex(ctx.TargetCombatId);
    if (targetIdx >= 0)
    {
        ref var target = ref ctx.State.Units[targetIdx];
        ref var casterRef = ref ctx.GetCaster();
        int dirCol = target.GridCol - casterRef.GridCol;
        int dirRow = target.GridRow - casterRef.GridRow;
        if (dirCol == 0 && dirRow == 0)
            dirCol = ctx.CasterTeam == 0 ? 1 : -1;
        else
        {
            dirCol = dirCol > 0 ? 1 : dirCol < 0 ? -1 : 0;
            dirRow = dirRow > 0 ? 1 : dirRow < 0 ? -1 : 0;
        }

        int actualMoved = SkillCCHelper.Knockback(ctx.State, ref target, dirCol, dirRow, dist, _worldTickRate);
        _knockbackHitWall = actualMoved < dist;
    }
    else
    {
        _knockbackHitWall = false;
    }

    if (_knockbackHitWall)
        DispatchActions(SkillTriggerType.OnKnockbackWall, tickCount, ctx);

    continue; // ActionExecutor.Execute 건너뜀
}
```

- [ ] **Step 5: DispatchActionsForHitFrame 메서드 추가**

```csharp
private void DispatchActionsForHitFrame(int hitFrameIndex, SkillExecuteContext ctx)
{
    if (_recipe.Actions == null) return;
    for (int i = 0; i < _recipe.Actions.Length; i++)
    {
        ref var action = ref _recipe.Actions[i];
        if (action.Trigger != SkillTriggerType.AtHitFrame) continue;
        if (action.HitFrameIndex != hitFrameIndex) continue;

        // Knockback 특별 처리 (위와 동일)
        if (action.Effect == SkillEffectType.Knockback)
        {
            // ... (DispatchActions와 동일 로직)
            continue;
        }

        ActionExecutor.Execute(ref action, ctx);
    }
}
```

- [ ] **Step 6: SpawnProjectile 실행 시 도착 타이머 시작**

DispatchActions에서 SpawnProjectile 액션 실행 후:
```csharp
if (action.Effect == SkillEffectType.SpawnProjectile)
{
    ActionExecutor.Execute(ref action, ctx);
    // 도착 타이머 시작 (OnProjectileArrive 지원)
    int travelFrames = action.RepeatIntervalFrames > 0 ? action.RepeatIntervalFrames : 30;
    _projectileArrivalTimer = travelFrames + 3; // +3 = DamageDelayFrames
    continue;
}
```

- [ ] **Step 7: MakeContext에 체이닝 필드 전달**

MakeContext 수정:
```csharp
return new SkillExecuteContext
{
    // ... 기존 필드 ...
    CurrentPower = _currentPower > 0 ? _currentPower : PowerPercent,
    BounceCount = _bounceCount,
    HitIds = _hitIds,
    HitIdCount = _hitIdCount,
};
```

- [ ] **Step 8: Reset에 새 필드 초기화 추가**

```csharp
_knockbackHitWall = false;
_projectileArrivalTimer = 0;
_currentPower = 0;
_bounceCount = 0;
_decayPercent = 0;
_hitIdCount = 0;
_currentHitFrameIndex = 0;
_hitFrameTimer = 0;
_hasMultiHitFrames = false;
for (int i = 0; i < _hitIds.Length; i++) _hitIds[i] = CombatUnit.InvalidId;
```

- [ ] **Step 9: Execute에서 _currentPower / _decayPercent 초기화**

Execute 메서드에서:
```csharp
_currentPower = PowerPercent;
_bounceCount = 0;
_hitIdCount = 0;

// Recipe에서 DecayParamIndex가 있는 액션 찾아서 감쇠율 캐시
if (_recipe.Actions != null)
{
    for (int i = 0; i < _recipe.Actions.Length; i++)
    {
        if (_recipe.Actions[i].DecayParamIndex >= 0)
        {
            _decayPercent = _paramValues != null && _recipe.Actions[i].DecayParamIndex < _paramValues.Length
                ? _paramValues[_recipe.Actions[i].DecayParamIndex] : 0;
            break;
        }
    }
}
```

- [ ] **Step 10: 컴파일 확인**

- [ ] **Step 11: 커밋**
```
feat: add chaining state, OnKnockbackWall, OnProjectileArrive, multi-hitframe to SimSkillGeneric
```

---

## Task 5: 테토라 + 라키유 Recipe 전환

**Files:**
- Modify: `Assets/_Project/Scripts/InGame_New/Simulation/Combat/Skills/SkillFactory.Character.cs`
- Modify: `Assets/_Project/Scripts/InGame_New/Simulation/Combat/Skills/SkillFactory.cs` (RegisterCustomSkills)

- [ ] **Step 1: 테토라 Recipe 선언 추가**

Character.cs의 커스텀 섹션에서 테토라 기존 선언을 교체:
```csharp
// ── 테토라: Damage + 넉백 4칸 + 벽 충돌 시 착지 AoE ──
Skill(217413301, E.DelayedApply, T.NearestEnemy)
    .Param(1, P.Int, 200f)           // [0] 주 데미지
    .Param(3, P.Int, 200f)           // [1] 착지 AoE 데미지
    .AtHit(Vfx(0, V.AtCasterWithDir))
    .AtHit(Damage(paramIndex: 0))
    .AtHit(Knockback(fixedDistance: 4))
    .OnKnockbackWall(Vfx(1, V.AtGridPos))
    .OnKnockbackWall(Damage(paramIndex: 1, filter: F.EnemiesInArea, area: S.Circle, range: 1))
    .WithTags(TraitTag.Damage | TraitTag.Knockback | TraitTag.CC | TraitTag.AoE)
    .Register();
```

- [ ] **Step 2: 라키유 Recipe 선언 교체**

```csharp
// ── 라키유: 베지어 투사체 → 범위 디버프 ──
Skill(217353203, E.Channeling, T.NearestEnemy).Projectile()
    .Param(1, P.Frames, 3f)          // [0] 디버프 지속
    .Param(2, P.Int, 50f)            // [1] 회복감소%
    .Param(3, P.Int, 30f)            // [2] 방어감소%
    .AtHit(SpawnProjectile(paramIndex: -1, vfxIndex: 0, travelFrames: 15))
    .OnProjectileArrive(AreaVfx(V.AreaEffect, 1))
    .OnProjectileArrive(Debuff(StatusEffectType.None, 2, 0, F.EnemiesInArea, S.Circle, 1))
    .OnProjectileArrive(Debuff(StatusEffectType.None, 2, 0, F.EnemiesInArea, S.Circle, 1))
    .OnProjectileArrive(Debuff(StatusEffectType.HealReduction, 1, 0, F.EnemiesInArea, S.Circle, 1))
    .WithTags(TraitTag.Projectile | TraitTag.Debuff | TraitTag.AoE)
    .Register();
```

- [ ] **Step 3: RegisterCustomSkills에서 테토라/라키유 제거**

```csharp
// 삭제:
Register(217413301, () => new SimSkillTetoraKnockback());
Register(217353203, () => new SimSkillRakiyuDebuff());
```

- [ ] **Step 4: Custom 클래스 파일 삭제**

```
rm SimSkillTetoraKnockback.cs + .meta
rm SimSkillRakiyuDebuff.cs + .meta
```

- [ ] **Step 5: 컴파일 + 인게임 테토라/라키유 검증**

- [ ] **Step 6: 커밋**
```
feat: convert Tetora and Rakiyu to Recipe (remove 2 Custom classes)
```

---

## Task 6: 마리에 + 시라유키 Recipe 전환

**Files:**
- Modify: `Assets/_Project/Scripts/InGame_New/Simulation/Combat/Skills/SkillFactory.Character.cs`
- Modify: `Assets/_Project/Scripts/InGame_New/Simulation/Combat/Skills/SkillFactory.cs`

- [ ] **Step 1: 마리에 Recipe 선언 교체**

```csharp
// ── 마리에: 텔레포트 + 다단히트 + 디버프 ──
Skill(217563405, E.Channeling, T.HighestAttackEnemy)
    .Param(2, P.Int, 200f)           // [0] 데미지 배율
    .Param(1, P.Int, 4f)             // [1] 히트수
    .Param(3, P.Frames, 3f)          // [2] 디버프 지속
    .Param(4, P.Int, 30f)            // [3] 디버프%
    .AtHit(Teleport())
    .AtHit(Vfx(0, V.AtTarget))
    .OnTick(WithRepeat(Damage(paramIndex: 0), dynamicFromClip: true))
    .OnComplete(Debuff(StatusEffectType.None, 3, 2, F.PrimaryTarget))
    .OnComplete(Debuff(StatusEffectType.None, 3, 2, F.PrimaryTarget))
    .OnComplete(AddMarker(SkillMarkerType.MarieAracne))
    .WithTags(TraitTag.Damage | TraitTag.Teleport | TraitTag.Debuff | TraitTag.MultiHit)
    .Register();
```

- [ ] **Step 2: 시라유키 Recipe 선언 교체**

```csharp
// ── 시라유키: 순차 텔레포트 + 지정불가 + 회피버프 ──
Skill(217663506, E.Channeling, T.LowestHPEnemy)
    .Param(2, P.Int, 200f)           // [0] 데미지 배율
    .Param(1, P.Frames, 3f)          // [1] 지정불가 시간
    .Param(3, P.Frames, 3f)          // [2] 회피 버프 지속
    .Param(4, P.Int, 30f)            // [3] 회피 증가율%
    .OnCast(ApplyStatusEffect(StatusEffectType.TargetImpossible, F.Self, durationParamIndex: 1))
    .OnCast(Vfx(0, V.AtCaster))
    .OnTick(WithRepeat(Teleport(), count: 3, dynamicFromClip: true))
    .OnTick(Vfx(1, V.AtTarget))
    .OnTick(Damage(paramIndex: 0))
    .OnTick(Retarget(F.LowestHpAllies))
    .OnComplete(Buff(StatModType.DodgeChance, 3, 2))
    .WithTags(TraitTag.Damage | TraitTag.Teleport | TraitTag.Buff)
    .Register();
```

- [ ] **Step 3: RegisterCustomSkills에서 제거 + Custom 파일 삭제**

- [ ] **Step 4: 컴파일 + 인게임 마리에/시라유키 검증**

- [ ] **Step 5: 커밋**
```
feat: convert Marie and Shirayuki to Recipe (remove 2 Custom classes)
```

---

## Task 7: 미노 + 오데트 + 베인 Recipe 전환

**Files:**
- Modify: `Assets/_Project/Scripts/InGame_New/Simulation/Combat/Skills/SkillFactory.Character.cs`
- Modify: `Assets/_Project/Scripts/InGame_New/Simulation/Combat/Skills/SkillFactory.cs`

- [ ] **Step 1: 미노 Recipe 선언 교체**

```csharp
// ── 미노: 최저HP 3명 순차 미사일 + Plus 스플래시 ──
Skill(217433302, E.Channeling, T.LowestHPEnemy).Projectile()
    .Param(1, P.Int, 200f)           // [0] 데미지 배율
    .OnTick(Retarget(F.LowestHpAllies, excludeHit: true))
    .OnTick(WithRepeat(
        SpawnProjectile(paramIndex: 0, vfxIndex: 0, travelFrames: 15),
        count: 3, intervalMs: 300))
    .OnProjectileArrive(Damage(paramIndex: 0))
    .OnProjectileArrive(Damage(paramIndex: 0, filter: F.EnemiesInArea, area: S.Plus, range: 1, excludePrimary: true))
    .WithTags(TraitTag.Damage | TraitTag.Projectile | TraitTag.MultiHit)
    .Register();
```

- [ ] **Step 2: 오데트 Recipe 선언 교체**

```csharp
// ── 오데트: 2단계 — Phase1 Rect AoE + Phase2 텔레포트 3×3 ──
Skill(217613501, E.Channeling, T.NearestEnemy)
    .Param(1, P.Int, 200f)           // [0] 데미지 배율
    .Param(2, P.Frames, 3f)          // [1] 디버프 지속
    .Param(3, P.Int, 30f)            // [2] 공속감소%
    .AtHit(Damage(paramIndex: 0, filter: F.EnemiesInArea, area: S.Rect, range: 1, rectDepth: 1), hitFrameIndex: 0)
    .AtHit(Debuff(StatusEffectType.None, 2, 1, F.EnemiesInArea, S.Rect, 1), hitFrameIndex: 0)
    .AtHit(AddMarker(SkillMarkerType.OdetteCold), hitFrameIndex: 0)
    .AtHit(Vfx(0, V.AtCasterWithDir), hitFrameIndex: 0)
    .AtHit(Teleport(distance: 2), hitFrameIndex: 1)
    .AtHit(Damage(paramIndex: 0, filter: F.EnemiesInArea, area: S.Circle, range: 1), hitFrameIndex: 1)
    .AtHit(Debuff(StatusEffectType.None, 2, 1, F.EnemiesInArea, S.Circle, 1), hitFrameIndex: 1)
    .AtHit(AddMarker(SkillMarkerType.OdetteCold), hitFrameIndex: 1)
    .AtHit(Vfx(1, V.AtCaster), hitFrameIndex: 1)
    .WithTags(TraitTag.Damage | TraitTag.Teleport | TraitTag.Debuff | TraitTag.AoE)
    .Register();
```

- [ ] **Step 3: 베인 Recipe 선언 교체**

```csharp
// ── 베인: 바운스 투사체 + 감쇠 + 공속 버프(히트당) ──
Skill(217363204, E.Channeling, T.NearestEnemy).Projectile()
    .Param(1, P.Int, 200f)           // [0] 데미지 배율
    .Param(2, P.Int, 20f)            // [1] 감쇠율%
    .Param(3, P.Frames, 3f)          // [2] 공속 버프 지속
    .Param(4, P.Int, 30f)            // [3] 공속 증가율% (히트당)
    .Param(5, P.Int, 5f)             // [4] 최대 바운스
    .AtHit(SpawnProjectile(paramIndex: 0, vfxIndex: 0, travelFrames: 9))
    .OnProjectileArrive(DamageWithDecay(paramIndex: 0, decayParamIndex: 1))
    .OnProjectileArrive(Retarget(F.NearestEnemy, excludeHit: true))
    .OnProjectileArrive(SpawnProjectile(paramIndex: 0, vfxIndex: 0, travelFrames: 9))
    .OnComplete(BuffScaled(StatModType.AttackSpeed, 3, 2, scaleByHitCount: true))
    .WithTags(TraitTag.Damage | TraitTag.Projectile | TraitTag.Buff)
    .Register();
```

- [ ] **Step 4: RegisterCustomSkills에서 3개 제거 + Custom 파일 삭제**

```
rm SimSkillMinoProjectile.cs + .meta
rm SimSkillOdetteStrike.cs + .meta
rm SimSkillVeinBounce.cs + .meta
```

- [ ] **Step 5: RegisterCustomSkills 최종 확인**

남은 4개만:
```csharp
Register(217333202, () => new SimSkillAprilBarrage());
Register(217653505, () => new SimSkillEnkiWaveHeal());
Register(217263103, () => new SimSkillRukidaFoxfire());
Register(217523403, () => new SimSkillAdriaExpand());
```

- [ ] **Step 6: 컴파일 + 인게임 미노/오데트/베인 검증**

- [ ] **Step 7: 커밋**
```
feat: convert Mino, Odette, Vein to Recipe (remove 3 Custom classes, 11→4 Custom total)
```
