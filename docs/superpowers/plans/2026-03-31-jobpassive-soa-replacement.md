# 직업 패시브 SOA 대체 구현 계획

> **For agentic workers:** REQUIRED: Use superpowers:subagent-driven-development (if subagents available) or superpowers:executing-plans to implement this plan. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** CombatTraitBase virtual 디스패치를 직업별 SOA 구조체 + 인라인 로직으로 대체하여 Quantum ECS 포팅에 유리한 구조로 전환한다.

**Architecture:** 각 직업 패시브를 독립 구조체(GuardianPassive, GhostPassive 등)로 정의하고 CombatMatchState에 병렬 배열로 저장한다. TraitSystem의 콜백 디스패치를 제거하고, 각 시스템(CombatAISystem, DamageSystem 등)에서 직접 SOA 배열을 읽어 로직을 실행한다.

**Tech Stack:** Unity C#

---

## 콜백 → 직업 매핑 (실제 사용되는 것만)

| 콜백 | 사용하는 직업 |
|------|-------------|
| OnCombatStart | Guardian(쉴드 초기화), Striker(CC면역 즉시부여) |
| OnTick | Guardian(쿨타임→쉴드충전), Striker(쿨타임→CC면역) |
| OnPreAttack | Ghost(확정크리 설정), Sharpshooter(관통 설정) |
| OnPostAttack | Ghost(크리 복원), Sharpshooter(관통 복원), Esper(폭발) |
| ModifyIncomingDamage | Guardian(일반공격 차단) |
| OnKill | SkillKillMana(마나 충전) |
| FindTrait\<Oracle\> | Oracle(힐러 판별 + 힐량 계산) |
| ModifyOutgoingDamage | *사용 없음* |
| OnDamageTaken | *사용 없음* |
| OnDeath | *사용 없음* |
| OnCritical | *사용 없음* |

> 사용되지 않는 콜백(ModifyOutgoingDamage, OnDamageTaken, OnDeath, OnCritical)의 호출부는 단순 제거한다.

---

## 파일 구조

### 새로 생성
| 파일 | 역할 |
|------|------|
| `Simulation/Combat/JobPassive/JobPassiveData.cs` | 6개 직업 패시브 구조체 + SkillKillManaData 정의 |
| `Simulation/Combat/JobPassive/JobPassiveLogic.cs` | 각 직업 패시브의 static 인라인 로직 (틱, 데미지 보정 등) |

### 수정
| 파일 | 변경 내용 |
|------|----------|
| `Simulation/Data/Components.cs` | CombatMatchState에 SOA 배열 추가, Traits/TraitCounts/_traitCombatStartDone 제거 |
| `Simulation/Combat/JobPassive/JobPassiveSystem.cs` | Traits 폴더에서 JobPassive 폴더로 이동, SOA 배열 초기화로 재작성 |
| `Simulation/Combat/CombatAISystem.cs` | TraitSystem.InvokeCombatStart/OnTick → JobPassiveLogic 호출 |
| `Simulation/Combat/Systems/Damage/DamageSystem.cs` | TraitSystem.Invoke* → JobPassiveLogic 인라인 |
| `Simulation/Combat/Systems/Damage/DamageSystem.BasicAttack.cs` | FindTrait\<Oracle\> → SOA 배열 직접 참조, PreAttack/PostAttack 인라인 |
| `Simulation/Combat/ProjectileSystem.cs` | TraitSystem.InvokeOnPostAttack → JobPassiveLogic 인라인 |
| `Simulation/Combat/SkillSystem.cs` | SkillKillManaResetTrait → SOA 배열 초기화 |
| `Simulation/Core/GameLoopSystem.cs` | ApplyBehaviors 호출 제거 (현재 null 반환이므로 무의미) |
| `Simulation/Synergy/SynergySystem.cs` | ApplyBehaviors 메서드 삭제 |
| `Simulation/Synergy/SynergyFactory.cs` | CreateTrait 메서드 삭제 |

### 삭제
| 파일 | 이유 |
|------|------|
| `Traits/CombatTraitBase.cs` | SOA로 대체됨 |
| `Traits/TraitSystem.cs` | 디스패처 불필요 |
| `Traits/SkillKillManaResetTrait.cs` | SOA로 대체됨 |
| `Traits/JobPassive/GuardianEndureTrait.cs` | SOA로 대체됨 |
| `Traits/JobPassive/GhostCritStackTrait.cs` | SOA로 대체됨 |
| `Traits/JobPassive/StrikerCCImmuneTrait.cs` | SOA로 대체됨 |
| `Traits/JobPassive/SharpshooterPierceTrait.cs` | SOA로 대체됨 |
| `Traits/JobPassive/EsperExplosionTrait.cs` | SOA로 대체됨 |
| `Traits/JobPassive/OracleHealerTrait.cs` | SOA로 대체됨 |

---

## Chunk 1: SOA 구조체 정의 및 CombatMatchState 확장

### Task 1: 직업 패시브 SOA 구조체 정의

**Files:**
- Create: `Assets/_Project/Scripts/InGame_New/Simulation/Combat/JobPassive/JobPassiveData.cs`

- [ ] **Step 1: 6개 직업 패시브 구조체 + SkillKillManaData 작성**

```csharp
namespace InGame_New.Simulation.Combat
{
    /// <summary>GUARDIAN: 쿨타임마다 일반공격 N회 무시 베리어</summary>
    public struct GuardianPassive
    {
        public bool Active;          // 이 유닛에 Guardian 패시브가 있는지
        public int CooldownFrames;   // 쉴드 재충전 쿨타임 (프레임)
        public int MaxCharges;       // 최대 충전 횟수
        public int Timer;            // 현재 타이머
        public int ShieldCharges;    // 남은 쉴드 충전 횟수
    }

    /// <summary>GHOST: N타마다 확정 크리티컬</summary>
    public struct GhostPassive
    {
        public bool Active;
        public int MaxStack;         // 확정 크리 필요 스택
        public int Stack;            // 현재 공격 스택
        public bool NextCrit;        // 다음 공격 확정 크리 플래그
        public int SavedCritRate;    // 원본 크리율 백업
        public bool CritOverrideActive; // 크리 오버라이드 활성 여부
    }

    /// <summary>STRIKER: 쿨타임마다 CC 면역 1회 부여</summary>
    public struct StrikerPassive
    {
        public bool Active;
        public int CooldownFrames;   // CC 면역 재충전 쿨타임 (프레임)
        public int Timer;            // 현재 타이머
    }

    /// <summary>SHARPSHOOTER: 확률적 방어 완전 관통</summary>
    public struct SharpshooterPassive
    {
        public bool Active;
        public int ChancePercent;    // 발동 확률 (정수 %)
        public int SavedAtkPierce;   // 원본 AtkPierce 백업
        public int SavedResPierce;   // 원본 ResPierce 백업
        public bool PierceActive;    // 현재 공격에서 관통 활성 여부
    }

    /// <summary>ESPER: 확률적 주변 3x3 폭발</summary>
    public struct EsperPassive
    {
        public bool Active;
        public int ChancePercent;    // 폭발 발동 확률 (정수 %)
        public int DamagePercent;    // 폭발 데미지 비율
    }

    /// <summary>ORACLE: 평타로 아군 힐</summary>
    public struct OraclePassive
    {
        public bool Active;
        public int HealPercent;      // 회복 비율 (정수 %)
    }

    /// <summary>특정 캐릭터 전용: 스킬 킬 시 마나 즉시 충전</summary>
    public struct SkillKillManaData
    {
        public bool Active;
        public int MarkerType;       // SkillMarkerType (int 변환)
    }
}
```

- [ ] **Step 2: 커밋**

```bash
git add Assets/_Project/Scripts/InGame_New/Simulation/Combat/JobPassive/JobPassiveData.cs
git commit -m "feat: 직업 패시브 SOA 구조체 정의 (6종 + SkillKillMana)"
```

---

### Task 2: CombatMatchState에 SOA 배열 추가

**Files:**
- Modify: `Assets/_Project/Scripts/InGame_New/Simulation/Data/Components.cs:806-832`

- [ ] **Step 1: SOA 배열 필드 추가**

기존 `CombatTraitBase[][] Traits` / `int[] TraitCounts` / `bool _traitCombatStartDone` 을 제거하고 다음으로 교체:

```csharp
// --- 직업 패시브 SOA 배열 ---
public GuardianPassive[] GuardianPassives;         // [MaxCombatUnits]
public GhostPassive[] GhostPassives;               // [MaxCombatUnits]
public StrikerPassive[] StrikerPassives;           // [MaxCombatUnits]
public SharpshooterPassive[] SharpshooterPassives; // [MaxCombatUnits]
public EsperPassive[] EsperPassives;               // [MaxCombatUnits]
public OraclePassive[] OraclePassives;             // [MaxCombatUnits]
public SkillKillManaData[] SkillKillManaPassives;  // [MaxCombatUnits]
public bool _jobPassiveCombatStartDone;            // OnCombatStart 1회 실행 플래그
```

- [ ] **Step 2: Create 메서드에서 초기화 코드 교체**

기존 Traits 초기화 코드를 제거하고:

```csharp
GuardianPassives = new GuardianPassive[MaxCombatUnits],
GhostPassives = new GhostPassive[MaxCombatUnits],
StrikerPassives = new StrikerPassive[MaxCombatUnits],
SharpshooterPassives = new SharpshooterPassive[MaxCombatUnits],
EsperPassives = new EsperPassive[MaxCombatUnits],
OraclePassives = new OraclePassive[MaxCombatUnits],
SkillKillManaPassives = new SkillKillManaData[MaxCombatUnits],
```

기존 Traits 초기화 for 루프도 제거 (struct 배열은 default 초기화로 충분).

- [ ] **Step 3: 커밋**

```bash
git add Assets/_Project/Scripts/InGame_New/Simulation/Data/Components.cs
git commit -m "refactor: CombatMatchState에 SOA 배열 추가, Traits/TraitCounts 제거"
```

---

## Chunk 2: JobPassiveLogic 인라인 로직 + JobPassiveSystem 재작성

### Task 3: JobPassiveLogic static 클래스 작성

**Files:**
- Create: `Assets/_Project/Scripts/InGame_New/Simulation/Combat/JobPassive/JobPassiveLogic.cs`

- [ ] **Step 1: Guardian 로직 (OnCombatStart, OnTick, ModifyIncomingDamage)**

```csharp
using InGame_New.Simulation.Data;

namespace InGame_New.Simulation.Combat
{
    public static class JobPassiveLogic
    {
        // ===== GUARDIAN =====

        public static void GuardianOnCombatStart(CombatMatchState state, int unitIndex)
        {
            ref var g = ref state.GuardianPassives[unitIndex];
            if (!g.Active) return;

            g.ShieldCharges = g.MaxCharges;
            g.Timer = 0;
            state.EventQueue?.PushStatusEffectAdded(
                state.Units[unitIndex].CombatId,
                StatusEffectVisualType.BasicAttackShield,
                g.ShieldCharges);
        }

        public static void GuardianOnTick(CombatMatchState state, int unitIndex)
        {
            ref var g = ref state.GuardianPassives[unitIndex];
            if (!g.Active) return;

            g.Timer++;
            if (g.Timer >= g.CooldownFrames)
            {
                g.ShieldCharges = g.MaxCharges;
                g.Timer = 0;
                state.EventQueue?.PushStatusEffectAdded(
                    state.Units[unitIndex].CombatId,
                    StatusEffectVisualType.BasicAttackShield,
                    g.ShieldCharges);
            }
        }

        /// <returns>보정된 데미지. 쉴드로 차단 시 0 반환.</returns>
        public static int GuardianModifyIncomingDamage(
            CombatMatchState state, int targetIndex, int damage, bool isBasicAttack)
        {
            ref var g = ref state.GuardianPassives[targetIndex];
            if (!g.Active || !isBasicAttack || g.ShieldCharges <= 0)
                return damage;

            g.ShieldCharges--;
            if (g.ShieldCharges <= 0)
            {
                state.EventQueue?.PushStatusEffectRemoved(
                    state.Units[targetIndex].CombatId,
                    StatusEffectVisualType.BasicAttackShield);
            }
            else
            {
                state.EventQueue?.PushStatusEffectAdded(
                    state.Units[targetIndex].CombatId,
                    StatusEffectVisualType.BasicAttackShield,
                    g.ShieldCharges);
            }
            return 0;
        }
    }
}
```

- [ ] **Step 2: Striker 로직 (OnCombatStart, OnTick)**

JobPassiveLogic 클래스에 추가:

```csharp
        // ===== STRIKER =====

        public static void StrikerOnCombatStart(CombatMatchState state, int unitIndex)
        {
            ref var s = ref state.StrikerPassives[unitIndex];
            if (!s.Active) return;

            state.Units[unitIndex].CCImmuneCharges = 1;
            s.Timer = 0;
            state.EventQueue?.PushStatusEffectAdded(
                state.Units[unitIndex].CombatId,
                StatusEffectVisualType.JobStriker, -1);
        }

        public static void StrikerOnTick(CombatMatchState state, int unitIndex)
        {
            ref var s = ref state.StrikerPassives[unitIndex];
            if (!s.Active) return;
            if (state.Units[unitIndex].CCImmuneCharges > 0) return;

            s.Timer++;
            if (s.Timer >= s.CooldownFrames)
            {
                state.Units[unitIndex].CCImmuneCharges = 1;
                s.Timer = 0;
                state.EventQueue?.PushStatusEffectAdded(
                    state.Units[unitIndex].CombatId,
                    StatusEffectVisualType.JobStriker, -1);
            }
        }
```

- [ ] **Step 3: Ghost 로직 (OnPreAttack, OnPostAttack)**

```csharp
        // ===== GHOST =====

        public static void GhostOnPreAttack(CombatMatchState state, int attackerIndex)
        {
            ref var g = ref state.GhostPassives[attackerIndex];
            if (!g.Active) return;

            g.CritOverrideActive = false;
            if (g.NextCrit)
            {
                g.SavedCritRate = state.Units[attackerIndex].CritRate;
                state.Units[attackerIndex].CritRate = 100;
                g.CritOverrideActive = true;
                g.NextCrit = false;
            }
        }

        public static void GhostOnPostAttack(CombatMatchState state, int attackerIndex)
        {
            ref var g = ref state.GhostPassives[attackerIndex];
            if (!g.Active) return;

            if (g.CritOverrideActive)
            {
                state.Units[attackerIndex].CritRate = g.SavedCritRate;
                g.CritOverrideActive = false;
            }

            g.Stack++;
            if (g.Stack >= g.MaxStack)
            {
                g.NextCrit = true;
                g.Stack = 0;
            }
        }
```

- [ ] **Step 4: Sharpshooter 로직 (OnPreAttack, OnPostAttack)**

```csharp
        // ===== SHARPSHOOTER =====

        public static void SharpshooterOnPreAttack(CombatMatchState state, int attackerIndex)
        {
            ref var ss = ref state.SharpshooterPassives[attackerIndex];
            if (!ss.Active || ss.ChancePercent <= 0) return;

            ss.PierceActive = false;
            if (state.Rng.Chance(ss.ChancePercent))
            {
                ss.SavedAtkPierce = state.Units[attackerIndex].AtkPierce;
                ss.SavedResPierce = state.Units[attackerIndex].ResPierce;
                state.Units[attackerIndex].AtkPierce = 100;
                state.Units[attackerIndex].ResPierce = 100;
                ss.PierceActive = true;
                state.Units[attackerIndex].ProjectileVfxOverride =
                    ProjectileVfxId.SharpshooterAD;
            }
        }

        public static void SharpshooterOnPostAttack(CombatMatchState state, int attackerIndex)
        {
            ref var ss = ref state.SharpshooterPassives[attackerIndex];
            if (!ss.Active || !ss.PierceActive) return;

            state.Units[attackerIndex].AtkPierce = ss.SavedAtkPierce;
            state.Units[attackerIndex].ResPierce = ss.SavedResPierce;
            ss.PierceActive = false;
        }
```

- [ ] **Step 5: Esper 로직 (OnPostAttack)**

```csharp
        // ===== ESPER =====

        public static void EsperOnPostAttack(
            CombatMatchState state, int attackerIndex, ref CombatUnit target)
        {
            ref var e = ref state.EsperPassives[attackerIndex];
            if (!e.Active || e.ChancePercent <= 0) return;
            if (!target.IsAlive) return;

            if (state.Rng.Chance(e.ChancePercent))
            {
                byte dmgPct = (byte)(e.DamagePercent > 255 ? 255 : e.DamagePercent);
                JobPassiveSystem.ProcessEsperExplosion(
                    state, ref state.Units[attackerIndex], ref target, dmgPct);
            }
        }
```

- [ ] **Step 6: Oracle 로직 (힐량 계산)**

```csharp
        // ===== ORACLE =====

        public const int OracleHealTargetHPThreshold = 50;
        public const int OracleHealRangeBonus = 0;

        public static bool IsOracleHealer(CombatMatchState state, int unitIndex)
        {
            return state.OraclePassives[unitIndex].Active;
        }

        public static int OracleCalculateHealAmount(
            CombatMatchState state, int healerIndex, ref CombatUnit target)
        {
            ref var o = ref state.OraclePassives[healerIndex];
            if (!o.Active) return 0;

            ref var healer = ref state.Units[healerIndex];
            int amount = healer.Attack * o.HealPercent / 100;
            amount = amount * (100 + healer.HealPower) / 100;
            amount = amount * (100 + target.HealPower) / 100;
            if (amount < 1) amount = 1;
            return amount;
        }
```

- [ ] **Step 7: SkillKillMana 로직 (OnKill)**

```csharp
        // ===== SKILL KILL MANA =====

        public static void SkillKillManaOnKill(
            CombatMatchState state, int killerIndex, ref CombatUnit victim)
        {
            ref var skm = ref state.SkillKillManaPassives[killerIndex];
            if (!skm.Active) return;

            ref var killer = ref state.Units[killerIndex];
            if (killer.MaxMana <= 0 || !killer.IsAlive) return;

            if (StatusEffectSystem.CountMarkers(state, killerIndex, skm.MarkerType) > 0)
            {
                killer.CurrentMana = killer.MaxMana;
                StatusEffectSystem.RemoveOldestMarker(state, killerIndex, skm.MarkerType);
            }
        }
```

- [ ] **Step 8: 커밋**

```bash
git add Assets/_Project/Scripts/InGame_New/Simulation/Combat/JobPassive/JobPassiveLogic.cs
git commit -m "feat: JobPassiveLogic static 인라인 로직 (6종 + SkillKillMana)"
```

---

### Task 4: JobPassiveSystem 재작성 (SOA 초기화)

**Files:**
- Modify: `Assets/_Project/Scripts/InGame_New/Simulation/Combat/Traits/JobPassive/JobPassiveSystem.cs`
  (이후 Task 11에서 `Combat/JobPassive/` 로 이동)

- [ ] **Step 1: AttachJobPassive를 SOA 초기화로 재작성**

기존 `new XxxTrait()` + `TraitSystem.AddTrait()` 를 SOA 배열 직접 설정으로 교체:

```csharp
private static void AttachJobPassive(CombatMatchState state, int unitIndex,
    CharacterPositionType posType, int param0, int param1, int tickRate)
{
    switch (posType)
    {
        case CharacterPositionType.SHARPSHOOTER:
            if (param0 > 0)
            {
                state.SharpshooterPassives[unitIndex] = new SharpshooterPassive
                {
                    Active = true,
                    ChancePercent = param0,
                };
            }
            break;

        case CharacterPositionType.GHOST:
        {
            int maxStack = param0 / 100;
            if (maxStack > 0)
            {
                state.GhostPassives[unitIndex] = new GhostPassive
                {
                    Active = true,
                    MaxStack = maxStack,
                };
                state.Units[unitIndex].HasBacklineJump = true;
            }
            break;
        }

        case CharacterPositionType.STRIKER:
        {
            int cooldownFrames = param0 * tickRate / 100;
            if (cooldownFrames > 0)
            {
                state.StrikerPassives[unitIndex] = new StrikerPassive
                {
                    Active = true,
                    CooldownFrames = cooldownFrames,
                };
            }
            break;
        }

        case CharacterPositionType.GUARDIAN:
        {
            int guardCooldown = param0 * tickRate / 100;
            int charges = param1 > 0 ? param1 / 100 : 3;
            if (guardCooldown > 0)
            {
                state.GuardianPassives[unitIndex] = new GuardianPassive
                {
                    Active = true,
                    CooldownFrames = guardCooldown,
                    MaxCharges = charges,
                };
            }
            break;
        }

        case CharacterPositionType.ORACLE:
            state.Units[unitIndex].IsHealer = true;
            if (param0 > 0)
            {
                state.OraclePassives[unitIndex] = new OraclePassive
                {
                    Active = true,
                    HealPercent = param0,
                };
            }
            break;

        case CharacterPositionType.ESPER:
        {
            int dmgPercent = param1 > 0 ? param1 : 100;
            if (param0 > 0)
            {
                state.EsperPassives[unitIndex] = new EsperPassive
                {
                    Active = true,
                    ChancePercent = param0,
                    DamagePercent = dmgPercent,
                };
            }
            break;
        }
    }
}
```

- [ ] **Step 2: SetupJobPassives에서 TraitSystem.AddTrait 참조 제거**

SetupJobPassives 메서드 내에서 TraitSystem 관련 import/호출이 있으면 제거. AttachJobPassive가 SOA 배열을 직접 설정하므로 TraitSystem 불필요.

- [ ] **Step 3: ProcessEsperExplosion은 그대로 유지**

이 메서드는 TraitSystem과 무관하게 독립적으로 동작하므로 변경 불필요.

- [ ] **Step 4: 커밋**

```bash
git add Assets/_Project/Scripts/InGame_New/Simulation/Combat/Traits/JobPassive/JobPassiveSystem.cs
git commit -m "refactor: JobPassiveSystem을 SOA 배열 초기화로 재작성"
```

---

### Task 5: SkillSystem에서 SkillKillMana SOA 초기화

**Files:**
- Modify: `Assets/_Project/Scripts/InGame_New/Simulation/Combat/SkillSystem.cs:45-53`

- [ ] **Step 1: RegisterCharacterTraits를 SOA 방식으로 교체**

기존:
```csharp
if (unit.ChampionSpecId == 215532401)
    TraitSystem.AddTrait(state, i, new SkillKillManaResetTrait(SkillMarkerType.PiliaSkillCast));
```

교체:
```csharp
if (unit.ChampionSpecId == 215532401)
{
    state.SkillKillManaPassives[i] = new SkillKillManaData
    {
        Active = true,
        MarkerType = (int)SkillMarkerType.PiliaSkillCast,
    };
}
```

- [ ] **Step 2: TraitSystem using 제거**

- [ ] **Step 3: 커밋**

```bash
git add Assets/_Project/Scripts/InGame_New/Simulation/Combat/SkillSystem.cs
git commit -m "refactor: SkillSystem의 SkillKillMana를 SOA 초기화로 교체"
```

---

## Chunk 3: 시스템별 TraitSystem 호출 → JobPassiveLogic 인라인 교체

### Task 6: CombatAISystem — OnCombatStart, OnTick 교체

**Files:**
- Modify: `Assets/_Project/Scripts/InGame_New/Simulation/Combat/CombatAISystem.cs:22-39`

- [ ] **Step 1: InvokeCombatStart 교체 (L22-27)**

기존:
```csharp
if (!state._traitCombatStartDone)
{
    TraitSystem.InvokeCombatStart(state);
    state._traitCombatStartDone = true;
}
```

교체:
```csharp
if (!state._jobPassiveCombatStartDone)
{
    for (int j = 0; j < state.UnitCount; j++)
    {
        if (!state.Units[j].IsAlive) continue;
        JobPassiveLogic.GuardianOnCombatStart(state, j);
        JobPassiveLogic.StrikerOnCombatStart(state, j);
    }
    state._jobPassiveCombatStartDone = true;
}
```

- [ ] **Step 2: InvokeOnTick 교체 (L39)**

기존:
```csharp
TraitSystem.InvokeOnTick(state, i, tickRate);
```

교체:
```csharp
JobPassiveLogic.GuardianOnTick(state, i);
JobPassiveLogic.StrikerOnTick(state, i);
```

- [ ] **Step 3: TraitSystem using 제거**

- [ ] **Step 4: 커밋**

```bash
git add Assets/_Project/Scripts/InGame_New/Simulation/Combat/CombatAISystem.cs
git commit -m "refactor: CombatAISystem의 trait 콜백을 JobPassiveLogic으로 교체"
```

---

### Task 7: DamageSystem — 데미지 파이프라인 교체

**Files:**
- Modify: `Assets/_Project/Scripts/InGame_New/Simulation/Combat/Systems/Damage/DamageSystem.cs:111-183`

- [ ] **Step 1: ModifyOutgoingDamage 호출 제거 (L111)**

현재 어떤 패시브도 사용하지 않으므로 단순 제거:
```csharp
// 제거: damage = TraitSystem.InvokeModifyOutgoingDamage(...)
```

- [ ] **Step 2: ModifyIncomingDamage → GuardianModifyIncomingDamage 교체 (L117)**

기존:
```csharp
damage = TraitSystem.InvokeModifyIncomingDamage(state, ref state.Units[attackerIndex], targetIndex, damage, damageType, isBasicAttack);
```

교체:
```csharp
damage = JobPassiveLogic.GuardianModifyIncomingDamage(state, targetIndex, damage, isBasicAttack);
```

- [ ] **Step 3: OnDamageTaken 호출 제거 (L130, 144, 167)**

현재 어떤 패시브도 사용하지 않으므로 3곳 모두 단순 제거.

- [ ] **Step 4: OnDeath 호출 제거 (L179)**

현재 어떤 패시브도 사용하지 않으므로 단순 제거.

- [ ] **Step 5: OnKill → SkillKillManaOnKill 교체 (L183)**

기존:
```csharp
TraitSystem.InvokeOnKill(state, attackerIndex, ref target);
```

교체:
```csharp
JobPassiveLogic.SkillKillManaOnKill(state, attackerIndex, ref target);
```

- [ ] **Step 6: TraitSystem using 제거**

- [ ] **Step 7: 커밋**

```bash
git add Assets/_Project/Scripts/InGame_New/Simulation/Combat/Systems/Damage/DamageSystem.cs
git commit -m "refactor: DamageSystem의 trait 콜백을 JobPassiveLogic으로 교체"
```

---

### Task 8: DamageSystem.BasicAttack — PreAttack/PostAttack/Oracle 교체

**Files:**
- Modify: `Assets/_Project/Scripts/InGame_New/Simulation/Combat/Systems/Damage/DamageSystem.BasicAttack.cs`

- [ ] **Step 1: OnPreAttack 교체 (L27, L151)**

기존:
```csharp
TraitSystem.InvokeOnPreAttack(state, attackerIndex, ref target);
```

교체 (2곳 모두):
```csharp
JobPassiveLogic.GhostOnPreAttack(state, attackerIndex);
JobPassiveLogic.SharpshooterOnPreAttack(state, attackerIndex);
```

- [ ] **Step 2: FindTrait\<OracleHealerTrait\> 교체 (L33, L157)**

기존:
```csharp
var healTrait = attackerIndex >= 0
    ? TraitSystem.FindTrait<OracleHealerTrait>(state, attackerIndex)
    : null;
```

교체 (2곳 모두):
```csharp
bool isOracleHealer = attackerIndex >= 0
    && JobPassiveLogic.IsOracleHealer(state, attackerIndex);
```

힐 분기에서 `healTrait != null` → `isOracleHealer` 로 조건 교체.
`healTrait.CalculateHealAmount(ref healer, ref target)` → `JobPassiveLogic.OracleCalculateHealAmount(state, attackerIndex, ref target)` 로 교체.

- [ ] **Step 3: OnPostAttack 교체 (L64, L101, L170, L205)**

기존:
```csharp
TraitSystem.InvokeOnPostAttack(state, attackerIndex, ref target);
```

교체 (4곳 모두):
```csharp
JobPassiveLogic.GhostOnPostAttack(state, attackerIndex);
JobPassiveLogic.SharpshooterOnPostAttack(state, attackerIndex);
JobPassiveLogic.EsperOnPostAttack(state, attackerIndex, ref target);
```

- [ ] **Step 4: OnCritical 호출 제거 (L75, L185)**

현재 어떤 패시브도 사용하지 않으므로 단순 제거.

- [ ] **Step 5: TraitSystem, OracleHealerTrait using 제거**

- [ ] **Step 6: 커밋**

```bash
git add Assets/_Project/Scripts/InGame_New/Simulation/Combat/Systems/Damage/DamageSystem.BasicAttack.cs
git commit -m "refactor: BasicAttack의 trait 콜백을 JobPassiveLogic으로 교체"
```

---

### Task 9: ProjectileSystem — OnPostAttack 교체

**Files:**
- Modify: `Assets/_Project/Scripts/InGame_New/Simulation/Combat/ProjectileSystem.cs:85-88`

- [ ] **Step 1: InvokeOnPostAttack 교체 (L88)**

기존:
```csharp
TraitSystem.InvokeOnPostAttack(state, srcIdx, ref target);
```

교체:
```csharp
JobPassiveLogic.GhostOnPostAttack(state, srcIdx);
JobPassiveLogic.SharpshooterOnPostAttack(state, srcIdx);
JobPassiveLogic.EsperOnPostAttack(state, srcIdx, ref target);
```

- [ ] **Step 2: TraitSystem using 제거**

- [ ] **Step 3: 커밋**

```bash
git add Assets/_Project/Scripts/InGame_New/Simulation/Combat/ProjectileSystem.cs
git commit -m "refactor: ProjectileSystem의 trait 콜백을 JobPassiveLogic으로 교체"
```

---

## Chunk 4: 시너지 정리 + 구 파일 삭제 + 검증

### Task 10: GameLoopSystem/SynergySystem 정리

**Files:**
- Modify: `Assets/_Project/Scripts/InGame_New/Simulation/Core/GameLoopSystem.cs:260,283-284`
- Modify: `Assets/_Project/Scripts/InGame_New/Simulation/Synergy/SynergySystem.cs:716-765`
- Modify: `Assets/_Project/Scripts/InGame_New/Simulation/Synergy/SynergyFactory.cs:117-121`

- [ ] **Step 1: GameLoopSystem에서 ApplyBehaviors 호출 제거**

L260 제거: `SynergySystem.ApplyBehaviors(world, matchState, 0, 0);`
L283-284 제거:
```csharp
SynergySystem.ApplyBehaviors(world, matchState, match.PlayerA, 0);
SynergySystem.ApplyBehaviors(world, matchState, match.PlayerB, 1);
```

(현재 ApplyBehaviors는 SynergyFactory.CreateTrait가 null만 반환하므로 기능 없음)

- [ ] **Step 2: SynergySystem.ApplyBehaviors 메서드 삭제**

- [ ] **Step 3: SynergyFactory.CreateTrait 메서드 삭제**

- [ ] **Step 4: 커밋**

```bash
git add Assets/_Project/Scripts/InGame_New/Simulation/Core/GameLoopSystem.cs
git add Assets/_Project/Scripts/InGame_New/Simulation/Synergy/SynergySystem.cs
git add Assets/_Project/Scripts/InGame_New/Simulation/Synergy/SynergyFactory.cs
git commit -m "refactor: ApplyBehaviors/CreateTrait 삭제 (trait 시스템 제거)"
```

---

### Task 11: Traits 폴더 삭제 + JobPassiveSystem 이동

**Files:**
- Move: `Traits/JobPassive/JobPassiveSystem.cs` → `Combat/JobPassive/JobPassiveSystem.cs`
- Delete: `Traits/` 폴더 전체 (이동한 파일 제외)

- [ ] **Step 1: JobPassiveSystem.cs를 새 위치로 이동**

```bash
git mv "Assets/_Project/Scripts/InGame_New/Simulation/Combat/Traits/JobPassive/JobPassiveSystem.cs" \
       "Assets/_Project/Scripts/InGame_New/Simulation/Combat/JobPassive/JobPassiveSystem.cs"
```

namespace가 달라야 하면 조정 (기존 namespace 확인 후 결정).

- [ ] **Step 2: Traits 폴더 전체 삭제**

```bash
rm -rf "Assets/_Project/Scripts/InGame_New/Simulation/Combat/Traits/"
rm -f "Assets/_Project/Scripts/InGame_New/Simulation/Combat/Traits.meta"
```

삭제되는 파일 (9개):
- CombatTraitBase.cs
- TraitSystem.cs
- SkillKillManaResetTrait.cs
- JobPassive/GuardianEndureTrait.cs
- JobPassive/GhostCritStackTrait.cs
- JobPassive/StrikerCCImmuneTrait.cs
- JobPassive/SharpshooterPierceTrait.cs
- JobPassive/EsperExplosionTrait.cs
- JobPassive/OracleHealerTrait.cs

- [ ] **Step 3: 커밋**

```bash
git add -A Assets/_Project/Scripts/InGame_New/Simulation/Combat/Traits/
git add -A Assets/_Project/Scripts/InGame_New/Simulation/Combat/JobPassive/
git commit -m "refactor: Traits 폴더 삭제, JobPassiveSystem을 Combat/JobPassive로 이동"
```

---

### Task 12: 컴파일 확인 및 잔여 참조 정리

- [ ] **Step 1: 프로젝트 전체에서 잔여 참조 검색**

```
검색 키워드:
CombatTraitBase, TraitSystem, SkillKillManaResetTrait,
GuardianEndureTrait, GhostCritStackTrait, StrikerCCImmuneTrait,
SharpshooterPierceTrait, EsperExplosionTrait, OracleHealerTrait,
InvokeCombatStart, InvokeOnTick, InvokeModifyOutgoingDamage,
InvokeModifyIncomingDamage, InvokeOnDamageTaken, InvokeOnKill,
InvokeOnDeath, InvokeOnPreAttack, InvokeOnPostAttack, InvokeOnCritical,
_traitCombatStartDone
```

- [ ] **Step 2: 발견된 잔여 참조 정리**

- [ ] **Step 3: Unity 에디터에서 컴파일 확인**

- [ ] **Step 4: 최종 커밋**

```bash
git add -A
git commit -m "refactor: CombatTraitBase → SOA 직업 패시브 마이그레이션 완료"
```

---

## 작업 순서 요약

```
Task 1:  JobPassiveData.cs — 7개 구조체 정의
Task 2:  Components.cs — SOA 배열 추가, Traits 제거
Task 3:  JobPassiveLogic.cs — 인라인 로직 (Guardian/Striker/Ghost/Sharpshooter/Esper/Oracle/SkillKillMana)
Task 4:  JobPassiveSystem.cs — SOA 초기화로 재작성
Task 5:  SkillSystem.cs — SkillKillMana SOA 초기화
Task 6:  CombatAISystem.cs — OnCombatStart/OnTick 교체
Task 7:  DamageSystem.cs — 데미지 파이프라인 교체
Task 8:  DamageSystem.BasicAttack.cs — PreAttack/PostAttack/Oracle 교체
Task 9:  ProjectileSystem.cs — OnPostAttack 교체
Task 10: GameLoopSystem/SynergySystem — ApplyBehaviors 정리
Task 11: Traits 폴더 삭제 + JobPassiveSystem 이동
Task 12: 컴파일 확인 및 잔여 참조 정리
```

## 최종 디렉토리 구조

```
Simulation/Combat/JobPassive/
├── JobPassiveData.cs      (신규: 7개 구조체)
├── JobPassiveLogic.cs     (신규: static 인라인 로직)
└── JobPassiveSystem.cs    (이동+수정: SOA 초기화 + ProcessEsperExplosion)
```

## 주의사항

1. **TraitFlags는 건드리지 않는다** — 시너지 식별 비트마스크로 이 작업과 독립적
2. **ProcessEsperExplosion은 JobPassiveSystem에 유지** — DamageSystem, SkillAreaHelper 등과의 의존성이 있으므로 이동만 하고 로직은 변경하지 않음
3. **OracleHealerTrait의 상수 (HealTargetHPThreshold=50, HealRangeBonus=0)** — JobPassiveLogic에 const로 이전
4. **namespace 확인 필요** — 이동 시 기존 namespace와 일치시킬 것
5. **InGameTestConfig 파일은 커밋에서 제외** — 기존 규칙 준수
