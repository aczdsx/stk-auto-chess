using System;
using System.Collections;
using System.Collections.Generic;
using CookApps.TeamBattle.UIManagements;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace CookApps.AutoBattler
{
    public class GachaPickUpCharacterLayer : MonoBehaviour
    {
        [SerializeField] private GachaType gachaType;
        
        [Space(10)]
        [SerializeField] private CAButton _gacha1Button;
        [SerializeField] private CAButton _gacha10Button;
        
        private SpecGacha _specGachaDataOneTime;
        private SpecGacha _specGachaDataTenTime;
        
        private GachaPopup _parentGachaPopup;
        public GachaType CurrentGachaType => gachaType;

        private void OnEnable()
        {
            _gacha1Button?.onClick.AddListener(OnClickGacha1Button);
            _gacha10Button?.onClick.AddListener(OnClickGacha10Button);
        }

        private void OnDisable()
        {
            _gacha1Button?.onClick.RemoveListener(OnClickGacha1Button);
            _gacha10Button?.onClick.RemoveListener(OnClickGacha10Button);
        }

        public void SetGachaLayer(GachaPopup parentPopup)
        {
            _parentGachaPopup = parentPopup;
            
            _specGachaDataOneTime = SpecDataManager.Instance.GetGachaData(CurrentGachaType, Defines.GACHA_1_TIME_COUNT);
            _specGachaDataTenTime = SpecDataManager.Instance.GetGachaData(CurrentGachaType, Defines.GACHA_10_TIME_COUNT);
        }
        
       private void OnClickGacha1Button()
        {
            if (_specGachaDataOneTime == null) return;
            
            // 재화 검사
            if (!UserDataManager.Instance.CheckEnoughItem(_specGachaDataOneTime.gacha_cost_item_type, 0, _specGachaDataOneTime.gacha_cost, true))
            {
                return;
            }

            int currentGachaCount = UserDataManager.Instance.UserBasicData.TotalGachaCount;

            var gachaScenarioList = SpecDataManager.Instance.GetGachaScenarioList(currentGachaCount, Defines.GACHA_1_TIME_COUNT);
            var resultGachaList = SpecDataManager.Instance.GetRewardItemListByGachaScenarioList(gachaScenarioList);

            // //AddressablesUtil.Instantiate("Gacha_VFX_Ver_Final_01").GetComponent<GachaFxByTen>().SetItem(tempResultList, true);
            Addressables.InstantiateAsync("Gacha_VFX_Ver_Final_01").WaitForCompletion().GetComponent<GachaFxByTen>().SetItem(resultGachaList, true);

            // 가챠 아이템 소모
            UserDataManager.Instance.DecreaseItem(_specGachaDataOneTime.gacha_cost_item_type, 0, _specGachaDataOneTime.gacha_cost, true, true);

            // 가챠 결과 아이템 저장
            UserDataManager.Instance.IncreaseRewardItemList(resultGachaList, true);

            // 가챠 진행횟수 유저 데이터 저장
            UserDataManager.Instance.AddUserGachaCount(_specGachaDataOneTime.gacha_count);

            // 가이드 미션 체크
            GuideMissionManager.Instance.AddGuideMissionActionValue(GuideMissionType.SUMMON_CHARCTER, 0, _specGachaDataOneTime.gacha_count);

            // 퀘스트 데이터 갱신
            UserDataManager.Instance.SetUserQuestActionCount(QuestType.SUMMON_CHARACTER, _specGachaDataOneTime.gacha_count, true, true);

            SoundManager.Instance.StopBGM();
            SoundManager.Instance.IsPlayingGacha = true;

            _parentGachaPopup.SetCanvasTargetDisplay(1);
        }

        private void OnClickGacha10Button()
        {
            if (_specGachaDataTenTime == null) return;
            
            // 재화 검사
            if (!UserDataManager.Instance.CheckEnoughItem(_specGachaDataTenTime.gacha_cost_item_type, 0, _specGachaDataTenTime.gacha_cost, true))
            {
                return;
            }

            int currentGachaCount = UserDataManager.Instance.UserBasicData.TotalGachaCount;

            var gachaScenarioList = SpecDataManager.Instance.GetGachaScenarioList(currentGachaCount, Defines.GACHA_10_TIME_COUNT);
            var resultGachaList = SpecDataManager.Instance.GetRewardItemListByGachaScenarioList(gachaScenarioList);

            //AddressablesUtil.Instantiate("Gacha_VFX_Ver_Final_01").GetComponent<GachaFxByTen>().SetItem(tempResultList, true);
            Addressables.InstantiateAsync("Gacha_VFX_Ver_Final_01").WaitForCompletion().GetComponent<GachaFxByTen>().SetItem(resultGachaList);

            // 가챠 아이템 소모
            UserDataManager.Instance.DecreaseItem(_specGachaDataTenTime.gacha_cost_item_type, 0, _specGachaDataTenTime.gacha_cost, true, true);

            // 가챠 결과 아이템 저장
            UserDataManager.Instance.IncreaseRewardItemList(resultGachaList, true);

            // 가챠 진행횟수 유저 데이터 저장
            UserDataManager.Instance.AddUserGachaCount(_specGachaDataTenTime.gacha_count);

            // 가이드 미션 체크
            GuideMissionManager.Instance.AddGuideMissionActionValue(GuideMissionType.SUMMON_CHARCTER, 0, _specGachaDataTenTime.gacha_count);

            // 퀘스트 데이터 갱신
            UserDataManager.Instance.SetUserQuestActionCount(QuestType.SUMMON_CHARACTER, _specGachaDataTenTime.gacha_count, true, true);


            SoundManager.Instance.StopBGM();
            SoundManager.Instance.IsPlayingGacha = true;

            _parentGachaPopup.SetCanvasTargetDisplay(1);
        }
    }
}