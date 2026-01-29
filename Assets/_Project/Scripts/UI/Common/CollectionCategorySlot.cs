using CookApps.TeamBattle.UIManagements;
using LitMotion;
using UnityEngine;

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
            // .From(): _beforeVector에서 현재 크기로 애니메이션
            var currentSize = _rect.sizeDelta;
            LMotion.Create(_beforeVector, currentSize, 0.1f)
                .WithEase(Ease.InOutQuad)
                .Bind(v => _rect.sizeDelta = v)
                .AddTo(this);
        }
    }
}
