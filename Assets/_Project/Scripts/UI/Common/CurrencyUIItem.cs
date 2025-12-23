using System.Collections;
using System.Collections.Generic;
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

        public void SetUIItem(ItemType type, int key, int amount)
        {
            if (type == ItemType.CHARACTER_PIECE)
            {
                var specCharacterData = SpecDataManager.Instance.GetCharacterData(key);

                _itemIconSpriteLoader.SetSprite(SpriteNameParser.GetCharacterPieceSprite(specCharacterData.prefab_id)).Forget();
            }
            else
            {
                _itemIconSpriteLoader.SetSprite(SpriteNameParser.GetSpriteName(type)).Forget();
            }

            itemAmountText.text = amount.ToString("N0");
        }

        public void SetUIItem(ItemType type, int key, int amount, bool isEnough)
        {
            if (type == ItemType.CHARACTER_PIECE)
            {
                var specCharacterData = SpecDataManager.Instance.GetCharacterData(key);

                _itemIconSpriteLoader.SetSprite(SpriteNameParser.GetCharacterPieceSprite(specCharacterData.prefab_id)).Forget();
            }
            else
            {
                _itemIconSpriteLoader.SetSprite(SpriteNameParser.GetSpriteName(type)).Forget();
            }

            itemAmountText.color = isEnough ? enoughColor : notEnoughColor;
            itemAmountText.text = amount.ToString("N0");
        }
    }
}
