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

        public void ShowToast(string message)
        {
            if (IsShowingToast) return;

            IsShowingToast = true;

            SceneUILayerManager.Instance.PushUILayerAsync<ToastSystemPopup>(message).Forget();
        }

        public void ShowToastByTokenKey(string tokenKey)
        {
            if (IsShowingToast) return;

            IsShowingToast = true;

            object message = LanguageManager.Instance.GetLanguageText(tokenKey);
            SceneUILayerManager.Instance.PushUILayerAsync<ToastSystemPopup>(message).Forget();
        }
    }
}
