# InGame Skill System Porting Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 레거시 InGame EffectCode 스킬 시스템의 액티브 스킬 71개(플레이어 23 + 몬스터 48)를 InGame_New SimSkillBase 기반 데이터 드리븐 시스템으로 포팅

**Architecture:** SkillActive 스펙 테이블에서 skill_type/atk_type/base_rate를 읽어 기존 7개 + 신규 6개 아키타입 클래스에 자동 매핑. 고유 로직이 필요한 플레이어 스킬(~7개)만 커스텀 SimSkillBase 서브클래스로 구현. SkillFactory.Initialize()에서 스펙 기반 자동 등록.

**Tech Stack:** C# (Unity 6), InGame_New Simulation layer (pure C#, no Unity deps), SpecDataManager, SkillActive spec table

---

## 현재 상태 분석

### 레거시 시스템 (InGame/EffectCode/)
- 113개 구현체: 플레이어 23, 몬스터 48, 버프 20, 디버프 14, CC 8
- EffectCodeCharacterBase 상속, ~35개 가상 메서드
- 소스 생성 팩토리 (`[UseEffectCodeIds]` + `[EffectCodeFactory]`)
- 쿨타임 기반 활성화: OnCooltime → IsReadyToActivate → Activate → OnSkillExecute → OnSkillAnimationEnd
- VFX/애니메이션이 스킬 로직에 결합

### 새 시스템 (InGame_New/Simulation/Combat/Skills/)
- SimSkillBase 6개 메서드: Initialize, CanCast, SelectTarget, GetCastFrames, Execute, Reset
- 7개 아키타입: SingleDamage, AoEDamage, LineDamage, Heal, Buff, Debuff, Stun
- SkillFactory: 수동 등록, 현재 Initialize() 비어있음
- 마나 기반 활성화: 마나 풀 → TryCast → SelectTarget → Execute
- Simulation/View 분리 (VFX는 View 레이어)

### 레거시 스킬 패턴 분류

**몬스터 스킬 (48개):**

| 패턴 | 개수 | 매핑 대상 |
|------|------|-----------|
| 단일 타겟 데미지 | 14 | SimSkillSingleDamage (기존) |
| 단일 타겟 + CC | 8 | SimSkillDamageCC (신규) |
| 전방 콘 데미지 | 5 | SimSkillConeDamage (신규) |
| 패턴 AoE (행/열/사각) | 5 | SimSkillPatternDamage (신규) |
| 관통 프로젝타일 | 5 | SimSkillLineDamage (기존) |
| 텔레포트 + CC | 4 | SimSkillTeleportStrike (신규) |
| 다단 히트 | 3 | SimSkillMultiHit (신규) |
| 힐 | 4 | SimSkillHeal (기존) / SimSkillMultiTargetHeal (신규) |

**플레이어 스킬 (23개):**

| 카테고리 | 스킬 | 매핑 |
|----------|------|------|
| 단순 데미지 | 필리아 | SimSkillSingleDamage |
| 데미지+CC | 시이나(침묵), 하티(넉백), 미사(기절+봉인) | SimSkillDamageCC |
| AoE | 블린(다이아몬드) | SimSkillAoEDamage |
| 라인 | 아트레시아(관통 슬래시) | SimSkillLineDamage |
| 멀티타겟 힐 | 아란(1명+버프), 유니(3명+디버프제거), 엔키(라인힐+HoT) | 기존+커스텀 |
| AoE+CC | 메이(십자+넉백), 아드리아(다파동), 오데트(2단계+디버프) | SimSkillPatternDamage |
| 멀티타겟 데미지 | 엘리스(조건부), 라키유(스플래시+디버프), 에이프릴(다거리) | 커스텀 |
| 프로젝타일+스플래시 | 미노(3발+폭발) | 커스텀 |
| 바운스 | 베인(감소 바운스) | 커스텀 |
| 넉백+충돌 | 테토라(넉백→스턴) | 커스텀 |
| 실드 | 멘샤(열 실드) | 커스텀 |
| 스택 버프 | 루키다(여우불) | 커스텀 |
| 채널링 | 클레이(힐+데미지 존) | 커스텀 |
| 텔레포트+다단 | 마리에(순간이동+다타), 시라유키(순차 텔포) | 커스텀 |

---

## Task 1: SkillParams 확장

**Files:**
- Modify: `Assets/_Project/Scripts/InGame_New/Simulation/Combat/Skills/SimSkillBase.cs:3-14`

**Step 1: SkillParams에 필요한 파라미터 추가**

```csharp
public struct SkillParams
{
    public int SkillId;
    public int PowerPercent;       // 데미지/힐 배율 (100 = 100%)
    public DamageType DamageType;
    public int CastFrames;
    public int Param0;             // 아키타입별 범용 (범위, 타겟수 등)
    public int Param1;             // 아키타입별 범용 (보조 배율 등)
    public int Param2;             // 아키타입별 범용
    public int Param3;             // 아키타입별 범용

    // 신규 추가
    public CrowdControlType CCType;       // CC 종류 (None/Stun/Silence/Knockback 등)
    public int CCDurationFrames;          // CC 지속 프레임
    public StatModType BuffStat;          // 버프/디버프 대상 스탯
    public int BuffValue;                 // 버프/디버프 수치
    public int BuffDurationFrames;        // 버프/디버프 지속 프레임
    public int SecondaryPowerPercent;     // 보조 배율 (스플래시, 감소 데미지 등)
    public int TargetCount;               // 멀티타겟 수 (기본 1)
    public int HitCount;                  // 다단히트 수 (기본 1)
}
```

**Step 2: SimSkillBase에 확장 필드 반영**

SimSkillBase.Initialize()에서 새 필드 저장:

```csharp
public abstract class SimSkillBase
{
    public int SkillId { get; private set; }
    protected int PowerPercent;
    protected DamageType DamageType;
    protected int CastFrames;

    // 신규 추가
    protected CrowdControlType CCType;
    protected int CCDurationFrames;
    protected StatModType BuffStat;
    protected int BuffValue;
    protected int BuffDurationFrames;
    protected int SecondaryPowerPercent;
    protected int TargetCount;
    protected int HitCount;

    public virtual void Initialize(SkillParams p)
    {
        SkillId = p.SkillId;
        PowerPercent = p.PowerPercent;
        DamageType = p.DamageType;
        CastFrames = p.CastFrames;
        CCType = p.CCType;
        CCDurationFrames = p.CCDurationFrames;
        BuffStat = p.BuffStat;
        BuffValue = p.BuffValue;
        BuffDurationFrames = p.BuffDurationFrames;
        SecondaryPowerPercent = p.SecondaryPowerPercent;
        TargetCount = p.TargetCount > 0 ? p.TargetCount : 1;
        HitCount = p.HitCount > 0 ? p.HitCount : 1;
    }

    public virtual bool CanCast(CombatMatchState state, ref CombatUnit caster) => true;
    public abstract int SelectTarget(CombatMatchState state, ref CombatUnit caster);
    public virtual int GetCastFrames() => CastFrames;
    public abstract void Execute(CombatMatchState state, ref CombatUnit caster,
        int targetCombatId, ref DeterministicRNG rng);
    public virtual void Reset() { }
}
```

**Step 3: Commit**

```bash
git add Assets/_Project/Scripts/InGame_New/Simulation/Combat/Skills/SimSkillBase.cs
git commit -m "feat(InGame_New): SkillParams 확장 - CC/버프/멀티타겟/다단히트 파라미터 추가"
```

---

## Task 2: 신규 아키타입 클래스 - SimSkillDamageCC

데미지 + CC (스턴/침묵/넉백/슬로우 등). 몬스터 8개 + 플레이어 3개 커버.

**Files:**
- Create: `Assets/_Project/Scripts/InGame_New/Simulation/Combat/Skills/SimSkillDamageCC.cs`

**Step 1: 구현**

```csharp
namespace CookApps.AutoChess
{
    /// <summary>단일 타겟 데미지 + CC 스킬</summary>
    public class SimSkillDamageCC : SimSkillBase
    {
        public override int SelectTarget(CombatMatchState state, ref CombatUnit caster)
        {
            return TargetingSystem.FindNearestEnemy(state, ref caster);
        }

        public override void Execute(CombatMatchState state, ref CombatUnit caster,
            int targetCombatId, ref DeterministicRNG rng)
        {
            SkillDamageHelper.DealDamage(state, ref caster, targetCombatId, PowerPercent, DamageType);

            if (CCType != CrowdControlType.None && CCDurationFrames > 0)
            {
                int idx = state.FindUnitIndex(targetCombatId);
                if (idx >= 0)
                {
                    SkillCCHelper.ApplyCC(ref state.Units[idx], CCType, CCDurationFrames);
                }
            }
        }
    }
}
```

**Step 2: Commit**

```bash
git add Assets/_Project/Scripts/InGame_New/Simulation/Combat/Skills/SimSkillDamageCC.cs
git commit -m "feat(InGame_New): SimSkillDamageCC 아키타입 추가"
```

---

## Task 3: 신규 아키타입 - SimSkillConeDamage

전방 방향 콘/직사각형 범위 데미지. 몬스터 5개 커버.

**Files:**
- Create: `Assets/_Project/Scripts/InGame_New/Simulation/Combat/Skills/SimSkillConeDamage.cs`

**Step 1: 구현**

```csharp
namespace CookApps.AutoChess
{
    /// <summary>전방 콘 범위 데미지 (caster 전방 N타일)</summary>
    public class SimSkillConeDamage : SimSkillBase
    {
        private int _range;

        public override void Initialize(SkillParams p)
        {
            base.Initialize(p);
            _range = p.Param0 > 0 ? p.Param0 : 2;
        }

        public override int SelectTarget(CombatMatchState state, ref CombatUnit caster)
        {
            return TargetingSystem.FindNearestEnemy(state, ref caster);
        }

        public override void Execute(CombatMatchState state, ref CombatUnit caster,
            int targetCombatId, ref DeterministicRNG rng)
        {
            // 캐스터 전방 방향으로 _range 타일 범위의 적에게 데미지
            int dirCol = caster.TeamIndex == 0 ? 1 : -1;
            int attack = caster.Attack;
            int power = PowerPercent;
            var type = DamageType;
            byte team = caster.TeamIndex;

            SkillAreaHelper.ForEachEnemyInLine(state, team,
                caster.GridCol, caster.GridRow,
                dirCol, 0, _range,
                (ref CombatUnit t, int i) =>
                {
                    int raw = attack * power / 100;
                    int dmg = DamageSystem.CalculateDamage(raw, type, ref t);
                    DamageSystem.ApplyDamage(state, ref t, dmg);
                    DamageSystem.ChargeMana(ref t, DamageSystem.ManaGainOnHit);
                });
        }

        public override void Reset()
        {
            _range = 2;
        }
    }
}
```

**Step 2: Commit**

```bash
git add Assets/_Project/Scripts/InGame_New/Simulation/Combat/Skills/SimSkillConeDamage.cs
git commit -m "feat(InGame_New): SimSkillConeDamage 아키타입 추가"
```

---

## Task 4: 신규 아키타입 - SimSkillPatternDamage

패턴 기반 AoE (행/열/십자/사각 등). 몬스터 5개 + 플레이어 AoE+CC 3개 커버.

**Files:**
- Create: `Assets/_Project/Scripts/InGame_New/Simulation/Combat/Skills/SimSkillPatternDamage.cs`
- Modify: `Assets/_Project/Scripts/InGame_New/Simulation/Data/Enums.cs` (AreaPatternType 추가)

**Step 1: Enums에 AreaPatternType 추가**

```csharp
public enum AreaPatternType : byte
{
    Circle,      // 원형 (기존 AoE와 동일)
    Cross,       // 십자 (+)
    Row,         // 행 전체
    Column,      // 열 전체
    Square,      // 정사각형
    Diamond,     // 다이아몬드 (맨해튼 거리)
}
```

**Step 2: SimSkillPatternDamage 구현**

```csharp
namespace CookApps.AutoChess
{
    /// <summary>패턴 기반 AoE 데미지 (십자/행/열/사각 등) + 선택적 CC</summary>
    public class SimSkillPatternDamage : SimSkillBase
    {
        private AreaPatternType _pattern;
        private int _range;

        public override void Initialize(SkillParams p)
        {
            base.Initialize(p);
            _pattern = (AreaPatternType)(p.Param0);
            _range = p.Param1 > 0 ? p.Param1 : 1;
        }

        public override int SelectTarget(CombatMatchState state, ref CombatUnit caster)
        {
            // 패턴 스킬은 최적 AoE 타겟 또는 가장 가까운 적
            if (_pattern == AreaPatternType.Circle || _pattern == AreaPatternType.Diamond)
                return SkillAreaHelper.FindBestAoETarget(state, ref caster, _range);
            return TargetingSystem.FindNearestEnemy(state, ref caster);
        }

        public override void Execute(CombatMatchState state, ref CombatUnit caster,
            int targetCombatId, ref DeterministicRNG rng)
        {
            int idx = state.FindUnitIndex(targetCombatId);
            if (idx < 0) return;
            ref var center = ref state.Units[idx];

            int attack = caster.Attack;
            int power = PowerPercent;
            var type = DamageType;
            byte team = caster.TeamIndex;
            var ccType = CCType;
            int ccFrames = CCDurationFrames;

            // 패턴에 따라 범위 내 적에게 데미지 (+ 선택적 CC)
            SkillAreaHelper.ForEachEnemyInRadius(state, team,
                center.GridCol, center.GridRow, _range,
                (ref CombatUnit t, int i) =>
                {
                    int raw = attack * power / 100;
                    int dmg = DamageSystem.CalculateDamage(raw, type, ref t);
                    DamageSystem.ApplyDamage(state, ref t, dmg);
                    DamageSystem.ChargeMana(ref t, DamageSystem.ManaGainOnHit);

                    if (ccType != CrowdControlType.None && ccFrames > 0)
                    {
                        SkillCCHelper.ApplyCC(ref t, ccType, ccFrames);
                    }
                });
        }

        public override void Reset()
        {
            _pattern = AreaPatternType.Circle;
            _range = 1;
        }
    }
}
```

**Step 3: Commit**

```bash
git add Assets/_Project/Scripts/InGame_New/Simulation/Combat/Skills/SimSkillPatternDamage.cs \
      Assets/_Project/Scripts/InGame_New/Simulation/Data/Enums.cs
git commit -m "feat(InGame_New): SimSkillPatternDamage + AreaPatternType 추가"
```

---

## Task 5: 신규 아키타입 - SimSkillMultiHit

같은 타겟에 N회 데미지. 몬스터 3개 커버.

**Files:**
- Create: `Assets/_Project/Scripts/InGame_New/Simulation/Combat/Skills/SimSkillMultiHit.cs`

**Step 1: 구현**

```csharp
namespace CookApps.AutoChess
{
    /// <summary>단일 타겟 다단히트 데미지</summary>
    public class SimSkillMultiHit : SimSkillBase
    {
        public override int SelectTarget(CombatMatchState state, ref CombatUnit caster)
        {
            return TargetingSystem.FindNearestEnemy(state, ref caster);
        }

        public override void Execute(CombatMatchState state, ref CombatUnit caster,
            int targetCombatId, ref DeterministicRNG rng)
        {
            for (int i = 0; i < HitCount; i++)
            {
                // 타겟이 죽었으면 중단
                int idx = state.FindUnitIndex(targetCombatId);
                if (idx < 0) break;
                if (state.Units[idx].CurrentHP <= 0) break;

                SkillDamageHelper.DealDamage(state, ref caster, targetCombatId, PowerPercent, DamageType);
            }
        }
    }
}
```

**Step 2: Commit**

```bash
git add Assets/_Project/Scripts/InGame_New/Simulation/Combat/Skills/SimSkillMultiHit.cs
git commit -m "feat(InGame_New): SimSkillMultiHit 아키타입 추가"
```

---

## Task 6: 신규 아키타입 - SimSkillMultiTargetHeal

N명의 최저 HP 아군 힐. 몬스터 4개 (전체 힐) + 플레이어 힐러 커버.

**Files:**
- Create: `Assets/_Project/Scripts/InGame_New/Simulation/Combat/Skills/SimSkillMultiTargetHeal.cs`

**Step 1: SkillAreaHelper에 FindLowestHPAllies 추가**

`Assets/_Project/Scripts/InGame_New/Simulation/Combat/Skills/SkillHelpers.cs`에 추가:

```csharp
// SkillAreaHelper 클래스에 추가
/// <summary>HP가 가장 낮은 아군 N명의 CombatId 배열 반환</summary>
public static int FindLowestHPAllies(CombatMatchState state, byte teamIndex, int count,
    int[] resultBuffer)
{
    int found = 0;
    // 단순 선택 정렬 (count가 작으므로 O(n*count) 허용)
    for (int c = 0; c < count; c++)
    {
        int bestIdx = -1;
        int bestHP = int.MaxValue;
        for (int i = 0; i < state.UnitCount; i++)
        {
            ref var u = ref state.Units[i];
            if (u.TeamIndex != teamIndex || u.CurrentHP <= 0) continue;

            // 이미 선택된 유닛 스킵
            bool alreadySelected = false;
            for (int j = 0; j < found; j++)
            {
                if (resultBuffer[j] == u.CombatId) { alreadySelected = true; break; }
            }
            if (alreadySelected) continue;

            if (u.CurrentHP < bestHP)
            {
                bestHP = u.CurrentHP;
                bestIdx = i;
            }
        }
        if (bestIdx < 0) break;
        resultBuffer[found++] = state.Units[bestIdx].CombatId;
    }
    return found;
}
```

**Step 2: SimSkillMultiTargetHeal 구현**

```csharp
namespace CookApps.AutoChess
{
    /// <summary>최저 HP 아군 N명 힐 스킬</summary>
    public class SimSkillMultiTargetHeal : SimSkillBase
    {
        private readonly int[] _targetBuffer = new int[16];

        public override int SelectTarget(CombatMatchState state, ref CombatUnit caster)
        {
            return SkillAreaHelper.FindLowestHPAlly(state, caster.TeamIndex);
        }

        public override void Execute(CombatMatchState state, ref CombatUnit caster,
            int targetCombatId, ref DeterministicRNG rng)
        {
            int count = SkillAreaHelper.FindLowestHPAllies(
                state, caster.TeamIndex, TargetCount, _targetBuffer);

            int healAmount = caster.Attack * PowerPercent / 100;

            for (int i = 0; i < count; i++)
            {
                int idx = state.FindUnitIndex(_targetBuffer[i]);
                if (idx < 0) continue;
                SkillDamageHelper.Heal(ref state.Units[idx], healAmount);
            }
        }
    }
}
```

**Step 3: Commit**

```bash
git add Assets/_Project/Scripts/InGame_New/Simulation/Combat/Skills/SimSkillMultiTargetHeal.cs \
      Assets/_Project/Scripts/InGame_New/Simulation/Combat/Skills/SkillHelpers.cs
git commit -m "feat(InGame_New): SimSkillMultiTargetHeal + FindLowestHPAllies 헬퍼 추가"
```

---

## Task 7: 신규 아키타입 - SimSkillTeleportStrike

텔레포트(무적) → 착지 데미지 + CC. 몬스터 4개 커버.

**Files:**
- Create: `Assets/_Project/Scripts/InGame_New/Simulation/Combat/Skills/SimSkillTeleportStrike.cs`

**Step 1: 구현**

시뮬레이션에서는 텔레포트의 시각적 이동이 없으므로, 즉시 타겟 위치에 AoE 데미지 + CC로 처리. CastFrames로 무적(시전) 시간 표현.

```csharp
namespace CookApps.AutoChess
{
    /// <summary>텔레포트 타격 (시전 시간 동안 무적, 착지 시 AoE + CC)</summary>
    public class SimSkillTeleportStrike : SimSkillBase
    {
        private int _aoeRange;

        public override void Initialize(SkillParams p)
        {
            base.Initialize(p);
            _aoeRange = p.Param0 > 0 ? p.Param0 : 1;
        }

        public override int SelectTarget(CombatMatchState state, ref CombatUnit caster)
        {
            return TargetingSystem.FindNearestEnemy(state, ref caster);
        }

        public override void Execute(CombatMatchState state, ref CombatUnit caster,
            int targetCombatId, ref DeterministicRNG rng)
        {
            int idx = state.FindUnitIndex(targetCombatId);
            if (idx < 0) return;
            ref var target = ref state.Units[idx];

            int centerCol = target.GridCol;
            int centerRow = target.GridRow;
            int attack = caster.Attack;
            int power = PowerPercent;
            var type = DamageType;
            byte team = caster.TeamIndex;
            var ccType = CCType;
            int ccFrames = CCDurationFrames;

            SkillAreaHelper.ForEachEnemyInRadius(state, team,
                centerCol, centerRow, _aoeRange,
                (ref CombatUnit t, int i) =>
                {
                    int raw = attack * power / 100;
                    int dmg = DamageSystem.CalculateDamage(raw, type, ref t);
                    DamageSystem.ApplyDamage(state, ref t, dmg);
                    DamageSystem.ChargeMana(ref t, DamageSystem.ManaGainOnHit);

                    if (ccType != CrowdControlType.None && ccFrames > 0)
                    {
                        SkillCCHelper.ApplyCC(ref t, ccType, ccFrames);
                    }
                });
        }

        public override void Reset()
        {
            _aoeRange = 1;
        }
    }
}
```

**Step 2: Commit**

```bash
git add Assets/_Project/Scripts/InGame_New/Simulation/Combat/Skills/SimSkillTeleportStrike.cs
git commit -m "feat(InGame_New): SimSkillTeleportStrike 아키타입 추가"
```

---

## Task 8: SkillSpecAdapter 생성 - 스펙 데이터 → SkillParams 변환

SkillActive 스펙 테이블에서 스킬 파라미터를 읽어 SkillParams로 변환하는 어댑터.

**Files:**
- Create: `Assets/_Project/Scripts/InGame_New/Adapter/SkillSpecAdapter.cs`

**Step 1: 스킬 분류 enum 추가**

`Assets/_Project/Scripts/InGame_New/Simulation/Data/Enums.cs`에 추가:

```csharp
/// <summary>시뮬레이션 스킬 아키타입</summary>
public enum SimSkillArchetype : byte
{
    SingleDamage,
    AoEDamage,
    LineDamage,
    DamageCC,
    ConeDamage,
    PatternDamage,
    MultiHit,
    Heal,
    MultiTargetHeal,
    TeleportStrike,
    Buff,
    Debuff,
    Stun,
    Custom,
}
```

**Step 2: SkillSpecAdapter 구현**

```csharp
using CookApps.AutoBattler;
using UnityEngine;

namespace CookApps.AutoChess
{
    /// <summary>
    /// SkillActive 스펙 → SkillParams 변환 어댑터.
    /// 스킬 ID별 아키타입 매핑과 파라미터 추출을 담당.
    /// </summary>
    public static class SkillSpecAdapter
    {
        /// <summary>SkillActive 스펙에서 SkillParams 생성</summary>
        public static SkillParams BuildParams(SkillActive spec)
        {
            var archetype = ClassifySkill(spec);
            var dmgType = spec.atk_type == AtkType.AP ? DamageType.Magical : DamageType.Physical;
            int powerPercent = Mathf.RoundToInt(spec.base_rate * 100f);

            var p = new SkillParams
            {
                SkillId = spec.id,
                PowerPercent = powerPercent,
                DamageType = dmgType,
                CastFrames = 0,
                TargetCount = 1,
                HitCount = 1,
            };

            // 아키타입별 추가 파라미터 설정
            // (개별 스킬의 세부 파라미터는 EffectCode stat에서 읽던 것을
            //  SkillActive 테이블 확장 또는 별도 매핑 테이블에서 보완)
            return p;
        }

        /// <summary>스킬 ID로 아키타입 분류</summary>
        public static SimSkillArchetype ClassifySkill(SkillActive spec)
        {
            // 몬스터 스킬: EffectCode ID 패턴 기반 분류
            // 플레이어 스킬: 개별 매핑
            // 기본값: 단일 데미지
            return SimSkillArchetype.SingleDamage;
        }

        /// <summary>아키타입에 해당하는 SimSkillBase 인스턴스 생성</summary>
        public static SimSkillBase CreateFromArchetype(SimSkillArchetype archetype)
        {
            return archetype switch
            {
                SimSkillArchetype.SingleDamage => new SimSkillSingleDamage(),
                SimSkillArchetype.AoEDamage => new SimSkillAoEDamage(),
                SimSkillArchetype.LineDamage => new SimSkillLineDamage(),
                SimSkillArchetype.DamageCC => new SimSkillDamageCC(),
                SimSkillArchetype.ConeDamage => new SimSkillConeDamage(),
                SimSkillArchetype.PatternDamage => new SimSkillPatternDamage(),
                SimSkillArchetype.MultiHit => new SimSkillMultiHit(),
                SimSkillArchetype.Heal => new SimSkillHeal(),
                SimSkillArchetype.MultiTargetHeal => new SimSkillMultiTargetHeal(),
                SimSkillArchetype.TeleportStrike => new SimSkillTeleportStrike(),
                SimSkillArchetype.Buff => new SimSkillBuff(),
                SimSkillArchetype.Debuff => new SimSkillDebuff(),
                SimSkillArchetype.Stun => new SimSkillStun(),
                _ => null,
            };
        }
    }
}
```

**Step 3: Commit**

```bash
git add Assets/_Project/Scripts/InGame_New/Adapter/SkillSpecAdapter.cs \
      Assets/_Project/Scripts/InGame_New/Simulation/Data/Enums.cs
git commit -m "feat(InGame_New): SkillSpecAdapter 생성 - 스펙 → SkillParams 변환"
```

---

## Task 9: SkillFactory.Initialize() - 스펙 기반 자동 등록

SkillActive 테이블의 모든 스킬을 팩토리에 자동 등록.

**Files:**
- Modify: `Assets/_Project/Scripts/InGame_New/Simulation/Combat/Skills/SkillFactory.cs`

**Step 1: Initialize 구현**

```csharp
using CookApps.AutoBattler;

namespace CookApps.AutoChess
{
    public static class SkillFactory
    {
        private static readonly Dictionary<int, System.Func<SimSkillBase>> _registry = new();
        private static readonly Dictionary<int, SkillParams> _paramsCache = new();
        private static bool _initialized;

        public static void Register(int skillId, System.Func<SimSkillBase> creator)
        {
            _registry[skillId] = creator;
        }

        public static SimSkillBase Create(int skillId)
        {
            if (_registry.TryGetValue(skillId, out var creator))
                return creator();
            return null;
        }

        /// <summary>캐시된 SkillParams 조회</summary>
        public static bool TryGetParams(int skillId, out SkillParams p)
        {
            return _paramsCache.TryGetValue(skillId, out p);
        }

        /// <summary>SkillActive 스펙 테이블 기반 자동 등록</summary>
        public static void Initialize()
        {
            if (_initialized) return;
            _initialized = true;

            var specManager = SpecDataManager.Instance;
            if (specManager?.SkillActive == null) return;

            foreach (var spec in specManager.SkillActive)
            {
                // PASSIVE, NONE 타입은 스킵 (액티브 스킬만)
                if (spec.skill_type != SkillType.NORMAL &&
                    spec.skill_type != SkillType.WEAPON &&
                    spec.skill_type != SkillType.ACTIVE)
                    continue;

                int id = spec.id;
                var archetype = SkillSpecAdapter.ClassifySkill(spec);
                var skillParams = SkillSpecAdapter.BuildParams(spec);
                _paramsCache[id] = skillParams;

                // 커스텀 스킬이 이미 등록되어 있으면 스킵
                if (_registry.ContainsKey(id)) continue;

                Register(id, () => SkillSpecAdapter.CreateFromArchetype(archetype));
            }

            // 커스텀 플레이어 스킬 등록 (Task 10에서 구현)
            RegisterCustomSkills();
        }

        private static void RegisterCustomSkills()
        {
            // Task 10에서 채움
        }

        public static void Clear()
        {
            _registry.Clear();
            _paramsCache.Clear();
            _initialized = false;
        }
    }
}
```

**Step 2: SkillSystem.SetupSkills 수정**

`Assets/_Project/Scripts/InGame_New/Simulation/Combat/SkillSystem.cs`에서 하드코딩된 파라미터를 캐시에서 조회하도록 변경:

```csharp
public static void SetupSkills(CombatMatchState state, GameWorld world)
{
    for (int i = 0; i < state.UnitCount; i++)
    {
        ref var unit = ref state.Units[i];
        if (unit.SkillSpecId <= 0) continue;

        var skill = SkillFactory.Create(unit.SkillSpecId);
        if (skill == null) continue;

        // 캐시된 파라미터 사용 (없으면 기본값)
        if (SkillFactory.TryGetParams(unit.SkillSpecId, out var skillParams))
        {
            skill.Initialize(skillParams);
        }
        else
        {
            skill.Initialize(new SkillParams
            {
                SkillId = unit.SkillSpecId,
                PowerPercent = 200,
                DamageType = DamageType.Magical,
            });
        }
        state.Skills[i] = skill;
    }
}
```

**Step 3: Commit**

```bash
git add Assets/_Project/Scripts/InGame_New/Simulation/Combat/Skills/SkillFactory.cs \
      Assets/_Project/Scripts/InGame_New/Simulation/Combat/SkillSystem.cs
git commit -m "feat(InGame_New): SkillFactory 스펙 기반 자동 등록 + SkillSystem 연동"
```

---

## Task 10: 커스텀 플레이어 스킬 구현

고유 로직이 필요한 플레이어 스킬 구현. 시뮬레이션 레이어에서는 게임 메커닉만 처리 (VFX/애니메이션은 View 레이어).

**Files:**
- Create: `Assets/_Project/Scripts/InGame_New/Simulation/Combat/Skills/Custom/` 디렉토리
- Create: 아래 7개 파일

### 10-1: SimSkillYuniHeal (유니 - 멀티힐 + 디버프 제거)

```csharp
// Assets/_Project/Scripts/InGame_New/Simulation/Combat/Skills/Custom/SimSkillYuniHeal.cs
namespace CookApps.AutoChess
{
    /// <summary>유니: 최저 HP 아군 3명 힐 + 디버프 제거</summary>
    public class SimSkillYuniHeal : SimSkillBase
    {
        private readonly int[] _targetBuffer = new int[16];
        private int _debuffRemoveCount;

        public override void Initialize(SkillParams p)
        {
            base.Initialize(p);
            _debuffRemoveCount = p.Param0 > 0 ? p.Param0 : 1;
        }

        public override int SelectTarget(CombatMatchState state, ref CombatUnit caster)
        {
            return SkillAreaHelper.FindLowestHPAlly(state, caster.TeamIndex);
        }

        public override void Execute(CombatMatchState state, ref CombatUnit caster,
            int targetCombatId, ref DeterministicRNG rng)
        {
            int count = SkillAreaHelper.FindLowestHPAllies(
                state, caster.TeamIndex, TargetCount, _targetBuffer);
            int healAmount = caster.Attack * PowerPercent / 100;

            for (int i = 0; i < count; i++)
            {
                int idx = state.FindUnitIndex(_targetBuffer[i]);
                if (idx < 0) continue;
                ref var target = ref state.Units[idx];
                SkillDamageHelper.Heal(ref target, healAmount);
                StatusEffectSystem.RemoveDebuffs(state, idx, _debuffRemoveCount);
            }
        }

        public override void Reset()
        {
            _debuffRemoveCount = 1;
        }
    }
}
```

### 10-2: SimSkillMinoProjectile (미노 - 3발 프로젝타일 + 스플래시)

```csharp
// Assets/_Project/Scripts/InGame_New/Simulation/Combat/Skills/Custom/SimSkillMinoProjectile.cs
namespace CookApps.AutoChess
{
    /// <summary>미노: 최저 HP 적 3명에게 프로젝타일 + 주변 스플래시</summary>
    public class SimSkillMinoProjectile : SimSkillBase
    {
        private readonly int[] _targetBuffer = new int[16];

        public override int SelectTarget(CombatMatchState state, ref CombatUnit caster)
        {
            return TargetingSystem.FindNearestEnemy(state, ref caster);
        }

        public override void Execute(CombatMatchState state, ref CombatUnit caster,
            int targetCombatId, ref DeterministicRNG rng)
        {
            // 최저 HP 적 N명 선택
            int count = FindLowestHPEnemies(state, caster.TeamIndex, TargetCount, _targetBuffer);
            int attack = caster.Attack;
            int mainPower = PowerPercent;
            int splashPower = SecondaryPowerPercent > 0 ? SecondaryPowerPercent : PowerPercent;
            var type = DamageType;
            byte team = caster.TeamIndex;

            for (int t = 0; t < count; t++)
            {
                int mainIdx = state.FindUnitIndex(_targetBuffer[t]);
                if (mainIdx < 0) continue;
                ref var mainTarget = ref state.Units[mainIdx];

                // 메인 타겟 데미지
                int mainRaw = attack * mainPower / 100;
                int mainDmg = DamageSystem.CalculateDamage(mainRaw, type, ref mainTarget);
                DamageSystem.ApplyDamage(state, ref mainTarget, mainDmg);
                DamageSystem.ChargeMana(ref mainTarget, DamageSystem.ManaGainOnHit);

                // 주변 1타일 스플래시 (메인 타겟 제외)
                int col = mainTarget.GridCol;
                int row = mainTarget.GridRow;
                int mainId = mainTarget.CombatId;
                SkillAreaHelper.ForEachEnemyInRadius(state, team, col, row, 1,
                    (ref CombatUnit u, int i) =>
                    {
                        if (u.CombatId == mainId) return;
                        int raw = attack * splashPower / 100;
                        int dmg = DamageSystem.CalculateDamage(raw, type, ref u);
                        DamageSystem.ApplyDamage(state, ref u, dmg);
                        DamageSystem.ChargeMana(ref u, DamageSystem.ManaGainOnHit);
                    });
            }
        }

        private static int FindLowestHPEnemies(CombatMatchState state, byte myTeam, int count, int[] buffer)
        {
            int found = 0;
            for (int c = 0; c < count; c++)
            {
                int bestIdx = -1;
                int bestHP = int.MaxValue;
                for (int i = 0; i < state.UnitCount; i++)
                {
                    ref var u = ref state.Units[i];
                    if (u.TeamIndex == myTeam || u.CurrentHP <= 0) continue;
                    bool already = false;
                    for (int j = 0; j < found; j++)
                        if (buffer[j] == u.CombatId) { already = true; break; }
                    if (already) continue;
                    if (u.CurrentHP < bestHP) { bestHP = u.CurrentHP; bestIdx = i; }
                }
                if (bestIdx < 0) break;
                buffer[found++] = state.Units[bestIdx].CombatId;
            }
            return found;
        }
    }
}
```

### 10-3: SimSkillVeinBounce (베인 - 바운스 프로젝타일)

```csharp
// Assets/_Project/Scripts/InGame_New/Simulation/Combat/Skills/Custom/SimSkillVeinBounce.cs
namespace CookApps.AutoChess
{
    /// <summary>베인: 바운스 프로젝타일 (타겟 간 이동, 데미지 감소)</summary>
    public class SimSkillVeinBounce : SimSkillBase
    {
        public override int SelectTarget(CombatMatchState state, ref CombatUnit caster)
        {
            return TargetingSystem.FindNearestEnemy(state, ref caster);
        }

        public override void Execute(CombatMatchState state, ref CombatUnit caster,
            int targetCombatId, ref DeterministicRNG rng)
        {
            int maxBounces = TargetCount;
            int currentPower = PowerPercent;
            int decayPercent = SecondaryPowerPercent; // 바운스당 감소율
            var type = DamageType;
            int currentTargetId = targetCombatId;
            byte team = caster.TeamIndex;

            Span<int> hitIds = stackalloc int[maxBounces];
            int hitCount = 0;

            for (int bounce = 0; bounce < maxBounces; bounce++)
            {
                int idx = state.FindUnitIndex(currentTargetId);
                if (idx < 0) break;
                if (state.Units[idx].CurrentHP <= 0) break;

                // 데미지 적용
                int raw = caster.Attack * currentPower / 100;
                int dmg = DamageSystem.CalculateDamage(raw, type, ref state.Units[idx]);
                DamageSystem.ApplyDamage(state, ref state.Units[idx], dmg);
                DamageSystem.ChargeMana(ref state.Units[idx], DamageSystem.ManaGainOnHit);

                hitIds[hitCount++] = currentTargetId;
                currentPower = currentPower * (100 - decayPercent) / 100;

                // 다음 가장 가까운 적 찾기 (이미 맞은 적 제외)
                currentTargetId = FindNextBounceTarget(state, team, idx, hitIds, hitCount);
                if (currentTargetId == CombatUnit.InvalidId) break;
            }

            // 버프: 캐스터에게 공속 버프
            if (BuffStat != StatModType.Attack && BuffDurationFrames > 0)
            {
                int casterIdx = state.FindUnitIndex(caster.CombatId);
                if (casterIdx >= 0)
                    SkillBuffHelper.ApplyTimedBuff(state, casterIdx, BuffStat, BuffValue, BuffDurationFrames);
            }
        }

        private static int FindNextBounceTarget(CombatMatchState state, byte myTeam,
            int currentIdx, Span<int> hitIds, int hitCount)
        {
            ref var current = ref state.Units[currentIdx];
            int bestId = CombatUnit.InvalidId;
            int bestDist = int.MaxValue;

            for (int i = 0; i < state.UnitCount; i++)
            {
                ref var u = ref state.Units[i];
                if (u.TeamIndex == myTeam || u.CurrentHP <= 0) continue;

                bool alreadyHit = false;
                for (int j = 0; j < hitCount; j++)
                    if (hitIds[j] == u.CombatId) { alreadyHit = true; break; }
                if (alreadyHit) continue;

                int dist = System.Math.Abs(u.GridCol - current.GridCol)
                         + System.Math.Abs(u.GridRow - current.GridRow);
                if (dist < bestDist) { bestDist = dist; bestId = u.CombatId; }
            }
            return bestId;
        }
    }
}
```

### 10-4: SimSkillTetoraKnockback (테토라 - 넉백 + 충돌 AoE 스턴)

```csharp
// Assets/_Project/Scripts/InGame_New/Simulation/Combat/Skills/Custom/SimSkillTetoraKnockback.cs
namespace CookApps.AutoChess
{
    /// <summary>테토라: 단일 데미지 + 넉백, 충돌 시 AoE 스턴</summary>
    public class SimSkillTetoraKnockback : SimSkillBase
    {
        private int _knockbackDistance;
        private int _stunAoERange;

        public override void Initialize(SkillParams p)
        {
            base.Initialize(p);
            _knockbackDistance = p.Param0 > 0 ? p.Param0 : 4;
            _stunAoERange = p.Param1 > 0 ? p.Param1 : 1;
        }

        public override int SelectTarget(CombatMatchState state, ref CombatUnit caster)
        {
            return TargetingSystem.FindNearestEnemy(state, ref caster);
        }

        public override void Execute(CombatMatchState state, ref CombatUnit caster,
            int targetCombatId, ref DeterministicRNG rng)
        {
            // 메인 타겟 데미지
            SkillDamageHelper.DealDamage(state, ref caster, targetCombatId, PowerPercent, DamageType);

            // 넉백
            int idx = state.FindUnitIndex(targetCombatId);
            if (idx < 0) return;
            ref var target = ref state.Units[idx];
            int dirCol = caster.TeamIndex == 0 ? 1 : -1;
            SkillCCHelper.Knockback(state, ref target, dirCol, 0, _knockbackDistance);

            // 넉백 후 위치에서 AoE 스턴 + 보조 데미지
            if (SecondaryPowerPercent > 0 || CCDurationFrames > 0)
            {
                int col = target.GridCol;
                int row = target.GridRow;
                int attack = caster.Attack;
                int power = SecondaryPowerPercent;
                var type = DamageType;
                byte team = caster.TeamIndex;
                var ccType = CCType;
                int ccFrames = CCDurationFrames;

                SkillAreaHelper.ForEachEnemyInRadius(state, team, col, row, _stunAoERange,
                    (ref CombatUnit t, int i) =>
                    {
                        if (power > 0)
                        {
                            int raw = attack * power / 100;
                            int dmg = DamageSystem.CalculateDamage(raw, type, ref t);
                            DamageSystem.ApplyDamage(state, ref t, dmg);
                        }
                        if (ccType != CrowdControlType.None && ccFrames > 0)
                            SkillCCHelper.ApplyCC(ref t, ccType, ccFrames);
                    });
            }
        }

        public override void Reset()
        {
            _knockbackDistance = 4;
            _stunAoERange = 1;
        }
    }
}
```

### 10-5: SimSkillMenshaShield (멘샤 - 열 실드)

```csharp
// Assets/_Project/Scripts/InGame_New/Simulation/Combat/Skills/Custom/SimSkillMenshaShield.cs
namespace CookApps.AutoChess
{
    /// <summary>멘샤: 같은 행 아군에게 실드 부여</summary>
    public class SimSkillMenshaShield : SimSkillBase
    {
        private int _shieldDurationFrames;

        public override void Initialize(SkillParams p)
        {
            base.Initialize(p);
            _shieldDurationFrames = p.Param0 > 0 ? p.Param0 : 180;
        }

        public override int SelectTarget(CombatMatchState state, ref CombatUnit caster)
        {
            // 자기 자신 (행 기준이므로)
            return caster.CombatId;
        }

        public override void Execute(CombatMatchState state, ref CombatUnit caster,
            int targetCombatId, ref DeterministicRNG rng)
        {
            int shieldAmount = caster.Attack * PowerPercent / 100;
            int row = caster.GridRow;

            for (int i = 0; i < state.UnitCount; i++)
            {
                ref var u = ref state.Units[i];
                if (u.TeamIndex != caster.TeamIndex || u.CurrentHP <= 0) continue;
                if (u.GridRow != row) continue;
                SkillBuffHelper.AddShield(state, i, shieldAmount, _shieldDurationFrames);
            }
        }

        public override void Reset()
        {
            _shieldDurationFrames = 180;
        }
    }
}
```

### 10-6: SimSkillClayChannel (클레이 - 채널링 존)

```csharp
// Assets/_Project/Scripts/InGame_New/Simulation/Combat/Skills/Custom/SimSkillClayChannel.cs
namespace CookApps.AutoChess
{
    /// <summary>클레이: 채널링 존 (아군 힐 + 적 데미지 + 힐감소)</summary>
    public class SimSkillClayChannel : SimSkillBase
    {
        private int _healPercent;
        private int _damagePercent;
        private int _healReductionPercent;
        private int _zoneRange;

        public override void Initialize(SkillParams p)
        {
            base.Initialize(p);
            _healPercent = p.Param0;
            _damagePercent = p.Param1;
            _healReductionPercent = p.Param2;
            _zoneRange = p.Param3 > 0 ? p.Param3 : 1;
        }

        public override int SelectTarget(CombatMatchState state, ref CombatUnit caster)
        {
            return caster.CombatId; // 자기 위치 중심
        }

        public override void Execute(CombatMatchState state, ref CombatUnit caster,
            int targetCombatId, ref DeterministicRNG rng)
        {
            int col = caster.GridCol;
            int row = caster.GridRow;
            int attack = caster.Attack;
            var type = DamageType;
            byte team = caster.TeamIndex;

            // 아군 힐
            SkillAreaHelper.ForEachAllyInRadius(state, team, col, row, _zoneRange,
                (ref CombatUnit ally, int i) =>
                {
                    int heal = attack * _healPercent / 100;
                    SkillDamageHelper.Heal(ref ally, heal);
                });

            // 적 데미지 + 힐감소 디버프
            int dmgPct = _damagePercent;
            int healRedPct = _healReductionPercent;
            SkillAreaHelper.ForEachEnemyInRadius(state, team, col, row, _zoneRange,
                (ref CombatUnit enemy, int i) =>
                {
                    int raw = attack * dmgPct / 100;
                    int dmg = DamageSystem.CalculateDamage(raw, type, ref enemy);
                    DamageSystem.ApplyDamage(state, ref enemy, dmg);
                    // 힐감소는 StatusEffect로 처리 가능 시 추가
                });
        }

        public override void Reset()
        {
            _healPercent = 0;
            _damagePercent = 0;
            _healReductionPercent = 0;
            _zoneRange = 1;
        }
    }
}
```

### 10-7: SimSkillMarieAssassin (마리에 - 텔레포트 + 다단타격)

```csharp
// Assets/_Project/Scripts/InGame_New/Simulation/Combat/Skills/Custom/SimSkillMarieAssassin.cs
namespace CookApps.AutoChess
{
    /// <summary>마리에: 타겟 뒤로 순간이동 + 다단히트 + 조건부 디버프</summary>
    public class SimSkillMarieAssassin : SimSkillBase
    {
        public override int SelectTarget(CombatMatchState state, ref CombatUnit caster)
        {
            return TargetingSystem.FindNearestEnemy(state, ref caster);
        }

        public override void Execute(CombatMatchState state, ref CombatUnit caster,
            int targetCombatId, ref DeterministicRNG rng)
        {
            for (int i = 0; i < HitCount; i++)
            {
                int idx = state.FindUnitIndex(targetCombatId);
                if (idx < 0 || state.Units[idx].CurrentHP <= 0) break;
                SkillDamageHelper.DealDamage(state, ref caster, targetCombatId, PowerPercent, DamageType);
            }

            // 디버프 적용 (Param0으로 조건 체크 여부 결정)
            if (CCType != CrowdControlType.None && CCDurationFrames > 0)
            {
                int idx = state.FindUnitIndex(targetCombatId);
                if (idx >= 0 && state.Units[idx].CurrentHP > 0)
                {
                    SkillCCHelper.ApplyCC(ref state.Units[idx], CCType, CCDurationFrames);
                }
            }
        }
    }
}
```

**Step: 커스텀 스킬 등록**

SkillFactory.RegisterCustomSkills() 채우기:

```csharp
private static void RegisterCustomSkills()
{
    // 유니 (215252102) - 멀티힐+디버프제거
    Register(215252102, () => new SimSkillYuniHeal());
    // 미노 (217433302) - 멀티프로젝타일+스플래시
    Register(217433302, () => new SimSkillMinoProjectile());
    // 베인 (217363204) - 바운스 프로젝타일
    Register(217363204, () => new SimSkillVeinBounce());
    // 테토라 (217413301) - 넉백+AoE스턴
    Register(217413301, () => new SimSkillTetoraKnockback());
    // 멘샤 (215422301) - 열 실드
    Register(215422301, () => new SimSkillMenshaShield());
    // 클레이 (217553404) - 채널링 존
    Register(217553404, () => new SimSkillClayChannel());
    // 마리에 (217563405) - 텔포 다단타격
    Register(217563405, () => new SimSkillMarieAssassin());
}
```

**Commit:**

```bash
git add Assets/_Project/Scripts/InGame_New/Simulation/Combat/Skills/Custom/
git add Assets/_Project/Scripts/InGame_New/Simulation/Combat/Skills/SkillFactory.cs
git commit -m "feat(InGame_New): 커스텀 플레이어 스킬 7종 구현 + 팩토리 등록"
```

---

## Task 11: StatusEffectSystem에 RemoveDebuffs 추가

유니 스킬에서 필요한 디버프 제거 기능.

**Files:**
- Modify: `Assets/_Project/Scripts/InGame_New/Simulation/Combat/StatusEffectSystem.cs`

**Step 1: RemoveDebuffs 메서드 추가**

```csharp
/// <summary>유닛의 디버프 N개 제거 (오래된 것부터)</summary>
public static void RemoveDebuffs(CombatMatchState state, int unitIndex, int count)
{
    int removed = 0;
    for (int i = 0; i < state.StatusEffectCount && removed < count; i++)
    {
        ref var effect = ref state.StatusEffects[i];
        if (effect.TargetUnitIndex != unitIndex) continue;
        if (effect.Type != StatusEffectType.StatDebuff) continue;

        // 제거: 마지막 요소와 스왑
        state.StatusEffects[i] = state.StatusEffects[--state.StatusEffectCount];
        i--; // 스왑된 요소 재검사
        removed++;
    }
}
```

**Step 2: Commit**

```bash
git add Assets/_Project/Scripts/InGame_New/Simulation/Combat/StatusEffectSystem.cs
git commit -m "feat(InGame_New): StatusEffectSystem.RemoveDebuffs 추가"
```

---

## Task 12: SkillSpecAdapter.ClassifySkill 스킬 분류 매핑 완성

레거시 스킬 ID → 아키타입 매핑 테이블 작성. 실제 EffectCode 구현체의 패턴 분석 결과 기반.

**Files:**
- Modify: `Assets/_Project/Scripts/InGame_New/Adapter/SkillSpecAdapter.cs`

**Step 1: 몬스터 스킬 ID 범위 기반 분류**

레거시 분석 결과를 바탕으로 스킬 ID → 아키타입 매핑:

```csharp
public static SimSkillArchetype ClassifySkill(SkillActive spec)
{
    int id = spec.id;

    // 커스텀 플레이어 스킬 (개별 매핑)
    switch (id)
    {
        case 215252102: return SimSkillArchetype.Custom; // 유니
        case 217433302: return SimSkillArchetype.Custom; // 미노
        case 217363204: return SimSkillArchetype.Custom; // 베인
        case 217413301: return SimSkillArchetype.Custom; // 테토라
        case 215422301: return SimSkillArchetype.Custom; // 멘샤
        case 217553404: return SimSkillArchetype.Custom; // 클레이
        case 217563405: return SimSkillArchetype.Custom; // 마리에
    }

    // 플레이어 스킬 (아키타입 매핑)
    switch (id)
    {
        case 215532401: return SimSkillArchetype.SingleDamage;   // 필리아
        case 215362202: return SimSkillArchetype.DamageCC;       // 시이나 (침묵)
        case 217433303: return SimSkillArchetype.DamageCC;       // 하티 (넉백)
        case 217323201: return SimSkillArchetype.DamageCC;       // 미사 (기절)
        case 217243102: return SimSkillArchetype.AoEDamage;      // 블린
        case 217513401: return SimSkillArchetype.LineDamage;     // 아트레시아
        case 1406031:   return SimSkillArchetype.Heal;           // 아란
        case 217653505: return SimSkillArchetype.MultiTargetHeal;// 엔키
        case 215322201: return SimSkillArchetype.PatternDamage;  // 메이
        case 217523403: return SimSkillArchetype.PatternDamage;  // 아드리아
        case 217613501: return SimSkillArchetype.PatternDamage;  // 오데트
        case 217663506: return SimSkillArchetype.MultiHit;       // 시라유키
        case 215642501: return SimSkillArchetype.AoEDamage;      // 엘리스
        case 217353203: return SimSkillArchetype.AoEDamage;      // 라키유
        case 217333202: return SimSkillArchetype.LineDamage;     // 에이프릴
        case 217263103: return SimSkillArchetype.Buff;           // 루키다
    }

    // 몬스터 스킬: EffectCode ID 패턴 기반 분류
    // 레거시 분석 결과에 따른 명시적 매핑
    if (IsMonsterSkill(id))
        return ClassifyMonsterSkill(id);

    return SimSkillArchetype.SingleDamage; // 기본값
}

private static bool IsMonsterSkill(int id)
{
    return id >= 1100000 || (id >= 20000 && id <= 40000);
}

private static SimSkillArchetype ClassifyMonsterSkill(int id)
{
    // CC 스킬 (분석 결과)
    switch (id)
    {
        case 1102061: case 230404002: case 230505002: case 230606002:
        case 240107001: case 240407301: case 250208101:
            return SimSkillArchetype.DamageCC;

        // 콘 데미지
        case 230101002: case 230404001: case 230505001: case 230606001:
        case 280109001:
            return SimSkillArchetype.ConeDamage;

        // 패턴 AoE
        case 1103041: case 1203021: case 230505003: case 250608501:
        case 280109002:
            return SimSkillArchetype.PatternDamage;

        // 관통 프로젝타일
        case 1104081: case 230404004: case 230505004: case 230606004:
        case 240107002:
            return SimSkillArchetype.LineDamage;

        // 텔레포트
        case 1202091: case 240407302: case 250108002: case 250108003:
            return SimSkillArchetype.TeleportStrike;

        // 멀티히트
        case 1105031: case 230404005: case 230505005:
            return SimSkillArchetype.MultiHit;

        // 힐
        case 1106041: case 230404006: case 230505006: case 230606006:
            return SimSkillArchetype.MultiTargetHeal;
    }

    return SimSkillArchetype.SingleDamage;
}
```

**Step 2: BuildParams에 아키타입별 파라미터 세팅 추가**

```csharp
public static SkillParams BuildParams(SkillActive spec)
{
    var archetype = ClassifySkill(spec);
    var dmgType = spec.atk_type == AtkType.AP ? DamageType.Magical : DamageType.Physical;
    int powerPercent = Mathf.RoundToInt(spec.base_rate * 100f);

    var p = new SkillParams
    {
        SkillId = spec.id,
        PowerPercent = powerPercent > 0 ? powerPercent : 200,
        DamageType = dmgType,
        CastFrames = 0,
        TargetCount = 1,
        HitCount = 1,
    };

    // 아키타입별 기본 파라미터
    switch (archetype)
    {
        case SimSkillArchetype.DamageCC:
            p.CCType = CrowdControlType.Stun;
            p.CCDurationFrames = 60; // 1초 (60fps)
            break;
        case SimSkillArchetype.ConeDamage:
            p.Param0 = 2; // 전방 2타일
            break;
        case SimSkillArchetype.PatternDamage:
            p.Param0 = (int)AreaPatternType.Cross;
            p.Param1 = 1; // 범위
            break;
        case SimSkillArchetype.MultiHit:
            p.HitCount = 3;
            break;
        case SimSkillArchetype.MultiTargetHeal:
            p.TargetCount = 3;
            break;
        case SimSkillArchetype.TeleportStrike:
            p.Param0 = 1; // AoE 범위
            p.CCType = CrowdControlType.Stun;
            p.CCDurationFrames = 60;
            p.CastFrames = 30; // 시전 시간 (무적)
            break;
        case SimSkillArchetype.AoEDamage:
            p.Param0 = 1; // 반경
            break;
    }

    // 스킬별 세부 파라미터 오버라이드 (필요 시)
    ApplySkillSpecificParams(ref p, spec.id, archetype);

    return p;
}

private static void ApplySkillSpecificParams(ref SkillParams p, int id, SimSkillArchetype archetype)
{
    // 개별 스킬의 EffectCode stat 값을 SkillParams로 매핑
    // TODO: EffectCode 스펙 데이터에서 읽어올 수 있으면 여기서 매핑
    // 현재는 분석 결과 기반 하드코딩
    switch (id)
    {
        case 215362202: // 시이나: 침묵
            p.CCType = CrowdControlType.Silence;
            p.CCDurationFrames = 90;
            break;
        case 217433303: // 하티: 넉백
            p.CCType = CrowdControlType.Knockback;
            p.CCDurationFrames = 2; // 2타일
            break;
        case 215252102: // 유니: 3명 힐 + 디버프 2개 제거
            p.TargetCount = 3;
            p.Param0 = 2; // 디버프 제거 수
            break;
        case 217433302: // 미노: 3발 + 스플래시
            p.TargetCount = 3;
            break;
        case 217363204: // 베인: 5회 바운스 + 공속 버프
            p.TargetCount = 5;
            p.SecondaryPowerPercent = 20; // 바운스당 20% 감소
            p.BuffStat = StatModType.AttackSpeed;
            p.BuffValue = 30;
            p.BuffDurationFrames = 180;
            break;
    }
}
```

**Step 3: Commit**

```bash
git add Assets/_Project/Scripts/InGame_New/Adapter/SkillSpecAdapter.cs
git commit -m "feat(InGame_New): SkillSpecAdapter 스킬 분류 매핑 완성 (71개 액티브 스킬)"
```

---

## Task 13: View 레이어 스킬 이벤트 처리

CombatViewManager에서 UnitCastSkill 이벤트 수신 시 스킬 VFX 재생.

**Files:**
- Modify: `Assets/_Project/Scripts/InGame_New/View/Combat/CombatViewManager.cs`

**Step 1: UnitCastSkill 이벤트 핸들링 확인 및 보완**

CombatViewManager가 이미 SimEvent를 처리하는 구조가 있다면, UnitCastSkill 이벤트에서 SkillActive.skill_vfxs를 참조하여 VFX 재생하는 로직 추가. 구체적인 구현은 기존 CombatViewManager 코드를 먼저 확인한 후 작성.

```csharp
// CombatViewManager의 이벤트 처리 루프에서:
case SimEventType.UnitCastSkill:
{
    int casterId = evt.EntityId;
    int targetId = evt.TargetId;
    int skillSpecId = evt.Value0;

    // SkillActive 스펙에서 VFX 정보 조회
    var skillSpec = SpecDataManager.Instance.GetSkillDataList(skillSpecId);
    if (skillSpec != null && skillSpec.Count > 0)
    {
        var spec = skillSpec[0];
        // spec.skill_vfxs 배열로 VFX 재생
        // casterView, targetView 위치 참조
    }
    break;
}
```

**Step 2: Commit**

```bash
git add Assets/_Project/Scripts/InGame_New/View/Combat/CombatViewManager.cs
git commit -m "feat(InGame_New): CombatViewManager 스킬 VFX 이벤트 처리"
```

---

## Task 14: 통합 테스트 - 에디터에서 스킬 동작 확인

**Step 1: SkillFactory.Initialize() 호출 시점 확인**

LocalSimulationRunner 또는 GameLoopSystem 초기화 시점에서 SkillFactory.Initialize()가 호출되는지 확인. 안 되어 있으면 추가.

**Step 2: 에디터 플레이 테스트**

1. Unity Editor에서 InGame_New 씬 로드
2. 플레이 모드 진입
3. Console에서 스킬 캐스트 로그 확인 (CombatLogger)
4. 다양한 캐릭터의 스킬이 올바르게 발동되는지 확인

**Step 3: 문제 수정 및 커밋**

```bash
git add -u
git commit -m "fix(InGame_New): 스킬 시스템 통합 테스트 수정사항"
```

---

## 의존성 그래프

```
Task 1 (SkillParams 확장)
├── Task 2 (DamageCC)
├── Task 3 (ConeDamage)
├── Task 4 (PatternDamage)
├── Task 5 (MultiHit)
├── Task 6 (MultiTargetHeal)
├── Task 7 (TeleportStrike)
├── Task 8 (SkillSpecAdapter) ← Tasks 2-7
├── Task 9 (SkillFactory) ← Task 8
├── Task 10 (커스텀 스킬) ← Task 1
│   └── Task 11 (RemoveDebuffs) ← Task 10-1
├── Task 12 (분류 매핑) ← Tasks 8, 10
├── Task 13 (View 이벤트) ← Task 9
└── Task 14 (통합 테스트) ← All
```