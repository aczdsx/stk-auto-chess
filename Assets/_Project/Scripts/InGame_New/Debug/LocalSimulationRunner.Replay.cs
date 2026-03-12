namespace CookApps.AutoChess
{
    // ── 리플레이 전용 (디버그 빌드 전용) ──
    public partial class LocalSimulationRunner
    {
        private ReplayController _replayController;
        private bool _replayRngCaptured;

        /// <summary>리플레이 컨트롤러 설정</summary>
        public void SetReplayController(ReplayController controller) { _replayController = controller; }

        /// <summary>리플레이 컨트롤러 접근</summary>
        public ReplayController ReplayController => _replayController;

        /// <summary>첫 RecordFrame 시 리플레이용 RNG 상태 캡처</summary>
        partial void OnBeforeRecordFrame(GameWorld world)
        {
            if (_replayController != null && !_replayRngCaptured)
            {
                _replayController.CaptureInitialState(world);
                _replayRngCaptured = true;
            }
        }
    }
}
