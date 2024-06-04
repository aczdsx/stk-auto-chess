using System;
using System.Collections.Generic;
using Cookapps.Autobattleproject.V1;
using CookApps.gRPC.Hatchery;
using CookApps.gRPC.Universal;

namespace CookApps.AutoBattler
{
    public partial class UserDataManager
    {
        private UserWallet userWallet;

        public UserWallet UserWallet => userWallet;

        public static event Action<int> OnEnergyChanged;
        public static event Action<int> OnGoldChanged;
        public static event Action<int> OnJewelChanged;

        [Initialize(DataCategory.UserWallet)]
        private void Initialize_Wallet(string data)
        {
            if (string.IsNullOrEmpty(data))
            {
                userWallet = new UserWallet
                {
                    Gold = 0,
                    Jewel = 0,
                    Energy = 100,
                };
                return;
            }

            userWallet = MessageUtility.FromBase64String<UserWallet>(data);
        }

        [Clear]
        private void Clear_Wallet()
        {
            userWallet = null;
        }

        public void IncreaseItem(ItemType type, int amount, bool isSave)
        {
            switch (type)
            {
                case ItemType.GOLD:
                    userWallet.Gold += amount;
                    OnGoldChanged?.Invoke(userWallet.Gold);
                    break;
                case ItemType.JEWEL:
                    userWallet.Jewel += amount;
                    OnJewelChanged?.Invoke(userWallet.Jewel);
                    break;
                case ItemType.AP:
                    userWallet.Energy += amount;
                    OnEnergyChanged?.Invoke(userWallet.Energy);
                    break;
            }

            if (isSave)
            {
                SaveUserWallet();
            }
        }

        public void IncreaseRewardItemList(List<RewardItem> rewardList, bool isSave)
        {
            if (rewardList == null || rewardList.Count == 0) return;

            // 리워드 적용
            foreach (var reward in rewardList)
            {
                IncreaseItem(reward.Type, reward.Count, false);
            }

            if (isSave)
            {
                SaveUserWallet();
            }
        }

        public void DecreaseItem(ItemType type, int amount, bool isSave)
        {
            switch (type)
            {
                case ItemType.GOLD:
                    userWallet.Gold -= amount;
                    OnGoldChanged?.Invoke(userWallet.Gold);
                    break;
                case ItemType.JEWEL:
                    userWallet.Jewel -= amount;
                    OnJewelChanged?.Invoke(userWallet.Jewel);
                    break;
                case ItemType.AP:
                    userWallet.Energy -= amount;
                    OnEnergyChanged?.Invoke(userWallet.Energy);
                    break;
            }

            if (isSave)
            {
                SaveUserWallet();
            }
        }

        public void SaveUserWallet()
        {
            HatcheryGrpcManager.Instance.SetPlayerDataAsync(DataCategory.UserWallet.ToCategoryString(), userWallet);
        }
    }
}
