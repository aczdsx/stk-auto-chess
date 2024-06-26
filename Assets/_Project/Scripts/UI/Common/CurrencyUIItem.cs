using System.Collections;
using System.Collections.Generic;
using CookApps.TeamBattle;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CookApps.AutoBattler
{
    public class CurrencyUIItem : CachedMonoBehaviour
    {
        [SerializeField] private Image _itemIconImage;
        [SerializeField] private TextMeshProUGUI itemAmountText;

        public void SetUIItem(ItemType type, int key, int amount)
        {
            if (type == ItemType.CHARACTER_PIECE)
            {
                var specCharacterData = SpecDataManager.Instance.GetCharacterData(key);

                _itemIconImage.sprite = ImageManager.Instance.GetCharacterPieceSprite(specCharacterData.prefab_id);
            }
            else
            {
                _itemIconImage.sprite = ImageManager.Instance.GetItemSprite(type);
            }

            itemAmountText.text = amount.ToString("N0");
        }
    }
}
