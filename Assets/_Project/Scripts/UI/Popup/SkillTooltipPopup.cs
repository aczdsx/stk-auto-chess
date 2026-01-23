using CookApps.TeamBattle;
using CookApps.TeamBattle.UIManagements;
using CookApps.BattleSystem;
using Cysharp.Threading.Tasks;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using CharacterController = CookApps.BattleSystem.CharacterController;

namespace CookApps.AutoBattler
{
    public class SkillTooltipPopup : UILayerPopupBase
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

        public void SetSkillToolTipPopup(List<SkillActive> skillData)
        {
            if (skillData == null || skillData.Count == 0) return;

            _skillIconSpriteLoader.SetSprite(SpriteNameParser.GetCharacterSkillSprite(skillData[0].skill_group_id)).Forget();

            _skillNameText.text = LanguageManager.Instance.GetDefaultText(skillData[0].skill_name_token);
            string text = LanguageManager.Instance.GetDefaultText(skillData[0].skill_desc_token);
            
            if (!string.IsNullOrEmpty(text))
            {
                // 플레이스홀더 개수 확인
                int maxPlaceholderIndex = -1;
                for (int i = 0; i < CharacterController.CharacterActiveSkillStatCnt; i++)
                {
                    if (text.Contains($"{{{i}}}"))
                    {
                        maxPlaceholderIndex = i;
                    }
                }
                
                if (maxPlaceholderIndex >= 0)
                {
                    // 필요한 플레이스홀더 개수만큼 base_rate 값 수집
                    int requiredCount = maxPlaceholderIndex + 1;
                    int availableCount = Mathf.Min(skillData.Count, CharacterController.CharacterActiveSkillStatCnt);
                    
                    if (requiredCount <= availableCount)
                    {
                        object[] formatArgs = new object[requiredCount];
                        for (int i = 0; i < requiredCount; i++)
                        {
                            formatArgs[i] = skillData[i].base_rate;
                        }
                        _skillDescText.text = string.Format(text, formatArgs);
                    }
                    else
                    {
                        // 필요한 개수만큼 skillData가 없는 경우
                        _skillDescText.text = text;
                    }
                }
                else
                {
                    // 플레이스홀더가 없는 경우
                    _skillDescText.text = text;
                }
            }
            else
            {
                _skillDescText.text = text;
            }

            _skillDamageTypeObject.SetActive(skillData[0].atk_type != AtkType.NONE);
            _skillDamageAPTypeObject.SetActive(skillData[0].atk_type == AtkType.AP);
            _skillDamageADTypeObject.SetActive(skillData[0].atk_type == AtkType.AD);

            _skillTypeText.text = LanguageManager.Instance.GetAtkTypeText(skillData[0].atk_type);

            var extraSkillData = SpecDataManager.Instance.GetSkillData(skillData[0].skill_group_id, SkillValueType.COOL);
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
            gameObject.SetActive(false);
        }
    }
}
