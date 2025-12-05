using System;
using System.Collections.Generic;
using Tech.Hive.V1;
using UnityEngine;

namespace CookApps.AutoBattler.Data
{
    /// <summary>
    /// 지갑 데이터 모델 (통화 관리)
    /// CurrencyDelta를 사용한 델타 업데이트 지원
    /// </summary>
    public class WalletModel : IDataModel
    {
        public const string CATEGORY_KEY = "wallet";

        // 통화 데이터 (ItemId -> Amount)
        private readonly Dictionary<uint, ulong> _currencies;

        // 버전 정보
        private int _version;

        public string CategoryKey => CATEGORY_KEY;
        public int Version => _version;

        // 통화 변경 이벤트
        public event Action<uint, ulong, ulong> OnCurrencyChanged; // (itemId, oldAmount, newAmount)

        public WalletModel()
        {
            _currencies = new Dictionary<uint, ulong>(16);
            _version = 0;
        }

        /// <summary>
        /// 델타 업데이트 적용
        /// </summary>
        public void ApplyDelta(IDataModel delta)
        {
            if (delta is not WalletModel walletDelta)
            {
                Debug.LogError("[WalletModel] Invalid delta type");
                return;
            }

            // 변경된 통화만 업데이트
            foreach (var kvp in walletDelta._currencies)
            {
                var itemId = kvp.Key;
                var newAmount = kvp.Value;

                _currencies.TryGetValue(itemId, out var oldAmount);
                _currencies[itemId] = newAmount;

                OnCurrencyChanged?.Invoke(itemId, oldAmount, newAmount);
            }

            _version = walletDelta._version;
        }

        /// <summary>
        /// 데이터 초기화
        /// </summary>
        public void Reset()
        {
            _currencies.Clear();
            _version = 0;
        }

        /// <summary>
        /// 유효성 검증
        /// </summary>
        public bool Validate()
        {
            // 음수 통화 체크
            foreach (var amount in _currencies.Values)
            {
                if (amount < 0)
                {
                    Debug.LogError("[WalletModel] Invalid currency: negative amount");
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// 통화 가져오기
        /// </summary>
        public ulong GetCurrency(uint itemId)
        {
            return _currencies.TryGetValue(itemId, out var amount) ? amount : 0;
        }

        /// <summary>
        /// 통화 설정 (내부용)
        /// </summary>
        internal void SetCurrency(uint itemId, ulong amount)
        {
            _currencies.TryGetValue(itemId, out var oldAmount);
            _currencies[itemId] = amount;

            OnCurrencyChanged?.Invoke(itemId, oldAmount, amount);
            _version++;
        }

        /// <summary>
        /// CurrencyDelta 배열로 업데이트
        /// </summary>
        internal void ApplyCurrencyDeltas(IEnumerable<CurrencyDelta> deltas)
        {
            if (deltas == null) return;

            foreach (var delta in deltas)
            {
                var oldAmount = _currencies.TryGetValue(delta.ItemId, out var current) ? current : 0;
                _currencies[delta.ItemId] = delta.After;

                OnCurrencyChanged?.Invoke(delta.ItemId, oldAmount, delta.After);
            }

            _version++;
        }

        /// <summary>
        /// 모든 통화 가져오기
        /// </summary>
        public void GetAllCurrencies(Dictionary<uint, ulong> output)
        {
            if (output == null) return;

            output.Clear();
            foreach (var kvp in _currencies)
            {
                output[kvp.Key] = kvp.Value;
            }
        }

        /// <summary>
        /// 통화 개수
        /// </summary>
        public int CurrencyCount => _currencies.Count;

        /// <summary>
        /// 특정 통화 보유 여부
        /// </summary>
        public bool HasCurrency(uint itemId)
        {
            return _currencies.ContainsKey(itemId) && _currencies[itemId] > 0;
        }

        /// <summary>
        /// 통화 충분 여부 체크
        /// </summary>
        public bool HasEnoughCurrency(uint itemId, ulong requiredAmount)
        {
            return GetCurrency(itemId) >= requiredAmount;
        }
    }
}
