using System.Collections;
using System.Collections.Generic;
using CookApps.TeamBattle.UIManagements;
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

        private ImageInfo _specImageInfo;

        protected override void Awake()
        {
            base.Awake();

            _dimCloseButton.onClick.AddListener(OnClickCloseButton);
            _surveyButton.onClick.AddListener(OnClickCloseButton);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            _dimCloseButton.onClick.RemoveListener(OnClickCloseButton);
            _surveyButton.onClick.RemoveListener(OnClickCloseButton);
        }

        protected override void OnPreEnter(object param)
        {
            base.OnPreEnter(param);
            //TopCurrencyAndMenuBar.AddToUILayer(this, TopPanelType.CloseButton);

            int imageInfoID = (int) param;

            _specImageInfo = SpecDataManager.Instance.GetImageInfoData(imageInfoID);
            
            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_btn_popup);

            _infoImage.sprite = ImageManager.Instance.GetInfoImageSprite(_specImageInfo.image_info_id);
            _titleText.text = LanguageManager.Instance.GetLanguageText(_specImageInfo.title_token);
            _descText.text = LanguageManager.Instance.GetLanguageText(_specImageInfo.desc_token);
        }
        
        private void OnClickCloseButton()
        {
            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_btn_confirm);

            SceneUILayerManager.Instance.PopUILayer(this);
        }
    }
}
