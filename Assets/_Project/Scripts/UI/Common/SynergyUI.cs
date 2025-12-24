using System.Collections;
using System.Collections.Generic;
using CookApps.TeamBattle;
using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;

namespace CookApps.AutoBattler
{
    public class SynergyUI : CachedMonoBehaviour
    {
        [SerializeField] private Image _iconImage;
        [SerializeField] private SpriteLoader _iconSpriteLoader;

        public void SetSynergyUI(SynergyType synergyType, bool isActive = true)
        {
            _iconSpriteLoader.SetSprite(SpriteNameParser.GetSpriteName(synergyType, isActive)).Forget();
        }
    }
}
