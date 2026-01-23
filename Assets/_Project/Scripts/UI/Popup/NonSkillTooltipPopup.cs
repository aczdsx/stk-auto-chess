using CookApps.TeamBattle;
using CookApps.TeamBattle.UIManagements;
using Cysharp.Threading.Tasks;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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

        public void SetPassiveSkillToolTipPopup(SkillPassive skillData)
        {
            if (skillData == null) return;

            _IconSpriteLoader.SetSprite(SpriteNameParser.GetCharacterPassiveSkillSprite(skillData.passive_group_id)).Forget();

            _NameText.text = LanguageManager.Instance.GetDefaultText(skillData.passive_name_token);
            _DescText.text = LanguageManager.Instance.GetDefaultText(skillData.passive_desc_token);

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
