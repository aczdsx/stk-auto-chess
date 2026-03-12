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
        private int _currentEnemyCount;

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

                int threshold = unit.MaxHP / 10;
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
            return _rng.Range(100, 401) / 100f;
        }

        private void OnDestroy()
        {
            if (_isRunning) StopIdleCombat();
        }
    }
}
