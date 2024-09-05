using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cookapps.Stkauto.V1;
using CookApps.TeamBattle.UIManagements;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace CookApps.AutoBattler
{
    [RegisterUILayer(UILayerType.Popup, "Prefabs/UI/01_Pops/GachaPopup.prefab")]
    public class GachaPopup : UILayer
    {
        [SerializeField] private CAButton _backButton;

        private Canvas cc;

        [Space(10)]
        [SerializeField] private CAButton _gacha1Button;
        [SerializeField] private CAButton _gacha10Button;

        private void Awake()
        {
            _gacha1Button.onClick.AddListener(OnClickGacha1Button);
            _gacha10Button.onClick.AddListener(OnClickGacha10Button);
            _backButton.onClick.AddListener(OnClickCloseButton);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            _gacha1Button.onClick.RemoveListener(OnClickGacha1Button);
            _gacha10Button.onClick.RemoveListener(OnClickGacha10Button);
            _backButton.onClick.RemoveListener(OnClickCloseButton);
        }

        protected override void OnPreEnter(object param)
        {
            base.OnPreEnter(param);
            TopCurrencyAndMenuBar.AddToUILayer(this, TopPanelType.C_Ticket);

            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_btn_popup);

            // 상점 배너 팝업 체크
            ShopPurchaseManager.Instance.UpdateShopBannerConditionValue(ShopBannerConditionType.ENTER_GACHA_POP, 0,  1, false);
            ShopPurchaseManager.Instance.ShowShopBannerPopup(ShopBannerShowType.IMMEDIATE);
            
            // test
            //DialogueManager.Instance.UpdateDialogueEvent(DialogueEventType.POPUP_OPEN, nameof(gameObject));
        }

        public void SetCanvasTargetDisplay(int targetDisplay)
        {
            GameObject.Find("MainCanvas").GetComponent<Canvas>().targetDisplay = targetDisplay;
        }

        private void OnClickGacha1Button()
        {
            // 재화 검사
            if (!UserDataManager.Instance.CheckEnoughItem(ItemType.C_TICKET, 0, Defines.GACHA_1_TIME_COUNT, true))
            {
                return;
            }

            // SpecCharacter result = SpecDataManager.Instance.GetCharacterData(40101);
            // List<SpecCharacter> tempResultList = new List<SpecCharacter>();
            // tempResultList.Add(result);

            int currentGachaCount = UserDataManager.Instance.UserBasicData.TotalGachaCount;

            // 최대 가챠 횟수 체크 (임시)
            // if (currentGachaCount >= SpecDataManager.Instance.SpecGachaScenario.All.Count)
            // {
            //     return;
            // }

            var gachaScenarioList = SpecDataManager.Instance.GetGachaScenarioList(currentGachaCount, Defines.GACHA_1_TIME_COUNT);
            var resultGachaList = SpecDataManager.Instance.GetRewardItemListByGachaScenarioList(gachaScenarioList);

            // //AddressablesUtil.Instantiate("Gacha_VFX_Ver_Final_01").GetComponent<GachaFxByTen>().SetItem(tempResultList, true);
            Addressables.InstantiateAsync("Gacha_VFX_Ver_Final_01").WaitForCompletion().GetComponent<GachaFxByTen>().SetItem(resultGachaList, true);

            // 가챠 티켓 소모
            UserDataManager.Instance.DecreaseItem(ItemType.C_TICKET, 0, Defines.GACHA_1_TIME_COUNT, true, true);

            // 가챠 결과 아이템 저장
            UserDataManager.Instance.IncreaseRewardItemList(resultGachaList, true);

            // 가챠 진행횟수 유저 데이터 저장
            UserDataManager.Instance.AddUserGachaCount(Defines.GACHA_1_TIME_COUNT);

            // 가이드 미션 체크
            GuideMissionManager.Instance.AddGuideMissionActionValue(GuideMissionType.SUMMON_CHARCTER, 0, Defines.GACHA_1_TIME_COUNT);

            // 퀘스트 데이터 갱신
            UserDataManager.Instance.SetUserQuestActionCount(QuestType.SUMMON_CHARACTER, Defines.GACHA_1_TIME_COUNT, true, true);

            SoundManager.Instance.StopBGM();
            SoundManager.Instance.IsPlayingGacha = true;

            SetCanvasTargetDisplay(1);
        }

        private void OnClickGacha10Button()
        {
            // 재화 검사
            if (!UserDataManager.Instance.CheckEnoughItem(ItemType.C_TICKET, 0, Defines.GACHA_10_TIME_COUNT, true))
            {
                return;
            }

            // var gacahaScenarios = SpecDataManager.Instance.SpecGachaScenario.All.ToList();
            // List<RewardItem> tempResultList = new List<RewardItem>();
            // for (int i = 0; i < 10; ++i)
            // {
            //     RewardItem newItem = new RewardItem(gacahaScenarios[i].item_type, gacahaScenarios[i].item_key, gacahaScenarios[i].item_count);
            //     tempResultList.Add(newItem);
            // }

            int currentGachaCount = UserDataManager.Instance.UserBasicData.TotalGachaCount;

            // 최대 가챠 횟수 체크 (임시)
            // if (currentGachaCount >= SpecDataManager.Instance.SpecGachaScenario.All.Count)
            // {
            //     return;
            // }

            var gachaScenarioList = SpecDataManager.Instance.GetGachaScenarioList(currentGachaCount, Defines.GACHA_10_TIME_COUNT);
            var resultGachaList = SpecDataManager.Instance.GetRewardItemListByGachaScenarioList(gachaScenarioList);

            //AddressablesUtil.Instantiate("Gacha_VFX_Ver_Final_01").GetComponent<GachaFxByTen>().SetItem(tempResultList, true);
            Addressables.InstantiateAsync("Gacha_VFX_Ver_Final_01").WaitForCompletion().GetComponent<GachaFxByTen>().SetItem(resultGachaList);

            // 가챠 티켓 소모
            UserDataManager.Instance.DecreaseItem(ItemType.C_TICKET, 0, Defines.GACHA_10_TIME_COUNT, true, true);

            // 가챠 결과 아이템 저장
            UserDataManager.Instance.IncreaseRewardItemList(resultGachaList, true);

            // 가챠 진행횟수 유저 데이터 저장
            UserDataManager.Instance.AddUserGachaCount(Defines.GACHA_10_TIME_COUNT);

            // 가이드 미션 체크
            GuideMissionManager.Instance.AddGuideMissionActionValue(GuideMissionType.SUMMON_CHARCTER, 0, Defines.GACHA_10_TIME_COUNT);

            // 퀘스트 데이터 갱신
            UserDataManager.Instance.SetUserQuestActionCount(QuestType.SUMMON_CHARACTER, Defines.GACHA_10_TIME_COUNT, true, true);

            SoundManager.Instance.StopBGM();
            SoundManager.Instance.IsPlayingGacha = true;

            SetCanvasTargetDisplay(1);
        }

        private void OnClickCloseButton()
        {
            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_btn_touch);

            SceneUILayerManager.Instance.PopUILayer(this);
        }
    }
}
