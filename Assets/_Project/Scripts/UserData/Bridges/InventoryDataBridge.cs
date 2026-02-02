using System.Collections.Generic;
using R3;

namespace CookApps.AutoBattler
{
    /// <summary>
    /// 인벤토리 데이터 브릿지
    /// ServerDataManager와 UI 사이의 중간 레이어
    /// </summary>
    public class InventoryDataBridge : DataBridgeBase
    {
        private InventoryModel Model;
        // Public Observable 노출
        public Observable<(uint itemId, ulong oldAmount, ulong newAmount)> OnCurrencyChanged;

        public InventoryDataBridge()
        {
            Model = ServerDataManager.Instance.Inventory;
            OnCurrencyChanged = Model.OnCurrencyChanged;
        }

        /// <summary>
        /// 통화 가져오기
        /// </summary>
        public ulong GetCurrency(ItemId itemId)
        {
            return Model?.GetCurrency(itemId) ?? 0;
        }

        /// <summary>
        /// 모든 통화 가져오기
        /// </summary>
        public void GetAllCurrencies(Dictionary<uint, ulong> output)
        {
            Model?.GetAllCurrencies(output);
        }

        /// <summary>
        /// 통화 충분 여부
        /// </summary>
        public bool HasEnoughCurrency(ItemId itemId, ulong requiredAmount, bool showToast = true)
        {
            bool hasEnough = Model?.HasEnoughCurrency(itemId, requiredAmount) ?? false;
            if (!hasEnough && showToast)
            {
                string toastKey = GetNotEnoughCurrencyToastKey(itemId);
                ToastManager.Instance.ShowToastByTokenKey(toastKey);
            }
            return hasEnough;
        }

        private string GetNotEnoughCurrencyToastKey(ItemId itemId)
        {
            if (itemId == IdMap.Item.Gold)
                return "MSG_NOT_ENOUGH_GOLD";
            if (itemId == IdMap.Item.ActionPoint)
                return "MSG_NOT_ENOUGH_AP";
            if (itemId == IdMap.Item.Jewel)
                return "MSG_NOT_ENOUGH_GACHA_JEWEL";
            if (itemId == IdMap.Item.CharacterTicket)
                return "MSG_NOT_ENOUGH_GACHA_C_TICKET";
            if (itemId == IdMap.Item.CharExp)
                return "MSG_NOT_ENOUGH_CHAR_EXP";
            if (itemId == IdMap.Item.Soul)
                return "MSG_NOT_ENOUGH_CHAR_EXP_2";
            if (itemId.IsCharacterPiece())
                return "MSG_NOT_ENOUGH_CHAR_PIECE";
            return "MSG_NOT_ENOUGH_CURRENCY";
        }

        /// <summary>
        /// 특정 통화 보유 여부
        /// </summary>
        public bool HasCurrency(ItemId itemId)
        {
            return Model?.HasCurrency(itemId) ?? false;
        }

        /// <summary>
        /// 통화 개수
        /// </summary>
        public int CurrencyCount => Model?.CurrencyCount ?? 0;

        /// <summary>
        /// ActionPoint 데이터
        /// </summary>
        public ActionPoint ActionPoint => Model?.ActionPoint;

        /// <summary>
        /// 통화 변화량 계산
        /// </summary>
        public long GetCurrencyDelta(ItemId itemId, ulong previousAmount)
        {
            var currentAmount = GetCurrency(itemId);
            return (long)currentAmount - (long)previousAmount;
        }
    }
}
