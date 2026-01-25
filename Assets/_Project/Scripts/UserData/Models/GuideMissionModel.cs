using System.Collections.Generic;
using Cysharp.Threading.Tasks;
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
        // 가이드 미션 데이터
        private GuideMissionData _data;

        // R3 이벤트
        public Subject<Unit> OnChanged { get; } = new();
        public readonly Subject<uint> OnMissionIdChanged = new();
        public readonly Subject<GuideMissionState> OnStateChanged = new();
        public readonly Subject<uint> OnProgressChanged = new();

        /// <summary>
        /// 원본 데이터
        /// </summary>
        public GuideMissionData Data => _data;

        /// <summary>
        /// 데이터 초기화
        /// </summary>
        public void Reset()
        {
            _data = null;
            OnChanged.OnNext(Unit.Default);
        }

        /// <summary>
        /// GuideMissionData 설정
        /// </summary>
        internal void SetGuideMission(GuideMissionData data)
        {
            var prevData = _data;
            _data = data;

            if (data == null)
            {
                OnChanged.OnNext(Unit.Default);
                return;
            }

            // 변경 이벤트 발생
            if (prevData?.GuideMissionId != data.GuideMissionId)
                OnMissionIdChanged.OnNext(data.GuideMissionId);

            if (prevData?.State != data.State)
                OnStateChanged.OnNext(data.State);

            if (prevData?.CurrentCount != data.CurrentCount)
                OnProgressChanged.OnNext(data.CurrentCount);

            OnChanged.OnNext(Unit.Default);
        }

        #region 속성

        /// <summary>
        /// 가이드 미션 ID
        /// </summary>
        public uint GuideMissionId => _data?.GuideMissionId ?? 0;

        /// <summary>
        /// 미션 순서
        /// </summary>
        public uint Order => _data?.Order ?? 0;

        /// <summary>
        /// 현재 진행 횟수
        /// </summary>
        public uint CurrentCount => _data?.CurrentCount ?? 0;

        /// <summary>
        /// 목표 횟수
        /// </summary>
        public uint GoalCount => _data?.GoalCount ?? 0;

        /// <summary>
        /// 미션 상태
        /// </summary>
        public GuideMissionState State => _data?.State ?? GuideMissionState.Unspecified;

        /// <summary>
        /// 보상 목록
        /// </summary>
        public IReadOnlyList<Reward> Rewards => _data?.Rewards;

        #endregion

        #region 편의 속성

        /// <summary>
        /// 미션 완료 여부
        /// </summary>
        public bool IsCompleted => State == GuideMissionState.Completed;

        /// <summary>
        /// 미션 진행 중 여부
        /// </summary>
        public bool IsInProgress => State == GuideMissionState.InProgress;

        /// <summary>
        /// 목표 달성 여부 (보상 수령 가능)
        /// </summary>
        public bool IsGoalReached => CurrentCount >= GoalCount && GoalCount > 0;

        /// <summary>
        /// 진행률 (0.0 ~ 1.0)
        /// </summary>
        public float Progress
        {
            get
            {
                if (GoalCount == 0) return 0f;
                return (float)CurrentCount / GoalCount;
            }
        }

        /// <summary>
        /// 모든 가이드 미션 완료 여부 (더 이상 진행할 미션 없음)
        /// </summary>
        public bool IsAllCompleted => _data == null || (GuideMissionId == 0 && State == GuideMissionState.Unspecified);

        #endregion

        #region 액션

        /// <summary>
        /// 가이드 미션 액션 값 추가
        /// 현재 진행 중인 미션 타입이 맞는 경우에만 서버에 완료를 보고합니다.
        /// </summary>
        public void AddActionValue(GuideMissionType missionType, int subKey = 0, uint addCount = 1)
        {
            AddActionValueAsync(missionType, subKey, addCount).Forget();
        }

        /// <summary>
        /// 가이드 미션 액션 값 추가 (비동기)
        /// 현재 진행 중인 미션 타입이 맞는 경우에만 서버에 완료를 보고합니다.
        /// </summary>
        public async UniTask AddActionValueAsync(GuideMissionType missionType, int subKey = 0, uint addCount = 1)
        {
            // 현재 미션이 없거나 이미 완료된 경우 무시
            if (_data == null || IsCompleted || IsGoalReached)
                return;

            // 현재 미션의 스펙 데이터 확인
            var specMission = SpecDataManager.Instance.GuideMissionInfo.Get((int)GuideMissionId);
            if (specMission == null)
                return;

            // 미션 타입 확인
            if (specMission.guide_mission_type != missionType)
                return;

            // 서브 키 확인 (서브 키가 있는 경우에만)
            if (specMission.sub_key > 0 && specMission.sub_key != subKey)
                return;

            // 서버에 액션 완료 보고
            await UpdateActionAsync(addCount);
        }

        /// <summary>
        /// 서버에 클라이언트 액션 완료를 보고합니다.
        /// </summary>
        public async UniTask UpdateActionAsync(uint addCount)
        {
            await NetManager.Instance.GuideMission.UpdateActionAsync(addCount);
        }

        #endregion
    }
}
