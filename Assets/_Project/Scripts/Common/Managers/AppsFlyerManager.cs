using System.Collections.Generic;
using AppsFlyerSDK;
using CookApps.TeamBattle;
using CookApps.Utility;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace CookApps.AutoBattler
{
    public enum AppsflyerEvent
    {
        LOGIN,
        SESSION,
        REVENUE,
        SHOP_VISIT,
        REGISTRATION,

        STAGE_1_10_CLEAR,
        STAGE_2_10_CLEAR,
        CHAPTER_1_CLEAR,
        CHAPTER_2_CLEAR,
        FIRST_SHOP_OPEN,
        CASTLE_2,
        CASTLE_3,
        CASTLE_4,
        BUY_JEWEL_10000,
        BUY_PACKAGE_EXPENSIVE,
        JOIN_GUILD,

        PURCHASE,
        PURCHASE_FIRST,

        CHAPTER_CLEAR_3 = 300,
        CHAPTER_CLEAR_4,
        CHAPTER_CLEAR_5,
        CHAPTER_CLEAR_6,
        CHAPTER_CLEAR_7,

        CHAPTER_CLEAR_HARD_3 = 3000,
        CHAPTER_CLEAR_HARD_4,
        CHAPTER_CLEAR_HARD_5,
        PURCHASE_SUMMONPACK_VER_1
    }

    public sealed class AppsFlyerManager : SingletonMonoBehaviour<AppsFlyerManager>
    {
        bool _initialized;
        UniTaskCompletionSource _initTcs;

        public async UniTask InitializeAsync(string devKey, string iosAppId, string oneLinkId = null, bool debug = false)
        {
            if (_initialized) return;
            if (_initTcs != null) { await _initTcs.Task; return; }
            _initTcs = new UniTaskCompletionSource();

#if !HAOPLAY
            AppsFlyer.setIsDebug(debug);
            AppsFlyer.initSDK(devKey, iosAppId);
            AppsFlyer.setCustomerUserId(SystemInfo.deviceUniqueIdentifier);
            if (!string.IsNullOrEmpty(oneLinkId)) AppsFlyer.setAppInviteOneLinkID(oneLinkId);
#if UNITY_IOS && !UNITY_EDITOR
            AppsFlyer.waitForATTUserAuthorizationWithTimeoutInterval(60);
#endif
            AppsFlyer.startSDK();
#endif
            _initialized = true;
            _initTcs.TrySetResult();
        }

        public void ReportRevenue(string currencyIso, string price)
        {
#if !HAOPLAY
            var ev = new Dictionary<string, string>
        {
            { AFInAppEvents.CURRENCY, currencyIso },
            { AFInAppEvents.REVENUE, price },
            { AFInAppEvents.QUANTITY, "1" }
        };
            AppsFlyer.sendEvent(AFInAppEvents.PURCHASE, ev);
#endif
        }

        public void Report(AppsflyerEvent evt, string p1 = "", string p2 = "", string p3 = "")
        {
#if !HAOPLAY
            var ev = new Dictionary<string, string>
        {
            { AFInAppEvents.PARAM_1, p1 },
            { AFInAppEvents.PARAM_2, p2 },
            { AFInAppEvents.PARAM_3, p3 },
        };
            AppsFlyer.sendEvent(evt.ToString(), ev);
#endif
        }
    }
}