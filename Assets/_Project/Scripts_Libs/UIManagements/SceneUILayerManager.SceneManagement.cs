using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;

namespace CookApps.TeamBattle.UIManagements
{
    public sealed partial class SceneUILayerManager
    {
        #region Scene Load
        public class SceneLoadAsyncOperationWrapper
        {
            private AsyncOperationHandle<SceneInstance>? asyncOperation;
            public event Action Completed;

            internal void SetAsyncOperation(AsyncOperationHandle<SceneInstance> asyncOperation)
            {
                this.asyncOperation = asyncOperation;
                asyncOperation.Completed += CompleteCallback;
            }

            public float progress => asyncOperation?.PercentComplete ?? 0f;
            public bool allowSceneActivation = true;

            public bool IsDone => asyncOperation?.IsDone ?? false;

            private void CompleteCallback(AsyncOperationHandle<SceneInstance> operation)
            {
                operation.Completed -= CompleteCallback;
                Completed?.Invoke();
                Completed = null;
            }
        }

        private SceneInstance? currentSceneInstance;
        public void ForceSetCurrentSceneInstance(SceneInstance sceneInstance)
        {
            currentSceneInstance = sceneInstance;
        }

        /// <summary>
        /// 씬을 변경합니다. Lobby => Game, Game => Lobby 등 무거운 씬 간의 전환은 SceneLoading.GoToNextScene 을 사용하세요.
        /// </summary>
        /// <param name="sceneName">SceneData내의 sceneName</param>
        /// <param name="defaultUIData">씬에 기본으로 포함되어있는 UI에 전달할 정보</param>
        /// <param name="transition">전환 연출</param>
        /// <returns>씬 전환을 제어하고 싶은 경우 이 객체의 allowSceneActivation로 제어할 것</returns>
        public SceneLoadAsyncOperationWrapper ChangeScene(string sceneName, object defaultUIData = null, ISceneTransition transition = null)
        {
            var operationWrapper = new SceneLoadAsyncOperationWrapper();
            isSceneChanging = true;
            if (transition == null)
            {
                transition = new SceneTransition_Instant();
            }

            ChangeSceneAsync(sceneName, operationWrapper, defaultUIData, transition).AttachExternalCancellation(this.GetCancellationTokenOnDestroy());
            return operationWrapper;
        }

        private async UniTask ChangeSceneAsync(string sceneName, SceneLoadAsyncOperationWrapper operationWrapper, object defaultUIData, ISceneTransition transition)
        {
            // 1. 먼저 화면을 가림
            await transition.FadeInAsync();

            // 2. 풀에 있는 UI들을 제거
            ClearUIPool();

            // 3. 다음 씬에서 필요한 UI들을 미리 로드하고 풀에 넣음
            // 여기서 하는 이유는 씬 로드 중에는 addressable을 로드할 수 없기 때문
            SceneData sceneData = SceneDataList[sceneName];
            {
                var tasks = new UniTask<UILayer>[SceneDataList[sceneName].DefaultUILayers.Count];
                for (int i = 0; i < SceneDataList[sceneName].DefaultUILayers.Count; i++)
                {
                    var index = i;
                    tasks[i] = LoadUILayer(SceneDataList[sceneName].DefaultUILayers[index]);
                }

                var res = await UniTask.WhenAll(tasks);
                for (int i = 0; i < res.Length; i++)
                {
                    PoolingUILayer(res[i]);
                }
            }

            // 4. 씬 로드
            var asyncOperationHandle = Addressables.LoadSceneAsync(sceneData.AddressableName, activateOnLoad: false);
            operationWrapper.SetAsyncOperation(asyncOperationHandle);
            var nextSceneInstance = await asyncOperationHandle;
            await UniTask.WaitUntil(() => operationWrapper.allowSceneActivation);

            if (isLoadingUI)
            {
                noNeedToLoadUI = true;
            }

            // 5. 현재씬에 떠있는 UI들 정리
            for (var i = 0; i < uiLayerStacks.Count; i++)
            {
                uiLayerStacks[i].Layer.OnPreExit();
                uiLayerStacks[i].SetState(UILayerState.Exiting);
                OnUITransitionEvent?.Invoke(UILayerTransition.Exiting, uiLayerStacks[i].Key, uiLayerStacks[i].Layer);
                uiLayerStacks[i].Layer.OnPostExit();
                OnUITransitionEvent?.Invoke(UILayerTransition.ExitFinished, uiLayerStacks[i].Key, uiLayerStacks[i].Layer);
                Addressables.ReleaseInstance(uiLayerStacks[i].Layer.CachedGo);
            }

            uiLayerStacks.Clear();
            dimLayer = null;
            isDimLayerOn = false;

            // 6. 씬 로드 완료
            OnSceneUnloadedEvent?.Invoke(CurrentSceneName);
            if (currentSceneInstance.HasValue)
            {
                await Addressables.UnloadSceneAsync(currentSceneInstance.Value);
            }
            Resources.UnloadUnusedAssets();
            GC.Collect();

            // 7. 다음 씬 활성화
            await nextSceneInstance.ActivateAsync();
            currentSceneInstance = nextSceneInstance;
            OnSceneLoaded(sceneName, defaultUIData, transition);
        }

        private void OnSceneLoaded(string sceneName, object defaultUIData, ISceneTransition transition)
        {
            CurrentSceneName = sceneName;
            ResetNodeRefs();

            for (var i = 0; i < mainNode.childCount; i++)
            {
                var uiLayer = mainNode.GetChild(i).GetComponent<UILayer>();
                if (uiLayer == null)
                {
                    continue;
                }

                UILayerStackData stackData = MakeUIStackData(uiLayer, uiLayer.GetType().Name, null);
                PushUILayerInternal(stackData, defaultUIData);
            }

            for (var i = 0; i < SceneDataList[sceneName].DefaultUILayers.Count; i++)
            {
                var uiName = SceneDataList[sceneName].DefaultUILayers[i].Name;
                PushUILayerAsync(SceneDataList[sceneName].DefaultUILayers[i], uiName, defaultUIData).Forget();
            }

            transition.FadeOutAsync(true);

            isSceneChanging = false;
            OnSceneLoadedEvent?.Invoke(sceneName);
        }
        #endregion
    }
}
