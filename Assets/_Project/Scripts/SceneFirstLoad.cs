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
        public SceneUIDataSource(SceneDatabase sceneDatabase, UIDatabase uiDatabase)
        {
            sceneDatas = new Dictionary<string, SceneUIManager.SceneData>();

            foreach (SceneUIManager.SceneData e in sceneDatabase.List)
            {
                sceneDatas.Add(e.sceneName, e);
            }

            uiDatas = new Dictionary<string, SceneUIManager.UIData>();

            foreach (SceneUIManager.UIData e in uiDatabase.List)
            {
                uiDatas.Add(e.uiName, e);
            }
        }

        private Dictionary<string, SceneUIManager.SceneData> sceneDatas;
        public Dictionary<string, SceneUIManager.SceneData> SceneDataList => sceneDatas;

        public Dictionary<string, SceneUIManager.UIData> uiDatas;
        public Dictionary<string, SceneUIManager.UIData> UIDataList => uiDatas;
    }

    public class SceneFirstLoad : MonoBehaviour
    {
        private SceneDatabase SceneData;
        private UIDatabase UIData;

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
            var UIData = await AddressableLoadHelper.LoadAssetAsync<UIDatabase>("Data/UIData.asset");
            var dataSource = new SceneUIDataSource(SceneData, UIData);
            SceneUIManager.Instance.Initialize(dataSource);

#if UNITY_EDITOR
            NativeLeakDetection.Mode = NativeLeakDetectionMode.EnabledWithStackTrace;
#endif
            var transaction = SceneTransaction_FadeInOut.Create();
            await SceneLoading.GoToNextScene("Title", null, transaction);
        }
    }
}
