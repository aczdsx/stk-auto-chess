using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cookapps.Autobattleproject.V1;
using CookApps.TeamBattle.UIManagements;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace CookApps.AutoBattler
{
    [RegisterUILayer(UILayerType.Popup, "Prefabs/UI/01_Pops/GachaPopup.prefab")]
    public class GachaPopup : UILayer
    {
        [SerializeField] private CAButton _backButton;

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



            // test
            //DialogueManager.Instance.UpdateDialogueEvent(DialogueEventType.POPUP_OPEN, nameof(gameObject));
        }

        private void OnClickGacha1Button()
        {
            // 재화 검사
            // if (SpecDataManager.Instance.GetGameConfig<int>("gacha_jewel_cost_1") > UserDataManager.Instance.UserWallet.Jewel)
            // {
            //     return;
            // }

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

            // 가챠 결과 아이템 저장
            UserDataManager.Instance.IncreaseRewardItemList(resultGachaList, true);

            // 가챠 진행횟수 유저 데이터 저장
            UserDataManager.Instance.AddUserGachaCount(Defines.GACHA_1_TIME_COUNT);

        }

        private void OnClickGacha10Button()
        {
            // 재화 검사
            // if (SpecDataManager.Instance.GetGameConfig<int>("gacha_jewel_cost_10") > UserDataManager.Instance.UserWallet.Jewel)
            // {
            //     return;
            // }

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

            // 가챠 결과 아이템 저장
            UserDataManager.Instance.IncreaseRewardItemList(resultGachaList, true);

            // 가챠 진행횟수 유저 데이터 저장
            UserDataManager.Instance.AddUserGachaCount(Defines.GACHA_10_TIME_COUNT);
        }

        private void OnClickCloseButton()
        {
            SceneUILayerManager.Instance.PopUILayer(this);
        }
    }
}
