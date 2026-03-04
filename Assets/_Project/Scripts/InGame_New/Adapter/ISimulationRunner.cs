namespace CookApps.AutoChess
{
    /// <summary>
    /// 시뮬레이션 구동기 인터페이스.
    /// View 레이어는 이 인터페이스만 참조하여 구체 구현(Local/Quantum/Fusion)과 분리.
    /// </summary>
    public interface ISimulationRunner
    {
        /// <summary>결정론적 시뮬레이션 시드</summary>
        ulong RandomSeed { get; set; }

        /// <summary>시뮬레이션 시작</summary>
        void StartSimulation(GameConfig config = null);

        /// <summary>시뮬레이션 중지</summary>
        void StopSimulation();

        /// <summary>커맨드 입력 (View → Simulation)</summary>
        void EnqueueCommand(GameCommand command);

        /// <summary>현재 게임 상태 읽기 (View에서 사용)</summary>
        GameWorld GetWorld();

        /// <summary>실행 중 여부</summary>
        bool IsRunning { get; }

        /// <summary>매 시뮬레이션 틱 완료 시 발생. GameWorld 전달.</summary>
        event System.Action<GameWorld> OnTick;

        /// <summary>페이즈 전환 시 발생. (이전 페이즈, 새 페이즈) 전달.</summary>
        event System.Action<GamePhase, GamePhase> OnPhaseChanged;
    }
}
