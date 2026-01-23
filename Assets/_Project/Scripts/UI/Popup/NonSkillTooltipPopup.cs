using CookApps.TeamBattle;
using CookApps.TeamBattle.UIManagements;
using Cysharp.Threading.Tasks;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

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
                // 플레이스홀더 개수에 따라 다른 값 전달
                if (text.Contains("{2}"))
                {
                    // {0}, {1}, {2} 모두 필요한 경우
                    _DescText.text = string.Format(text, skillData[0].base_rate, skillData[1].base_rate, skillData[2].base_rate);
                }
                else if (text.Contains("{1}"))
                {
                    // {0}, {1}만 필요한 경우
                    _DescText.text = string.Format(text, skillData[0].base_rate, skillData[1].base_rate);
                }
                else if (text.Contains("{0}"))
                {
                    // {0}만 필요한 경우
                    _DescText.text = string.Format(text, skillData[0].base_rate);
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


            _NameText.text = LanguageManager.Instance.GetDefaultText(skillData[0].passive_name_token);

        }

        public void SetJobSkillToolTipPopup(SkillJob skillData, CharacterPositionType positionType)
        {
            if (skillData == null) return;

            _IconSpriteLoader.SetSprite(SpriteNameParser.GetCharacterJobSkillSprite(positionType)).Forget();

            _NameText.text = LanguageManager.Instance.GetDefaultText(skillData.jobs_name_token);
            _DescText.text = LanguageManager.Instance.GetDefaultText(skillData.jobs_desc_token);

        }

        private void OnClickCloseButton()
        {
            gameObject.SetActive(false);
        }
    }
}
