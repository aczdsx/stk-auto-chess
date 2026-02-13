using CookApps.TeamBattle.UIManagements;
using Cysharp.Threading.Tasks;
using Unity.Collections;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;

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

            Debug.Log("SceneFirstLoad Awake");

            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            AppLifeCycleEventsDispatcher _ = AppLifeCycleEventsDispatcher.Instance;

            SceneUILayerManager.Instance.Initialize();

            Debug.Log("SceneFirstLoad Awake -1");

            SoundManager.Instance.Initialize();

#if UNITY_EDITOR
            NativeLeakDetection.Mode = NativeLeakDetectionMode.EnabledWithStackTrace;
#endif
            SceneTransition.Create<SceneTransition_FadeInOut>();
            await SceneTransition.FadeInAsync(playSound: false);
            SceneUILayerManager.Instance.ChangeScene("Title");

            Debug.Log("SceneFirstLoad Awake -2");
        }
    }
}
