using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cookapps.Stkauto.V1;
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

                CreateDungeonTrialDataList();

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

        // 가장 마지막 시련 던전 데이터 반환
        public UserTrialDungeonData GetLastTrialDungeonData()
        {
            var clearDungeonList = GetTrialDungeonDataList(DungeonStateType.CLEAR);

            // 아무것도 클리어 하지 않은 경우 가장 첫번째 던전 데이터 반환
            if (clearDungeonList == null || clearDungeonList.Count <= 0)
            {
                var firstDungeonData = SpecDataManager.Instance.GetSpecDungeonTrialDataByOrder(1);
                if (firstDungeonData != null)
                {
                    return GetTrialDungeonData(firstDungeonData.dungeon_id);
                }
            }

            int maxOrder = clearDungeonList.Max(data => data.Order);
            var lastDungeonData = SpecDataManager.Instance.GetSpecDungeonTrialDataByOrder(maxOrder);
            return GetTrialDungeonData(lastDungeonData.dungeon_id);
        }

        public List<UserTrialDungeonData> GetTrialDungeonDataList(DungeonStateType stateType)
        {
            return userDungeon.UserTrialDungeonDatas.Values.Where(x => x.DungeonStateType == (int)stateType).ToList();
        }

        public void SetTrialDungeonData(int dungeonID, DungeonStateType stateType, bool needSave)
        {
            if (userDungeon.UserTrialDungeonDatas.ContainsKey(dungeonID))
            {
                userDungeon.UserTrialDungeonDatas[dungeonID].DungeonStateType = (int)stateType;

                // 던전 클리어 시 연관 데이터 처리
                if (stateType == DungeonStateType.CLEAR)
                {
                    var specDungeonData = SpecDataManager.Instance.GetSpecDungeonTrialData(dungeonID);
                    if (specDungeonData != null)
                    {
                        // 최대 배치 기사 수 증가
                        SetMaxSquadCount(specDungeonData.squad_count, needSave);
                    }
                }
            }

            if (needSave)
            {
                SaveUserDungeonData();
            }
        }

        private void CreateDungeonTrialDataList()
        {
            // 전체 시련 던전 데이터 생성
            var dungeonDataList = SpecDataManager.Instance.GetSpecDungeonTrialDataList(DungeonType.TRIAL);
            foreach (var dungeonData in dungeonDataList)
            {
                if (userDungeon.UserTrialDungeonDatas.ContainsKey(dungeonData.dungeon_id)) continue;

                userDungeon.UserTrialDungeonDatas.Add(dungeonData.dungeon_id, new UserTrialDungeonData
                {
                    DungeonId = dungeonData.dungeon_id,
                    Order = dungeonData.order,
                    DungeonStateType = (int)DungeonStateType.WAIT,
                });
            }
        }

        #endregion
    }
}

