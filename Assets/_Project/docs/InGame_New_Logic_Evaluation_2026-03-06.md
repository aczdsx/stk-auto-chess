# InGame_New 인게임 로직 평가 문서 (2026-03-06, 검증 반영본)

## 1) 평가 범위와 방법
- 분석 범위: `Assets/_Project/Scripts/InGame_New` 하위 C# 스크립트 72개 전수 (12,784 LOC)
- 모듈 분포: `Simulation` 49개(8,085 LOC), `View` 19개(4,100 LOC), `Adapter` 4개(599 LOC)
- 분석 방법:
  - 코드 경로 추적: 입력 → 커맨드 → 시뮬레이션 → 이벤트 → 뷰 반영
  - 경계조건/상태전이/ID 계약 검증
  - 심각도 분류: Critical / High / Medium
- 제약:
  - 기존 문서 내용 비참조
  - 코드만 근거로 평가

## 2) 전체 요약
- 강점: 시뮬레이션과 뷰 분리, 커맨드 기반 입력, 결정론 RNG 구조는 우수.
- 핵심 리스크: 전투 정확성에 직접 영향 주는 결함 2건(Critical), 전투/입력 일관성 문제 3건(High).
- 재분류 포인트:
  - `PvECampaign PlayerCount 불일치`는 즉시 크래시보다는 잠재 구조 불일치(Medium)
  - `스킬 이벤트 ID 불일치`는 크래시보다 VFX/피드백 누락(High)
  - `WithdrawUnit 멀티타일 문제`는 기능 오류지만 범위 한정(High)

평가 점수(10점):
- 아키텍처: 8.0
- 정확성/안정성: 5.5
- 기능 완성도: 5.5
- 성능/확장성: 6.8
- 종합: 6.0

## 3) 강점
- 시스템 계층 분리가 명확함 (`Simulation/*`, `Adapter/*`, `View/*`)
- RNG/정수 기반 시뮬레이션 설계가 결정론 지향에 적합 (`Simulation/Math/DeterministicRNG.cs`)
- `GameCommand` 표준화로 네트워크/리플레이 확장성 확보 (`Simulation/Data/Commands.cs`)
- 전투 구조체/배열 재사용으로 GC 억제 의도 분명 (`CombatMatchState`, `StatusEffects`, `Projectiles`)

## 4) 주요 이슈

## Critical

### CR1. 즉시 스킬이 2회 실행되는 상태머신 결함
- 근거:
  - 즉시 분기에서 `skill.Execute(...)` 즉시 호출 (`Simulation/Combat/SkillSystem.cs:77`)
  - 동시에 `unit.State = CastingSkill` 유지 (`Simulation/Combat/SkillSystem.cs:76`)
  - 다음 틱 `unit.SkillCastTimer--` 수행 후(`0 -> -1`) `> 0` 조건 미충족으로 실행 블록 진입 (`Simulation/Combat/SkillSystem.cs:95-96`)
  - 결과적으로 `skill.Execute(...)` 재호출 (`Simulation/Combat/SkillSystem.cs:114`)
- 영향:
  - 즉시형 스킬 데미지/힐/CC 과적용
  - 밸런스 및 QA 신뢰도 훼손

### CR2. 멀티타일 유닛 전투 진입 시 중복 스폰 가능
- 근거:
  - `SpawnTeamUnits()`가 `world.BoardSize` 전 슬롯을 순회 (`Simulation/Combat/CombatSetupSystem.cs:33`)
  - 멀티타일 동일 `entityId` 중복 슬롯을 앵커 체크 없이 전부 스폰 경로 진입 (`Simulation/Combat/CombatSetupSystem.cs:60`)
- 영향:
  - 하나의 유닛이 여러 CombatUnit으로 복제
  - 전투 결과 자체 왜곡 (체력/사망/타겟팅)

## High

### H1. `WithdrawUnit`이 멀티타일 풋프린트를 부분 해제
- 근거:
  - 회수 시 anchor 한 칸만 `InvalidId` 처리 (`Simulation/Board/BoardSystem.cs:173-174`)
  - 반면 공용 제거 루틴은 `ClearBoardFootprint` 사용 (`Simulation/Board/BoardSystem.cs:330`)
- 영향:
  - 보드 점유 잔상(유령 타일)로 후속 배치/충돌 판정 오류

### H2. 스킬 시전 이벤트의 ID 계약 불일치(시뮬레이션 vs 뷰)
- 근거:
  - 시뮬레이션이 `SourceEntityId` 전달 (`Simulation/Combat/SkillSystem.cs:80-82`, `:116-118`)
  - 브릿지가 그대로 전달 (`View/AutoChessViewBridge.cs:187`)
  - 뷰는 `combatId`로 가정해 조회 (`View/Combat/CombatViewManager.cs:117`)
  - 원소 해석도 `CombatId == casterId` 비교 (`View/AutoChessViewBridge.cs:241`)
- 영향:
  - 스킬 캐스트 VFX/원소 연출 누락 또는 불안정 동작

### H3. 보드 입력 경로가 PlayerIndex를 0으로 고정
- 근거:
  - `WithdrawUnit(0, ...)`, `MoveUnit(0, ...)` (`View/Board/BoardInputHandler.cs:355`, `:372`)
  - 타일 점유 조회도 `BoardSlots[0]` 고정 (`View/Board/BoardInputHandler.cs:420`, `:460`)
  - 벤치 드래그 배치도 `PlaceUnit(0, ...)` (`View/UI/BenchUnitSlot.cs:228`)
- 영향:
  - 관전/멀티 보드 상황에서 잘못된 보드 조작

## Medium

### M1. PvECampaign의 `PlayerCount`와 `MaxPlayers` 구조 불일치
- 근거:
  - `MaxPlayersForGameMode`에서 PvE 슬롯이 1 (`Simulation/Data/GameWorld.cs:11`)
  - `PvECampaign`은 `PlayerCount = 2` (`Simulation/Data/GameConfig.cs:128`)
  - `Create()`는 `maxPlayers` 기준으로 배열 생성 (`Simulation/Data/GameWorld.cs:123`)
  - 동시에 `AlivePlayerCount = config.PlayerCount` (`Simulation/Data/GameWorld.cs:109`)
- 영향:
  - 즉시 크래시 지점이라기보다 상태/루프 계약 불일치로 잠재 위험

### M2. Preparation 그리드 표시 조건의 의도 불명확
- 근거:
  - `_combatHeight = _boardHeight`, `playerBoardHeight = _boardHeight / 2` (`View/Board/BoardGridView.cs:42-43`)
  - Preparation 표시에서 `row < _boardHeight` 조건 사용 (`View/Board/BoardGridView.cs:85`)
- 영향:
  - 설정값 해석에 따라 준비 페이즈 노출 범위가 의도와 다를 수 있음

### M3. `WaitForCompletion()` 동기 로딩 사용 지점이 전투 중에도 존재
- 근거:
  - 투사체 VFX 생성 시 동기 인스턴스화 (`View/Combat/CombatViewManager.cs:299`)
  - 타일 FX 풀 생성 시 동기 인스턴스화 (`View/Board/TileEffectManager.cs:243`)
- 영향:
  - 실전 전투 중 프레임 히치 가능

### M4. 이벤트 큐 초과 시 이벤트 유실
- 근거:
  - `MaxEvents = 128` (`Simulation/Data/SimulationEvents.cs:76`)
  - 초과 시 무시 (`Simulation/Data/SimulationEvents.cs:89`)
- 영향:
  - 특정 프레임에서 뷰-시뮬레이션 피드백 누락

### M5. `GetUnit()`의 invalid ID 보호 부재
- 근거:
  - 인덱스 검증 없이 `ref Units[index]` 반환 (`Simulation/Data/GameWorld.cs:213`, `:216`)
- 영향:
  - stale entity 입력 시 즉시 예외 가능

### M6. 기능 미완성 현황(결함이라기보다 구현 상태)
- 근거:
  - Campaign/Competitive UI 다수 TODO (`View/UI/CampaignAutoChessUI.cs`, `View/UI/CompetitiveAutoChessUI.cs`)
  - 어댑터에서 아이템/시너지 효과 매핑 미구현 (`Adapter/AutoChessSpecAdapter.cs:23`, `:137`)
- 영향:
  - 모드 완성도/검증 범위 제한

### M7. `SwapBenchToBoard` 중복 대입(코드 품질 이슈)
- 근거:
  - 동일 슬롯 대입이 2회 존재 (`Simulation/Board/BoardSystem.cs:415`, `:419`)
- 영향:
  - 기능 영향은 크지 않지만 코드 신뢰도 저하

## 5) 우선 조치 순서
1. `CR1` 수정: 즉시 스킬 경로에서 단일 실행 보장
2. `CR2` 수정: 전투 스폰에 멀티타일 앵커 체크 추가
3. `H1` 수정: `WithdrawUnit`을 풋프린트 기반 해제로 통일
4. `H3` 수정: 입력/커맨드 경로를 `PlayerIndex` 기반으로 통일
5. `H2` 수정: 이벤트 ID 계약(CombatId vs EntityId) 명시 및 통일
6. `M3` 개선: 전투 중 동기 로딩 제거(사전 preload/pool warmup)
7. `M1` 정리: PvE 모드의 플레이어 모델/배열 모델 일치화

## 6) 결론
- 핵심 수정 포인트는 `즉시 스킬 2회 실행`과 `멀티타일 중복 스폰`이며, 이 두 항목은 전투 결과 정확성을 직접 훼손하므로 최우선 대응이 필요하다.
- 나머지 이슈는 모드 확장성/연출 완성도/성능 안정성 관점에서 단계적으로 정리하는 것이 적절하다.
