using System;
using System.Collections;
using System.Collections.Generic;
using CookApps.TeamBattle.Utility;
using CookApps.TeamBattle.UIManagements;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Cysharp.Threading.Tasks;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace CookApps.AutoBattler
{
    public enum GachaCountType
    {
        ONE,
        TEN,
    }
    
    public class GachaBaseLayer : MonoBehaviour
    {
        [SerializeField] private GachaType gachaType;
        
        protected GachaInfo _specGachaDataOneTime;
        protected GachaInfo _specGachaDataTenTime;
        
        protected GachaPopup _parentGachaPopup;
        
        
        private GachaInfo _currentSpecGachaData;
        
        public GachaType CurrentGachaType => gachaType;

        private List<AsyncOperationHandle<GameObject>> _loadedGachaFxHandles = new List<AsyncOperationHandle<GameObject>>();
        
        private void OnDestroy()
        {
            foreach (var handle in _loadedGachaFxHandles)
            {
                handle.Release();
            }
            _loadedGachaFxHandles.Clear();
        }

        // 유효 기간이 있는 가챠 타입인 경우 체크
        public bool CheckValidGachaPeriod(GachaCountType gachaCountType)
        {
            SetGachaData(gachaCountType);
            
            if (_currentSpecGachaData == null) return false;

            // 기간 타입 체크
            if (_currentSpecGachaData.gacha_term_type != GachaTermType.PERIOD) return true;
            
            // 유효 기간 체크
            var startTimeStamp = TimeManager.Instance.ChangeDateStringToTimeStamp(_currentSpecGachaData.start_at);
            var endTimeStamp = TimeManager.Instance.ChangeDateStringToTimeStamp(_currentSpecGachaData.end_at);
            
            return TimeManager.Instance.IsValidTimeNow(startTimeStamp, endTimeStamp);
        }
        
        // 캐릭터 가챠 프로세스 진행
        protected async UniTask ProcessCharacterGacha(GachaCountType gachaCountType)
        {
            SetGachaData(gachaCountType);

            if (_currentSpecGachaData == null) return;

            // 재화 검사 (ServerDataManager 사용)
            ItemId costItemId = _currentSpecGachaData.gacha_cost_item_id;
            if (!ServerDataManager.Instance.Inventory.HasEnoughCurrency(costItemId, (ulong)_currentSpecGachaData.gacha_cost))
            {
                // 재화 부족 토스트 메시지
                ToastManager.Instance.ShowToastByTokenKey("MSG_NOT_ENOUGH_CURRENCY");
                return;
            }

            // 서버 가챠 API 호출
            var response = await NetManager.Instance.Gacha.DrawAsync(_currentSpecGachaData.gacha_id.ToString());

            if (response == null || !response.IsSuccess)
            {
                // 가챠 실패 처리
                return;
            }

            // 서버 응답의 가챠 결과를 RewardItem 리스트로 변환
            var resultGachaList = new List<RewardItem>();
            for (int i = 0; i < response.Results.Count; i++)
            {
                var result = response.Results[i];
                resultGachaList.Add(new RewardItem
                {
                    Id = (int)result.ItemId,
                    Count = (int)result.Count
                });
            }

            if (resultGachaList.Count <= 0) return;

            bool isOneTime = gachaCountType == GachaCountType.ONE;
            var handle = Addressables.InstantiateAsync("Gacha_VFX_Ver_Final_01");
            await handle.WaitUntilDone();
            handle.Result.GetComponent<GachaFxByTen>().SetItem(resultGachaList, isOneTime);
            _loadedGachaFxHandles.Add(handle);

            // 통화 변화는 GachaService에서 자동으로 적용됨 (CurrencyDeltas)

            SoundManager.Instance.StopBGM();
            SoundManager.Instance.IsPlayingGacha = true;

            _parentGachaPopup.SetCanvasTargetDisplay(1);
        }

        private void SetGachaData(GachaCountType gachaCountType)
        {
            switch (gachaCountType)
            {
                case GachaCountType.ONE:
                    _currentSpecGachaData = _specGachaDataOneTime;
                    break;
                case GachaCountType.TEN:
                    _currentSpecGachaData = _specGachaDataTenTime;
                    break;
            }
        }
    }
}