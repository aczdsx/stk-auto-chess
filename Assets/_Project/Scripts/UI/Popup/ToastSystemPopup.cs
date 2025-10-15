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
        [SerializeField] private Animator _animator;

        private string _messageString;
        private bool _isLongToast;

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

            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_btn_negative);

            _messageString = param as string;
            _isLongToast = (bool)param;
            if (_isLongToast)
            {
                _animator.SetTrigger("LongAnim");
            }
            else
            {
                _animator.SetTrigger("ShortAnim");
            }

            SetToastSystemPopup();
        }

        // 수동으로 토스트팝업을 제어
        public void SetToastSystemPopupByManual(string message, float duration, bool isLongToast = false)
        {
            _messageString = message;
            _messageText.text = _messageString;
            _isLongToast = isLongToast;
            if (_isLongToast)
            {
                _animator.SetTrigger("LongAnim");
            }
            else
            {
                _animator.SetTrigger("ShortAnim");
            }
            gameObject.SetActive(true);

            Invoke(nameof(ClosePopupManual), duration);
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

        private void ClosePopupManual()
        {
            gameObject.SetActive(false);
        }
    }
}
