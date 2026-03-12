using System;
using System.Collections.Generic;
using UnityEngine;

namespace CookApps.AutoChess
{
    public class IdleCombatRunner : MonoBehaviour
    {
        private CombatMatchState _matchState;
        private DeterministicRNG _rng;
        private SimEventQueue _eventQueue;
        private bool _isRunning;

        private const int TickRate = 30;
        private float _tickAccumulator;
        private const int MaxTicksPerFrame = 3;

        // enemy spawn
        private List<int> _enemySpecIds;
        private float _enemySpawnTimer;
        private int _maxEnemyCount;

        // View events
        public event Action<CombatMatchState> OnTick;
        public event Action OnCombatStarted;
        public event Action OnCombatStopped;

        public CombatMatchState MatchState => _matchState;
        public SimEventQueue EventQueue => _eventQueue;
        public bool IsRunning => _isRunning;

        public void StartIdleCombat(List<int> playerChampionSpecIds, List<int> enemySpecIds, int maxEnemyCount)
        {
            if (playerChampionSpecIds == null || playerChampionSpecIds.Count == 0) return;

            SkillFactory.Initialize(TickRate);

            _eventQueue = new SimEventQueue();
            _rng = new DeterministicRNG((ulong)DateTime.Now.Ticks);

            _matchState = IdleCombatSetup.CreateMatchState(
                playerChampionSpecIds, _eventQueue, ref _rng, TickRate);

            _enemySpecIds = enemySpecIds;
            _maxEnemyCount = maxEnemyCount;
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

            UpdateEnemySpawn(dt);

            float tickInterval = 1f / TickRate;
            _tickAccumulator += dt;

            int ticksThisFrame = 0;
            while (_tickAccumulator >= tickInterval && ticksThisFrame < MaxTicksPerFrame)
            {
                RestoreLowHPUnits();
                _matchState.IsFinished = false;

                CombatAISystem.Tick(_matchState, ref _rng, TickRate);

                OnTick?.Invoke(_matchState);

                _tickAccumulator -= tickInterval;
                ticksThisFrame++;
            }
        }

        private void UpdateEnemySpawn(float dt)
        {
            if (_enemySpecIds == null || _enemySpecIds.Count == 0) return;
            if (_matchState.AliveCountB >= _maxEnemyCount) return;

            _enemySpawnTimer -= dt;
            if (_enemySpawnTimer > 0f) return;

            int specIndex = _rng.Range(0, _enemySpecIds.Count);
            int enemySpecId = _enemySpecIds[specIndex];

            IdleCombatSetup.TryAddEnemy(_matchState, enemySpecId, ref _rng, TickRate);

            _enemySpawnTimer = GetRandomSpawnInterval();
        }

        private void RestoreLowHPUnits()
        {
            for (int i = 0; i < _matchState.UnitCount; i++)
            {
                ref var unit = ref _matchState.Units[i];

                bool needsRestore = false;

                if (!unit.IsAlive || unit.State == CombatState.Dead)
                {
                    // 사망한 유닛 복원 (idle 전투는 영구 사망 없음)
                    needsRestore = true;
                }
                else if (unit.CurrentHP <= unit.MaxHP / 10)
                {
                    // HP가 임계값 이하인 유닛 복원
                    needsRestore = true;
                }

                if (needsRestore)
                {
                    unit.IsAlive = true;
                    unit.CurrentHP = unit.MaxHP;
                    unit.State = CombatState.Idle;
                    unit.CurrentTargetId = CombatUnit.InvalidId;
                    unit.AttackCooldown = 0;
                    unit.CCRemainingFrames = 0;
                    unit.ActiveCC = CrowdControlType.None;

                    // 그리드 재등록 (사망 시 그리드에서 제거됐을 수 있음)
                    _matchState.SetGridMulti(unit.GridCol, unit.GridRow, unit.SizeW, unit.SizeH, unit.CombatId);
                }
            }

            // AliveCount 갱신
            _matchState.AliveCountA = CombatSetupSystem.CountAliveByTeam(_matchState, 0);
            _matchState.AliveCountB = CombatSetupSystem.CountAliveByTeam(_matchState, 1);
        }

        private float GetRandomSpawnInterval()
        {
            return _rng.Range(100, 401) / 100f;
        }

        private void OnDestroy()
        {
            if (_isRunning) StopIdleCombat();
        }
    }
}
