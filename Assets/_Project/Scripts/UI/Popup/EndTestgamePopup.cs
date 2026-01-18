using CookApps.TeamBattle.UIManagements;
using R3;
using TMPro;
using UnityEngine;

namespace CookApps.AutoBattler
{
    public class EndTestgamePopup : UILayerPopupBase
    {
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private TextMeshProUGUI _descText;

        [SerializeField] private CAButton _dimCloseButton;
        [SerializeField] private CAButton _surveyButton;

        private string _surveyURL = "";

        protected override void Awake()
        {
            base.Awake();

            _dimCloseButton.OnClickAsObservable().Subscribe(this, (_, self) => self.OnClickCloseButton()).AddTo(this);
            _surveyButton.OnClickAsObservable().Subscribe(this, (_, self) => self.OnClickSurveyButton()).AddTo(this);
        }

        protected override void OnPreEnter(object param)
        {
            base.OnPreEnter(param);
            //TopCurrencyAndMenuBar.AddToUILayer(this, TopPanelType.CloseButton);

            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_btn_popup);

            _titleText.text = LanguageManager.Instance.GetDefaultText("END_TEST_GAME_SURVEY_TITLE");
            _descText.text = LanguageManager.Instance.GetDefaultText("END_TEST_GAME_SURVEY_DESC");

            if (LanguageManager.Instance.CurrentLanguageType == SystemLanguage.Korean)
            {
                _surveyURL = SpecDataManager.Instance.GetGameConfig<string>("survey_link_url");
            }
            else
            {
                _surveyURL = SpecDataManager.Instance.GetGameConfig<string>("survey_link_url_en");
            }
        }

        private void OnClickSurveyButton()
        {
            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_btn_confirm);

            Application.OpenURL(_surveyURL);

            SceneUILayerManager.Instance.PopUILayer(this);
        }

        private void OnClickCloseButton()
        {
            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_btn_confirm);

            SceneUILayerManager.Instance.PopUILayer(this);
        }
    }
}
