# 캐릭터 시스템 분석

> 관련 파일:
> - `InGame/Character/Controller/CharacterController.cs` (Core, Combat, Stat 부분 클래스)
> - `InGame/Character/CharacterStateBase.cs`
> - `InGame/Character/CharacterStates/*.cs`
> - `InGame/Character/StatData/CharacterStatData.cs`

---

## 1. CharacterController 구조

`CharacterController`는 `IEffectCodeSource` 인터페이스를 구현하며, 부분 클래스(partial class)로 분리되어 있다.

### 핵심 프로퍼티

| 분류 | 프로퍼티 | 타입 | 설명 |
|------|----------|------|------|
| 식별 | `CharacterUId` | `int` | 인스턴스 고유 ID (증가) |
| 식별 | `CharacterId` | `int` | 캐릭터 정의 ID (스펙) |
| 식별 | `SpecCharacter` | `ISpecCharacterInfo` | 캐릭터 스펙 데이터 |
| 위치 | `CurrentTile` | `InGameTile` | 현재 점유 타일 |
| 위치 | `Position` | `Vector2` | 논리적 위치 |
| 위치 | `ViewPosition` | `Vector2` | 시각적 위치 (에어본 등 차이 발생) |
| 전투 | `Target` | `CharacterController` | 현재 공격 대상 |
| 전투 | `AllianceType` | `AllianceType` | Player / Enemy / Neutral / Wall |
| 전투 | `CurrentHp` | `double` | 현재 체력 |
| 전투 | `IsAlive` | `bool` | 생존 여부 |
| 상태 | `_currState` | `CharacterStateBase` | 현재 실행 중인 상태 |
| 상태 | `_nextState` | `CharacterStateBase` | 다음 전환할 상태 |
| 이펙트 | `ecc` | `EffectCodeContainer` | 이펙트코드 컨테이너 |

### AllianceType

```csharp
public enum AllianceType
{
    Player,       // 플레이어 진영
    Enemy,        // 적 진영
    Neutral,      // 중립 (양쪽 모두 타겟 가능)
    Wall,         // 벽 (이동 불가 장애물)
    BattleItem    // 전투 아이템
}
```

---

## 2. 캐릭터 생명주기

### 초기화

```
InGameObjectManager.AddCharacterToField()
  │
  ├─ CharacterStatData 생성 (ID, 레벨, 스탯 배율 등)
  ├─ CharacterController 인스턴스 생성
  ├─ Initialize() 호출:
  │   ├─ 기본 스탯 설정 (HP, AD, DEF, AP 등)
  │   ├─ 타일 점유 (tile.SetOccupied)
  │   ├─ 스킬 이펙트코드 등록 (hasSkill = true)
  │   ├─ 초기 상태 설정 (보통 CharacterStateReady)
  │   └─ 뷰 생성 (SpriteCharacterView, HpBarView)
  │
  └─ InGameObjectManager의 캐릭터 리스트에 등록
```

### 프레임 업데이트

```
CharacterController.ManagedUpdate(float dt)
  │
  ├─ 1. 상태 전환 처리
  │   └─ _nextState가 있으면:
  │       ├─ _currState.StateEnd(false)
  │       ├─ StatePool.Return(_currState)
  │       ├─ _currState = _nextState
  │       ├─ _nextState = null
  │       ├─ _currState.StateInit(this)
  │       └─ _currState.StateStart()
  │
  ├─ 2. 현재 상태 실행
  │   └─ result = _currState.CharacterStateRunning(dt)
  │       → CharacterStateRunningResult 플래그 반환
  │
  ├─ 3. 플래그 기반 이펙트코드 처리
  │   ├─ CanCallEffectCodeOnUpdate → 이펙트코드 OnUpdate(dt) 호출
  │   ├─ CanCallEffectCodeOnCooltime → 스킬 쿨타임 OnCooltime(dt) 호출
  │   ├─ CanCallEffectCodeActivate → IsReadyToActivate() 체크 → Activate()
  │   └─ CanCallMove → 시각적 위치 업데이트
  │
  ├─ 4. 공격 쿨다운 업데이트
  │
  └─ 5. HP 자연 회복
```

### 사망 처리

```
CharacterController.GetDamaged(DamageInfo, attacker)
  │
  ├─ HP 감소
  ├─ OnDamaged 이펙트코드 호출
  ├─ HP ≤ 0 이면:
  │   ├─ IsAlive = false
  │   ├─ attacker의 OnKill 이펙트코드 호출
  │   └─ AddNextState<CharacterStateDead>()
  │
  └─ HP바 업데이트
```

---

## 3. 상태 머신

### CharacterStateBase 인터페이스

```csharp
public abstract class CharacterStateBase : StateBase
{
    protected CharacterController characCtrl;
    public abstract StatePriority StatePriority { get; }
    protected bool isBlockingChangeState = false;

    public override void StateInit(object owner);                    // 초기화
    public override void StateStart();                               // 시작
    public virtual CharacterStateRunningResult CharacterStateRunning(float dt); // 실행
    public override void StateEnd(bool isForced);                   // 종료
    public virtual void AnimationEventCallback(AnimationKey, AnimationEventKey); // 애니메이션 이벤트
}
```

### 우선순위 시스템 (StatePriority)

```
Dead (10) ─────────── 최고 (절대 우선)
Groggy (99) ────────── 특수 (병렬 상태)
Knockback (98) ──────── 특수 (병렬 상태)
Buff (96) ───────────── 특수 (병렬 상태)
Move (5) ────────────── 이동
CC (4) ──────────────── 군중제어
Skill (3) ───────────── 스킬 사용
Attack (2) ──────────── 일반 공격
Ready (1) ───────────── 준비 (잠금)
Idle (0) ────────────── 대기
```

### 상태 전환 규칙

```csharp
// AddNextState<T>(stateData) 호출 시:
if (_nextState != null && newState.StatePriority < _nextState.StatePriority)
{
    // 새 상태의 우선순위가 더 낮음 → 무시
    StatePool.Return(newState);
    return null;
}
else
{
    // 새 상태의 우선순위가 같거나 높음 → 교체
    _nextState = newState;
    return newState;
}
```

**예시**: Attack(2) 상태에서 Skill(3)이 준비되면 → 스킬이 우선 (3 > 2)

### isBlockingChangeState

일부 상태는 `isBlockingChangeState = true`로 설정하여 상태 전환을 차단한다:
- `CharacterStateCC`: CC 지속 중 다른 상태로 전환 불가
- `CharacterStateDead`: 사망 처리 중 전환 불가

---

## 4. CharacterStateRunningResult 플래그

```csharp
[Flags]
public enum CharacterStateRunningResult
{
    None = 0,
    CanCallEffectCodeOnUpdate = 1 << 0,   // 이펙트코드 OnUpdate 허용
    CanCallEffectCodeOnCooltime = 1 << 1, // 스킬 쿨타임 감소 허용
    CanCallEffectCodeActivate = 1 << 2,   // 스킬 활성화 체크 허용
    CanCallMove = 1 << 3,                 // 이동 허용

    // 편의 조합
    CanCallEffectCodeOnUpdateAndOnCooltime = 0x03,
    CanCallAllWithoutMove = 0x07,
    CanCallAllWithoutActivate = 0x0B,
    CanCallAll = 0x0F,
}
```

### 상태별 반환 플래그

| 상태 | 반환 플래그 | 설명 |
|------|------------|------|
| Ready | `CanCallMove` | 이동만 가능, 이펙트코드 비활성 |
| Idle | `CanCallAllWithoutMove` ~ `CanCallAll` | 스캔/공격 준비 |
| Attack | `CanCallEffectCodeOnUpdateAndOnCooltime` | 공격 중엔 스킬 활성화 불가 |
| Skill | `CanCallEffectCodeOnUpdateAndOnCooltime` | 스킬 실행 중엔 다른 활성화 불가 |
| CC | `CanCallEffectCodeOnUpdateAndOnCooltime` | CC 중에도 버프/디버프 틱은 동작 |
| Dead | `None` | 모든 것 중단 |
| Move | `CanCallAllWithoutActivate` | 이동 중엔 스킬 활성화 불가 |

---

## 5. 각 상태 상세 분석

### 5.1 CharacterStateIdle (대기)

```
┌─ 진입: Ready → Idle (전투 시작 시), 또는 Attack/Move/CC 종료 후
│
├─ 동작:
│   ├─ 0.1초 간격으로 타겟 스캔
│   ├─ CC 체크 → CC 상태로 전환
│   ├─ 타겟 없으면 FindTarget() 호출
│   ├─ 타겟이 죽었으면 null 처리
│   └─ 타겟이 범위 내:
│       ├─ YES → CharacterStateAttack
│       └─ NO → MoveToCharacter() → CharacterStateMove
│
└─ 반환: CanCallAllWithoutMove (타겟 탐색 중)
         CanCallAll (범위 체크 후)
```

### 5.2 CharacterStateAttack (일반 공격)

```
┌─ 진입: Idle에서 타겟이 범위 내일 때
│
├─ 동작:
│   ├─ CC 체크 → CC 상태로 전환
│   ├─ 타겟 사망/범위 이탈 → Idle로 복귀
│   ├─ 공격 쿨다운 체크
│   │   └─ 쿨다운 완료 → ATK 애니메이션 시작
│   └─ 애니메이션 이벤트 콜백:
│       ├─ ExecuteStart~ExecuteEnd → 데미지 적용
│       │   ├─ 투사체 있으면 → 투사체 VFX 생성
│       │   └─ 없으면 → 직접 데미지
│       └─ End → 공격 완료, 다음 공격 준비
│
├─ 데미지 계산:
│   └─ CalculateNormalAttackDamage()
│       ├─ AD 또는 AP 기반 (캐릭터 타입에 따라)
│       └─ hitCount > 1이면 데미지 분할
│
└─ 반환: CanCallEffectCodeOnUpdateAndOnCooltime
```

**변형 공격 상태:**

| 상태 | 파일 | 특수 동작 |
|------|------|----------|
| `CharacterStateAttackHealer` | `CharacterStateAttackHealer.cs` | 공격 시 아군 힐 |
| `CharacterStateAttackPierce` | `CharacterStateAttackPierce.cs` | 관통 공격 |
| `CharacterStateAttackEsper` | `CharacterStateAttackEsper.cs` | 에스퍼 전용 공격 |
| `CharacterStateAttackAnimEventDamage` | `CharacterStateAttackAnimEventDamage.cs` | 애니메이션 이벤트 기반 데미지 |
| `CharacterStateAssassinFirstMove` | `CharacterStateAssassinFirstMove.cs` | 암살자 첫 이동 (후방 진입) |

### 5.3 CharacterStateSkill (스킬)

```
┌─ 진입: 이펙트코드 IsReadyToActivate() → Activate()
│         → owner.AddNextState<CharacterStateSkill>(effectCode)
│
├─ 동작:
│   ├─ 스킬 애니메이션 재생
│   ├─ 애니메이션 이벤트 콜백:
│   │   ├─ OnSkillExecute(index, total) → 스킬 데미지/효과
│   │   ├─ OnSkillVFX(index) → VFX 재생
│   │   └─ OnSkillAnimationEnd() → 스킬 종료
│   └─ 스킬 종료 → Idle로 복귀
│
└─ 반환: CanCallEffectCodeOnUpdateAndOnCooltime
```

### 5.4 CharacterStateMove (이동)

```
┌─ 진입: Idle에서 타겟이 범위 밖일 때
│
├─ 동작:
│   ├─ BFS로 다음 이동 타일 계산
│   ├─ 현재 타일 해제, 다음 타일 점유
│   ├─ 이동 애니메이션 (선형 보간)
│   └─ 도착 → Idle로 복귀
│
└─ 반환: CanCallAllWithoutActivate
```

### 5.5 CharacterStateCC (군중 제어)

```
┌─ 진입: 어떤 상태에서든 CC 이펙트코드 적용 시
│
├─ 동작:
│   ├─ isBlockingChangeState = true (상태 전환 차단)
│   ├─ DEAD 애니메이션 재생 (시각적 잠금)
│   ├─ 매 프레임 CC 상태 체크
│   └─ CC 해제 → isBlockingChangeState = false → Idle
│
└─ 반환: CanCallEffectCodeOnUpdateAndOnCooltime
         (CC 중에도 버프/디버프 틱은 동작)
```

### 5.6 CharacterStateDead (사망)

```
┌─ 진입: HP ≤ 0
│
├─ 동작:
│   ├─ isBlockingChangeState = true
│   ├─ DEAD 애니메이션 재생
│   ├─ 현재 타일 점유 해제
│   ├─ 스킬 이펙트 자식 → Playground로 이동 (사망 후에도 유지)
│   └─ 애니메이션 End → InGameObjectManager.RemoveCharacterFromField()
│
└─ 반환: None (모든 것 중단)
```

### 5.7 CharacterStateForceMove (강제 이동)

- 넉백 등 강제 이동 시 사용
- 목표 타일로 이동 후 Idle 복귀

### 5.8 CharactorStateBuff / CharactorStateGroggy (특수 상태)

- 높은 우선순위 (96, 99)
- 시각적 효과만 담당 (실제 로직은 이펙트코드에서)

---

## 6. 상태 전환 다이어그램

```
                    ┌──────────┐
           ┌───────│  Ready   │
           │       └────┬─────┘
           │            │ 전투 시작
           │            ▼
           │       ┌──────────┐
           │  ┌───►│   Idle   │◄────────────────────────────┐
           │  │    └──┬──┬──┬─┘                              │
           │  │       │  │  │                                │
           │  │       │  │  └──── 타겟 범위 밖 ───►┌────────┐│
           │  │       │  │                        │  Move  ├┘
           │  │       │  │                        └────────┘
           │  │       │  │
           │  │       │  └── 타겟 범위 내 ──►┌──────────┐
           │  │       │                     │  Attack  ├──► Idle (완료)
           │  │       │                     └──────────┘
           │  │       │
           │  │       └── 스킬 준비 ──►┌──────────┐
           │  │                       │  Skill   ├──► Idle (완료)
           │  │                       └──────────┘
           │  │
           │  │    ┌──────────┐
           │  └────│    CC    │◄── 모든 상태에서 CC 적용 시
           │       └──────────┘
           │
           │       ┌──────────┐
           └───────│   Dead   │ ← HP ≤ 0 (모든 상태에서)
                   └──────────┘
```

---

## 7. 캐릭터 스탯 시스템 (CharacterController.Stat)

### 기본 스탯

| 스탯 | 약어 | 설명 |
|------|------|------|
| HP | HP | 최대 체력 |
| AD | AD | 물리 공격력 |
| DEF | DEF | 물리 방어력 |
| ADPierce | 물관 | 물리 관통력 |
| AP | AP | 마법 공격력 |
| APReduce | 마방 | 마법 방어력 |
| APPierce | 마관 | 마법 관통력 |
| RecoveryHP | HP 회복 | 자연 회복량 |
| MoveSpeed | 이속 | 이동 속도 |
| AttackSpeed | 공속 | 공격 속도 |
| CriticalProb | 치확 | 크리티컬 확률 |
| CriticalDamageRate | 치피 | 크리티컬 데미지 배율 |
| DoubleCriticalProb | 더블치확 | 더블 크리티컬 확률 |
| DoubleCriticalDamageRate | 더블치피 | 더블 크리티컬 데미지 배율 |
| AttackRange | 사거리 | 공격 사거리 |
| SkillDamageRate | 스킬뎀 | 스킬 데미지 배율 |
| SkillCooltimeRate | 쿨감 | 스킬 쿨타임 감소율 |
| TotalDamageRate | 총뎀 | 최종 데미지 배율 |
| TakenDamageRate | 피뎀 | 받는 데미지 배율 |
| GivenHealRate | 힐량 | 주는 힐 배율 |
| TakenHealRate | 받힐 | 받는 힐 배율 |
| PureDamageProb | 순뎀확 | 순수 데미지 확률 |
| BlockingProb | 블록확 | 블록 확률 |
| AvoidProb | 회피확 | 회피 확률 |
| HitProb | 명중확 | 명중 확률 |

### 스탯 계산 순서

스탯은 `CalcOrder` (0~9) 기반 다단계 계산:

```
기본값 (Base)
  → CalcOrder 0: (기본 + Fixed₀) × (1 + Percent₀)
  → CalcOrder 1: (결과 + Fixed₁) × (1 + Percent₁)
  → ...
  → CalcOrder 9: (결과 + Fixed₉) × (1 + Percent₉)
  = 최종값
```

자세한 내용은 [03_EffectCodeSystem.md](03_EffectCodeSystem.md) 참조.

---

## 8. 타겟팅 시스템

### FindTarget()

```
CharacterController.FindTarget()
  └─ InGameObjectManager.GetOptimalAttackTarget(attacker)
     ├─ 반대 진영의 살아있는 캐릭터 검색
     ├─ 가장 가까운 캐릭터 우선
     └─ 동일 거리면 HP 비율 낮은 캐릭터 우선
```

### IsInRange()

```
InGameObjectManager.IsInRange(attacker, target)
  └─ InGameGrid.IsInRange(attackerTile, targetTile, attackRange, attackRangeShape)
     └─ Manhattan 거리 기반 판정
```

---

## 9. 데미지 처리 흐름

```
공격자: CalculateDamageAmount()
  ├─ AD × power (또는 AP × power)
  ├─ 순수 데미지 확률 체크 → 방어 무시
  ├─ 크리티컬 확률 체크 → 크리티컬 배율 적용
  ├─ 더블 크리티컬 체크 → 추가 배율
  ├─ 스킬 데미지 배율 적용 (isSkill)
  ├─ 최종 데미지 배율 적용
  └─ DamageInfo 구조체 반환

피격자: GetDamaged(DamageInfo, attacker)
  ├─ 회피 확률 체크 → Miss
  ├─ 블록 확률 체크 → 감소
  ├─ 방어력/마방 적용 (InGameCalculator.CalculateDefaultDamage)
  │   └─ damage = AD × 50/(50+DEF) + AP × 50/(50+RES)
  ├─ 받는 데미지 배율 적용
  ├─ 최소 데미지 보정 (MinDamageRate)
  ├─ 무적 체크 (IsPlayerInvincible / IsEnemyInvincible)
  ├─ HP 감소
  ├─ OnDamaged 이펙트코드 호출
  ├─ HP ≤ 0 → 사망 처리
  └─ HP바 업데이트
```
