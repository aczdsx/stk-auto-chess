using System.Collections;
using System.Collections.Generic;
using CookApps.TeamBattle.UIManagements;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CookApps.AutoBattler
{
    public class SkillTooltipPopup : UILayer
    {
        [SerializeField] private CAButton _closeButton;
        [SerializeField] private CAButton _dimButton;

        [Space(10)]
        [SerializeField] private Image _skillIconImage;
        [SerializeField] private GameObject _skillDamageTypeObject;
        [SerializeField] private GameObject _skillDamageAPTypeObject;
        [SerializeField] private GameObject _skillDamageADTypeObject;

        [Space(10)]
        [SerializeField] private TextMeshProUGUI _skillNameText;
        [SerializeField] private TextMeshProUGUI _skillDescText;
        [SerializeField] private TextMeshProUGUI _skillCoolTimeText;
        [SerializeField] private TextMeshProUGUI _skillTypeText;

        [SerializeField] private ContentSizeFitter _sizeFitter;

        protected override void Awake()
        {
            base.Awake();

            _closeButton.onClick.AddListener(OnClickCloseButton);
            _dimButton.onClick.AddListener(OnClickCloseButton);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            _closeButton.onClick.RemoveListener(OnClickCloseButton);
            _dimButton.onClick.RemoveListener(OnClickCloseButton);
        }

        protected override void OnPreEnter(object param)
        {
            base.OnPreEnter(param);
            //TopCurrencyAndMenuBar.AddToUILayer(this, TopPanelType.CloseButton);

            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_btn_popup);
        }

        public void SetSkillToolTipPopup(SpecSkill skillData)
        {
            if (skillData == null) return;

            _skillIconImage.sprite = ImageManager.Instance.GetCharacterSkillSprite(skillData.skill_id);

            _skillNameText.text = LanguageManager.Instance.GetLanguageText(skillData.skill_name_token);
            _skillDescText.text = LanguageManager.Instance.GetLanguageText(skillData.skill_desc_token);

            _skillDamageTypeObject.SetActive(skillData.atk_type != AtkType.NONE);
            _skillDamageAPTypeObject.SetActive(skillData.atk_type == AtkType.AP);
            _skillDamageADTypeObject.SetActive(skillData.atk_type == AtkType.AD);

            _skillTypeText.text = LanguageManager.Instance.GetAtkTypeText(skillData.atk_type);

            var extraSkillData = SpecDataManager.Instance.GetSkillData(skillData.skill_id, SkillValueType.COOL);
            if (extraSkillData != null)
            {
                string cooltimeString = LanguageManager.Instance.GetLanguageText("SKILL_COOLTIME");
                _skillCoolTimeText.text = string.Format(cooltimeString, extraSkillData.base_rate);
                Run.NextFrame(() =>
                {
                    _sizeFitter.enabled = false;
                    _sizeFitter.enabled = true;
                });
            }
        }

        public void SetCommanderSkillToolTipPopup(SpecCommanderSkill skillData)
        {
            if (skillData == null) return;

            _skillIconImage.sprite = ImageManager.Instance.GetCommanderSkillSprite(skillData.commander_skill_id);

            _skillNameText.text = LanguageManager.Instance.GetLanguageText(skillData.name_token);
            _skillDescText.text = LanguageManager.Instance.GetLanguageText(skillData.desc_token);

            // 커맨더 스킬은 타입 off
            _skillDamageTypeObject.SetActive(false);

            var extraSkillData = SpecDataManager.Instance.GetCommanderSkillData(skillData.commander_skill_id, SkillValueType.COOL);
            if (extraSkillData != null)
            {
                string cooltimeString = LanguageManager.Instance.GetLanguageText("SKILL_COOLTIME");
                _skillCoolTimeText.text = string.Format(cooltimeString, extraSkillData.base_rate);
            }
        }

        private void OnClickCloseButton()
        {
            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_btn_touch);

            gameObject.SetActive(false);
        }
    }
}
