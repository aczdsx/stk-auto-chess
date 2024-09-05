using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CookApps.gRPC.Hatchery;
using CookApps.gRPC.Universal;
using Cookapps.Stkauto.V1;
using UnityEngine;

namespace CookApps.AutoBattler
{
    public partial class UserDataManager
    {
        private UserShopPurchase userShopPurchase;

        public UserShopPurchase UserShopPurchase => userShopPurchase;

        [Initialize(DataCategory.UserShopPurchase, 13)]
        private void Initialize_ShopPurchaseData(string data)
        {
            if (string.IsNullOrEmpty(data))
            {
                userShopPurchase = new UserShopPurchase();

                UpdateShopBannerData(true);

                return;
            }

            userShopPurchase = MessageUtility.FromBase64String<UserShopPurchase>(data);
            
            UpdateShopBannerData(true);
        }

        [Clear]
        private void Clear_ShopPurchaseData()
        {
            userShopPurchase = null;
        }

        public void SaveUserShopPurchaseData()
        {
            HatcheryGrpcManager.Instance.SetPlayerDataAsync(DataCategory.UserShopPurchase.ToCategoryString(), userShopPurchase);
        }

        public bool CheckValidShopPeriod(int shopID)
        {
            var targetShopData = SpecDataManager.Instance.GetShopData(shopID);
            if (targetShopData == null) return false;

            bool checkPeriod = false;
            
            if (targetShopData.shop_term_type == ShopTermType.PERIOD || targetShopData.shop_term_type == ShopTermType.PERIOD_TIME)
            {
                var startAtTimeStamp = TimeManager.Instance.ChangeDateStringToTimeStamp(targetShopData.start_at);
                var endAtTimeStamp = TimeManager.Instance.ChangeDateStringToTimeStamp(targetShopData.end_at);
            
                checkPeriod = startAtTimeStamp <= TimeManager.Instance.UtcNowTimeStampLocal() && TimeManager.Instance.UtcNowTimeStampLocal() < endAtTimeStamp;
            }
            
            return checkPeriod;
        }
        
        public bool CheckValidShopTime(int shopID)
        {
            var targetShopData = SpecDataManager.Instance.GetShopData(shopID);
            if (targetShopData == null) return false;
            
            var targetShopBannerData = GetShopBannerData(shopID);   // 현재는 배너 형태 상점 데이터만 허용
            
            bool checkTime = false;
            
            if (targetShopData.shop_term_type == ShopTermType.TIME || targetShopData.shop_term_type == ShopTermType.PERIOD_TIME)
            {
                checkTime = TimeManager.Instance.UtcNowTimeStampLocal() < targetShopBannerData.EndPurchaseTimestamp;
            }
            
            return checkTime;
        }
        
        public UserPurchaseData GetPurchaseData(int shopID)
        {
            if (UserShopPurchase.UserPurchaseDatas.ContainsKey(shopID) == false) return null;
            
            return UserShopPurchase.UserPurchaseDatas[shopID];
        }
        
        public UserShopBannerData GetShopBannerData(int shopID)
        {
            if (UserShopPurchase.UserShopBannerDatas.ContainsKey(shopID) == false) return null;
            
            return UserShopPurchase.UserShopBannerDatas[shopID];
        }

        public List<UserShopBannerData> GetAllShopBannerDataList()
        {
            return UserShopPurchase.UserShopBannerDatas.Values.ToList();
        }
        
        public void SetShopBannerConditionValue(int shopID, int value, bool isAdd, bool needSave)
        {
            if (UserShopPurchase.UserShopBannerDatas.ContainsKey(shopID) == false) return;

            if (isAdd)
            {
                UserShopPurchase.UserShopBannerDatas[shopID].ShowConditionValue += value;
            }
            else
            {
                UserShopPurchase.UserShopBannerDatas[shopID].ShowConditionValue = value;
            }
            
            // 컨디션에 따른 상태 업데이트
            var specShopBannerData = SpecDataManager.Instance.GetShopBannerData(shopID);
            if (specShopBannerData != null &&
                UserShopPurchase.UserShopBannerDatas[shopID].ShowConditionValue >= specShopBannerData.condition_count)
            {
                SetShopBanneraStateType(shopID, ShopBannerStateType.ACTIVE, false);
            }
            
            if (needSave)
            {
                SaveUserShopPurchaseData();
            }
        }
        
        public void SetShopBannerShowCount(int shopID, int value, bool isAdd, bool needSave)
        {
            if (UserShopPurchase.UserShopBannerDatas.ContainsKey(shopID) == false) return;

            if (isAdd)
            {
                UserShopPurchase.UserShopBannerDatas[shopID].ShowCount += value;
            }
            else
            {
                UserShopPurchase.UserShopBannerDatas[shopID].ShowCount = value;
            }

            if (needSave)
            {
                SaveUserShopPurchaseData();
            }
        }
        
        public void SetShopBanneraStateType(int shopID, ShopBannerStateType stateType, bool needSave)
        {
            if (UserShopPurchase.UserShopBannerDatas.ContainsKey(shopID) == false) return;

            UserShopPurchase.UserShopBannerDatas[shopID].ShopBannerStateType = (int)stateType;

            if (needSave)
            {
                SaveUserShopPurchaseData();
            }
        }
        
        public void SetShopBannerEndPurchaseTime(int shopID, long endTimeStamp, bool needSave)
        {
            if (UserShopPurchase.UserShopBannerDatas.ContainsKey(shopID) == false) return;

            UserShopPurchase.UserShopBannerDatas[shopID].EndPurchaseTimestamp = endTimeStamp;

            if (needSave)
            {
                SaveUserShopPurchaseData();
            }
        }
        
        // 상품 배너 데이터를 최신상태로 업데이트
        private void UpdateShopBannerData(bool needSave)
        {
            // 배너 형태의 모든 상점 데이터를 로드
            var specShopDataList = SpecDataManager.Instance.GetShopDataList(ShopMainGroupType.BANNER);

            foreach (var shopData in specShopDataList)
            {
                /* 데이터 유효성 검증*/
                // 상품 판매 기간 유효성 체크에 따른 처리
                // PERIOD 타입 : 기간이 지나면 삭제
                // TIME 타입 : 데이터가 없을 경우 추가하고 따로 데이터를 삭제하지 않음  
                if (shopData.shop_term_type == ShopTermType.PERIOD_TIME)
                {
                    if (CheckValidShopPeriod(shopData.shop_id))
                    {
                        var targetShopBannerData = GetShopBannerData(shopData.shop_id);
                        if (targetShopBannerData == null)
                        {
                            AddShopBannerData(shopData.shop_id, false);
                        }
                    }
                    else
                    {
                        RemoveShopBannerData(shopData.shop_id, false);
                    }
                }
                else if (shopData.shop_term_type == ShopTermType.PERIOD)
                {
                    if (CheckValidShopPeriod(shopData.shop_id))
                    {
                        AddShopBannerData(shopData.shop_id, false);
                    }
                    else
                    {
                        RemoveShopBannerData(shopData.shop_id, false);
                    }
                }
                else if (shopData.shop_term_type == ShopTermType.TIME)
                {
                    var targetShopBannerData = GetShopBannerData(shopData.shop_id);
                    if (targetShopBannerData == null)
                    {
                        AddShopBannerData(shopData.shop_id, false);
                    }
                }
            }

            if (needSave)
            {
                SaveUserShopPurchaseData();
            }
        }

        private void AddShopBannerData(int targetShopID, bool needSave)
        {
            if (UserShopPurchase.UserShopBannerDatas.ContainsKey(targetShopID)) return;
            
            var specShopData = SpecDataManager.Instance.GetShopData(targetShopID);
            var specShopBannerData = SpecDataManager.Instance.GetShopBannerData(targetShopID);
            
            if (specShopData == null || specShopBannerData == null) return;

            var newBannerData = new UserShopBannerData();
            newBannerData.ShopId = specShopData.shop_id;
            newBannerData.ShowConditionValue = 0;
            newBannerData.ShopBannerStateType = (int)ShopBannerStateType.INACTIVE;
            newBannerData.ShowCount = 0;
            newBannerData.EndPurchaseTimestamp = 0;
            
            UserShopPurchase.UserShopBannerDatas.Add(targetShopID, newBannerData);

            if (needSave)
            {
                SaveUserShopPurchaseData();
            }
        }
        
        private void RemoveShopBannerData(int targetShopID, bool needSave)
        {
            if (UserShopPurchase.UserShopBannerDatas.ContainsKey(targetShopID) == false) return;

            UserShopPurchase.UserShopBannerDatas.Remove(targetShopID);
            
            if (needSave)
            {
                SaveUserShopPurchaseData();
            }
        }
    }
}