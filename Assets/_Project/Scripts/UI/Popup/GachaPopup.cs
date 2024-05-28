using System.Collections;
using System.Collections.Generic;
using CookApps.TeamBattle.UIManagements;
using UnityEngine;

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

        protected override void OnPreEnter(object param)
        {
            base.OnPreEnter(param);
            TopCurrencyAndMenuBar.AddToUILayer(this, TopPanelType.CloseButton);
        }

        private void OnClickGacha1Button()
        {

        }

        private void OnClickGacha10Button()
        {

        }
    }
}
