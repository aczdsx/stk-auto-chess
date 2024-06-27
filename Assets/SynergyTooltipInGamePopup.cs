using System.Collections;
using System.Collections.Generic;
using CookApps.TeamBattle.UIManagements;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CookApps.AutoBattler
{
    [RegisterUILayer(UILayerType.Overlay, "Prefabs/UI/01_Pops/CharacterCollectionPopup/SynergyTooltipInGamePopup.prefab")]
    public class SynergyTooltipInGamePopup : UILayer
    {
        [SerializeField] private CAButton _closeButton;
        [SerializeField] private CAButton _dimLayerButton;

        [Header("Synergy Info Layer")]
        [SerializeField] private SynergyUI _synergyUI;
        [SerializeField] private TextMeshProUGUI _synergyNameText;
        [SerializeField] private TextMeshProUGUI _synergyDescText;

        private List<SpecSynergy> _synergyList = new List<SpecSynergy>();
        private int grade = 0;

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

            (_synergyList, grade) = ((List<SpecSynergy>, int))param;

            SetSynergyInfo();
        }

        private void SetSynergyInfo()
        {
            if (_synergyList == null || _synergyList.Count == 0) return;

            var baseSynergyData = _synergyList[0];

            if (baseSynergyData.character_position_type == CharacterPositionType.NONE)
            {
                _synergyUI.SetSynergyUI(baseSynergyData.element_type);
            }
            else if (baseSynergyData.element_type == ElementType.NONE)
            {
                _synergyUI.SetPositionSynergyUI(baseSynergyData.character_position_type);
            }

            _synergyNameText.text = LanguageManager.Instance.GetLanguageText(baseSynergyData.name_token);
            _synergyDescText.text = LanguageManager.Instance.GetLanguageText(baseSynergyData.desc_token);
        }

        private void OnClickCloseButton()
        {
            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_btn_touch);

            SceneUILayerManager.Instance.PopUILayer(this);
        }
    }
}
