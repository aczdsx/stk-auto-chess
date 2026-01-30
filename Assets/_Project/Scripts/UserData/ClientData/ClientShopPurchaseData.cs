using System.Collections.Generic;
using System.Linq;
using MemoryPack;

namespace CookApps.AutoBattler
{
    [MemoryPackable]
    public partial class ClientShopPurchaseData : ClientDataBase
    {
        public const string CategoryName = "client_shop_purchase";
        public override string Category => CategoryName;

        public static ClientShopPurchaseData Get() => ClientDataManager.Instance.GetData<ClientShopPurchaseData>(CategoryName);

        [MemoryPackOrder(0)] private Dictionary<int, ShopBannerData> _shopBannerDatas = new();
        [MemoryPackOrder(1)] private Dictionary<int, PurchaseData> _purchaseDatas = new();

        public IReadOnlyDictionary<int, ShopBannerData> ShopBannerDatas => _shopBannerDatas;
        public IReadOnlyDictionary<int, PurchaseData> PurchaseDatas => _purchaseDatas;

        #region Purchase Data

        public PurchaseData GetPurchaseData(int shopId)
        {
            return _purchaseDatas.GetValueOrDefault(shopId);
        }

        public void SetPurchaseCount(int shopId, int value, bool isAdd)
        {
            if (!_purchaseDatas.ContainsKey(shopId))
            {
                _purchaseDatas[shopId] = new PurchaseData { ShopId = shopId };
            }

            if (isAdd)
                _purchaseDatas[shopId].PurchaseCount += value;
            else
                _purchaseDatas[shopId].PurchaseCount = value;

            _purchaseDatas[shopId].PurchaseTimestamp = TimeManager.Instance.UtcNowTimeStampLocal();
            SetDirty();
        }

        public bool CheckPurchaseLimitCount(int shopId)
        {
            if (!_purchaseDatas.ContainsKey(shopId)) return true; // 구매 기록이 없으면 구매 가능

            var specShopData = SpecDataManager.Instance.GetShopData(shopId);
            if (specShopData == null) return false;

            return _purchaseDatas[shopId].PurchaseCount < specShopData.buy_limit_count;
        }

        #endregion

        #region Shop Banner Data

        public ShopBannerData GetShopBannerData(int shopId)
        {
            return _shopBannerDatas.GetValueOrDefault(shopId);
        }

        public List<ShopBannerData> GetAllShopBannerDataList()
        {
            return _shopBannerDatas.Values.ToList();
        }

        public void SetShopBannerConditionValue(int shopId, int value, bool isAdd)
        {
            if (!_shopBannerDatas.ContainsKey(shopId)) return;

            if (isAdd)
                _shopBannerDatas[shopId].ShowConditionValue += value;
            else
                _shopBannerDatas[shopId].ShowConditionValue = value;

            // 컨디션에 따른 상태 업데이트
            var specShopBannerData = SpecDataManager.Instance.GetShopBannerData(shopId);
            if (specShopBannerData != null &&
                _shopBannerDatas[shopId].ShowConditionValue >= specShopBannerData.condition_count)
            {
                SetShopBannerStateType(shopId, ShopBannerStateType.ACTIVE);

                // 판매 종료 시간이 있는 타입일 경우 처리
                var specShopData = SpecDataManager.Instance.GetShopData(shopId);
                if (specShopData != null &&
                    (specShopData.shop_term_type == ShopTermType.TIME || specShopData.shop_term_type == ShopTermType.PERIOD_TIME))
                {
                    var endTimeStamp = TimeManager.Instance.AddMinuteTimeStamp(specShopData.duration_time);
                    SetShopBannerEndPurchaseTime(shopId, endTimeStamp);
                }
            }

            SetDirty();
        }

        public void SetShopBannerShowCount(int shopId, int value, bool isAdd)
        {
            if (!_shopBannerDatas.ContainsKey(shopId)) return;

            if (isAdd)
                _shopBannerDatas[shopId].ShowCount += value;
            else
                _shopBannerDatas[shopId].ShowCount = value;

            SetDirty();
        }

        public void SetShopBannerStateType(int shopId, ShopBannerStateType stateType)
        {
            if (!_shopBannerDatas.ContainsKey(shopId)) return;

            _shopBannerDatas[shopId].ShopBannerStateType = stateType;
            SetDirty();
        }

        public void SetShopBannerEndPurchaseTime(int shopId, long endTimeStamp)
        {
            if (!_shopBannerDatas.ContainsKey(shopId)) return;

            _shopBannerDatas[shopId].EndPurchaseTimestamp = endTimeStamp;
            SetDirty();
        }

        public void ResetShopBannerData(int shopId)
        {
            if (!_shopBannerDatas.ContainsKey(shopId)) return;

            _shopBannerDatas[shopId].ShowConditionValue = 0;
            _shopBannerDatas[shopId].ShowCount = 0;
            _shopBannerDatas[shopId].ShopBannerStateType = ShopBannerStateType.INACTIVE;
            _shopBannerDatas[shopId].EndPurchaseTimestamp = 0;

            SetDirty();
        }

        public void AddShopBannerData(int shopId)
        {
            if (_shopBannerDatas.ContainsKey(shopId)) return;

            var specShopData = SpecDataManager.Instance.GetShopData(shopId);
            var specShopBannerData = SpecDataManager.Instance.GetShopBannerData(shopId);

            if (specShopData == null || specShopBannerData == null) return;

            _shopBannerDatas[shopId] = new ShopBannerData
            {
                ShopId = shopId,
                ShowConditionValue = 0,
                ShopBannerStateType = ShopBannerStateType.INACTIVE,
                ShowCount = 0,
                EndPurchaseTimestamp = 0
            };

            SetDirty();
        }

        public void RemoveShopBannerData(int shopId)
        {
            if (!_shopBannerDatas.ContainsKey(shopId)) return;

            _shopBannerDatas.Remove(shopId);
            SetDirty();
        }

        #endregion

        #region Validation

        public bool CheckValidShopPeriod(int shopId)
        {
            var targetShopData = SpecDataManager.Instance.GetShopData(shopId);
            if (targetShopData == null) return false;

            if (targetShopData.shop_term_type == ShopTermType.PERIOD || targetShopData.shop_term_type == ShopTermType.PERIOD_TIME)
            {
                var startAtTimeStamp = TimeManager.Instance.ChangeDateStringToTimeStamp(targetShopData.start_at);
                var endAtTimeStamp = TimeManager.Instance.ChangeDateStringToTimeStamp(targetShopData.end_at);

                return startAtTimeStamp <= TimeManager.Instance.UtcNowTimeStampLocal() &&
                       TimeManager.Instance.UtcNowTimeStampLocal() < endAtTimeStamp;
            }

            return false;
        }

        public bool CheckValidShopTime(int shopId)
        {
            var targetShopData = SpecDataManager.Instance.GetShopData(shopId);
            if (targetShopData == null) return false;

            var targetShopBannerData = GetShopBannerData(shopId);
            if (targetShopBannerData == null) return false;

            if (targetShopData.shop_term_type == ShopTermType.TIME || targetShopData.shop_term_type == ShopTermType.PERIOD_TIME)
                return TimeManager.Instance.UtcNowTimeStampLocal() < targetShopBannerData.EndPurchaseTimestamp;

            return false;
        }

        #endregion

        #region Update

        public void UpdateShopBannerData()
        {
            // 배너 형태의 모든 상점 데이터를 로드
            var specShopDataList = SpecDataManager.Instance.GetShopDataList(ShopMainGroupType.BANNER);

            foreach (var shopData in specShopDataList)
            {
                if (shopData.shop_term_type == ShopTermType.PERIOD_TIME)
                {
                    if (CheckValidShopPeriod(shopData.shop_id))
                    {
                        var targetShopBannerData = GetShopBannerData(shopData.shop_id);
                        if (targetShopBannerData == null) AddShopBannerData(shopData.shop_id);
                    }
                    else
                    {
                        RemoveShopBannerData(shopData.shop_id);
                    }
                }
                else if (shopData.shop_term_type == ShopTermType.PERIOD)
                {
                    if (CheckValidShopPeriod(shopData.shop_id))
                        AddShopBannerData(shopData.shop_id);
                    else
                        RemoveShopBannerData(shopData.shop_id);
                }
                else if (shopData.shop_term_type == ShopTermType.TIME)
                {
                    var targetShopBannerData = GetShopBannerData(shopData.shop_id);
                    if (targetShopBannerData == null) AddShopBannerData(shopData.shop_id);
                }
            }
        }

        #endregion
    }

    [MemoryPackable]
    public partial class ShopBannerData
    {
        [MemoryPackOrder(0)] public int ShopId { get; set; }
        [MemoryPackOrder(1)] public int ShowConditionValue { get; set; }
        [MemoryPackOrder(2)] public ShopBannerStateType ShopBannerStateType { get; set; }
        [MemoryPackOrder(3)] public int ShowCount { get; set; }
        [MemoryPackOrder(4)] public long EndPurchaseTimestamp { get; set; }
    }

    [MemoryPackable]
    public partial class PurchaseData
    {
        [MemoryPackOrder(0)] public int ShopId { get; set; }
        [MemoryPackOrder(1)] public int PurchaseCount { get; set; }
        [MemoryPackOrder(2)] public long PurchaseTimestamp { get; set; }
    }
}
