using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace CookApps.TeamBattle.UIManagements
{
    public class SceneUIManager : SingletonMonoBehaviour<SceneUIManager>, ISelectableBlocker
    {
        public enum PushReqCode
        {
            OK,
            DuplicatedKey,
            NotExistUIName,
            Loading,
        }

        public enum PopReqCode
        {
            OK,
            NotExist,
        }

        private enum UIState
        {
            Initialized,
            Entering,
            Entered,
            Exiting,
        }

        public enum UITransition
        {
            Entering,
            EnterFinished,
            Exiting,
            ExitFinished,
        }

        public enum UIType
        {
            None = 0,
            Cover,
            Overlay,
            Popup,
        }

        [Serializable]
        private class UIStackData
        {
            public UIStackData(string uiName, string key, long inc, UILayer ui, UIState state, Action<object> closeCallback)
            {
                this.uiName = uiName;
                this.key = key;
                this.ui = ui;
                this.ui.Key = key;
                this.state = state;
                this.closeCallback = closeCallback;
                this.inc = inc;
            }

            public readonly long inc;
            public string uiName;
            public readonly string key;
            public UILayer ui;
            public UIState state;
            public Action<object> closeCallback;

            public static bool operator ==(UIStackData a, UIStackData b)
            {
                if (a is null || b is null)
                {
                    return false;
                }

                return a.key == b.key;
            }

            public static bool operator !=(UIStackData a, UIStackData b)
            {
                return !(a == b);
            }

            public override int GetHashCode()
            {
                return key.GetHashCode();
            }

            public override bool Equals(object obj)
            {
                return key == (obj as UIStackData)?.key;
            }
        }

        [Serializable]
        public class UIData
        {
            public string uiName;
            public UIType uiType;
            public string assetName;
            public string[] preloads;
            public string bgmKey;
        }

        [Serializable]
        public class SceneData
        {
            public string sceneName;
            public string[] defaultUINames;
            public string[] preloads;
        }

        public string[] GetPreloads(string sceneName)
        {
            if (_dataSource.SceneDataList.TryGetValue(sceneName, out SceneData sceneData))
            {
                return sceneData.preloads;
            }

            return null;
        }

        public string[] GetDefaultUINames(string sceneName)
        {
            if (_dataSource.SceneDataList.TryGetValue(sceneName, out SceneData sceneData))
            {
                return sceneData.defaultUINames;
            }

            return null;
        }

        private ISceneUIDataSource _dataSource;

        // lobby ui pool
        private Dictionary<string, Queue<GameObject>> lobbyUIPool = new ();

        private List<UIStackData> uiStacks = new ();

        private bool isLoadingUI;

        private bool noNeedToLoadUI;
        // private WaitingUIData currLoadingUIData = null;
        // private List<WaitingUIData> waitingUIDataList = new ();

        public string CurrentSceneName { get; set; }
        private Transform mainNode = null;
        private Canvas mainNodeCanvas;
        public Transform MainNode => mainNode;

        private Transform recycles = null;
        private Image dimLayer = null;
        private bool isDimLayerOn;
        private Canvas mainCanvas;
        public Canvas MainCanvas => mainCanvas;

        private Transform floatingNode;
        private Canvas floatingNodeCanvas;
        public Transform FloatingNode => floatingNode;

        public static event Action<UITransition, string, UILayer> OnUITransitionEvent;
        public static event Action<string> OnSceneUnloadedEvent;
        public static event Action<string> OnSceneLoadedEvent;

        private static List<Func<string, string, object, UniTask>> sceneLoadedAsyncTasks;

        public static void AddSceneLoadedAsyncTask(Func<string, string, object, UniTask> task)
        {
            if (sceneLoadedAsyncTasks == null)
            {
                sceneLoadedAsyncTasks = new List<Func<string, string, object, UniTask>>();
            }

            sceneLoadedAsyncTasks.Add(task);
        }

        public static void RemoveSceneLoadedAsyncTask(Func<string, string, object, UniTask> task)
        {
            if (sceneLoadedAsyncTasks == null)
            {
                return;
            }

            sceneLoadedAsyncTasks.Remove(task);
        }

        public int CurrentUICount => /*waitingUIDataList.Count + */uiStacks.Count;
        private List<string> blockBackKeySources = new ();

        public void BlockBackKey(string srcKey)
        {
            blockBackKeySources.Add(srcKey);
        }

        public void ReleaseBackKey(string srcKey)
        {
            blockBackKeySources.Remove(srcKey);
        }

        public bool isSceneChanging;

        public long uiIncAcc = 0;

        public void Initialize(ISceneUIDataSource dataSource)
        {
            var recycleGo = new GameObject("recycledUIs");
            var recycleCanvas = recycleGo.AddComponent<Canvas>();
            var recycleCanvasScaler = recycleGo.AddComponent<CanvasScaler>();
            recycleCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            recycleCanvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            recycleCanvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            recycles = recycleGo.GetComponent<Transform>();
            recycles.SetParent(transform, false);
            recycleCanvas.enabled = false;
            dimLayer = null;
            isDimLayerOn = false;

            // 첫번째 씬에서 필요
            ResetNodeRefs();

            _dataSource = dataSource;
            SelectableBlockerManager.Instance.AddBlocker(this);
        }

        private void ResetNodeRefs()
        {
            CameraManager.Instance.ReleaseMainCamera();
            mainCanvas = null;
            Scene activeScene = SceneManager.GetActiveScene();
            GameObject[] rootGOs = SceneManager.GetActiveScene().GetRootGameObjects();
            bool isLoadingScene = activeScene.name == "SceneLoading";
            var hasLoadingComp = false;
            for (var i = 0; i < rootGOs.Length; i++)
            {
                if (rootGOs[i].name == "MainCanvas")
                {
                    mainCanvas = rootGOs[i].GetComponent<Canvas>();
                    if (CameraManager.Main != null)
                    {
                        mainCanvas.worldCamera = CameraManager.Main;
                    }

                    var canvas = rootGOs[i].GetComponent<Transform>();
                    mainNode = canvas.Find("MainNode");
                    mainNodeCanvas = mainNode.GetComponent<Canvas>();
                    if (mainNodeCanvas == null)
                    {
                        mainNodeCanvas = mainNode.gameObject.AddComponent<Canvas>();
                    }

                    var mainNodeGR = mainNode.GetComponent<GraphicRaycaster>();
                    if (mainNodeGR == null)
                    {
                        mainNode.gameObject.AddComponent<GraphicRaycaster>();
                    }

                    floatingNode = canvas.Find("FloatingNode");
                    if (floatingNodeCanvas == null)
                    {
                        floatingNodeCanvas = floatingNode.gameObject.AddComponent<Canvas>();
                    }

                    var floatingNodeGR = floatingNode.GetComponent<GraphicRaycaster>();
                    if (floatingNodeGR == null)
                    {
                        floatingNode.gameObject.AddComponent<GraphicRaycaster>();
                    }

                    var mainCanvasScaler = mainCanvas.GetComponent<CanvasScaler>();
                    var recycleCanvasScaler = recycles.GetComponent<CanvasScaler>();
                    recycleCanvasScaler.referenceResolution = mainCanvasScaler.referenceResolution;
                    continue;
                }

                if (rootGOs[i].name == "EventSystem")
                {
                    rootGOs[i].GetComponent<EventSystem>().pixelDragThreshold = UIManagementsConst.DragThreshold;
                    continue;
                }

                if (CameraManager.Main == null)
                {
                    var camera = rootGOs[i].GetComponent<Camera>();
                    if (camera != null && camera.name.Contains("Main"))
                    {
                        CameraManager.Instance.SetMainCamera(camera);
                        if (mainCanvas != null)
                        {
                            mainCanvas.worldCamera = camera;
                        }
                    }
                }

                if (isLoadingScene && rootGOs[i].GetComponent<SceneLoading>() != null)
                {
                    hasLoadingComp = true;
                }
            }

            if (isLoadingScene && !hasLoadingComp)
            {
                var loadingGO = new GameObject("SceneLoading");
                loadingGO.AddComponent<SceneLoading>();
            }
        }

        private void Update()
        {
            if (Input.GetKeyUp(KeyCode.Escape))
            {
                OnEventBackKey();
            }
        }

        private void LateUpdate()
        {
            if (dimLayer != null)
            {
                float targetOpacity = isDimLayerOn ? 0.82f : 0f;
                float opacity = dimLayer.color.a;

                if (opacity > 0 && !dimLayer.gameObject.activeSelf)
                {
                    dimLayer.gameObject.SetActive(true);
                }

                if (opacity <= 0 && dimLayer.gameObject.activeSelf)
                {
                    dimLayer.gameObject.SetActive(false);
                }

                if (isDimLayerOn ? targetOpacity > opacity : targetOpacity < opacity)
                {
                    bool sign = Mathf.Sign(targetOpacity - opacity) > 0;
                    Color color = dimLayer.color;
                    opacity += (sign ? 1 : -1) * 5f * Time.unscaledDeltaTime;
                    color.a = opacity;
                    bool resSign = Mathf.Sign(targetOpacity - opacity) > 0;
                    if (resSign != sign)
                    {
                        color.a = targetOpacity;
                        dimLayer.color = color;
                    }
                    else
                    {
                        dimLayer.color = color;
                    }
                }
            }

            // if (currLoadingUIData != null)
            // {
            //     return;
            // }
            //
            // if (waitingUIDataList.Count == 0)
            // {
            //     return;
            // }
            //
            // currLoadingUIData = waitingUIDataList[0];
            // waitingUIDataList.RemoveAt(0);
            // StartLoadingUI().Forget();
        }

        // private async UniTaskVoid StartLoadingUI()
        // {
        //     try
        //     {
        //         UIBase uiBase;
        //         string uiName = currLoadingUIData.uiName;
        //         string uiKey = currLoadingUIData.key;
        //         Action<object> closeCallback = currLoadingUIData.closeCallback;
        //
        //         // LoadingIndicator.ShowIndicator();
        //         if (lobbyUIPool.ContainsKey(uiName) && lobbyUIPool[uiName].Count > 0)
        //         {
        //             GameObject ui = lobbyUIPool[uiName].Dequeue();
        //             uiBase = ui.GetComponent<UIBase>();
        //             if (lobbyUIPool[uiName].Count == 0)
        //             {
        //                 lobbyUIPool.Remove(uiName);
        //             }
        //         }
        //         else
        //         {
        //             uiBase = await LoadUI(uiName);
        //         }
        //
        //         if (currLoadingUIData.noNeedOpen)
        //         {
        //             AddressableInstantiateHelper.ReleaseGameObject(uiBase.CachedGo);
        //             Destroy(uiBase.CachedGo);
        //             return;
        //         }
        //
        //         UIStackData uiData = MakeUIStackData(uiBase, uiKey, uiName, closeCallback);
        //         PushUI(uiData, currLoadingUIData.data);
        //         currLoadingUIData = null;
        //     }
        //     catch (Exception ex) when (!(ex is OperationCanceledException))
        //     {
        //         Debug.LogException(ex);
        //     }
        // }

        public void ClearUIPool()
        {
            foreach (KeyValuePair<string, Queue<GameObject>> pair in lobbyUIPool)
            {
                while (pair.Value.Count > 0)
                {
                    GameObject go = pair.Value.Dequeue();
                    AddressableInstantiateHelper.ReleaseGameObject(go);
                    Destroy(go);
                }
            }

            lobbyUIPool.Clear();
        }

        public string[] GetAllLoadedUIKeys()
        {
            return uiStacks.Select(x => x.key).ToArray();
        }

        /// <summary>
        /// uiBase의 값들을 바꾸진 말자
        /// </summary>
        public UILayer[] GetUIRoutes(bool isContainCover = true, bool isContainOverlay = false)
        {
            return uiStacks.Where(x =>
            {
                if (x.ui.UIType == UIType.Popup)
                {
                    return true;
                }

                if (isContainCover && x.ui.UIType == UIType.Cover)
                {
                    return true;
                }

                if (isContainOverlay && x.ui.UIType == UIType.Overlay)
                {
                    return true;
                }

                return false;
            }).Select(x => x.ui).ToArray();
        }

        #region Push or Pop UI
        public async UniTask<(PushReqCode, object)> PushUILayerAsync(string uiName, object data = null)
        {
            (PushReqCode, object) ret = (PushReqCode.OK, null);
            var isClosed = false;
            Action<object> closeCallback = res =>
            {
                ret = (ret.Item1, res);
                isClosed = true;
            };
            PushReqCode reqCode = PushUILayerWithKey(uiName, uiName, data, closeCallback);
            if (reqCode != PushReqCode.OK)
            {
                ret.Item1 = reqCode;
                return ret;
            }

            await UniTask.WaitUntil(() => isClosed);
            return ret;
        }

        public PushReqCode PushUILayer(string uiName, object data = null, Action<object> closeCallback = null)
        {
            return PushUILayerWithKey(uiName, uiName, data, closeCallback);
        }

        public PushReqCode PushUILayerWithKey(string uiName, string key, object data = null, Action<object> closeCallback = null)
        {
            bool isExistUIStack = uiStacks.Exists(x =>
                x.state != UIState.Exiting && x.key.Equals(key));

            // bool isExistWaiting = waitingUIDataList.Exists(x => x.key == key);
            if (isExistUIStack /* || isExistWaiting*/)
            {
                Debug.LogAssertion($"{uiName}::{key} is already exist!!");
                return PushReqCode.DuplicatedKey;
            }

            if (!_dataSource.UIDataList.ContainsKey(uiName))
            {
                Debug.LogAssertion($"{uiName} is not exist UI name!");
                return PushReqCode.NotExistUIName;
            }

            if (lobbyUIPool.ContainsKey(uiName) && lobbyUIPool[uiName].Count > 0)
            {
                GameObject ui = lobbyUIPool[uiName].Dequeue();
                if (lobbyUIPool[uiName].Count == 0)
                {
                    lobbyUIPool.Remove(uiName);
                }

                var uiBase = ui.GetComponent<UILayer>();
                UIStackData uiData = MakeUIStackData(uiBase, key, uiName, closeCallback);
                PushUI(uiData, data);
                return PushReqCode.OK;
            }

            isLoadingUI = true;
            LoadUILayer(uiName).ContinueWith(ui =>
            {
                isLoadingUI = false;
                if (noNeedToLoadUI)
                {
                    AddressableInstantiateHelper.ReleaseGameObject(ui.CachedGo);
                    Destroy(ui.CachedGo);
                    return;
                }

                UIStackData uiData = MakeUIStackData(ui, key, uiName, closeCallback);
                PushUI(uiData, data);
            }).Forget();
            return PushReqCode.OK;
        }

        private UIStackData MakeUIStackData(UILayer uiLayer, string key, string uiName, Action<object> closeCallback)
        {
            uiLayer.CachedGo.SetActive(false);
            uiLayer.CachedRectTr.SetParent(mainNode, false);
            uiLayer.name = key;
            uiLayer.UIType = _dataSource.UIDataList[uiName].uiType;
            long inc = uiIncAcc + (uiLayer.Priority * 100);
            uiIncAcc++;
            return new UIStackData(uiName, key, inc, uiLayer, UIState.Initialized, closeCallback);
        }

        private void PushUI(UIStackData uiData, object data)
        {
            if (_dataSource.UIDataList[uiData.uiName].bgmKey != null)
            {
                // SoundManager.Instance.PlayMusic(_dataSource.UIDataList[uiData.uiName].bgmKey);
            }

            uiData.ui.CachedGo.SetActive(true);
            uiData.ui.OnPreEnter(data);
            uiData.state = UIState.Entering;
            OnUITransitionEvent?.Invoke(UITransition.Entering, uiData.key, uiData.ui);
            uiStacks.Add(uiData);
            uiStacks.Sort((x, y) => (int) (x.inc - y.inc));

            // managing z order
            isDimLayerOn = false;
            for (int i = uiStacks.Count - 1; i >= 0; i--)
            {
                RectTransform uiTr = uiStacks[i].ui.CachedRectTr;

                uiTr.SetAsFirstSibling();
                if (uiStacks[i].state != UIState.Exiting && uiStacks[i].ui.UIType == UIType.Popup && !isDimLayerOn)
                {
                    if (dimLayer == null)
                    {
                        dimLayer = CreateDimLayer("DimLayer");
                    }

                    dimLayer.transform.SetAsFirstSibling();
                    isDimLayerOn = true;
                }
            }

            // if (dimLayer != null)
            //     dimLayer.gameObject.SetActive(isDimLayerOn);

            // preload addressables
            if (_dataSource.UIDataList[uiData.uiName].preloads != null)
            {
                for (var i = 0; i < _dataSource.UIDataList[uiData.uiName].preloads.Length; i++)
                {
                    AddressableLoadHelper.LoadAssetAsync<GameObject>(_dataSource.UIDataList[uiData.uiName].preloads[i]).Forget();
                }
            }

            Canvas.ForceUpdateCanvases();
            uiData.ui.StartEnterAnimation(OnEndEnterAnimation);
        }

        private void OnEndEnterAnimation(UILayer ui)
        {
            bool isExist = uiStacks.Exists(x => x.ui == ui);
            if (!isExist)
            {
                return;
            }

            UIStackData uiData = uiStacks.Find(x => x.ui == ui);

            uiData.state = UIState.Entered;
            ui.OnPostEnter();
            OnUITransitionEvent?.Invoke(UITransition.EnterFinished, uiData.key, uiData.ui);

            if (ui.UIType != UIType.Cover)
            {
                return;
            }

            var isFoundPrevCover = false;
            for (var i = 0; i < uiStacks.Count; i++)
            {
                if (uiStacks[i].ui == ui)
                {
                    break;
                }

                if (!isFoundPrevCover)
                {
                    isFoundPrevCover = uiStacks[i].ui.UIType == UIType.Cover;
                }

                if (isFoundPrevCover)
                {
                    uiStacks[i].ui.CachedGo.SetActive(false);
                }
            }
        }

        public PopReqCode PopUILayer(string key, object dataToCloseCallback = null)
        {
            bool isExist = uiStacks.Exists(x => x.key.Equals(key));
            if (!isExist)
            {
                return PopReqCode.NotExist;
            }

            UIStackData uiData = uiStacks.Find(x => x.key.Equals(key));
            if (uiData.state == UIState.Exiting)
            {
                return PopReqCode.NotExist;
            }

            PopUI(uiData, dataToCloseCallback);
            return PopReqCode.OK;
        }

        public PopReqCode PopUILayer(UILayer ui, object dataToCloseCallback = null)
        {
            bool isExist = uiStacks.Exists(x => x.ui.Equals(ui));
            if (!isExist)
            {
                return PopReqCode.NotExist;
            }

            UIStackData uiData = uiStacks.Find(x => x.ui.Equals(ui));
            if (uiData.state == UIState.Exiting)
            {
                return PopReqCode.NotExist;
            }

            PopUI(uiData, dataToCloseCallback);
            return PopReqCode.OK;
        }

        public PopReqCode PopTopUILayer(object dataToCloseCallback = null)
        {
            if (uiStacks.Count <= 1)
            {
                return PopReqCode.NotExist;
            }

            PopUI(uiStacks[^1], dataToCloseCallback);
            return PopReqCode.OK;
        }

        public void PopAllUI()
        {
            for (int i = uiStacks.Count - 1; i >= 0; i--)
            {
                if (uiStacks[i].state == UIState.Initialized ||
                    uiStacks[i].state == UIState.Entering ||
                    uiStacks[i].state == UIState.Entered)
                {
                    uiStacks[i].ui.OnPreExit();
                }

                OnUITransitionEvent?.Invoke(UITransition.Exiting, uiStacks[i].key, uiStacks[i].ui);
                uiStacks[i].state = UIState.Exiting;
                uiStacks[i].ui.OnPostExit();
                OnUITransitionEvent?.Invoke(UITransition.ExitFinished, uiStacks[i].key, uiStacks[i].ui);
                PoolingUI(uiStacks[i].uiName, uiStacks[i].ui);
                uiStacks[i] = null;
            }

            uiStacks.Clear();
        }

        private enum CoverState
        {
            NoNeedToCheck,
            Check,
        }

        private void PopUI(UIStackData uiData, object dataToCloseCallback)
        {
            // z order 정렬
            uiData.ui.OnPreExit();
            OnUITransitionEvent?.Invoke(UITransition.Exiting, uiData.key, uiData.ui);
            uiData.state = UIState.Exiting;
            uiStacks.Remove(uiData);

            uiData.ui.StartExitAnimation(ui =>
            {
                // if (dimLayer != null)
                //     dimLayer.gameObject.SetActive(isDimLayerOn);

                ui.OnPostExit();
                PoolingUI(uiData.uiName, ui);

                uiData.closeCallback?.Invoke(dataToCloseCallback);
                OnUITransitionEvent?.Invoke(UITransition.ExitFinished, uiData.key, uiData.ui);
            });

            CoverState coverState = uiData.ui.UIType == UIType.Cover ? CoverState.Check : CoverState.NoNeedToCheck;
            isDimLayerOn = false;
            bool needChangeBgm = _dataSource.UIDataList[uiData.uiName].bgmKey != null;
            for (int i = uiStacks.Count - 1; i >= 0; i--)
            {
                if (coverState == CoverState.Check)
                {
                    uiStacks[i].ui.CachedGo.SetActive(true);
                    if (uiStacks[i].ui.UIType == UIType.Cover)
                    {
                        coverState = CoverState.NoNeedToCheck;
                    }
                }

                RectTransform uiTr = uiStacks[i].ui.CachedRectTr;

                uiTr.SetAsFirstSibling();

                if (uiStacks[i].state != UIState.Exiting && uiStacks[i].ui.UIType == UIType.Popup && !isDimLayerOn)
                {
                    dimLayer.rectTransform.SetAsFirstSibling();
                    isDimLayerOn = true;
                }

                if (needChangeBgm && uiStacks[i] != uiData && _dataSource.UIDataList[uiStacks[i].uiName].bgmKey != null)
                {
                    needChangeBgm = false;
                    // SoundManager.Instance.PlayMusic(uiDatas[uiStacks[i].uiName].bgmKey);
                }
            }
        }

        private void PoolingUI(string uiName, UILayer ui)
        {
            ui.CachedGo.SetActive(false);
            ui.CachedRectTr.SetParent(recycles, false);
            if (!lobbyUIPool.ContainsKey(uiName))
            {
                lobbyUIPool.Add(uiName, new Queue<GameObject>());
            }

            lobbyUIPool[uiName].Enqueue(ui.CachedGo);
        }

        public UILayer GetUIBase(string uiKey)
        {
            for (var i = 0; i < uiStacks.Count; i++)
            {
                if (uiStacks[i].key == uiKey)
                {
                    return uiStacks[i].ui;
                }
            }

            return null;
        }
        #endregion

        #region Control Canvas
        public void ActivateMainNodeCanvas()
        {
            mainNodeCanvas.enabled = true;
        }

        public void DeactivateMainNodeCanvas()
        {
            mainNodeCanvas.enabled = false;
        }

        public void ActivateFloatingNodeCanvas()
        {
            floatingNodeCanvas.enabled = true;
        }

        public void DeactivateFloatingNodeCanvas()
        {
            floatingNodeCanvas.enabled = false;
        }
        #endregion

        #region Load UI from addressables
        private async UniTask<UILayer> LoadUILayer(string uiName)
        {
            UIData sceneUIData = _dataSource.UIDataList[uiName];
            GameObject instance = await AddressableInstantiateHelper.InstantiateAsync(sceneUIData.assetName, mainNode).AttachExternalCancellation(this.GetCancellationTokenOnDestroy());
            return instance.GetComponent<UILayer>();
        }
        #endregion

        private void OnEventBackKey()
        {
            if (isSceneChanging)
            {
                return;
            }

            if (blockBackKeySources.Count != 0)
            {
                return;
            }

            for (var i = 0; i < uiStacks.Count; i++)
            {
                if (uiStacks[i].state == UIState.Entering || uiStacks[i].state == UIState.Exiting)
                {
                    return;
                }
            }

            if (uiStacks.Count > 0)
            {
                int index = uiStacks.Count - 1;
                while (index >= 0)
                {
                    var isOffPrevUI = false;
                    uiStacks[index].ui.OnBackButton(ref isOffPrevUI);
                    if (!isOffPrevUI)
                    {
                        break;
                    }

                    --index;
                }
            }
        }

        #region Dim Layer
        public void SetSizeAsDimLayer(RectTransform targetRectTr)
        {
            targetRectTr.anchorMin = new Vector2(0.5f, 0.5f);
            targetRectTr.anchorMax = new Vector2(0.5f, 0.5f);
            targetRectTr.pivot = new Vector2(0.5f, 0.5f);
            var mainCanvasRectTr = mainCanvas.GetComponent<RectTransform>();
            targetRectTr.sizeDelta = mainCanvasRectTr.rect.size;
            var parentRectTrs = new List<RectTransform>();
            var tmpTr = targetRectTr.parent.GetComponent<RectTransform>();
            while (tmpTr != null && tmpTr != mainCanvas.transform)
            {
                parentRectTrs.Add(tmpTr);
                tmpTr = tmpTr.parent.GetComponent<RectTransform>();
            }

            parentRectTrs.Sort((x, y) =>
            {
                if (x.parent == y)
                {
                    return 1;
                }

                return -1;
            });

            Vector2 currPos = Vector2.zero;
            Vector3 currScale = Vector3.one;
            for (var i = 0; i < parentRectTrs.Count; i++)
            {
                currPos -= (Vector2) parentRectTrs[i].localPosition * currScale;
                Vector3 localScale = parentRectTrs[i].localScale;
                currScale.x = currScale.x * localScale.x;
                currScale.y = currScale.y * localScale.y;
                currScale.z = currScale.z * localScale.z;
            }

            targetRectTr.anchoredPosition = new Vector3(currPos.x / currScale.x, currPos.y / currScale.y, 0f);
            targetRectTr.localScale = new Vector3(1f / currScale.x, 1f / currScale.y, 1f / currScale.z);
        }

        private Image CreateDimLayer(string name)
        {
            var dimLayerGo = new GameObject(name, typeof(RectTransform));
            var dimLayer = dimLayerGo.AddComponent<Image>();
            dimLayer.color = new Color(0f, 0f, 0f, 0f);
            dimLayer.sprite = Resources.Load<Sprite>("UI/Common/black");
            var dimLayerTr = dimLayerGo.GetComponent<RectTransform>();
            dimLayerTr.SetParent(mainNode, false);
            SetSizeAsDimLayer(dimLayerTr);

            var btn = dimLayerGo.AddComponent<Button>();
            btn.onClick = new Button.ButtonClickedEvent();
            btn.transition = Selectable.Transition.None;
            btn.onClick.AddListener(() => OnClickDimLayer(dimLayerGo));

            if (mainCanvas.renderMode == RenderMode.ScreenSpaceCamera)
            {
                var boxColliderGo = new GameObject("boxCollider", typeof(RectTransform), typeof(BoxCollider));
                var boxColliderTr = boxColliderGo.GetComponent<RectTransform>();
                var boxCollider = boxColliderGo.GetComponent<BoxCollider>();
                boxColliderTr.SetParent(dimLayerTr, false);
                boxColliderTr.anchorMin = Vector2.zero;
                boxColliderTr.anchorMax = Vector2.one;
                boxColliderTr.sizeDelta = Vector2.zero;
                boxColliderTr.localScale = Vector3.one;
                boxColliderTr.localPosition = new Vector3(0, 0, 1);
                Vector2 canvasSize = boxColliderTr.rect.size;
                boxCollider.size = canvasSize;
            }

            return dimLayer;
        }

        public void OnClickDimLayer(GameObject go)
        {
            // SoundManager.Instance.PlaySFX("button_close");
            // Debug.Log("OnClickDimLayer!");

            if (isDimLayerOn)
            {
                OnEventBackKey();
            }
        }
        #endregion

        #region Scene Load
        public class SceneLoadAsyncOperationWrapper
        {
            private AsyncOperation asyncOperation;
            public event Action completed;

            internal void SetAsyncOperation(AsyncOperation asyncOperation)
            {
                this.asyncOperation = asyncOperation;
                asyncOperation.completed += CompleteCallback;
            }

            public float progress => asyncOperation?.progress ?? 0f;
            public bool allowSceneActivation = true;

            internal bool internalAllowSceneActivation
            {
                get => asyncOperation.allowSceneActivation;
                set => asyncOperation.allowSceneActivation = value;
            }

            public bool isDone => asyncOperation?.isDone ?? false;
            public int priority => asyncOperation?.priority ?? int.MaxValue;

            private void CompleteCallback(AsyncOperation operation)
            {
                completed?.Invoke();
                completed = null;
                asyncOperation.completed -= CompleteCallback;
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

            operationWrapper.completed += () => OnSceneLoaded(sceneName, defaultUIData, transition);
            ChangeSceneAsync(sceneName, operationWrapper, defaultUIData, transition).AttachExternalCancellation(this.GetCancellationTokenOnDestroy());
            return operationWrapper;
        }

        private async UniTask ChangeSceneAsync(string sceneName, SceneLoadAsyncOperationWrapper operationWrapper, object defaultUIData, ISceneTransition transition)
        {
            ClearUIPool();

            await transition.FadeInAsync();

            if (sceneLoadedAsyncTasks != null)
            {
                var tasks = new UniTask[sceneLoadedAsyncTasks.Count];
                for (var i = 0; i < sceneLoadedAsyncTasks.Count; i++)
                {
                    tasks[i] = sceneLoadedAsyncTasks[i].Invoke(CurrentSceneName, sceneName, defaultUIData);
                }

                await tasks;
            }

            // default UI load
            {
                var tasks = new UniTask<UILayer>[_dataSource.SceneDataList[sceneName].defaultUINames.Length];
                for (var i = 0; i < _dataSource.SceneDataList[sceneName].defaultUINames.Length; i++)
                {
                    int index = i;
                    tasks[i] = LoadUILayer(_dataSource.SceneDataList[sceneName].defaultUINames[index]);
                }

                UILayer[] res = await UniTask.WhenAll(tasks);
                for (var i = 0; i < res.Length; i++)
                {
                    PoolingUI(_dataSource.SceneDataList[sceneName].defaultUINames[i], res[i]);
                }
            }

            // preload addressable
            if (_dataSource.SceneDataList[sceneName].preloads != null)
            {
                for (var i = 0; i < _dataSource.SceneDataList[sceneName].preloads.Length; i++)
                {
                    AddressableLoadHelper.LoadAssetAsync<GameObject>(_dataSource.SceneDataList[sceneName].preloads[i]).Forget();
                }
            }

            AsyncOperation asyncOperation = SceneManager.LoadSceneAsync(sceneName);
            asyncOperation.allowSceneActivation = false;
            operationWrapper.SetAsyncOperation(asyncOperation);
            await UniTask.WaitUntil(() => operationWrapper.progress >= 0.9f);
            await UniTask.WaitUntil(() => operationWrapper.allowSceneActivation);

            if (isLoadingUI)
            {
                noNeedToLoadUI = true;
            }

            for (var i = 0; i < uiStacks.Count; i++)
            {
                uiStacks[i].ui.OnPreExit();
                uiStacks[i].state = UIState.Exiting;
                OnUITransitionEvent?.Invoke(UITransition.Exiting, uiStacks[i].key, uiStacks[i].ui);
                uiStacks[i].ui.OnPostExit();
                OnUITransitionEvent?.Invoke(UITransition.ExitFinished, uiStacks[i].key, uiStacks[i].ui);
            }

            uiStacks.Clear();
            dimLayer = null;
            isDimLayerOn = false;

            OnSceneUnloadedEvent?.Invoke(CurrentSceneName);
            Resources.UnloadUnusedAssets();
            GC.Collect();

            operationWrapper.internalAllowSceneActivation = true;
        }

        private void OnSceneLoaded(string sceneName, object defaultUIData, ISceneTransition transition)
        {
            CurrentSceneName = sceneName;
            ResetNodeRefs();

            for (var i = 0; i < _dataSource.SceneDataList[sceneName].defaultUINames.Length; i++)
            {
                PushUILayer(_dataSource.SceneDataList[sceneName].defaultUINames[i], defaultUIData); // default UI는 key와 uiName이 같아야 한다.
            }

            transition.FadeOutAsync(true);

            isSceneChanging = false;
            OnSceneLoadedEvent?.Invoke(sceneName);
        }
        #endregion

        #region ISelectableBlocker
        bool ISelectableBlocker.IsAllowSelectable(string selectableName)
        {
            // 띄울 유아이가 있을 때 누르는 버튼 차단
            if (isLoadingUI)
            {
                return false;
            }

            // 유아이가 뜨거나 닫히고 있다면 버튼 차단
            for (var i = 0; i < uiStacks.Count; i++)
            {
                if (uiStacks[i].state == UIState.Entering || uiStacks[i].state == UIState.Exiting)
                {
                    return false;
                }
            }

            return true;
        }

        void ISelectableBlocker.OnClicked(string selectableName)
        {
        }

        int ISelectableBlocker.GetPriority()
        {
            return 0;
        }
        #endregion
    }
}
