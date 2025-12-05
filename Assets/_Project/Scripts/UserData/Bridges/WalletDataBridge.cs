using System;
using System.Collections.Generic;
using CookApps.AutoBattler.Data;
using UnityEngine;

namespace CookApps.AutoBattler.UI
{
    /// <summary>
    /// 지갑 데이터 브릿지
    /// ServerDataManager와 UI 사이의 중간 레이어
    /// </summary>
    public class WalletDataBridge
    {
        private WalletModel _model;
        private ServerDataManager _dataManager;
        private DataEventBus _eventBus;

        // UI 갱신 이벤트 (itemId별)
        public event Action<uint, ulong> OnCurrencyChanged;
        public event Action OnWalletChanged;

        public WalletDataBridge()
        {
            _dataManager = ServerDataManager.Instance;
            _eventBus = DataEventBus.Instance;

            // 데이터 모델 가져오기
            _model = _dataManager.GetData<WalletModel>(WalletModel.CATEGORY_KEY);
            if (_model == null)
            {
                _model = new WalletModel();
                _dataManager.RegisterFactory(WalletModel.CATEGORY_KEY, () => new WalletModel());
                _dataManager.SetData(WalletModel.CATEGORY_KEY, _model);
            }

            // 이벤트 구독
            SubscribeEvents();
        }

        /// <summary>
        /// 이벤트 구독
        /// </summary>
        private void SubscribeEvents()
        {
            // 데이터 변경 감지
            _eventBus.Subscribe(WalletModel.CATEGORY_KEY, OnDataChanged);

            // 모델 이벤트 구독
            _model.OnCurrencyChanged += OnCurrencyChangedInternal;
        }

        /// <summary>
        /// 이벤트 구독 해제
        /// </summary>
        public void Dispose()
        {
            _eventBus.Unsubscribe(WalletModel.CATEGORY_KEY, OnDataChanged);

            if (_model != null)
            {
                _model.OnCurrencyChanged -= OnCurrencyChangedInternal;
            }
        }

        /// <summary>
        /// 데이터 변경 콜백
        /// </summary>
        private void OnDataChanged(DataChangeEvent changeEvent)
        {
            OnWalletChanged?.Invoke();
        }

        private void OnCurrencyChangedInternal(uint itemId, ulong oldAmount, ulong newAmount)
        {
            OnCurrencyChanged?.Invoke(itemId, newAmount);
        }

        /// <summary>
        /// 통화 가져오기
        /// </summary>
        public ulong GetCurrency(uint itemId)
        {
            return _model?.GetCurrency(itemId) ?? 0;
        }

        /// <summary>
        /// 모든 통화 가져오기
        /// </summary>
        public void GetAllCurrencies(Dictionary<uint, ulong> output)
        {
            _model?.GetAllCurrencies(output);
        }

        /// <summary>
        /// 통화 충분 여부
        /// </summary>
        public bool HasEnoughCurrency(uint itemId, ulong requiredAmount)
        {
            return _model?.HasEnoughCurrency(itemId, requiredAmount) ?? false;
        }

        /// <summary>
        /// 특정 통화 보유 여부
        /// </summary>
        public bool HasCurrency(uint itemId)
        {
            return _model?.HasCurrency(itemId) ?? false;
        }

        /// <summary>
        /// 통화 개수
        /// </summary>
        public int CurrencyCount => _model?.CurrencyCount ?? 0;

        /// <summary>
        /// 여러 통화의 충분 여부 체크 (배치)
        /// </summary>
        public bool HasEnoughCurrencies(Dictionary<uint, ulong> requirements)
        {
            if (requirements == null || _model == null)
                return false;

            foreach (var kvp in requirements)
            {
                if (!_model.HasEnoughCurrency(kvp.Key, kvp.Value))
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
