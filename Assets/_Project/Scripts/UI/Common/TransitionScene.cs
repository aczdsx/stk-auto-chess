using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace CookApps.AutoBattler
{
    public class TransitionScene : MonoBehaviour
    {
        public RawImage _rawImage;
        public Material _mat;
        [Space(10)]
        [Header("Radius")]
        public string _radiusPropertyName = "_CircleRadius";
        public float _radiusValue = 0f;
        [Space(10)]
        [Header("DotColor")]
        public string _colorPropertyName = "_DotColor";
        public Color _targetColor = Color.red;

        private CancellationTokenSource cts;

        private void OnEnable()
        {
            _mat = _rawImage.material;

            if (_mat != null)
            {
                cts = new CancellationTokenSource();
                UpdateShaderProperty(cts.Token).Forget();
            }
            else
            {
                Debug.LogError("Target Material is not assigned.");
            }
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

    }

}
