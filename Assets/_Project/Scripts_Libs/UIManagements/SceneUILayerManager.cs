using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

namespace CookApps.TeamBattle.UIManagements
{
    public sealed partial class SceneUILayerManager : SingletonMonoBehaviour<SceneUILayerManager>, ISelectableBlocker
    {
        public enum PushReqCode
        {
            OK,
            DuplicatedKey,
            NotExistUILayerName,
            Loading,
        }

        public enum PopReqCode
        {
            OK,
            NotExist,
        }

        private enum UILayerState
        {
            Initialized,
            Entering,
            Entered,
            Exiting,
            Hiding, // use only popup
        }

        public enum UILayerTransition
        {
            Entering,
            EnterFinished,
            Exiting,
            ExitFinished,
        }

        public enum UILayerType
        {
            None = 0,
            Cover,
            Overlay,
            Popup,
            Modal,
        }

        [Serializable]
        private class UILayerStackData
        {
            public UILayerStackData(string layerName, string key, long inc, UILayer layer, UILayerState state, Action<object> closeCallback)
            {
                this.layerName = layerName;
                this.key = key;
                this.layer = layer;
                this.layer.Key = key;
                this.state = state;
                this.closeCallback = closeCallback;
                this.inc = inc;
            }

            public readonly long inc;
            public string layerName;
            public readonly string key;
            public UILayer layer;

            public UILayerState state;

            public Action<object> closeCallback;
        }

        [Serializable]
        public class UILayerData
        {
            public string name;
            public UILayerType layerType;
            public string addressableName;
        }

        [Serializable]
        public class SceneData
        {
            public string sceneName;
            public string addressableName;
            public string[] defaultUILayerNames;
        }

        public string[] GetDefaultUILayerNames(string sceneName)
        {
            if (_dataSource.SceneDataList.TryGetValue(sceneName, out SceneData sceneData))
            {
                return sceneData.defaultUILayerNames;
            }

            return null;
        }

        private ISceneUIDataSource _dataSource;

        // ui layer pool
        private Dictionary<string, Queue<GameObject>> uiLayerPool = new ();

        private List<UILayerStackData> uiLayerStacks = new ();

        private bool isLoadingUI;

        private bool noNeedToLoadUI;
        // private WaitingUIData currLoadingUIData = null;
        // private List<WaitingUIData> waitingUIDataList = new ();

        public string CurrentSceneName { get; private set; }
        private Transform mainNode;
        private Canvas mainNodeCanvas;
        public Transform MainNode => mainNode;

        private Transform recycles;
        private Image dimLayer;
        private bool isDimLayerOn;
        private Canvas mainCanvas;
        public Canvas MainCanvas => mainCanvas;

        private Transform floatingNode;
        private Canvas floatingNodeCanvas;
        public Transform FloatingNode => floatingNode;

        public static event Action<UILayerTransition, string, UILayer> OnUITransitionEvent;
        public static event Action<string> OnSceneUnloadedEvent;
        public static event Action<string> OnSceneLoadedEvent;

        public int CurrentUICount => /*waitingUIDataList.Count + */uiLayerStacks.Count;
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

        public long uiIncAcc;

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
        }

        public void ClearUIPool()
        {
            foreach (KeyValuePair<string, Queue<GameObject>> pair in uiLayerPool)
            {
                while (pair.Value.Count > 0)
                {
                    GameObject go = pair.Value.Dequeue();
                    AddressableInstantiateHelper.ReleaseGameObject(go);
                    Destroy(go);
                }
            }

            uiLayerPool.Clear();
        }

        public string[] GetAllLoadedUIKeys()
        {
            return uiLayerStacks.Select(x => x.key).ToArray();
        }

        /// <summary>
        /// uiLayer의 값들을 바꾸진 말자
        /// </summary>
        public UILayer[] GetUIRoutes(bool isContainCover = true, bool isContainOverlay = false)
        {
            return uiLayerStacks.Where(x =>
            {
                if (x.layer.UILayerType is UILayerType.Popup or UILayerType.Modal)
                {
                    return true;
                }

                if (isContainCover && x.layer.UILayerType == UILayerType.Cover)
                {
                    return true;
                }

                if (isContainOverlay && x.layer.UILayerType == UILayerType.Overlay)
                {
                    return true;
                }

                return false;
            }).Select(x => x.layer).ToArray();
        }

        #region Push or Pop UI
        public async UniTask<(PushReqCode, object)> PushUILayerAsync(string uiName, object data = null)
        {
            (PushReqCode, object) ret = (PushReqCode.OK, null);
            var isClosed = false;

            PushReqCode reqCode = PushUILayerWithKey(uiName, uiName, data, closeCallback);
            if (reqCode != PushReqCode.OK)
            {
                ret.Item1 = reqCode;
                return ret;
            }

            await UniTask.WaitUntil(() => isClosed);
            return ret;

            void closeCallback(object res)
            {
                ret = (ret.Item1, res);
                isClosed = true;
            }
        }

        public PushReqCode PushUILayer(string uiName, object data = null, Action<object> closeCallback = null)
        {
            return PushUILayerWithKey(uiName, uiName, data, closeCallback);
        }

        public PushReqCode PushUILayerWithKey(string uiName, string key, object data = null, Action<object> closeCallback = null)
        {
            bool isExistUIStack = uiLayerStacks.Exists(x =>
                x.state != UILayerState.Exiting && x.key.Equals(key));

            // bool isExistWaiting = waitingUIDataList.Exists(x => x.key == key);
            if (isExistUIStack /* || isExistWaiting*/)
            {
                Debug.LogAssertion($"{uiName}::{key} is already exist!!");
                return PushReqCode.DuplicatedKey;
            }

            if (!_dataSource.UIDataList.ContainsKey(uiName))
            {
                Debug.LogAssertion($"{uiName} is not exist UI name!");
                return PushReqCode.NotExistUILayerName;
            }

            if (uiLayerPool.ContainsKey(uiName) && uiLayerPool[uiName].Count > 0)
            {
                GameObject ui = uiLayerPool[uiName].Dequeue();
                if (uiLayerPool[uiName].Count == 0)
                {
                    uiLayerPool.Remove(uiName);
                }

                var uiBase = ui.GetComponent<UILayer>();
                UILayerStackData stackData = MakeUIStackData(uiBase, key, uiName, closeCallback);
                PushUILayerInternal(stackData, data);
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

                UILayerStackData stackData = MakeUIStackData(ui, key, uiName, closeCallback);
                PushUILayerInternal(stackData, data);
            }).Forget();
            return PushReqCode.OK;
        }

        private UILayerStackData MakeUIStackData(UILayer uiLayer, string key, string uiName, Action<object> closeCallback)
        {
            uiLayer.CachedGo.SetActive(false);
            uiLayer.CachedRectTr.SetParent(mainNode, false);
            uiLayer.name = key;
            uiLayer.UILayerType = _dataSource.UIDataList[uiName].layerType;
            long inc = uiIncAcc + (uiLayer.Priority * 100);
            uiIncAcc++;
            return new UILayerStackData(uiName, key, inc, uiLayer, UILayerState.Initialized, closeCallback);
        }

        private void PushUILayerInternal(UILayerStackData uiLayerData, object data)
        {
            uiLayerData.layer.CachedGo.SetActive(true);
            uiLayerData.layer.OnPreEnter(data);
            uiLayerData.state = UILayerState.Entering;
            OnUITransitionEvent?.Invoke(UILayerTransition.Entering, uiLayerData.key, uiLayerData.layer);
            uiLayerStacks.Add(uiLayerData);
            uiLayerStacks.Sort((x, y) => (int) (x.inc - y.inc));

            // managing z order
            isDimLayerOn = false;
            var isPopupShown = false;
            for (int i = uiLayerStacks.Count - 1; i >= 0; i--)
            {
                UILayerStackData stackData = uiLayerStacks[i];
                RectTransform uiTr = stackData.layer.CachedRectTr;
                uiTr.SetAsFirstSibling();
                if (stackData.state == UILayerState.Exiting)
                {
                    continue;
                }

                if (stackData.layer.UILayerType == UILayerType.Popup)
                {
                    if (isPopupShown)
                    {
                        if (stackData.state != UILayerState.Hiding)
                        {
                            stackData.state = UILayerState.Hiding;
                            stackData.layer.StartExitAnimation(null);
                        }
                    }
                    else
                    {
                        isPopupShown = true;
                    }
                }

                {
                    if (isDimLayerOn)
                    {
                        continue;
                    }
                }

                if (uiLayerStacks[i].layer.UILayerType is UILayerType.Popup or UILayerType.Modal)
                {
                    if (dimLayer == null)
                    {
                        dimLayer = CreateDimLayer("DimLayer");
                    }

                    dimLayer.transform.SetAsFirstSibling();
                    isDimLayerOn = true;
                }
            }

            Canvas.ForceUpdateCanvases();
            uiLayerData.layer.StartEnterAnimation(OnEndEnterAnimation);
        }

        private void OnEndEnterAnimation(UILayer ui)
        {
            bool isExist = uiLayerStacks.Exists(x => x.layer == ui);
            if (!isExist)
            {
                return;
            }

            UILayerStackData stackData = uiLayerStacks.Find(x => x.layer == ui);

            stackData.state = UILayerState.Entered;
            ui.OnPostEnter();
            OnUITransitionEvent?.Invoke(UILayerTransition.EnterFinished, stackData.key, stackData.layer);

            if (ui.UILayerType != UILayerType.Cover)
            {
                return;
            }

            var isFoundPrevCover = false;
            for (var i = 0; i < uiLayerStacks.Count; i++)
            {
                if (uiLayerStacks[i].layer == ui)
                {
                    break;
                }

                if (!isFoundPrevCover)
                {
                    isFoundPrevCover = uiLayerStacks[i].layer.UILayerType == UILayerType.Cover;
                }

                if (isFoundPrevCover)
                {
                    uiLayerStacks[i].layer.CachedGo.SetActive(false);
                }
            }
        }

        public PopReqCode PopUILayer(string key, object dataToCloseCallback = null)
        {
            bool isExist = uiLayerStacks.Exists(x => x.key.Equals(key));
            if (!isExist)
            {
                return PopReqCode.NotExist;
            }

            UILayerStackData stackData = uiLayerStacks.Find(x => x.key.Equals(key));
            if (stackData.state == UILayerState.Exiting)
            {
                return PopReqCode.NotExist;
            }

            PopUILayerInternal(stackData, dataToCloseCallback);
            return PopReqCode.OK;
        }

        public PopReqCode PopUILayer(UILayer ui, object dataToCloseCallback = null)
        {
            bool isExist = uiLayerStacks.Exists(x => x.layer.Equals(ui));
            if (!isExist)
            {
                return PopReqCode.NotExist;
            }

            UILayerStackData stackData = uiLayerStacks.Find(x => x.layer.Equals(ui));
            if (stackData.state == UILayerState.Exiting)
            {
                return PopReqCode.NotExist;
            }

            PopUILayerInternal(stackData, dataToCloseCallback);
            return PopReqCode.OK;
        }

        public PopReqCode PopTopUILayer(object dataToCloseCallback = null)
        {
            if (uiLayerStacks.Count <= 1)
            {
                return PopReqCode.NotExist;
            }

            PopUILayerInternal(uiLayerStacks[^1], dataToCloseCallback);
            return PopReqCode.OK;
        }

        public void PopAllUI()
        {
            for (int i = uiLayerStacks.Count - 1; i >= 0; i--)
            {
                if (uiLayerStacks[i].state == UILayerState.Initialized ||
                    uiLayerStacks[i].state == UILayerState.Entering ||
                    uiLayerStacks[i].state == UILayerState.Entered)
                {
                    uiLayerStacks[i].layer.OnPreExit();
                }

                OnUITransitionEvent?.Invoke(UILayerTransition.Exiting, uiLayerStacks[i].key, uiLayerStacks[i].layer);
                uiLayerStacks[i].state = UILayerState.Exiting;
                uiLayerStacks[i].layer.OnPostExit();
                OnUITransitionEvent?.Invoke(UILayerTransition.ExitFinished, uiLayerStacks[i].key, uiLayerStacks[i].layer);
                PoolingUILayer(uiLayerStacks[i].layerName, uiLayerStacks[i].layer);
                uiLayerStacks[i] = null;
            }

            uiLayerStacks.Clear();
        }

        private enum CoverState
        {
            NoNeedToCheck,
            Check,
        }

        private void PopUILayerInternal(UILayerStackData popUILayerData, object dataToCloseCallback)
        {
            // z order 정렬
            popUILayerData.layer.OnPreExit();
            OnUITransitionEvent?.Invoke(UILayerTransition.Exiting, popUILayerData.key, popUILayerData.layer);
            popUILayerData.state = UILayerState.Exiting;
            uiLayerStacks.Remove(popUILayerData);

            popUILayerData.layer.StartExitAnimation(ui =>
            {
                ui.OnPostExit();
                PoolingUILayer(popUILayerData.layerName, ui);

                popUILayerData.closeCallback?.Invoke(dataToCloseCallback);
                OnUITransitionEvent?.Invoke(UILayerTransition.ExitFinished, popUILayerData.key, popUILayerData.layer);
            });

            CoverState coverState = popUILayerData.layer.UILayerType == UILayerType.Cover ? CoverState.Check : CoverState.NoNeedToCheck;
            var isPopupShown = false;
            isDimLayerOn = false;
            for (int i = uiLayerStacks.Count - 1; i >= 0; i--)
            {
                UILayerStackData stackData = uiLayerStacks[i];
                if (coverState == CoverState.Check)
                {
                    stackData.layer.CachedGo.SetActive(true);
                    if (stackData.layer.UILayerType == UILayerType.Cover)
                    {
                        coverState = CoverState.NoNeedToCheck;
                    }
                }

                stackData.layer.CachedRectTr.SetAsFirstSibling();

                if (stackData.layer.UILayerType == UILayerType.Popup)
                {
                    if (isPopupShown)
                    {
                        if (stackData.state != UILayerState.Hiding)
                        {
                            stackData.state = UILayerState.Hiding;
                            stackData.layer.StartExitAnimation(null);
                        }
                    }
                    else
                    {
                        isPopupShown = true;
                        if (stackData.state == UILayerState.Hiding)
                        {
                            stackData.state = UILayerState.Entering;
                            stackData.layer.StartEnterAnimation(_ => stackData.state = UILayerState.Entered);
                        }
                    }
                }

                if (uiLayerStacks[i].state == UILayerState.Exiting)
                {
                    continue;
                }

                if (isDimLayerOn)
                {
                    continue;
                }

                if (uiLayerStacks[i].layer.UILayerType is UILayerType.Popup or UILayerType.Modal)
                {
                    dimLayer.rectTransform.SetAsFirstSibling();
                    isDimLayerOn = true;
                }
            }
        }

        private void PoolingUILayer(string uiName, UILayer ui)
        {
            ui.CachedGo.SetActive(false);
            ui.CachedRectTr.SetParent(recycles, false);
            if (!uiLayerPool.ContainsKey(uiName))
            {
                uiLayerPool.Add(uiName, new Queue<GameObject>());
            }

            uiLayerPool[uiName].Enqueue(ui.CachedGo);
        }

        public UILayer GetUIBase(string uiKey)
        {
            for (var i = 0; i < uiLayerStacks.Count; i++)
            {
                if (uiLayerStacks[i].key == uiKey)
                {
                    return uiLayerStacks[i].layer;
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
            UILayerData sceneUILayerData = _dataSource.UIDataList[uiName];
            GameObject instance = await AddressableInstantiateHelper.InstantiateAsync(sceneUILayerData.addressableName, mainNode).AttachExternalCancellation(this.GetCancellationTokenOnDestroy());
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

            for (var i = 0; i < uiLayerStacks.Count; i++)
            {
                if (uiLayerStacks[i].state == UILayerState.Entering || uiLayerStacks[i].state == UILayerState.Exiting)
                {
                    return;
                }
            }

            if (uiLayerStacks.Count > 0)
            {
                int index = uiLayerStacks.Count - 1;
                while (index >= 0)
                {
                    var isOffPrevUI = false;
                    uiLayerStacks[index].layer.OnBackButton(ref isOffPrevUI);
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

            var btn = dimLayerGo.AddComponent<CAButton>();
            btn.onClick = new Button.ButtonClickedEvent();
            btn.transition = Selectable.Transition.None;
            btn.onClick.AddListener(OnClickDimLayer);

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

        private void OnClickDimLayer()
        {
            if (isDimLayerOn)
            {
                OnEventBackKey();
            }
        }
        #endregion

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

            SceneData sceneData = _dataSource.SceneDataList[sceneName];
            // default UI load
            {
                var uiLayerLoadingTask = new UniTask<UILayer>[sceneData.defaultUILayerNames.Length];
                for (var i = 0; i < sceneData.defaultUILayerNames.Length; i++)
                {
                    int index = i;
                    uiLayerLoadingTask[i] = LoadUILayer(sceneData.defaultUILayerNames[index]);
                }

                UILayer[] res = await UniTask.WhenAll(uiLayerLoadingTask);
                for (var i = 0; i < res.Length; i++)
                {
                    PoolingUILayer(sceneData.defaultUILayerNames[i], res[i]);
                }
            }
            await transition.FadeInAsync();

            AsyncOperationHandle<SceneInstance> asyncOperation = Addressables.LoadSceneAsync(sceneData.addressableName, activateOnLoad: false);
            operationWrapper.SetAsyncOperation(asyncOperation);
            SceneInstance sceneInstance = await asyncOperation;
            await UniTask.WaitUntil(() => operationWrapper.allowSceneActivation);

            if (isLoadingUI)
            {
                noNeedToLoadUI = true;
            }

            for (var i = 0; i < uiLayerStacks.Count; i++)
            {
                uiLayerStacks[i].layer.OnPreExit();
                uiLayerStacks[i].state = UILayerState.Exiting;
                OnUITransitionEvent?.Invoke(UILayerTransition.Exiting, uiLayerStacks[i].key, uiLayerStacks[i].layer);
                uiLayerStacks[i].layer.OnPostExit();
                OnUITransitionEvent?.Invoke(UILayerTransition.ExitFinished, uiLayerStacks[i].key, uiLayerStacks[i].layer);
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

            for (var i = 0; i < _dataSource.SceneDataList[sceneName].defaultUILayerNames.Length; i++)
            {
                PushUILayer(_dataSource.SceneDataList[sceneName].defaultUILayerNames[i], defaultUIData); // default UI는 key와 uiName이 같아야 한다.
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
            for (var i = 0; i < uiLayerStacks.Count; i++)
            {
                if (uiLayerStacks[i].state == UILayerState.Entering || uiLayerStacks[i].state == UILayerState.Exiting)
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
