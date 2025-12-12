using System.Collections;
using System.Collections.Generic;
using CookApps.TeamBattle;
using UnityEngine;
using UnityEngine.UI;

namespace CookApps.AutoBattler
{
    public class SynergyUI : CachedMonoBehaviour
    {
        [SerializeField] private Image _iconImage;

        public void SetSynergyUI(SynergyType synergyType, bool isActive = true)
        {
            _iconImage.sprite = ImageManager.Instance.GetSynergySprite(synergyType, isActive);
        }
    }
}
