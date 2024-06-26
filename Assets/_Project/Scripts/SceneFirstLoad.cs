using System;
using System.Collections.Generic;
using CookApps.AutoBattler.Utilities;
using CookApps.TeamBattle;
using CookApps.TeamBattle.UIManagements;
using CookApps.TeamBattle.Utility;
using Cysharp.Threading.Tasks;
using Unity.Collections;
using UnityEngine;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.AddressableAssets;

namespace CookApps.AutoBattler
{
    internal static class IDTransformer
    {
        //Implement a method to transform the internal ids of locations
        private static string MyCustomTransform(IResourceLocation location)
        {
            if (location.ResourceType == typeof(IAssetBundleResource)
                && location.InternalId.StartsWith("http"))
            {
                return location.InternalId;
            }

            return location.InternalId;
        }

        //Override the Addressables transform method with your custom method.
        //This can be set to null to revert to default behavior.
        [RuntimeInitializeOnLoadMethod]
        private static void SetInternalIdTransform()
        {
            Addressables.InternalIdTransformFunc = MyCustomTransform;
        }
    }

    public class SceneFirstLoad : MonoBehaviour
    {
        private async void Awake()
        {
            await UniTask.Yield();

            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            AppLifeCycleEventsDispatcher _ = AppLifeCycleEventsDispatcher.Instance;

            var sceneData = await Addressables.LoadAssetAsync<SceneDatabase>("Data/SceneData.asset");
            Type[] allUILayers = InheritHelper.GetAllImplementations<UILayer>();
            SceneUILayerManager.Instance.Initialize(sceneData, allUILayers);
            Addressables.Release(sceneData);

            SoundManager.Instance.Initialize();

#if UNITY_EDITOR
            NativeLeakDetection.Mode = NativeLeakDetectionMode.EnabledWithStackTrace;
#endif
            var transition = SceneTransition_FadeInOut.Create();
            await SceneLoading.GoToNextScene("Title", null, transition);
        }
    }
}
