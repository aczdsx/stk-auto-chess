# 이펙트코드 시스템 분석

> 관련 파일:
> - `InGame/EffectCode/EffectCodeBase.cs`
> - `InGame/EffectCode/EffectCodeContainer.cs`
> - `InGame/EffectCode/EffectCodeContainerTeam.cs`
> - `InGame/EffectCode/Character/EffectCodeStatBase.cs` (1257줄)
> - `InGame/EffectCode/Character/EffectCodeCharacterBase.cs` (656줄)
> - `InGame/EffectCode/Character/EffectCodeBuffDebuffBase.cs`
> - `InGame/EffectCode/EffectCodePoolManager.cs`
> - `InGame/EffectCode/EffectCodeHelper.cs`

---

## 1. 클래스 계층 구조

```
EffectCodeBase (abstract)
│
├── EffectCodeStatBase (abstract)
│   │   - CalcOrder 기반 스탯 계산
│   │   - 64비트 플래그 시스템
│   │   - Fixed/Percent 스탯 수정자
│   │
│   ├── EffectCodeCharacterBase (abstract)
│   │   │   - 스킬 쿨타임/활성화
│   │   │   - 이벤트 콜백 (OnUpdate, OnAttack, OnSkill 등)
│   │   │   - 스킬 애니메이션 연동
│   │   │
│   │   ├── EffectCodeBuffDebuffBase (abstract)
│   │   │   ├── EffectCodeBuffBase → 버프 구현체들
│   │   │   │   └─ EffectCodeBuffAtkUp, EffectCodeBuffDefUp, ...
│   │   │   └── EffectCodeDebuffBase → 디버프 구현체들
│   │   │       └─ EffectCodeDebuffSlow, EffectCodeDebuffPoison, ...
│   │   │
│   │   ├── 스킬 구현체 (Skills/)
│   │   │   ├── Player/ → 플레이어 스킬
│   │   │   └── Monster/ → 몬스터 스킬
│   │   │
│   │   ├── CC 구현체 (CrowdControls/)
│   │   │   └─ EffectCodeCrowdControlAirborne, Stun, Freezing, ...
│   │   │
│   │   ├── 패시브 (Passive/)
│   │   │   ├── JobPassive/ → 직업별 패시브
│   │   │   └── SkillPassive/ → 스킬 연계 패시브
│   │   │
│   │   ├── 시너지 (Synergy/) → 팀 시너지 효과
│   │   ├── 스탯 (Stats/) → 스탯 수정 코드 (30+개)
│   │   └── 근접 버프 (NearlyBuff/)
│   │
│   └── EffectCodeGameBase
│       ├── ChapterRule/ → 챕터 전용 규칙
│       └── CommanderSkill/ → 지휘관 스킬
│
└── EffectCodeTileBase → 타일 전용 이펙트코드
```

---

## 2. EffectCodeType (이펙트코드 분류)

```csharp
public enum EffectCodeType : byte
{
    Character,      // 캐릭터 스킬/패시브
    Buff,           // 양성 상태이상
    Debuff,         // 음성 상태이상
    Stat,           // 스탯 수정자
    Tile,           // 타일 효과
    Game,           // 전역/팀 효과
    CrowdControl,   // 군중 제어
    Item            // 전투 아이템
}
```

---

## 3. EffectCodeContainer (컨테이너)

### 역할
- 엔티티(캐릭터, 타일, 팀)에 부착된 모든 이펙트코드를 관리
- 등록, 제거, 조회, 타입/플래그별 분류

### 핵심 자료구조

```csharp
class EffectCodeContainer
{
    List<EffectCodeBase> effectCodes;                                    // 전체 이펙트코드
    Dictionary<EffectCodeType, List<EffectCodeBase>> effectCodesDividedByType;   // 타입별 분류
    Dictionary<EffectCodeInheritFlag, List<EffectCodeStatBase>> effectCodesDividedByFlag; // 플래그별 분류
}
```

### 핵심 메서드

| 메서드 | 동작 |
|--------|------|
| `AddOrMergeEffectCode(codeInfo, source)` | 같은 ID가 있으면 Merge, 없으면 새로 생성 후 Add |
| `RemoveEffectCode(effectCode)` | 이펙트코드 제거 |
| `RemoveEffectCode(codeId)` | ID로 제거 |
| `RemoveEffectCodesBySource(source)` | 소스(캐릭터)의 모든 이펙트코드 제거 |
| `GetEffectCodesByType(type)` | 타입별 조회 |
| `GetEffectCodesByFlag(flag)` | 플래그별 조회 |
| `SetDirtyFlag(effectCode)` | 스탯 재계산 필요 표시 |

### 등록/병합 흐름

```
AddOrMergeEffectCode(codeInfo, source)
  │
  ├─ 같은 codeId의 기존 코드가 있는가?
  │   ├─ YES → effectCode.Merge(codeInfo, source)
  │   │        (스택 추가, 지속시간 갱신 등)
  │   └─ NO  → EffectCodePoolManager.Get(codeId)
  │            → effectCode.Initialize(codeInfo, container, source)
  │            → effectCodes에 추가
  │            → 우선순위 정렬
  │            → 타입/플래그별 Dictionary에 등록
  │
  └─ DirtyFlag 설정 → 스탯 재계산
```

---

## 4. 64비트 플래그 시스템 (EffectCodeInheritFlag)

각 이펙트코드는 자신이 영향을 주는 스탯과 구독하는 이벤트를 64비트 플래그로 선언한다.

### 스탯 플래그 (28비트)

| 플래그 | 스탯 |
|--------|------|
| HP | 최대 체력 |
| AD | 물리 공격력 |
| DEF | 물리 방어력 |
| ADReduce / ADPierce | 물관 / 물리 관통 |
| AP / APReduce / APPierce | 마공 / 마방 / 마관 |
| RecoveryHP | HP 자연 회복 |
| MoveSpeed | 이동 속도 |
| CriticalProb / CriticalDamageRate | 치명타 확률 / 배율 |
| DoubleCriticalProb / DoubleCriticalDamageRate | 더블 크리 확률 / 배율 |
| AttackSpeed | 공격 속도 |
| AttackRange / AttackRangeShape | 사거리 / 공격 형태 |
| SkillDamageRate / SkillCooltimeRate | 스킬 데미지 / 쿨감 |
| AttackDamageRate / TakenDamageRate | 최종 데미지 / 받는 데미지 |
| GivenHealRate / TakenHealRate | 힐량 / 받힐 |
| CrowdControlImmune | CC 면역 |
| PureDamageProb | 순수 데미지 확률 |
| BlockingProb / AvoidProb / HitProb | 블록 / 회피 / 명중 |

### 이벤트 플래그 (23비트)

| 플래그 | 콜백 메서드 | 시점 |
|--------|------------|------|
| UseOnUpdate | `OnUpdate(float dt)` | 주기적 호출 |
| UseOnCooltime | `OnCooltime(float dt)` | 쿨타임 감소 |
| UseOnAttack | `OnAttack()` | 일반 공격 시 |
| UseOnAttackEnd | `OnAttackEnd(target)` | 공격 완료 시 |
| UseOnSkill | `OnSkill(skillCode)` | 스킬 사용 후 |
| UseOnCombatStart | `OnCombatStart()` | 전투 시작 시 |
| UseOnKill | `OnKill(dead)` | 적 처치 시 |
| UseOnDamaged | `OnDamaged(DamageInfo, attacker, isPure)` | 피격 시 |
| UseOnHealed | `OnHealed(amount, isPure)` | 힐 받을 때 |
| UseOnCritical | `OnCritical(target)` | 크리티컬 히트 시 |
| UseOnDead | `OnDead(DeathInfo)` | 사망 시 |
| UseIsReadyToActivate | `IsReadyToActivate()` | 스킬 준비 체크 |
| UseModifyDamageAmount | `ModifyDamageAmount(double)` | 데미지 수정 |
| UseModifyHealAmount | `ModifyHealAmount(double)` | 힐량 수정 |
| UseModifyShieldAmount | `ModifyShieldAmount(double)` | 쉴드량 수정 |
| UseAddSkillCooltime | `AddSkillCooltime(float)` | 쿨타임 조정 |
| UseModifyDamageTestFlags | `GetDamageTestFlags()` | 데미지 판정 플래그 |
| UseOnHpChange | `OnHpChange()` | HP 변동 시 |
| UseOnCanceledCC | `OnCanceledCC(attacker, ccType)` | CC 면역 시 |
| UseOnCharacterDragging | `OnCharacterDragging()` | 드래그 중 |
| UseOnCharacterDraggingEnd | `OnCharacterDraggingEnd()` | 드래그 종료 |
| UseOnFlowStateStageReadyStart | `OnFlowStateStageReadyStart()` | Ready 상태 시작 |
| UseIsUseNormalAttack | `IsUseNormalAttack()` | 일반공격 사용 여부 |

### 어트리뷰트 기반 플래그 자동 등록

```csharp
[AssignEffectCodeFlag(EffectCodeInheritFlag.UseOnUpdate)]
public virtual void OnUpdate(float dt) { }

[AssignEffectCodeFlag(EffectCodeInheritFlag.UseOnKill)]
public virtual void OnKill(CharacterController dead) { }
```

이펙트코드가 메서드를 override하면 `[AssignEffectCodeFlag]` 어트리뷰트를 통해 자동으로 해당 플래그가 등록된다. 컨테이너는 플래그 기반으로 호출 대상을 필터링하여 불필요한 호출을 방지한다.

---

## 5. 스킬 쿨타임/활성화 흐름

### 쿨타임 관리

```
EffectCodeCharacterBase
  │
  ├─ CoolTimeDurationTime  // 전체 쿨타임 (초)
  ├─ CoolTimeElapsedTime   // 현재 경과 시간
  │
  ├─ waitCooltimeElapsedTime  // 쿨타임 배칭용 경과
  ├─ cooltimePendingTime      // 배칭 간격 (InGameCalculator.EffectCodeCooltimePendingTime)
  │
  └─ OnCooltime(float dt):
      waitCooltimeElapsedTime += dt
      if (waitCooltimeElapsedTime > cooltimePendingTime)
          CoolTimeElapsedTime += waitCooltimeElapsedTime
          waitCooltimeElapsedTime = 0
```

### 활성화 흐름

```
CharacterController.ManagedUpdate(dt)
  │
  ├─ state.CharacterStateRunning(dt) → CanCallEffectCodeActivate?
  │
  └─ YES → 이펙트코드 순회:
      effectCode.IsReadyToActivate()
        │
        ├─ 쿨타임 완료?
        ├─ 사일런스 아닌가?
        └─ 기타 조건 충족?
            │
            └─ YES → effectCode.Activate()
                ├─ 타겟 확인/설정
                ├─ 사운드 재생
                └─ owner.AddNextState<CharacterStateSkill>(this)
```

---

## 6. 업데이트 배칭

이펙트코드의 OnUpdate와 OnCooltime은 성능을 위해 매 프레임이 아닌 주기적으로 호출된다.

```
updatePendingTime = InGameCalculator.EffectCodeUpdatePendingTime
cooltimePendingTime = InGameCalculator.EffectCodeCooltimePendingTime

매 프레임:
  waitUpdateElapsedTime += dt
  if (waitUpdateElapsedTime > updatePendingTime)
      OnUpdate(waitUpdateElapsedTime)  // 누적된 시간으로 호출
      waitUpdateElapsedTime = 0

// updatePendingTime이 0이면 매 프레임 호출
// CC(에어본 등)는 updatePendingTime = 0으로 설정하여 항상 매 프레임 호출
```

---

## 7. 버프/디버프 스택 관리

### BuffStackData

```csharp
class BuffStackData
{
    int sourceCodeId;        // 누가 적용했는가 (스킬 ID)
    float duration;          // 지속 시간
    double value;            // 효과 값 (% 증가분 등)
    float elapsedTime;       // 경과 시간
    IEffectCodeSource source; // 적용한 캐릭터
}
```

### 스택 규칙

```
Merge 호출 시:
  │
  ├─ 같은 sourceCodeId의 스택이 있는가?
  │   ├─ YES → 기존 스택 갱신 (지속시간/값 교체, 경과시간 리셋)
  │   └─ NO  → 새 스택 추가 (중첩)
  │
  └─ 서로 다른 스킬이 같은 버프를 적용하면 → 중첩 (additive)
     같은 스킬이 다시 적용하면 → 갱신 (refresh)
```

### 만료 처리

```
OnUpdate(dt):
  for each stack in _stackDatas:
      stack.AddDeltaTime(dt)
      if (stack.elapsed >= stack.duration):
          remove stack
          release to pool

  if (all stacks removed):
      RemoveFromContainer()  // 이펙트코드 자체 제거
      → DirtyFlag → 스탯 재계산
```

---

## 8. CC(군중 제어) 구현 방식

### CrowdControlType

```csharp
[Flags]
public enum CrowdControlType
{
    Airborne,     // 에어본 (공중)
    KnockBack,    // 넉백
    Stun,         // 스턴
    Slowing,      // 슬로우
    Freezing,     // 빙결
    Silence,      // 침묵 (스킬 사용 불가)
    Entangle,     // 속박 (이동 불가)
    // ... 추가 CC 타입
}
```

### CC 적용 흐름

```
CC 이펙트코드 Initialize:
  owner.AddCrowdControl(CrowdControlType.Airborne)
  → 캐릭터에 CC 플래그 추가

캐릭터 상태 체크:
  characCtrl.NeedToBeCrowdControlState()
  → 활성 CC가 있으면 true
  → CharacterStateCC로 전환

CC 해제:
  이펙트코드 OnUpdate에서 duration 체크
  → 만료 시 owner.RemoveCrowdControl(type)
  → OnPreRemoved() 호출
  → CC 상태에서 Idle로 복귀
```

### 에어본 예시 (EffectCodeCrowdControlAirborne)

```
특징:
  - IsRemoveWithSource = false (시전자가 죽어도 유지)
  - updatePendingTime = 0 (매 프레임 업데이트)
  - 3단계 애니메이션:
    Phase 1: 상승 (0.2초)
    Phase 2: 부유 (duration 동안, 미세 진동)
    Phase 3: 하강 (0.2초)
  - Merge 시: 재적용하면 높이 1.25배로 증가
```

---

## 9. 스탯 계산 공식

### CalcOrder 기반 다단계 계산

```
EffectCodeStatBase에서 각 스탯을 CalcOrder(0~9) 단계로 계산:

result = basicStat

For i = 0 to 9:
    fixedSum = Σ(GetIncrementFixedXXX() for codes with CalcOrder == i)
    percentSum = Σ(GetIncrementPercentXXX() for codes with CalcOrder == i)
    result = (result + fixedSum) × (1 + percentSum)

return result
```

**예시: HP 계산**
```
기본 HP = 100
CalcOrder 0: 아이템 +50 Fixed = (100 + 50) × 1.0 = 150
CalcOrder 1: 버프 +20% Percent = (150 + 0) × 1.2 = 180
CalcOrder 1: 버프 +30 Fixed = 포함되어 → (150 + 30) × 1.2 = 216
CalcOrder 2: 시너지 +10% = (216 + 0) × 1.1 = 237.6
```

### CalcOrder 배분

| CalcOrder | 용도 |
|-----------|------|
| 0 | 기본 스탯 (아이템, 레벨 보너스) |
| 1 | 버프/디버프 (BuffDebuffBase.CalcOrder = 1) |
| 2+ | 시너지, 특수 효과 |

### 데미지 계산 공식

```csharp
// InGameCalculator.CalculateDefaultDamage(ad, ap, target, attacker)

1. 순수 데미지 체크:
   if (attacker.PureDamageTest())
       return ap >= 0 ? ap : ad;  // 방어 무시

2. 방어력 계산:
   def = target.ADReduce × (1 - attacker.ADPierce)
   res = target.APReduce × (1 - attacker.APPierce)

3. 데미지 계산:
   damage = 0
   if (def >= 0) damage += ad × 50 / (50 + def)
   if (res >= 0) damage += ap × 50 / (50 + res)

4. return damage
```

### 쿨타임 감소 공식

```csharp
// InGameCalculator.CalculateCooltimeRate(skillCooltimeRate)
return 1 - (MaxCooltime × (1 - (1 / (1 + skillCooltimeRate))))

// skillCooltimeRate가 높을수록 쿨타임이 짧아짐 (수확체감)
```

---

## 10. 스킬 구현 패턴

### 기본 구조 ([UseEffectCodeIds] 어트리뷰트)

```csharp
[UseEffectCodeIds(CodeId)]                    // ID 등록 (소스 생성용)
public partial class EffectCodeSkillAura : EffectCodeCharacterBase
{
    private const int CodeId = 401012;        // 고유 스킬 ID

    // 스킬 파라미터 (EffectCodeInfo에서 파싱)
    private ObfuscatorFloat cooltime;
    private ObfuscatorFloat power;
    private ObfuscatorInt splashRange;
    private AttackRangeShape _splashShapeType;
    private ObfuscatorFloat splashPower;

    // 런타임 상태
    private ObfuscatorFloat elapsedTime;
    private bool isReadyToActivate;
    private bool isSkillActivated;
}
```

### EffectCodeInfo 데이터 파싱

```csharp
public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container, IEffectCodeSource source)
{
    base.Initialize(codeInfo, container, source);

    // 인덱스 기반 파라미터 추출:
    cooltime = codeInfo.GetCodeStatToFloat(0);        // index 0: 쿨타임
    power = codeInfo.GetCodeStatToFloat(1) * 0.01f;   // index 1: 데미지 배율
    splashRange = codeInfo.GetCodeStatToInt(2);        // index 2: 범위
    _splashShapeType = (AttackRangeShape)codeInfo.GetCodeStatToInt(3); // index 3: 형태
    splashPower = codeInfo.GetCodeStatToFloat(4) * 0.01f; // index 4: 범위 데미지 배율
}
```

### 스킬 실행 흐름

```
1. OnUpdate(dt) → elapsedTime 누적 → 쿨타임 도달 → isReadyToActivate = true
2. IsReadyToActivate() → true 반환
3. Activate() → 타겟 확인 → owner.AddNextState<CharacterStateSkill>(this)
4. CharacterStateSkill → 스킬 애니메이션 재생
5. 애니메이션 이벤트:
   ├─ OnSkillExecute(index, total) → 데미지 적용, AoE 처리
   ├─ OnSkillVFX(index) → VFX 재생
   └─ OnSkillAnimationEnd() → isSkillActivated = false, Idle 복귀
```

---

## 11. 오브젝트 풀링

```
EffectCodePoolManager
  │
  ├─ Get(codeId) → 풀에서 인스턴스 반환 (없으면 새로 생성)
  └─ Return(effectCode) → 풀로 반환

BuffStackData: GenericPool<BuffStackData>.Get() / Release()
List 임시: ListPool<T>.Get(out var list) → using으로 자동 반환
```

---

## 12. 팀 이펙트코드 (EffectCodeContainerTeam)

- `InGameManager.TeamEcc` 에서 관리
- 시너지 효과, 지휘관 스킬 등 팀 전체에 적용되는 이펙트코드
- `AllianceType` 별로 분리 관리 (플레이어 시너지 vs 적 시너지)
- 타일 점유/해제 이벤트와 연동

---

## 13. 값 난독화 (Obfuscator)

모든 중요 수치는 `ObfuscatorFloat` / `ObfuscatorInt`로 래핑:

```csharp
protected ObfuscatorFloat value;   // XOR 인코딩
protected ObfuscatorInt count;

// 투명하게 사용 가능 (연산자 오버로딩):
value = 100f;         // 자동 인코딩
float x = value;      // 자동 디코딩
elapsedTime += dt;    // 연산 지원
```

목적: 메모리 스캔 기반 치팅 방지
