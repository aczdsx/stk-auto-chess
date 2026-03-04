using CookApps.TeamBattle.UIManagements;
using Cysharp.Threading.Tasks;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CookApps.AutoBattler
{
    public class SettingPopup : UILayerPopupBase
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

        protected override void Awake()
        {
            base.Awake();
            _closeButton.OnClickAsObservable().Subscribe(this, (_, self) => self.OnClickCloseButton()).AddTo(this);
            _krLanguageButton.OnClickAsObservable().Subscribe(this, (_, self) => self.OnClickLanguageKrButton()).AddTo(this);
            _enLanguageButton.OnClickAsObservable().Subscribe(this, (_, self) => self.OnClickLanguageEnButton()).AddTo(this);
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
            var serverState = Preference.LoadPreference(Pref.SERVER_STATE, "");

            _versionText.text = $"{appVersion} ({specVersion} / {serverState} / {uuid})";
        }

        private async UniTask ChangeLanguage(SystemLanguage language)
        {
            SystemConfirmPopupData newPopupData = new SystemConfirmPopupData("UI_SYSTEM_ALERT", "MSG_ALARM_LANGUAGE_TEXT_CHANGE", "UI_CONFIRM_BTN", "UI_CANCEL_BTN");
            var popup = await SceneUILayerManager.Instance.PushUILayerAsync<SystemConfirmPopup>(newPopupData);
            var isConfirmed = await popup.WaitForExit();
            if (isConfirmed is true)
            {
                SceneTransition.Create<SceneTransition_FadeInOut>();
                await SceneTransition.FadeInAsync();
                await LanguageManager.Instance.SetLanguageAsync(language);
                await SceneTransition.FadeOutAsync();
            }
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
            ChangeLanguage(SystemLanguage.Korean).Forget();
        }
        
        private void OnClickLanguageEnButton()
        {
            ChangeLanguage(SystemLanguage.English).Forget();
        }
        
        private void OnClickCloseButton()
        {
            SceneUILayerManager.Instance.PopUILayer(this);
        }
    }
}
