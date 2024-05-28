using System.Collections;
using System.Collections.Generic;
using CookApps.TeamBattle.UIManagements;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace CookApps.AutoBattler
{
    [RegisterUILayer(UILayerType.Popup, "Prefabs/UI/01_Pops/PopupGacha.prefab")]
    public class GachaPopup : UILayer
    {
        [SerializeField] private CAButton _gacha1Button;
        [SerializeField] private CAButton _gacha10Button;

        private void Awake()
        {
            _gacha1Button.onClick.AddListener(OnClickGacha1Button);
            _gacha10Button.onClick.AddListener(OnClickGacha10Button);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            _gacha1Button.onClick.RemoveListener(OnClickGacha1Button);
            _gacha10Button.onClick.RemoveListener(OnClickGacha10Button);
        }

        public override void OnPreEnter(object param)
        {
            base.OnPreEnter(param);
            TopCurrencyAndMenuBar.AddToUILayer(this, TopPanelType.CloseButton);
        }

        private void OnClickGacha1Button()
        {
            Character result = SpecDataManager.Instance.Character.Get(40101);
            List<Character> tempResultList = new List<Character>();
            tempResultList.Add(result);

            //AddressablesUtil.Instantiate("Gacha_VFX_Ver_Final_01").GetComponent<GachaFxByTen>().SetItem(tempResultList, true);
            Addressables.InstantiateAsync("Gacha_VFX_Ver_Final_01").WaitForCompletion().GetComponent<GachaFxByTen>().SetItem(tempResultList, true);

        }

        private void OnClickGacha10Button()
        {

        }
    }
}
