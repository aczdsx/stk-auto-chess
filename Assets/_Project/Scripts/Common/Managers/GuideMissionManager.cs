using System;
using CookApps.TeamBattle;
using CookApps.TeamBattle.UIManagements;
using Cysharp.Threading.Tasks;

namespace CookApps.AutoBattler
{
    public class GuideMissionManager : Singleton<GuideMissionManager>
    {
        public Action OnGuideAlertUpdated;  // 가이드 미션 안내용 알림 갱신

        /// <summary>
        /// [DEPRECATED] 서버에서 자동으로 가이드 미션 진행 상황을 추적합니다.
        /// 기존 호출 코드와의 호환성을 위해 유지되며, 서버에서 가이드 미션 정보를 다시 조회합니다.
        /// </summary>
        [Obsolete("서버에서 자동으로 진행 상황을 추적합니다. 이 메서드는 호환성을 위해 유지됩니다.")]
        public void AddGuideMissionActionValue(GuideMissionType missonType, int subKey, int actionValue)
        {
            // 서버에서 자동으로 진행 상황을 추적하므로, 서버에서 최신 상태를 조회합니다.
            RefreshGuideMissionFromServerAsync().Forget();
        }

        /// <summary>
        /// [DEPRECATED] 서버에서 자동으로 가이드 미션 상태를 관리합니다.
        /// 기존 호출 코드와의 호환성을 위해 유지되며, 서버에서 가이드 미션 정보를 다시 조회합니다.
        /// </summary>
        [Obsolete("서버에서 자동으로 상태를 관리합니다. 보상 수령은 ClaimRewardAsync를 사용하세요.")]
        public void ChangeGuideMissionState(GuideMissionType missonType, int subKey, MissionStateType stateType)
        {
            // 서버에서 자동으로 상태를 관리하므로, 서버에서 최신 상태를 조회합니다.
            RefreshGuideMissionFromServerAsync().Forget();
        }

        /// <summary>
        /// 서버에서 가이드 미션 정보를 다시 조회하고 UI를 갱신합니다.
        /// </summary>
        public async UniTaskVoid RefreshGuideMissionFromServerAsync()
        {
            await NetManager.Instance.GuideMission.GetAsync();
            RefreshGuideMissionUI();
        }

        // 로비 메인 가이드 미션 UI 갱신
        public void RefreshGuideMissionUI()
        {
            var battleReadyMain = SceneUILayerManager.Instance.GetUILayer<BattleReadyMain>();
            if (battleReadyMain != null)
            {
                battleReadyMain.RefreshUI(LobbyMainRefreshType.GUIDE_MISSION);
            }
        }

        // 가이드 미션 안내 알림 갱신
        public void UpdateGuideMissionAlert()
        {
            OnGuideAlertUpdated?.Invoke();
        }
    }
}
