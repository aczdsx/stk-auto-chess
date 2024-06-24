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
        public void SetSynergy(ElementType type, int count, int step, bool isActive = true)
        {
            _iconImage.sprite = ImageManager.Instance.GetSynergySprite(type, isActive);
            _countText.text = count.ToString();

            _iconImage.color = (step > 0) ? Color.white : Color.gray;
            _countText.color = (step > 0) ? Color.white : Color.gray;
        }

        // 캐릭터 직업 속성 시너지 세팅
        public void SetPositionSynergy(CharacterPositionType type, int count, int step, bool isActive = true)
        {
            _iconImage.sprite = ImageManager.Instance.GetPositionSprite(type, isActive);
            _countText.text = count.ToString();

            _iconImage.color = (step > 0) ? Color.white : Color.gray;
            _countText.color = (step > 0) ? Color.white : Color.gray;
        }
    }
}
