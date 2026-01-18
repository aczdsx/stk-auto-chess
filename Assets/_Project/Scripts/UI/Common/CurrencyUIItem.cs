using CookApps.TeamBattle;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CookApps.AutoBattler
{
    public class CurrencyUIItem : CachedMonoBehaviour
    {
        [SerializeField] private Image _itemIconImage;
        [SerializeField] private SpriteLoader _itemIconSpriteLoader;
        [SerializeField] private TextMeshProUGUI itemAmountText;
        [SerializeField] private Color enoughColor;
        [SerializeField] private Color notEnoughColor;

        public void SetUIItem(ItemId itemId, int amount)
        {
            if (itemId.IsCharacterPiece())
            {
                itemId.GetCharacterId(out var charIndex);
                var specCharacterData = SpecDataManager.Instance.CharacterInfo.Get(charIndex);

                _itemIconSpriteLoader.SetSprite(SpriteNameParser.GetCharacterPieceSprite(specCharacterData.prefab_id)).Forget();
            }
            else
            {
                _itemIconSpriteLoader.SetSprite(SpriteNameParser.GetItemSprite(itemId)).Forget();
            }

            itemAmountText.text = amount.ToString("N0");
        }

        public void SetUIItem(ItemId itemId, int amount, bool isEnough)
        {
            if (itemId.IsCharacterPiece())
            {
                itemId.GetCharacterId(out var charIndex);
                var specCharacterData = SpecDataManager.Instance.CharacterInfo.Get(charIndex);

                _itemIconSpriteLoader.SetSprite(SpriteNameParser.GetCharacterPieceSprite(specCharacterData.prefab_id)).Forget();
            }
            else
            {
                _itemIconSpriteLoader.SetSprite(SpriteNameParser.GetItemSprite(itemId)).Forget();
            }

            itemAmountText.color = isEnough ? enoughColor : notEnoughColor;
            itemAmountText.text = amount.ToString("N0");
        }
    }
}
