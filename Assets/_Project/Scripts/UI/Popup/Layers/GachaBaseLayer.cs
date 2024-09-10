using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

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
        
        protected SpecGacha _specGachaDataOneTime;
        protected SpecGacha _specGachaDataTenTime;
        
        protected GachaPopup _parentGachaPopup;
        
        
        private SpecGacha _currentSpecGachaData;
        
        public GachaType CurrentGachaType => gachaType;
        
        // 캐릭터 가챠 프로세스 진행
        public void ProcessCharacterGacha(GachaCountType gachaCountType)
        {
            SetGachaData(gachaCountType);

            if (_currentSpecGachaData == null) return;
            
            // 재화 검사
            if (!UserDataManager.Instance.CheckEnoughItem(_currentSpecGachaData.gacha_cost_item_type, 0, _currentSpecGachaData.gacha_cost, true))
            {
                return;
            }

            // 가챠 시나리오 테이블 사용 시 코드 (old)
            // int currentGachaCount = UserDataManager.Instance.UserBasicData.TotalGachaCount;
            // var gachaScenarioList = SpecDataManager.Instance.GetGachaScenarioList(currentGachaCount, Defines.GACHA_1_TIME_COUNT);
            // var resultGachaList = SpecDataManager.Instance.GetRewardItemListByGachaScenarioList(gachaScenarioList);

            bool isOneTime = gachaCountType == GachaCountType.ONE;
            
            var resultGachaList = SpecDataManager.Instance.GetRandomPickGachaRewardItemList(_currentSpecGachaData.gacha_id, _currentSpecGachaData.gacha_count);
            if (resultGachaList == null || resultGachaList.Count <= 0) return;
            
            // //AddressablesUtil.Instantiate("Gacha_VFX_Ver_Final_01").GetComponent<GachaFxByTen>().SetItem(tempResultList, true);
            Addressables.InstantiateAsync("Gacha_VFX_Ver_Final_01").WaitForCompletion().GetComponent<GachaFxByTen>().SetItem(resultGachaList, isOneTime);
            
            // 가챠 아이템 소모
            UserDataManager.Instance.DecreaseItem(_currentSpecGachaData.gacha_cost_item_type, 0, _currentSpecGachaData.gacha_cost, true, true);

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