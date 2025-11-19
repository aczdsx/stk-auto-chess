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

        // 캐릭터 속성 시너지 세팅
        public void SetSynergyUI(SynergyType elementType, bool isActive = true)
        {
            _iconImage.sprite = ImageManager.Instance.GetSynergySprite(elementType, isActive);
        }

        // 캐릭터 직업 속성 시너지 세팅
        public void SetPositionSynergyUI(SynergyType positionType, bool isActive = true)
        {
            _iconImage.sprite = ImageManager.Instance.GetPositionSprite(positionType, isActive);
        }
    }
}
