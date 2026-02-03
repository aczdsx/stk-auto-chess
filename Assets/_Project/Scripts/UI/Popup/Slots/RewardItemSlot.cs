using CookApps.TeamBattle;
using CookApps.TeamBattle.UIManagements;
using Cysharp.Threading.Tasks;
using R3;
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

        private ISpecItemInfo _specItemData;
        private int _rewardId;

        private void Awake()
        {
            _rewardSlotButton.OnClickAsObservable().Subscribe(this, (_, self) => self.OnClickRewardSlotButton()).AddTo(this);
        }

        public void SetRewardSlot(RewardItem reward)
        {
            if (reward.Id.IsCharacter())
            {
                SetRewardCharacter(reward);
            }
            else if (reward.Id.IsCharacterPiece())
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
            _specItemData = SpecDataManager.Instance.GetSpecItemData(rewardItem.Id);
            
            _rewardItemSpriteLoader.SetSprite(SpriteNameParser.GetItemSprite(rewardItem.Id)).Forget();
            _rewardItemCountText.text = $"x{rewardItem.Count}";
            
            // 레이어 활성화
            _rewardItemLayerObject.SetActive(true);
        }

        // 캐릭터 조각 보상 세팅
        public void SetRewardPiece(RewardItem rewardPiece)
        {
            if (rewardPiece == null) return;

            ClearSlot();

            _specItemData = SpecDataManager.Instance.GetSpecItemData(rewardPiece.Id);
            if (_specItemData.GetItemId().IsCharacterPiece())
                return;

            _rewardId = rewardPiece.Id;
            var specCharacterData = SpecDataManager.Instance.CharacterInfo.Get(_specItemData.GetItemId());
            if (specCharacterData == null) return;
            var userCharacterData = ServerDataManager.Instance.Character.GetCharacter(specCharacterData.id);
            if (userCharacterData == null) return;

            _rewardPieceSpriteLoader.SetSprite(SpriteNameParser.GetCharacterPieceSprite(specCharacterData.id)).Forget();
            int characterPiece = (int)ServerDataManager.Instance.Inventory.GetCurrency((uint)rewardPiece.Id);
            _rewardPieceSliderImage.fillAmount = (float)characterPiece / specCharacterData.need_piece;
            _rewardPieceCountText.text = $"{characterPiece}/{specCharacterData.need_piece}";
            
            // 레이어 활성화
            _rewardPieceLayerObject.SetActive(true);
        }

        // 캐릭터 보상 세팅
        public void SetRewardCharacter(RewardItem rewardCharacter)
        {
            if (rewardCharacter == null) return;

            ClearSlot();

            _specItemData = SpecDataManager.Instance.GetSpecItemData(rewardCharacter.Id);
            _specItemData.GetItemId().GetCharacterId(out var charIndex);
            var specCharacterData = SpecDataManager.Instance.CharacterInfo.Get(charIndex);
            if (specCharacterData == null) return;
            _rewardCharacterSpriteLoader.SetSprite(SpriteNameParser.GetCharacterInGamePortraitSprite(specCharacterData.prefab_id)).Forget();
            _rewardCharacterNameText.text = LanguageManager.Instance.GetDefaultText(specCharacterData.name_token);
            _rewardElementSynergyUI.SetSynergyUI(specCharacterData.character_element_type);
            _rewardClassSynergyUI.SetSynergyUI(specCharacterData.character_stella_type);
            
            _gradeSpriteLoader.SetSprite(SpriteNameParser.GetSpriteName(specCharacterData.grade_type)).Forget();
            
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

            SceneUILayerManager.Instance.PushUILayerAsync<ItemTooltipPopup>(_specItemData).Forget();
        }

        private void ClearSlot()
        {
            _rewardItemLayerObject.SetActive(false);
            _rewardPieceLayerObject.SetActive(false);
            _rewardCharacterLayerObject.SetActive(false);
        }
    }
}
