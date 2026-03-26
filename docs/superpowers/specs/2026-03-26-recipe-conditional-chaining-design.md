# Recipe 조건부 체이닝 — Custom 스킬 7개 Recipe 전환

## 한 줄 요약

새 Trigger 2개 + 새 EffectType 3개 + SkillAreaShape 1개 + SimSkillGeneric 상태 확장으로
Custom 스킬 7개를 Recipe 선언으로 전환. Custom 11개 → 4개로 축소.

## 설계 원칙

**Recipe = 선언, 시스템 = 실행.**
- EffectType은 기존 시스템(TargetingSystem, GridSystem, ProjectileSystem, SkillCCHelper)의 호출 래퍼
- Trigger는 기존 시스템의 반환값/콜백을 조건으로 사용
- 스킬 전용 로직을 만들지 않음

## 추가할 것

### 새 EffectType 3개

```csharp
SkillEffectType.Teleport           // GridSystem으로 순간이동
SkillEffectType.Retarget           // TargetingSystem으로 다음 타겟 선택
SkillEffectType.ApplyStatusEffect  // StatusEffectSystem으로 상태이상 적용 (CC와 별도)
```

**Teleport**:
- `TeleportDistance == 0` → 타겟 뒤로 이동 (마리에/시라유키)
- `TeleportDistance > 0` → 타겟 방향 전방 N칸 이동 (오데트)
- 내부: 기존 GridSystem의 ClearGrid/SetGrid + EventQueue.PushUnitMoved 호출
- 뒤쪽 타일 막혀있으면 인접 빈 타일 탐색 (기존 TeleportBehindTarget 로직을 ActionExecutor로 이동)

**Retarget**:
- SkillAction.TargetFilter로 규칙 지정
- `ExcludeHit` 플래그: 이미 히트한 타겟 제외 (베인/미노)
- 내부: 기존 TargetingSystem.FindTarget 호출 → SimSkillGeneric._cachedTargetId 갱신

**ApplyStatusEffect** (CC와 다름):
- StatusEffectSystem.AddEffect() 직접 호출 (CC 시스템 경유 안 함)
- TargetFilter 지원: Self(시라유키 TargetImpossible), PrimaryTarget 등
- CC 시스템(SkillCCHelper.ApplyCC)과 StatusEffect 시스템(StatusEffectSystem.AddEffect)은 다른 API
- 시라유키의 TargetImpossible은 CC가 아니라 StatusEffect (지속시간 만료 방식)

### 새 Trigger 2개

```csharp
SkillTriggerType.OnKnockbackWall   // 넉백이 벽에 충돌했을 때
SkillTriggerType.OnProjectileArrive // 투사체가 도착했을 때
```

**OnKnockbackWall**:
- SkillCCHelper.Knockback() 반환값 `actualMoved < requestedDistance` 체크
- 착지 지점(넉백 후 타겟 현재 위치)을 Area 중심으로 사용
- **결과 전달 방식**: Knockback 실행을 ActionExecutor가 아닌 SimSkillGeneric.DispatchActions 내부에서 직접 처리.
  ActionExecutor.Execute는 static이라 반환값 전파 불가 → DispatchActions가 Knockback을 특별 처리하여 결과를 `_knockbackHitWall`에 저장.
- 벽 충돌 시 스턴은 SkillCCHelper.Knockback 내부에서 자동 적용됨 (Recipe에서 별도 처리 불필요)

**OnProjectileArrive**:
- SpawnProjectile 실행 시 SimSkillGeneric이 도착 타이머 시작 (`_projectileArrivalTimer = travelFrames`)
- OnChannelTick에서 카운트다운 → 0 도달 시 OnProjectileArrive 트리거 디스패치
- 도착 지점을 Area 중심으로 사용
- **체인 지원**: OnProjectileArrive에서 SpawnProjectile 재실행 → 새 타이머 시작 → 루프 (베인 바운스)
- SpawnProjectile은 damage=0으로 VFX 전용 투사체 생성 (실제 데미지는 OnProjectileArrive에서 처리)

### 새 SkillAreaShape 1개

```csharp
SkillAreaShape.Rect   // 방향 기반 직사각형 (폭 × 깊이)
```

- AreaRange = 좌우 반경 (1 = 3칸 폭)
- RectDepth 필드 = 전방 깊이 (1 = 시전자 행 + 전방 1행 = 2행)
- 오데트 Phase 1의 ⊔자형(3폭 × 2깊이) 패턴 표현
- ※ TeleportDistance와 별도 필드로 분리 (의미 충돌 방지)

### 새 SkillTargetFilter 1개

```csharp
SkillTargetFilter.NearestEnemy   // 가장 가까운 적 (Retarget에서 사용, 베인)
```

### SkillAction 필드 추가

```csharp
// SkillAction struct에 추가
public byte TeleportDistance;     // Teleport: 전방 이동 거리 (0이면 타겟 뒤로)
public byte RectDepth;            // Rect: 전방 깊이 (TeleportDistance와 분리)
public byte KnockbackDistance;    // Knockback: 고정 거리 (0이면 SecondaryParamIndex 사용)
public bool ExcludeHit;           // Retarget: 이미 히트한 타겟 제외
public bool ExcludePrimary;       // Damage(Area): 메인 타겟 제외 (미노 Plus 스플래시)
public bool ScaleByHitCount;      // Buff: 값에 히트수를 곱함
public sbyte DecayParamIndex;     // Damage: 바운스 감쇠율 ParamSlots 인덱스 (-1=없음)
public StatusEffectType StatusEffect2; // ApplyStatusEffect: 적용할 상태이상 타입 (기존 StatusEffect 필드와 별도)
```

### SimSkillGeneric 상태 확장

```csharp
// 넉백 결과
private bool _knockbackHitWall;

// 투사체 도착 타이머
private int _projectileArrivalTimer;

// 바운스/히트 추적
private int _currentPower;           // 감쇠 적용된 현재 파워
private int _bounceCount;            // 현재 바운스/히트 횟수
private readonly int[] _hitIds = new int[8];  // 히트한 타겟 ID 추적 (최대 8)
private int _hitIdCount;

// 복수 HitFrame 지원 (오데트 2페이즈)
private int _currentHitFrameIndex;   // 현재 대기 중인 SkillHitFrames 인덱스
private int _hitFrameTimer;          // 현재 HitFrame까지 남은 프레임
```

## 리뷰 반영: Critical 이슈 해결

### C1. 오데트 multi-hitframe 디스패치

**문제**: SimSkillGeneric의 DispatchActions는 AtHitFrame 액션을 hitFrameIndex로 필터링하지 않음. 모든 AtHit 액션이 동시 실행.

**해결**: SimSkillGeneric.OnChannelTick에 복수 hitframe 순차 디스패치 추가.

```csharp
// OnChannelTick 내부:
// _currentHitFrameIndex를 순차 증가하며 SkillHitFrames[N] 타이밍에 도달 시
// hitFrameIndex == N인 AtHitFrame 액션만 디스패치
if (_hitFrameTimer > 0)
{
    _hitFrameTimer--;
    if (_hitFrameTimer <= 0)
    {
        // hitFrameIndex == _currentHitFrameIndex인 액션만 실행
        DispatchActionsForHitFrame(_currentHitFrameIndex, ctx);
        _currentHitFrameIndex++;

        // 다음 hitframe이 있으면 타이머 설정
        if (SkillHitFrames != null && _currentHitFrameIndex < SkillHitFrames.Length)
        {
            _hitFrameTimer = SkillHitFrames[_currentHitFrameIndex]
                           - SkillHitFrames[_currentHitFrameIndex - 1];
        }
    }
}
```

DispatchActions 수정 — hitFrameIndex 필터링 추가:
```csharp
private void DispatchActionsForHitFrame(int hitFrameIndex, SkillExecuteContext ctx)
{
    for (int i = 0; i < _recipe.Actions.Length; i++)
    {
        ref var action = ref _recipe.Actions[i];
        if (action.Trigger != SkillTriggerType.AtHitFrame) continue;
        if (action.HitFrameIndex != hitFrameIndex) continue;
        ActionExecutor.Execute(ref action, ctx);
    }
}
```

### C2+C3. 시라유키 StatusEffect Self 적용

**문제**: TargetImpossible은 CC(SkillCCHelper)가 아니라 StatusEffect(StatusEffectSystem). 현재 ExecuteCC는 PrimaryTarget만 지원.

**해결**: 새 EffectType `ApplyStatusEffect` 추가. Self/PrimaryTarget 모두 지원.

```csharp
// 시라유키 Recipe (수정 후)
.OnCast(ApplyStatusEffect(StatusEffectType.TargetImpossible, F.Self, durationParamIndex: 1))

// ActionExecutor.ExecuteApplyStatusEffect()
private static void ExecuteApplyStatusEffect(ref SkillAction action, SkillExecuteContext ctx)
{
    int duration = ctx.GetParamValue(action.SecondaryParamIndex);
    int value = ctx.GetParamValue(action.ParamIndex);

    int targetIdx;
    if (action.TargetFilter == SkillTargetFilter.Self)
        targetIdx = ctx.State.FindUnitIndex(ctx.CasterCombatId);
    else
        targetIdx = ctx.State.FindUnitIndex(ctx.TargetCombatId);

    if (targetIdx >= 0)
        StatusEffectSystem.AddEffect(ctx.State, targetIdx, action.StatusEffect2, value, duration);
}
```

### C4. Knockback 결과 전달

**문제**: ActionExecutor.Execute는 static이라 Knockback 반환값이 SimSkillGeneric으로 전파 불가.

**해결**: Knockback을 ActionExecutor에서 실행하지 않고, **SimSkillGeneric.DispatchActions 내부에서 직접 처리**.

```csharp
// SimSkillGeneric.DispatchActions 수정:
for (int i = 0; i < _recipe.Actions.Length; i++)
{
    ref var action = ref _recipe.Actions[i];
    if (action.Trigger != trigger) continue;
    if (!CheckCondition(action.Condition, tickCount)) continue;

    // Knockback 특별 처리: 결과를 직접 저장
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
            // 방향 계산 (기존 ExecuteKnockback 로직)
            ref var caster = ref ctx.GetCaster();
            int dirCol = target.GridCol - caster.GridCol;
            int dirRow = target.GridRow - caster.GridRow;
            if (dirCol == 0 && dirRow == 0) dirCol = caster.TeamIndex == 0 ? 1 : -1;
            else { dirCol = dirCol > 0 ? 1 : dirCol < 0 ? -1 : 0; dirRow = dirRow > 0 ? 1 : dirRow < 0 ? -1 : 0; }

            int actualMoved = SkillCCHelper.Knockback(ctx.State, ref target, dirCol, dirRow, dist, _worldTickRate);
            _knockbackHitWall = actualMoved < dist;
        }

        // OnKnockbackWall 즉시 디스패치
        if (_knockbackHitWall)
            DispatchActions(SkillTriggerType.OnKnockbackWall, tickCount, ctx);
        continue; // ActionExecutor.Execute 건너뜀
    }

    ActionExecutor.Execute(ref action, ctx);
}
```

## 리뷰 반영: Important 이슈 해결

### I1. 테토라 넉백 4칸 고정

Knockback 팩토리에 `fixedDistance` 파라미터 추가:
```csharp
public static SkillAction Knockback(sbyte distParamIndex = -1, byte fixedDistance = 0)
    => new SkillAction { Effect = SkillEffectType.Knockback, SecondaryParamIndex = distParamIndex,
        KnockbackDistance = fixedDistance };
```
테토라: `.AtHit(Knockback(fixedDistance: 4))`

### I2. 라키유 디버프 타입 명시

```csharp
// 수정: BuffStat을 명시적으로 지정
.OnProjectileArrive(Debuff(StatusEffectType.None, 2, 0, F.EnemiesInArea, S.Circle, 1, buffStat: StatModType.AdReduce))
.OnProjectileArrive(Debuff(StatusEffectType.None, 2, 0, F.EnemiesInArea, S.Circle, 1, buffStat: StatModType.ApReduce))
.OnProjectileArrive(Debuff(StatusEffectType.HealReduction, 1, 0, F.EnemiesInArea, S.Circle, 1))
```

### I3. 라키유 ExecutionType

라키유는 Channeling이 맞음. 투사체 도착 타이머를 OnChannelTick에서 관리해야 하므로.
```csharp
// 수정
Skill(217353203, E.Channeling, T.NearestEnemy).Projectile()
```

### I4. 미노 타겟 수집 방식

미노의 Custom 코드는 시전 시 3명을 사전 수집. Recipe에서도 같은 동작을 위해:
- Execute 시점에 Retarget을 3번 실행하여 `_hitIds`에 사전 수집
- 또는 OnCast에서 `CollectTargets` 전용 EffectType 추가

**간소화**: Retarget의 `excludeHit`를 활용하되, 매 틱 재탐색이 아닌 **OnCast 시점에 3번 Retarget 실행 후 _hitIds에 저장**하는 방식.
실제 구현에서 미세한 동작 차이(사전 수집 vs 매 틱 재탐색)는 기획 확인 필요. 매 틱 재탐색이 기획적으로 문제없으면 현재 Recipe 그대로 유지.

### I5. 미노 Plus 스플래시 메인 타겟 제외

`ExcludePrimary` 필드 추가:
```csharp
.OnProjectileArrive(Damage(paramIndex: 0, filter: F.EnemiesInArea, area: S.Plus, range: 1, excludePrimary: true))
```

ActionExecutor.ExecuteDamage에서 `action.ExcludePrimary`이면 `ctx.TargetCombatId` 스킵.

### I6. 베인 NearestEnemy

SkillTargetFilter에 `NearestEnemy` 추가 (이미 SkillTargetType에는 NearestEnemy 존재).
Retarget에서 TargetingSystem.FindNearestEnemy 호출.

### I7. TeleportDistance/RectDepth 분리

`RectDepth` 별도 필드로 분리. 의미 충돌 방지.

### I8. Rect 깊이 의미 정의

Rect(AreaRange=1, RectDepth=1) = 좌우 1칸(3폭) × 전방 0~1행(시전자 행 포함 2행).
RectDepth는 "시전자 행 기준 전방 추가 행 수". RectDepth=1이면 시전자 행 + 1행 = 총 2행.

## 7개 스킬 Recipe 변환 (수정 후)

### 테토라

```csharp
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

### 라키유

```csharp
Skill(217353203, E.Channeling, T.NearestEnemy).Projectile()
    .Param(1, P.Frames, 3f)          // [0] 디버프 지속
    .Param(2, P.Int, 50f)            // [1] 회복감소%
    .Param(3, P.Int, 30f)            // [2] 방어감소%
    .AtHit(SpawnProjectile(paramIndex: -1, vfxIndex: 0, travelFrames: 15))
    .OnProjectileArrive(AreaVfx(V.AreaEffect, 1))
    .OnProjectileArrive(Debuff(StatusEffectType.None, 2, 0, F.EnemiesInArea, S.Circle, 1, buffStat: StatModType.AdReduce))
    .OnProjectileArrive(Debuff(StatusEffectType.None, 2, 0, F.EnemiesInArea, S.Circle, 1, buffStat: StatModType.ApReduce))
    .OnProjectileArrive(Debuff(StatusEffectType.HealReduction, 1, 0, F.EnemiesInArea, S.Circle, 1))
    .WithTags(TraitTag.Projectile | TraitTag.Debuff | TraitTag.AoE)
    .Register();
```

### 마리에

```csharp
Skill(217563405, E.Channeling, T.HighestAttackEnemy)
    .Param(2, P.Int, 200f)           // [0] 데미지 배율
    .Param(1, P.Int, 4f)             // [1] 히트수
    .Param(3, P.Frames, 3f)          // [2] 디버프 지속
    .Param(4, P.Int, 30f)            // [3] 디버프%
    .AtHit(Teleport())               // 타겟 뒤로
    .AtHit(Vfx(0, V.AtTarget))
    .OnTick(WithRepeat(Damage(paramIndex: 0), dynamicFromClip: true))
    .OnComplete(Debuff(StatusEffectType.None, 3, 2, F.PrimaryTarget, buffStat: StatModType.Attack))
    .OnComplete(Debuff(StatusEffectType.None, 3, 2, F.PrimaryTarget, buffStat: StatModType.Def))
    .OnComplete(AddMarker(SkillMarkerType.MarieAracne))
    .WithTags(TraitTag.Damage | TraitTag.Teleport | TraitTag.Debuff | TraitTag.MultiHit)
    .Register();
```

### 시라유키

```csharp
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

### 미노

```csharp
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

### 오데트

```csharp
Skill(217613501, E.Channeling, T.NearestEnemy)
    .Param(1, P.Int, 200f)           // [0] 데미지 배율
    .Param(2, P.Frames, 3f)          // [1] 디버프 지속
    .Param(3, P.Int, 30f)            // [2] 공속감소%
    // Phase 1 (SkillHitFrames[0])
    .AtHit(Damage(paramIndex: 0, filter: F.EnemiesInArea, area: S.Rect, range: 1, rectDepth: 1), hitFrameIndex: 0)
    .AtHit(Debuff(StatusEffectType.None, 2, 1, F.EnemiesInArea, S.Rect, 1, buffStat: StatModType.AttackSpeed), hitFrameIndex: 0)
    .AtHit(AddMarker(SkillMarkerType.OdetteCold), hitFrameIndex: 0)
    .AtHit(Vfx(0, V.AtCasterWithDir), hitFrameIndex: 0)
    // Phase 2 (SkillHitFrames[1])
    .AtHit(Teleport(distance: 2), hitFrameIndex: 1)
    .AtHit(Damage(paramIndex: 0, filter: F.EnemiesInArea, area: S.Circle, range: 1), hitFrameIndex: 1)
    .AtHit(Debuff(StatusEffectType.None, 2, 1, F.EnemiesInArea, S.Circle, 1, buffStat: StatModType.AttackSpeed), hitFrameIndex: 1)
    .AtHit(AddMarker(SkillMarkerType.OdetteCold), hitFrameIndex: 1)
    .AtHit(Vfx(1, V.AtCaster), hitFrameIndex: 1)
    .WithTags(TraitTag.Damage | TraitTag.Teleport | TraitTag.Debuff | TraitTag.AoE)
    .Register();
```

### 베인

```csharp
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

**바운스 루프 동작**:
1. AtHit: 첫 발사 (SkillHitFrames[0] 타이밍)
2. OnProjectileArrive: 도착 → DamageWithDecay(감쇠) → Retarget(미피격 적) → SpawnProjectile(재발사)
3. 재발사 → 새 타이머 → 다시 OnProjectileArrive → 2번 반복
4. Retarget 실패(적 없음) 또는 _bounceCount ≥ TargetCount(최대 바운스) → 루프 종료
5. OnComplete: BuffScaled (hitCount × value)
- OnComplete는 루프 정상 종료/Retarget 실패 양쪽에서 모두 발동 보장

## 최종 범위

| 스킬 | Recipe 전환 | 핵심 기능 블록 |
|------|-----------|-------------|
| 테토라 | O | Knockback(fixed:4) + OnKnockbackWall |
| 라키유 | O | SpawnProjectile + OnProjectileArrive(AoE Debuff) |
| 마리에 | O | Teleport + OnTick(Damage) + OnComplete(Debuff+Marker) |
| 시라유키 | O | ApplyStatusEffect(Self) + Teleport + Retarget + OnTick |
| 미노 | O | Retarget(excludeHit) + SpawnProjectile + OnProjectileArrive(+ExcludePrimary) |
| 오데트 | O | Rect AoE + AtHit(hitFrameIndex:0/1) + Teleport(forward) |
| 베인 | O | SpawnProjectile + OnProjectileArrive 체인 + Decay + ScaleByHitCount |

**Custom 11개 → 4개** (7개 Recipe 전환, ~2,291줄 삭제)

## 유지 대상 (Custom 4개)

| 파일 | 이유 |
|------|------|
| SimSkillAprilBarrage.cs | 거리별 배율 3단계 — Damage 계산 자체가 특수 |
| SimSkillEnkiWaveHeal.cs | 행 단위 이동 투사체 + HoT — 이동 패턴이 특수 |
| SimSkillRukidaFoxfire.cs | 스택 카운트 기반 동적 버프 스케일 |
| SimSkillAdriaExpand.cs | 확장 패턴(+→X→+) + 비트마스크 히트 중복 방지 |

## 변경 파일 목록

| 파일 | 변경 |
|------|------|
| `Enums.cs` | SkillEffectType +3, SkillTriggerType +2, SkillAreaShape +1, SkillTargetFilter +1 |
| `SkillRecipe.cs` | SkillAction에 7개 필드 추가 |
| `SkillFactory.cs` | Builder에 팩토리+빌더 메서드 추가 |
| `SkillFactory.Character.cs` | 7개 Custom → Recipe, RegisterCustomSkills 11→4 |
| `ActionExecutor.cs` | ExecuteTeleport, ExecuteRetarget, ExecuteApplyStatusEffect, Rect/ExcludePrimary 지원 |
| `SimSkillGeneric.cs` | 바운스/히트 추적, multi-hitframe, OnKnockbackWall/OnProjectileArrive 디스패치 |
| `SkillAreaHelper.cs` | IsInArea에 Rect case 추가 |
| **삭제**: 7개 Custom .cs + .meta | ~2,291줄 |

## 제약조건

- GC-free 유지 (모든 새 필드는 값 타입, _hitIds는 고정 배열 8개)
- 퀀텀 안전 (enum 추가만, 힙 할당 없음)
- 기존 스킬 동작 변경 없음 (순수 추가)
- ProjectileSystem 구조 변경 없음 (타이머 기반 도착 처리)
- TeleportBehindTarget/TryTeleport 로직은 ActionExecutor로 이동 (공용 static)
- OnProjectileArrive 체인 루프는 Retarget 실패 또는 _bounceCount ≥ TargetCount 시 자동 종료
- OnComplete는 루프 종료 시 항상 발동 (정상 종료/타겟 없음 양쪽)
- _hitIds 배열 크기 8 = 하드 리밋 (Param으로 설정 가능한 최대 바운스도 8 이하로 제한)
