using System;
using CookApps.TeamBattle;
using CookApps.TeamBattle.UIManagements;

namespace CookApps.AutoBattler
{
    public class GuideMissionManager : Singleton<GuideMissionManager>
    {
        public Action OnGuideAlertUpdated;  // 가이드 미션 안내용 알림 갱신

        // 가이드 미션 수치 증가
        public void AddGuideMissionActionValue(GuideMissionType missonType, int subKey, int actionValue)
        {
            UserDataManager.Instance.SetGuideMissionActionValue(missonType, subKey, actionValue);

            RefreshGuideMissionUI();
        }

        // 가이드 미션 상태 변경
        public void ChangeGuideMissionState(GuideMissionType missonType, int subKey, MissionStateType stateType)
        {
            UserDataManager.Instance.SetGuideMissionState(missonType, subKey, stateType);

            RefreshGuideMissionUI();
        }

        // 로비 메인  가이드 미션 UI 갱신
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
