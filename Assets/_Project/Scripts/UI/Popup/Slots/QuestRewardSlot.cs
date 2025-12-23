using CookApps.TeamBattle;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CookApps.AutoBattler
{
    public class QuestRewardSlot : CachedMonoBehaviour
    {
        [Header("Common")]
        [SerializeField] private TextMeshProUGUI _rewardItemCountText;

        [Header("Reward - Item")]
        [SerializeField] private Image _rewardItemImage;
        [SerializeField] private SpriteLoader _rewardItemSpriteLoader;

        [Header("Reward - Character")]
        [SerializeField] private GameObject _rewardCharacterBGObject;
        [SerializeField] private Image _rewardCharacterImage;
        [SerializeField] private SpriteLoader _rewardCharacterSpriteLoader;

        public void SetRewardSlot(RewardItem reward)
        {
            ClearSlot();

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
        private void SetRewardItem(RewardItem rewardItem)
        {
            if (rewardItem == null) return;

            _rewardItemImage.gameObject.SetActive(true);

            _rewardItemSpriteLoader.SetSprite(SpriteNameParser.GetSpriteName(rewardItem.Type)).Forget();
            _rewardItemCountText.text = $"x{rewardItem.Count}";
        }

        // 캐릭터 조각 보상 세팅
        private void SetRewardPiece(RewardItem rewardPiece)
        {
            if (rewardPiece == null) return;

            var specCharacterData = SpecDataManager.Instance.GetCharacterData(rewardPiece.Key);
            if (specCharacterData == null) return;

            var userCharacterData = UserDataManager.Instance.GetUserCharacter(specCharacterData.character_id);
            if (userCharacterData == null) return;

            _rewardCharacterBGObject.SetActive(true);
            _rewardCharacterImage.gameObject.SetActive(true);

            _rewardCharacterSpriteLoader.SetSprite(SpriteNameParser.GetCharacterPieceSprite(specCharacterData.prefab_id)).Forget();
            _rewardItemCountText.text = $"x{rewardPiece.Count}";
        }

        // 캐릭터 보상 세팅
        private void SetRewardCharacter(RewardItem rewardCharacter)
        {
            if (rewardCharacter == null) return;

            var specCharacterData = SpecDataManager.Instance.GetCharacterData(rewardCharacter.Key);
            if (specCharacterData == null) return;

            _rewardCharacterBGObject.SetActive(true);
            _rewardCharacterImage.gameObject.SetActive(true);

            _rewardCharacterSpriteLoader.SetSprite(SpriteNameParser.GetCharacterInGamePortraitSprite(specCharacterData.prefab_id)).Forget();
            _rewardItemCountText.text = $"x{rewardCharacter.Count}";
        }

        private void ClearSlot()
        {
            _rewardItemImage.gameObject.SetActive(false);

            _rewardCharacterBGObject.SetActive(false);
            _rewardCharacterImage.gameObject.SetActive(false);
        }
    }
}
