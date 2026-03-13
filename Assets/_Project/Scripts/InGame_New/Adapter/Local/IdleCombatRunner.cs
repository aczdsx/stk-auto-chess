using System;
using System.Collections.Generic;
using CookApps.AutoBattler;
using UnityEngine;

namespace CookApps.AutoChess
{
    public class IdleCombatRunner : MonoBehaviour
    {
        private CombatMatchState _matchState;
        private DeterministicRNG _rng;
        private SimEventQueue _eventQueue;
        private bool _isRunning;

        public const int TickRate = 30;
        private float _tickAccumulator;
        private const int MaxTicksPerFrame = 3;

        // enemy spawn
        private List<StageMonster> _enemyMonsters;
        private float _enemySpawnTimer;
        private int _maxEnemyCount;

        // View events
        public event Action<CombatMatchState> OnTick;
        public event Action OnCombatStarted;
        public event Action OnCombatStopped;

        public CombatMatchState MatchState => _matchState;
        public SimEventQueue EventQueue => _eventQueue;
        public bool IsRunning => _isRunning;

        public void StartIdleCombat(List<int> playerChampionSpecIds, List<StageMonster> enemyMonsters, int maxEnemyCount)
        {
            if (playerChampionSpecIds == null || playerChampionSpecIds.Count == 0) return;

            SkillFactory.Initialize(TickRate);

            _eventQueue = new SimEventQueue();
            _rng = new DeterministicRNG((ulong)DateTime.Now.Ticks);

            _matchState = IdleCombatSetup.CreateMatchState(
                playerChampionSpecIds, _eventQueue, ref _rng, TickRate);

            _enemyMonsters = enemyMonsters;
            _maxEnemyCount = maxEnemyCount;
            _enemySpawnTimer = GetRandomSpawnInterval();
            _tickAccumulator = 0f;
            _isRunning = true;

            DamageSystem.PlayerInvincible = true;

            Debug.Log($"[IdleCombatRunner] IdleCombat started - {_matchState.UnitCount} units, maxEnemy={_maxEnemyCount}");
            OnCombatStarted?.Invoke();
        }

        public void StopIdleCombat()
        {
            _isRunning = false;
            _matchState = null;
            _eventQueue = null;
            DamageSystem.PlayerInvincible = false;
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
                CombatAISystem.Tick(_matchState, ref _rng, TickRate);

                OnTick?.Invoke(_matchState);

                _tickAccumulator -= tickInterval;
                ticksThisFrame++;
            }
        }

        private void UpdateEnemySpawn(float dt)
        {
            if (_enemyMonsters == null || _enemyMonsters.Count == 0) return;
            if (_matchState.AliveCountB >= _maxEnemyCount) return;

            _enemySpawnTimer -= dt;
            if (_enemySpawnTimer > 0f) return;

            int idx = _rng.Range(0, _enemyMonsters.Count);
            var monster = _enemyMonsters[idx];

            IdleCombatSetup.TryAddEnemy(_matchState, monster.monster_id, monster.multiple_atk, monster.multiple_hp, ref _rng, TickRate);

            _enemySpawnTimer = GetRandomSpawnInterval();
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
