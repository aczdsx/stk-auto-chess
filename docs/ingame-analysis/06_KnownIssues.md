# 현행 구조 문제점 및 개선 포인트

> 인게임 로직 개편 시 참고할 현행 아키텍처의 구조적 특성과 개선 기회를 정리한다.

---

## 1. God Object 문제

### InGameObjectManager (44KB)

가장 큰 매니저 파일로, 아래 책임이 모두 한 클래스에 집중:
- 캐릭터 생명주기 관리 (스폰/제거)
- 진영별 캐릭터 컬렉션 관리
- 타겟 검색 알고리즘
- 범위 판정
- 프레임 업데이트 루프
- 타겟 라인 렌더링
- 드래그 관련 로직

**개선 방향**: 역할별 분리 고려
- `CharacterRegistry` - 캐릭터 등록/조회
- `TargetingSystem` - 타겟 검색/범위 판정
- `CharacterFactory` - 스폰/제거

### InGameTouchManager (49KB)

인게임 내 가장 큰 파일. 모든 터치 입력 처리가 집중:
- 캐릭터 드래그 배치
- 전투 아이템 드래그
- 터치 영역 판정
- UI 상호작용

**개선 방향**: 입력 모드별 Strategy 패턴 분리

---

## 2. 강결합 지점

### CharacterController ←→ InGameObjectManager 순환 의존

```
CharacterController.FindTarget()
  → InGameObjectManager.GetOptimalAttackTarget()

InGameObjectManager.ManagedUpdate()
  → character.ManagedUpdate()
```

캐릭터가 매니저를 직접 호출하고, 매니저가 캐릭터를 직접 호출하는 양방향 의존.

**개선 방향**:
- 이벤트/콜백 기반 디커플링
- 타겟팅을 별도 서비스로 추출하여 CharacterController에 주입

### 싱글턴 의존 (Instance)

대부분의 매니저가 `SingletonMonoBehaviour<T>.Instance`로 접근:
```
InGameManager.Instance
InGameMainFlowManager.Instance
InGameObjectManager.Instance
InGameRandomManager.Instance
InGameSynergyManager.Instance
InGameCommanderManager.Instance
SpecDataManager.Instance
```

캐릭터, 이펙트코드, FlowState 등 모든 곳에서 전역 접근. 테스트와 모킹이 어려움.

**개선 방향**:
- 생성자/초기화 시점에 의존성 주입
- 인터페이스 기반 추상화

---

## 3. 상태 머신 구조

### StatePriority 숫자 불일치

```csharp
public enum StatePriority
{
    Idle = 0,
    Ready = 1,
    Attack = 2,
    Skill = 3,
    CC = 4,
    Move = 5,
    Dead = 10,
    Buff = 96,
    Groggy = 99,
    Knockback = 98,
}
```

- 0~10 구간과 96~99 구간의 큰 갭 → 의도가 불명확
- Buff(96), Knockback(98), Groggy(99)는 일반 상태와 다른 용도(병렬/시각 효과)인데 같은 enum에 혼재
- 우선순위 높은 숫자가 우선인지, 낮은 숫자가 우선인지 코드를 읽어야 파악 가능

**개선 방향**:
- 병렬 상태(Buff, Groggy)는 별도 레이어로 분리
- 우선순위 값에 명확한 의미 부여 (또는 비교 메서드 추출)

### CharacterStateRunningResult 플래그 혼재

`CharacterStateRunning()` 메서드가 두 가지 역할:
1. 상태 로직 실행 (부작용: 상태 전환, 타겟 검색)
2. 이후 처리 플래그 반환

반환값에 따라 `ManagedUpdate`에서 이펙트코드 호출 여부가 결정되는데, 이 연결이 암시적.

**개선 방향**: 상태 로직과 제어 흐름 결정을 분리

---

## 4. 이펙트코드 시스템

### EffectCodeInfo 인덱스 기반 파라미터

```csharp
cooltime = codeInfo.GetCodeStatToFloat(0);
power = codeInfo.GetCodeStatToFloat(1) * 0.01f;
splashRange = codeInfo.GetCodeStatToInt(2);
```

각 스킬마다 인덱스 0~N의 의미가 다름. 문서 없이는 파악 불가.

**개선 방향**:
- 명명된 파라미터 구조체
- 또는 스킬별 전용 EffectCodeInfo 타입

### 클래스 수 폭증

`EffectCode/Character/Impls/` 하위에 100개 이상의 구현 클래스:
- 각 스킬/버프/디버프/CC마다 별도 클래스
- 패턴이 유사한 구현체가 많음 (쿨타임 관리, 데미지 적용, 범위 계산)

**개선 방향**:
- 데이터 드리븐 방식으로 공통 로직 추출
- 구성(Composition) 패턴으로 기능 조합

### CalcOrder 관리 어려움

스탯 계산에서 CalcOrder(0~9) 의존:
- 어떤 이펙트코드가 어떤 CalcOrder를 사용하는지 한눈에 파악 불가
- 잘못된 CalcOrder 배정 시 스탯 계산 오류 발생
- 디버깅이 어려움

---

## 5. 그리드 시스템

### A* 미활용

`InGameTile`에 A* 관련 필드(`G`, `H`, `cameFrom`)가 있으나, 실제로는 BFS 주로 사용. 사용하지 않는 코드가 타일 구조에 포함되어 있음.

### 그리드 크기 하드코딩 경향

```csharp
[SerializeField] private int2 _gridSize; // 임시. Spec에서 받아오거나 다른 방법으로 변경 필요
```

코드 주석에 임시 표시가 있음. 스펙 데이터로부터 동적 생성 필요.

---

## 6. 메모리/성능

### 값 난독화 오버헤드

모든 중요 수치가 `ObfuscatorFloat`/`ObfuscatorInt`로 래핑:
- 매 접근마다 XOR 연산
- 프레임 당 수천~수만 회 발생
- 프로파일링으로 실제 영향도 측정 필요

### 리스트 할당

일부 코드에서 임시 리스트를 `new List<>()` 로 생성:
```csharp
// InGameStage.GraduallyChangeBoardColor
List<InGameTileView> tileViews = new List<InGameTileView>();
tileViews.AddRange(_tileViews.Where(...));
```

인게임 루프 내에서는 `ListPool<T>` 사용이 일반적이나, 일부 빠진 곳이 있음.

### SetOccupied/SetUnoccupied 이벤트 비용

타일 점유/해제 시 3종의 이펙트코드 컨테이너를 순회:
1. 타일 자체의 이펙트코드
2. 게임 레벨 이펙트코드
3. 팀 이펙트코드

이동이 빈번한 전투 상황에서 비용이 누적될 수 있음.

---

## 7. FlowState 코드 중복

스테이지/던전/PVP 등 모드별 FlowState가 대부분 유사한 구조:
- Ready: 캐릭터 스폰 → 배치 → UI
- Combat: Unlock → 승패 체크
- Clear: 별점 → 서버 전송 → 결과 팝업
- Fail: 결과 처리

각 모드마다 거의 동일한 코드가 반복되고, 모드별 차이점만 다름.

**개선 방향**:
- Template Method 패턴으로 공통 흐름 추출
- 모드별 차이점만 override

---

## 8. 네이밍 불일치

- `CharactorStateBuff` / `CharactorStateGroggy` → "Character"가 아닌 "Charactor"로 오타
- `EffectCodeDebffSilence` / `EffectCodeDebffAirborne` → "Debuff"가 아닌 "Debff"로 오타
- `EffectCodeDebuffPoision` → "Poison"이 아닌 "Poision"으로 오타

---

## 9. 타입 안전성

### StateBase.SetStateData(object data)

```csharp
public virtual void SetStateData(object data) { }
```

`object` 타입으로 데이터를 전달하여 컴파일 타임 타입 체크 불가. 런타임 캐스팅에 의존.

**개선 방향**: 제네릭 `StateBase<TData>` 도입

### EffectCodeInfo 인덱스 접근

`GetCodeStatToFloat(int index)` → 인덱스 범위 초과 시 런타임 에러

---

## 10. 개편 시 핵심 고려사항 요약

| 영역 | 현행 문제 | 개선 방향 |
|------|----------|----------|
| **매니저 크기** | InGameObjectManager(44K), TouchManager(49K) God Object | 역할별 분리 |
| **의존성** | 싱글턴 전역 접근, 순환 의존 | DI, 인터페이스 추상화 |
| **상태 머신** | 우선순위 갭, 병렬/직렬 혼재 | 레이어 분리, 명확한 규칙 |
| **이펙트코드** | 인덱스 기반 파라미터, 100+ 클래스 | 데이터 드리븐, 구성 패턴 |
| **FlowState** | 모드별 코드 중복 | Template Method |
| **타입 안전** | object 캐스팅, 인덱스 접근 | 제네릭, 명명된 파라미터 |
| **네이밍** | 오타 다수 | 리네이밍 (개편 시 일괄) |
| **그리드** | A* 미활용, 크기 하드코딩 | 불필요 코드 정리, 동적 생성 |
