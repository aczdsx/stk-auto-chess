using System;
using CookApps.TeamBattle;
using Spine.Unity;
using UnityEngine;
using UnityEngine.UI;

namespace CookApps.AutoBattler
{
    public class CharacterIllust : CachedMonoBehaviour
    {
        public Material IllustMaterial => _skeletonGraphic != null ? _skeletonGraphic.material : null;

        [SerializeField] private SkeletonGraphic _skeletonGraphic;
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

        public void SetCharacterAnimation(string animText)
        {
            if (_skeletonGraphic == null) return;

            // 가능한 애니메이션 이름을 대소문자 무시하고 탐색
            string resolved = ResolveAnimationName(animText);
            if (resolved == null)
            {
                // 공통 폴백: Idle 계열 우선 탐색
                resolved = ResolveAnimationName("Idle");
            }

            if (resolved == null)
            {
                return; // 설정 가능한 애니메이션이 없음
            }

            _skeletonGraphic.Initialize(true);
            _skeletonGraphic.AnimationState.ClearTrack(0);
            _skeletonGraphic.AnimationState.SetAnimation(0, resolved, true);
        }

        private string ResolveAnimationName(string nameCandidate)
        {
            var data = _skeletonGraphic.Skeleton?.Data;
            if (data == null) return null;

            // 1) 정확히 존재하면 그대로 사용
            if (data.FindAnimation(nameCandidate) != null)
            {
                return nameCandidate;
            }

            // 2) 대소문자 무시하고 검색 (idle/Idle/IDLE 등 처리)
            foreach (var anim in data.Animations)
            {
                if (string.Equals(anim.Name, nameCandidate, StringComparison.OrdinalIgnoreCase))
                {
                    return anim.Name; // 실제 등록된 이름으로 반환
                }
                if (anim.Name.Contains(nameCandidate, StringComparison.OrdinalIgnoreCase))
                {
                    return anim.Name;
                }
            }

            return null;
        }
    }
}
