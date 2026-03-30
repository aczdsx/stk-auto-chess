# TeleportReturn + 대쉬 패턴 지원

> **NOTE (2026-03-30):** 빅마우스 대쉬는 이 문서의 TeleportReturn 기반이 아닌 **DashSystem 3페이즈** 방식으로 재구현됨.
> 상세: `docs/superpowers/plans/2026-03-30-bigmouse-dash-phases.md`

## 한 줄 요약

`TeleportReturn` EffectType + `AtCasterToSaved` VfxPlacement 추가로 "돌진 → 공격 → 원위치 복귀" 패턴을 Recipe로 표현.

## 문제

빅마우스(240107002)의 대쉬 스킬:
- Execute0: 타겟으로 **대쉬 이동** + 경로 관통 데미지 + 기절
- Execute1: 타겟 너머 **오버슈트** + 원위치 방향 포탈 VFX
- Execute2: **원위치 복귀**

현재 Recipe에서 시전자가 이동하지 않으므로:
- VFX가 전부 시전자 위치에 겹침
- 대쉬 연출 없음
- "현재 위치 → 원래 위치" 방향 VFX 불가

## 해결

### 1. 위치 저장 (SimSkillGeneric)

```csharp
private byte _savedGridCol;
private byte _savedGridRow;
```

Execute()에서 시전 시작 위치 저장:
```csharp
_savedGridCol = caster.GridCol;
_savedGridRow = caster.GridRow;
```

비용: 2바이트. 모든 스킬에서 실행되지만 사용하지 않으면 무영향.

### 2. SkillExecuteContext 확장

```csharp
public byte SavedGridCol;
public byte SavedGridRow;
```

MakeContext에서 전달:
```csharp
SavedGridCol = _savedGridCol,
SavedGridRow = _savedGridRow,
```

### 3. TeleportReturn EffectType

```csharp
SkillEffectType.TeleportReturn  // 저장된 위치로 복귀
```

ActionExecutor:
```csharp
private static void ExecuteTeleportReturn(ref SkillAction action, SkillExecuteContext ctx)
{
    ref var caster = ref ctx.GetCaster();
    TryTeleport(ctx.State, ref caster, ctx.SavedGridCol, ctx.SavedGridRow);
}
```

기존 TryTeleport 재사용. ClearGrid → SetGrid → PushUnitMoved.

### 4. AtCasterToSaved VfxPlacement

```csharp
SkillVfxPlacement.AtCasterToSaved  // 현재 위치에 VFX, 방향은 저장 위치를 향함
```

SpawnVfx:
```csharp
case SkillVfxPlacement.AtCasterToSaved:
{
    ref var caster = ref ctx.GetCaster();
    sbyte dirCol = (sbyte)(ctx.SavedGridCol - caster.GridCol);
    sbyte dirRow = (sbyte)(ctx.SavedGridRow - caster.GridRow);
    if (dirCol != 0) dirCol = (sbyte)(dirCol > 0 ? 1 : -1);
    if (dirRow != 0) dirRow = (sbyte)(dirRow > 0 ? 1 : -1);
    eq.PushSkillPhaseVfx(ctx.CasterCombatId, ctx.SkillSpecId, (byte)action.VfxIndex,
        dirCol: dirCol, dirRow: dirRow);
    break;
}
```

방향: 현재 위치 → 저장 위치 (= 돌아갈 방향).

### 5. 빌더 팩토리

```csharp
public static SkillAction TeleportReturn()
    => new SkillAction { Effect = SkillEffectType.TeleportReturn };
```

## 빅마우스 Recipe

```csharp
Skill(240107002, E.Channeling, T.NearestEnemy)
    .On(Evt.Execute1)  // 대쉬: 경로 데미지 + 기절 + 타겟으로 이동
        .Do(Vfx(0, V.AtTargetWithDir))             // 도착 포탈 (타겟 위치, 시전자→타겟 방향)
        .Do(Damage(power: Spec(1, 200f),
            filter: F.EnemiesInArea, area: S.Line)) // 경로 관통 데미지 (이동 전에 계산)
        .Do(CC(CrowdControlType.Stun,
            duration: Spec(2, P.Frames, 3f)))       // 기절
        .Do(Teleport())                             // 타겟 인접으로 이동
    .On(Evt.Execute2)  // 오버슈트 + 복귀 포탈
        .Do(Vfx(0, V.AtCasterToSaved))             // 복귀 포탈 (현재 위치, 원위치 방향)
    .On(Evt.Execute3)  // 원위치 복귀
        .Do(TeleportReturn())                       // 저장 위치로 복귀
    .Register();
```

**Execute1 액션 순서 중요:**
1. VFX(포탈) — 이동 전 타겟 위치에 스폰
2. Damage(Line) — 이동 전 시전자 위치에서 직선 데미지
3. CC(Stun) — 타겟에 기절
4. Teleport — 타겟 인접으로 이동 (이후 VFX 위치가 바뀜)

## 재사용

TeleportReturn은 빅마우스 외에도:
- TeleportStrike 몬스터 (1202091, 240407302, 250108002/3) — 현재 "이동 후 범위 공격"으로 등록
- 미래 대쉬형 스킬 / 지휘자 스킬

TeleportStrike 몬스터도 같은 패턴으로 변경 가능:
```csharp
// 현재: 이동 없이 범위 공격
.Do(Damage(Circle, 1))
.Do(AreaCC(Stun, Circle, 1))

// 변경: 텔레포트 + 범위 공격 + 복귀
.On(Evt.Execute1)
    .Do(Teleport())
    .Do(Damage(Circle, 1))
    .Do(AreaCC(Stun, Circle, 1))
.On(Evt.Execute2)
    .Do(TeleportReturn())
```

## 변경 파일

| 파일 | 변경 |
|------|------|
| Enums.cs | SkillEffectType.TeleportReturn, SkillVfxPlacement.AtCasterToSaved |
| SimSkillGeneric.cs | _savedGridCol/Row 필드, Execute()에서 저장, MakeContext에서 전달, Reset 초기화 |
| ActionExecutor.cs | SkillExecuteContext에 SavedGridCol/Row, ExecuteTeleportReturn, SpawnVfx AtCasterToSaved case, dispatch switch case |
| SkillFactory.cs | TeleportReturn() 팩토리 |
| SkillFactory.Monster.cs | 빅마우스 Recipe 재작성 |

## 런타임 영향

- SimSkillGeneric: Execute()에서 2바이트 저장 추가 (모든 스킬, 무시 가능)
- SkillExecuteContext: 2바이트 필드 추가
- 기존 스킬 동작 변경 없음
