using System.Collections;
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

        // 캐릭터 속성 시너지 세팅
        public void SetGrayCharacter(bool isGray)
        {
            foreach (var uiEffect in _uiEffectList)
            {
                uiEffect.effectMode = (isGray) ? EffectMode.Grayscale : EffectMode.None;
            }
        }
    }
}
