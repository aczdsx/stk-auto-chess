using System;
using System.Collections.Generic;
using CookApps.AutoBattler;
using CookApps.AutoChess.View;
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
        private int _tickCount;
        private const int StateDumpInterval = 60; // 2초마다 (30fps * 2)

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

        public void StartIdleCombat(List<int> playerChampionSpecIds, List<StageMonster> enemyMonsters, int maxEnemyCount,
            int boardWidth = 7, int boardHeight = 4)
        {
            if (playerChampionSpecIds == null || playerChampionSpecIds.Count == 0) return;

            SkillFactory.Initialize(TickRate);

            _eventQueue = new SimEventQueue();
            _rng = new DeterministicRNG((ulong)DateTime.Now.Ticks);

            _matchState = IdleCombatSetup.CreateMatchState(
                playerChampionSpecIds, _eventQueue, ref _rng, TickRate, boardWidth, boardHeight);

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

                _tickCount++;
                if (_tickCount % StateDumpInterval == 0)
                    DumpUnitStates();

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


        private void DumpUnitStates()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"[IdleCombat][Dump] tick={_tickCount} AliveA={_matchState.AliveCountA} AliveB={_matchState.AliveCountB}");
            for (int i = 0; i < _matchState.UnitCount; i++)
            {
                ref var u = ref _matchState.Units[i];
                string team = u.TeamIndex == 0 ? "P" : "E";
                string alive = u.IsAlive ? "O" : "X";
                string targetStr = u.CurrentTargetId == CombatUnit.InvalidId ? "none" : u.CurrentTargetId.ToString();
                sb.AppendLine($"  [{i}] {team} id={u.CombatId} spec={u.ChampionSpecId} {alive} " +
                    $"state={u.State} pos=({u.GridCol},{u.GridRow}) " +
                    $"target={targetStr} HP={u.CurrentHP}/{u.MaxHP} " +
                    $"atkCD={u.AttackCooldown} pendAtk={u.PendingAtkTimer} moveT={u.MoveTimer}");
            }
            Debug.Log(sb.ToString());
        }

        private float GetRandomSpawnInterval()
        {
            return _rng.Range(100, 401) / 100f;
        }

        #if UNITY_EDITOR
        private GUIStyle _debugStyle;

        private void OnGUI()
        {
            if (!_isRunning || _matchState == null) return;
            var cam = Camera.main;
            if (cam == null) return;

            if (_debugStyle == null)
            {
                _debugStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 11,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                };
            }

            for (int i = 0; i < _matchState.UnitCount; i++)
            {
                ref var u = ref _matchState.Units[i];
                if (!u.IsAlive) continue;

                var worldPos = BoardWorldHelper.CombatGridToWorld(0, u.GridCol, u.GridRow);
                worldPos.y += 1.8f;
                var screenPos = cam.WorldToScreenPoint(worldPos);
                if (screenPos.z < 0) continue;

                string team = u.TeamIndex == 0 ? "P" : "E";
                string targetStr = u.CurrentTargetId == CombatUnit.InvalidId ? "-" : u.CurrentTargetId.ToString();
                string label = $"{team}{u.CombatId} {u.State} ({u.GridCol},{u.GridRow})\ntgt={targetStr} cd={u.AttackCooldown}\npAtk={u.PendingAtkTimer} mv={u.MoveTimer}";

                _debugStyle.normal.textColor = u.TeamIndex == 0 ? Color.cyan : Color.red;

                var rect = new Rect(screenPos.x - 60, Screen.height - screenPos.y - 40, 120, 50);
                GUI.Label(rect, label, _debugStyle);
            }
        }
        #endif

        private void OnDestroy()
        {
            if (_isRunning) StopIdleCombat();
        }
    }
}
