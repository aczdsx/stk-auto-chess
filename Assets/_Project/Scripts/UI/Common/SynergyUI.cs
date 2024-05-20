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
        public void SetSynergyUI(CharacterType type)
        {
            _iconImage.sprite = ImageManager.Instance.GetSynergySprite(type);
        }

        // 캐릭터 직업 속성 시너지 세팅
        public void SetPositionSynergyUI(CharacterPosition type)
        {
            _iconImage.sprite = ImageManager.Instance.GetPositionSprite(type);
        }
    }
}
