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
        private UserDungeon userDungeon;

        public UserDungeon UserDungon => userDungeon;

        [Initialize(DataCategory.UserDungeon, 11)]
        private void Initialize_DungeonData(string data)
        {
            if (string.IsNullOrEmpty(data))
            {
                userDungeon = new UserDungeon();
                return;
            }

            userDungeon = MessageUtility.FromBase64String<UserDungeon>(data);
        }

        [Clear]
        private void Clear_DungeonData()
        {
            userDungeon = null;
        }

        public void SaveUserDungeonData()
        {
            HatcheryGrpcManager.Instance.SetPlayerDataAsync(DataCategory.UserDungeon.ToCategoryString(), userDungeon);
        }

        #region Trial Dungeon (시련던전)

        public UserTrialDungeonData GetTrialDungeonData(int dungeonID)
        {
            if (userDungeon.UserTrialDungeonDatas.ContainsKey(dungeonID))
            {
                return userDungeon.UserTrialDungeonDatas[dungeonID];
            }

            return null;
        }

        public void SetTrialDungeonData(int dungeonID, DungeonStateType stateType, bool needSave)
        {
            if (userDungeon.UserTrialDungeonDatas.ContainsKey(dungeonID))
            {
                userDungeon.UserTrialDungeonDatas[dungeonID].DungeonStateType = (int)stateType;
            }

            if (needSave)
            {
                SaveUserDungeonData();
            }
        }

        #endregion
    }
}

