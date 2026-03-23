# 스킬 시스템 Recipe 패턴 전환 — 왜, 어떻게, 뭐가 좋아졌는지

## 한줄 요약

스킬 ID별 switch 하드코딩 → 데이터 기반 Recipe로 전환. 파일 35개 → 19개 (Generic 1 + Custom 11 + Recipe 7), hot path GC 0, Quantum 이전 준비 완료.

---

## 1. 기존 구조의 문제

### SkillSpecAdapter — 3단계 switch 하드코딩

```
ClassifySkill()           → 스킬 ID별 switch (50줄, 아키타입 분류)
BuildParams()             → 아키타입별 switch (30줄, Param0~3 기본값)
ApplySkillSpecificParams()→ 스킬 ID별 switch (20줄, 개별 오버라이드)
```

**문제**: 새 스킬 추가 시 3곳을 동시에 수정해야 함. 한 곳 빠지면 silent bug.

### 아키타입 클래스 14개 + 커스텀 클래스 21개 = 35개 파일

```
SimSkillSingleDamage.cs    (17줄)  ← 이 정도 로직을 위해 파일 1개?
SimSkillHeal.cs            (22줄)
SimSkillAoEDamage.cs       (49줄)  ← 60%가 보일러플레이트
SimSkillClayChannel.cs     (150줄) ← 동일한 채널링 타이머 코드가 6곳에 반복
```

**문제**:
- 채널링 보일러플레이트 (startDelay, tickTimer, tickInterval, remainingTicks)가 모든 채널링 스킬에 복붙
- 새 스킬 추가 시 파일 1개 + SkillFactory 등록 + SkillSpecAdapter 분류 = 3곳 수정
- 아키타입 클래스의 AoE 순회에서 lambda 클로저 → **매 틱 GC 할당**

### SkillSpecAdapter.Param0~3 — 의미 없는 범용 필드

```csharp
p.Param0 = 2;  // ConeDamage에서는 "range", AoEDamage에서는 "radius", TeleportStrike에서는 "AoE range"
p.Param1 = 1;  // PatternDamage에서는 "range"
p.Param2 = 3;  // LineDamage에서는 "width"
```

**문제**: 같은 Param0이 아키타입마다 다른 의미. 주석 없이는 이해 불가.

---

## 2. Recipe 패턴으로 뭐가 바뀌었나

### Before → After 비교

| 항목 | Before | After |
|------|--------|-------|
| **스킬 정의** | 클래스 1개 + Factory 등록 + Adapter 분류 | Recipe 데이터 1곳에 선언 |
| **새 스킬 추가** | 3곳 수정 | Recipe 배열에 1곳 추가 |
| **파일 수** | 35개 | 19개 (**46% 감소**) |
| **보일러플레이트** | 채널링 타이머 6곳 반복 | SimSkillGeneric이 1곳에서 처리 |
| **GC 할당** | AoE 스킬마다 lambda 클로저 | **0** (직접 for-loop) |
| **Param 의미** | Param0~3 범용 (주석 필요) | ParamSlot에 명시적 이름 |

### 코드량 비교

```
Before:
  SimSkillSingleDamage.cs    17줄
  SimSkillAoEDamage.cs       49줄
  SimSkillClayChannel.cs    150줄
  SimSkillHeal.cs            22줄
  ... (35개 총 ~2,500줄)

After:
  SkillRecipeRegistry.cs              — 코어 + Builder 헬퍼
  SkillRecipeRegistry.Archetypes.cs   — 14개 아키타입 = Recipe 데이터만
  SkillRecipeRegistry.Character.cs    — 플레이어 스킬 Recipe 데이터
  SkillRecipeRegistry.Monster.cs      — 몬스터 스킬 Recipe 데이터
  SkillRecipeBuilder.cs               — 체이닝 Builder API
  SkillRecipe.cs                      — 데이터 구조 (SkillRecipe, SkillAction, ParamSlot)
  SimSkillGeneric.cs                  — 1개 실행기 (~350줄)
  ActionExecutor.cs                   — 1개 디스패처 (~700줄)
```

### 새 스킬 추가 예시

```csharp
// Before: 파일 생성 + Factory 등록 + Adapter 분류 (3곳)
// After: Recipe 1곳 추가
_recipes[NEW_SKILL_ID] = new SkillRecipe
{
    ExecutionType = SkillExecutionType.DelayedApply,
    TargetRule = SkillTargetType.NearestEnemy,
    ParamSlots = new[] { new ParamSlot(1, ParamValueType.Int, 200f) },
    Actions = new[]
    {
        new SkillAction { Trigger = AtHitFrame, Effect = Damage, ... },
        new SkillAction { Trigger = AtHitFrame, Effect = Knockback, ... },
    }
};
// 끝. 파일 생성 없음, Factory 수정 없음.
```

---

## 3. SkillSpecAdapter 현재 상태

`ClassifySkill()`, `BuildParams()`의 switch는 **아직 유지**됩니다. 이유:

- `ClassifySkill()` → SkillFactory에서 아키타입을 판별해 Recipe를 찾는 데 사용
- `BuildParams()` → SkillParams (PowerPercent, CooldownSeconds, SkillHitFrames 등) 추출에 사용
- `ApplySkillSpecificParams()` → 시이나(침묵 CC), 블린(범위 2), 아트레시아(폭 3) 같은 아키타입 오버라이드에 사용

**이것들은 점진적으로 Recipe의 ParamSlots로 이전 가능합니다.** 하지만 현재 동작하는 코드를 한번에 바꾸면 위험하므로, 기존 파이프라인을 유지하면서 Recipe가 우선 적용되는 구조입니다:

```
SkillFactory.Initialize()
  1. SkillSpecAdapter.BuildParams() → SkillParams 생성 (기존 유지)
  2. SkillRecipeRegistry.TryGetByArchetype() → Recipe 있으면 SimSkillGeneric 사용
  3. Recipe 없으면 SkillSpecAdapter.CreateFromArchetype() → null (모두 Recipe로 전환됨)
```

---

## 4. 성능

### GC 할당 제거

| 지점 | Before | After |
|------|--------|-------|
| AoE 데미지 (매 틱) | lambda 클로저 힙 할당 | 직접 for-loop (**0**) |
| AoE 힐 (매 틱) | lambda 클로저 | 직접 for-loop (**0**) |
| AoE 디버프 (매 틱) | lambda 클로저 | 직접 for-loop (**0**) |
| LowestHpAllies 버퍼 | `new int[count]` 매번 | static readonly 버퍼 재사용 |
| StatusEffectSystem 마커 | `new int[32]` x2 매번 | static readonly 버퍼 재사용 |
| 미노 타겟 수집 | `new HashSet<int>()` | pre-allocated `int[]` 배열 (할당 0) |

**결과**: 전투 hot path에서 `new` 힙 할당 **0**.

### CPU

- lambda delegate invoke (vtable lookup) → switch 기반 직접 호출
- JIT 인라인 최적화 가능
- 실측 차이는 미미하지만, Quantum 이전 시 delegate 자체를 쓸 수 없으므로 필수 전환

---

## 5. Quantum 이전 준비

| 현재 구조 | Quantum 매핑 |
|----------|-------------|
| `SkillRecipe` (static Dictionary) | `QAsset SkillRecipeAsset` (바이너리) |
| `SkillAction[]` 배열 | `array<SkillActionData>[8]` (Quantum DSL 고정 배열) |
| `ActionExecutor` switch | Quantum System의 동일한 switch |
| `SkillExecuteContext` struct | Quantum Frame context |
| `SkillAreaHelper.IsInArea()` | Quantum physics query 또는 동일 로직 |

**핵심**: Recipe 패턴은 "데이터(struct) + 로직(switch)" 분리 구조 → Quantum의 "Component + System" 패턴과 **1:1 대응**.

기존 아키타입 클래스(OOP 상속 + virtual 메서드)는 Quantum의 ECS에 매핑할 수 없었음. Recipe 전환이 Quantum 이전의 **필수 선행 작업**.

---

## 6. 아직 커스텀인 11개는?

미노, 베인, 에이프릴, 엔키, 테토라, 오데트, 시라유키, 마리에, 아드리아, 라키유, 루키다 — 텔레포트 위치 탐색, 투사체 개별 타이머, 동적 타겟 재탐색 등 **런타임 상태 관리**가 본질인 스킬은 데이터만으로 표현 불가. 하지만:

- Recipe의 ParamSlots를 통해 **스펙 파싱은 통일**됨
- 향후 ActionExecutor에 새 EffectType을 추가하면 **점진적 전환 가능**
- Quantum에서도 이런 스킬은 전용 System으로 처리하게 될 것 (현재 구조와 동일)

---

## 7. 리스크

| 리스크 | 대응 |
|--------|------|
| Recipe 동작이 기존과 다를 수 있음 | 결정론적 시뮬레이션이므로 seed 고정 후 Before/After 비교 가능 |
| 삭제된 파일 복원 필요 시 | git history에 전부 남아있음 |
| Param0 오버라이드 누락 | `SimSkillGeneric.ApplyParamOverrides()`에서 SkillParams → Recipe Actions 자동 반영 |
| 신규 아키타입 추가 시 | Recipe에 데이터 1곳 추가 + SkillSpecAdapter.ClassifySkill에 1줄 추가 |

---

## 8. 다음 단계

1. **인게임 테스트**: Recipe 전환된 스킬들의 VFX 타이밍, 데미지 수치 검증
2. **SkillSpecAdapter 정리**: ClassifySkill의 Custom 목록 → Recipe가 있으면 Custom이 아닌 걸로 자동 판별하도록 변경
3. **커스텀 11개 점진적 전환**: ActionExecutor에 새 EffectType 추가하며 하나씩 (라키유, 루키다 우선)
4. **아키타입 클래스 삭제**: Recipe로 완전 대체 확인 후 Archetype/ 폴더 내 파일 삭제
5. **Quantum 이전**: Recipe → QAsset, ActionExecutor → Quantum System

---

## 부록: 스킬 개발 플로우 요약

> 상세 내용은 `skill-system-recipe-refactoring.md`의 "스킬 개발 플로우" 섹션 참조

```
새 스킬 추가?
  └── 기존 EffectType 조합 가능? → YES: Recipe 1곳 추가 (파일 생성 없음)
                                  → NO: 새 EffectType 추가 → Recipe
                                  → 런타임 상태 필수? → Custom/ 에 클래스 생성
```
