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
        public static event Action<int> OnCTicketChanged;
        public static event Action<int> OnCharUserExpItemChanged;
        public static event Action<int> OnCharUserExpItem2Changed;

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

        public void IncreaseItem(ItemType itemType, int itemKey, int itemAmount, bool isSave)
        {
            switch (itemType)
            {
                case ItemType.GOLD:
                    userWallet.Gold += itemAmount;
                    OnGoldChanged?.Invoke(userWallet.Gold);
                    break;
                case ItemType.JEWEL:
                    userWallet.Jewel += itemAmount;
                    OnJewelChanged?.Invoke(userWallet.Jewel);
                    break;
                case ItemType.AP:
                    userWallet.Energy += itemAmount;
                    OnEnergyChanged?.Invoke(userWallet.Energy);
                    break;
                case ItemType.C_TICKET:
                    userWallet.CTicket += itemAmount;
                    OnCTicketChanged?.Invoke(userWallet.CTicket);
                    break;
                case ItemType.CHAR_USER_EXP_ITEM:
                    userWallet.CharUserExpItem += itemAmount;
                    OnCharUserExpItemChanged?.Invoke(userWallet.CharUserExpItem);
                    break;
                case ItemType.CHAR_USER_EXP_ITEM_2:
                    userWallet.CharUserExpItem2 += itemAmount;
                    OnCharUserExpItem2Changed?.Invoke(userWallet.CharUserExpItem2);
                    break;
                case ItemType.CHARACTER:
                    // 최초 완성형 캐릭터 획득 처리 (20조각)
                    var specCharacter = SpecDataManager.Instance.GetCharacterData(itemKey);
                    if (IsHaveCharacter(specCharacter.character_id) == false)
                    {
                        AddNewCharacter(specCharacter.character_id);
                    }
                    else
                    {
                        IncreaseKnightPieceCount(specCharacter.character_id, specCharacter.need_piece);
                    }
                    break;
                case ItemType.CHARACTER_PIECE:
                    // 최초 완성형 캐릭터 획득 처리 (20조각)
                    var specCharacterPiece = SpecDataManager.Instance.GetCharacterData(itemKey);
                    if (IsHaveCharacter(specCharacterPiece.character_id) == false && itemAmount >= 20)
                    {
                        AddNewCharacter(specCharacterPiece.character_id);
                    }
                    else
                    {
                        IncreaseKnightPieceCount(specCharacterPiece.character_id, itemAmount);
                    }

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
                IncreaseItem(reward.Type, reward.Key, reward.Count, false);
            }

            if (isSave)
            {
                SaveUserWallet();
            }
        }

        public void DecreaseItem(ItemType itemType, int itemKey, int itemAmount, bool isSave)
        {
            switch (itemType)
            {
                case ItemType.GOLD:
                    userWallet.Gold -= itemAmount;
                    OnGoldChanged?.Invoke(userWallet.Gold);
                    break;
                case ItemType.JEWEL:
                    userWallet.Jewel -= itemAmount;
                    OnJewelChanged?.Invoke(userWallet.Jewel);
                    break;
                case ItemType.AP:
                    userWallet.Energy -= itemAmount;
                    OnEnergyChanged?.Invoke(userWallet.Energy);
                    break;
                case ItemType.C_TICKET:
                    userWallet.CTicket -= itemAmount;
                    OnCTicketChanged?.Invoke(userWallet.CTicket);
                    break;
                case ItemType.CHAR_USER_EXP_ITEM:
                    userWallet.CharUserExpItem -= itemAmount;
                    OnCharUserExpItemChanged?.Invoke(userWallet.CharUserExpItem);
                    break;
                case ItemType.CHAR_USER_EXP_ITEM_2:
                    userWallet.CharUserExpItem2 -= itemAmount;
                    OnCharUserExpItem2Changed?.Invoke(userWallet.CharUserExpItem2);
                    break;
                case ItemType.CHARACTER_PIECE:
                    var specCharacter = SpecDataManager.Instance.GetCharacterData(itemKey);
                    DecreaseKnightPieceCount(specCharacter.character_id, itemAmount);
                    break;
            }

            if (isSave)
            {
                SaveUserWallet();
            }
        }

        public void DecreaseRewardItemList(List<RewardItem> rewardList, bool isSave)
        {
            if (rewardList == null || rewardList.Count == 0) return;

            // 리워드 적용
            foreach (var reward in rewardList)
            {
                DecreaseItem(reward.Type, reward.Key, reward.Count, false);
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
