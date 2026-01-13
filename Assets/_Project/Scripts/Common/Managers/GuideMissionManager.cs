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
        /// 가이드 미션 액션 값 추가
        /// 현재 진행 중인 미션 타입이 맞는 경우에만 서버에 완료를 보고합니다.
        /// </summary>
        public void AddGuideMissionActionValue(GuideMissionType missionType, int subKey, int actionValue)
        {
            var guideMission = ServerDataManager.Instance.GuideMission;

            // 현재 미션이 없거나 이미 완료된 경우 무시
            if (guideMission.Data == null || guideMission.IsCompleted || guideMission.IsGoalReached)
                return;

            // 현재 미션의 스펙 데이터 확인
            var specMission = SpecDataManager.Instance.GuideMissionInfo.Get((int)guideMission.GuideMissionId);
            if (specMission == null)
                return;

            // 미션 타입 확인
            if (specMission.guide_mission_type != missionType)
                return;

            // 서브 키 확인 (서브 키가 있는 경우에만)
            if (specMission.sub_key > 0 && specMission.sub_key != subKey)
                return;

            CompleteActionAsync().Forget();
        }

        /// <summary>
        /// 서버에 클라이언트 액션 완료를 보고하고 UI를 갱신합니다.
        /// </summary>
        private async UniTaskVoid CompleteActionAsync()
        {
            await NetManager.Instance.GuideMission.CompleteActionAsync();
            RefreshGuideMissionUI();
            UpdateGuideMissionAlert();
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
