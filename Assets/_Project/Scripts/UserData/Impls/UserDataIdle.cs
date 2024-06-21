using System;
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
                    //LastRewardGetTimestamp = 1718582400,
                    LastRewardGetTimestamp = TimeManager.Instance.UtcNowTimeStamp(),
                };
                return;
            }

            userIdleData = MessageUtility.FromBase64String<UserIdleData>(data);
        }

        [Clear]
        private void Clear_IdleData()
        {
            userStageGroup = null;
        }

        // 현재 시간을 기준으로 보상 수령 시간 갱신
        public void RefreshLastRewardGetTime()
        {
            UserIdleData.LastRewardGetTimestamp = TimeManager.Instance.UtcNowTimeStamp();

            SaveUserIdle();
        }

        // 현재 마지막 보상 수령 타임 스탬프 기준 현재 누적 방치 보상 리스트 반환
        public List<RewardItem> GetCurrentIdleRewardItemList()
        {
            List<RewardItem> resultItemList = new List<RewardItem>();

            int lastStageID = GetLastUserStageID();
            var lastStageData = SpecDataManager.Instance.GetStageData(lastStageID);

            int totalStageClearCount = GetAllClearUserStageList().Count;
            var specIdleRewardList = SpecDataManager.Instance.GetAllIdleRewardList(lastStageData.chapter_id);

            TimeSpan currentRewardTimeSpan = TimeManager.Instance.GetTimeSpanFromNow(UserIdleData.LastRewardGetTimestamp);

            int maxMinute = SpecDataManager.Instance.GetGameConfig<int>("idle_reward_acc_time_limit");
            int diffMinute = Mathf.Min((int)currentRewardTimeSpan.TotalMinutes, maxMinute);

            // 보상 데이터 생성
            foreach (var idleReward in specIdleRewardList)
            {
                double baseAmount = idleReward.min_count;
                double addAmount = idleReward.add_count * (double)totalStageClearCount;
                double totalAmount = baseAmount + addAmount;    // 기본 지급 갯수 + 스테이지 클리어 보너스

                int timeCount = diffMinute / idleReward.supply_time_m; // 누적 시간 기반 보상 갯수

                int resultAmount = (int) Math.Truncate(totalAmount * timeCount);

                if (resultAmount > 0)
                {
                    RewardItem rewardItem = new RewardItem
                    {
                        Type = idleReward.item_type,
                        Key = 0,
                        Count = resultAmount,
                    };

                    resultItemList.Add(rewardItem);
                }
            }

            return resultItemList;
        }

        public void SaveUserIdle()
        {
            HatcheryGrpcManager.Instance.SetPlayerDataAsync(DataCategory.UserIdleData.ToCategoryString(), userIdleData);
        }
    }
}
