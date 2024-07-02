using System.Collections;
using System.Collections.Generic;
using System;
using System.Threading; 
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using Cysharp.Threading.Tasks;


namespace CookApps.AutoBattler
{
    public class PutInCharacterSlotFocus : MonoBehaviour
    {
        [SerializeField] private Material _focusMat;
        [Space(10)]
        [Header("Glitch Amount")]
        [Range(0f,5f)]
        [SerializeField] private float _randNumb;
        [SerializeField] private float _speed;
        [Space(10)]
        [Header("Chromatic Aberration")]
        [Range(0f, 0.08f)]
        [SerializeField] private float _chromAberrAmount;
        [SerializeField] private  float changeStep = 0.001f;
        private CancellationTokenSource _cts;
        private bool _increasing = true;

        private void OnEnable()
        {
            StartGlitchFX();
        }

        private void OnDisable()
        {
            StopGlitchFX();
        }

        private void StartGlitchFX()
        {
            // 토큰소스 만들고 비동기 GlitchFX 비동기함수 실행
            _cts = new CancellationTokenSource();
            if(_focusMat != null)
            {
                GlitchFX(_cts.Token).Forget();
            }
            else
            {
                Debug.LogError("_focusMaterial Null");
            }

        }

        private void StopGlitchFX()
        {
            //작업취소
            if(_cts != null)
            {
                _cts.Cancel();
                _cts.Dispose();
                _cts = null;
            }

          
        }

        private async UniTaskVoid GlitchFX(CancellationToken cts)
        {
            while (!cts.IsCancellationRequested)
            {
                try
                {
                    _focusMat?.SetFloat("_GlitchAmount", UnityEngine.Random.Range(0, _randNumb));

                    float currentValue = _focusMat.GetFloat("_ChromAberrAmount");

                    if (_increasing)
                    {
                        currentValue += changeStep;
                        if (currentValue >= 0.08f)
                        {
                            currentValue = 0.08f;
                            _increasing = false;
                        }
                    }
                    else
                    {
                        currentValue -= changeStep;
                        if (currentValue <= 0f)
                        {
                            currentValue = 0f;
                            _increasing = true;
                        }
                    }

                    _focusMat?.SetFloat("_ChromAberrAmount", currentValue);

                    await UniTask.Delay(TimeSpan.FromSeconds(_speed), cancellationToken: cts);
                }
                catch(OperationCanceledException)
                {
                    //취소되면
                    Debug.Log("CharacterCard GlitchFX End");
                    break;
                }
            }
        }
    }

}
