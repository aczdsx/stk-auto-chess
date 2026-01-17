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
    public class ItemTooltipPopup : UILayerPopupBase
    {
        [SerializeField] private CAButton _closeButton;
        [SerializeField] private CAButton _dimButton;

        [Header("Item Info")]
        [SerializeField] private GameObject _itemInfoObj;
        [SerializeField] private Image _itemIconImage;
        [SerializeField] private SpriteLoader _itemIconSpriteLoader;
        [SerializeField] private TextMeshProUGUI _itemNameText;
        [SerializeField] private TextMeshProUGUI _itemDescText;
        [SerializeField] private TextMeshProUGUI _itemCategoryText;

        [Header("Item Info")]
        [SerializeField] private GameObject _characterInfoObj;
        [SerializeField] private Image _characterIconImage;
        [SerializeField] private SpriteLoader _characterIconSpriteLoader;
        [SerializeField] private SynergyUI _characterSynergyUI1;
        [SerializeField] private SynergyUI _characterSynergyUI2;

        private ISpecItemInfo _specItemInfo;

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

            _specItemInfo = param as ISpecItemInfo;

            SetTooltipPopup();
        }

        private void SetTooltipPopup()
        {
            if (_specItemInfo == null) return;

            bool isCharacter = _specItemInfo.GetItemId().IsCharacter() ||
                               _specItemInfo.GetItemId().IsCharacterPiece();

            _characterInfoObj.SetActive(isCharacter);
            _itemInfoObj.SetActive(!isCharacter);

            if (isCharacter)
            {
                _specItemInfo.GetItemId().GetCharacterId(out var charIndex);
                var specCharacterData = SpecDataManager.Instance.CharacterInfo.Get(charIndex);
                _characterIconSpriteLoader.SetSprite(SpriteNameParser.GetCharacterInGamePortraitSprite(specCharacterData.prefab_id)).Forget();
                _itemNameText.text = LanguageManager.Instance.GetDefaultText(specCharacterData.name_token);
                _itemDescText.text = LanguageManager.Instance.GetDefaultText(specCharacterData.desc_token);
                _characterSynergyUI1.SetSynergyUI(specCharacterData.character_element_type);
                _characterSynergyUI2.SetSynergyUI(specCharacterData.character_stella_type);
            }
            else
            {
                _itemIconSpriteLoader.SetSprite(SpriteNameParser.GetItemSprite(_specItemInfo.GetItemId())).Forget();
                _itemNameText.text = LanguageManager.Instance.GetDefaultText(_specItemInfo.name_token);
                _itemDescText.text = LanguageManager.Instance.GetDefaultText(_specItemInfo.desc_token);
            }

            // TODO: 뭐고 이건
            // _itemCategoryText.text = LanguageManager.Instance.GetItemCategoryText(_specItemInfo);
        }

        private void OnClickCloseButton()
        {
            SceneUILayerManager.Instance.PopUILayer(this);
        }
    }
}
