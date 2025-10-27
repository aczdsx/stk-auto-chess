using System.Collections;
using System.Collections.Generic;
using CookApps.BattleSystem;
using CookApps.TeamBattle.UIManagements;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CookApps.AutoBattler
{
    [RegisterUILayer(UILayerType.Popup, "Prefabs/UI/01_Pops/WindowPopup/SettingPopup.prefab")]
    public class SettingPopup : UILayer
    {
        [Header("Common")]
        [SerializeField] private CAButton _closeButton;

        [Header("Language")] 
        [SerializeField] private CAButton _krLanguageButton;
        [SerializeField] private CAButton _enLanguageButton;

        [Header("Sound")] 
        [SerializeField] private Slider _bgmSlider;
        [SerializeField] private Slider _sfxSlider;

        [Header("Version Text")]
        [SerializeField] private TextMeshProUGUI _versionText;

        private void Awake()
        {
            _closeButton.onClick.AddListener(OnClickCloseButton);
            _krLanguageButton.onClick.AddListener(OnClickLanguageKrButton);
            _enLanguageButton.onClick.AddListener(OnClickLanguageEnButton);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            _closeButton.onClick.RemoveListener(OnClickCloseButton);
            _krLanguageButton.onClick.RemoveListener(OnClickLanguageKrButton);
            _enLanguageButton.onClick.RemoveListener(OnClickLanguageEnButton);
        }

        protected override void OnPreEnter(object param)
        {
            base.OnPreEnter(param);
            //TopCurrencyAndMenuBar.AddToUILayer(this, TopPanelType.PVP_Ticket);

            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_btn_popup);

            InitSettingPop();
        }

        private void InitSettingPop()
        {
            _bgmSlider.value = Preference.LoadPreference(Pref.BGM_V, 1f);
            _sfxSlider.value = Preference.LoadPreference(Pref.SFX_V, 1f);

            var specVersion = Preference.LoadPreference(Pref.LOCAL_SPEC_VERSION, 0);
            var appVersion = Application.version;
            var uuid = Preference.LoadPreference(Pref.GUEST_ID, "");

            _versionText.text = $"{appVersion} ({specVersion} / {uuid})";
        }

        private void ChangeLanguage(LanguageType targetType)
        {
            string contentText = LanguageManager.Instance.GetLanguageText("MSG_ALARM_LANGUAGE_TEXT_CHANGE");

            SystemConfirmPopupData newPopupData = new SystemConfirmPopupData();
            newPopupData.SetPopupData("시스템 알림", contentText, "확인", "취소", () =>
            {
                LanguageManager.Instance.SetGameLanguage(targetType);
        
                InGameManager.Instance.EndInGame();
                var transition = SceneTransition_FadeInOut.Create();
                SceneLoading.GoToNextScene("Title", null, transition);
            });

            SceneUILayerManager.Instance.PushUILayerAsync<SystemConfirmPopup>(newPopupData).Forget();
        }

        public void OnBGMValueChanged()
        {
            SoundManager.Instance.SetBGMVolume(_bgmSlider.value);
        }

        public void OnSFXValueChanged()
        {
            SoundManager.Instance.SetSFXVolume(_sfxSlider.value);
        }

        private void OnClickLanguageKrButton()
        {
            ChangeLanguage(LanguageType.KR);
        }
        
        private void OnClickLanguageEnButton()
        {
            ChangeLanguage(LanguageType.EN);
        }
        
        private void OnClickCloseButton()
        {
            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_btn_touch);
            SceneUILayerManager.Instance.PopUILayer(this);
        }
    }
}