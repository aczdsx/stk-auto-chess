using System;
using System.Collections;
using System.Collections.Generic;
using CookApps.TeamBattle;
using CookApps.TeamBattle.UIManagements;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;

namespace CookApps.AutoBattler
{
    public class RewardItemSlot : CachedMonoBehaviour
    {
        [SerializeField] private CAButton _rewardSlotButton;

        [Header("Reward - Item")]
        [SerializeField] private GameObject _rewardItemLayerObject;
        [SerializeField] private Image _rewardItemImage;
        [SerializeField] private SpriteLoader _rewardItemSpriteLoader;
        [SerializeField] private TextMeshProUGUI _rewardItemCountText;
        [SerializeField] private GameObject _checkObj;

        [Header("Reward - Piece")]
        [SerializeField] private GameObject _rewardPieceLayerObject;
        [SerializeField] private Image _rewardPieceImage;
        [SerializeField] private SpriteLoader _rewardPieceSpriteLoader;
        [SerializeField] private Image _rewardPieceSliderImage;
        [SerializeField] private TextMeshProUGUI _rewardPieceCountText;

        [Header("Reward - Character")]
        [SerializeField] private GameObject _rewardCharacterLayerObject;
        [SerializeField] private Image _rewardCharacterImage;
        [SerializeField] private SpriteLoader _rewardCharacterSpriteLoader;
        [SerializeField] private TextMeshProUGUI _rewardCharacterNameText;
        [SerializeField] private SynergyUI _rewardElementSynergyUI;
        [SerializeField] private SynergyUI _rewardClassSynergyUI;
        [SerializeField] private Image _gradeImage;
        [SerializeField] private SpriteLoader _gradeSpriteLoader;

        private Item _specItemData;
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
            if (reward.Key.IsCharacterId())
            {
                SetRewardCharacter(reward);
            }
            else if (reward.Key.IsCharacterPieceId())
            {
                SetRewardPiece(reward);
            }
            else
            {
                SetRewardItem(reward);   
            }
        }

        // 일반 아이템 보상 세팅
        public void SetRewardItem(RewardItem rewardItem)
        {
            if (rewardItem == null) return;

            ClearSlot();

            // TODO: 아이템 표시
            // _specItemData = SpecDataManager.Instance.GetSpecItemData(rewardItem.Type);
            //
            // _rewardItemSpriteLoader.SetSprite(SpriteNameParser.GetSpriteName(rewardItem.Type)).Forget();
            // _rewardItemCountText.text = $"x{rewardItem.Count}";
            //
            // // 레이어 활성화
            // _rewardItemLayerObject.SetActive(true);
        }

        // 캐릭터 조각 보상 세팅
        public void SetRewardPiece(RewardItem rewardPiece)
        {
            if (rewardPiece == null) return;

            ClearSlot();

            // TODO: 아이템 표시
            // _specItemData = SpecDataManager.Instance.GetSpecItemData(rewardPiece.Type);
            // _rewardKey = rewardPiece.Key;
            //
            // var specCharacterData = SpecDataManager.Instance.GetCharacterData(rewardPiece.Key);
            // if (specCharacterData == null) return;
            //
            // var userCharacterData = UserDataManager.Instance.GetUserCharacter(specCharacterData.character_id);
            // if (userCharacterData == null) return;
            //
            // _rewardPieceSpriteLoader.SetSprite(SpriteNameParser.GetCharacterPieceSprite(specCharacterData.prefab_id)).Forget();
            // _rewardPieceSliderImage.fillAmount = (float)userCharacterData.CharacterPiece / specCharacterData.need_piece;
            // _rewardPieceCountText.text = $"{userCharacterData.CharacterPiece}/{specCharacterData.need_piece}";
            //
            // // 레이어 활성화
            // _rewardPieceLayerObject.SetActive(true);
        }

        // 캐릭터 보상 세팅
        public void SetRewardCharacter(RewardItem rewardCharacter)
        {
            if (rewardCharacter == null) return;

            ClearSlot();

            // TODO: 아이템 표시
            // _specItemData = SpecDataManager.Instance.GetSpecItemData(rewardCharacter.Type);
            //
            // var specCharacterData = SpecDataManager.Instance.GetCharacterData(rewardCharacter.Key);
            // if (specCharacterData == null) return;
            //
            // _rewardKey = rewardCharacter.Key;
            // _rewardCharacterSpriteLoader.SetSprite(SpriteNameParser.GetCharacterInGamePortraitSprite(specCharacterData.prefab_id)).Forget();
            // _rewardCharacterNameText.text = LanguageManager.Instance.GetLanguageText(specCharacterData.name_token);
            // _rewardElementSynergyUI.SetSynergyUI(specCharacterData.character_element_type);
            // _rewardClassSynergyUI.SetSynergyUI(specCharacterData.character_stella_type);
            //
            // _gradeSpriteLoader.SetSprite(SpriteNameParser.GetSpriteName(specCharacterData.grade_type)).Forget();
            //
            // // 레이어 활성화
            // _rewardCharacterLayerObject.SetActive(true);
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
