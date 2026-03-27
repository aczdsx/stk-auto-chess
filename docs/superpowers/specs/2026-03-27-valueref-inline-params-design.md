# ValueRef 인라인 파라미터 — .Param() 제거, 액션이 값 소스를 직접 참조

## 한 줄 요약

`.Param()` + ParamIndex 매직넘버 체계를 제거하고, 각 액션 팩토리가 `ValueRef`(Spec/Fixed/Power)로 값의 출처를 직접 선언. 빌더가 Build 시점에 ParamSlots 자동 생성.

## 현재 문제

```csharp
// 선언과 사용이 분리, 인덱스로 연결 — 매직넘버
.Param(2, P.Int, 200f)       // [0] ← 이게 뭔지 주석 없으면 모름
.Param(1, P.Frames, 3f)      // [1]
.Param(4, P.Int, 30f)        // [2]
.Do(Damage(paramIndex: 0))              // "0번" ← 매직넘버
.Do(Buff(DodgeChance, 2, 1))            // "2번 값, 1번 지속" ← 매직넘버
```

- 중간에 .Param 하나 추가하면 모든 인덱스 밀림
- 고정값(specData 없는 스킬) 지원 불가
- 선언(.Param)과 사용(.Do) 사이 거리가 멀어 가독성 나쁨

## 변경 후

```csharp
Skill(217663506, E.Channeling, T.LowestHPEnemy)
    .On(Evt.Cast)
        .Do(ApplyStatusEffect(TargetImpossible, F.Self,
            duration: Spec(1, P.Frames, 3f)))
    .On(Evt.Execute1)
        .Do(Teleport())
        .Do(Damage(power: Spec(2, 200f)))
        .Do(Retarget(F.LowestHpAllies, excludeHit: true))
    .On(Evt.Complete)
        .Do(Buff(DodgeChance,
            value: Spec(4, 30f),
            duration: Spec(3, P.Frames, 3f)))
    .Register();

// 지휘자 스킬 (specData 없음):
Skill(commanderSkillId, E.Instant, T.Self)
    .On(Evt.Cast)
        .Do(Buff(DodgeChance,
            value: Fixed(50),
            duration: FixedFrames(5f)))
    .Register();
```

## 핵심 구조

### ValueRef

```csharp
/// <summary>값 참조 — specData 또는 고정값. 빌드 시점에만 사용.</summary>
public readonly struct ValueRef
{
    public readonly byte SpecIndex;        // specData 인덱스 (255 = 고정값)
    public readonly ParamValueType Type;   // Int / Frames
    public readonly float Value;           // fallback 또는 고정값
    public readonly bool IsDefault;        // true = PowerPercent 사용 (ParamIndex -1)

    private ValueRef(byte specIndex, ParamValueType type, float value, bool isDefault = false)
    {
        SpecIndex = specIndex; Type = type; Value = value; IsDefault = isDefault;
    }

    public static ValueRef Spec(byte specIndex, float fallback)
        => new ValueRef(specIndex, ParamValueType.Int, fallback);
    public static ValueRef Spec(byte specIndex, ParamValueType type, float fallback)
        => new ValueRef(specIndex, type, fallback);
    public static ValueRef Fixed(float value)
        => new ValueRef(255, ParamValueType.Int, value);
    public static ValueRef FixedFrames(float seconds)
        => new ValueRef(255, ParamValueType.Frames, seconds);

    /// <summary>PowerPercent 참조 (기본 데미지 배율)</summary>
    public static readonly ValueRef Power = new ValueRef(0, ParamValueType.Int, 0, isDefault: true);
}
```

### ActionTemplate

```csharp
/// <summary>빌드 전 액션 + 값 참조. Do()에서 ParamIndex로 변환됨.</summary>
public readonly struct ActionTemplate
{
    public readonly SkillAction Action;
    public readonly ValueRef PrimaryValue;
    public readonly ValueRef SecondaryValue;
    public readonly ValueRef DecayValue;   // DamageWithDecay용

    public ActionTemplate(SkillAction action,
        ValueRef primary = default, ValueRef secondary = default, ValueRef decay = default)
    {
        Action = action;
        PrimaryValue = primary;
        SecondaryValue = secondary;
        DecayValue = decay;
    }
}
```

### Do() — 슬롯 자동 할당

```csharp
public SkillRecipeBuilder Do(ActionTemplate template)
{
    var action = template.Action;
    action.Trigger = _currentTrigger;
    if (_currentTrigger == SkillTriggerType.AtHitFrame)
        action.HitFrameIndex = _currentHitFrameIndex;

    action.ParamIndex = AllocateSlot(template.PrimaryValue);
    action.SecondaryParamIndex = AllocateSlot(template.SecondaryValue);
    if (template.DecayValue.SpecIndex != 0 || template.DecayValue.IsDefault)
        action.DecayParamIndex = AllocateSlot(template.DecayValue);

    _actions.Add(action);
    return this;
}

private sbyte AllocateSlot(ValueRef vref)
{
    if (vref.IsDefault) return -1;  // PowerPercent
    if (vref.SpecIndex == 0 && vref.Value == 0 && vref.Type == 0) return -1; // 미지정

    // 같은 Spec 참조가 이미 있으면 재사용
    for (int i = 0; i < _params.Count; i++)
    {
        var existing = _params[i];
        if (existing.SpecIndex == vref.SpecIndex && existing.ValueType == vref.Type)
            return (sbyte)i;
    }

    // 새 슬롯 할당
    _params.Add(new ParamSlot(vref.SpecIndex, vref.Type, vref.Value));
    return (sbyte)(_params.Count - 1);
}
```

### 액션 팩토리 — ActionTemplate 반환

```csharp
// Before: SkillAction 반환, paramIndex 직접 지정
public static SkillAction Damage(sbyte paramIndex = -1, ...)
    => new SkillAction { ParamIndex = paramIndex, ... };

// After: ActionTemplate 반환, ValueRef로 값 참조
public static ActionTemplate Damage(ValueRef power = default, ...)
    => new ActionTemplate(
        new SkillAction { Effect = SkillEffectType.Damage, ... },
        primary: power.IsDefault && power.SpecIndex == 0 ? ValueRef.Power : power);

public static ActionTemplate Buff(StatModType stat, ValueRef value, ValueRef duration, ...)
    => new ActionTemplate(
        new SkillAction { Effect = SkillEffectType.ApplyBuff, BuffStat = stat, ... },
        primary: value, secondary: duration);
```

### 값이 필요 없는 팩토리 — SkillAction 그대로

```csharp
// 값 참조가 없는 액션은 기존 SkillAction 반환
public static SkillAction Teleport(byte distance = 0) => ...;
public static SkillAction Retarget(SkillTargetFilter filter, ...) => ...;
public static SkillAction Vfx(sbyte vfxIndex, SkillVfxPlacement at) => ...;
public static SkillAction AddMarker(SkillMarkerType marker) => ...;
public static SkillAction TileEffect(...) => ...;
public static SkillAction RemoveDebuffs(...) => ...;
```

Do() 오버로드:
```csharp
public SkillRecipeBuilder Do(ActionTemplate template) { ... } // ValueRef 해석
public SkillRecipeBuilder Do(SkillAction action) { ... }       // 기존 동작 (ParamIndex 없는 액션)
```

## InitializeFromSpec 변경

ParamSlot.SpecIndex == 255면 고정값 사용:

```csharp
if (slot.SpecIndex == 255)
{
    _paramValues[i] = slot.ValueType == ParamValueType.Frames
        ? (int)(slot.Fallback * tickRate + 0.5f)
        : Mathf.RoundToInt(slot.Fallback);
}
else
{
    // 기존 specData 조회 로직
}
```

## 변경 파일 목록

| 파일 | 변경 |
|------|------|
| `SkillFactory.cs` | ValueRef/ActionTemplate struct 추가, Do() 오버로드, AllocateSlot, 액션 팩토리 시그니처 변경 |
| `SkillFactory.Character.cs` | 모든 Recipe를 ValueRef 방식으로 재작성, .Param() 제거 |
| `SkillFactory.Monster.cs` | Preset + 개별 Recipe를 ValueRef 방식으로 재작성 |
| `SimSkillGeneric.cs` | InitializeFromSpec에 SpecIndex==255 분기 추가 (1줄) |
| `SkillRecipe.cs` | 변경 없음 (ParamSlot/SkillAction 구조 유지) |
| `ActionExecutor.cs` | 변경 없음 (GetParamValue 그대로) |

## 런타임 영향

**없음.** _paramValues[], ParamIndex, GetParamValue, ActionExecutor 전부 변경 없음.
빌더 API만 변경. GC-free 유지.

## 마이그레이션

모든 .Param() + paramIndex 매직넘버를 ValueRef 인라인으로 변환.
기계적 치환 — 로직 변경 아님.
