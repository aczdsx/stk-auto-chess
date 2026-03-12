using System.Collections.Generic;
using UnityEngine;

namespace CookApps.AutoChess
{
    /// <summary>
    /// 로컬 시뮬레이션 구동기. Unity Update에서 시뮬레이션 틱을 구동.
    /// 네트워크 없이 로컬에서 게임을 실행하는 어댑터.
    /// 나중에 Quantum/Fusion 어댑터로 교체 가능.
    /// </summary>
    public partial class LocalSimulationRunner : MonoBehaviour, ISimulationRunner
    {
        [Header("Settings")]
        [SerializeField] private GameModeType _gameMode = GameModeType.ClassicBattle;
        [SerializeField] private ulong _randomSeed = 12345;

        public ulong RandomSeed { get => _randomSeed; set => _randomSeed = value; }

        private GameWorld _world;
        private readonly List<GameCommand> _pendingCommands = new();
        private GameCommand[] _commandBuffer = new GameCommand[32];
        private float _tickAccumulator;
        private bool _isRunning;
        private bool _isPausedByTutorial;
        private bool _isPausedByDebugger;

        // ── 프레임 레코더 ──
        private CombatFrameRecorder _frameRecorder;

        // ── 리플레이 (Debug/LocalSimulationRunner.Replay.cs) ──
        partial void OnBeforeRecordFrame(GameWorld world);

        /// <summary>
        /// 배속 배율. Time.unscaledDeltaTime에 곱하여 적용.
        /// Time.timeScale 해킹 방지를 위해 unscaledDeltaTime 기반.
        /// </summary>
        public static float SpeedMultiplier = 1f;

        // ── 이벤트 (View 레이어에서 구독) ──
        public event System.Action<GameWorld> OnTick;
        public event System.Action<GamePhase, GamePhase> OnPhaseChanged;
        public event System.Action<GameWorld> OnGameOver;

        // ── 공개 API ──

        /// <summary>시뮬레이션 시작</summary>
        public void StartSimulation(GameConfig config = null)
        {
            if (config == null)
            {
                config = _gameMode switch
                {
                    GameModeType.ClassicBattle => GameConfig.ClassicBattle(),
                    GameModeType.PvECampaign => GameConfig.PvECampaign(),
                    GameModeType.Competitive => GameConfig.Competitive(),
                    _ => GameConfig.Competitive(),
                };
            }

            _world = GameWorld.Create(config);
            _world.RNG = new DeterministicRNG(_randomSeed);

            // 스펙 데이터 주입 (SpecDataManager → 시뮬레이션 구조체 변환)
            AutoChessSpecAdapter.InjectSpecs(_world);

            GameLoopSystem.Initialize(_world, config);

            // 시뮬레이션 로그 출력 콜백 설정
            CombatLogger.LogOutput = msg => Debug.Log(msg);

            _tickAccumulator = 0f;
            _isRunning = true;

            Debug.Log($"[AutoChess] Simulation started. Mode={config.GameMode}, Players={config.PlayerCount}, Seed={_randomSeed}");
        }

        /// <summary>시뮬레이션 중지</summary>
        public void StopSimulation()
        {
            _isRunning = false;
            _world = null;
            _pendingCommands.Clear();
            Debug.Log("[AutoChess] Simulation stopped.");
        }

        /// <summary>커맨드 입력 (View → Adapter)</summary>
        public void EnqueueCommand(GameCommand command)
        {
            _pendingCommands.Add(command);
        }

        /// <summary>현재 게임 상태 읽기 (View에서 사용)</summary>
        public GameWorld GetWorld() => _world;

        /// <summary>실행 중 여부</summary>
        public bool IsRunning => _isRunning;

        /// <summary>튜토리얼에 의한 틱 일시정지</summary>
        public void PauseTick() { _isPausedByTutorial = true; }

        /// <summary>튜토리얼에 의한 틱 재개</summary>
        public void ResumeTick() { _isPausedByTutorial = false; }

        /// <summary>디버거에 의한 틱 일시정지</summary>
        public void PauseTickByDebugger()
        {
            _isPausedByDebugger = true;
            OnDebuggerPauseChanged?.Invoke(true);
        }

        /// <summary>디버거에 의한 틱 재개</summary>
        public void ResumeTickByDebugger()
        {
            _isPausedByDebugger = false;
            OnDebuggerPauseChanged?.Invoke(false);
        }

        /// <summary>디버거 pause/resume 이벤트 (View에서 구독)</summary>
        public event System.Action<bool> OnDebuggerPauseChanged;

        /// <summary>프레임 레코더 설정</summary>
        public void SetFrameRecorder(CombatFrameRecorder recorder) { _frameRecorder = recorder; }

        /// <summary>프레임 레코더 접근</summary>
        public CombatFrameRecorder FrameRecorder => _frameRecorder;

        // SetReplayController / ReplayController → Debug/LocalSimulationRunner.Replay.cs

        // ── Unity Lifecycle ──

        private void Update()
        {
            if (!_isRunning || _isPausedByTutorial || _isPausedByDebugger || _world == null) return;

            float tickInterval = 1f / _world.TickRate;
            _tickAccumulator += Time.unscaledDeltaTime * SpeedMultiplier;

            // 프레임 스킵 방지: 최대 3틱/프레임
            int maxTicksPerFrame = 3;
            int ticksThisFrame = 0;

            while (_tickAccumulator >= tickInterval && ticksThisFrame < maxTicksPerFrame)
            {
                var prevPhase = _world.CurrentPhase;

                // 커맨드 버퍼 준비
                int commandCount = _pendingCommands.Count;
                if (commandCount > _commandBuffer.Length)
                    _commandBuffer = new GameCommand[commandCount * 2];

                for (int i = 0; i < commandCount; i++)
                    _commandBuffer[i] = _pendingCommands[i];
                _pendingCommands.Clear();

                // 틱 실행
                GameLoopSystem.Tick(_world, _commandBuffer, commandCount);

                // 스냅샷 기록 (이벤트 큐 클리어 전에 기록)
                if (_frameRecorder != null && _world.CombatMatchStates != null && _world.CombatMatchStates[0] != null)
                {
                    OnBeforeRecordFrame(_world);
                    _frameRecorder.RecordFrame(_world.CombatMatchStates[0], _world.FrameCount);
                }

                // 이벤트 발행
                OnTick?.Invoke(_world);

                if (_world.CurrentPhase != prevPhase)
                    OnPhaseChanged?.Invoke(prevPhase, _world.CurrentPhase);

                _tickAccumulator -= tickInterval;
                ticksThisFrame++;

                // 게임 종료 체크
                if (_world.IsGameOver)
                {
                    Debug.Log("[AutoChess] Game Over!");
                    _isRunning = false;
                    OnGameOver?.Invoke(_world);
                    break;
                }
            }
        }

        // ── 디버그 ──

        [ContextMenu("Debug: Dump World State")]
        private void DebugDumpWorldState()
        {
            if (_world == null)
            {
                Debug.Log("[AutoChess] No active world.");
                return;
            }

            Debug.Log($"[AutoChess] Frame={_world.FrameCount} Phase={_world.CurrentPhase} " +
                       $"Stage={_world.CurrentStage} Round={_world.CurrentRound} " +
                       $"Timer={_world.PhaseTimerFrames / (float)_world.TickRate:F1}s " +
                       $"Alive={_world.AlivePlayerCount}");

            for (int i = 0; i < _world.Config.PlayerCount; i++)
            {
                var p = _world.Players[i];
                var e = _world.Economies[i];
                var b = _world.Boards[i];
                Debug.Log($"  P{i}: HP={p.HP}/{p.MaxHP} Gold={e.Gold} Lv={e.Level} " +
                          $"Board={b.UnitCount} Bench={b.BenchCount} " +
                          $"Alive={p.IsAlive} Ready={p.IsReady}");
            }
        }

        [ContextMenu("Debug: Create Test Unit P0")]
        private void DebugCreateTestUnit()
        {
            if (_world == null) return;
            int entityId = BoardSystem.CreateUnit(_world, 0, 1001, 1);
            Debug.Log($"[AutoChess] Created unit EntityId={entityId} on bench for P0");
        }

        [ContextMenu("Debug: Place Unit (0,0) P0")]
        private void DebugPlaceUnit()
        {
            if (_world == null) return;

            // 벤치 첫 유닛을 (0,0)에 배치
            var bench = _world.BenchSlots[0];
            for (int i = 0; i < bench.Length; i++)
            {
                if (bench[i] != UnitData.InvalidId)
                {
                    bool ok = BoardSystem.PlaceUnit(_world, 0, bench[i], 0, 0);
                    Debug.Log($"[AutoChess] PlaceUnit EntityId={bench[i]} → (0,0): {ok}");
                    return;
                }
            }
            Debug.Log("[AutoChess] No unit on bench to place.");
        }
    }
}
