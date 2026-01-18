using System.Collections.Generic;
using Coffee.UIEffects;
using CookApps.TeamBattle;
using UnityEngine;
using UnityEngine.UI;

namespace CookApps.AutoBattler
{
    public class UICharacter : CachedMonoBehaviour
    {
        [SerializeField] private List<UIEffect> _uiEffectList;
        [SerializeField] private Image _characterImage;

        // 캐릭터 속성 시너지 세팅
        public void SetGrayCharacter(bool isGray)
        {
            foreach (var uiEffect in _uiEffectList)
            {
                uiEffect.effectMode = (isGray) ? EffectMode.Grayscale : EffectMode.None;
            }
        }
        
        // 캐릭터 이미지 컬러 세팅
        public void SetCharacterImageColor(Color color)
        {
            if (_characterImage == null)
            {
                _characterImage = GetComponentInChildren<Image>();
            }

            if (_characterImage != null)
            {
                _characterImage.color = color;
            }
        }
    }
}
