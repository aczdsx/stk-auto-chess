# 시간 기반 마나 리젠 시스템 설계

## 배경

현재 InGame_New 시스템의 마나 충전은 타격(+10)/피격(+10) 하드코딩 상수로만 동작한다.
이를 시간 기반 리젠을 메인으로 전환하고, 타격/피격 마나도 스펙에서 조절 가능하게 변경한다.
쿨타임 감소 버프(리젠 속도 증가, MaxMana 감소) 지원도 추가한다.

## 마나 충전 소스 (3가지)

| 소스 | 설명 | 틱 |
|------|------|----|
| 시간 리젠 | 초당 고정량 충전 | 매 초 1회 (frameCount % tickRate == 0) |
| 타격 충전 | 기본 공격/스킬로 적 타격 시 | 타격 발생 시 |
| 피격 충전 | 데미지를 받을 때 | 피격 발생 시 |

세 값 모두 캐릭터 스펙에서 개별 설정 가능. 미설정(0) 시 글로벌 기본값 fallback.

## CombatUnit 필드 추가

```csharp
// 마나 리젠 관련
public int ManaRegenPerSec;    // 초당 시간 리젠량 (기본값: GameConfig에서)
public int ManaGainOnAttack;   // 타격 시 마나 획득량
public int ManaGainOnHit;      // 피격 시 마나 획득량
```

## 쿨타임 감소 버프 (2가지 경로)

### 1. 마나 리젠 속도 증가
- `StatModType.ManaRegenRate` 추가
- 시간 리젠에 % 보너스 적용
- 계산: `baseManaRegen * (100 + regenRateBonus) / 100`
- 정수 연산 유지

### 2. MaxMana 감소
- `StatModType.MaxMana` 추가
- 필요 마나 총량을 줄여서 빨리 채워지게
- MaxMana 변경 시 CurrentMana가 MaxMana를 초과하면 클램프

## StatModType 확장

```csharp
public enum StatModType
{
    Attack,
    Armor,
    MagicResist,
    AttackSpeed,
    ManaRegenRate,  // NEW: 마나 리젠 속도 %
    MaxMana,        // NEW: 최대 마나 증감
}
```

## 글로벌 기본값 (GameConfig)

```csharp
public const int DefaultManaRegenPerSec = 10;
public const int DefaultManaGainOnAttack = 10;
public const int DefaultManaGainOnHit = 10;
```

실제 값은 밸런스에 따라 조정.

## 시간 리젠 틱 로직

ManaSystem을 static 시스템으로 신규 생성:

```csharp
public static class ManaSystem
{
    public static void TickManaRegen(CombatMatchState state)
    {
        // 초당 1회만 실행 (frameCount % tickRate == 0)
        if (state.FrameCount % state.TickRate != 0) return;

        foreach unit in alive units:
            int baseRegen = unit.ManaRegenPerSec;
            int regenBonus = unit.GetStatMod(StatModType.ManaRegenRate); // %
            int finalRegen = baseRegen * (100 + regenBonus) / 100;
            ChargeMana(ref unit, finalRegen);
    }
}
```

GameLoopSystem의 매 프레임 루프에서 호출.

## DamageSystem 변경

기존 하드코딩 상수 제거:
```csharp
// 변경 전
private const int ManaGainOnAttack = 10;
private const int ManaGainOnHit = 10;

// 변경 후: 유닛 필드 참조
DamageSystem.ChargeMana(ref attacker, attacker.ManaGainOnAttack);
DamageSystem.ChargeMana(ref target, target.ManaGainOnHit);
```

## CombatSetupSystem 변경

캐릭터 스펙에서 마나 리젠 값 초기화. 스펙 값이 0이면 GameConfig 기본값 사용:

```csharp
unit.ManaRegenPerSec = spec.ManaRegenPerSec > 0 ? spec.ManaRegenPerSec : GameConfig.DefaultManaRegenPerSec;
unit.ManaGainOnAttack = spec.ManaGainOnAttack > 0 ? spec.ManaGainOnAttack : GameConfig.DefaultManaGainOnAttack;
unit.ManaGainOnHit = spec.ManaGainOnHit > 0 ? spec.ManaGainOnHit : GameConfig.DefaultManaGainOnHit;
```

## SkillBuffHelper 확장

`ModifyStat()`에 새 StatModType 처리 추가:

- `ManaRegenRate`: 리젠 보너스 % 누적 (별도 필드 또는 기존 패턴 따름)
- `MaxMana`: `unit.MaxMana` 직접 증감, CurrentMana 클램프

## View 보간

시뮬레이션은 초당 1회 충전이지만 마나바 UI는 lerp 보간으로 부드럽게 표시:
- View가 이전 마나값 → 현재 마나값을 프레임마다 보간
- Simulation/View 분리 구조 유지

## 설계 원칙

- 정수 연산만 사용 (float 금지)
- 시뮬레이션에 Unity 의존성 없음
- static 시스템 메서드 패턴: `ManaSystem.TickManaRegen(state)`
- `DeterministicRNG` 사용 (해당 시 )
- Quantum 전환 대비: 단순한 struct 필드 + static 메서드 유지

## 변경 파일 목록

| 파일 | 변경 |
|------|------|
| `Simulation/Data/Components.cs` | CombatUnit에 마나 리젠 필드 3개 추가 |
| `Simulation/Data/Enums.cs` | StatModType에 ManaRegenRate, MaxMana 추가 |
| `Simulation/Data/GameConfig.cs` | 글로벌 기본값 상수 추가 |
| `Simulation/Combat/ManaSystem.cs` | 신규 — 시간 리젠 틱 로직 |
| `Simulation/Combat/DamageSystem.cs` | 하드코딩 상수 → 유닛 필드 참조 |
| `Simulation/Combat/CombatSetupSystem.cs` | 스펙에서 마나 리젠 값 초기화 |
| `Simulation/Combat/SkillBuffHelper.cs` | ManaRegenRate, MaxMana 스탯 변경 처리 |
| `Simulation/Core/GameLoopSystem.cs` | ManaSystem.TickManaRegen 호출 추가 |
| View 레이어 (마나바) | lerp 보간 처리 |