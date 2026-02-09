using LitMotion;
using LitMotion.Extensions;
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

        private MotionHandle _sequenceHandle;

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

            var scaleHandle = LMotion.Create(Vector3.one * StartScale, Vector3.one * EndScale, AnimationDuration)
                .WithEase(Ease.OutBack)
                .BindToLocalScale(starTransform);
            var rotateHandle = LMotion.Create(StartRotationZ, EndRotationZ, AnimationDuration)
                .WithEase(Ease.OutBack)
                .Bind(z => starTransform.localRotation = Quaternion.Euler(0f, 0f, z));
            var fadeHandle = LMotion.Create(0f, 1f, AnimationDuration * 0.5f)
                .BindToColorA(_star);

            _sequenceHandle = LSequence.Create()
                .Append(scaleHandle)
                .Join(rotateHandle)
                .Join(fadeHandle)
                .Run();
            _sequenceHandle.AddTo(this);
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
            if (_sequenceHandle.IsActive())
            {
                _sequenceHandle.Cancel();
            }
            _sequenceHandle = default;
        }

        private void OnDestroy()
        {
            KillSequence();
        }
    }
}
