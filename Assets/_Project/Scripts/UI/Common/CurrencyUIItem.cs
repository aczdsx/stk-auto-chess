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

        public void SetUIItem(ItemType type, int amount)
        {
            _itemIconImage.sprite = ImageManager.Instance.GetItemSprite(type);
            itemAmountText.text = amount.ToString("N0");
        }
    }
}
