using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace CookApps.AutoBattler
{
    public class TranscendStar : MonoBehaviour
    {
        private const float AnimationDuration = 0.6f;
        private const float StartScale = 3f;
        private const float EndScale = 1f;
        private const float StartRotationZ = 170f;
        private const float EndRotationZ = -13f;

        [SerializeField] private Image _star;

        public Image Star => _star;

        private Sequence _sequence;

        public void SetActive(bool active, bool withAnimation)
        {
            _star.enabled = active;

            if (active)
            {
                if (withAnimation)
                {
                    PlayAnimation();
                }
                else
                {
                    SetEndState();
                }
            }
        }

        private void PlayAnimation()
        {
            KillSequence();

            var starTransform = _star.transform;
            starTransform.localScale = Vector3.one * StartScale;
            starTransform.localRotation = Quaternion.Euler(0f, 0f, StartRotationZ);
            var color = _star.color;
            color.a = 0f;
            _star.color = color;

            _sequence = DOTween.Sequence()
                .Append(starTransform.DOScale(EndScale, AnimationDuration).SetEase(Ease.OutBack))
                .Join(starTransform.DOLocalRotate(new Vector3(0f, 0f, EndRotationZ), AnimationDuration, RotateMode.Fast).SetEase(Ease.OutBack))
                .Join(_star.DOFade(1f, AnimationDuration * 0.5f))
                .SetLink(gameObject);
        }

        private void SetEndState()
        {
            KillSequence();

            var starTransform = _star.transform;
            starTransform.localScale = Vector3.one * EndScale;
            starTransform.localRotation = Quaternion.Euler(0f, 0f, EndRotationZ);
            var color = _star.color;
            color.a = 1f;
            _star.color = color;
        }

        private void KillSequence()
        {
            if (_sequence != null && _sequence.IsActive())
            {
                _sequence.Kill();
                _sequence = null;
            }
        }

        private void OnDestroy()
        {
            KillSequence();
        }
    }
}
