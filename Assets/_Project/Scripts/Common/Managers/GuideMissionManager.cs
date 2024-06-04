using System.Collections;
using System.Collections.Generic;
using CookApps.TeamBattle;
using CookApps.TeamBattle.UIManagements;
using UnityEngine;

namespace CookApps.AutoBattler
{
    public class GuideMissionManager : Singleton<GuideMissionManager>
    {
        // 가이드 미션 수치 증가
        public void AddGuideMissionActionValue(GuideMissionType missonType, int actionValue)
        {
            UserDataManager.Instance.SetGuideMissionActionValue(missonType, actionValue);

            RefreshGuideMissionUI();
        }

        // 가이드 미션 상태 변경
        public void ChangeGuideMissionState(GuideMissionType missonType, MissionStateType stateType)
        {
            UserDataManager.Instance.SetGuideMissionState(missonType, stateType);

            RefreshGuideMissionUI();
        }

        // 로비 메인  가이드 미션 UI 갱신
        public void RefreshGuideMissionUI()
        {
            var lobbyMain = SceneUILayerManager.Instance.GetUILayer<LobbyMain>();
            if (lobbyMain != null)
            {
                lobbyMain.RefreshUI(LobbyMainRefreshType.GUIDE_MISSION);
            }
        }
    }
}
