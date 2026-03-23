# InGame_New 시스템 Weakpoint 분석

**작성일**: 2026-03-23
**목적**: 코드 품질 및 유지보수성 현황 파악
**분석 대상**: `Assets/_Project/Scripts/InGame_New/` (61,472줄, 85+ 파일)
**분석 방법**: 레이어별 정적 분석 + 메트릭 기반 분석 (A+B 조합)

---

## 아키텍처 개요

```
Adapter (6파일)          → 외부 데이터 → 시뮬레이션 구조체 변환
Simulation (50+파일)     → 결정론적 전투 시뮬레이션 (SOD 패턴, GC 최소화)
View (30+파일)           → MonoBehaviour 기반 렌더링, 이벤트 구독 방식
Debug (6파일)            → 리플레이, 프레임 레코더
```

**의존 방향**: Adapter → Simulation ← View (단방향 읽기), View → Simulation (커맨드 전송)

---

## 높은 심각도 (6건)

### 1. SynergyType 하드코딩 상한 — 데이터 누락

| 항목 | 내용 |
|------|------|
| **위치** | `AutoChessSpecAdapter.cs:146` |
| **현상** | `for (synergyTypeInt = 1; synergyTypeInt <= 9; ...)` — ARCANA, ECLIPSE 등 10+ 타입 미등록 |
| **영향** | 해당 시너지 캐릭터가 시너지 보너스를 받지 못함 |
| **수정 방향** | `Enum.GetValues(typeof(SynergyType))`로 순회하거나 enum에 카테고리 Attribute 부착 |

### 2. CombatUnit 구조체 비대화 (55+ 필드)

| 항목 | 내용 |
|------|------|
| **위치** | `Components.cs:496-633` |
| **현상** | 식별자 8, 기본스탯 12, Base스탯 6, 마나 5, 특수 5, 타겟/쿨다운 8, 이동 6, 범위공격 8, CC/스킬 6, 플래그 6 = 55+필드 |
| **영향** | `var unit = state.Units[i]` 실수 시 55필드 전체 복사. 코드 이해 난이도 증가 |
| **수정 방향** | 범위공격 상태(`IsAreaAttacking` + `AreaHit*` 8개)를 `CombatAreaAttackState`로 분리 |

### 3. CombatSetupSystem 유닛 초기화 3중 중복

| 항목 | 내용 |
|------|------|
| **위치** | `CombatSetupSystem.cs` — `SpawnTeamUnits`(L68-153), `SpawnPvEEnemies`(L262-343), `SpawnTutorialUnit`(L346-421) |
| **현상** | 60개 필드 할당 블록이 세 곳에 반복. `SpawnTutorialUnit`은 TODO 주석과 함께 스탯 하드코딩 |
| **영향** | 새 필드 추가 시 세 곳 동기화 필요 — 한 곳 누락 시 silent bug |
| **수정 방향** | `InitCombatUnitDefaults(ref CombatUnit unit, ...)` 헬퍼 메서드로 통합 |

### 4. IdleCombatSetup static mutable 상태

| 항목 | 내용 |
|------|------|
| **위치** | `IdleCombatSetup.cs:28-29` |
| **현상** | `static int _boardWidth, _boardHeight` — `TryAddEnemy()`가 이전 호출의 값을 참조할 수 있음 |
| **영향** | 재진입/씬 전환 시 적군 스폰 범위 오작동 |
| **수정 방향** | static field 제거, `TryAddEnemy()` 매개변수로 보드 크기 전달 |

### 5. ~~StatusEffectSystem.Tick 조건부 힙 할당~~ (수정 완료)

| 항목 | 내용 |
|------|------|
| **위치** | `StatusEffectSystem.cs:10-11` |
| **현상** | ~~hot path에서 `new int[32]` 두 개 조건부 할당~~ |
| **수정** | `static readonly int[] MarkerExpUnits/MarkerExpValues = new int[32]`로 변경 완료 |

### 6. SynergyEffectType.SpellDamagePercent 미구현

| 항목 | 내용 |
|------|------|
| **위치** | `SynergySystem.cs:389-390` |
| **현상** | `case SynergyEffectType.SpellDamagePercent: break;` — 빈 body, 컴파일러 경고 없음 |
| **영향** | 해당 시너지 효과 무동작 |
| **수정 방향** | 구현하거나 `NotSupportedException`으로 명시적 처리 |

---

## 중간 심각도 (12건)

### 아키텍처 경계 위반 (4건)

#### 7. View → Simulation 직접 쓰기

- **위치**: `AutoChessViewBridge.cs:566-569`
- **현상**: 컷씬 종료 시 `world.IsCutscenePlaying = false` 직접 할당
- **원칙 위반**: View는 읽기 전용이어야 하며, `GameCommand.EndCutscene` 커맨드로 처리해야 함

#### 8. GetWorld() 추상화 누수

- **위치**: `ISimulationRunner.GetWorld()` + `AutoChessViewBridge.cs` (20회+ 호출)
- **현상**: View에서 `world.Pool`, `world.CombatMatchStates[0]` 등 시뮬레이션 내부 구조 직접 접근
- **위험**: `GameWorld`가 class이고 필드 모두 public → 실수로 Sim 상태 변경 가능

#### 9. Debug 레이어 타입 캐스트

- **위치**: `AutoChessViewBridge.cs:77,88`
- **현상**: `_runner is LocalSimulationRunner` — 인터페이스 추상화 파괴
- **위험**: 네트워크 어댑터 교체 시 자동 파손

#### 10. DamageSystem → CombatSetupSystem 역방향 의존

- **위치**: `DamageSystem.cs:173-175`
- **현상**: `CombatSetupSystem.CountAliveByTeam` 직접 호출
- **수정 방향**: `CombatMatchState`의 인스턴스 메서드로 이동

### 코드 중복 (4건)

#### 11. SkillAreaHelper Enemy/Ally 순회 6중복

- **위치**: `SkillAreaHelper.cs`
- **현상**: `ForEachEnemyInRadius`, `ForEachAllyInRadius` 등 6개 메서드 — 팀 필터 1줄만 다름
- **수정 방향**: 팀 필터를 파라미터로 받는 단일 메서드로 통합

#### 12. ~~채널링 스킬 보일러플레이트 중복~~ (부분 해소)

- **위치**: `SimSkillAprilBarrage`, `SimSkillOdetteStrike` (나머지는 Recipe 전환 완료)
- **현상**: ~~`SimSkillClayChannel`, `SimSkillBossTankLine` 포함 4곳에서 반복~~ → ClayChannel/BossTankLine은 Recipe 전환으로 삭제됨
- **현재**: `SimSkillGeneric`이 채널링 타이머를 1곳에서 처리. 남은 커스텀 2개(AprilBarrage, OdetteStrike)에만 잔존

#### 13. GetPrimarySkillId 2중 구현

- **위치**: `AutoChessSpecAdapter.cs:115`, `IdleCombatSetup.cs:303`
- **현상**: 동일 메서드가 두 파일에 존재
- **수정 방향**: `AutoChessSpecAdapter`의 것을 `public static`으로 통일

#### 14. SpawnSkillVfx 4중복

- **위치**: `CombatViewManager.cs:604-692`
- **현상**: 4개 메서드가 위치/회전만 다르고 나머지 동일
- **수정 방향**: 내부 헬퍼 메서드로 통합 (30줄+ 제거 가능)

### 안전성 문제 (4건)

#### 15. async void 사용

- **위치**: `AutoChessViewBridge.cs:764,826`
- **현상**: 예외 발생 시 catch 불가, 앱 크래시 위험
- **수정 방향**: `UniTaskVoid` 또는 `.Forget()`으로 통일

#### 16. SimEventQueue 오버플로우 무음 드롭

- **위치**: `SimulationEvents.cs:136`
- **현상**: `MaxEvents=128` 초과 시 이벤트를 경고 없이 버림
- **영향**: 대규모 AoE 전투에서 VFX 누락, 킬 로그 누락 가능
- **수정 방향**: 최소한 에디터에서 `Debug.LogWarning` 추가

#### 17. OnAllBoardViewsReady 람다 구독 미해제

- **위치**: `AutoChessViewBridge.cs:65`
- **현상**: 람다 캡처로 구독 → 해제 불가능
- **영향**: 뷰 재사용 시 소멸된 객체 참조 위험
- **수정 방향**: 명명된 메서드로 교체 후 `OnDestroy`에서 해제

#### 18. RespawnUnit/SpawnUnit 초기화 불일치

- **위치**: `IdleCombatSetup.cs:131 vs 216`
- **현상**: `MoveDuration`, `SkillCastTimer` 명시적 초기화가 `SpawnUnit`에만 존재
- **위험**: `CombatUnit`에 non-zero default 필드 추가 시 `RespawnUnit`만 잘못된 상태

---

## 낮은 심각도 (4건)

### 19. 매직 넘버 산재

| 위치 | 값 | 의미 |
|------|-----|------|
| `DamageSystem.BasicAttack.cs:62` | `/ 8` | 투사체 속도 |
| `CombatSetupSystem.cs:115-116` | `25`, `150` | 크리율/크리배율 기본값 |
| `AutoChessViewBridge.cs:332` | `217323201` | 미사 스킬 SpecId (가장 위험) |
| `CombatViewManager.cs:763` | `30f` | Homing 투사체 속도 (DefaultProjectileSpeed=20f와 불일치) |
| `SynergySystem.cs:27` | `new int[8]` | 보드 유닛 수 상한 (BoardSize=28과 불일치) |

### 20. Components.cs 도메인 미분리

- 893줄에 Board/Combat/Synergy/Item/Champion 15개+ 구조체 혼재
- 도메인별 파일 분리 권장 (`CombatComponents.cs`, `BoardComponents.cs` 등)

### 21. AutoChessUIBase.OnDestroy가 private

- `AutoChessUIBase.cs:490` — 서브클래스에서 `OnDestroy` 재선언 시 베이스 정리 코드 미실행
- 현재는 `OnCleanup()` 훅으로 우회 중이라 실질적 문제 없음

### 22. 이벤트 네이밍 혼재

- Bridge 제거 과정에서 `OnXxxChanged` vs `OnXxxUpdated` 혼용 잔존

---

## 파일 크기 메트릭

### 500줄 이상 파일 (우려 순)

| 파일 | 줄수 | 책임 수 | 평가 |
|------|------|---------|------|
| CombatViewManager.cs | 1,161 | 7 | **분리 필요** — 투사체, VFX, 사운드, 타일이펙트 혼재 |
| AutoChessViewBridge.cs | 1,091 | 6 | **분리 필요** — 슈퍼노바 로직(300줄+) 별도 관리자로 |
| Components.cs | 893 | 15+ | 도메인별 분리 권장 |
| SynergySystem.cs | 879 | 4 | ApplyStatEffect 160줄 switch 분리 권장 |
| BoardInputHandler.cs | 784 | 5 | 보드/고스트/오브젝트 드래그 분리 가능 |
| SkillRecipeRegistry.cs | 577 | 1 | **양호** (단일 책임) |
| UnitView.cs | 564 | 3 | 허용 범위 |
| ClassicAutoChessUI.cs | 527 | 5 | 중간 |
| AutoChessUIBase.cs | 500 | 4 | 허용 범위 |
| AnimKeyframeData.Mob.cs | 1,349 | 1 | **문제 없음** (데이터 전용) |
| AnimKeyframeData.Characters.cs | 1,184 | 1 | **문제 없음** (데이터 전용) |

### 심각도별 분포 요약

| 심각도 | 건수 | 카테고리 |
|--------|------|----------|
| **높음** | 6 | 데이터 누락, 구조체 비대, 초기화 중복, static 상태, GC, 미구현 |
| **중간** | 12 | 경계 위반 4, 코드 중복 4, 안전성 4 |
| **낮음** | 4 | 매직 넘버, 파일 구조, 네이밍 |
| **합계** | **22** | |

---

## 긍정적 측면

모든 것이 문제인 것은 아닙니다. 다음은 잘 설계된 부분입니다.

- **Simulation/View 분리**: 전반적으로 단방향 의존이 잘 지켜짐 (위반은 일부)
- **결정론적 RNG**: `LocalSimulationRunner`와 시뮬레이션 코어에서 일관적 사용 (IdleCombat만 예외)
- **SOD 패턴**: struct 배열 + ref 접근으로 GC 최소화 설계가 잘 되어 있음
- **스킬 Recipe 시스템**: 최근 추가된 확장 가능한 스킬 정의 방식 — 올바른 방향
- **리플레이 시스템**: partial class로 분리하여 프로덕션 코드 오염 최소화
- **UI 훅 패턴**: `AutoChessUIBase`의 `OnInitialize`, `OnCleanup` 등 가상 메서드 패턴이 일관적
- **방어적 에러 처리**: 대부분의 시스템에서 `if (idx < 0) return;` 패턴 사용
