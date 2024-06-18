using System.Collections;
using System.Collections.Generic;
using CookApps.TeamBattle.UIManagements;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace CookApps.AutoBattler
{
    [RegisterUILayer(UILayerType.Overlay, "Prefabs/UI/01_Pops/WindowPopup/ToastSystemPopup.prefab")]
    public class ToastSystemPopup : UILayer
    {
        [SerializeField] private TextMeshProUGUI _messageText;

        private string _messageString;

        protected override void Awake()
        {
            base.Awake();

        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

        }

        protected override void OnPreEnter(object param)
        {
            base.OnPreEnter(param);
            //TopCurrencyAndMenuBar.AddToUILayer(this, TopPanelType.CloseButton);

            _messageString = param as string;

            SetToastSystemPopup();
        }

        private async void SetToastSystemPopup()
        {
            _messageText.text = _messageString;

            await ShowToastPopup();
        }

        private async UniTask ShowToastPopup()
        {
            await UniTask.Delay(Defines.TOAST_POPUP_DURATION);

            ClosePopup();
        }

        private void ClosePopup()
        {
            SceneUILayerManager.Instance.PopUILayer(this);

            ToastManager.Instance.IsShowingToast = false;
        }
    }
}
