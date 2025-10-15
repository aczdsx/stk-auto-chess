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

        public void ShowToast(string message, bool isLongToast = false)
        {
            if (IsShowingToast) return;

            IsShowingToast = true;

            SceneUILayerManager.Instance.PushUILayerAsync<ToastSystemPopup>((object)(message, isLongToast)).Forget();
        }

        public void ShowToastByTokenKey(string tokenKey, bool isLongToast = false)
        {
            if (IsShowingToast) return;

            IsShowingToast = true;

            object message = LanguageManager.Instance.GetLanguageText(tokenKey);
            SceneUILayerManager.Instance.PushUILayerAsync<ToastSystemPopup>((object)(message, isLongToast)).Forget();
        }
    }
}
