# Idle Combat Mode Implementation Plan

> **For agentic workers:** REQUIRED: Use superpowers:subagent-driven-development (if subagents available) or superpowers:executing-plans to implement this plan. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** BattleReady 씬의 idle 전투를 레거시 InGame에서 InGame_New 시스템으로 이전 — 경량 전투 러너(CombatMatchState + CombatAISystem.Tick 직접 구동)

**Architecture:** GameWorld/GameLoopSystem을 거치지 않고, CombatMatchState만 직접 관리하는 IdleCombatRunner MonoBehaviour. SimEventQueue → IdleCombatViewBridge → CombatViewManager/UnitViewManager로 View 연결. SpawnTutorialUnit 패턴을 참조하여 유닛 동적 생성.

**Tech Stack:** Unity 6, C#, InGame_New Simulation/View 시스템, UniTask

**Spec:** `docs/superpowers/specs/2026-03-12-idle-combat-mode-design.md`

---

## File Structure

| Action | File | Responsibility |
|--------|------|---------------|
| Create | `Scripts/InGame_New/Adapter/Local/IdleCombatRunner.cs` | 경량 전투 러너 MonoBehaviour — 시간 누적, CombatAISystem.Tick 호출, HP 보정, 적 스폰 |
| Create | `Scripts/InGame_New/Adapter/IdleCombatSetup.cs` | CombatMatchState 생성, 캐릭터/몬스터 → CombatUnit 변환, 동적 적 스폰 |
| Create | `Scripts/InGame_New/View/IdleCombatViewBridge.cs` | SimEventQueue → CombatViewManager/UnitViewManager 디스패치, View 생명주기 |
| Modify | `Scripts/UI/BattleReady/BattleReadyMain.cs` | FlowStateLobbyCombat → IdleCombatRunner 호출로 교체 |

---

## Chunk 1: Simulation Layer

### Task 1: IdleCombatSetup — CombatMatchState 생성 및 유닛 스폰

**Files:**
- Create: `Assets/_Project/Scripts/InGame_New/Adapter/IdleCombatSetup.cs`

**참조 파일:**
- `Assets/_Project/Scripts/InGame_New/Simulation/Combat/CombatSetupSystem.cs:28-128` (SpawnTeamUnits 패턴)
- `Assets/_Project/Scripts/InGame_New/Simulation/Combat/CombatSetupSystem.cs:275-336` (SpawnTutorialUnit 패턴)
- `Assets/_Project/Scripts/InGame_New/Simulation/Combat/SkillSystem.cs:12-37` (SetupSkills)
- `Assets/_Project/Scripts/InGame_New/Adapter/AutoChessSpecAdapter.cs` (스펙 변환 패턴)
- `Assets/_Project/Scripts/InGame_New/Simulation/Data/Components.cs` (CombatMatchState, CombatUnit, CombatGrid)

- [ ] **Step 1: IdleCombatSetup 클래스 생성 — CreateMatchState**

```csharp
// Assets/_Project/Scripts/InGame_New/Adapter/IdleCombatSetup.cs
using System.Collections.Generic;
using UnityEngine;

namespace CookApps.AutoChess
{
    /// <summary>
    /// Idle 전투용 CombatMatchState 생성 및 유닛 스폰 유틸리티.
    /// CombatSetupSystem.SpawnTutorialUnit() 패턴을 따름.
    /// </summary>
    public static class IdleCombatSetup
    {
        /// <summary>
        /// 플레이어 캐릭터로 CombatMatchState 생성.
        /// playerA=0 (플레이어), playerB=0xFF (적 AI)
        /// </summary>
        public static CombatMatchState CreateMatchState(
            List<int> playerChampionSpecIds,
            SimEventQueue eventQueue,
            ref DeterministicRNG rng,
            int tickRate)
        {
            var state = CombatMatchState.Create(0, 0, 0xFF);
            state.EventQueue = eventQueue;

            // 플레이어 유닛 배치 (팀 A, row 0-3)
            for (int i = 0; i < playerChampionSpecIds.Count && i < 5; i++)
            {
                int specId = playerChampionSpecIds[i];
                if (!FindEmptyTile(state, 0, 3, ref rng, out int col, out int row))
                    break;
                SpawnUnit(state, specId, col, row, teamIndex: 0, ownerIndex: 0, tickRate);
            }

            state.AliveCountA = CombatSetupSystem.CountAliveByTeam(state, 0);
            state.AliveCountB = 0;

            // 스킬 셋업 (SkillFactory 직접 사용 — GameWorld 불필요)
            SetupSkillsForAllUnits(state);

            return state;
        }

        /// <summary>
        /// 적 유닛 동적 스폰. MaxCombatUnits 초과 시 false 반환.
        /// </summary>
        public static bool TryAddEnemy(
            CombatMatchState matchState,
            int enemyChampionSpecId,
            ref DeterministicRNG rng,
            int tickRate)
        {
            if (matchState.UnitCount >= CombatMatchState.MaxCombatUnits) return false;

            if (!FindEmptyTile(matchState, 4, 7, ref rng, out int col, out int row))
                return false;

            int slotIndex = SpawnUnit(matchState, enemyChampionSpecId, col, row,
                teamIndex: 1, ownerIndex: 0xFF, tickRate);

            SetupSkillForUnit(matchState, slotIndex);

            matchState.AliveCountB = CombatSetupSystem.CountAliveByTeam(matchState, 1);

            return true;
        }

        private static int SpawnUnit(CombatMatchState state, int champSpecId,
            int col, int row, byte teamIndex, byte ownerIndex, int tickRate)
        {
            int combatId = state.NextCombatId++;
            int slotIndex = state.UnitCount++;

            ref var unit = ref state.Units[slotIndex];
            unit.CombatId = combatId;
            unit.SourceEntityId = -1;
            unit.ChampionSpecId = champSpecId;
            unit.StarLevel = 1;
            unit.OwnerIndex = ownerIndex;
            unit.TeamIndex = teamIndex;
            unit.GridCol = (byte)col;
            unit.GridRow = (byte)row;
            unit.SizeW = 1;
            unit.SizeH = 1;
            unit.State = CombatState.Idle;
            unit.IsAlive = true;

            // SpecDataManager에서 스탯 조회
            var charInfo = SpecDataManager.Instance.GetCharacterData(champSpecId);
            if (charInfo != null)
            {
                unit.MaxHP = charInfo.stat_hp;
                unit.CurrentHP = charInfo.stat_hp;
                unit.Attack = charInfo.stat_atk;
                unit.Armor = charInfo.stat_def;
                unit.MagicResist = (int)charInfo.ap_reduce;
                unit.AttackSpeed = Mathf.Max(1, (int)(charInfo.atk_speed * 100));
                unit.AttackRange = charInfo.atk_range > 0 ? charInfo.atk_range : 1;
                unit.MoveSpeed = Mathf.Max(1, (int)(charInfo.move_speed * 100));
                unit.MaxMana = 100;
                unit.CurrentMana = 0;
                unit.CritChance = Mathf.Max(0, (int)(charInfo.crit_rate * 100));
                unit.CritMultiplier = Mathf.Max(0, (int)(charInfo.crit_power * 100));
                if (unit.CritChance <= 0) unit.CritChance = 25;
                if (unit.CritMultiplier <= 0) unit.CritMultiplier = 150;
                unit.ArmorPenetration = Mathf.Clamp((int)(charInfo.stat_atk_pierce * 100), 0, 100);
                unit.MagicPenetration = Mathf.Clamp((int)(charInfo.stat_res_pierce * 100), 0, 100);
                unit.SkillSpecId = AutoChessSpecAdapter.GetPrimarySkillId(charInfo);
                unit.AtkHitDelay = ExtractAtkHitDelay(charInfo.prefab_id, tickRate);
                unit.HasAreaAttack = AreaAttackRegistry.TryGetPattern(champSpecId, out _);
            }
            else
            {
                // 스펙 못 찾으면 기본값 (SpawnTutorialUnit 패턴)
                unit.MaxHP = 100;
                unit.CurrentHP = 100;
                unit.Attack = 10;
                unit.AttackSpeed = 100;
                unit.AttackRange = 1;
                unit.MoveSpeed = 100;
                unit.MaxMana = 100;
                unit.CritChance = 25;
                unit.CritMultiplier = 150;
                unit.AtkHitDelay = 1;
            }

            unit.HitChance = 100;
            unit.CurrentTargetId = CombatUnit.InvalidId;
            unit.AttackCooldown = 0;
            unit.PendingAtkTargetId = CombatUnit.InvalidId;
            unit.PendingAtkTimer = 0;
            unit.MoveTimer = 0;
            unit.MoveDuration = 0;
            unit.SkillCastTimer = 0;

            // 그리드 등록
            state.SetGridMulti(col, row, 1, 1, combatId);

            // UnitSpawned 이벤트
            state.EventQueue?.Push(new SimEvent
            {
                Type = SimEventType.UnitSpawned,
                EntityId = combatId,
                Value0 = champSpecId,
                Col = (byte)col,
                Row = (byte)row,
            });

            return slotIndex;
        }

        private static void SetupSkillsForAllUnits(CombatMatchState state)
        {
            for (int i = 0; i < state.UnitCount; i++)
                SetupSkillForUnit(state, i);
        }

        private static void SetupSkillForUnit(CombatMatchState state, int slotIndex)
        {
            ref var unit = ref state.Units[slotIndex];
            if (unit.SkillSpecId <= 0) return;

            var skill = SkillFactory.Create(unit.SkillSpecId);
            if (skill == null) return;

            if (SkillFactory.TryGetParams(unit.SkillSpecId, out var skillParams))
                skill.Initialize(skillParams);
            else
                skill.Initialize(new SkillParams
                {
                    SkillId = unit.SkillSpecId,
                    PowerPercent = 200,
                    DamageType = DamageType.Magical,
                });

            state.Skills[slotIndex] = skill;
        }

        private static bool FindEmptyTile(CombatMatchState state, int minRow, int maxRow,
            ref DeterministicRNG rng, out int outCol, out int outRow)
        {
            var emptyCols = new int[CombatGrid.Width * (maxRow - minRow + 1)];
            var emptyRows = new int[emptyCols.Length];
            int count = 0;

            for (int r = minRow; r <= maxRow; r++)
            {
                for (int c = 0; c < CombatGrid.Width; c++)
                {
                    int idx = c + r * CombatGrid.Width;
                    if (state.GridTiles[idx] == CombatUnit.InvalidId)
                    {
                        emptyCols[count] = c;
                        emptyRows[count] = r;
                        count++;
                    }
                }
            }

            if (count == 0)
            {
                outCol = 0;
                outRow = 0;
                return false;
            }

            int pick = rng.Range(0, count);
            outCol = emptyCols[pick];
            outRow = emptyRows[pick];
            return true;
        }

        private static int ExtractAtkHitDelay(int prefabId, int tickRate)
        {
            if (prefabId <= 0) return 1;
            int atkKey = AnimKeyframeData.MakeKey(prefabId, false, AnimClipType.ATK);
            if (AnimKeyframeData.ExecuteTimes.TryGetValue(atkKey, out float execTime))
            {
                int frames = (int)(execTime * tickRate + 0.5f);
                return frames > 0 ? frames : 1;
            }
            return 1;
        }
    }
}
```

> **NOTE:** `AutoChessSpecAdapter.GetPrimarySkillId()` 메서드가 private이거나 시그니처가 다를 경우, `AutoChessSpecAdapter`에서 해당 로직을 참조하여 동일한 스펙 프로퍼티에 직접 접근하는 코드로 대체. Reflection 사용 금지 (CLAUDE.md).

- [ ] **Step 2: 컴파일 확인**

Unity Editor에서 `IdleCombatSetup.cs` 컴파일 에러 없는지 확인.
네임스페이스, 타입 참조, SpecDataManager 반환 타입에 맞게 조정.

- [ ] **Step 3: Commit**

```bash
git add Assets/_Project/Scripts/InGame_New/Adapter/IdleCombatSetup.cs
git commit -m "feat(idle-combat): IdleCombatSetup — CombatMatchState 생성 및 유닛 스폰 유틸"
```

---

### Task 2: IdleCombatRunner — 경량 전투 러너

**Files:**
- Create: `Assets/_Project/Scripts/InGame_New/Adapter/Local/IdleCombatRunner.cs`

**참조 파일:**
- `Assets/_Project/Scripts/InGame_New/Adapter/Local/LocalSimulationRunner.cs` (시간 누적 + 틱 패턴)
- `Assets/_Project/Scripts/InGame_New/Simulation/Combat/CombatAISystem.cs` (Tick 호출)
- `Assets/_Project/Scripts/InGame_New/Simulation/Math/DeterministicRNG.cs`

- [ ] **Step 1: IdleCombatRunner MonoBehaviour 작성**

```csharp
// Assets/_Project/Scripts/InGame_New/Adapter/Local/IdleCombatRunner.cs
using System;
using System.Collections.Generic;
using UnityEngine;

namespace CookApps.AutoChess
{
    /// <summary>
    /// BattleReady 씬용 경량 idle 전투 러너.
    /// GameWorld/GameLoopSystem 없이 CombatMatchState + CombatAISystem.Tick()만 직접 구동.
    /// </summary>
    public class IdleCombatRunner : MonoBehaviour
    {
        private CombatMatchState _matchState;
        private DeterministicRNG _rng;
        private SimEventQueue _eventQueue;
        private bool _isRunning;

        private const int TickRate = 30;
        private float _tickAccumulator;
        private const int MaxTicksPerFrame = 3;

        // 적 스폰
        private List<int> _enemySpecIds;
        private float _enemySpawnTimer;
        private int _maxEnemyCount;
        private int _currentEnemyCount;

        // View 이벤트
        public event Action<CombatMatchState> OnTick;
        public event Action OnCombatStarted;
        public event Action OnCombatStopped;

        public CombatMatchState MatchState => _matchState;
        public bool IsRunning => _isRunning;

        public void StartIdleCombat(List<int> playerChampionSpecIds, List<int> enemySpecIds, int maxEnemyCount)
        {
            if (playerChampionSpecIds == null || playerChampionSpecIds.Count == 0) return;

            // SkillFactory 초기화 (이미 초기화된 경우 내부에서 스킵)
            SkillFactory.Initialize(TickRate);

            _eventQueue = new SimEventQueue();
            _rng = new DeterministicRNG((ulong)DateTime.Now.Ticks);

            _matchState = IdleCombatSetup.CreateMatchState(
                playerChampionSpecIds, _eventQueue, ref _rng, TickRate);

            _enemySpecIds = enemySpecIds;
            _maxEnemyCount = maxEnemyCount;
            _currentEnemyCount = 0;
            _enemySpawnTimer = GetRandomSpawnInterval();
            _tickAccumulator = 0f;
            _isRunning = true;

            OnCombatStarted?.Invoke();
        }

        public void StopIdleCombat()
        {
            _isRunning = false;
            _matchState = null;
            _eventQueue = null;
            OnCombatStopped?.Invoke();
        }

        private void Update()
        {
            if (!_isRunning || _matchState == null) return;

            float dt = Time.unscaledDeltaTime;

            // 적 스폰 타이머
            UpdateEnemySpawn(dt);

            // 틱 누적
            float tickInterval = 1f / TickRate;
            _tickAccumulator += dt;

            int ticksThisFrame = 0;
            while (_tickAccumulator >= tickInterval && ticksThisFrame < MaxTicksPerFrame)
            {
                // HP 보정 (사망 방지) — 틱 전
                RestoreLowHPUnits();

                // IsFinished 리셋 (종료 조건 무력화)
                _matchState.IsFinished = false;

                // 시뮬레이션 틱
                CombatAISystem.Tick(_matchState, ref _rng, TickRate);

                // View에 이벤트 발행
                OnTick?.Invoke(_matchState);

                _tickAccumulator -= tickInterval;
                ticksThisFrame++;
            }
        }

        private void UpdateEnemySpawn(float dt)
        {
            if (_enemySpecIds == null || _enemySpecIds.Count == 0) return;
            if (_currentEnemyCount >= _maxEnemyCount) return;

            _enemySpawnTimer -= dt;
            if (_enemySpawnTimer > 0f) return;

            int specIndex = _rng.Range(0, _enemySpecIds.Count);
            int enemySpecId = _enemySpecIds[specIndex];

            if (IdleCombatSetup.TryAddEnemy(_matchState, enemySpecId, ref _rng, TickRate))
            {
                _currentEnemyCount++;
            }

            _enemySpawnTimer = GetRandomSpawnInterval();
        }

        private void RestoreLowHPUnits()
        {
            for (int i = 0; i < _matchState.UnitCount; i++)
            {
                ref var unit = ref _matchState.Units[i];
                if (!unit.IsAlive) continue;

                int threshold = unit.MaxHP / 10;  // MaxHP * 0.1
                if (unit.CurrentHP <= threshold)
                {
                    unit.CurrentHP = unit.MaxHP;
                    unit.State = CombatState.Idle;
                    unit.CurrentTargetId = CombatUnit.InvalidId;
                    unit.AttackCooldown = 0;
                    unit.CCRemainingFrames = 0;
                    unit.ActiveCC = CrowdControlType.None;
                }
            }
        }

        private float GetRandomSpawnInterval()
        {
            // 1~4초 랜덤 (Range는 maxExclusive이므로 401)
            return _rng.Range(100, 401) / 100f;
        }

        private void OnDestroy()
        {
            if (_isRunning) StopIdleCombat();
        }
    }
}
```

- [ ] **Step 2: 컴파일 확인**

Unity Editor에서 컴파일 에러 없는지 확인. `CombatAISystem`, `SkillFactory`, `DeterministicRNG` 등이 `CookApps.AutoChess` 네임스페이스에 있는지 확인.

- [ ] **Step 3: Commit**

```bash
git add Assets/_Project/Scripts/InGame_New/Adapter/Local/IdleCombatRunner.cs
git commit -m "feat(idle-combat): IdleCombatRunner — 경량 전투 러너 MonoBehaviour"
```

---

## Chunk 2: View Layer + Integration

### Task 3: IdleCombatViewBridge — View 연결

**Files:**
- Create: `Assets/_Project/Scripts/InGame_New/View/IdleCombatViewBridge.cs`

**참조 파일:**
- `Assets/_Project/Scripts/InGame_New/View/AutoChessViewBridge.cs:163-349` (DispatchEvent 패턴)
- `Assets/_Project/Scripts/InGame_New/View/Unit/UnitViewManager.cs:142-223` (SyncCombatUnits)
- `Assets/_Project/Scripts/InGame_New/View/Combat/CombatViewManager.cs` (OnCombatStart/OnCombatEnd)

- [ ] **Step 1: IdleCombatViewBridge 작성**

```csharp
// Assets/_Project/Scripts/InGame_New/View/IdleCombatViewBridge.cs
using UnityEngine;

namespace CookApps.AutoChess.View
{
    /// <summary>
    /// Idle 전투용 경량 View 브릿지.
    /// SimEventQueue → CombatViewManager/UnitViewManager 디스패치.
    /// AutoChessViewBridge와 달리 GameWorld에 의존하지 않음.
    /// </summary>
    public class IdleCombatViewBridge : MonoBehaviour
    {
        private IdleCombatRunner _runner;

        private UnitViewManager _unitViewManager;
        private CombatViewManager _combatViewManager;
        private CombatVfxManager _combatVfxManager;

        private const int BoardIndex = 0;

        public void Initialize(
            IdleCombatRunner runner,
            UnitViewManager unitViewManager,
            CombatViewManager combatViewManager,
            CombatVfxManager combatVfxManager = null)
        {
            _runner = runner;
            _unitViewManager = unitViewManager;
            _combatViewManager = combatViewManager;
            _combatVfxManager = combatVfxManager;

            // View 매니저 초기화 (풀 생성 등)
            _unitViewManager?.Initialize();
            _combatViewManager?.Initialize();

            _runner.OnTick += HandleTick;
            _runner.OnCombatStarted += HandleCombatStarted;
            _runner.OnCombatStopped += HandleCombatStopped;
        }

        private void HandleCombatStarted()
        {
            _unitViewManager?.OnCombatStart();
            _combatViewManager?.OnCombatStart();
        }

        private void HandleCombatStopped()
        {
            _combatViewManager?.OnCombatEnd();
            _unitViewManager?.OnCombatEnd();
        }

        private void HandleTick(CombatMatchState matchState)
        {
            // 이벤트 디스패치
            var queue = matchState.EventQueue;
            for (int i = 0; i < queue.Count; i++)
            {
                ref var evt = ref queue.Events[i];
                DispatchEvent(ref evt, matchState);
            }
            queue.Clear();

            // 유닛 뷰 동기화
            _unitViewManager?.SyncCombatUnits(matchState, BoardIndex);
        }

        private void DispatchEvent(ref SimEvent evt, CombatMatchState matchState)
        {
            switch (evt.Type)
            {
                case SimEventType.UnitAttacked:
                {
                    bool isProjectile = (evt.Value1 & 1) != 0;
                    bool isPreTimed = (evt.Value1 & 2) != 0;
                    _combatViewManager?.OnUnitAttacked(
                        evt.EntityId, evt.TargetEntityId, evt.Value0,
                        evt.Flag0, isProjectile, isPreTimed);
                    break;
                }

                case SimEventType.UnitDamaged:
                    _combatViewManager?.OnUnitDamaged(
                        evt.EntityId, evt.Value0, (DamageType)evt.Value1, evt.Flag0);
                    break;

                case SimEventType.UnitDied:
                    // idle 전투에서는 사망 이벤트 무시 (HP 보정으로 사망 방지)
                    break;

                case SimEventType.UnitCastSkill:
                {
                    var element = ResolveElementFromCaster(matchState, evt.EntityId);
                    _combatViewManager?.OnUnitCastSkill(
                        evt.EntityId, evt.TargetEntityId, evt.Value0,
                        element, evt.Flag0, evt.Flag1);
                    break;
                }

                case SimEventType.ProjectileSpawned:
                {
                    int champSpecId = ResolveChampSpecId(matchState, evt.EntityId);
                    _combatViewManager?.OnProjectileSpawned(
                        evt.EntityId, evt.TargetEntityId, evt.ProjType,
                        evt.Col, evt.Row, (sbyte)evt.DirCol, (sbyte)evt.DirRow,
                        champSpecId, evt.Value0, evt.Value1);
                    break;
                }

                case SimEventType.ProjectileMoved:
                    _combatViewManager?.OnProjectileMoved(evt.Value0, evt.Col, evt.Row);
                    break;

                case SimEventType.ProjectileExpired:
                    _combatViewManager?.OnProjectileExpired(evt.Value0);
                    break;

                case SimEventType.ProjectileExploded:
                {
                    var element = ResolveElementFromSkill(evt.Value0);
                    _combatViewManager?.OnProjectileExploded(evt.Col, evt.Row, evt.Radius, element);
                    break;
                }

                case SimEventType.SkillPhaseVfx:
                    _combatViewManager?.OnSkillPhaseVfx(
                        evt.EntityId, evt.Value0, (byte)evt.Value1,
                        (sbyte)evt.DirCol, (sbyte)evt.DirRow);
                    break;

                case SimEventType.SkillRectAreaEffect:
                {
                    var element = ResolveElementFromCaster(matchState, evt.EntityId);
                    _combatViewManager?.OnSkillRectAreaEffect(
                        evt.Col, evt.Row, (sbyte)evt.DirCol, (sbyte)evt.DirRow, element);
                    break;
                }

                case SimEventType.SkillAreaEffect:
                {
                    var element = ResolveElementFromCaster(matchState, evt.EntityId);
                    bool isBox = evt.Value1 != 0;
                    _combatViewManager?.OnSkillAreaEffect(
                        evt.Col, evt.Row, evt.Radius, element, evt.Flag0, isBox);
                    break;
                }

                case SimEventType.UnitMissed:
                    _combatViewManager?.OnUnitMissed(evt.EntityId, evt.TargetEntityId);
                    break;

                case SimEventType.UnitHealed:
                    _combatViewManager?.OnUnitHealed(evt.EntityId, evt.Value0);
                    break;

                case SimEventType.UnitSpawned:
                    // UnitViewManager.SyncCombatUnits에서 자동 처리 (GetOrCreateCombatView)
                    break;

                case SimEventType.StatusEffectAdded:
                    _combatVfxManager?.OnEffectAdded(evt.EntityId, (CombatVfxType)evt.Value0);
                    break;

                case SimEventType.StatusEffectRemoved:
                    _combatVfxManager?.OnEffectRemoved(evt.EntityId, (CombatVfxType)evt.Value0);
                    break;

                case SimEventType.CCAdded:
                    _combatVfxManager?.OnEffectAdded(evt.EntityId, (CombatVfxType)evt.Value0);
                    break;

                case SimEventType.CCRemoved:
                    _combatVfxManager?.OnEffectRemoved(evt.EntityId, (CombatVfxType)evt.Value0);
                    break;

                // UI 전용 이벤트는 idle에서 무시
                case SimEventType.GoldChanged:
                case SimEventType.LevelUp:
                case SimEventType.PlayerEliminated:
                case SimEventType.CombatResult:
                case SimEventType.SynergyUpdated:
                    break;
            }
        }

        // ── 원소 타입 조회 (AutoChessViewBridge 패턴 단순화) ──

        private SynergyType ResolveElementFromCaster(CombatMatchState matchState, int casterId)
        {
            for (int u = 0; u < matchState.UnitCount; u++)
            {
                if (matchState.Units[u].CombatId == casterId ||
                    matchState.Units[u].SourceEntityId == casterId)
                {
                    return GetElementFromCharacterId(matchState.Units[u].ChampionSpecId);
                }
            }
            return SynergyType.NONE;
        }

        private SynergyType ResolveElementFromSkill(int skillSpecId)
        {
            if (skillSpecId <= 0) return SynergyType.NONE;
            // idle에서는 원소 VFX 생략 가능 — 정확한 역추적이 복잡하므로 NONE 반환
            return SynergyType.NONE;
        }

        private int ResolveChampSpecId(CombatMatchState matchState, int combatId)
        {
            for (int u = 0; u < matchState.UnitCount; u++)
            {
                if (matchState.Units[u].CombatId == combatId)
                    return matchState.Units[u].ChampionSpecId;
            }
            return 0;
        }

        private static SynergyType GetElementFromCharacterId(int champId)
        {
            var charInfo = SpecDataManager.Instance?.GetCharacterData(champId);
            return charInfo?.character_element_type ?? SynergyType.NONE;
        }

        private void OnDestroy()
        {
            if (_runner != null)
            {
                _runner.OnTick -= HandleTick;
                _runner.OnCombatStarted -= HandleCombatStarted;
                _runner.OnCombatStopped -= HandleCombatStopped;
            }

            HandleCombatStopped();
        }
    }
}
```

> **NOTE:** `UnitViewManager.Initialize()`와 `CombatViewManager.Initialize()`의 실제 시그니처를 확인하여 파라미터 조정. 이미 초기화된 상태라면 중복 호출 방어 필요.

- [ ] **Step 2: 컴파일 확인**

Unity Editor에서 컴파일 에러 없는지 확인. `CombatViewManager`, `UnitViewManager`, `CombatVfxManager` 메서드 시그니처 매칭 확인.

- [ ] **Step 3: Commit**

```bash
git add Assets/_Project/Scripts/InGame_New/View/IdleCombatViewBridge.cs
git commit -m "feat(idle-combat): IdleCombatViewBridge — 경량 View 브릿지"
```

---

### Task 4: BattleReadyMain 통합

**Files:**
- Modify: `Assets/_Project/Scripts/UI/BattleReady/BattleReadyMain.cs:116-120` (StartInGame 교체)
- Modify: `Assets/_Project/Scripts/UI/BattleReady/BattleReadyMain.cs:471-480` (EndInGame 교체 — OnClickGoToLobby)
- Modify: `Assets/_Project/Scripts/UI/BattleReady/BattleReadyMain.cs:531` (EndInGame 교체 — OnClickStartButtonAsync)

**참조 파일:**
- `Assets/_Project/Scripts/InGame/GameFlowStates/Stage/FlowStateLobbyCombat.cs` (기존 데이터 소스)

- [ ] **Step 1: BattleReadyMain에 IdleCombatRunner 필드 추가 및 StartIdleCombat 호출**

기존 코드:
```csharp
var stageSpecData = SpecDataManager.Instance.GetStageData(currentStageId);
InGameManager.Instance.StartInGame<FlowStateLobbyCombat>(stageSpecData);
```

교체 후:
```csharp
// idle 전투 시작 (InGame_New 경량 러너)
StartIdleCombatAsync().Forget();
```

BattleReadyMain에 추가할 필드/메서드:
```csharp
using CookApps.AutoChess;
using CookApps.AutoChess.View;

private IdleCombatRunner _idleCombatRunner;
private IdleCombatViewBridge _idleCombatViewBridge;

private async UniTaskVoid StartIdleCombatAsync()
{
    // 플레이어 캐릭터 specId 수집 (기존 FlowStateLobbyCombat 패턴)
    var allChars = ServerDataManager.Instance.Character.GetAllCharacters();
    var playerSpecIds = new List<int>();
    // seq 기준 정렬 후 최대 5명
    // NOTE: 실제 타입에 맞게 조정 필요 (FlowStateLobbyCombat.cs 참조)
    var sorted = new List<ServerCharacterData>(allChars);
    sorted.Sort((a, b) => b.Seq.CompareTo(a.Seq));
    for (int i = 0; i < sorted.Count && i < 5; i++)
        playerSpecIds.Add(sorted[i].CharacterId);

    if (playerSpecIds.Count == 0) return;

    // 현재 챕터 몬스터 specId 수집
    var monsterList = SpecDataManager.Instance.GetStageMonsterList(currentChapterId);
    var enemySpecIds = new List<int>();
    foreach (var monster in monsterList)
        enemySpecIds.Add(monster.monster_id);

    // 최대 적 수 (게임 설정)
    int maxEnemyCount = GameConfigManager.Instance.GetInt("max_idle_battle_monster_count", 5);

    // Runner + ViewBridge 생성
    var runnerGo = new GameObject("IdleCombatRunner");
    _idleCombatRunner = runnerGo.AddComponent<IdleCombatRunner>();
    _idleCombatViewBridge = runnerGo.AddComponent<IdleCombatViewBridge>();

    // View 매니저 참조 획득 (씬에 존재하는 인스턴스)
    var unitViewManager = FindObjectOfType<UnitViewManager>();
    var combatViewManager = FindObjectOfType<CombatViewManager>();
    var combatVfxManager = FindObjectOfType<CombatVfxManager>();

    _idleCombatViewBridge.Initialize(
        _idleCombatRunner, unitViewManager, combatViewManager, combatVfxManager);

    _idleCombatRunner.StartIdleCombat(playerSpecIds, enemySpecIds, maxEnemyCount);
}

private void StopIdleCombat()
{
    if (_idleCombatRunner != null)
    {
        _idleCombatRunner.StopIdleCombat();
        Destroy(_idleCombatRunner.gameObject);
        _idleCombatRunner = null;
        _idleCombatViewBridge = null;
    }
}
```

> **NOTE:** 실제 타입(`ServerCharacterData`, `GetAllCharacters()` 반환 타입, `GetStageMonsterList` 파라미터, `GameConfigManager` API)은 기존 `FlowStateLobbyCombat.cs`의 데이터 소스 코드와 `ServerDataManager` API에 맞춰 조정 필요. 위 코드는 구조적 패턴을 보여주는 것이며, 정확한 타입/메서드명은 구현 시 확인.

- [ ] **Step 2: 씬 정리 코드 교체 (두 곳)**

**OnClickGoToLobby (line ~475):**
기존: `InGameManager.Instance.EndInGame();`
교체: `StopIdleCombat();`

**OnClickStartButtonAsync (line ~531):**
기존: `InGameManager.Instance.EndInGame();`
교체: `StopIdleCombat();`

- [ ] **Step 3: 컴파일 확인**

Unity Editor에서 컴파일 에러 없는지 확인.

- [ ] **Step 4: Commit**

```bash
git add Assets/_Project/Scripts/UI/BattleReady/BattleReadyMain.cs
git commit -m "feat(idle-combat): BattleReadyMain에 IdleCombatRunner 통합"
```

---

### Task 5: Unity Editor 수동 검증

- [ ] **Step 1: BattleReady 씬 진입 테스트**

Unity Editor에서 Play → BattleReady 씬으로 이동:
1. 플레이어 캐릭터가 그리드 하단에 배치되는지 확인
2. 1~4초 후 적이 그리드 상단에 스폰되는지 확인
3. 유닛들이 서로 이동/공격하는 전투 연출이 보이는지 확인
4. 스킬 사용 VFX가 표시되는지 확인

- [ ] **Step 2: 사망 방지 확인**

전투가 지속될 때:
1. 유닛이 HP가 낮아져도 사망하지 않고 HP가 회복되는지 확인
2. 사망 애니메이션이 재생되지 않는지 확인

- [ ] **Step 3: 씬 전환 확인**

BattleReady → 다른 씬(Lobby 또는 InGame)으로 전환 시:
1. 에러/경고 없이 정리되는지 확인
2. VFX/투사체가 남지 않는지 확인

- [ ] **Step 4: Commit (필요 시 수정 후)**

수동 검증에서 발견된 문제 수정 후 커밋.