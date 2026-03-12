# 시간 기반 마나 리젠 시스템 구현 계획

> **For agentic workers:** REQUIRED: Use superpowers:subagent-driven-development (if subagents available) or superpowers:executing-plans to implement this plan. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 마나 충전 방식을 시간 기반 리젠 + 스펙 기반 타격/피격 마나로 전환하고, 리젠 속도 증가 및 MaxMana 감소 버프를 지원한다.

**Architecture:** CombatUnit에 마나 리젠 필드 추가 → DamageSystem 하드코딩 상수를 유닛 필드 참조로 변경 → ManaSystem 신규 생성으로 초당 1회 시간 리젠 → StatModType 확장으로 버프 지원. 모든 연산은 정수, static 시스템 메서드 패턴.

**Tech Stack:** C# (Unity 6), InGame_New Simulation 레이어

**Spec:** `docs/superpowers/specs/2026-03-12-time-based-mana-regen-design.md`

---

## Chunk 1: 데이터 레이어 + 시간 리젠 코어

### Task 1: CombatUnit 마나 리젠 필드 추가

**Files:**
- Modify: `Assets/_Project/Scripts/InGame_New/Simulation/Data/Components.cs:509-510`

- [ ] **Step 1: CombatUnit에 마나 리젠 필드 3개 추가**

`Components.cs`에서 `MaxMana`, `CurrentMana` 필드 근처(line 510 이후)에 추가:

```csharp
public int MaxMana;
public int CurrentMana;
// ── 마나 리젠 ──
public int ManaRegenPerSec;    // 초당 시간 리젠량
public int ManaGainOnAttack;   // 타격 시 마나 획득량
public int ManaGainOnHit;      // 피격 시 마나 획득량
public int ManaRegenRateBonus; // 마나 리젠 속도 보너스 % (버프/디버프 누적)
```

`ManaRegenRateBonus`는 StatBuff로 증감되는 누적 % 값. 리젠 계산 시 `baseManaRegen * (100 + ManaRegenRateBonus) / 100`.

- [ ] **Step 2: 커밋**

```bash
git add Assets/_Project/Scripts/InGame_New/Simulation/Data/Components.cs
git commit -m "feat(mana): CombatUnit에 마나 리젠 관련 필드 추가"
```

---

### Task 2: StatModType 확장

**Files:**
- Modify: `Assets/_Project/Scripts/InGame_New/Simulation/Data/Enums.cs:128-134`

- [ ] **Step 1: StatModType에 ManaRegenRate, MaxMana 추가**

`Enums.cs`의 `StatModType` enum 끝에 추가:

```csharp
public enum StatModType
{
    Attack,
    Armor,
    MagicResist,
    AttackSpeed,
    ManaRegenRate,  // 마나 리젠 속도 % 보너스
    MaxMana,        // 최대 마나 증감
}
```

- [ ] **Step 2: 커밋**

```bash
git add Assets/_Project/Scripts/InGame_New/Simulation/Data/Enums.cs
git commit -m "feat(mana): StatModType에 ManaRegenRate, MaxMana 추가"
```

---

### Task 3: GameConfig 글로벌 기본값 추가

**Files:**
- Modify: `Assets/_Project/Scripts/InGame_New/Simulation/Data/GameConfig.cs`

- [ ] **Step 1: 마나 리젠 기본값 상수 추가**

`GameConfig.cs`에 기존 상수들 근처에 추가:

```csharp
// ── 마나 리젠 기본값 ──
public const int DefaultManaRegenPerSec = 10;
public const int DefaultManaGainOnAttack = 10;
public const int DefaultManaGainOnHit = 10;
```

- [ ] **Step 2: 커밋**

```bash
git add Assets/_Project/Scripts/InGame_New/Simulation/Data/GameConfig.cs
git commit -m "feat(mana): GameConfig에 마나 리젠 글로벌 기본값 추가"
```

---

### Task 4: ManaSystem 신규 생성 — 시간 리젠 틱

**Files:**
- Create: `Assets/_Project/Scripts/InGame_New/Simulation/Combat/ManaSystem.cs`

- [ ] **Step 1: ManaSystem static 클래스 작성**

```csharp
using CookApps.AutoBattler;

namespace CookApps.AutoBattler.InGameNew
{
    public static class ManaSystem
    {
        /// <summary>
        /// 초당 1회 호출. 살아있는 모든 유닛에게 시간 기반 마나 리젠 적용.
        /// </summary>
        public static void TickManaRegen(CombatMatchState state)
        {
            // 초당 1회만 실행
            if (state.FrameCount % state.TickRate != 0) return;

            for (int t = 0; t < 2; t++)
            {
                var team = t == 0 ? state.TeamA : state.TeamB;
                for (int i = 0; i < team.Length; i++)
                {
                    ref var unit = ref team[i];
                    if (!unit.IsAlive) continue;
                    if (unit.MaxMana <= 0) continue;

                    int baseRegen = unit.ManaRegenPerSec;
                    if (baseRegen <= 0) continue;

                    int finalRegen = baseRegen * (100 + unit.ManaRegenRateBonus) / 100;
                    if (finalRegen < 0) finalRegen = 0;

                    DamageSystem.ChargeMana(ref unit, finalRegen);
                }
            }
        }
    }
}
```

패턴 참고: 다른 시스템 클래스(`DamageSystem`, `SkillSystem`)와 동일하게 static 클래스 + static 메서드.
네임스페이스는 기존 파일들의 네임스페이스를 따를 것 — 실제 파일에서 확인 후 맞출 것.

- [ ] **Step 2: 커밋**

```bash
git add Assets/_Project/Scripts/InGame_New/Simulation/Combat/ManaSystem.cs
git commit -m "feat(mana): ManaSystem — 초당 1회 시간 기반 마나 리젠"
```

---

### Task 5: GameLoopSystem에서 ManaSystem 호출

**Files:**
- Modify: `Assets/_Project/Scripts/InGame_New/Simulation/Core/GameLoopSystem.cs:111`

- [ ] **Step 1: UpdateCombat()에서 ManaSystem.TickManaRegen 호출 추가**

`GameLoopSystem.cs`의 `UpdateCombat()` 메서드에서 `CombatAISystem.Tick()` 호출 **전에** 마나 리젠을 처리:

```csharp
ManaSystem.TickManaRegen(state);
CombatAISystem.Tick(state, ref rng); // 기존 코드
```

마나 리젠이 먼저 적용되어야 해당 프레임의 AI가 마나풀 상태를 올바르게 감지.

- [ ] **Step 2: 커밋**

```bash
git add Assets/_Project/Scripts/InGame_New/Simulation/Core/GameLoopSystem.cs
git commit -m "feat(mana): GameLoopSystem에서 ManaSystem.TickManaRegen 호출"
```

---

## Chunk 2: 기존 하드코딩 제거 + 셋업 연동

### Task 6: DamageSystem 하드코딩 상수 → 유닛 필드 참조

**Files:**
- Modify: `Assets/_Project/Scripts/InGame_New/Simulation/Combat/DamageSystem.cs:15-18, 241, 268, 320-321, 563`

- [ ] **Step 1: 하드코딩 상수 제거**

`DamageSystem.cs` 상단의 상수 선언 삭제:

```csharp
// 삭제:
// private const int ManaGainOnAttack = 10;
// private const int ManaGainOnHit = 10;
```

- [ ] **Step 2: 모든 ChargeMana 호출을 유닛 필드 참조로 변경**

각 호출 위치를 다음과 같이 변경:

```csharp
// 기존: ChargeMana(ref target, ManaGainOnHit);
// 변경:
ChargeMana(ref target, target.ManaGainOnHit);

// 기존: ChargeMana(ref attacker, ManaGainOnAttack);
// 변경:
ChargeMana(ref attacker, attacker.ManaGainOnAttack);
```

**모든 호출 위치** (line 241, 268, 320, 321, 563)에서 변경. 563번 라인 area attack에서 attacker 마나 충전이 빠져있다면 그건 기존 동작 유지 (이 PR 스코프 밖).

- [ ] **Step 3: 커밋**

```bash
git add Assets/_Project/Scripts/InGame_New/Simulation/Combat/DamageSystem.cs
git commit -m "refactor(mana): DamageSystem 하드코딩 마나 상수를 유닛 필드로 대체"
```

---

### Task 7: CombatSetupSystem에서 마나 리젠 값 초기화

**Files:**
- Modify: `Assets/_Project/Scripts/InGame_New/Simulation/Combat/CombatSetupSystem.cs:88`

- [ ] **Step 1: 유닛 초기화에 마나 리젠 값 설정**

`CombatSetupSystem.cs`에서 `combatUnit.CurrentMana = 0;` 근처에 추가. 스펙에서 값을 읽어오되, 0이면 GameConfig 기본값 fallback:

```csharp
combatUnit.CurrentMana = 0;
// 마나 리젠 초기화 (스펙 값 > 0이면 스펙, 아니면 글로벌 기본값)
combatUnit.ManaRegenPerSec = spec.ManaRegenPerSec > 0 ? spec.ManaRegenPerSec : GameConfig.DefaultManaRegenPerSec;
combatUnit.ManaGainOnAttack = spec.ManaGainOnAttack > 0 ? spec.ManaGainOnAttack : GameConfig.DefaultManaGainOnAttack;
combatUnit.ManaGainOnHit = spec.ManaGainOnHit > 0 ? spec.ManaGainOnHit : GameConfig.DefaultManaGainOnHit;
combatUnit.ManaRegenRateBonus = 0;
```

**중요**: 여기서 `spec`이 어떤 타입인지 확인 필요. 현재 CombatSetupSystem에서 사용하는 스펙 타입에 해당 필드가 없을 수 있음. 해당 스펙 타입에 필드가 없으면 일단 GameConfig 기본값만 사용하고, 스펙 필드 추가는 별도 작업으로:

```csharp
// 스펙 필드가 아직 없는 경우 임시 처리
combatUnit.ManaRegenPerSec = GameConfig.DefaultManaRegenPerSec;
combatUnit.ManaGainOnAttack = GameConfig.DefaultManaGainOnAttack;
combatUnit.ManaGainOnHit = GameConfig.DefaultManaGainOnHit;
combatUnit.ManaRegenRateBonus = 0;
```

- [ ] **Step 2: 커밋**

```bash
git add Assets/_Project/Scripts/InGame_New/Simulation/Combat/CombatSetupSystem.cs
git commit -m "feat(mana): CombatSetupSystem에서 마나 리젠 값 초기화"
```

---

## Chunk 3: 버프 시스템 연동

### Task 8: SkillBuffHelper에 ManaRegenRate, MaxMana 스탯 처리 추가

**Files:**
- Modify: `Assets/_Project/Scripts/InGame_New/Simulation/Combat/Skills/SkillHelpers.cs:323-346`

- [ ] **Step 1: ModifyStat에 새 case 추가**

`SkillBuffHelper.ModifyStat()` 메서드의 switch문에 추가:

```csharp
case StatModType.ManaRegenRate:
    target.ManaRegenRateBonus += value;
    break;
case StatModType.MaxMana:
    target.MaxMana += value;
    if (target.MaxMana < 1) target.MaxMana = 1;
    if (target.CurrentMana > target.MaxMana)
        target.CurrentMana = target.MaxMana;
    break;
```

`MaxMana` 변경 시 `CurrentMana`가 초과하면 클램프. `MaxMana`는 최소 1 유지.
`ManaRegenRateBonus`는 음수 가능 (디버프로 리젠 감소).

- [ ] **Step 2: 커밋**

```bash
git add Assets/_Project/Scripts/InGame_New/Simulation/Combat/Skills/SkillHelpers.cs
git commit -m "feat(mana): SkillBuffHelper에 ManaRegenRate, MaxMana 스탯 처리"
```

---

### Task 9: 최종 검증 및 정리

**Files:**
- 전체 변경 파일 확인

- [ ] **Step 1: 컴파일 확인**

Unity Editor에서 컴파일 에러 없는지 확인. 특히:
- `ManaSystem` 네임스페이스가 다른 시스템 파일과 일치하는지
- `CombatMatchState`에서 TeamA/TeamB 접근 방식이 실제 구조와 맞는지 (배열 vs 리스트 등)
- `DamageSystem.ChargeMana`가 `ManaSystem`에서 접근 가능한지 (접근제어자)

- [ ] **Step 2: HasPushedManaFull 리셋 확인**

`SkillSystem.TryCast()`에서 `unit.CurrentMana = 0` 후 `unit.HasPushedManaFull = false`로 리셋하는지 확인. 시간 리젠으로 마나가 다시 차면 ManaFull 이벤트가 재발행되어야 하므로, 리셋이 안 되어 있으면 추가:

```csharp
unit.CurrentMana = 0;
unit.HasPushedManaFull = false; // 마나 리셋 시 플래그도 초기화
```

- [ ] **Step 3: 최종 커밋**

```bash
git add -A
git commit -m "fix(mana): HasPushedManaFull 리셋 및 최종 정리"
```

---

## View 레이어 (별도 작업)

마나바 lerp 보간은 이 계획 스코프에서 제외. 현재 `UnitViewManager.UpdateMana(current, max)`가 매 sync frame 호출되므로 기본 동작은 유지됨. 부드러운 보간이 필요하면 View 레이어에서 `previousMana → currentMana` lerp를 별도로 추가.

---

## 요약

| Task | 설명 | 파일 |
|------|------|------|
| 1 | CombatUnit 필드 추가 | Components.cs |
| 2 | StatModType 확장 | Enums.cs |
| 3 | GameConfig 기본값 | GameConfig.cs |
| 4 | ManaSystem 신규 생성 | ManaSystem.cs (new) |
| 5 | GameLoopSystem 연동 | GameLoopSystem.cs |
| 6 | DamageSystem 하드코딩 제거 | DamageSystem.cs |
| 7 | CombatSetupSystem 초기화 | CombatSetupSystem.cs |
| 8 | SkillBuffHelper 확장 | SkillHelpers.cs |
| 9 | 최종 검증 | 전체 |
