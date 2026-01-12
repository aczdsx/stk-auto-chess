using System;
using System.Collections.Generic;
using R3;
using CookApps.AutoBattler;
using UnityEngine;

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
        public ulong GetCurrency(uint itemId)
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
        public bool HasEnoughCurrency(uint itemId, ulong requiredAmount)
        {
            return Model?.HasEnoughCurrency(itemId, requiredAmount) ?? false;
        }

        /// <summary>
        /// 특정 통화 보유 여부
        /// </summary>
        public bool HasCurrency(uint itemId)
        {
            return Model?.HasCurrency(itemId) ?? false;
        }

        /// <summary>
        /// 통화 개수
        /// </summary>
        public int CurrencyCount => Model?.CurrencyCount ?? 0;

        /// <summary>
        /// 여러 통화의 충분 여부 체크 (배치)
        /// </summary>
        public bool HasEnoughCurrencies(Dictionary<uint, ulong> requirements)
        {
            if (requirements == null || Model == null)
                return false;

            foreach (var kvp in requirements)
            {
                if (!Model.HasEnoughCurrency(kvp.Key, kvp.Value))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// 통화 변화량 계산
        /// </summary>
        public long GetCurrencyDelta(uint itemId, ulong previousAmount)
        {
            var currentAmount = GetCurrency(itemId);
            return (long)currentAmount - (long)previousAmount;
        }
    }
}
