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
        public Material IllustMaterial => _skeletonGraphic.material;

        [SerializeField] private SkeletonGraphic _skeletonGraphic;

        public void SetCharacterAnimation(string animText)
        {
            _skeletonGraphic.Initialize(true);
            _skeletonGraphic.AnimationState.SetAnimation(0, animText, true); // 트랙0, 루프
        }
    }
}
