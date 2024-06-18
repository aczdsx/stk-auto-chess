using System;
using System.Linq;
using Cookapps.Autobattleproject.V1;
using CookApps.gRPC.Hatchery;
using CookApps.gRPC.Universal;
using Google.Protobuf.Collections;

namespace CookApps.AutoBattler
{
    public partial class UserDataManager
    {
        private UserCommanderSkillData userCommanderSkillData;

        public UserCommanderSkillData UserCommanderSkillData => userCommanderSkillData;

        [Initialize(DataCategory.UserCommanderSkillData)]
        private void Initialize_CommanderSkillData(string data)
        {
            if (string.IsNullOrEmpty(data))
            {
                userCommanderSkillData = new UserCommanderSkillData
                {
                    EquippedCommanderSkillId = 0,
                };

                return;
            }

            userCommanderSkillData = MessageUtility.FromBase64String<UserCommanderSkillData>(data);
        }

        [Clear]
        private void Clear_CommanderSkillData()
        {
            userCommanderSkillData = null;
        }

        public void SetEquippedCommanderSkill(int commanderSkillID)
        {
            userCommanderSkillData.EquippedCommanderSkillId = commanderSkillID;

            SaveUserCommanderSKillData();
        }

        public int GetEquippedCommanderSkill()
        {
            return userCommanderSkillData.EquippedCommanderSkillId;
        }

        public void AddCommanderSkillData(int commanderSkillID)
        {
            if (UserCommanderSkillData.UserCommanderSkillList.ToList().Exists(data => data.CommanderSkillId == commanderSkillID)) return;
        }

        public void SaveUserCommanderSKillData()
        {
            HatcheryGrpcManager.Instance.SetPlayerDataAsync(DataCategory.UserCommanderSkillData.ToCategoryString(), userCommanderSkillData);
        }
    }
}
