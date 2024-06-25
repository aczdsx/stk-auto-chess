using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CookApps.TeamBattle.UIManagements;
using DG.Tweening;

namespace CookApps.AutoBattler
{
    public class CollectionCategorySlot : MonoBehaviour
    {
        [SerializeField] private CAToggle _toggle;
        [SerializeField] private RectTransform _rect;
        private Vector2 _beforeVector = new Vector2(200f, 124f);

        private void OnEnable()
        {
            _toggle.onToggleOn.AddListener(FrameTween);
        }

        private void OnDisable()
        {
            _toggle.onToggleOn.RemoveListener(FrameTween);
        }

        void FrameTween()
        {
            _rect.DOSizeDelta(_beforeVector, 0.1f).SetEase(Ease.InOutQuad).From();
        }
    }
}
