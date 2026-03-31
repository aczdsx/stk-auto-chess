# CombatTraitBase 시스템 제거 계획

> **For agentic workers:** REQUIRED: Use superpowers:subagent-driven-development (if subagents available) or superpowers:executing-plans to implement this plan. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** InGame_New → Quantum 전환에 따라 C# virtual 디스패치 기반 CombatTraitBase 시스템을 완전히 제거한다.

**Architecture:** CombatTraitBase는 전투 특성(직업 패시브 6종 + 스킬킬마나 1종)의 추상 베이스 클래스로, TraitSystem이 10개 콜백을 디스패치한다. Quantum ECS 전환 시 이 패턴은 Quantum 시스템으로 대체되므로, 기존 코드를 삭제하고 14개 외부 참조 파일에서 호출부를 제거한다.

**Tech Stack:** Unity C#, Photon Quantum

---

## 영향 범위 요약

### 삭제 대상 (Traits 폴더 전체 — 10개 파일)

| 파일 | 역할 |
|------|------|
| `Traits/CombatTraitBase.cs` | 추상 베이스 클래스 (10개 virtual 콜백) |
| `Traits/TraitSystem.cs` | 콜백 디스패치 시스템 |
| `Traits/SkillKillManaResetTrait.cs` | 스킬 킬 시 마나 리셋 trait |
| `Traits/JobPassive/JobPassiveSystem.cs` | 직업 패시브 부착 시스템 |
| `Traits/JobPassive/EsperExplosionTrait.cs` | ESPER 패시브 |
| `Traits/JobPassive/OracleHealerTrait.cs` | ORACLE 패시브 |
| `Traits/JobPassive/GuardianEndureTrait.cs` | GUARDIAN 패시브 |
| `Traits/JobPassive/StrikerCCImmuneTrait.cs` | STRIKER 패시브 |
| `Traits/JobPassive/SharpshooterPierceTrait.cs` | SHARPSHOOTER 패시브 |
| `Traits/JobPassive/GhostCritStackTrait.cs` | GHOST 패시브 |

### 참조 정리 대상 (14개 외부 파일)

| 파일 | 제거할 참조 | 비고 |
|------|------------|------|
| **GameLoopSystem.cs** | `JobPassiveSystem.SetupJobPassives()` (L261,285), `SynergySystem.ApplyBehaviors()` (L260,283-284) | 전투 초기화 흐름에서 호출 제거 |
| **CombatAISystem.cs** | `TraitSystem.InvokeCombatStart()` (L25), `TraitSystem.InvokeOnTick()` (L39) | 전투 AI 틱에서 호출 제거 |
| **SkillSystem.cs** | `TraitSystem.AddTrait()` + `SkillKillManaResetTrait` (L52) | 스킬 킬 마나 trait 부착 제거 |
| **DamageSystem.cs** | `InvokeModifyOutgoing/IncomingDamage`, `InvokeOnDamageTaken`, `InvokeOnKill`, `InvokeOnDeath` (L111,117,130,144,167,179,183) | 데미지 파이프라인 trait 콜백 제거 |
| **DamageSystem.BasicAttack.cs** | `InvokeOnPreAttack`, `InvokeOnPostAttack`, `InvokeOnCritical`, `FindTrait<OracleHealerTrait>` (L27,33,64,75,101,151,157,170,185,205) | 기본 공격 trait 콜백 제거 |
| **ProjectileSystem.cs** | `TraitSystem.InvokeOnPostAttack()` (L88) | 투사체 명중 시 콜백 제거 |
| **SynergySystem.cs** | `TraitSystem.AddTrait()` (L762), `SynergyFactory.CreateTrait()` (L754), `ApplyBehaviors()` 메서드 본체 (L716-765) | 시너지 행동 부착 로직 제거 |
| **SynergyFactory.cs** | `CreateTrait()` 메서드 전체 (L117-121) | 팩토리 메서드 제거 |
| **Components.cs** | `CombatTraitBase[][] Traits` (L807), `TraitCounts[]` (L808), 초기화 (L827-832) | 상태 구조체에서 trait 필드 제거 |

### TraitFlags — 별도 검토 필요

TraitFlags는 시너지 식별용 비트마스크로 CombatTraitBase와는 **독립적인 개념**. 아래 파일에서 사용 중:

| 파일 | 줄 |
|------|-----|
| Components.cs | L47, 219, 509, 666 |
| BoardSystem.cs | L292 |
| CombatSetupSystem.cs | L90, 170, 341 |
| SynergySystem.cs | L58, 232, 270, 807, 861 |
| SynergyPrepSupernova.cs | L112, 172 |
| AutoChessViewBridge.cs | L481, 957 |
| SupernovaObjectView.cs | L63 |
| AutoChessSpecAdapter.cs | L53, 104 |

> **TraitFlags는 이번 제거 범위에 포함하지 않는다.** 시너지 식별 로직이므로 Quantum에서도 유사한 개념이 필요할 수 있다.

---

## Chunk 1: Traits 폴더 삭제 및 Components 정리

### Task 1: Components.cs에서 CombatTraitBase 관련 필드 제거

**Files:**
- Modify: `Assets/_Project/Scripts/InGame_New/Simulation/Data/Components.cs:807-832`

- [ ] **Step 1: CombatMatchState에서 Traits/TraitCounts 필드 제거**

`Components.cs`에서 다음을 제거:
```csharp
// L807-808 제거
public CombatTraitBase[][] Traits;
public int[] TraitCounts;
```

- [ ] **Step 2: InitMatchState()에서 Traits 초기화 코드 제거**

`Components.cs`에서 다음을 제거:
```csharp
// L827-832 부근 제거
Traits = new CombatTraitBase[MaxCombatUnits][];
// ... TraitCounts 초기화 및 for 루프 내 Traits[i] 초기화
```

- [ ] **Step 3: 커밋**

```bash
git add Assets/_Project/Scripts/InGame_New/Simulation/Data/Components.cs
git commit -m "refactor: CombatMatchState에서 Traits/TraitCounts 필드 제거"
```

---

### Task 2: Traits 폴더 전체 삭제

**Files:**
- Delete: `Assets/_Project/Scripts/InGame_New/Simulation/Combat/Traits/` (폴더 전체)

- [ ] **Step 1: Traits 폴더 및 .meta 파일 삭제**

```bash
rm -rf Assets/_Project/Scripts/InGame_New/Simulation/Combat/Traits/
rm -f Assets/_Project/Scripts/InGame_New/Simulation/Combat/Traits.meta
```

- [ ] **Step 2: 커밋**

```bash
git add -A Assets/_Project/Scripts/InGame_New/Simulation/Combat/Traits/
git add -A Assets/_Project/Scripts/InGame_New/Simulation/Combat/Traits.meta
git commit -m "refactor: CombatTraitBase 시스템 전체 삭제 (Traits 폴더)"
```

---

## Chunk 2: 전투 시스템 참조 정리

### Task 3: GameLoopSystem.cs — 전투 초기화에서 trait 호출 제거

**Files:**
- Modify: `Assets/_Project/Scripts/InGame_New/Simulation/Core/GameLoopSystem.cs:260-285`

- [ ] **Step 1: PvE 초기화에서 제거**

L260-261 제거:
```csharp
SynergySystem.ApplyBehaviors(world, matchState, 0, 0);
JobPassiveSystem.SetupJobPassives(matchState, world);
```

- [ ] **Step 2: PvP 초기화에서 제거**

L283-285 제거:
```csharp
SynergySystem.ApplyBehaviors(world, matchState, match.PlayerA, 0);
SynergySystem.ApplyBehaviors(world, matchState, match.PlayerB, 1);
JobPassiveSystem.SetupJobPassives(matchState, world);
```

- [ ] **Step 3: 커밋**

```bash
git add Assets/_Project/Scripts/InGame_New/Simulation/Core/GameLoopSystem.cs
git commit -m "refactor: GameLoopSystem에서 trait/jobpassive 초기화 호출 제거"
```

---

### Task 4: CombatAISystem.cs — 전투 AI에서 trait 콜백 제거

**Files:**
- Modify: `Assets/_Project/Scripts/InGame_New/Simulation/Combat/CombatAISystem.cs:25,39`

- [ ] **Step 1: InvokeCombatStart 호출 제거 (L25)**

```csharp
// 제거: TraitSystem.InvokeCombatStart(...)
```

- [ ] **Step 2: InvokeOnTick 호출 제거 (L39)**

```csharp
// 제거: TraitSystem.InvokeOnTick(...)
```

- [ ] **Step 3: 커밋**

```bash
git add Assets/_Project/Scripts/InGame_New/Simulation/Combat/CombatAISystem.cs
git commit -m "refactor: CombatAISystem에서 trait 콜백 호출 제거"
```

---

### Task 5: DamageSystem.cs — 데미지 파이프라인에서 trait 콜백 제거

**Files:**
- Modify: `Assets/_Project/Scripts/InGame_New/Simulation/Combat/Systems/Damage/DamageSystem.cs:111,117,130,144,167,179,183`

- [ ] **Step 1: ModifyOutgoingDamage 호출 제거 (L111)**
- [ ] **Step 2: ModifyIncomingDamage 호출 제거 (L117)**
- [ ] **Step 3: OnDamageTaken 호출 제거 (L130, 144, 167)**
- [ ] **Step 4: OnDeath 호출 제거 (L179)**
- [ ] **Step 5: OnKill 호출 제거 (L183)**
- [ ] **Step 6: 커밋**

```bash
git add Assets/_Project/Scripts/InGame_New/Simulation/Combat/Systems/Damage/DamageSystem.cs
git commit -m "refactor: DamageSystem에서 trait 콜백 호출 제거"
```

---

### Task 6: DamageSystem.BasicAttack.cs — 기본 공격에서 trait 콜백 제거

**Files:**
- Modify: `Assets/_Project/Scripts/InGame_New/Simulation/Combat/Systems/Damage/DamageSystem.BasicAttack.cs:27,33,64,75,101,151,157,170,185,205`

- [ ] **Step 1: OnPreAttack 호출 제거 (L27, 151)**
- [ ] **Step 2: FindTrait<OracleHealerTrait> 및 힐 분기 제거 (L33)**
- [ ] **Step 3: OnPostAttack 호출 제거 (L64, 101, 170, 205)**
- [ ] **Step 4: OnCritical 호출 제거 (L75, 185)**
- [ ] **Step 5: 커밋**

```bash
git add Assets/_Project/Scripts/InGame_New/Simulation/Combat/Systems/Damage/DamageSystem.BasicAttack.cs
git commit -m "refactor: DamageSystem.BasicAttack에서 trait 콜백 호출 제거"
```

---

### Task 7: SkillSystem.cs — 스킬킬마나 trait 부착 제거

**Files:**
- Modify: `Assets/_Project/Scripts/InGame_New/Simulation/Combat/SkillSystem.cs:52`

- [ ] **Step 1: SkillKillManaResetTrait 생성 및 AddTrait 호출 제거 (L52)**

```csharp
// 제거: TraitSystem.AddTrait(state, unitIndex, new SkillKillManaResetTrait());
```

- [ ] **Step 2: 커밋**

```bash
git add Assets/_Project/Scripts/InGame_New/Simulation/Combat/SkillSystem.cs
git commit -m "refactor: SkillSystem에서 SkillKillManaResetTrait 부착 제거"
```

---

### Task 8: ProjectileSystem.cs — 투사체 명중 시 trait 콜백 제거

**Files:**
- Modify: `Assets/_Project/Scripts/InGame_New/Simulation/Combat/ProjectileSystem.cs:88`

- [ ] **Step 1: InvokeOnPostAttack 호출 제거 (L88)**

- [ ] **Step 2: 커밋**

```bash
git add Assets/_Project/Scripts/InGame_New/Simulation/Combat/ProjectileSystem.cs
git commit -m "refactor: ProjectileSystem에서 trait 콜백 호출 제거"
```

---

## Chunk 3: 시너지 시스템 참조 정리

### Task 9: SynergySystem.cs — ApplyBehaviors 메서드 정리

**Files:**
- Modify: `Assets/_Project/Scripts/InGame_New/Simulation/Synergy/SynergySystem.cs:716-765`

- [ ] **Step 1: ApplyBehaviors() 메서드 본체 비우기 또는 삭제**

`ApplyBehaviors()` 메서드 전체를 삭제한다. GameLoopSystem에서의 호출은 Task 3에서 이미 제거됨.

주의: `ApplyBehaviors`가 public static이므로 다른 곳에서 호출하는지 확인할 것 (GameLoopSystem 외에는 없음).

- [ ] **Step 2: CreateTrait 호출부 제거 (L754)**

ApplyBehaviors 삭제 시 함께 제거됨.

- [ ] **Step 3: TraitSystem.AddTrait 호출부 제거 (L762)**

ApplyBehaviors 삭제 시 함께 제거됨.

- [ ] **Step 4: 커밋**

```bash
git add Assets/_Project/Scripts/InGame_New/Simulation/Synergy/SynergySystem.cs
git commit -m "refactor: SynergySystem에서 ApplyBehaviors 메서드 삭제"
```

---

### Task 10: SynergyFactory.cs — CreateTrait 메서드 제거

**Files:**
- Modify: `Assets/_Project/Scripts/InGame_New/Simulation/Synergy/SynergyFactory.cs:117-121`

- [ ] **Step 1: CreateTrait() 메서드 삭제**

```csharp
// L117-121 전체 제거
public static CombatTraitBase CreateTrait(SynergyType type, byte tier)
{
    return null;
}
```

- [ ] **Step 2: CombatTraitBase using 문 제거 (있다면)**

- [ ] **Step 3: 커밋**

```bash
git add Assets/_Project/Scripts/InGame_New/Simulation/Synergy/SynergyFactory.cs
git commit -m "refactor: SynergyFactory에서 CreateTrait 메서드 삭제"
```

---

## Chunk 4: 최종 검증

### Task 11: 컴파일 확인 및 누락 참조 정리

- [ ] **Step 1: Unity 에디터에서 컴파일 확인**

Unity 에디터를 열어 컴파일 에러가 없는지 확인한다.

- [ ] **Step 2: 잔여 참조 검색**

프로젝트 전체에서 다음 키워드가 남아있지 않은지 확인:
```
CombatTraitBase, TraitSystem, JobPassiveSystem, SkillKillManaResetTrait,
EsperExplosionTrait, OracleHealerTrait, GuardianEndureTrait,
StrikerCCImmuneTrait, SharpshooterPierceTrait, GhostCritStackTrait,
InvokeCombatStart, InvokeOnTick, InvokeModifyOutgoingDamage,
InvokeModifyIncomingDamage, InvokeOnDamageTaken, InvokeOnKill,
InvokeOnDeath, InvokeOnPreAttack, InvokeOnPostAttack, InvokeOnCritical
```

주의: `TraitFlags`, `TraitCounts`는 이번 제거 범위 밖이므로 남아있어도 정상. 단, `TraitCounts`는 Components.cs에서 Traits와 함께 제거되었으므로 다른 곳에서 참조하면 에러 발생 → 발견 시 추가 정리.

- [ ] **Step 3: 누락 발견 시 추가 정리 후 커밋**

```bash
git add -A
git commit -m "refactor: CombatTraitBase 잔여 참조 정리"
```

- [ ] **Step 4: 최종 커밋 (스쿼시 또는 정리 필요 시)**

```bash
# 필요 시 전체 작업을 하나의 커밋으로 합치기
git rebase -i HEAD~N
```

---

## 작업 순서 요약

```
Task 1:  Components.cs Traits/TraitCounts 필드 제거
Task 2:  Traits 폴더 전체 삭제
Task 3:  GameLoopSystem.cs 호출 제거
Task 4:  CombatAISystem.cs 콜백 호출 제거
Task 5:  DamageSystem.cs 콜백 호출 제거
Task 6:  DamageSystem.BasicAttack.cs 콜백 호출 제거
Task 7:  SkillSystem.cs 스킬킬마나 trait 제거
Task 8:  ProjectileSystem.cs 콜백 호출 제거
Task 9:  SynergySystem.cs ApplyBehaviors 삭제
Task 10: SynergyFactory.cs CreateTrait 삭제
Task 11: 컴파일 확인 및 잔여 참조 정리
```

## 주의사항

1. **TraitFlags는 건드리지 않는다** — 시너지 식별 비트마스크로 CombatTraitBase와 독립적
2. **OracleHealerTrait 힐 로직** — DamageSystem.BasicAttack에서 `FindTrait<OracleHealerTrait>()`로 힐 분기하는 부분이 있음. 이 힐 로직이 Quantum에서 별도 구현되지 않으면 ORACLE 직업의 힐 기능이 사라짐
3. **GuardianEndureTrait 쉴드 로직** — ModifyIncomingDamage에서 데미지를 0으로 만드는 로직. Quantum에서 별도 구현 필요
4. **전투 밸런스** — 6가지 직업 패시브가 모두 비활성화되므로 Quantum 쪽에서 동등한 구현이 선행되어야 함
