# InGame_New 인게임 로직 평가 문서 (2026-03-06)

## 1) 평가 범위와 방법
- 분석 범위: `Assets/_Project/Scripts/InGame_New` 하위 C# 스크립트 전수(72개 파일, 총 12,784 LOC)
- 모듈 구성: `Simulation` 49개(8,085 LOC), `View` 19개(4,100 LOC), `Adapter` 4개(599 LOC)
- 방법:
  - 코드 정적 분석(구조, 상태전이, 데이터 흐름, 경계조건)
  - 핵심 경로 추적(입력→커맨드→시뮬레이션→이벤트→뷰 반영)
  - 결함 위험도 분류(Critical / High / Medium)
- 제약:
  - 기존 프로젝트 문서 내용은 참조하지 않음
  - 본 문서는 스크립트 코드만 근거로 작성

## 2) 전체 평가 요약
- 아키텍처 방향(시뮬레이션/뷰 분리, 커맨드 기반, 결정론 RNG)은 좋음.
- 다만 런타임 안정성을 깨는 Critical 결함이 다수 존재.
- 특히 `PvECampaign 플레이어 수 설정`, `즉시 스킬 이중 발동`, `멀티타일 처리 불일치`, `시뮬레이션 이벤트 ID 불일치`는 실제 플레이에서 오동작/크래시로 이어질 가능성이 높음.

평가 점수(10점 만점):
- 아키텍처: 8.0
- 정확성/안정성: 4.5
- 기능 완성도: 5.0
- 성능/확장성: 7.0
- 종합: 5.6 (리팩터링/버그 수정 선행 필요)

## 3) 강점
- 시뮬레이션 코어 분리 설계가 명확함 (`Simulation/Core/*`, `CommandProcessor`)
- 결정론 기반 RNG 구현이 적절함 (`Simulation/Math/DeterministicRNG.cs`)
- 전투 데이터 구조가 고정 배열 기반이라 GC 부담을 줄이려는 의도가 분명함 (`CombatMatchState`, `StatusEffects`, `Projectiles`)
- 입력을 `GameCommand`로 표준화해 네트워크/리플레이 확장 여지가 있음 (`Simulation/Data/Commands.cs`)

## 4) 주요 이슈 (우선순위 순)

## Critical

### C1. PvECampaign에서 배열 크기와 PlayerCount가 불일치
- 근거:
  - `GameWorld.MaxPlayersForGameMode = { 1, 1, 4 }` (`Simulation/Data/GameWorld.cs:11`)
  - `PvECampaign()`은 `PlayerCount = 2` (`Simulation/Data/GameConfig.cs:128`)
  - `GameWorld.Create()`가 `maxPlayers = world.MaxPlayers`로 배열 생성 (`Simulation/Data/GameWorld.cs:113`, `:115`)
  - 이후 `world.Config.PlayerCount` 기준 루프 사용 (`Simulation/Core/GameLoopSystem.cs:210`)
- 영향:
  - PvE 모드에서 `Players[1]`, `Economies[1]` 접근 시 out-of-range 위험
  - 페이즈 진입/시너지 계산/경제 처리에서 런타임 예외 가능

### C2. 즉시 시전 스킬이 1회 시전당 2번 실행되는 구조
- 근거:
  - 즉시 스킬 분기에서 `skill.Execute(...)` 즉시 호출 (`Simulation/Combat/SkillSystem.cs:76-77`)
  - 같은 유닛 상태를 `CastingSkill`로 유지 (`Simulation/Combat/SkillSystem.cs:76`)
  - 다음 틱 `TickCasting()`에서 `unit.SkillCastTimer--` 후 다시 `skill.Execute(...)` 호출 (`Simulation/Combat/SkillSystem.cs:95`, `:114`)
- 영향:
  - 즉시형 스킬 데미지/힐/CC 과적용
  - 밸런스 붕괴 및 디버깅 난이도 급상승

### C3. 멀티타일 유닛 보드 로직이 일부 경로에서 단일타일로 처리됨
- 근거:
  - 회수 시 anchor 1칸만 비움 (`Simulation/Board/BoardSystem.cs:173-174`)
  - Swap 경로 내부 `SetUnitPosition()`이 보드 1칸만 점유 처리 (`Simulation/Board/BoardSystem.cs:341`, `:354`)
  - `SwapBenchToBoard()`도 anchor 위주 처리 (`Simulation/Board/BoardSystem.cs:397`, `:410`)
- 영향:
  - 풋프린트 잔상(유령 점유), 충돌 판정 불일치, 배치 불가 타일 발생
  - 전투 진입 전 보드 상태 오염

### C4. 전투 유닛 생성 시 멀티타일 중복 스폰 가능
- 근거:
  - `SpawnTeamUnits()`가 `world.BoardSize` 전체 슬롯을 순회해 스폰 (`Simulation/Combat/CombatSetupSystem.cs:33`)
  - 슬롯→좌표 변환 후 앵커 타일 여부 검증 없이 `state.UnitCount++` 진행 (`Simulation/Combat/CombatSetupSystem.cs:45` 이후)
- 영향:
  - 멀티타일 1개가 여러 CombatUnit으로 복제될 위험
  - 체력/타겟팅/사망 판정 왜곡

### C5. 스킬 시전 이벤트의 ID 의미가 시뮬레이션과 뷰 사이에서 불일치
- 근거:
  - 시뮬레이션은 `PushUnitCastSkill(unit.SourceEntityId, ...)`로 board EntityId를 보냄 (`Simulation/Combat/SkillSystem.cs:80-82`, `:116-118`)
  - 뷰는 이를 combatId로 가정해 `FindCombatView(casterId)` 조회 (`View/Combat/CombatViewManager.cs:117`)
  - 브릿지에서도 동일 이벤트 전달 (`View/AutoChessViewBridge.cs:184`, `:187`)
- 영향:
  - 스킬 VFX/원소 연출이 누락되거나 비결정적으로 실패
  - 전투 피드백 신뢰도 저하

## High

### H1. 입력/커맨드가 PlayerIndex를 0으로 하드코딩
- 근거:
  - `WithdrawUnit(0, ...)`, `MoveUnit(0, ...)` (`View/Board/BoardInputHandler.cs:355`, `:372`)
  - 보드 점유 조회도 `BoardSlots[0]` 고정 (`View/Board/BoardInputHandler.cs:420`, `:460`)
  - 벤치 드롭도 `PlaceUnit(0, ...)` (`View/UI/BenchUnitSlot.cs:228`)
- 영향:
  - 관전/멀티 보드 전환 시 잘못된 플레이어 보드 조작
  - Competitive 모드 확장 시 기능 불량

### H2. Preparation 그리드 표시 조건이 설계 의도와 불일치 가능
- 근거:
  - `_combatHeight = _boardHeight`, `playerBoardHeight = _boardHeight / 2` (`View/Board/BoardGridView.cs:42-43`)
  - Preparation 표시에서 `row < _boardHeight` (`View/Board/BoardGridView.cs:85`)
- 영향:
  - `_boardHeight`를 전투 전체 높이로 쓰는 현재 주석/초기화 의도 기준으로는, 준비 페이즈에서 전투 영역 전체가 표시될 가능성 높음

### H3. 시너지/아이템 데이터 어댑터가 실효 기능을 비활성 상태로 주입
- 근거:
  - 아이템 스펙을 빈 배열로 주입 (`Adapter/AutoChessSpecAdapter.cs:23`)
  - 시너지 티어 효과를 빈 배열로 주입 (`Adapter/AutoChessSpecAdapter.cs:137`)
- 영향:
  - 시스템은 켜져 있어도 실제 전투 보정 효과가 없음
  - 기획 대비 동작 불일치

### H4. Campaign/Competitive UI가 실제 기능 대부분 TODO 상태
- 근거:
  - `CampaignAutoChessUI.cs` 다수 TODO (`View/UI/CampaignAutoChessUI.cs:17`, `:51`, `:59`, `:67`)
  - `CompetitiveAutoChessUI.cs` 다수 TODO (`View/UI/CompetitiveAutoChessUI.cs:26`, `:69`, `:76`, `:85`)
- 영향:
  - 모드 확장 시 UI-시뮬레이션 연동 불완전
  - QA 가능한 기능 범위가 제한됨

## Medium

### M1. 이벤트 큐 오버플로우 시 묵시적으로 이벤트 유실
- 근거:
  - `MaxEvents = 128` (`Simulation/Data/SimulationEvents.cs:76`)
  - 초과 시 `return`으로 무시 (`Simulation/Data/SimulationEvents.cs:89`)
- 영향:
  - 전투가 복잡한 프레임에서 뷰 동기화 누락 가능

### M2. `GameWorld.GetUnit()`가 invalid entity에 대해 보호 없음
- 근거:
  - `FindUnitIndex` 결과 검증 없이 `return ref Units[index]` (`Simulation/Data/GameWorld.cs:213`, `:216`)
- 영향:
  - UI/입력 경계에서 stale entity가 들어오면 즉시 예외 가능

### M3. 타일 FX 생성 시 `WaitForCompletion()` 사용으로 프레임 스톨 가능
- 근거:
  - 풀 생성 `createFunc`에서 동기 인스턴스화 (`View/Board/TileEffectManager.cs:243`)
- 영향:
  - 첫 생성 구간에서 히치 발생 가능

## 5) 개선 우선순위 (실행 순서 제안)
1. `C1` 수정: `GameModeType.PvECampaign`의 `MaxPlayers`/`PlayerCount` 일관화
2. `C2` 수정: 즉시 시전 스킬의 상태머신 분기 정리(1회 실행 보장)
3. `C3`/`C4` 수정: 멀티타일 anchor 규칙 단일화(보드/전투 모두)
4. `C5` 수정: 이벤트 payload ID 계약(CombatId vs EntityId) 명시/통일
5. `H1` 수정: 입력 경로 전부 `PlayerIndex` 기반으로 치환
6. `H3` 수정: 시너지/아이템 스펙 매핑 실제 구현
7. `H2` 검증/수정: Preparation 표시 규칙을 `playerBoardHeight` 기준으로 정리
8. `M1`~`M3` 보강: 이벤트 큐 정책, 안전한 조회 API, 비동기 FX preload

## 6) 결론
- 현재 `InGame_New`는 구조적으로는 좋은 기반을 갖췄지만, 실제 서비스 기준으로는 **핵심 전투/모드 안정성 결함(Critical) 우선 해소가 필수**다.
- 특히 `PvECampaign`, `즉시 스킬`, `멀티타일`, `이벤트 ID` 4축을 먼저 정리하지 않으면 QA 단계에서 재현 난이도 높은 버그가 반복될 가능성이 높다.
