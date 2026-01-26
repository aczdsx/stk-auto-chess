using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace CookApps.AutoBattler
{
    public class SubTransition_Animator : SubTransitionBase
    {
        public const string Address = "Prefabs/UI/Loading/AnimateTransition.prefab";

        [SerializeField] private Animator animator;
        [SerializeField] private RawImage _rawImage;
        [Space(10)]
        [Header("Radius")]
        [SerializeField] private string _radiusPropertyName = "_CircleRadius";
        [SerializeField] private float _radiusValue = 0f;
        [Space(10)]
        [Header("DotColor")]
        [SerializeField] private string _colorPropertyName = "_DotColor";
        [SerializeField] private Color _targetColor = Color.red;
        
        private Material _mat;

        private UniTaskCompletionSource _fadeInCompletionSource;
        private UniTaskCompletionSource _fadeOutCompletionSource;

        public override async UniTask FadeInAsync()
        {
            _fadeInCompletionSource = new UniTaskCompletionSource();
            animator.SetTrigger("SetTransitionIn");
            await _fadeInCompletionSource.Task;
        }

        public override async UniTask FadeOutAsync()
        {
            _fadeOutCompletionSource = new UniTaskCompletionSource();
            animator.SetTrigger("SetTransitionOut");
            await _fadeOutCompletionSource.Task;
        }
        
        private CancellationTokenSource cts;

        private void OnEnable()
        {
            cts = new CancellationTokenSource();
            _mat = _rawImage.material;
            _radiusValue = 10;
            _mat.SetFloat(_radiusPropertyName, _radiusValue);
            UpdateShaderProperty(cts.Token).Forget();
        }

        private void OnDisable()
        {
            _radiusValue = 10f;
            _mat.SetFloat(_radiusPropertyName, _radiusValue);

            if (cts != null)
            {
                cts.Cancel();
                cts.Dispose();
                cts = null;
            }
            _mat = null;
        }

        private async UniTaskVoid UpdateShaderProperty(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                if (!_rawImage.enabled)
                {
                    _rawImage.enabled = true;
                }
                //Radius Value
                if (_mat.HasProperty(_radiusPropertyName)){
                    _mat.SetFloat(_radiusPropertyName, _radiusValue);
                    // Debug.Log($"Shader property {_radiusPropertyName} set to: {_radiusValue}");
                }
                else
                {
                    Debug.LogWarning($"Property {_radiusPropertyName} not found in the target material.");
                }


                // Set Color
                if (_mat.HasProperty(_colorPropertyName))
                {
                    _mat.SetColor(_colorPropertyName, _targetColor);
                    // Debug.Log($"Shader property {_colorPropertyName} set to: {_targetColor}");
                }
                else
                {
                    Debug.LogWarning($"Property {_colorPropertyName} not found in the target material.");
                }

                // 지정된 주기만큼 대기
                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }
        }

        public void FadeInComplete()
        {
            _fadeInCompletionSource?.TrySetResult();
        }

        public void FadeOutComplete()
        {
            _fadeOutCompletionSource?.TrySetResult();
        }
    }
}
