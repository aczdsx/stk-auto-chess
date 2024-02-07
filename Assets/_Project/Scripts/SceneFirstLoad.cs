using System.Collections.Generic;
using CookApps.SampleTeamBattle.Utilities;
using CookApps.TeamBattle;
using CookApps.TeamBattle.UIManagements;
using Cysharp.Threading.Tasks;
using Unity.Collections;
using UnityEngine;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.AddressableAssets;

namespace CookApps.SampleTeamBattle
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

    public class SceneUIDataSource : ISceneUIDataSource
    {
        public SceneUIDataSource(SceneDatabase sceneDatabase, UILayerDatabase uiLayerDatabase)
        {
            sceneDatas = new Dictionary<string, SceneUILayerManager.SceneData>();

            foreach (SceneUILayerManager.SceneData e in sceneDatabase.List)
            {
                sceneDatas.Add(e.sceneName, e);
            }

            uiDatas = new Dictionary<string, SceneUILayerManager.UILayerData>();

            foreach (SceneUILayerManager.UILayerData e in uiLayerDatabase.List)
            {
                uiDatas.Add(e.name, e);
            }
        }

        private Dictionary<string, SceneUILayerManager.SceneData> sceneDatas;
        public Dictionary<string, SceneUILayerManager.SceneData> SceneDataList => sceneDatas;

        public Dictionary<string, SceneUILayerManager.UILayerData> uiDatas;
        public Dictionary<string, SceneUILayerManager.UILayerData> UIDataList => uiDatas;
    }

    public class SceneFirstLoad : MonoBehaviour
    {
        private void Awake()
        {
            StartUp().Forget();
        }

        private async UniTask StartUp()
        {
            await UniTask.Yield();

            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            AppLifeCycleEventsDispatcher _ = AppLifeCycleEventsDispatcher.Instance;
            var SceneData = await AddressableLoadHelper.LoadAssetAsync<SceneDatabase>("Data/SceneData.asset");
            var UIData = await AddressableLoadHelper.LoadAssetAsync<UILayerDatabase>("Data/UIData.asset");
            var dataSource = new SceneUIDataSource(SceneData, UIData);
            SceneUILayerManager.Instance.Initialize(dataSource);

#if UNITY_EDITOR
            NativeLeakDetection.Mode = NativeLeakDetectionMode.EnabledWithStackTrace;
#endif
            var transition = SceneTransition_FadeInOut.Create();
            await SceneLoading.GoToNextScene("Title", null, transition);
        }
    }
}
