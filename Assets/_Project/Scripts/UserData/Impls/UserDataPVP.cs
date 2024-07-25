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
        private UserPVP userPVP;

        public UserPVP UserPVP => userPVP;

        [Initialize(DataCategory.UserPvp, 12)]
        private void Initialize_PVPData(string data)
        {
            if (string.IsNullOrEmpty(data))
            {
                userPVP = new UserPVP();
                userPVP.RecentSeasonId = 1;
                userPVP.RankId = 0;
                userPVP.RankPoint = 0;
                userPVP.Ranking = 0;

                return;
            }

            userPVP = MessageUtility.FromBase64String<UserPVP>(data);
        }

        [Clear]
        private void Clear_PVPData()
        {
            userPVP = null;
        }

        public void SaveUserPVPData()
        {
            HatcheryGrpcManager.Instance.SetPlayerDataAsync(DataCategory.UserPvp.ToCategoryString(), userPVP);
        }
        
        // 매칭 리스트에 데이터 추가
        public void AddPVPMatchingList(UserPVPBattleData pvpData, bool needSave)
        {
            if (pvpData == null) return;
            
            UserPVP.CurrentPvpMatchingList.Add(pvpData);
            
            if (needSave)
            {
                SaveUserPVPData();
            }
        }
        
        // 매칭 리스트에서 데이터 반환 (단일)
        public UserPVPBattleData GetPVPMatchingData(int playerID)
        {
            if (playerID <= 0) return null;
            
            return UserPVP.CurrentPvpMatchingList.ToList().Find(x => x.PlayerId == playerID);
        }
        
        // 매칭 리스트에서 데이터 반환 (전체)
        public List<UserPVPBattleData> GetPVPMatchingDataList()
        {
            return UserPVP.CurrentPvpMatchingList.ToList();
        }
    }
}
