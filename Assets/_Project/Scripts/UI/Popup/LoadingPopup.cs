using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using CookApps.TeamBattle.UIManagements;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace CookApps.AutoBattler
{
    [RegisterUILayer(UILayerType.Popup, "Prefabs/UI/01_Pops/LoadingPopup.prefab")]
    public class LoadingPopup : UILayer
    {
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] float _delayDuration = 0.8f;
        [SerializeField] float _fadeDuration = 0.7f;
        [SerializeField] float _endDuration = 1.2f;
        [SerializeField] float _hideDuration = 0.3f;

        private CancellationTokenSource _cancellationToken;

        protected override void OnPreEnter(object param)
        {
            base.OnPreEnter(param);
            _cancellationToken = new CancellationTokenSource();
            ShowAndHideCanvasGroup(_cancellationToken.Token).Forget();
        }
        
        protected override void OnDestroy()
        {
            _cancellationToken?.Cancel();
            base.OnDestroy();
        }

        protected override void OnBackButton(ref bool offPrevUI)
        {
        }

        private async UniTask ShowAndHideCanvasGroup(CancellationToken cancellationToken = default)
        {
            await FadeInCanvasGroup(_fadeDuration, cancellationToken);
            await UniTask.Delay(TimeSpan.FromSeconds(_endDuration), cancellationToken: cancellationToken);
            await FadeOutCanvasGroup(_hideDuration, cancellationToken);
            ToastManager.Instance.ShowToastByTokenKey("MSG_NETWORK_WRONG");
            SceneUILayerManager.Instance.PopUILayer(this);
        }

        private async UniTask FadeInCanvasGroup(float duration, CancellationToken cancellationToken = default)
        {
            float elapsed = 0f;
            _canvasGroup.alpha = 0f;

            await UniTask.Delay(TimeSpan.FromSeconds(_delayDuration), cancellationToken: cancellationToken);

            while (elapsed < duration)
            {
                if (_canvasGroup == null)
                    return;

                elapsed += Time.deltaTime;
                _canvasGroup.alpha = Mathf.Clamp01(elapsed / duration);
                await UniTask.Yield(cancellationToken);
            }

            _canvasGroup.alpha = 1f;
        }

        private async UniTask FadeOutCanvasGroup(float duration, CancellationToken cancellationToken = default)
        {
            float elapsed = 0f;

            while (elapsed < duration)
            {
                if (_canvasGroup == null)
                    return;

                elapsed += Time.deltaTime;
                _canvasGroup.alpha = Mathf.Clamp01(1 - (elapsed / duration));
                await UniTask.Yield(cancellationToken);
            }

            _canvasGroup.alpha = 0f;
        }
    }
}
