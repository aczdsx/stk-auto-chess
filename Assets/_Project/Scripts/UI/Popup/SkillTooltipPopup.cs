using System.Collections;
using System.Collections.Generic;
using CookApps.TeamBattle;
using CookApps.TeamBattle.UIManagements;
using Cysharp.Threading.Tasks;
using R3;
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
        [SerializeField] private SpriteLoader _skillIconSpriteLoader;
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

            _closeButton.OnClickAsObservable().Subscribe(this, (_, self) => self.OnClickCloseButton()).AddTo(this);
            _dimButton.OnClickAsObservable().Subscribe(this, (_, self) => self.OnClickCloseButton()).AddTo(this);
        }

        protected override void OnPreEnter(object param)
        {
            base.OnPreEnter(param);
            //TopCurrencyAndMenuBar.AddToUILayer(this, TopPanelType.CloseButton);

            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_btn_popup);
        }

        public void SetSkillToolTipPopup(SkillActive skillData)
        {
            if (skillData == null) return;

            _skillIconSpriteLoader.SetSprite(SpriteNameParser.GetCharacterSkillSprite(skillData.skill_group_id)).Forget();

            _skillNameText.text = LanguageManager.Instance.GetDefaultText(skillData.skill_name_token);
            _skillDescText.text = LanguageManager.Instance.GetDefaultText(skillData.skill_desc_token);

            _skillDamageTypeObject.SetActive(skillData.atk_type != AtkType.NONE);
            _skillDamageAPTypeObject.SetActive(skillData.atk_type == AtkType.AP);
            _skillDamageADTypeObject.SetActive(skillData.atk_type == AtkType.AD);

            _skillTypeText.text = LanguageManager.Instance.GetAtkTypeText(skillData.atk_type);

            var extraSkillData = SpecDataManager.Instance.GetSkillData(skillData.skill_group_id, SkillValueType.COOL);
            if (extraSkillData != null)
            {
                string cooltimeString = LanguageManager.Instance.GetDefaultText("SKILL_COOLTIME");
                _skillCoolTimeText.text = string.Format(cooltimeString, extraSkillData.base_rate);
                Run.NextFrame(() =>
                {
                    _sizeFitter.enabled = false;
                    _sizeFitter.enabled = true;
                });
            }
        }

        public void SetCommanderSkillToolTipPopup(SkillCommander skillData)
        {
            if (skillData == null) return;

            _skillIconSpriteLoader.SetSprite(SpriteNameParser.GetCommanderSkillSprite(skillData.commander_skill_id)).Forget();

            _skillNameText.text = LanguageManager.Instance.GetDefaultText(skillData.name_token);
            _skillDescText.text = LanguageManager.Instance.GetDefaultText(skillData.desc_token);

            // 커맨더 스킬은 타입 off
            _skillDamageTypeObject.SetActive(false);

            var extraSkillData = SpecDataManager.Instance.GetCommanderSkillDataList(skillData.commander_skill_id)[0];
            if (extraSkillData != null)
            {
                string cooltimeString = LanguageManager.Instance.GetDefaultText("SKILL_COOLTIME");
                _skillCoolTimeText.text = string.Format(cooltimeString, extraSkillData.cool_time);
            }
        }

        private void OnClickCloseButton()
        {
            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_btn_touch);

            gameObject.SetActive(false);
        }
    }
}
