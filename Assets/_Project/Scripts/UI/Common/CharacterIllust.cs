using System;
using CookApps.TeamBattle;
using Spine.Unity;
using UnityEngine;
using UnityEngine.UI;

namespace CookApps.AutoBattler
{
    public class CharacterIllust : CachedMonoBehaviour
    {
        [SerializeField] private Image _pivotReferenceImage;

#if UNITY_EDITOR
        private void OnValidate()
        {
            // 에디터에서 값 변경 시 pivot 갱신
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this != null)
                {
                    ApplySpritePivot();
                }
            };
        }

        [ContextMenu("Apply Sprite Pivot")]
        private void ApplySpritePivotMenu()
        {
            ApplySpritePivot();
        }
#endif

        /// <summary>
        /// Image에 연결된 Sprite의 pivot을 RectTransform에 적용
        /// </summary>
        public void ApplySpritePivot()
        {
            if (_pivotReferenceImage == null) return;
            if (_pivotReferenceImage.sprite == null) return;

            var sprite = _pivotReferenceImage.sprite;
            // Sprite의 pivot은 픽셀 단위이므로 정규화된 값으로 변환
            Vector2 normalizedPivot = new Vector2(
                sprite.pivot.x / sprite.rect.width,
                sprite.pivot.y / sprite.rect.height
            );

            _pivotReferenceImage.rectTransform.pivot = normalizedPivot;
        }

       
    }
}
