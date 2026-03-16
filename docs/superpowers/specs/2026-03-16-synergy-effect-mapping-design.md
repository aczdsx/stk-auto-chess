# Synergy Effect Mapping — 원소 구현 + Asterism 프레임워크

## 목표

InGame_New `SynergySystem`의 TODO를 해결: `AutoChessSpecAdapter.BuildSynergySpecs()`에서
`Effects = Array.Empty<SynergyEffect>()` → 실제 효과 매핑.

- **1계층**: 원소 시너지 — 스펙 데이터 → `SynergyEffect[]` 스탯 매핑 (이번에 완성)
- **2계층**: Asterism 시너지 — `ISynergyBehavior` 인터페이스 + 팩토리 (프레임워크만)

## 아키텍처

```
ISpecSynergyData (스펙 테이블)
    ↓ AutoChessSpecAdapter.BuildSynergySpecs()
SynergySpec { Tiers[] → SynergyEffect[] }        ← 1계층: 원소 (스탯 매핑)
SynergySpec { Tiers[], BehaviorId }               ← 2계층: asterism (행동 연결)
    ↓ SynergySystem.ApplyEffects()
CombatUnit 스탯 직접 수정                           ← 1계층
    ↓ SynergySystem.ApplyBehaviors()  [신규]
ISynergyBehavior 인스턴스 생성 → CombatMatchState에 등록  ← 2계층
```

## 1계층: 원소 시너지 스탯 매핑

### 변경 파일
- `AutoChessSpecAdapter.cs` — `BuildSynergySpecs()` 내 Effects 매핑 로직

### SynergyType → SynergyEffectType 매핑

| SynergyType | effect_1 매핑 | effect_2 매핑 |
|---|---|---|
| FIRE(2) | BonusAttackPercent | — |
| WIND(3) | BonusAttackSpeedPercent | DodgeChance |
| LIGHTNING(4) | BonusCritChance | BonusCritMultiplier |
| EARTH(5) | BonusAdReduce + BonusApReduce (동일값) | — |
| WATER(6) | BonusHPPercent | BonusDef |

### SynergyCoverType → SynergyTarget 매핑

| SynergyCoverType | SynergyTarget |
|---|---|
| SQUAD_ALL(1) | AllAllies |
| SYNERGY_ELEMENTAL(4) | TraitUnits |
| SYNERGY_STELLA(5) | TraitUnits |
| 그 외 | AllAllies (fallback) |

### 핵심 메서드

```csharp
// AutoChessSpecAdapter.cs
private static SynergyEffect[] BuildEffects(SynergyType type, ISpecSynergyData data)
{
    var target = MapCoverType(data.synergy_cover_type);
    var effects = new List<SynergyEffect>();

    // SynergyType별 스위치: 어떤 SynergyEffectType에 어떤 값을 매핑할지 결정
    // effect_stat_value_1/2/3 → Value 또는 ValuePercent

    return effects.ToArray();
}

private static SynergyTarget MapCoverType(SynergyCoverType cover) { ... }
```

**설계 포인트**: SynergyType별 스위치는 한 곳(`BuildEffects`)에만 존재.
스펙 테이블 구조가 바뀌면 이 메서드만 수정.

## 2계층: Asterism 프레임워크

### 새 파일

| 파일 | 역할 |
|---|---|
| `Simulation/Synergy/ISynergyBehavior.cs` | 인터페이스 정의 |
| `Simulation/Synergy/SynergyBehaviorFactory.cs` | SynergyType → 행동 클래스 생성 |

### ISynergyBehavior 인터페이스

```csharp
public interface ISynergyBehavior
{
    void OnCombatStart(CombatMatchState state, byte teamIndex, int traitId, byte tier);
    void OnTick(CombatMatchState state, byte teamIndex);
    void OnUnitAttack(CombatMatchState state, ref CombatUnit attacker, ref CombatUnit target);
    void OnUnitDamaged(CombatMatchState state, ref CombatUnit victim, ref CombatUnit attacker, ref int damage);
    void OnUnitKill(CombatMatchState state, ref CombatUnit killer, ref CombatUnit victim);
}
```

- 모든 메서드는 빈 default 구현 (필요한 것만 override)
- 기존 `CombatTraitBase` 콜백과 유사하지만, 시너지 전용으로 분리

### SynergyBehaviorFactory

```csharp
public static class SynergyBehaviorFactory
{
    public static ISynergyBehavior Create(SynergyType type, byte tier)
    {
        return type switch
        {
            // asterism 구현 시 여기에 추가
            // SynergyType.NOBLESSE => new SynergyBehaviorNoblesse(tier),
            // SynergyType.TROUBLESHOOTER => new SynergyBehaviorTroubleShooter(tier),
            _ => null,  // null = 행동 없음 (원소는 스탯만)
        };
    }
}
```

### SynergySpec 확장

```csharp
public struct SynergySpec
{
    public int TraitId;
    public TraitCategory Category;
    public SynergyTier[] Tiers;
    public bool HasBehavior;  // 신규: asterism처럼 행동 클래스가 필요한지

    public bool IsValid => Tiers != null && Tiers.Length > 0;
}
```

### CombatMatchState 확장

```csharp
// 활성 시너지 행동 저장
public ISynergyBehavior[] ActiveSynergyBehaviors; // [MaxBehaviors]
public int ActiveBehaviorCount;
```

### SynergySystem 확장

```csharp
// 기존 ApplyEffects() 이후 호출
public static void ApplyBehaviors(GameWorld world, CombatMatchState state,
    byte playerIndex, byte teamIndex)
{
    // HasBehavior인 시너지의 활성 티어에 대해
    // SynergyBehaviorFactory.Create() → state.ActiveSynergyBehaviors에 등록
    // OnCombatStart() 호출
}
```

콜백 디스패치는 기존 `TraitSystem` 패턴을 따름:
- `CombatAISystem`의 공격/피격 처리 지점에서 `ActiveSynergyBehaviors` 순회

## 변경 범위 요약

| 파일 | 변경 내용 | 위험도 |
|---|---|---|
| `AutoChessSpecAdapter.cs` | BuildEffects() 추가, BuildSynergySpecs() 수정 | 낮음 |
| `Components.cs` | SynergySpec에 HasBehavior 필드 | 낮음 |
| `Components.cs` | CombatMatchState에 ActiveSynergyBehaviors | 낮음 |
| `ISynergyBehavior.cs` | 새 파일 (인터페이스) | 없음 |
| `SynergyBehaviorFactory.cs` | 새 파일 (빈 팩토리) | 없음 |
| `SynergySystem.cs` | ApplyBehaviors() 추가 | 낮음 |
| `GameLoopSystem.cs` | ApplyBehaviors() 호출 추가 | 낮음 |
