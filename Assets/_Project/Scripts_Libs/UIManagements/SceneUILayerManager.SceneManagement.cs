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
            ClearUIPool();

            SceneData sceneData = SceneDataList[sceneName];
            await transition.FadeInAsync();

            AsyncOperationHandle<SceneInstance> asyncOperation = Addressables.LoadSceneAsync(sceneData.AddressableName, activateOnLoad: false);
            operationWrapper.SetAsyncOperation(asyncOperation);
            SceneInstance sceneInstance = await asyncOperation;
            await UniTask.WaitUntil(() => operationWrapper.allowSceneActivation);

            if (isLoadingUI)
            {
                noNeedToLoadUI = true;
            }

            for (var i = 0; i < uiLayerStacks.Count; i++)
            {
                uiLayerStacks[i].Layer.OnPreExit();
                uiLayerStacks[i].SetState(UILayerState.Exiting);
                OnUITransitionEvent?.Invoke(UILayerTransition.Exiting, uiLayerStacks[i].Key, uiLayerStacks[i].Layer);
                uiLayerStacks[i].Layer.OnPostExit();
                OnUITransitionEvent?.Invoke(UILayerTransition.ExitFinished, uiLayerStacks[i].Key, uiLayerStacks[i].Layer);
            }

            uiLayerStacks.Clear();
            dimLayer = null;
            isDimLayerOn = false;

            OnSceneUnloadedEvent?.Invoke(CurrentSceneName);
            Resources.UnloadUnusedAssets();
            GC.Collect();
            await sceneInstance.ActivateAsync();
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

            transition.FadeOutAsync(true);

            isSceneChanging = false;
            OnSceneLoadedEvent?.Invoke(sceneName);
        }
        #endregion
    }
}
