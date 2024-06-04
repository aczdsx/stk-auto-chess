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

        // 현재 가이드 미션 상태 갱신 (행동 횟수)
        public void UpdateCurrentGuideMissionState(int actionValue)
        {
            if (UserGuideMissionDic.ContainsKey(UserMissionData.GuideMissionCurrentOrder))
            {
                var userData = UserGuideMissionDic[UserMissionData.GuideMissionCurrentOrder];
                var specMissionData = SpecDataManager.Instance.SpecGuideMission.Get(userData.MissionId);

                UserGuideMissionDic[UserMissionData.GuideMissionCurrentOrder].ActionCount += actionValue;

                // 클리어 여부 체크
                if (specMissionData.action_count <= UserGuideMissionDic[UserMissionData.GuideMissionCurrentOrder].ActionCount)
                {
                    UserGuideMissionDic[UserMissionData.GuideMissionCurrentOrder].MissionStateType = (int)MissionStateType.REWARD;
                }

                SaveUserMissionData();
            }
        }

        // 현재 가이드 미션 상태 갱신 (미션 상태)
        public void UpdateCurrentGuideMissionState(MissionStateType type)
        {
            if (UserGuideMissionDic.ContainsKey(UserMissionData.GuideMissionCurrentOrder))
            {
                UserGuideMissionDic[UserMissionData.GuideMissionCurrentOrder].MissionStateType = (int)type;

                SaveUserMissionData();
            }
        }

        public void UpdateGuideMissionState(GuideMissionType type, int value)
        {
            switch (type)
            {
                case GuideMissionType.CLEAR_STAGE:
                    break;
                case GuideMissionType.USE_BUILDING:
                    break;
                case GuideMissionType.OPEN_IDLECHEST:
                    break;
                case GuideMissionType.SET_CHARACTER:
                    break;
                case GuideMissionType.LEVELUP_CHARACTER:
                    break;
                case GuideMissionType.OPEN_CHEST:
                    break;
                case GuideMissionType.SUMMON_CHARCTER:
                    break;
            }
        }

        public void SaveUserMissionData()
        {
            HatcheryGrpcManager.Instance.SetPlayerDataAsync(DataCategory.UserMissionData.ToCategoryString(), userMissionData);
        }
    }
}
