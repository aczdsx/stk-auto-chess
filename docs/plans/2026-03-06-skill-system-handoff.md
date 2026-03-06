# Skill System Porting - Handoff Document

> **Context**: InGame_New 스킬 시스템 포팅 작업의 현재 상태와 후속 작업 정의.
> **Previous plan**: `docs/plans/2026-03-05-skill-system-porting.md` (14개 태스크, 모두 완료)
> **Branch**: `develop`

---

## 1. COMPLETED WORK

### 1.1 Architecture (data-driven skill system)

```
SkillActive spec table
  → SkillSpecAdapter.ClassifySkill()    // ID → SimSkillArchetype enum
  → SkillSpecAdapter.BuildParams()      // spec → SkillParams struct
  → SkillFactory.Initialize()           // 전체 스펙 순회, 자동 등록
  → SkillFactory.Create(skillId)        // registry lookup → SimSkillBase instance
  → SkillSystem.SetupSkills()           // 매치 시작 시 유닛별 스킬 인스턴스 생성
  → SkillSystem.TryCast()              // 마나 풀 → SelectTarget → Execute
```

### 1.2 Call chain (initialization)

```
LocalSimulationRunner.Initialize()
  → GameLoopSystem.Initialize(world, config)
    → SkillFactory.Initialize()              // ← 이 호출이 누락되어 버그 발생했었음, 수정 완료
      → RegisterCustomSkills()               // 7개 커스텀 스킬 수동 등록
      → SpecDataManager.Instance.SkillActive.All 순회
        → SkillSpecAdapter.ClassifySkill()
        → SkillSpecAdapter.BuildParams()
        → SkillFactory.Register(id, creator)
```

### 1.3 File inventory

| Path | Role |
|------|------|
| `Scripts/InGame_New/Simulation/Combat/Skills/SimSkillBase.cs` | Base class + SkillParams struct |
| `Scripts/InGame_New/Simulation/Combat/Skills/SkillFactory.cs` | Registry, Initialize(), Create(), TryGetParams() |
| `Scripts/InGame_New/Simulation/Combat/Skills/SkillHelpers.cs` | SkillDamageHelper, SkillAreaHelper, SkillCCHelper, SkillBuffHelper |
| `Scripts/InGame_New/Simulation/Combat/SkillSystem.cs` | Orchestration: SetupSkills, TryCast, TickCasting, Cleanup |
| `Scripts/InGame_New/Simulation/Combat/StatusEffectSystem.cs` | Timed buffs/debuffs/shield/DOT/HOT + RemoveDebuffs() |
| `Scripts/InGame_New/Adapter/SkillSpecAdapter.cs` | Spec → archetype classification + params builder |
| `Scripts/InGame_New/Simulation/Data/Enums.cs` | SimSkillArchetype enum (14 values) |

**Archetype classes** (13, under `Skills/`):
SimSkillSingleDamage, SimSkillAoEDamage, SimSkillLineDamage, SimSkillDamageCC, SimSkillConeDamage, SimSkillPatternDamage, SimSkillMultiHit, SimSkillHeal, SimSkillMultiTargetHeal, SimSkillTeleportStrike, SimSkillBuff, SimSkillDebuff, SimSkillStun

**Custom player skills** (7, under `Skills/Custom/`):
SimSkillYuniHeal(215252102), SimSkillMinoProjectile(217433302), SimSkillVeinBounce(217363204), SimSkillTetoraKnockback(217413301), SimSkillMenshaShield(215422301), SimSkillClayChannel(217553404), SimSkillMarieAssassin(217563405)

### 1.4 View layer changes

- `CombatViewManager.OnUnitCastSkill()` — `SkillActive.skill_vfxs[]` 기반 VFX 재생 (vfx[0]=caster, vfx[1]=target)
- `AutoChessViewBridge.DispatchEvent()` — `UnitCastSkill` 이벤트에 `targetEntityId` 전달 추가

### 1.5 Commits (oldest → newest)

```
942719f28 SkillParams 확장 - CC/버프/멀티타겟/다단히트 파라미터 추가
356d38e5a 신규 스킬 아키타입 6종 추가
7f415d898 SkillSpecAdapter + SimSkillArchetype enum 추가
1ffd77467 SkillFactory 스펙 기반 자동 등록 + SkillSystem 연동
0d8870153 커스텀 스킬 7종 + 헬퍼 확장 + View 스킬 VFX 연동
3433cdb3b SkillSpecAdapter.cs.meta 누락 파일 추가
bb4d03fbb GameLoopSystem에서 SkillFactory.Initialize() 호출 추가
```

---

## 2. KNOWN ISSUES & RISKS

### 2.1 Parameter accuracy (HIGH priority)
SkillSpecAdapter.ApplySkillSpecificParams()의 하드코딩 값들이 레거시 EffectCode 구현체와 정확히 일치하는지 미검증.
- CC 지속시간 (CCDurationFrames: 60, 90 등)
- 데미지 배율 (PowerPercent)
- 히트 수, 타겟 수
- **Action**: 레거시 EffectCode 구현체(`Scripts/InGame/EffectCode/Character/`)에서 실제 수치 추출 → SkillSpecAdapter에 반영

### 2.2 Monster skill ID mapping (MEDIUM priority)
SkillSpecAdapter.ClassifyMonsterSkill()의 몬스터 스킬 ID 매핑이 하드코딩됨.
- 새 몬스터/스테이지 추가 시 수동 업데이트 필요
- 매핑되지 않은 몬스터 스킬은 SingleDamage fallback
- **Action**: SkillActive 스펙의 필드(예: skill_target_type, skill_range_type 등)로 자동 분류 로직 개선 가능

### 2.3 VFX data dependency (LOW priority)
skill_vfxs 배열이 비어있는 스킬은 VFX 없이 발동됨.
- 기획팀 스펙 테이블에 skill_vfxs 채워야 연출 완성
- CombatViewManager는 이미 대응 완료

### 2.4 Untested edge cases
- 채널링 스킬(SimSkillClayChannel): 다중 틱에 걸친 효과 — SkillSystem.TickCasting()이 단일 Execute() 호출만 하므로, 채널링은 Execute() 내부에서 zone 상태를 CombatMatchState에 등록하고 별도 시스템에서 틱 처리해야 할 수 있음
- 바운스 스킬(SimSkillVeinBounce): Span<int> stackalloc 사용 — IL2CPP 환경에서 동작 확인 필요
- 넉백(SimSkillTetoraKnockback): 넉백 착지점 AoE 스턴이 multi-tile 유닛과 상호작용하는 경우 미검증

---

## 3. FOLLOW-UP TASKS

### Task A: 파라미터 정합성 검증 (권장 우선순위 1)
```
FOR EACH custom skill in SkillFactory.RegisterCustomSkills():
  1. Read legacy EffectCode: Scripts/InGame/EffectCode/Character/EffectCodeSkill{id}.cs
  2. Extract: damage multiplier, CC duration, hit count, target count, cooldown, special mechanics
  3. Compare with SkillSpecAdapter.ApplySkillSpecificParams() values
  4. Update mismatched values
```

### Task B: 몬스터 스킬 자동 분류 개선 (권장 우선순위 2)
현재 ClassifyMonsterSkill()이 ID switch문. SkillActive 스펙 필드 기반 자동 분류로 전환하면 유지보수성 향상.
```
Candidate fields for auto-classification:
  - skill_target_type → SingleDamage vs AoE vs Heal
  - skill_range_type → Melee vs Ranged vs Cone vs Line
  - skill_cc_type → DamageCC, Stun
  - skill_hit_count → MultiHit
  - skill_teleport → TeleportStrike
```

### Task C: 채널링 스킬 시스템 (권장 우선순위 3)
SimSkillClayChannel은 현재 Execute()에서 즉시 효과만 적용. 레거시에서는 지속 zone 효과.
필요 시 CombatMatchState에 ActiveZone[] 추가 + CombatTickSystem에서 zone 틱 처리.

### Task D: 스킬 밸런싱 툴 (선택)
에디터 윈도우에서 SkillFactory 등록 현황 조회, SkillParams 실시간 편집, 전투 시뮬레이션 결과 비교.

---

## 4. REFERENCE: Key types and their relationships

```
SkillParams (struct)
  ├── SkillId, PowerPercent, DamageType, CastFrames
  ├── Param0~3 (archetype-specific)
  ├── CCType, CCDurationFrames
  ├── BuffStat, BuffValue, BuffDurationFrames
  ├── SecondaryPowerPercent
  └── TargetCount, HitCount

SimSkillBase (abstract)
  ├── Initialize(SkillParams)     // params → protected fields
  ├── CanCast(state, caster)      // default: true
  ├── SelectTarget(state, caster) // returns CombatId
  ├── GetCastFrames()             // 0 = instant
  ├── Execute(state, caster, targetId, rng)
  └── Reset()

CombatUnit (struct, in CombatMatchState.Units[])
  ├── SkillSpecId    // set by CombatSetupSystem.FindSkillId()
  ├── CurrentMana, MaxMana
  ├── SkillCastTimer // countdown for cast time
  └── State          // CastingSkill during cast

CombatMatchState
  ├── Skills[MaxCombatUnits]  // SimSkillBase instances, 1:1 with Units[]
  └── EventQueue              // SimEvent push for view layer
```

## 5. HOW TO ADD A NEW SKILL

```
Case 1: Generic archetype (most monster skills)
  → SkillSpecAdapter.ClassifyMonsterSkill()에 ID 추가, 적절한 SimSkillArchetype 반환
  → ApplySkillSpecificParams()에 필요 시 파라미터 오버라이드 추가
  → 끝. SkillFactory.Initialize()가 자동 등록

Case 2: New archetype needed
  → SimSkillArchetype enum에 값 추가
  → SimSkill{Name}.cs 작성 (SimSkillBase 상속)
  → SkillSpecAdapter.CreateFromArchetype()에 case 추가
  → ClassifySkill/ClassifyMonsterSkill에 ID 매핑

Case 3: Unique player skill
  → Skills/Custom/SimSkill{Name}.cs 작성
  → SkillFactory.RegisterCustomSkills()에 Register(id, () => new SimSkill{Name}()) 추가
  → SkillSpecAdapter.ApplySkillSpecificParams()에 파라미터 설정
  → ClassifySkill()에 ID → SimSkillArchetype.Custom 추가
```
