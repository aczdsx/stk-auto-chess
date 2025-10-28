using System;
using System.Collections;
using System.Collections.Generic;
using CookApps.TeamBattle;
using Spine.Unity;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CookApps.AutoBattler
{
    public class CharacterIllust : CachedMonoBehaviour
    {
        public Material IllustMaterial => _skeletonGraphic != null ? _skeletonGraphic.material : null;

        [SerializeField] private SkeletonGraphic _skeletonGraphic;

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
            }

            return null;
        }
    }
}
