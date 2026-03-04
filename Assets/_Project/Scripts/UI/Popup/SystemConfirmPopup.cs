using System;
using CookApps.TeamBattle.UIManagements;
using R3;
using TMPro;
using UnityEngine;

namespace CookApps.AutoBattler
{
    public class SystemConfirmPopupData
    {
        public readonly string titleText = "UI_SYSTEM_ALERT";
        public readonly string descText;
        public readonly string confirmButtonText = "UI_CONFIRM_BTN";
        public readonly string cancelButtonText = "UI_CANCEL_BTN";

        public SystemConfirmPopupData(string titleTextKey, string descTextKey, string confirmButtonTextKey, string cancelButtonTextKey)
        {
            titleText = LanguageManager.Instance.GetDefaultText(titleTextKey);
            descText = LanguageManager.Instance.GetDefaultText(descTextKey);
            confirmButtonText = LanguageManager.Instance.GetDefaultText(confirmButtonTextKey);
            cancelButtonText = LanguageManager.Instance.GetDefaultText(cancelButtonTextKey);
        }
    }

    public class SystemConfirmPopup : UILayerPopupBase
    {
        [SerializeField] private CAButton _closeButton;
        [SerializeField] private CAButton _cancelButton;
        [SerializeField] private CAButton _confirmButton;

        [Space(10)]
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private TextMeshProUGUI _descText;
        [SerializeField] private TextMeshProUGUI _cancelButtonText;
        [SerializeField] private TextMeshProUGUI _confirmButtonText;

        private Action _onConfirmAction;

        private SystemConfirmPopupData _currentSystemPopupData;

        protected override void Awake()
        {
            base.Awake();

            _closeButton.OnClickAsObservable().Subscribe(this, (_, self) => self.OnClickCloseButton()).AddTo(this);
            
            _cancelButton.OnClickAsObservable().Subscribe(this, (_, self) => self.OnClickCloseButton()).AddTo(this);
            _cancelButton.DefaultClickSoundType = DefaultClickSoundType.Negative;
            
            _confirmButton.OnClickAsObservable().Subscribe(this, (_, self) => self.OnClickConfirmButton()).AddTo(this);
            _confirmButton.DefaultClickSoundType = DefaultClickSoundType.Confirm;
        }

        protected override void OnPreEnter(object param)
        {
            base.OnPreEnter(param);
            //TopCurrencyAndMenuBar.AddToUILayer(this, TopPanelType.CloseButton);

            _currentSystemPopupData = param as SystemConfirmPopupData;

            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_btn_popup);

            _titleText.text = _currentSystemPopupData.titleText;
            _descText.text = _currentSystemPopupData.descText;
            _confirmButtonText.text = _currentSystemPopupData.confirmButtonText;
            _cancelButtonText.text = _currentSystemPopupData.cancelButtonText;
        }

        private void OnClickConfirmButton()
        {
            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_btn_confirm);

            _onConfirmAction?.Invoke();

            SceneUILayerManager.Instance.PopUILayer(this, true);
        }

        private void OnClickCloseButton()
        {
            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_btn_confirm);

            SceneUILayerManager.Instance.PopUILayer(this, false);
        }
    }
}
