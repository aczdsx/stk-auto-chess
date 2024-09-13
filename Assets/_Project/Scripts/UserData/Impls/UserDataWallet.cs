using System;
using System.Collections.Generic;
using CookApps.gRPC;
using Cookapps.Stkauto.V1;
using CookApps.TeamBattle.UIManagements;

namespace CookApps.AutoBattler
{
    public partial class UserDataManager
    {
        private UserWallet userWallet;

        public UserWallet UserWallet => userWallet;

        public static event Action<int> OnAPChanged;
        public static event Action<int> OnGoldChanged;
        public static event Action<int> OnJewelChanged;
        public static event Action<int> OnCTicketChanged;
        public static event Action<int> OnPVPTicketChanged;
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
                    Ap = 100,
                    PvpTicket = 10
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

        public bool CheckEnoughItem(ItemType itemType, int itemKey, int itemAmount, bool isShowToast)
        {
            switch (itemType)
            {
                case ItemType.GOLD:
                    if (userWallet.Gold < itemAmount && isShowToast) ToastManager.Instance.ShowToastByTokenKey("MSG_NOT_ENOUGH_GOLD");
                    return userWallet.Gold >= itemAmount;
                case ItemType.JEWEL:
                    if (userWallet.Jewel < itemAmount && isShowToast) ToastManager.Instance.ShowToastByTokenKey("MSG_NOT_ENOUGH_GACHA_JEWEL");
                    return userWallet.Jewel >= itemAmount;
                case ItemType.AP:
                    if (userWallet.Ap < itemAmount && isShowToast) ToastManager.Instance.ShowToastByTokenKey("MSG_NOT_ENOUGH_AP");
                    return userWallet.Ap >= itemAmount;
                case ItemType.C_TICKET:
                    if (userWallet.CTicket < itemAmount && isShowToast) ToastManager.Instance.ShowToastByTokenKey("MSG_NOT_ENOUGH_GACHA_C_TICKET");
                    return userWallet.CTicket >= itemAmount;
                case ItemType.PVP_TICKET:
                    if (userWallet.PvpTicket < itemAmount && isShowToast) ToastManager.Instance.ShowToastByTokenKey("MSG_NOT_ENOUGH_GACHA_PVP_TICKET");
                    return userWallet.PvpTicket >= itemAmount;
                case ItemType.CHAR_USER_EXP_ITEM:
                    if (userWallet.CharUserExpItem < itemAmount && isShowToast) ToastManager.Instance.ShowToastByTokenKey("MSG_NOT_ENOUGH_CHAR_EXP");
                    return userWallet.CharUserExpItem >= itemAmount;
                case ItemType.CHAR_USER_EXP_ITEM_2:
                    if (userWallet.CharUserExpItem2 < itemAmount && isShowToast) ToastManager.Instance.ShowToastByTokenKey("MSG_NOT_ENOUGH_CHAR_EXP_2");
                    return userWallet.CharUserExpItem2 >= itemAmount;
                case ItemType.CHARACTER_PIECE:
                    var userCharacter = GetUserCharacter(itemKey);
                    if (userCharacter != null)
                        if (userCharacter.CharacterPiece < itemAmount && isShowToast)
                            ToastManager.Instance.ShowToastByTokenKey("MSG_NOT_ENOUGH_CHAR_PIECE");
                    return userCharacter != null && userCharacter.CharacterPiece >= itemAmount;
            }

            return false;
        }

        public void SetItemCount(ItemType itemType, int itemKey, int itemAmount, bool isSave, bool needUpdateReddot)
        {
            switch (itemType)
            {
                case ItemType.GOLD:
                    userWallet.Gold = itemAmount;
                    OnGoldChanged?.Invoke(userWallet.Gold);
                    break;
                case ItemType.JEWEL:
                    userWallet.Jewel = itemAmount;
                    OnJewelChanged?.Invoke(userWallet.Jewel);
                    break;
                case ItemType.AP:
                    userWallet.Ap = itemAmount;
                    OnAPChanged?.Invoke(userWallet.Ap);
                    break;
                case ItemType.C_TICKET:
                    userWallet.CTicket = itemAmount;
                    OnCTicketChanged?.Invoke(userWallet.CTicket);
                    break;
                case ItemType.PVP_TICKET:
                    userWallet.PvpTicket = itemAmount;
                    OnPVPTicketChanged?.Invoke(userWallet.PvpTicket);
                    break;
                case ItemType.CHAR_USER_EXP_ITEM:
                    userWallet.CharUserExpItem = itemAmount;
                    OnCharUserExpItemChanged?.Invoke(userWallet.CharUserExpItem);
                    break;
                case ItemType.CHAR_USER_EXP_ITEM_2:
                    userWallet.CharUserExpItem2 = itemAmount;
                    OnCharUserExpItem2Changed?.Invoke(userWallet.CharUserExpItem2);
                    break;
                    // case ItemType.USER_EXP:
                    //     AddUserLevelExp(itemAmount);
                    //     break;
                    // case ItemType.CHARACTER:
                    //     // 최초 완성형 캐릭터 획득 처리 (20조각)
                    //     var specCharacter = SpecDataManager.Instance.GetCharacterData(itemKey);
                    //     if (IsHaveCharacter(specCharacter.character_id) == false)
                    //     {
                    //         AddNewCharacter(specCharacter.character_id);
                    //     }
                    //     else
                    //     {
                    //         IncreaseKnightPieceCount(specCharacter.character_id, specCharacter.need_piece);
                    //     }
                    //     break;
                    // case ItemType.CHARACTER_PIECE:
                    //     // 최초 완성형 캐릭터 획득 처리 (20조각)
                    //     var specCharacterPiece = SpecDataManager.Instance.GetCharacterData(itemKey);
                    //     if (IsHaveCharacter(specCharacterPiece.character_id) == false && itemAmount >= 20)
                    //     {
                    //         AddNewCharacter(specCharacterPiece.character_id);
                    //     }
                    //     else
                    //     {
                    //         IncreaseKnightPieceCount(specCharacterPiece.character_id, itemAmount);
                    //     }

                    break;
            }

            if (isSave) SaveUserWallet();

            if (needUpdateReddot)
            {
                // 메인 로비 레드닷 갱신
                var lobbyMain = SceneUILayerManager.Instance.GetUILayer<LobbyMain>();
                if (lobbyMain != null) lobbyMain.RefreshUI(LobbyMainRefreshType.REDDOT);
            }
        }

        public void IncreaseItem(ItemType itemType, int itemKey, int itemAmount, bool isSave, bool needUpdateReddot)
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
                    userWallet.Ap += itemAmount;
                    OnAPChanged?.Invoke(userWallet.Ap);
                    break;
                case ItemType.C_TICKET:
                    userWallet.CTicket += itemAmount;
                    OnCTicketChanged?.Invoke(userWallet.CTicket);
                    break;
                case ItemType.PVP_TICKET:
                    userWallet.PvpTicket += itemAmount;
                    OnPVPTicketChanged?.Invoke(userWallet.PvpTicket);
                    break;
                case ItemType.CHAR_USER_EXP_ITEM:
                    userWallet.CharUserExpItem += itemAmount;
                    OnCharUserExpItemChanged?.Invoke(userWallet.CharUserExpItem);
                    break;
                case ItemType.CHAR_USER_EXP_ITEM_2:
                    userWallet.CharUserExpItem2 += itemAmount;
                    OnCharUserExpItem2Changed?.Invoke(userWallet.CharUserExpItem2);
                    break;
                case ItemType.USER_EXP:
                    AddUserLevelExp(itemAmount);
                    break;
                case ItemType.CHARACTER:
                    // 최초 완성형 캐릭터 획득 처리 (20조각)
                    var specCharacter = SpecDataManager.Instance.GetCharacterData(itemKey);
                    if (IsHaveCharacter(specCharacter.character_id) == false)
                        AddNewCharacter(specCharacter.character_id);
                    else
                        IncreaseKnightPieceCount(specCharacter.character_id, specCharacter.need_piece);
                    break;
                case ItemType.CHARACTER_PIECE:
                    // 최초 완성형 캐릭터 획득 처리 (20조각)
                    var specCharacterPiece = SpecDataManager.Instance.GetCharacterData(itemKey);
                    if (IsHaveCharacter(specCharacterPiece.character_id) == false && itemAmount >= 20)
                        AddNewCharacter(specCharacterPiece.character_id);
                    else
                        IncreaseKnightPieceCount(specCharacterPiece.character_id, itemAmount);

                    break;
            }

            if (isSave) SaveUserWallet();

            if (needUpdateReddot)
            {
                // 메인 로비 레드닷 갱신
                var lobbyMain = SceneUILayerManager.Instance.GetUILayer<LobbyMain>();
                if (lobbyMain != null) lobbyMain.RefreshUI(LobbyMainRefreshType.REDDOT);
            }
        }

        public void IncreaseRewardItemList(List<RewardItem> rewardList, bool isSave)
        {
            if (rewardList == null || rewardList.Count == 0) return;

            // 리워드 적용
            foreach (var reward in rewardList) IncreaseItem(reward.Type, reward.Key, reward.Count, false, false);

            if (isSave) SaveUserWallet();

            // 메인 로비 레드닷 갱신
            var lobbyMain = SceneUILayerManager.Instance.GetUILayer<LobbyMain>();
            if (lobbyMain != null) lobbyMain.RefreshUI(LobbyMainRefreshType.REDDOT);
        }

        public void DecreaseItem(ItemType itemType, int itemKey, int itemAmount, bool isSave, bool needUpdateReddot)
        {
            switch (itemType)
            {
                case ItemType.GOLD:
                    userWallet.Gold -= itemAmount;
                    userWallet.Gold = Math.Max(0, userWallet.Gold);
                    OnGoldChanged?.Invoke(userWallet.Gold);
                    break;
                case ItemType.JEWEL:
                    userWallet.Jewel -= itemAmount;
                    userWallet.Jewel = Math.Max(0, userWallet.Jewel);
                    OnJewelChanged?.Invoke(userWallet.Jewel);
                    break;
                case ItemType.AP:
                    userWallet.Ap -= itemAmount;
                    userWallet.Ap = Math.Max(0, userWallet.Ap);
                    OnAPChanged?.Invoke(userWallet.Ap);
                    break;
                case ItemType.C_TICKET:
                    userWallet.CTicket -= itemAmount;
                    userWallet.CTicket = Math.Max(0, userWallet.CTicket);
                    OnCTicketChanged?.Invoke(userWallet.CTicket);
                    break;
                case ItemType.PVP_TICKET:
                    userWallet.PvpTicket -= itemAmount;
                    userWallet.PvpTicket = Math.Max(0, userWallet.PvpTicket);
                    OnPVPTicketChanged?.Invoke(userWallet.PvpTicket);
                    break;
                case ItemType.CHAR_USER_EXP_ITEM:
                    userWallet.CharUserExpItem -= itemAmount;
                    userWallet.CharUserExpItem = Math.Max(0, userWallet.CharUserExpItem);
                    OnCharUserExpItemChanged?.Invoke(userWallet.CharUserExpItem);
                    break;
                case ItemType.CHAR_USER_EXP_ITEM_2:
                    userWallet.CharUserExpItem2 -= itemAmount;
                    userWallet.CharUserExpItem2 = Math.Max(0, userWallet.CharUserExpItem2);
                    OnCharUserExpItem2Changed?.Invoke(userWallet.CharUserExpItem2);
                    break;
                case ItemType.CHARACTER_PIECE:
                    var specCharacter = SpecDataManager.Instance.GetCharacterData(itemKey);
                    DecreaseKnightPieceCount(specCharacter.character_id, itemAmount);
                    break;
            }

            if (isSave) SaveUserWallet();

            if (needUpdateReddot)
            {
                // 메인 로비 레드닷 갱신
                var lobbyMain = SceneUILayerManager.Instance.GetUILayer<LobbyMain>();
                if (lobbyMain != null) lobbyMain.RefreshUI(LobbyMainRefreshType.REDDOT);
            }
        }

        public void DecreaseRewardItemList(List<RewardItem> rewardList, bool isSave)
        {
            if (rewardList == null || rewardList.Count == 0) return;

            // 리워드 적용
            foreach (var reward in rewardList) DecreaseItem(reward.Type, reward.Key, reward.Count, false, false);

            if (isSave) SaveUserWallet();

            // 메인 로비 레드닷 갱신
            var lobbyMain = SceneUILayerManager.Instance.GetUILayer<LobbyMain>();
            if (lobbyMain != null) lobbyMain.RefreshUI(LobbyMainRefreshType.REDDOT);
        }

        public void SaveUserWallet()
        {
            GrpcManager.Instance.PlayerData.SetAsync(DataCategory.UserWallet.ToCategoryString(), userWallet);
        }
    }
}