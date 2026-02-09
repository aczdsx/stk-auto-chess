# 매니저 시스템 분석

> 관련 파일: `InGame/Managers/*.cs`

---

## 1. 매니저 목록 및 크기

| 매니저 | 파일 크기 | 역할 |
|--------|----------|------|
| `InGameObjectManager` | 44KB | 캐릭터 생명주기, 타겟팅, 범위 검색 |
| `InGameTouchManager` | 49KB | 터치 입력 처리, 드래그 배치 |
| `InGameSynergyManager` | 22KB | 시너지 계산 및 적용 |
| `InGameBattleItemComponent` | 14KB | 전투 아이템 사용 |
| `InGameCommanderManager` | 13KB | 지휘관 스킬, 카메라 |
| `InGameStatistics` | 12KB | 전투 통계, MVP |
| `InGameMainFlowManager` | 11KB | 메인 루프, 흐름 상태 전환 |
| `InGameVfxManager` | 11KB | VFX 관리 |
| `InGameManager` | 10KB | 게임 오케스트레이터 |
| `InGameRandomManager` | 8KB | 시드 기반 RNG |
| `InGameResourceHolder` | 4KB | 리소스 캐싱 |
| `InGameCalculator` | 2KB | 데미지/쿨타임 공식 |

---

## 2. InGameObjectManager (캐릭터 관리)

> `Managers/InGameObjectManager.cs` (44KB)

인게임에서 가장 큰 매니저. 모든 캐릭터의 생명주기와 필드 관리를 담당.

### 캐릭터 컬렉션

```
진영별 캐릭터 리스트:
  ├─ Player 리스트
  ├─ Enemy 리스트
  ├─ Neutral 리스트
  ├─ Wall 리스트
  └─ BattleItem 리스트

업데이트용 별도 리스트:
  ├─ charactersInPlaygroundForUpdate (플레이어 업데이트 순서)
  └─ enemiesInPlaygroundForUpdate (적군 업데이트 순서)
```

### 핵심 기능

**캐릭터 스폰/제거**

| 메서드 | 동작 |
|--------|------|
| `AddCharacterToField(statData, coordinate, alliance, stateType, hasSkill, hpBarType)` | 캐릭터 생성, 타일 배치, 스킬 로딩 |
| `RemoveCharacterFromField(controller)` | 캐릭터 제거, 타일 해제, 뷰 정리 |

**타겟 검색**

| 메서드 | 동작 |
|--------|------|
| `GetOptimalAttackTarget(attacker)` | 최적 타겟 (거리 + HP 기반) |
| `GetNearestTargetOnce(attacker)` | 가장 가까운 적 (1회) |
| `GetNearestTargetByBFS(attacker)` | BFS 기반 최근접 적 |
| `GetNearestEnemiesInRange(center, range, shape, result)` | 범위 내 적 리스트 |

**범위 판정**

| 메서드 | 동작 |
|--------|------|
| `IsInRange(attacker, target)` | 공격 사거리 내 판정 |

**상태 조회**

| 메서드 | 동작 |
|--------|------|
| `GetCharacterList(allianceType)` | 진영별 캐릭터 리스트 |
| `GetAllAliveOnlyCharacters(allianceType, outList)` | 생존 캐릭터만 |
| `IsCheckAllPlayerCharacterAlive()` | 플레이어 전원 생존 체크 |

**프레임 업데이트**

```
ManagedUpdate(dt):
  for each player character:
      character.ManagedUpdate(dt)
  for each enemy character:
      character.ManagedUpdate(dt)
```

---

## 3. InGameMainFlowManager (메인 루프)

> `Managers/InGameMainFlowManager.cs` (11KB)

상세 분석: [01_BattleFlow.md](01_BattleFlow.md) 참조

### 요약

- Unity `Update()` / `LateUpdate()` 루프 관리
- 우선순위 기반 핸들러 시스템
- FlowState 전환 관리 (큐 기반)
- 게임 속도 조절 (`fastForwardRate`, `Time.timeScale`)
- 일시정지/재개
- 백그라운드 복귀 시 deltaTime 보정 (> 0.5f → 모듈로)

---

## 4. InGameSynergyManager (시너지)

> `Managers/InGameSynergyManager.cs` (22KB)

### 시너지 타입

```
SynergyType:
  ├─ 속성 시너지 (character_element_type)
  │   └─ 불, 물, 풍, 토, 빛, 어둠 등
  └─ 성좌 시너지 (character_stella_type)
      └─ Guardian, Striker, Esper, Oracle, Sharpshooter, Ghost 등
```

### 시너지 계산 흐름

```
1. 현재 필드의 플레이어 캐릭터 수집
2. 각 캐릭터의 속성/성좌 타입 집계
3. 시너지 등급 결정 (1개 = Bronze, 2개 = Silver, ...)
4. InGameManager.AddSynergyTeamOnce() 호출
   → EffectCodeContainerTeam에 시너지 이펙트코드 등록
5. 시너지 UI 업데이트
6. 캐릭터 HP바에 시너지 효과 표시
```

### 시너지 갱신 시점

- Ready 상태에서 캐릭터 배치 변경 시
- Combat 시작 시 (최종 확정)

---

## 5. InGameCommanderManager (지휘관)

> `Managers/InGameCommanderManager.cs` (13KB)

### 역할

- 지휘관(Commander) 스킬 관리
- 인게임 카메라 제어
- 지휘관 스킬 쿨타임/활성화

### 카메라 제어

```csharp
InGameCommanderManager.Instance.InGameCamera.SetCameraSize(size, position, duration)
```

---

## 6. InGameRandomManager (RNG)

> `Managers/InGameRandomManager.cs` (8KB)

### 시드 기반 결정론적 RNG

```
서버에서 받은 seed
  → InGameManager.SetSessionIdAndRandomSeed(sessionId, seed)
  → InGameRandomManager.ResetRandomSeedGenerator(seed)
  → 글로벌 랜덤 타입별 시드 생성
```

### GlobalRandomType

```csharp
enum GlobalRandomType
{
    // 용도별 독립 RNG (리플레이 결정론 보장)
    Damage,         // 데미지 랜덤
    Critical,       // 크리티컬 판정
    EffectCode,     // 이펙트코드 확률
    // ...
    MAX
}
```

### 사용 패턴

```csharp
Random random = InGameRandomManager.Instance.GetRandom(GlobalRandomType.Damage);
int randomValue = random.Next(0, 100);
```

---

## 7. InGameCalculator (공식)

> `Managers/InGameCalculator.cs` (2KB)

### 스펙 기반 상수

| 프로퍼티 | 소스 | 설명 |
|----------|------|------|
| `EffectCodeUpdatePendingTime` | `SpecOptionCache` | 이펙트코드 업데이트 주기 |
| `EffectCodeCooltimePendingTime` | `SpecOptionCache` | 쿨타임 업데이트 주기 |
| `MaxCooltime` | `SpecOptionCache` | 최대 쿨감율 |
| `CrowdControlSlowRate` | `SpecOptionCache` | CC 슬로우 비율 |
| `RegenHPPendingTime` | `SpecOptionCache` | HP 자연회복 주기 |
| `MinDamageRate` | `SpecOptionCache` | 최소 데미지 비율 |

### 데미지 공식

```
damage = AD × 50/(50 + DEF) + AP × 50/(50 + RES)
  - DEF = target.ADReduce × (1 - attacker.ADPierce)
  - RES = target.APReduce × (1 - attacker.APPierce)
  - 순수 데미지: 방어 무시, ap >= 0 ? ap : ad
```

### 쿨타임 감소 공식

```
cooltimeRate = 1 - (MaxCooltime × (1 - (1 / (1 + skillCooltimeRate))))
  - 수확체감 곡선 (쿨감이 높을수록 효율 감소)
```

---

## 8. InGameStatistics (통계)

> `Managers/InGameStatistics.cs` (12KB)

### 추적 데이터

- 캐릭터별 총 데미지
- 캐릭터별 받은 데미지
- 캐릭터별 힐량
- 킬 수

### MVP 선정

```csharp
int mvpId = InGameStatistics.Instance.GetMvpID();
// 가장 높은 데미지를 가한 플레이어 캐릭터
```

---

## 9. InGameVfxManager (VFX)

> `Managers/InGameVfxManager.cs` (11KB)

### 역할

- VFX 프리팹 풀링
- VFX 생성/재생/반환
- 투사체 VFX 관리

### 투사체 흐름

```
CharacterStateAttack.AnimationEvent
  → 투사체 VFX 생성
  → 타겟까지 이동 애니메이션
  → 도달 시 데미지 적용
  → VFX 풀로 반환
```

---

## 10. InGameTouchManager (터치 입력)

> `Managers/InGameTouchManager.cs` (49KB)

인게임에서 가장 큰 파일. Ready 상태에서의 캐릭터 드래그 배치를 주로 담당.

### 주요 기능

- 캐릭터 터치/드래그 감지
- 드래그 중 고스트 프리뷰 표시
- 캐릭터 위치 교환 (드래그 앤 드롭)
- 터치 영역 판정
- 전투 아이템 드래그 배치

---

## 11. InGameBattleItemComponent (전투 아이템)

> `Managers/InGameBattleItemComponent.cs` (14KB)

### 역할

- 전투 아이템 사용 (다이너마이트, 슈퍼노바 등)
- 아이템 드래그 배치
- 아이템 효과 적용 (이펙트코드 기반)

---

## 12. InGameResourceHolder (리소스)

> `Managers/InGameResourceHolder.cs` (4KB)

### 역할

- 인게임에서 사용하는 프리팹 캐싱
- HP바, 텍스트, 버프/디버프 아이콘 등 공용 리소스 참조

### 캐싱 대상

```
HpBarView → 체력바 프리팹
InGameText → 플로팅 텍스트 프리팹
InGameBuffDebuff → 버프/디버프 아이콘 프리팹
```

---

## 13. 매니저 초기화 순서

```csharp
// InGameManager.InitializeInGameComponents()에서 순서대로:
1. InGameVfxManager.Initialize()           // VFX 풀 준비
2. InGameHpBarViewPool.Initialize()        // HP바 풀 준비
3. InGameTextViewPool.InitializePool()     // 텍스트 풀 준비
4. InGameBuffDebuffPool.Initialize()       // 버프/디버프 UI 풀 준비
5. InGameObjectManager.Initialize()        // 캐릭터 관리 초기화
6. InGameCommanderManager.Initialize()     // 지휘관 시스템 초기화
7. InGameSynergyManager.Initialize()       // 시너지 시스템 초기화
8. InGameSynergyUI.ClearPreviousSynergyStates() // 시너지 UI 리셋
```

### 정리 순서

```csharp
// InGameManager.EndInGame()에서:
1. InGameMainFlowManager.StopInGameMainLoop()
2. InGameCommanderManager.Clear()
3. InGameObjectManager.Clear()
4. InGameTextViewPool.ReleasePool()
5. InGameHpBarViewPool.Clear()
6. InGameBuffDebuffPool.Clear()
7. InGameVfxManager.Clear()
8. InGameStatistics.Clear()
9. InGameSynergyManager.Clear()
10. TeamEcc.Clear()
```

---

## 14. 매니저 간 의존성 맵

```
InGameManager (최상위)
  ├── InGameMainFlowManager (루프)
  │   └── FlowStates → InGameObjectManager, InGameSynergyManager
  ├── InGameObjectManager (캐릭터)
  │   ├── InGameGrid (그리드)
  │   ├── CharacterController
  │   ├── InGameVfxManager (VFX)
  │   ├── InGameHpBarViewPool (HP바)
  │   └── InGameStatistics (통계)
  ├── InGameSynergyManager
  │   └── InGameManager.TeamEcc (팀 이펙트코드)
  ├── InGameCommanderManager
  │   └── InGameCamera (카메라)
  ├── InGameTouchManager
  │   └── InGameObjectManager
  ├── InGameRandomManager
  │   └── (독립 - 시드만 의존)
  └── InGameCalculator
      └── SpecOptionCache (스펙 데이터)
```

### 순환 의존 주의

```
InGameObjectManager ←→ CharacterController
  (ObjectManager가 Character를 관리하고, Character가 ObjectManager를 호출)
  예: character.FindTarget() → InGameObjectManager.GetOptimalAttackTarget()
```
