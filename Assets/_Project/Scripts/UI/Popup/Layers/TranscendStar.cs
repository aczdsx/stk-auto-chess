using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace CookApps.AutoBattler
{
    public class TranscendStar : MonoBehaviour
    {
        [SerializeField] private Image _star;
        [SerializeField] private DOTweenAnimation[] _levelUpAnimations;

        public Image Star => _star;

        public void SetActive(bool active, bool withAnimation)
        {
            _star.enabled = active;

            if (active)
            {
                foreach (var anim in _levelUpAnimations)
                {
                    if (withAnimation)
                    {
                        anim.DORestart();
                    }
                    else
                    {
                        anim.DOComplete();
                    }
                }
            }
        }
    }
}
