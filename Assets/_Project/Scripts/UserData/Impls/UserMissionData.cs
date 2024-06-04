using System;
using Cookapps.Autobattleproject.V1;
using CookApps.gRPC.Hatchery;
using CookApps.gRPC.Universal;
using Google.Protobuf.Collections;

namespace CookApps.AutoBattler
{
    public partial class UserDataManager
    {
        private UserMissionData userMissionData;

        public UserMissionData UserMissionData => userMissionData;
        public MapField<int, UserGuideMission> UserGuideMissionDic => UserMissionData.UserGuideMissions;

        [Initialize(DataCategory.UserMissionData)]
        private void Initialize_MissionData(string data)
        {
            if (string.IsNullOrEmpty(data))
            {
                userMissionData = new UserMissionData
                {
                    GuideMissionCurrentOrder = 1,
                };

                // 가이드 미션 전체 리스트 생성
                var allGuideMissionList = SpecDataManager.Instance.SpecGuideMission.All;
                foreach (var guideMission in allGuideMissionList)
                {
                    userMissionData.UserGuideMissions.Add(guideMission.order, new UserGuideMission
                    {
                        MissionId = guideMission.id,
                        MissionStateType = guideMission.order == 1 ? (int)MissionStateType.WAIT : (int)MissionStateType.NONE,
                        ActionCount = 0,
                    });
                }

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
            if (UserGuideMissionDic.TryGetValue(UserMissionData.GuideMissionCurrentOrder, out resultData))
            {
                return resultData;
            }

            return resultData;
        }

        public void SaveUserMissionData()
        {
            HatcheryGrpcManager.Instance.SetPlayerDataAsync(DataCategory.UserMissionData.ToCategoryString(), userMissionData);
        }

        // 현재 가이드 미션 상태 갱신 (행동 횟수)
        public void SetGuideMissionActionValue(GuideMissionType missionType, int actionValue)
        {
            if (UserGuideMissionDic.ContainsKey(UserMissionData.GuideMissionCurrentOrder))
            {
                var targetUserData = UserGuideMissionDic[UserMissionData.GuideMissionCurrentOrder];
                if (targetUserData == null) return;

                var specMissionData = SpecDataManager.Instance.SpecGuideMission.Get(targetUserData.MissionId);
                if (specMissionData == null || specMissionData.type != missionType) return;

                targetUserData.ActionCount += actionValue;

                // 클리어 여부 체크
                if (specMissionData.action_count <= targetUserData.ActionCount)
                {
                    targetUserData.MissionStateType = (int)MissionStateType.REWARD;
                }

                UserGuideMissionDic[UserMissionData.GuideMissionCurrentOrder] = targetUserData;

                SaveUserMissionData();
            }
        }

        // 현재 가이드 미션 상태 갱신 (미션 상태)
        public void SetGuideMissionState(GuideMissionType missionType, MissionStateType stateType)
        {
            if (UserGuideMissionDic.ContainsKey(UserMissionData.GuideMissionCurrentOrder))
            {
                var targetUserData = UserGuideMissionDic[UserMissionData.GuideMissionCurrentOrder];
                if (targetUserData == null) return;

                var specMissionData = SpecDataManager.Instance.SpecGuideMission.Get(targetUserData.MissionId);
                if (specMissionData == null || specMissionData.type != missionType) return;

                targetUserData.MissionStateType = (int)stateType;

                UserGuideMissionDic[UserMissionData.GuideMissionCurrentOrder] = targetUserData;

                // 클리어한 경우 다음 가이드 미션으로 변경
                if (stateType == MissionStateType.CLEAR &&
                    userMissionData.GuideMissionCurrentOrder < SpecDataManager.Instance.GetGuideMissionMaxOrder())
                {
                    UserMissionData.GuideMissionCurrentOrder++;
                }

                SaveUserMissionData();
            }
        }
    }
}
