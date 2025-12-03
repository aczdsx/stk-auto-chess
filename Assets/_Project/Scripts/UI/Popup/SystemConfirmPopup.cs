using System;
using CookApps.TeamBattle.UIManagements;
using TMPro;
using UnityEngine;

namespace CookApps.AutoBattler
{
    public class SystemConfirmPopupData
    {
        public string titleText;
        public string descText;
        public string confirmButtonText;
        public string cancelButtonText;
        public Action onConfirmAction;

        public void SetPopupData(string titleText, string descText, string confirmButtonText, string cancelButtonText, Action onConfirmAction)
        {
            this.titleText = titleText;
            this.descText = descText;
            this.confirmButtonText = confirmButtonText;
            this.cancelButtonText = cancelButtonText;
            this.onConfirmAction = onConfirmAction;
        }

        public void SetPopupDataByStringKey(string titleTextKey, string descTextKey, string confirmButtonTextKey, string cancelButtonTextKey, Action onConfirmAction)
        {
            this.titleText = LanguageManager.Instance.GetLanguageText(titleTextKey);
            this.descText = LanguageManager.Instance.GetLanguageText(descTextKey);
            this.confirmButtonText = LanguageManager.Instance.GetLanguageText(confirmButtonTextKey);
            this.cancelButtonText = LanguageManager.Instance.GetLanguageText(cancelButtonTextKey);
            this.onConfirmAction = onConfirmAction;
        }
    }

    public class SystemConfirmPopup : UILayer
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

            _closeButton.onClick.AddListener(OnClickCloseButton);
            _cancelButton.onClick.AddListener(OnClickCloseButton);
            _confirmButton.onClick.AddListener(OnClickConfirmButton);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            _closeButton.onClick.RemoveListener(OnClickCloseButton);
            _cancelButton.onClick.RemoveListener(OnClickCloseButton);
            _confirmButton.onClick.RemoveListener(OnClickConfirmButton);
        }

        protected override void OnPreEnter(object param)
        {
            base.OnPreEnter(param);
            //TopCurrencyAndMenuBar.AddToUILayer(this, TopPanelType.CloseButton);

            _currentSystemPopupData = param as SystemConfirmPopupData;

            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_btn_popup);

            SetSystemConfirmPopup();
        }

        // 시스템 컨펌 팝업은 제목, 설명, 확인버튼, 취소버튼, 확인버튼 클릭시 실행할 액션을 받아서 세팅
        private void SetSystemConfirmPopup()
        {
            if (_currentSystemPopupData == null) return;

            _titleText.text = _currentSystemPopupData.titleText;
            _descText.text = _currentSystemPopupData.descText;
            _confirmButtonText.text = _currentSystemPopupData.confirmButtonText;
            _cancelButtonText.text = _currentSystemPopupData.cancelButtonText;

            _onConfirmAction = _currentSystemPopupData.onConfirmAction;
        }

        private void OnClickConfirmButton()
        {
            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_btn_confirm);

            _onConfirmAction?.Invoke();

            SceneUILayerManager.Instance.PopUILayer(this);
        }

        private void OnClickCloseButton()
        {
            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_btn_confirm);

            SceneUILayerManager.Instance.PopUILayer(this);
        }
    }
}
