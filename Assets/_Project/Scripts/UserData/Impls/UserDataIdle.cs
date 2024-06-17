using System.Collections;
using System.Collections.Generic;
using Cookapps.Autobattleproject.V1;
using CookApps.gRPC.Hatchery;
using CookApps.gRPC.Universal;
using UnityEngine;

namespace CookApps.AutoBattler
{
    public partial class UserDataManager
    {
        private UserIdleData userIdleData;

        public UserIdleData UserIdleData => userIdleData;

        [Initialize(DataCategory.UserIdleData, 1)]
        private void Initialize_IdleData(string data)
        {
            if (string.IsNullOrEmpty(data))
            {
                userIdleData = new UserIdleData
                {
                    LastRewardGetTimestamp = 0,
                };
                return;
            }

            userIdleData = MessageUtility.FromBase64String<UserIdleData>(data);

            UpdateAllCacheData();
        }

        [Clear]
        private void Clear_IdleData()
        {
            userStageGroup = null;
        }

        public void SaveUserIdle()
        {
            HatcheryGrpcManager.Instance.SetPlayerDataAsync(DataCategory.UserIdleData.ToCategoryString(), userIdleData);
        }
    }
}
