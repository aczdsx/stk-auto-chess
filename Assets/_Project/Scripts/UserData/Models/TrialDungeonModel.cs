using R3;
using Tech.Hive.V1;

namespace CookApps.AutoBattler
{
    /// <summary>
    /// 시련 던전 데이터 모델
    /// </summary>
    public class TrialDungeonModel
    {
        private TrialDungeonData _data;

        // R3 이벤트
        public Subject<Unit> OnChanged { get; } = new();
        public readonly Subject<uint> OnOrderChanged = new();
        public readonly Subject<TrialDungeonState> OnStateChanged = new();

        /// <summary>
        /// 원본 데이터
        /// </summary>
        public TrialDungeonData Data => _data;

        /// <summary>
        /// 데이터 초기화
        /// </summary>
        public void Reset()
        {
            _data = null;
            OnChanged.OnNext(Unit.Default);
        }

        /// <summary>
        /// TrialDungeonData 설정
        /// </summary>
        internal void SetTrialDungeon(TrialDungeonData data)
        {
            var prevData = _data;
            _data = data;

            if (data == null)
            {
                OnChanged.OnNext(Unit.Default);
                return;
            }

            // 변경 이벤트 발생
            if (prevData?.Order != data.Order)
                OnOrderChanged.OnNext(data.Order);

            if (prevData?.State != data.State)
                OnStateChanged.OnNext(data.State);

            OnChanged.OnNext(Unit.Default);
        }

        #region 속성

        /// <summary>
        /// 던전 순서
        /// </summary>
        public uint Order => _data?.Order ?? 0;

        /// <summary>
        /// 던전 상태
        /// </summary>
        public TrialDungeonState State => _data?.State ?? TrialDungeonState.Unspecified;

        #endregion

        #region 편의 속성

        /// <summary>
        /// 던전 시작 전 여부
        /// </summary>
        public bool IsNotStarted => State == TrialDungeonState.NotStarted;

        /// <summary>
        /// 던전 진행 중 여부
        /// </summary>
        public bool IsInProgress => State == TrialDungeonState.InProgress;

        /// <summary>
        /// 던전 완료 여부
        /// </summary>
        public bool IsCompleted => State == TrialDungeonState.Completed;

        /// <summary>
        /// 던전 입장 가능 여부
        /// </summary>
        public bool CanEnter => IsNotStarted;

        #endregion
    }
}
