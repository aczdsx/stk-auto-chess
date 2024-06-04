using System;
using Cookapps.Autobattleproject.V1;
using CookApps.gRPC.Hatchery;
using CookApps.gRPC.Universal;

namespace CookApps.AutoBattler
{
    public partial class UserDataManager
    {
        private UserMissionData userMissionData;

        public UserMissionData UserMissionData => userMissionData;

        [Initialize(DataCategory.UserMissionData)]
        private void Initialize_MissionData(string data)
        {
            if (string.IsNullOrEmpty(data))
            {
                userMissionData = new UserMissionData
                {
                    CurrentOrder = 1,
                };
                return;
            }

            userMissionData = MessageUtility.FromBase64String<UserMissionData>(data);
        }

        [Clear]
        private void Clear_MissionData()
        {
            userMissionData = null;
        }

        public void SaveUserMissionData()
        {
            HatcheryGrpcManager.Instance.SetPlayerDataAsync(DataCategory.UserMissionData.ToCategoryString(), userMissionData);
        }
    }
}
