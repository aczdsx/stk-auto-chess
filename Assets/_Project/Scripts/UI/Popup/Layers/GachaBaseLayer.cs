using System;
using System.Collections;
using System.Collections.Generic;
using CookApps.TeamBattle.Utility;
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
            
            // 재화 검사
            if (!UserDataManager.Instance.CheckEnoughItem(_currentSpecGachaData.gacha_cost_item_type, 0, _currentSpecGachaData.gacha_cost, true))
            {
                return;
            }

            // 가챠 시나리오 테이블 사용 시 코드
            int currentGachaCount = UserDataManager.Instance.UserBasicData.TotalGachaCount;
            var gachaScenarioList = SpecDataManager.Instance.GetGachaScenarioList(currentGachaCount, _currentSpecGachaData.gacha_count);
            var resultGachaList = SpecDataManager.Instance.GetRewardItemListByGachaScenarioList(gachaScenarioList);

            // 일반 확률 가챠 테이블 사용 시 코드
            //var resultGachaList = SpecDataManager.Instance.GetRandomPickGachaRewardItemList(_currentSpecGachaData.gacha_group_id, _currentSpecGachaData.gacha_count);
            
            if (resultGachaList == null || resultGachaList.Count <= 0) return;
            
            bool isOneTime = gachaCountType == GachaCountType.ONE;
            var handle = Addressables.InstantiateAsync("Gacha_VFX_Ver_Final_01");
            await handle.WaitUntilDone();
            handle.Result.GetComponent<GachaFxByTen>().SetItem(resultGachaList, isOneTime);
            _loadedGachaFxHandles.Add(handle);
            
            // TODO: 가챠 아이템 소모
            // UserDataManager.Instance.DecreaseItem(_currentSpecGachaData.gacha_cost_item_type, 0, _currentSpecGachaData.gacha_cost, true, true);

            // 가챠 결과 아이템 저장
            UserDataManager.Instance.IncreaseRewardItemList(resultGachaList, true);

            // 가챠 진행횟수 유저 데이터 저장
            UserDataManager.Instance.AddUserGachaCount(_currentSpecGachaData.gacha_count);

            // 가이드 미션 체크
            GuideMissionManager.Instance.AddGuideMissionActionValue(GuideMissionType.SUMMON_CHARCTER, 0, _currentSpecGachaData.gacha_count);

            // 퀘스트 데이터 갱신
            UserDataManager.Instance.SetUserQuestActionCount(QuestType.SUMMON_CHARACTER, _currentSpecGachaData.gacha_count, true, true);
            
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