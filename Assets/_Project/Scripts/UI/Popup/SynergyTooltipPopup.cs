using System.Collections;
using System.Collections.Generic;
using CookApps.TeamBattle.UIManagements;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CookApps.AutoBattler
{
    public class SynergyTooltipPopup : UILayer
    {
        [SerializeField] private CAButton _closeButton;
        [SerializeField] private CAButton _dimLayerButton;

        [Header("Synergy Info Layer")]
        [SerializeField] private SynergyUI _synergyUI;
        [SerializeField] private TextMeshProUGUI _synergyNameText;
        [SerializeField] private TextMeshProUGUI _synergyNameTitleText;
        [SerializeField] private TextMeshProUGUI _synergyDescText;
        
        [Header("Vertical Layout Group")]
        [SerializeField] private List<TextMeshProUGUI> _synergyEffectList;

        // private List<SpecSynergy> _synergyList = new List<SpecSynergy>();

        protected override void Awake()
        {
            base.Awake();

            _closeButton.onClick.AddListener(OnClickCloseButton);
            _dimLayerButton.onClick.AddListener(OnClickCloseButton);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            _closeButton.onClick.RemoveListener(OnClickCloseButton);
            _dimLayerButton.onClick.RemoveListener(OnClickCloseButton);
        }

        protected override void OnPreEnter(object param)
        {
            base.OnPreEnter(param);
            //TopCurrencyAndMenuBar.AddToUILayer(this, TopPanelType.CloseButton);

            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_btn_popup);

            // _synergyList = param as List<SpecSynergy>;

            SetSynergyInfo();
        }

        private void SetSynergyInfo()
        {
            // if (_synergyList == null || _synergyList.Count == 0) return;
            //
            // var baseSynergyData = _synergyList[0];
            //
            // if (baseSynergyData.synergy_type == SynergyType.NONE)
            // {
            //     _synergyUI.SetSynergyUI(baseSynergyData.synergy_type);
            // }
            //
            // string synergyName = LanguageManager.Instance.GetLanguageText(baseSynergyData.name_token);
            // _synergyNameText.text = synergyName;
            // _synergyNameTitleText.text = string.Format(LanguageManager.Instance.GetLanguageText("SYNERGY_PLACE_EFFECT"), synergyName);
            // _synergyDescText.text = LanguageManager.Instance.GetLanguageText(baseSynergyData.desc_token_1);
            //
            // for (int i = 0; i < _synergyEffectList.Count; i++)
            // {
            //     bool isActive = _synergyList.Count > i;
            //     _synergyEffectList[i].gameObject.SetActive(isActive);
            //
            //     if (!isActive) continue;
            //     string text = LanguageManager.Instance.GetLanguageText(_synergyList[i].desc_token_2);
            //     float statValue = _synergyList[i].skill_value_type == SkillValueType.PERCENT ? _synergyList[i].stat_value * 100f : _synergyList[i].stat_value;
            //
            //     _synergyEffectList[i].text = string.Format(text, _synergyList[i].min_count, statValue);
            // }
        }

        private void OnClickCloseButton()
        {
            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_btn_touch);

            SceneUILayerManager.Instance.PopUILayer(this);
        }
    }
}
