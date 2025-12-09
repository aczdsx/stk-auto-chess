using System;
using System.Linq;
using Cookapps.Stkauto.V1;
using CookApps.TeamBattle.UIManagements;
using Cysharp.Threading.Tasks;
using Google.Protobuf.Collections;

namespace CookApps.AutoBattler
{
    public partial class UserDataManager
    {
        private UserMissionData userMissionData;

        public UserMissionData UserMissionData => userMissionData;
        public MapField<int, UserGuideMission> UserGuideMissionDic => UserMissionData.UserGuideMissions;

        public bool ClearToastPopupFlag { get; set; } // 클리어 안내 토스트 팝업 노출 제어 플래그

        [Initialize(DataCategory.UserMissionData)]
        private void Initialize_MissionData(string data)
        {
            if (string.IsNullOrEmpty(data))
            {
                userMissionData = new UserMissionData
                {
                    GuideMissionCurrentOrder = 1
                };

                // 가이드 미션 전체 리스트 생성
                var allGuideMissionList = SpecDataManager.Instance.GuideMissionInfo.All;
                foreach (var guideMission in allGuideMissionList)
                    userMissionData.UserGuideMissions.Add(guideMission.order, new UserGuideMission
                    {
                        MissionId = guideMission.id,
                        MissionStateType = guideMission.order == 1 ? (int)MissionStateType.WAIT : (int)MissionStateType.NONE,
                        ActionCount = 0
                    });

                return;
            }

            userMissionData = MessageUtility.FromBase64String<UserMissionData>(data);
        }

        [Clear]
        private void Clear_MissionData()
        {
            userMissionData = null;
        }

        public UserGuideMission GetCurrentGuideMissionData()
        {
            UserGuideMission resultData = null;
            if (UserGuideMissionDic.TryGetValue(UserMissionData.GuideMissionCurrentOrder, out resultData)) return resultData;

            return resultData;
        }

        public void SaveUserMissionData()
        {
            QueueSave(DataCategory.UserMissionData.ToCategoryString(), userMissionData);
        }

        // 현재 가이드 미션 상태 세팅 (행동 횟수)
        public void SetGuideMissionActionValue(GuideMissionType missionType, int subKey, int actionValue)
        {
            var isGuideMissionAllClear = userMissionData.GuideMissionCurrentOrder > SpecDataManager.Instance.GetGuideMissionMaxOrder();
            if (isGuideMissionAllClear) return;

            if (UserGuideMissionDic.ContainsKey(UserMissionData.GuideMissionCurrentOrder))
            {
                var targetUserData = UserGuideMissionDic[UserMissionData.GuideMissionCurrentOrder];
                if (targetUserData == null) return;

                var specMissionData = SpecDataManager.Instance.GuideMissionInfo.Get(targetUserData.MissionId);
                if (specMissionData == null
                    || specMissionData.guide_mission_type != missionType
                    || specMissionData.sub_key != subKey) return;

                targetUserData.ActionCount += actionValue;

                // 클리어 여부 체크
                if (specMissionData.need_count <= targetUserData.ActionCount)
                {
                    ToastManager.Instance.ShowToastByTokenKey("GUIDE_MISSION_CLEAR_MSG");

                    targetUserData.MissionStateType = (int)MissionStateType.REWARD;
                }

                UserGuideMissionDic[UserMissionData.GuideMissionCurrentOrder] = targetUserData;

                SaveUserMissionData();
            }
        }

        // 현재 가이드 미션 상태 세팅 (미션 상태)
        public void SetGuideMissionState(GuideMissionType missionType, int subKey, MissionStateType stateType)
        {
            var isGuideMissionAllClear = userMissionData.GuideMissionCurrentOrder > SpecDataManager.Instance.GetGuideMissionMaxOrder();
            if (isGuideMissionAllClear) return;

            if (UserGuideMissionDic.ContainsKey(UserMissionData.GuideMissionCurrentOrder))
            {
                var targetUserData = UserGuideMissionDic[UserMissionData.GuideMissionCurrentOrder];
                if (targetUserData == null) return;

                var specMissionData = SpecDataManager.Instance.GuideMissionInfo.Get(targetUserData.MissionId);
                if (specMissionData == null
                    || specMissionData.guide_mission_type != missionType
                    || specMissionData.sub_key != subKey) return;

                targetUserData.MissionStateType = (int)stateType;

                UserGuideMissionDic[UserMissionData.GuideMissionCurrentOrder] = targetUserData;

                // 클리어한 경우 다음 가이드 미션으로 변경
                if (stateType == MissionStateType.CLEAR &&
                    userMissionData.GuideMissionCurrentOrder <= SpecDataManager.Instance.GetGuideMissionMaxOrder())
                {
                    UserMissionData.GuideMissionCurrentOrder++;
                    ClearToastPopupFlag = false;

                    // 앱이벤트 처리
                    AppEventManager.Instance.GuideMissionClear(specMissionData.order);
                }

                // 모든 가이드 미션 클리어 상태 체크
                if (userMissionData.GuideMissionCurrentOrder > SpecDataManager.Instance.GetGuideMissionMaxOrder())
                {
                    //SceneUILayerManager.Instance.PushUILayerAsync<EndTestgamePopup>().Forget();
                }

                SaveUserMissionData();
            }
        }

        // 현재 가이드 미션 상태 갱신
        public void RefreshCurrentGuideMissionData()
        {
            var specGuideMissionData = SpecDataManager.Instance.GetGuideMissionDataByOrder(UserMissionData.GuideMissionCurrentOrder);
            if (specGuideMissionData == null) return;

            switch (specGuideMissionData.guide_mission_type)
            {
                case GuideMissionType.END_DIALOGUE:
                    if (CheckDialogHistory(specGuideMissionData.dialogue))
                        SetGuideMissionState(GuideMissionType.END_DIALOGUE, specGuideMissionData.sub_key, MissionStateType.REWARD);
                    break;
                case GuideMissionType.CLEAR_STAGE:
                    if (IsClearStage(specGuideMissionData.sub_key))
                        SetGuideMissionState(GuideMissionType.CLEAR_STAGE, specGuideMissionData.sub_key, MissionStateType.REWARD);
                    break;
                case GuideMissionType.LEVELUP_CHARACTER_TARGET:
                    var userCharacterData = GetUserCharacter(specGuideMissionData.sub_key);
                    if (userCharacterData != null && userCharacterData.Level >= specGuideMissionData.need_count)
                    {
                        SetGuideMissionState(GuideMissionType.LEVELUP_CHARACTER_TARGET, specGuideMissionData.sub_key, MissionStateType.REWARD);

                        if (ClearToastPopupFlag == false)
                        {
                            ToastManager.Instance.ShowToastByTokenKey("GUIDE_MISSION_CLEAR_MSG");
                            ClearToastPopupFlag = true;
                        }
                    }

                    break;
                case GuideMissionType.SET_LV_CHARACTER_TARGET:
                    var userCharacterData1 = GetUserCharacter(specGuideMissionData.sub_key);
                    if (userCharacterData1 != null && userCharacterData1.Level >= specGuideMissionData.need_count)
                    {
                        SetGuideMissionState(GuideMissionType.SET_LV_CHARACTER_TARGET, specGuideMissionData.sub_key, MissionStateType.REWARD);

                        if (ClearToastPopupFlag == false)
                        {
                            ToastManager.Instance.ShowToastByTokenKey("GUIDE_MISSION_CLEAR_MSG");
                            ClearToastPopupFlag = true;
                        }
                    }

                    break;
                case GuideMissionType.CLEAR_TRIAL:
                    var trialDungeonData = GetTrialDungeonData(specGuideMissionData.sub_key);
                    if (trialDungeonData != null && trialDungeonData.DungeonStateType > 0)
                        SetGuideMissionState(GuideMissionType.CLEAR_TRIAL, specGuideMissionData.sub_key, MissionStateType.REWARD);
                    break;
                case GuideMissionType.SUM_CHARACTER_LEVEL:
                    var allUserCharacterList = GetAllUserCharacterList();
                    if (allUserCharacterList != null && allUserCharacterList.Count > 0)
                    {
                        var totalLevel = allUserCharacterList.Sum(data => data.Level);
                        if (totalLevel >= specGuideMissionData.need_count)
                        {
                            SetGuideMissionState(GuideMissionType.SUM_CHARACTER_LEVEL, specGuideMissionData.sub_key, MissionStateType.REWARD);

                            if (ClearToastPopupFlag == false)
                            {
                                ToastManager.Instance.ShowToastByTokenKey("GUIDE_MISSION_CLEAR_MSG");
                                ClearToastPopupFlag = true;
                            }
                        }
                    }

                    break;
            }
        }

        // 다이얼로그 히스토리 데이터 추가
        public void AddDialogHistory(int dialogueGroupID)
        {
            if (!UserMissionData.UserDialogueGroupIds.Contains(dialogueGroupID))
            {
                UserMissionData.UserDialogueGroupIds.Add(dialogueGroupID);
                SaveUserMissionData();
            }
        }

        // 다이얼로그 히스토리 데이터 확인
        public bool CheckDialogHistory(int dialogueGroupID)
        {
            return UserMissionData.UserDialogueGroupIds.Contains(dialogueGroupID);
        }
    }
}