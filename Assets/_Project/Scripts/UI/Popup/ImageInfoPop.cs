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
    public class ImageInfoPop : UILayer
    {
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private TextMeshProUGUI _descText;

        [SerializeField] private CAButton _dimCloseButton;
        [SerializeField] private CAButton _surveyButton;

        [SerializeField] private Image _infoImage;
        [SerializeField] private SpriteLoader _infoSpriteLoader;

        private ImageInfo _specImageInfo;

        protected override void Awake()
        {
            base.Awake();

            _dimCloseButton.OnClickAsObservable().Subscribe(this, (_, self) => self.OnClickCloseButton()).AddTo(this);
            _surveyButton.OnClickAsObservable().Subscribe(this, (_, self) => self.OnClickCloseButton()).AddTo(this);
        }

        protected override void OnPreEnter(object param)
        {
            base.OnPreEnter(param);
            //TopCurrencyAndMenuBar.AddToUILayer(this, TopPanelType.CloseButton);

            int imageInfoID = (int)param;

            _specImageInfo = SpecDataManager.Instance.GetImageInfoData(imageInfoID);

            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_btn_popup);

            _infoSpriteLoader.SetSprite(SpriteNameParser.GetInfoImageSprite(_specImageInfo.image_info_id)).Forget();
            _titleText.text = LanguageManager.Instance.GetDefaultText(_specImageInfo.title_token);
            _descText.text = LanguageManager.Instance.GetDefaultText(_specImageInfo.desc_token);
        }

        private void OnClickCloseButton()
        {
            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_btn_confirm);

            SceneUILayerManager.Instance.PopUILayer(this);
        }
    }
}
