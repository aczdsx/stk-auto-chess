using R3;
using Tech.Hive.V1;

namespace CookApps.AutoBattler
{
    /// <summary>
    /// 가이드 미션 데이터 모델
    /// 현재 진행 중인 가이드 미션 정보를 관리
    /// </summary>
    public class GuideMissionModel
    {
        // 가이드 미션 정보
        private uint _guideMissionId;
        private uint _order;
        private uint _currentCount;
        private uint _goalCount;
        private GuideMissionState _state;

        // R3 이벤트
        public Subject<Unit> OnChanged { get; } = new();
        public readonly Subject<uint> OnMissionIdChanged = new();
        public readonly Subject<GuideMissionState> OnStateChanged = new();
        public readonly Subject<uint> OnProgressChanged = new();

        /// <summary>
        /// 데이터 초기화
        /// </summary>
        public void Reset()
        {
            _guideMissionId = 0;
            _order = 0;
            _currentCount = 0;
            _goalCount = 0;
            _state = GuideMissionState.Unspecified;

            OnChanged.OnNext(Unit.Default);
        }

        /// <summary>
        /// GuideMissionData로부터 데이터 설정
        /// </summary>
        internal void SetGuideMission(GuideMissionData data)
        {
            if (data == null) return;

            var missionIdChanged = _guideMissionId != data.GuideMissionId;
            var stateChanged = _state != data.State;
            var progressChanged = _currentCount != data.CurrentCount;

            _guideMissionId = data.GuideMissionId;
            _order = data.Order;
            _currentCount = data.CurrentCount;
            _goalCount = data.GoalCount;
            _state = data.State;

            // 변경 이벤트 발생
            if (missionIdChanged)
                OnMissionIdChanged.OnNext(_guideMissionId);

            if (stateChanged)
                OnStateChanged.OnNext(_state);

            if (progressChanged)
                OnProgressChanged.OnNext(_currentCount);

            OnChanged.OnNext(Unit.Default);
        }

        /// <summary>
        /// 가이드 미션 ID
        /// </summary>
        public uint GuideMissionId => _guideMissionId;

        /// <summary>
        /// 미션 순서
        /// </summary>
        public uint Order => _order;

        /// <summary>
        /// 현재 진행 횟수
        /// </summary>
        public uint CurrentCount => _currentCount;

        /// <summary>
        /// 목표 횟수
        /// </summary>
        public uint GoalCount => _goalCount;

        /// <summary>
        /// 미션 상태
        /// </summary>
        public GuideMissionState State => _state;

        /// <summary>
        /// 미션 완료 여부
        /// </summary>
        public bool IsCompleted => _state == GuideMissionState.Completed;

        /// <summary>
        /// 미션 진행 중 여부
        /// </summary>
        public bool IsInProgress => _state == GuideMissionState.InProgress;

        /// <summary>
        /// 목표 달성 여부 (보상 수령 가능)
        /// </summary>
        public bool IsGoalReached => _currentCount >= _goalCount;

        /// <summary>
        /// 진행률 (0.0 ~ 1.0)
        /// </summary>
        public float Progress
        {
            get
            {
                if (_goalCount == 0)
                    return 0f;

                return (float)_currentCount / (float)_goalCount;
            }
        }
    }
}
