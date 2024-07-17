using System;
using System.Collections.Generic;
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

        [Initialize(DataCategory.UserCommanderSkillData, 8)]
        private void Initialize_CommanderSkillData(string data)
        {
            if (string.IsNullOrEmpty(data))
            {
                userCommanderSkillData = new UserCommanderSkillData();

                // 슬롯 데이터 추가 (현재 2개로 제한)
                userCommanderSkillData.EquippedCommanderSkillIds.Add(0, 0);
                userCommanderSkillData.EquippedCommanderSkillIds.Add(1, 0);

                UpdateCommanderSkillState();

                return;
            }

            // 지휘자 스킬 상태 갱신
            //UpdateCommanderSkillState();

            userCommanderSkillData = MessageUtility.FromBase64String<UserCommanderSkillData>(data);
        }

        [Clear]
        private void Clear_CommanderSkillData()
        {
            userCommanderSkillData = null;
        }

        private void UpdateCommanderSkillState()
        {
            // 지휘자 스킬 상태 갱신
            int lastStageID = GetLatestClearUserStageID();
            var lastStageData = SpecDataManager.Instance.GetStageData(lastStageID);
            if (lastStageData != null)
            {
                var commanderList = SpecDataManager.Instance.GetCommanderSkillList(lastStageData.chapter_id);

                foreach (var commander in commanderList)
                {
                    if (commander.skill_value_type == SkillValueType.COOL) continue;

                    AddCommanderSkillData(commander.commander_skill_id, false);
                }

                SaveUserCommanderSKillData();
            }
        }

        public void SetEquippedCommanderSkill(int targetSlot, int commanderSkillID)
        {
            userCommanderSkillData.EquippedCommanderSkillIds[targetSlot] = commanderSkillID;
            SaveUserCommanderSKillData();
        }

        public int GetEquippedCommanderSkill(int targetSlot)
        {
            if (userCommanderSkillData.EquippedCommanderSkillIds.ContainsKey(targetSlot))
            {
                return userCommanderSkillData.EquippedCommanderSkillIds[targetSlot];
            }

            return 0;
        }

        public bool IsAllCommanderSkillsEquipped()
        {
            foreach (var skillId in userCommanderSkillData.EquippedCommanderSkillIds.Values)
            {
                if (skillId == 0)
                {
                    return false;
                }
            }
            return true;
        }

        public bool IsEquippedCommanderSkill(int skillID)
        {
            return userCommanderSkillData.EquippedCommanderSkillIds.Values.Any(value => value == skillID);
        }

        // 현재 장착 중인 지휘자 스킬 ID 리스트 반환
        public List<int> GetAllEquippedCommanderSkillIDList()
        {
            List<int> commanderSkillIDList = new List<int>();
            foreach (var commanderSkill in userCommanderSkillData.EquippedCommanderSkillIds)
            {
                if (commanderSkill.Value > 0)
                {
                    commanderSkillIDList.Add(commanderSkill.Value);
                }
            }

            return commanderSkillIDList;
        }

        public void AddCommanderSkillData(int commanderSkillID, bool needSave)
        {
            bool saveFlag = false;

            if (UserCommanderSkillData.UserCommanderSkillList.ToList()
                    .Exists(data => data.CommanderSkillId == commanderSkillID) == false)
            {
                var newCommanderSkill = new UserCommanderSkill();
                newCommanderSkill.CommanderSkillId = commanderSkillID;
                newCommanderSkill.Level = 1;

                UserCommanderSkillData.UserCommanderSkillList.Add(newCommanderSkill);

                saveFlag = true;
            }

            if (needSave && saveFlag)
            {
                SaveUserCommanderSKillData();
            }
        }

        // 지휘자 스킬 획득 여부 확인
        public bool IsOpenedCommanderSkill(int commanderSkillID)
        {
            return UserCommanderSkillData.UserCommanderSkillList.ToList().Exists(data => data.CommanderSkillId == commanderSkillID);
        }

        public void SaveUserCommanderSKillData()
        {
            HatcheryGrpcManager.Instance.SetPlayerDataAsync(DataCategory.UserCommanderSkillData.ToCategoryString(), userCommanderSkillData);
        }
    }
}
