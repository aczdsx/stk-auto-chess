using System.Collections;
using System.Collections.Generic;
using CookApps.TeamBattle;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CookApps.AutoBattler
{
    public class InGameSynergyUI : CachedMonoBehaviour
    {
        [SerializeField] private Image _iconImage;
        [SerializeField] private TextMeshProUGUI _countText;

        // 캐릭터 속성 시너지 세팅
        public void SetSynergy(CharacterType type, int count, bool isActive = true)
        {
            _iconImage.sprite = ImageManager.Instance.GetSynergySprite(type, isActive);
            _countText.text = count.ToString();
        }

        // 캐릭터 직업 속성 시너지 세팅
        public void SetPositionSynergy(CharacterPositionType type, int count, bool isActive = true)
        {
            _iconImage.sprite = ImageManager.Instance.GetClassSprite(type, isActive);
            _countText.text = count.ToString();
        }
    }
}
