using System.Collections;
using System.Collections.Generic;
using CookApps.TeamBattle.UIManagements;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CookApps.AutoBattler
{
    [RegisterUILayer(UILayerType.Modal, "Prefabs/UI/01_Pops/WindowPopup/ItemTooltipPopup.prefab")]
    public class ItemTooltipPopup : UILayer
    {
        [SerializeField] private CAButton _closeButton;
        [SerializeField] private CAButton _dimButton;

        [Header("Item Info")] 
        [SerializeField] private Image _itemIconImage;
        [SerializeField] private TextMeshProUGUI _itemNameText;
        [SerializeField] private TextMeshProUGUI _itemDescText;
        [SerializeField] private TextMeshProUGUI _itemCategoryText;

        private SpecItem _specItemData;
        
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
            
            _specItemData = param as SpecItem;

            SetTooltipPopup();
        }
        
        private void SetTooltipPopup()
        {
            if (_specItemData == null) return;

            _itemIconImage.sprite = ImageManager.Instance.GetItemSprite(_specItemData.item_type);
            _itemNameText.text = LanguageManager.Instance.GetLanguageText(_specItemData.name_token);
            _itemDescText.text = LanguageManager.Instance.GetLanguageText(_specItemData.desc_token);
            _itemCategoryText.text = LanguageManager.Instance.GetItemCategoryText(_specItemData.item_category_type);
        }
        
        private void OnClickCloseButton()
        {
            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_btn_touch);

            SceneUILayerManager.Instance.PopUILayer(this);
        }
    }
}