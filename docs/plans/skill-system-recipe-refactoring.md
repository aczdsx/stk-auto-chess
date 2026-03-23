# 스킬 시스템 리팩토링 — Recipe 패턴 도입

## Context

현재 스킬 시스템의 문제:
1. **19개 커스텀 스킬** 각각 별도 클래스 (60-70% 보일러플레이트 중복)
2. **SkillSpecAdapter 하드코딩** — 스킬 ID별 switch문으로 아키타입 분류/파라미터 오버라이드
3. **SpecData(JSON) 구조** — specList 인덱스 기반 매직넘버, 같은 PERCENT 타입이 스킬마다 다른 의미
4. **Quantum 이전 대비** — 데이터와 로직 분리가 필요

목표: Code-side Recipe + Spec-side Values 패턴으로 전환하여 커스텀 클래스 수를 줄이고, Quantum 이전에 유리한 구조를 만든다.

---

## 커스텀 스킬 분류 (Recipe 전환 가능 여부)

### Yes — Recipe만으로 완전 대체 (10개, 클래스 삭제 완료/가능)

| 스킬 | ID | ExecutionType | Recipe 요약 | 상태 |
|------|----|---------------|-------------|------|
| 유니 | 215252102 | DelayedApply | Heal + RemoveDebuffs on LowestHpAllies(3), 다중 VFX | **전환 완료** |
| 멘샤 | 215422301 | DelayedApply | Shield on SameRowAllies | **전환 완료** |
| 미사 | 217323201 | DelayedApply | CC(Stun) + AddMarker on HighestAtkEnemy | **전환 완료** |
| 필리아 | 215532401 | DelayedApply | Damage + AddMarker(PiliaSkillCast), 3단계 VFX | **전환 완료** |
| 하티 | 217433303 | DelayedApply | Damage + Knockback(2), 3단계 VFX | **전환 완료** |
| 클레이 | 217553404 | Channeling | Zone(Diamond2): Heal+RemoveDebuffs(아군) + Damage+Debuff(적), 짝수틱 VFX | **전환 완료** |
| 엘리스 | 215642501 | Channeling | Phase0: VFX(예약), Phase1: Damage(Diamond1), 딜레이 | **전환 완료** |
| 메이 | 215322201 | DelayedApply | Plus AoE + 넉백(방향 개별 계산) + 방어 버프 | **전환 완료** |
| 보스탱커 | 250108001 | Channeling | 전방 10칸 순차 타격 + 카메라쉐이크 + 넉백 | **전환 완료** |
| SingleProjectile | 230101005 등 | DelayedApply | SpawnProjectile(Homing) → Damage | **전환 완료** |

> 메이와 보스탱커는 당초 Hybrid/Custom 분류였으나, ActionExecutor에 `DirectionalKnockback`, `BossLineSequence` 등의 EffectType 추가로 Recipe 전환 가능해짐.

### No — 커스텀 유지 필수 (11개, 보일러플레이트만 축소)

| 스킬 | ID | 커스텀 필수 이유 |
|------|----|-----------------|
| 미노 | 217433302 | 순차 미사일 발사, 개별 투사체 도착 타이머 관리 |
| 베인 | 217363204 | 바운스 투사체, 히트 기록 추적, 동적 타겟 재탐색 |
| 에이프릴 | 217333202 | 확장 콘(1→4칸), 거리별 3단계 배율, 동적 틱 간격 |
| 엔키 | 217653505 | 보드 전체 스윕, 5개 Linear 투사체 동시 생성 |
| 테토라 | 217413301 | 넉백 후 벽 충돌 판정 → 착지점 3×3 AoE + 스턴 |
| 오데트 | 217613501 | 2단계(ㄷ자형→텔레포트→3×3), Phase별 다른 범위 |
| 시라유키 | 217663506 | 최저HP 3명 순차 텔레포트 암살, 동적 위치 탐색 |
| 마리에 | 217563405 | 텔레포트 위치 탐색 + HitFrame별 순차 히트 + 마지막만 디버프 |
| 아드리아 | 217523403 | 3단계 확장 패턴(+→X→+2) + 비트마스크 중복방지 + 방어력 스케일 |
| 라키유 | 217353203 | 투사체 도착 후 범위 내 2종 디버프 + 마커 후처리 |
| 루키다 | 217263103 | 마커 카운트 기반 동적 버프 값 계산 (foxfire 수 × 공속비율) |

### 기존 아키타입 (13개 → 전부 Recipe로 대체 가능)

SingleDamage, AoEDamage, LineDamage, ConeDamage, DamageCC, PatternDamage, MultiHit, Heal, MultiTargetHeal, TeleportStrike, Buff, Debuff, Stun, DiamondAoE

---

## 성능 분석

### Recipe 전환으로 인한 성능 변화

| 항목 | 현재 | Recipe 후 | 영향 |
|------|------|----------|------|
| **클래스 수** | 13 아키타입 + 21 커스텀 = 34개 | 1 Generic + 10 커스텀 = 11개 | vtable 축소, 코드 캐시 개선 |
| **인스턴스 메모리** | 클래스별 다른 필드 크기 | SimSkillGeneric 고정 크기 | 메모리 예측 가능 |
| **GC 압력** | InitializeFromSpec에서 배열 할당 | Recipe는 static readonly (할당 0) | GC 감소 |
| **틱 당 CPU** | virtual dispatch (vtable lookup) | switch dispatch + Action 순회(~5개) | 미미한 차이 |
| **SkillFactory** | Reflection 없이 수동 등록 + 아키타입별 new | Registry lookup + Generic 재사용 | 초기화 단순화 |

### 핵심 성능 수치 추정

- 60fps × 최대 20유닛 = **1,200 OnChannelTick/sec** (최악의 경우, 모두 채널링)
- Action 배열 평균 5개 × 조건 체크 = **6,000 조건 비교/sec**
- 현재 DealDamage/Heal 등 Helper 호출은 동일 → **효과 실행 비용 변화 없음**
- **결론: 측정 가능한 성능 차이 없음.** 실제 병목은 SkillAreaHelper의 범위 쿼리와 데미지 계산

### JSON 부담

- SkillActive 테이블 구조 변경 **없음** (specList 유지)
- 새 테이블 추가 **없음**
- Recipe 데이터는 코드에 static으로 존재 (deserialize 비용 0)
- **결론: JSON 부담 현재와 동일**

---

## 코드 파일 구조 (현재)

```
Assets/_Project/Scripts/InGame_New/Simulation/Combat/Skills/
│
├── SimSkillBase.cs                    (기존, 변경 최소)
├── SimSkillGeneric.cs                 (~350줄, Recipe 기반 범용 실행기)
│
├── Recipe/                            (신규 폴더)
│   ├── SkillRecipe.cs                 (데이터 구조: SkillRecipe, SkillAction, ParamSlot, ParamValueType)
│   ├── SkillRecipeBuilder.cs          (Builder 패턴 — 체이닝 API로 Recipe 선언 간결화)
│   ├── ActionExecutor.cs              (이펙트 실행: switch 기반 디스패치, ~700줄)
│   ├── SkillRecipeRegistry.cs         (코어: TryGet/TryGetByArchetype + Builder 헬퍼)
│   ├── SkillRecipeRegistry.Archetypes.cs (partial — 14개 아키타입 Recipe 정의)
│   ├── SkillRecipeRegistry.Character.cs  (partial — 플레이어 스킬 Recipe 정의)
│   └── SkillRecipeRegistry.Monster.cs    (partial — 몬스터 스킬 Recipe 정의)
│
├── Custom/                            (기존 폴더, 11개 유지)
│   ├── SimSkillMinoProjectile.cs      (미노: 순차 미사일)
│   ├── SimSkillVeinBounce.cs          (베인: 바운스 투사체)
│   ├── SimSkillAprilBarrage.cs        (에이프릴: 확장 콘)
│   ├── SimSkillEnkiWaveHeal.cs        (엔키: 보드 스윕)
│   ├── SimSkillTetoraKnockback.cs     (테토라: 넉백 + 착지 AoE)
│   ├── SimSkillOdetteStrike.cs        (오데트: 2단계)
│   ├── SimSkillShirayukiAssassin.cs   (시라유키: 순차 암살)
│   ├── SimSkillMarieAssassin.cs       (마리에: 텔레포트 순차 히트)
│   ├── SimSkillAdriaExpand.cs         (아드리아: 3단계 확장)
│   ├── SimSkillRakiyuDebuff.cs        (라키유: 투사체 후 범위 디버프)
│   └── SimSkillRukidaFoxfire.cs       (루키다: 마커 기반 동적 버프)
│   # 삭제 완료 (8개): YuniHeal, MenshaShield, MisaRestraint,
│   #   PiliaStrike, HatiKnockback, ClayChannel, EllisAoE, SingleProjectile
│
│   # Archetype 폴더 — 전부 Recipe로 대체, 점진적 삭제 가능
│
├── Helpers/                           (기존, 변경 없음)
│   ├── SkillDamageHelper.cs
│   ├── SkillBuffHelper.cs
│   ├── SkillCCHelper.cs
│   ├── SkillAreaHelper.cs
│   └── SkillSpecHelper.cs
│
└── SkillFactory.cs                    (수정 완료: Recipe Registry 기반 생성)
```

### 수정 대상 기존 파일

| 파일 | 변경 내용 |
|------|----------|
| `SkillFactory.cs` | RegisterCustomSkills → Recipe Registry에서 SimSkillGeneric 생성. 커스텀 10개만 수동 등록 |
| `SkillSpecAdapter.cs` | ClassifySkill() 단순화 (Recipe에 아키타입 정보 포함), ApplySkillSpecificParams() 축소 |
| `Enums.cs` | SimSkillArchetype enum에 Recipe 추가 (또는 아키타입 자체를 Recipe로 대체) |
| `SimSkillBase.cs` | ParamSlots/ParamValues 지원을 위한 최소 인터페이스 추가 |

---

## 구현 순서 (단계별)

### Phase 1: 기반 구조 (Recipe 시스템 코어) — **완료**
1. `Recipe/SkillRecipe.cs` — 데이터 구조 정의
2. `Recipe/SkillRecipeBuilder.cs` — 체이닝 Builder API
3. `Recipe/ActionExecutor.cs` — 이펙트 디스패치 로직
4. `SimSkillGeneric.cs` — Generic Executor
5. `Recipe/SkillRecipeRegistry.cs` — 코어 + partial class 분리

### Phase 2: 아키타입 Recipe 전환 — **완료**
1. `SkillRecipeRegistry.Archetypes.cs` — 14개 아키타입 Recipe 정의
2. `SkillRecipeRegistry.Monster.cs` — 몬스터 스킬 Recipe
3. `SkillFactory.cs` 수정 — Recipe 기반 생성 경로 추가
4. 기존 아키타입 클래스와 병행 운영하면서 검증

### Phase 3: 커스텀 → Recipe 전환 (Yes 그룹 10개) — **완료**
1. 유니, 멘샤, 미사, 필리아, 하티, 클레이, 엘리스, 메이, 보스탱커, SingleProjectile Recipe 정의
2. 해당 커스텀 클래스 삭제 (8개 삭제 완료)
3. SkillFactory.RegisterRecipeSkills()에 일괄 등록

### Phase 4: 정리 — **진행 중**
1. SkillSpecAdapter 하드코딩 점진적 제거 (Recipe의 ParamSlots가 대체)
2. 아키타입 클래스 파일 삭제 (Recipe로 완전 대체 확인 후)
3. SimSkillArchetype enum 정리

### Phase 5: 커스텀 스킬 점진적 전환 — **미착수**
1. ActionExecutor에 새 EffectType 추가하며 라키유, 루키다 등 하나씩 Recipe화
2. 목표: 커스텀 11개 → 최소화

---

## Quantum 이전 시 매핑

| 현재 구조 | Quantum 구조 |
|----------|-------------|
| `SkillRecipe` (static Dictionary) | `QAsset SkillRecipeAsset` (바이너리 에셋) |
| `SkillAction[]` (코드 내 배열) | `array<SkillActionData>[8]` (Quantum DSL 고정 배열) |
| `SimSkillGeneric.DispatchActions()` | `SkillActionSystem.Execute()` (Quantum System) |
| `specList → _paramValues[]` | `RuntimeConfig.SkillBalance[]` 또는 `AssetRef` |
| `SkillDamageHelper` 등 | Quantum System 내 static 메서드 |
| `switch(SkillEffectType)` | 동일 (Quantum도 enum switch) |

---

## 검증 방법

1. **유닛 테스트**: 기존 아키타입 스킬을 Recipe로 전환 후, 동일한 입력에 동일한 출력(데미지, VFX 이벤트)이 나오는지 비교
2. **인게임 테스트**: 각 전환된 스킬의 전투 동작 확인 (VFX 타이밍, 데미지 수치, CC 적용)
3. **성능 프로파일링**: Recipe 전환 전후 프레임 타임 비교 (Profiler에서 SkillSystem.Tick 확인)
4. **회귀 테스트**: 기존 전투 시뮬레이션 결과가 동일한지 확인 (결정론적 시뮬레이션이므로 seed 고정 후 비교)

---

## 스킬 개발 플로우

새 스킬을 추가하거나 기존 스킬을 수정할 때의 워크플로우.

### A. Recipe 스킬 추가 (일반적인 경우)

대부분의 새 스킬은 Recipe만으로 정의 가능. **파일 생성 없이** 데이터 추가만으로 완료.

```
1. 기획서에서 스킬 동작 파악
   ├── 타겟 선정 (NearestEnemy, LowestHpAlly, FarthestEnemy 등)
   ├── 효과 (Damage, Heal, CC, Buff, Debuff, Shield 등)
   ├── 범위 (Single, Circle, Diamond, Plus, Line, Cone 등)
   └── VFX 타이밍 (OnCast, AtHitFrame, OnTick)

2. SkillRecipeRegistry.Character.cs에 Recipe 선언
   Skill(SKILL_ID, ExecutionType, TargetRule)
       .Param(specIndex, valueType, fallback)
       .OnCast(Vfx(0, AtCaster))
       .AtHit(Damage())
       .AtHit(Vfx(1, AtTarget))
       .Register();

3. SkillFactory.RegisterRecipeSkills()에 스킬 ID 추가
   recipeSkillIds 배열에 새 ID 한 줄 추가

4. SkillSpecAdapter.ClassifySkill()에 분류 추가
   스킬 spec의 skill_type에 따라 Custom 아키타입으로 분류
   (또는 기존 아키타입에 매핑되면 자동 처리)

5. 테스트 모드에서 확인
   InGameTestConfig에 해당 캐릭터 배치 → 전투 동작/VFX/데미지 확인
```

### B. 커스텀 스킬 추가 (복잡한 경우)

런타임 상태 관리(투사체 개별 타이머, 동적 타겟 탐색, 텔레포트 위치 계산 등)가 필요한 경우.

```
1. Custom/ 폴더에 SimSkillXxx.cs 생성
   SimSkillBase를 상속하고 Execute()/OnChannelTick() 오버라이드

2. SkillRecipeRegistry에 ParamSlots만 정의 (선택)
   스펙 파싱 통일을 위해 Recipe를 만들되, Actions는 비워둠
   → SimSkillGeneric이 아닌 커스텀 클래스가 실행

3. SkillFactory.RegisterCustomSkills()에 등록
   Register(SKILL_ID, () => new SimSkillXxx());

4. SkillSpecAdapter.ClassifySkill()에 Custom 분류 추가

5. 테스트
```

### C. 기존 스킬 수치 변경 (밸런스 패치)

**코드 수정 불필요.** SkillActive JSON 스펙 테이블의 `specList[].base_rate` 값만 변경.

Recipe의 `ParamSlots`가 specList 인덱스를 매핑하고 있으므로, JSON 변경 → 빌드 → 반영.

### D. 기존 스킬에 효과 추가 (예: 넉백 추가)

```
1. SkillRecipeRegistry에서 해당 스킬의 Recipe 찾기
2. .AtHit(CC(CrowdControlType.Knockback, paramIndex)) 추가
3. 필요시 ParamSlot 추가 (넉백 거리 등)
4. 테스트
```

### E. 새 EffectType 추가 (시스템 확장)

기존 ActionExecutor로 표현할 수 없는 새로운 효과가 필요한 경우.

```
1. Enums.cs에 SkillEffectType 열거값 추가
2. ActionExecutor.cs에 case 추가 + Helper 호출
3. Recipe에서 새 EffectType 사용
```

### 의사결정 플로우차트

```
새 스킬이 필요하다
    |
    v
기존 EffectType 조합으로 표현 가능한가?
    ├── YES → Recipe 스킬 (A 경로)
    └── NO → 새 EffectType이면 충분한가?
            ├── YES → EffectType 추가 후 Recipe (E → A 경로)
            └── NO → 런타임 상태가 본질적인가?
                    ├── YES → 커스텀 스킬 (B 경로)
                    └── NO → Recipe로 다시 시도 (설계 재검토)
```
