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
            if (DistinguishSynergyTypeHelper.IsElementSynergyType(synergyType))
            {
                _iconImage.sprite = ImageManager.Instance.GetElementSprite(synergyType, isActive);
            }
            else if (DistinguishSynergyTypeHelper.IsAsterismSynergyType(synergyType))
            {
                _iconImage.sprite = ImageManager.Instance.GetSynergySprite(synergyType, isActive);
            }
        }
    }
}
