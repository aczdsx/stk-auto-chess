using System;
using System.Collections.Generic;
using System.Linq;
using CookApps.gRPC;
using Cookapps.Stkauto.V1;
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
            var lastStageID = GetLatestClearUserStageID();
            var lastStageData = SpecDataManager.Instance.GetStageData(lastStageID);
            if (lastStageData != null)
            {
                var commanderList = SpecDataManager.Instance.GetCommanderSkillIncludeList(lastStageData.chapter_id);

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

        public int GetEquippedCommanderSkillID(int targetSlot)
        {
            if (userCommanderSkillData.EquippedCommanderSkillIds.ContainsKey(targetSlot))
            {
                return userCommanderSkillData.EquippedCommanderSkillIds[targetSlot];
            }
            return 0;
        }

        public int GetUserCommanderSkillLevel(int commanderSkillID)
        {
            foreach(var userCommanderSkill in userCommanderSkillData.UserCommanderSkillList)
            {
                if(userCommanderSkill.CommanderSkillId == commanderSkillID)
                {
                    return userCommanderSkill.Level;// 승급 5레ㅂㄹ +  어떤스킬 
                }
            }
            return 0;
        }

        public bool IsAllCommanderSkillsEquipped(int slotCount)
        {
            foreach (var skillId in userCommanderSkillData.EquippedCommanderSkillIds.Values.Take(slotCount))
                if (skillId == 0)
                    return false;
            return true;
        }

        public bool IsEquippedCommanderSkill(int skillID)
        {
            return userCommanderSkillData.EquippedCommanderSkillIds.Values.Any(value => value == skillID);
        }

        // 현재 장착 중인 지휘자 스킬 ID 리스트 반환
        public List<int> GetAllEquippedCommanderSkillIDList()
        {
            var commanderSkillIDList = new List<int>();
            foreach (var commanderSkill in userCommanderSkillData.EquippedCommanderSkillIds)
                if (commanderSkill.Value > 0)
                    commanderSkillIDList.Add(commanderSkill.Value);

            return commanderSkillIDList;
        }

        public void AddCommanderSkillData(int commanderSkillID, bool needSave)
        {
            var saveFlag = false;

            if (UserCommanderSkillData.UserCommanderSkillList.ToList()
                    .Exists(data => data.CommanderSkillId == commanderSkillID) == false)
            {
                var newCommanderSkill = new UserCommanderSkill();
                newCommanderSkill.CommanderSkillId = commanderSkillID;
                newCommanderSkill.Level = 1;

                UserCommanderSkillData.UserCommanderSkillList.Add(newCommanderSkill);

                saveFlag = true;
            }

            if (needSave && saveFlag) SaveUserCommanderSKillData();
        }

        // 지휘자 스킬 획득 여부 확인
        public bool IsOpenedCommanderSkill(int commanderSkillID)
        {
            return UserCommanderSkillData.UserCommanderSkillList.ToList()
                .Exists(data => data.CommanderSkillId == commanderSkillID);
        }

        public void SaveUserCommanderSKillData()
        {
            QueueSave(DataCategory.UserCommanderSkillData.ToCategoryString(), userCommanderSkillData);
        }
    }
}