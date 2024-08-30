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
        [SerializeField] private RawImage image;
        [SerializeField] private SkeletonGraphic _skeletonGraphic;

        public void SetImage()
        {
            image.enabled = true;
            _skeletonGraphic.gameObject.SetActive(false);
        }
        
        public void SetCharacterAnimation(string animText)
        {
            _skeletonGraphic.startingAnimation = animText;
        }
    }
}
