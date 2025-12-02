using System.Collections;
using System.Collections.Generic;
using CookApps.TeamBattle.UIManagements;
using TMPro;
using UnityEngine;

namespace CookApps.AutoBattler
{
    public class EndTestgamePopup : UILayer
    {
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private TextMeshProUGUI _descText;

        [SerializeField] private CAButton _dimCloseButton;
        [SerializeField] private CAButton _surveyButton;

        private string _surveyURL = "";

        protected override void Awake()
        {
            base.Awake();

            _dimCloseButton.onClick.AddListener(OnClickCloseButton);
            _surveyButton.onClick.AddListener(OnClickSurveyButton);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            _dimCloseButton.onClick.RemoveListener(OnClickCloseButton);
            _surveyButton.onClick.RemoveListener(OnClickSurveyButton);
        }

        protected override void OnPreEnter(object param)
        {
            base.OnPreEnter(param);
            //TopCurrencyAndMenuBar.AddToUILayer(this, TopPanelType.CloseButton);

            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_btn_popup);

            _titleText.text = LanguageManager.Instance.GetLanguageText("END_TEST_GAME_SURVEY_TITLE");
            _descText.text = LanguageManager.Instance.GetLanguageText("END_TEST_GAME_SURVEY_DESC");

            if (LanguageManager.Instance.CurrentLanguageType == LanguageType.KR)
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
