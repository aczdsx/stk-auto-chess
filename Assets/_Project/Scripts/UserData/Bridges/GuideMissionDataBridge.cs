using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using R3;
using Tech.Hive.V1;

namespace CookApps.AutoBattler
{
    /// <summary>
    /// 가이드 미션 데이터 브릿지
    /// ServerDataManager와 UI 사이의 중간 레이어
    /// </summary>
    public class GuideMissionDataBridge : DataBridgeBase
    {
        private GuideMissionModel Model;

        // Public Observable 노출
        public Observable<Unit> OnChanged;
        public Observable<uint> OnMissionIdChanged;
        public Observable<GuideMissionState> OnStateChanged;
        public Observable<uint> OnProgressChanged;

        public GuideMissionDataBridge()
        {
            Model = ServerDataManager.Instance.GuideMission;
            OnChanged = Model.OnChanged;
            OnMissionIdChanged = Model.OnMissionIdChanged;
            OnStateChanged = Model.OnStateChanged;
            OnProgressChanged = Model.OnProgressChanged;
        }

        #region 가이드 미션 정보

        /// <summary>
        /// 가이드 미션 ID
        /// </summary>
        public uint GuideMissionId => Model?.GuideMissionId ?? 0;

        /// <summary>
        /// 미션 순서
        /// </summary>
        public uint Order => Model?.Order ?? 0;

        /// <summary>
        /// 현재 진행 횟수
        /// </summary>
        public uint CurrentCount => Model?.CurrentCount ?? 0;

        /// <summary>
        /// 목표 횟수
        /// </summary>
        public uint GoalCount => Model?.GoalCount ?? 0;

        /// <summary>
        /// 미션 상태
        /// </summary>
        public GuideMissionState State => Model?.State ?? GuideMissionState.Unspecified;

        /// <summary>
        /// 미션 완료 여부
        /// </summary>
        public bool IsCompleted => Model?.IsCompleted ?? false;

        /// <summary>
        /// 미션 진행 중 여부
        /// </summary>
        public bool IsInProgress => Model?.IsInProgress ?? false;

        /// <summary>
        /// 목표 달성 여부 (보상 수령 가능)
        /// </summary>
        public bool IsGoalReached => Model?.IsGoalReached ?? false;

        /// <summary>
        /// 진행률 (0.0 ~ 1.0)
        /// </summary>
        public float Progress => Model?.Progress ?? 0f;

        /// <summary>
        /// 보상 목록
        /// </summary>
        public IReadOnlyList<Reward> Rewards => Model?.Rewards ?? System.Array.Empty<Reward>();

        /// <summary>
        /// 모든 가이드 미션 완료 여부
        /// </summary>
        public bool IsAllCompleted => Model?.IsAllCompleted ?? true;

        /// <summary>
        /// 보상 수령 가능 여부
        /// </summary>
        public bool CanClaimReward => Model?.IsCompleted ?? false;

        #endregion

        #region 서버 API 호출

        /// <summary>
        /// 가이드 미션 정보 조회 (서버 API 호출)
        /// </summary>
        public async UniTask<GuideMissionGetResponse> GetAsync()
        {
            try
            {
                var response = await NetManager.Instance.GuideMission.GetAsync();
                return response;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to get guide mission: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// 가이드 미션 보상 수령 (서버 API 호출)
        /// </summary>
        public async UniTask<GuideMissionClaimRewardResponse> ClaimRewardAsync(uint guideMissionId)
        {
            try
            {
                var response = await NetManager.Instance.GuideMission.ClaimRewardAsync(guideMissionId);
                return response;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to claim guide mission reward: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// 현재 가이드 미션 보상 수령 (서버 API 호출)
        /// </summary>
        public async UniTask<GuideMissionClaimRewardResponse> ClaimCurrentRewardAsync()
        {
            if (GuideMissionId == 0)
            {
                Debug.LogWarning("No guide mission to claim reward");
                return null;
            }

            return await ClaimRewardAsync(GuideMissionId);
        }

        /// <summary>
        /// 가이드 미션 액션 보고
        /// </summary>
        /// <param name="guideMissionType">미션 타입</param>
        /// <param name="addCount">추가 횟수</param>
        /// <param name="subKey">서브 키</param>
        public void AddAction(GuideMissionType guideMissionType, uint addCount = 1, int subKey = 0)
        {
            Model.AddActionValue(guideMissionType, subKey, addCount);
        }

        /// <summary>
        /// 가이드 미션 액션 보고 (비동기)
        /// </summary>
        /// <param name="guideMissionType">미션 타입</param>
        /// <param name="addCount">추가 횟수</param>
        /// <param name="subKey">서브 키</param>
        public async UniTask AddActionAsync(GuideMissionType guideMissionType, uint addCount = 1, int subKey = 0)
        {
            await Model.AddActionValueAsync(guideMissionType, subKey, addCount);
        }

        #endregion
    }
}
