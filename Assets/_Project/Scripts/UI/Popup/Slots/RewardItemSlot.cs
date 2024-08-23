using System;
using System.Collections;
using System.Collections.Generic;
using CookApps.TeamBattle;
using CookApps.TeamBattle.UIManagements;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CookApps.AutoBattler
{
    public class RewardItemSlot : CachedMonoBehaviour
    {
        [SerializeField] private CAButton _rewardSlotButton;
        
        [Header("Reward - Item")]
        [SerializeField] private GameObject _rewardItemLayerObject;
        [SerializeField] private Image _rewardItemImage;
        [SerializeField] private TextMeshProUGUI _rewardItemCountText;
        [SerializeField] private GameObject _checkObj;

        [Header("Reward - Piece")]
        [SerializeField] private GameObject _rewardPieceLayerObject;
        [SerializeField] private Image _rewardPieceImage;
        [SerializeField] private Image _rewardPieceSliderImage;
        [SerializeField] private TextMeshProUGUI _rewardPieceCountText;

        [Header("Reward - Character")]
        [SerializeField] private GameObject _rewardCharacterLayerObject;
        [SerializeField] private Image _rewardCharacterImage;
        [SerializeField] private TextMeshProUGUI _rewardCharacterNameText;
        [SerializeField] private SynergyUI _rewardElementSynergyUI;
        [SerializeField] private SynergyUI _rewardClassSynergyUI;

        private SpecItem _specItemData;
        private int _rewardKey;
        
        private void Awake()
        {
            _rewardSlotButton.onClick.AddListener(OnClickRewardSlotButton);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            
            _rewardSlotButton.onClick.RemoveListener(OnClickRewardSlotButton);
        }

        public void SetRewardSlot(RewardItem reward)
        {
            switch (reward.Type)
            {
                case ItemType.CHARACTER:
                    SetRewardCharacter(reward);
                    break;
                case ItemType.CHARACTER_PIECE:
                    SetRewardPiece(reward);
                    break;
                default:
                    SetRewardItem(reward);
                    break;
            }
        }

        // 일반 아이템 보상 세팅
        public void SetRewardItem(RewardItem rewardItem)
        {
            if (rewardItem == null) return;

            ClearSlot();

            _specItemData = SpecDataManager.Instance.GetSpecItemData(rewardItem.Type);
            
            _rewardItemImage.sprite = ImageManager.Instance.GetItemSprite(rewardItem.Type);
            _rewardItemCountText.text = $"x{rewardItem.Count}";

            // 레이어 활성화
            _rewardItemLayerObject.SetActive(true);
        }

        // 캐릭터 조각 보상 세팅
        public void SetRewardPiece(RewardItem rewardPiece)
        {
            if (rewardPiece == null) return;

            ClearSlot();
            
            _specItemData = SpecDataManager.Instance.GetSpecItemData(rewardPiece.Type);
            _rewardKey = rewardPiece.Key;

            var specCharacterData = SpecDataManager.Instance.GetCharacterData(rewardPiece.Key);
            if (specCharacterData == null) return;

            var userCharacterData = UserDataManager.Instance.GetUserCharacter(specCharacterData.character_id);
            if (userCharacterData == null) return;

            _rewardPieceImage.sprite = ImageManager.Instance.GetCharacterPieceSprite(specCharacterData.prefab_id);
            _rewardPieceSliderImage.fillAmount = (float)userCharacterData.CharacterPiece / specCharacterData.need_piece;
            _rewardPieceCountText.text = $"{userCharacterData.CharacterPiece}/{specCharacterData.need_piece}";

            // 레이어 활성화
            _rewardPieceLayerObject.SetActive(true);
        }

        // 캐릭터 보상 세팅
        public void SetRewardCharacter(RewardItem rewardCharacter)
        {
            if (rewardCharacter == null) return;

            ClearSlot();
            
            _specItemData = SpecDataManager.Instance.GetSpecItemData(rewardCharacter.Type);

            var specCharacterData = SpecDataManager.Instance.GetCharacterData(rewardCharacter.Key);
            if (specCharacterData == null) return;

            _rewardKey = rewardCharacter.Key;
            _rewardCharacterImage.sprite = ImageManager.Instance.GetCharacterInGamePortraitSprite(specCharacterData.prefab_id);
            _rewardCharacterNameText.text = LanguageManager.Instance.GetLanguageText(specCharacterData.name_token);
            _rewardElementSynergyUI.SetSynergyUI(specCharacterData.element_type);
            _rewardClassSynergyUI.SetPositionSynergyUI(specCharacterData.character_position_type);

            // 레이어 활성화
            _rewardCharacterLayerObject.SetActive(true);
        }

        public void SetCheckSlot(bool isActive)
        {
            _checkObj.SetActive(isActive);
        }

        private void OnClickRewardSlotButton()
        {
            if (_specItemData == null) return;
            
            SceneUILayerManager.Instance.PushUILayerAsync<ItemTooltipPopup>((_specItemData, _rewardKey));
        }
        
        private void ClearSlot()
        {
            _rewardItemLayerObject.SetActive(false);
            _rewardPieceLayerObject.SetActive(false);
            _rewardCharacterLayerObject.SetActive(false);
        }
    }
}
