using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Cysharp.Threading.Tasks;

namespace CookApps.TeamBattle.UIManagements
{
    public class SceneLoadingEventReceiver
    {
        public Func<UniTask> OnLoading { get; }
        public Action OnNextSceneLoaded { get; }

        public SceneLoadingEventReceiver(Func<UniTask> onLoading, Action onNextSceneLoaded)
        {
            OnLoading = onLoading;
            OnNextSceneLoaded = onNextSceneLoaded;
        }
    }

    public abstract class SceneLoading : UILayer
    {
        private class SceneLoadingData
        {
            public string CurrentSceneName;
            public string NextSceneName;
            public object NextSceneData;
            public SceneLoadingEventReceiver SceneLoadingEventReceiver;
            public string NaninovelScriptName; // 나니노벨 스크립트 이름 (있으면 나니노벨을 거쳐서 이동)
        }

        private SceneLoadingData sceneLoadingData;

        public delegate UniTask SceneLoadedAsyncTask(string prevScene, string nextScene, object defaultUIData);

        private static List<SceneLoadedAsyncTask> startChangeSceneAsyncTasks = new();

        public static event SceneLoadedAsyncTask OnStartChangeScene
        {
            add => startChangeSceneAsyncTasks.Add(value);
            remove => startChangeSceneAsyncTasks.Remove(value);
        }

        /// <summary>
        /// 씬 진입 시 나니노벨 트리거를 검색하는 델리게이트
        /// 외부에서 NaninovelTriggerManager 등을 연결
        /// </summary>
        public static Func<string, string> OnGetNaninovelTrigger { get; set; }

        /// <summary>
        /// SPECIAL 타입 나니노벨 트리거를 trigger_key로 검색하는 델리게이트
        /// 일회성 특수 트리거용 (PrologueStart, PrologueEnd 등)
        /// </summary>
        public static Func<string, string> OnGetSpecialNaninovelTrigger { get; set; }

        /// <summary>
        /// STAGE_CLEAR_NANI 타입 나니노벨 트리거를 스테이지 ID로 검색하는 델리게이트
        /// 스테이지 클리어 후 스토리 재생용
        /// </summary>
        public static Func<int, string> OnGetStageClearNaninovelTrigger { get; set; }

        /// <summary>
        /// STAGE_ENTER_NANI 타입 나니노벨 트리거를 스테이지 ID로 검색하는 델리게이트
        /// 스테이지 진입 전 스토리 재생용
        /// </summary>
        public static Func<int, string> OnGetStageEnterNaninovelTrigger { get; set; }

        /// <summary>
        /// 무거운 씬간 전환시 사용, 전환 중간에 가벼운 씬을 둠으로써 무거운 씬2개가 동시에 떠서 메모리가 부족해지는 것을 방지
        /// </summary>
        /// <param name="nextScene">목적지 씬 이름</param>
        /// <param name="nextSceneData">목적지 씬 데이터</param>
        /// <param name="sceneLoadingEventReceiver">씬 로딩 이벤트 리시버</param>
        public static void GoToNextScene(string nextScene, object nextSceneData = null, SceneLoadingEventReceiver sceneLoadingEventReceiver = null)
        {
            // 트리거 델리게이트가 설정되어 있으면 나니노벨 트리거 검색
            var naninovelScriptName = OnGetNaninovelTrigger?.Invoke(nextScene);
            GoToNextSceneInternal(nextScene, nextSceneData, sceneLoadingEventReceiver, naninovelScriptName);
        }

        /// <summary>
        /// SPECIAL 타입 나니노벨 트리거를 지정하여 씬 전환
        /// 일회성 특수 트리거용 (PrologueStart, PrologueEnd 등)
        /// </summary>
        /// <param name="nextScene">목적지 씬 이름</param>
        /// <param name="specialTriggerKey">SPECIAL 타입 나니노벨 trigger_key</param>
        /// <param name="nextSceneData">목적지 씬 데이터</param>
        /// <param name="sceneLoadingEventReceiver">씬 로딩 이벤트 리시버</param>
        public static void GoToNextSceneWithSpecialTrigger(string nextScene, string specialTriggerKey, object nextSceneData = null, SceneLoadingEventReceiver sceneLoadingEventReceiver = null)
        {
            // SPECIAL 트리거 검색
            var naninovelScriptName = OnGetSpecialNaninovelTrigger?.Invoke(specialTriggerKey);
            GoToNextSceneInternal(nextScene, nextSceneData, sceneLoadingEventReceiver, naninovelScriptName);
        }

        /// <summary>
        /// STAGE_CLEAR_NANI 타입 나니노벨 트리거를 지정하여 씬 전환
        /// 스테이지 클리어 후 스토리 재생용
        /// </summary>
        /// <param name="nextScene">목적지 씬 이름</param>
        /// <param name="stageId">클리어한 스테이지 ID</param>
        /// <param name="nextSceneData">목적지 씬 데이터</param>
        /// <param name="sceneLoadingEventReceiver">씬 로딩 이벤트 리시버</param>
        public static void GoToNextSceneWithStageClearTrigger(string nextScene, int stageId, object nextSceneData = null, SceneLoadingEventReceiver sceneLoadingEventReceiver = null)
        {
            // STAGE_CLEAR_NANI 트리거 검색
            Debug.Log($"[SceneLoading] GoToNextSceneWithStageClearTrigger: {stageId}");
            var naninovelScriptName = OnGetStageClearNaninovelTrigger?.Invoke(stageId);
            GoToNextSceneInternal(nextScene, nextSceneData, sceneLoadingEventReceiver, naninovelScriptName);
        }

        /// <summary>
        /// STAGE_ENTER_NANI 타입 나니노벨 트리거를 지정하여 씬 전환
        /// 스테이지 진입 전 스토리 재생용
        /// </summary>
        /// <param name="nextScene">목적지 씬 이름</param>
        /// <param name="stageId">진입할 스테이지 ID</param>
        /// <param name="nextSceneData">목적지 씬 데이터</param>
        /// <param name="sceneLoadingEventReceiver">씬 로딩 이벤트 리시버</param>
        public static void GoToNextSceneWithStageEnterTrigger(string nextScene, int stageId, object nextSceneData = null, SceneLoadingEventReceiver sceneLoadingEventReceiver = null)
        {
            // STAGE_ENTER_NANI 트리거 검색
            Debug.Log($"[SceneLoading] GoToNextSceneWithStageEnterTrigger: {stageId}");
            var naninovelScriptName = OnGetStageEnterNaninovelTrigger?.Invoke(stageId);
            GoToNextSceneInternal(nextScene, nextSceneData, sceneLoadingEventReceiver, naninovelScriptName);
        }

        private static void GoToNextSceneInternal(string nextScene, object nextSceneData, SceneLoadingEventReceiver sceneLoadingEventReceiver, string naninovelScriptName)
        {
            // 나니노벨이 있으면 나니노벨 씬으로 먼저 이동
            if (!string.IsNullOrEmpty(naninovelScriptName))
            {
                GoToNextSceneViaNaninovel(nextScene, nextSceneData, sceneLoadingEventReceiver, naninovelScriptName);
                return;
            }

            // transition 연출 진행중 다른 씬으로 넘어가는 것을 방지하기 위해
            SceneUILayerManager.Instance.isSceneChanging = true;
            var data = new SceneLoadingData
            {
                CurrentSceneName = SceneUILayerManager.Instance.CurrentSceneName,
                NextSceneName = nextScene,
                NextSceneData = nextSceneData,
                SceneLoadingEventReceiver = sceneLoadingEventReceiver,
                NaninovelScriptName = null
            };
            SceneUILayerManager.Instance.ChangeScene("SceneLoading", data);
        }

        /// <summary>
        /// 나니노벨을 거쳐서 목적지 씬으로 이동
        /// </summary>
        private static void GoToNextSceneViaNaninovel(string nextScene, object nextSceneData, SceneLoadingEventReceiver sceneLoadingEventReceiver, string naninovelScriptName)
        {
            // 나니노벨 종료 시 원래 목적지로 이동하는 액션 생성
            System.Action onNaninovelEnd = () =>
            {
                // 나니노벨이 끝나면 원래 목적지로 이동
                SceneUILayerManager.Instance.isSceneChanging = true;
                var data = new SceneLoadingData
                {
                    CurrentSceneName = "Naninovel",
                    NextSceneName = nextScene,
                    NextSceneData = nextSceneData,
                    SceneLoadingEventReceiver = sceneLoadingEventReceiver,
                    NaninovelScriptName = null
                };
                SceneUILayerManager.Instance.ChangeScene("SceneLoading", data);
            };

            // 나니노벨 씬으로 이동
            SceneUILayerManager.Instance.isSceneChanging = true;
            var naninovelData = new SceneLoadingData
            {
                CurrentSceneName = SceneUILayerManager.Instance.CurrentSceneName,
                NextSceneName = "Naninovel",
                NextSceneData = (naninovelScriptName, onNaninovelEnd), // 튜플로 스크립트 이름과 종료 액션 전달
                SceneLoadingEventReceiver = null,
                NaninovelScriptName = naninovelScriptName
            };
            SceneUILayerManager.Instance.ChangeScene("SceneLoading", naninovelData);
        }

        protected internal override void OnPreEnter(object param)
        {
            sceneLoadingData = param as SceneLoadingData;
            if (sceneLoadingData == null)
            {
                Debug.LogError($"[SceneLoading] OnPreEnter: param이 SceneLoadingData가 아닙니다. param type: {param?.GetType().Name ?? "null"}, param value: {param}");
            }
            EnterAsync().Forget();
        }

        protected virtual async UniTask EnterAsync()
        {
            await UniTask.Yield();

            if (sceneLoadingData == null)
            {
                Debug.LogError("[SceneLoading] EnterAsync: sceneLoadingData가 null입니다!");
                return;
            }

            Debug.Log($"[SceneLoading] EnterAsync: NextSceneName={sceneLoadingData.NextSceneName}, NextSceneData type={sceneLoadingData.NextSceneData?.GetType().Name ?? "null"}");

            await UniTask.WhenAll(startChangeSceneAsyncTasks.Select(x => x.Invoke(sceneLoadingData.CurrentSceneName, sceneLoadingData.NextSceneName, sceneLoadingData.NextSceneData)));

            if (sceneLoadingData.SceneLoadingEventReceiver != null)
            {
                if (sceneLoadingData.SceneLoadingEventReceiver.OnLoading != null)
                {
                    await sceneLoadingData.SceneLoadingEventReceiver.OnLoading();
                }
            }

            Resources.UnloadUnusedAssets();

            Debug.Log($"[SceneLoading] ChangeScene 호출: NextSceneName={sceneLoadingData.NextSceneName}, NextSceneData={sceneLoadingData.NextSceneData}");
            var wrapper = SceneUILayerManager.Instance.ChangeScene(sceneLoadingData.NextSceneName, sceneLoadingData.NextSceneData);
            await UniTask.WaitUntil(() => wrapper.IsDone);

            if (sceneLoadingData.SceneLoadingEventReceiver != null)
            {
                sceneLoadingData.SceneLoadingEventReceiver.OnNextSceneLoaded?.Invoke();
            }
            ClearData();
        }

        protected virtual void ClearData()
        {
            sceneLoadingData = null;
        }
    }
}
