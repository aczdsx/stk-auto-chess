using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using CookApps.TeamBattle.Utility;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using Cysharp.Threading.Tasks;

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
            }

            public float progress => asyncOperation?.PercentComplete ?? 0f;
            internal bool IsOperationDone => asyncOperation?.IsDone ?? false;
            public bool allowSceneActivation = true;

            internal void SetComplete()
            {
                IsDone = true;
                Completed?.Invoke();
                Completed = null;
            }
            public bool IsDone { get; private set; }
            
            public SceneLoadAsyncOperationAwaiter GetAwaiter()
            {
                return new SceneLoadAsyncOperationAwaiter(this);
            }
        }
        
        public struct SceneLoadAsyncOperationAwaiter : INotifyCompletion
        {
            private SceneLoadAsyncOperationWrapper operation;
            public SceneLoadAsyncOperationAwaiter(SceneLoadAsyncOperationWrapper operation)
            {
                this.operation = operation;
            }

            public bool IsCompleted => operation.IsDone;

            public void OnCompleted(Action continuation)
            {
                operation.Completed += continuation;
            }

            public void GetResult()
            {
            }
        }

        private AsyncOperationHandle<SceneInstance> handle;
        /// <summary>
        /// 씬을 변경합니다. Lobby => Game, Game => Lobby 등 무거운 씬 간의 전환은 SceneLoading.GoToNextScene 을 사용하세요.
        /// </summary>
        /// <param name="sceneName">SceneData내의 sceneName</param>
        /// <param name="defaultUIData">씬에 기본으로 포함되어있는 UI에 전달할 정보</param>
        /// <param name="transition">전환 연출</param>
        /// <returns>씬 전환을 제어하고 싶은 경우 이 객체의 allowSceneActivation로 제어할 것</returns>
        public SceneLoadAsyncOperationWrapper ChangeScene(string sceneName, object defaultUIData = null)
        {
            var operationWrapper = new SceneLoadAsyncOperationWrapper();
            isSceneChanging = true;
            ChangeSceneAsync(sceneName, operationWrapper, defaultUIData).Forget();
            return operationWrapper;
        }

        private async UniTask ChangeSceneAsync(string sceneName, SceneLoadAsyncOperationWrapper operationWrapper, object defaultUIData)
        {
            // 2. 풀에 있는 UI들을 제거
            ClearUIPool();
            
            {
                // dimlayer 관련 변수 초기화
                dimLayerCreated = false;
                dimLayer = null;
                isDimLayerOn = false;
            }
            
            // 3. 현재씬에 떠있는 UI들 정리
            var copy = new List<UILayerStackData>(uiLayerStacks);
            for (var i = copy.Count - 1; i >= 0; i--)
            {
                if (copy[i].State == UILayerState.Exiting)
                    continue;

                // 이미 파괴된 UI는 스킵 (씬 전환 중 자동 파괴된 경우)
                if (copy[i].Layer == null || copy[i].Layer.CachedGo == null)
                    continue;

                copy[i].Layer.OnPreExit();
                copy[i].State = UILayerState.Exiting;
                copy[i].Layer.OnPostExit();
                Addressables.ReleaseInstance(copy[i].Layer.CachedGo);
                Destroy(copy[i].Layer.CachedGo);
            }

            uiLayerStacks.Clear();
            dimLayer = null;
            isDimLayerOn = false;

            // 4. 씬 로드
            // var prevHandle = handle;
            var address = UILayerConstants.GetSceneAddress(sceneName);
 
              handle = Addressables.LoadSceneAsync(address, activateOnLoad: false);
            operationWrapper.SetAsyncOperation(handle);
            var nextSceneInstance = await handle.WaitUntilDone();
            while (!operationWrapper.allowSceneActivation)
            {
                await Awaitable.NextFrameAsync();
            }

            // 5. 이전 씬 언로드
            OnSceneUnloadedEvent?.Invoke(CurrentSceneName);
            // if (prevHandle.IsValid())
            //     Addressables.UnloadSceneAsync(prevHandle);

            Resources.UnloadUnusedAssets();
            GC.Collect();

            // 6. 다음 씬 활성화
            var oper = nextSceneInstance.ActivateAsync();
            while (!oper.isDone)
            {
                await Awaitable.NextFrameAsync();
            }
            CurrentSceneName = sceneName;
            ResetNodeRefs();

            var defaultUILayerLoader = FindFirstObjectByType<DefaultUILayerLoader>();
            Debug.Log($"[SceneUILayerManager] ChangeSceneAsync: sceneName={sceneName}, defaultUIData type={defaultUIData?.GetType().Name ?? "null"}");
            await defaultUILayerLoader.LoadDefaultUILayers(defaultUIData);
            Destroy(defaultUILayerLoader.CachedGo);

            operationWrapper.SetComplete();

            isSceneChanging = false;
            OnSceneLoadedEvent?.Invoke(sceneName);
        }
        #endregion
    }
}
