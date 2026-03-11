using CookApps.TeamBattle.UIManagements;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

/*
MSG_MAX_LV_UP 문제
*/

namespace CookApps.AutoBattler
{
    public class ToastSystemPopup : UILayerPopupBase
    {
        [SerializeField] private TextMeshProUGUI _messageText;
        [SerializeField] private Animator _animator;

        private string _messageString;

        protected override void Awake()
        {
            base.Awake();

        }

        protected override void OnPreEnter(object param)
        {
            base.OnPreEnter(param);
            //TopCurrencyAndMenuBar.AddToUILayer(this, TopPanelType.CloseButton);

            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_btn_popup);

            _messageString = param as string;

            SetToastSystemPopup();
        }

        // 수동으로 토스트팝업을 제어
        public void SetToastSystemLongPopup(string message)
        {
            _messageString = message;
            _messageText.text = _messageString;
            gameObject.SetActive(true);
            _animator.SetTrigger("LongAnim");
            Invoke(nameof(ClosePopupManual), Defines.TOAST_LONG_POPUP_DURATION);
        }

        private async void SetToastSystemPopup()
        {
            _messageText.text = _messageString;
            _animator.SetTrigger("ShortAnim");

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

            ToastManager.Instance.NotifyToastClosed();
        }

        private void ClosePopupManual()
        {
            gameObject.SetActive(false);
        }
    }
}
