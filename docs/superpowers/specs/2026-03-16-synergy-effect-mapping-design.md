# Synergy Effect Mapping — 원소 구현 + Asterism 프레임워크

> **최종 업데이트**: 2026-03-17 — SynergyBehaviorBase 제거, CombatTraitBase/TraitSystem 통합 완료

## 목표

InGame_New `SynergySystem`의 TODO를 해결: `AutoChessSpecAdapter.BuildSynergySpecs()`에서
`Effects = Array.Empty<SynergyEffect>()` → 실제 효과 매핑.

- **1계층**: 속성(원소) 시너지 — 스펙 데이터 → `SynergyEffect[]` 스탯 매핑 ✅
- **2계층**: 성군(Asterism) 시너지 — `CombatTraitBase` 서브클래스로 유닛에 부착 ✅
- **3계층**: 준비 페이즈 행동 `SynergyPrepBehaviorBase` + prep→combat 전달 ✅

## 아키텍처

```
ISpecSynergyData (스펙 테이블)
    ↓ AutoChessSpecAdapter.BuildSynergySpecs()
SynergySpec { Tiers[] → SynergyEffect[], HasBehavior }

    ↓ [준비 페이즈] SynergySystem.Recalculate() + SyncPrepBehaviors()
PlayerSynergy (TraitCounts, TraitTiers)
SynergyPrepBehaviorBase (prep 상호작용 관리)              ← 3계층

    ↓ [전투 전환] SynergySystem.ApplyEffects()
CombatUnit 스탯 직접 수정                                 ← 1계층

    ↓ SynergySystem.ApplyBehaviors()
SynergyTraitFactory → CombatTraitBase 서브클래스 생성      ← 2계층
    → TraitSystem.AddTrait()으로 대상 유닛에 부착
    → 기존 TraitSystem 디스패치로 콜백 자동 처리
```

---

## 1계층: 원소 시너지 스탯 매핑

### 변경 파일
- `Adapter/AutoChessSpecAdapter.cs` — `BuildSynergySpecs()` + `BuildEffects()` + `MapCoverType()`

### SynergyType → SynergyEffectType 매핑

| SynergyType | effect_1 매핑 | effect_2 매핑 |
|---|---|---|
| FIRE(2) | BonusAttackPercent | — |
| WIND(3) | BonusAttackSpeedPercent | DodgeChance |
| LIGHTNING(4) | BonusCritChance | BonusCritMultiplier |
| EARTH(5) | BonusAdReduce + BonusApReduce (동일값) | — |
| WATER(6) | BonusHPPercent | BonusDef |

### SynergyCoverType → SynergyTarget 매핑

| SynergyCoverType | SynergyTarget | 설명 |
|---|---|---|
| SQUAD_ALL(1) | AllAllies | 스쿼드 전체에 적용 |
| SQUAD_STELLA(2) | TraitUnits | 해당 성군 특성 유닛에만 적용 |
| KNIGHT_ALL(3) | AllAllies | 스쿼드 전체에 적용 |
| SYNERGY_ELEMENTAL(4) | — | 레거시 시너지 시스템용 (InGame_New에서 미사용) |
| SYNERGY_STELLA(5) | — | 레거시 시너지 시스템용 (InGame_New에서 미사용) |
| 그 외 | AllAllies | fallback |

### ApplyStatEffect 퍼센트 보정 기준

- **퍼센트 보너스는 Base 스탯 기준** (중복 적용 방지):
  - `BonusAttackPercent` → `unit.BaseAttack * percent / 100`
  - `BonusHPPercent` → `unit.BaseMaxHP * percent / 100`
  - `BonusAttackSpeedPercent` → `unit.BaseAttackSpeed * percent / 100`
  - `BonusDefPercent` → `unit.BaseDef * percent / 100`
  - `BonusAdReducePercent` → `unit.BaseAdReduce * percent / 100`
  - `BonusApReducePercent` → `unit.BaseApReduce * percent / 100`

### SynergyEffectType (전체 목록)

```csharp
// 고정값 보너스
BonusDef, BonusAdReduce, BonusApReduce, BonusAttack, BonusHP,
BonusAttackSpeed, BonusMana, BonusCritChance, BonusCritMultiplier,
// 퍼센트 보너스
BonusAttackPercent, BonusHPPercent, BonusAttackSpeedPercent,
BonusDefPercent, BonusAdReducePercent, BonusApReducePercent,
// 특수 효과
StartingMana, SpellDamagePercent, LifeSteal, DodgeChance,
BacklineJump, ShieldOnCombatStart,
// 디버프 (적군 대상)
ReduceDef, ReduceAdReduce, ReduceApReduce,
```

---

## 2계층: 성군(Asterism) 전투 행동 — CombatTraitBase 통합

> **변경 이력**: SynergyBehaviorBase(팀 단위 디스패치) 삭제 → CombatTraitBase(유닛 단위 디스패치)로 통합.
> Asterism 시너지도 결국 특정 유닛에 붙는 버프이므로, 기존 TraitSystem 디스패치를 그대로 활용.

### 파일

| 파일 | 역할 |
|---|---|
| `Synergy/SynergyTraitFactory.cs` | SynergyType + Tier → CombatTraitBase 서브클래스 생성 |
| `Combat/Traits/CombatTraitBase.cs` | 시너지 식별용 필드 4개 추가 |

### CombatTraitBase 시너지 필드

```csharp
public int SynergyTraitId = -1;     // 시너지에서 생성된 trait인지 식별 (-1이면 아님)
public int PrepTargetEntityId = -1;  // prep에서 전달된 타겟
public int PrepParam0;
public int PrepParam1;
```

기존 유닛별 trait(아이템/스킬 등)은 `SynergyTraitId = -1`로 무시됨.

### SynergyTraitFactory

```csharp
public static class SynergyTraitFactory
{
    public static CombatTraitBase Create(SynergyType type, byte tier)
    {
        // 구현체 추가 시 여기만 수정
        return null;
    }

    public static bool NeedsBehavior(SynergyType type) => type switch
    {
        SynergyType.NORMAL or ... SynergyType.WATER => false,  // 속성: 스탯만
        _ => true,  // 성군: 행동 필요
    };
}
```

### 콜백 디스패치

별도 `SynergySystem.Invoke*` 메서드 **불필요**. `TraitSystem`이 이미 제공하는 콜백으로 자동 디스패치:

```
TraitSystem.InvokeCombatStart()    — 전투 시작
TraitSystem.InvokeOnTick()         — 매 틱
TraitSystem.InvokeModifyOutgoingDamage() / InvokeModifyIncomingDamage()
TraitSystem.InvokeOnDamageTaken()  — 피격 후
TraitSystem.InvokeOnKill()         — 처치 시
TraitSystem.InvokeOnDeath()        — 사망 시
TraitSystem.InvokeOnPreAttack() / InvokeOnPostAttack()
TraitSystem.InvokeOnCritical()
```

### ApplyBehaviors 흐름

```csharp
SynergySystem.ApplyBehaviors(world, state, playerIndex, teamIndex)
  → SynergyTraitFactory.Create(type, tier) → CombatTraitBase 서브클래스
  → prep 데이터 복사 (SynergyTraitId, PrepTargetEntityId, PrepParam0/1)
  → TraitSystem.AddTrait(state, unitIndex, trait) — 대상 유닛에 부착
```

대상 유닛 결정: `prepTargetEntityId >= 0`이면 해당 유닛에만, 아니면 팀 전체에 부착.

---

## 3계층: 준비 페이즈 행동 (NEW — 스펙 원본에 없던 추가)

### 배경

Asterism 시너지(Supernova/Troubleshooter 등)는 준비 페이즈에서 맵 오브젝트 생성, 드래그 상호작용 등이 필요.
기존 add/remove 방식은 페이즈 이펙트 꼬임 버그를 유발했으므로 **UnitData 스탯은 절대 수정하지 않는** 구조.

### 파일

| 파일 | 역할 |
|---|---|
| `Synergy/SynergyPrepBehaviorBase.cs` | 준비 페이즈 행동 추상 베이스 + 팩토리 |
| `Data/GameWorld.cs` | PrepBehaviors / PrevSynergyTiers 저장소 |
| `Core/CommandProcessor.cs` | OnBoardChanged 헬퍼 + SetSynergyPrepTarget 처리 |
| `Core/GameLoopSystem.cs` | OnEnterPreparation에서 prep 정리/재동기화 |
| `Data/Enums.cs` | CommandType.SetSynergyPrepTarget |
| `Data/Commands.cs` | GameCommand.SetSynergyPrepTarget 팩토리 |

### SynergyPrepBehaviorBase

```csharp
public abstract class SynergyPrepBehaviorBase
{
    public int TraitId;
    public byte Tier;
    public byte PlayerIndex;

    public virtual void OnActivate(GameWorld world) { }
    public virtual void OnDeactivate(GameWorld world) { }
    public virtual void OnTierChanged(GameWorld world, byte oldTier, byte newTier) { }
    public virtual void OnBoardChanged(GameWorld world) { }
    public virtual void HandleCommand(GameWorld world, in GameCommand cmd) { }

    public int PrepTargetEntityId = -1;
    public int PrepParam0;
    public int PrepParam1;
}
```

### GameWorld 저장소

```csharp
public const int MaxPrepBehaviors = 8;
public SynergyPrepBehaviorBase[][] PrepBehaviors;  // [MaxPlayers][MaxPrepBehaviors]
public int[] PrepBehaviorCounts;                    // [MaxPlayers]
public byte[][] PrevSynergyTiers;                   // [MaxPlayers][MaxTraits] — diff 계산용
```

### 핵심 흐름

```
보드 변경 → CommandProcessor.OnBoardChanged()
  ① SynergySystem.Recalculate()
  ② SynergySystem.SyncPrepBehaviors()  ← PrevSynergyTiers diff로 생성/소멸/변경
  ③ EventQueue.PushSynergyUpdated()

라운드 전환 → GameLoopSystem.OnEnterPreparation()
  ① ClearPrepBehaviors()   ← 전부 해제 + prevTiers 초기화
  ② Recalculate()
  ③ SyncPrepBehaviors()    ← 새 라운드용 재생성

전투 전환 → SynergySystem.ApplyBehaviors()
  - SynergyTraitFactory.Create() → CombatTraitBase 서브클래스 생성
  - prep 데이터 복사:
    trait.SynergyTraitId = traitId
    trait.PrepTargetEntityId = prep.PrepTargetEntityId
    trait.PrepParam0 = prep.PrepParam0
    trait.PrepParam1 = prep.PrepParam1
  - TraitSystem.AddTrait(state, unitIndex, trait) — 유닛에 부착

타겟 지정 → GameCommand.SetSynergyPrepTarget(player, traitId, targetEntityId)
  → prep.HandleCommand() 호출
```

---

## 디버그 로그

| 태그 | 색상 | 시점 | 내용 |
|---|---|---|---|
| `[Synergy]` | 초록 | Recalculate | 시너지 활성: 타입, 티어, 유닛 수, 효과 목록 |
| `[Synergy]` | 시안 | ApplyEffects | 스탯 적용: unit별 before → after |
| `[SynergyPrep]` | 노랑 | SyncPrepBehaviors | prep 활성화/비활성화/티어변경 |

모든 로그는 `[System.Diagnostics.Conditional("UNITY_EDITOR")]` — 빌드에 포함 안 됨.

---

## 변경 범위 요약

| 파일 | 변경 내용 | 상태 |
|---|---|---|
| `AutoChessSpecAdapter.cs` | BuildEffects() + MapCoverType() + HasBehavior → SynergyTraitFactory | ✅ |
| `Components.cs` | SynergySpec.HasBehavior (SynergyBehaviors 배열 제거됨) | ✅ |
| `CombatTraitBase.cs` | SynergyTraitId/PrepTargetEntityId/PrepParam0/1 필드 추가 | ✅ |
| `SynergyTraitFactory.cs` | SynergyType → CombatTraitBase 팩토리 (SynergyBehaviorFactory 대체) | ✅ |
| `SynergyPrepBehaviorBase.cs` | 준비 페이즈 행동 베이스 + 팩토리 | ✅ |
| `SynergySystem.cs` | ApplyBehaviors → TraitSystem.AddTrait 전환, Invoke* 제거 | ✅ |
| `CommandProcessor.cs` | OnBoardChanged 헬퍼 통합 + SetSynergyPrepTarget | ✅ |
| `GameLoopSystem.cs` | ApplyBehaviors 호출 + OnEnterPreparation prep 동기화 | ✅ |
| `GameWorld.cs` | PrepBehaviors / PrevSynergyTiers 저장소 | ✅ |
| `Enums.cs` | TraitCategory 주석 갱신 + CommandType + SynergyEffectType | ✅ |
| `Commands.cs` | GameCommand.SetSynergyPrepTarget 팩토리 | ✅ |
| ~~`SynergyBehaviorBase.cs`~~ | **삭제** — CombatTraitBase로 통합 | ✅ |
| ~~`SynergyBehaviorFactory.cs`~~ | **삭제** — SynergyTraitFactory로 대체 | ✅ |
