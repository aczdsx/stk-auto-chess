using System;
using System.Collections;
using System.Collections.Generic;
using CookApps.TeamBattle;
using CookApps.TeamBattle.UIManagements;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace CookApps.AutoBattler
{
    public class ToastManager : Singleton<ToastManager>
    {
        public bool IsShowingToast { get; set; } = false;

        /// <summary>
        /// 토스트가 닫힐 때 호출되는 콜백
        /// </summary>
        public Action OnToastClosed { get; set; }

        public void ShowToast(string message, bool isLongToast = false)
        {
            if (IsShowingToast) return;

            IsShowingToast = true;

            SceneUILayerManager.Instance.PushUILayerAsync<ToastSystemPopup>((object)(message)).Forget();
        }

        public void ShowToastByTokenKey(string tokenKey, bool isLongToast = false)
        {
            if (IsShowingToast) return;

            IsShowingToast = true;

            object message = LanguageManager.Instance.GetDefaultText(tokenKey);
            SceneUILayerManager.Instance.PushUILayerAsync<ToastSystemPopup>((object)(message)).Forget();
        }

        /// <summary>
        /// 토스트 메시지 표시 (콜백 포함)
        /// </summary>
        public void ShowToastWithCallback(string message, Action onClosed)
        {
            if (IsShowingToast) return;

            IsShowingToast = true;
            OnToastClosed = onClosed;

            SceneUILayerManager.Instance.PushUILayerAsync<ToastSystemPopup>((object)(message)).Forget();
        }

        /// <summary>
        /// 토스트 메시지 표시 (토큰 키 + 콜백)
        /// </summary>
        public void ShowToastByTokenKeyWithCallback(string tokenKey, Action onClosed)
        {
            if (IsShowingToast) return;

            IsShowingToast = true;
            OnToastClosed = onClosed;

            object message = LanguageManager.Instance.GetDefaultText(tokenKey);
            SceneUILayerManager.Instance.PushUILayerAsync<ToastSystemPopup>((object)(message)).Forget();
        }

        /// <summary>
        /// 토스트 닫힘 알림 (ToastSystemPopup에서 호출)
        /// </summary>
        public void NotifyToastClosed()
        {
            IsShowingToast = false;
            OnToastClosed?.Invoke();
            OnToastClosed = null;
        }
    }
}
