using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
            //TopCurrencyAndMenuBar.AddToUILayer(this, TopPanelType.CloseButton);

            // test
            //DialogueManager.Instance.UpdateDialogueEvent(DialogueEventType.POPUP_OPEN, nameof(gameObject));
        }

        private void OnClickGacha1Button()
        {
            // SpecCharacter result = SpecDataManager.Instance.GetCharacterData(40101);
            // List<SpecCharacter> tempResultList = new List<SpecCharacter>();
            // tempResultList.Add(result);
            //
            // //AddressablesUtil.Instantiate("Gacha_VFX_Ver_Final_01").GetComponent<GachaFxByTen>().SetItem(tempResultList, true);
            // Addressables.InstantiateAsync("Gacha_VFX_Ver_Final_01").WaitForCompletion().GetComponent<GachaFxByTen>().SetItem(tempResultList, true);

        }

        private void OnClickGacha10Button()
        {
            var allChar = SpecDataManager.Instance.SpecCharacter.All.ToList();

            //Character result = SpecDataManager.Instance.Character.Get(40101);
            //allChar.Add(result);

            var gacahaScenarios = SpecDataManager.Instance.SpecGachaScenario.All.ToList();
            List<RewardItem> tempResultList = new List<RewardItem>();
            for (int i = 0; i < 10; ++i)
            {
                RewardItem newItem = new RewardItem(gacahaScenarios[i].item_type, gacahaScenarios[i].item_key, gacahaScenarios[i].item_count);
                tempResultList.Add(newItem);
            }

            //AddressablesUtil.Instantiate("Gacha_VFX_Ver_Final_01").GetComponent<GachaFxByTen>().SetItem(tempResultList, true);
            Addressables.InstantiateAsync("Gacha_VFX_Ver_Final_01").WaitForCompletion().GetComponent<GachaFxByTen>().SetItem(tempResultList);
        }

        private void OnClickCloseButton()
        {
            SceneUILayerManager.Instance.PopUILayer(this);
        }
    }
}
