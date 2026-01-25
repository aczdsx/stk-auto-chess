using CookApps.TeamBattle;
using CookApps.TeamBattle.UIManagements;
using Cysharp.Threading.Tasks;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace CookApps.AutoBattler
{
    public class NonSkillTooltipPopup : UILayerPopupBase
    {
        [SerializeField] private CAButton _closeButton;
        [SerializeField] private CAButton _dimButton;

        [Space(10)]
        [SerializeField] private Image _IconImage;
        [SerializeField] private SpriteLoader _IconSpriteLoader;

        [Space(10)]
        [SerializeField] private TextMeshProUGUI _NameText;
        [SerializeField] private TextMeshProUGUI _DescText;


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

        public void SetPassiveSkillToolTipPopup(List<SkillPassive> skillData)
        {
            if (skillData == null || skillData.Count == 0) return;

            _IconSpriteLoader.SetSprite(SpriteNameParser.GetCharacterPassiveSkillSprite(skillData[0].passive_group_id)).Forget();
            string text = LanguageManager.Instance.GetDefaultText(skillData[0].passive_desc_token);

            if (!string.IsNullOrEmpty(text))
            {
                _DescText.text = FormatDescWithPlaceholders(text, skillData);
            }
            else
            {
                _DescText.text = text ?? string.Empty;
            }


            _NameText.text = LanguageManager.Instance.GetDefaultText(skillData[0].passive_name_token);
        }

        /// <summary>
        /// text 내 {0}, {1}, {2}, {3} 등 모든 플레이스홀더의 최대 인덱스를 찾아
        /// skillData[0].base_rate, skillData[1].base_rate, ... 로 채운 뒤 string.Format.
        /// skillData 개수가 부족하면 원문 반환. 플레이스홀더가 없으면 원문 반환.
        /// </summary>
        private static string FormatDescWithPlaceholders(string text, List<SkillPassive> skillData)
        {
            int maxIndex = GetMaxPlaceholderIndex(text);
            if (maxIndex < 0)
                return text;

            int required = maxIndex + 1;
            if (skillData.Count < required)
                return text;

            var args = new object[required];
            for (int i = 0; i < required; i++)
                args[i] = skillData[i].base_rate;

            return string.Format(text, args);
        }

        private static int GetMaxPlaceholderIndex(string text)
        {
            int max = -1;
            foreach (Match m in Regex.Matches(text, @"\{(\d+)\}"))
            {
                int n = int.Parse(m.Groups[1].Value);
                if (n > max) max = n;
            }
            return max;
        }

        public void SetJobSkillToolTipPopup(SkillJob skillData, CharacterPositionType positionType)
        {
            if (skillData == null) return;

            _IconSpriteLoader.SetSprite(SpriteNameParser.GetCharacterJobSkillSprite(positionType)).Forget();

            _NameText.text = LanguageManager.Instance.GetDefaultText(skillData.jobs_name_token);
            string text = LanguageManager.Instance.GetDefaultText(skillData.jobs_desc_token);

            if (!string.IsNullOrEmpty(text))
            {
                if (text.Contains("{1}"))
                {
                    var first = skillData.passive_rate;
                    var second = skillData.passive_rate_2;
                    if (skillData.skill_value_type == SkillValueType.PERCENT)
                        first = first * 100;
                    if (skillData.skill_value_type_2 == SkillValueType.PERCENT)
                        second = second * 100;
                    _DescText.text = string.Format(text, first, second);
                }
                else if (text.Contains("{0}"))
                {
                    var first = skillData.passive_rate;
                    if (skillData.skill_value_type == SkillValueType.PERCENT)
                    {
                        first = first * 100;

                    }

                    // {0}만 필요한 경우
                    _DescText.text = string.Format(text, first);
                }
                else
                {
                    // 플레이스홀더가 없는 경우
                    _DescText.text = text;
                }
            }
            else
            {
                _DescText.text = text;
            }


        }

        private void OnClickCloseButton()
        {
            gameObject.SetActive(false);
        }
    }
}
