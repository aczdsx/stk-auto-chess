using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace CookApps.TeamBattle.UIManagements
{
    public sealed partial class SceneUILayerManager : SingletonMonoBehaviour<SceneUILayerManager>
    {
        private Dictionary<string, SceneData> SceneDataList { get; } = new ();
        private Dictionary<Type, UILayerData> UIDataList { get; } = new ();

        // ui layer pool
        private Dictionary<Type, Queue<GameObject>> uiLayerPool = new ();

        private List<UILayerStackData> uiLayerStacks = new ();

        private bool isLoadingUI;
        private bool noNeedToLoadUI;

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

        public int CurrentUICount => uiLayerStacks.Count;

        /// <summary>
        /// 간단하게 back key 블록을 하고 싶을 때 사용
        /// </summary>
        private List<string> blockBackKeySources = new ();

        public void BlockBackKey(string srcKey)
        {
            blockBackKeySources.Add(srcKey);
        }

        public void ReleaseBackKey(string srcKey)
        {
            blockBackKeySources.Remove(srcKey);
        }

        internal bool isSceneChanging;

        private long uiIncAcc;

        /// <summary>
        /// UIManagements 초기화
        /// 재사용 UI들을 관리할 recycles 생성, UI Layer Date Source 생성
        /// </summary>
        public void Initialize(SceneDatabase sceneDatabase, Type[] uiLayerTypes)
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

            SceneDataList.Clear();
            foreach (SceneData sceneData in sceneDatabase.List)
            {
                SceneDataList.Add(sceneData.SceneName, sceneData);
            }

            UIDataList.Clear();
            foreach (Type uiLayerType in uiLayerTypes)
            {
                RegisterUILayerAttribute attribute = RegisterUILayerAttributeHelper.GetAttribute(uiLayerType);
                if (attribute == null)
                {
                    continue;
                }

                UIDataList.Add(uiLayerType, new UILayerData(attribute.LayerType, attribute.AddressableName));

                SceneNameWithUILayerAttribute[] sceneNameWithUILayerAttributes = SceneNameWithUILayerAttributeHelper.GetAttribute(uiLayerType);
                if (sceneNameWithUILayerAttributes == null)
                {
                    continue;
                }

                foreach (SceneNameWithUILayerAttribute sceneNameWithUILayerAttribute in sceneNameWithUILayerAttributes)
                {
                    SceneData sceneData = SceneDataList[sceneNameWithUILayerAttribute.SceneName];
                    sceneData.AddDefaultUILayer(uiLayerType);
                    foreach (Type subUILayer in sceneNameWithUILayerAttribute.SubUILayers)
                    {
                        sceneData.AddDefaultUILayer(subUILayer);
                    }
                }
            }

            // 첫번째 씬에서 필요
            ResetNodeRefs();
            SelectableBlockerManager.Instance.AddBlocker(this);
        }

        private void ResetNodeRefs()
        {
            mainCanvas = null;
            Scene activeScene = SceneManager.GetActiveScene();
            GameObject[] rootGOs = activeScene.GetRootGameObjects();
            CameraManager.Instance.ReleaseMainCamera();
            for (var i = 0; i < rootGOs.Length; i++)
            {
                var camera = rootGOs[i].GetComponent<Camera>();
                if (camera == null)
                {
                    continue;
                }

                if (!camera.tag.Contains("MainCamera"))
                {
                    continue;
                }

                CameraManager.Instance.SetMainCamera(camera);
                break;
            }

            bool isLoadingScene = activeScene.name == "SceneLoading";
            var hasLoadingComp = false;
            for (var i = 0; i < rootGOs.Length; i++)
            {
                if (rootGOs[i].name == "MainCanvas")
                {
                    SetMainCanvas(rootGOs[i]);
                    continue;
                }

                if (rootGOs[i].name == "EventSystem")
                {
                    rootGOs[i].GetComponent<EventSystem>().pixelDragThreshold = UIManagementsConst.Default.DragThreshold;
                    continue;
                }

                if (isLoadingScene && rootGOs[i].GetComponent<SceneLoading>() != null)
                {
                    hasLoadingComp = true;
                }
            }

            if (isLoadingScene && !hasLoadingComp)
            {
                var loadingGo = new GameObject("SceneLoading");
                loadingGo.AddComponent<SceneLoading>();
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
            foreach (KeyValuePair<Type, Queue<GameObject>> pair in uiLayerPool)
            {
                while (pair.Value.Count > 0)
                {
                    GameObject go = pair.Value.Dequeue();
                    Addressables.ReleaseInstance(go);
                    Destroy(go);
                }
            }

            uiLayerPool.Clear();
        }

        public string[] GetAllLoadedUIKeys()
        {
            return uiLayerStacks.Select(x => x.Key).ToArray();
        }

        /// <summary>
        /// uiLayer의 값들을 바꾸진 말자
        /// </summary>
        public UILayer[] GetUIRoutes(bool isContainCover = true, bool isContainOverlay = false)
        {
            return uiLayerStacks.Where(x => x.Layer.UILayerType is UILayerType.Popup or UILayerType.Modal ||
                                            (isContainCover && x.Layer.UILayerType == UILayerType.Cover) ||
                                            (isContainOverlay && x.Layer.UILayerType == UILayerType.Overlay)).Select(x => x.Layer).ToArray();
        }

        #region Push or Pop UI
        /// <summary>
        /// 팝업을 띄우고 해당 팝업을 리턴합니다. 동일한 팝업은 여러개 띄울 수 없습니다.
        /// </summary>
        /// <param name="data">팝업의 OnPreEnter 함수의 인자로 넣어줄 데이터입니다.</param>
        /// <param name="closeCallback">팝업이 닫힐 때 호출될 콜백입니다.</param>
        /// <returns>팝업 객체입니다.</returns>
        public async UniTask<T> PushUILayerAsync<T>(object data = null, Action<object> closeCallback = null) where T : UILayer
        {
            string uiName = typeof(T).Name;
            var layer = await PushUILayerAsync<T>(uiName, data, closeCallback);
            return layer;
        }

        /// <summary>
        /// 팝업을 띄우고 해당 팝업을 리턴합니다. key를 사용하여 같은 팝업을 여러개 띄울 수 있습니다.
        /// </summary>
        /// <param name="key">팝업의 유니크한 키입니다.</param>
        /// <param name="data">팝업의 OnPreEnter 함수의 인자로 넣어줄 데이터입니다.</param>
        /// <param name="closeCallback">팝업이 닫힐 때 호출될 콜백입니다.</param>
        /// <returns>팝업 객체입니다.</returns>
        public async UniTask<T> PushUILayerAsync<T>(string key, object data = null, Action<object> closeCallback = null) where T : UILayer
        {
            var layer = await PushUILayerAsync(typeof(T), key, data, closeCallback);
            return layer as T;
        }

        private async UniTask<UILayer> PushUILayerAsync(Type uiType, string key, object data = null, Action<object> closeCallback = null)
        {
            while (isLoadingUI)
            {
                await UniTask.Yield();
            }

            var isExistUIStack = false;
            for (var i = 0; i < uiLayerStacks.Count; i++)
            {
                if (uiLayerStacks[i].Key != key ||
                    uiLayerStacks[i].State == UILayerState.Exiting)
                {
                    continue;
                }

                isExistUIStack = true;
                break;
            }

            if (isExistUIStack)
            {
                Debug.LogAssertion($"{uiType.Name}::{key} is already exist!!");
                return null;
            }

            if (!UIDataList.ContainsKey(uiType))
            {
                Debug.LogAssertion($"{uiType.Name} is not exist UI name!");
                return null;
            }

            UILayer uiLayer;
            if (uiLayerPool.TryGetValue(uiType, out Queue<GameObject> queue) && queue.Count > 0)
            {
                uiLayer = queue.Dequeue().GetComponent(uiType) as UILayer;
            }
            else
            {
                isLoadingUI = true;
                uiLayer = await LoadUILayer(uiType);
                isLoadingUI = false;
                if (noNeedToLoadUI)
                {
                    Addressables.ReleaseInstance(uiLayer.CachedGo);
                    Destroy(uiLayer.CachedGo);
                    return null;
                }
            }

            UILayerStackData stackData = MakeUIStackData(uiLayer, key, closeCallback);
            PushUILayerInternal(stackData, data);
            return uiLayer;
        }

        private UILayerStackData MakeUIStackData(UILayer uiLayer, string key, Action<object> closeCallback)
        {
            uiLayer.CachedGo.SetActive(false);
            uiLayer.CachedRectTr.SetParent(mainNode, false);
            uiLayer.name = key;
            Type type = uiLayer.GetType();
            uiLayer.UILayerType = UIDataList[type].LayerType;
            long inc = uiIncAcc + (uiLayer.Priority * 100);
            uiIncAcc++;
            return new UILayerStackData(type, key, inc, uiLayer, UILayerState.Initialized, closeCallback);
        }

        private void PushUILayerInternal(UILayerStackData uiLayerStackData, object data)
        {
            uiLayerStacks.Add(uiLayerStackData);
            uiLayerStacks.Sort(UILayerStackData.SortByInc);

            uiLayerStackData.Layer.CachedGo.SetActive(true);
            uiLayerStackData.Layer.OnPreEnter(data);
            uiLayerStackData.SetState(UILayerState.Entering);
            OnUITransitionEvent?.Invoke(UILayerTransition.Entering, uiLayerStackData.Key, uiLayerStackData.Layer);

            // managing z order
            isDimLayerOn = false;
            var isPopupShown = false;
            for (int i = uiLayerStacks.Count - 1; i >= 0; i--)
            {
                RectTransform uiTr = uiLayerStacks[i].Layer.CachedRectTr;
                uiTr.SetAsFirstSibling();
                if (uiLayerStacks[i].State == UILayerState.Exiting)
                {
                    continue;
                }

                if (uiLayerStacks[i].Layer.UILayerType == UILayerType.Popup)
                {
                    if (isPopupShown)
                    {
                        if (uiLayerStacks[i].State != UILayerState.Hiding)
                        {
                            uiLayerStacks[i].SetState(UILayerState.Hiding);
                            uiLayerStacks[i].Layer.StartExitAnimation(null);
                        }
                    }
                    else
                    {
                        isPopupShown = true;
                    }
                }

                // 24.06.18 - 자체 딤레이어 사용하지 않음
                // if (isDimLayerOn || uiLayerStacks[i].Layer.UILayerType is not (UILayerType.Popup or UILayerType.Modal))
                // {
                //     continue;
                // }
                //
                // if (dimLayer == null)
                // {
                //     dimLayer = CreateDimLayer("DimLayer");
                // }
                //
                // dimLayer.transform.SetAsFirstSibling();
                // isDimLayerOn = true;
            }

            Canvas.ForceUpdateCanvases();
            uiLayerStackData.Layer.StartEnterAnimation(OnEndEnterAnimation);
        }

        private void OnEndEnterAnimation(UILayer ui)
        {
            int index = -1;
            for (var i = 0; i < uiLayerStacks.Count; i++)
            {
                if (uiLayerStacks[i].Layer == ui)
                {
                    index = i;
                    break;
                }
            }

            if (index < 0)
            {
                return;
            }

            uiLayerStacks[index].SetState(UILayerState.Entered);
            ui.OnPostEnter();
            OnUITransitionEvent?.Invoke(UILayerTransition.EnterFinished, uiLayerStacks[index].Key, uiLayerStacks[index].Layer);

            if (ui.UILayerType != UILayerType.Cover)
            {
                return;
            }

            var isFoundPrevCover = false;
            for (var i = 0; i < uiLayerStacks.Count; i++)
            {
                if (uiLayerStacks[i].Layer == ui)
                {
                    break;
                }

                if (!isFoundPrevCover)
                {
                    isFoundPrevCover = uiLayerStacks[i].Layer.UILayerType == UILayerType.Cover;
                }

                if (isFoundPrevCover)
                {
                    uiLayerStacks[i].Layer.CachedGo.SetActive(false);
                }
            }
        }

        public bool PopUILayer(string key, object dataToCloseCallback = null)
        {
            int index = -1;
            for (var i = 0; i < uiLayerStacks.Count; i++)
            {
                if (uiLayerStacks[i].Key == key)
                {
                    index = i;
                    break;
                }
            }

            if (index < 0)
            {
                return false;
            }

            PopUILayerInternal(index, dataToCloseCallback);
            return true;
        }

        public bool PopUILayer(UILayer ui, object dataToCloseCallback = null)
        {
            int index = -1;
            for (var i = 0; i < uiLayerStacks.Count; i++)
            {
                if (uiLayerStacks[i].Layer == ui)
                {
                    index = i;
                    break;
                }
            }

            if (index < 0)
            {
                return false;
            }

            PopUILayerInternal(index, dataToCloseCallback);
            return true;
        }

        public bool PopTopUILayer(object dataToCloseCallback = null)
        {
            if (uiLayerStacks.Count <= 1)
            {
                return false;
            }

            PopUILayerInternal(uiLayerStacks.Count - 1, dataToCloseCallback);
            return true;
        }

        public void PopAllUI()
        {
            for (int i = uiLayerStacks.Count - 1; i >= 0; i--)
            {
                if (uiLayerStacks[i].State == UILayerState.Initialized ||
                    uiLayerStacks[i].State == UILayerState.Entering ||
                    uiLayerStacks[i].State == UILayerState.Entered)
                {
                    uiLayerStacks[i].Layer.OnPreExit();
                }

                OnUITransitionEvent?.Invoke(UILayerTransition.Exiting, uiLayerStacks[i].Key, uiLayerStacks[i].Layer);
                uiLayerStacks[i].SetState(UILayerState.Exiting);
                uiLayerStacks[i].Layer.OnPostExit();
                OnUITransitionEvent?.Invoke(UILayerTransition.ExitFinished, uiLayerStacks[i].Key, uiLayerStacks[i].Layer);
                PoolingUILayer(uiLayerStacks[i].Layer);
            }

            uiLayerStacks.Clear();
        }

        private enum CoverState
        {
            NoNeedToCheck,
            Check,
        }

        private void PopUILayerInternal(int index, object dataToCloseCallback)
        {
            // z order 정렬
            uiLayerStacks[index].Layer.OnPreExit();
            OnUITransitionEvent?.Invoke(UILayerTransition.Exiting, uiLayerStacks[index].Key, uiLayerStacks[index].Layer);
            uiLayerStacks[index].SetState(UILayerState.Exiting);
            UILayer uiLayer = uiLayerStacks[index].Layer;
            Action<object> callback = uiLayerStacks[index].CloseCallback;
            uiLayerStacks.RemoveAt(index);

            uiLayer.StartExitAnimation(ui =>
            {
                ui.OnPostExit();
                PoolingUILayer(ui);

                callback?.Invoke(dataToCloseCallback);
                OnUITransitionEvent?.Invoke(UILayerTransition.ExitFinished, uiLayer.Key, uiLayer);
            });

            CoverState coverState = uiLayer.UILayerType == UILayerType.Cover ? CoverState.Check : CoverState.NoNeedToCheck;
            var isPopupShown = false;
            isDimLayerOn = false;
            for (int i = uiLayerStacks.Count - 1; i >= 0; i--)
            {
                UILayerStackData stackData = uiLayerStacks[i];
                if (coverState == CoverState.Check)
                {
                    stackData.Layer.CachedGo.SetActive(true);
                    if (stackData.Layer.UILayerType == UILayerType.Cover)
                    {
                        coverState = CoverState.NoNeedToCheck;
                    }
                }

                stackData.Layer.CachedRectTr.SetAsFirstSibling();

                if (stackData.Layer.UILayerType == UILayerType.Popup)
                {
                    if (isPopupShown)
                    {
                        if (stackData.State != UILayerState.Hiding)
                        {
                            stackData.SetState(UILayerState.Hiding);
                            stackData.Layer.StartExitAnimation(null);
                        }
                    }
                    else
                    {
                        isPopupShown = true;
                        if (stackData.State == UILayerState.Hiding)
                        {
                            stackData.SetState(UILayerState.Entering);
                            stackData.Layer.StartEnterAnimation(_ => stackData.SetState(UILayerState.Entered));
                        }
                    }
                }

                if (uiLayerStacks[i].State == UILayerState.Exiting)
                {
                    continue;
                }

                // 24.06.18 - 자체 딤레이어 사용하지 않음

                // if (isDimLayerOn)
                // {
                //     continue;
                // }


                // if (uiLayerStacks[i].Layer.UILayerType is UILayerType.Popup or UILayerType.Modal)
                // {
                //     dimLayer.rectTransform.SetAsFirstSibling();
                //     isDimLayerOn = true;
                // }
            }
        }

        private void PoolingUILayer(UILayer ui)
        {
            ui.CachedGo.SetActive(false);
            ui.CachedRectTr.SetParent(recycles, false);
            Type type = ui.GetType();
            if (!uiLayerPool.ContainsKey(type))
            {
                uiLayerPool.Add(type, new Queue<GameObject>());
            }

            uiLayerPool[type].Enqueue(ui.CachedGo);
        }

        public UILayer GetUILayer(string uiKey)
        {
            for (var i = 0; i < uiLayerStacks.Count; i++)
            {
                if (uiLayerStacks[i].Key == uiKey)
                {
                    return uiLayerStacks[i].Layer;
                }
            }

            return null;
        }

        public T GetUILayer<T>() where T : UILayer
        {
            for (var i = 0; i < uiLayerStacks.Count; i++)
            {
                if (uiLayerStacks[i].Layer is T layer)
                {
                    return layer;
                }
            }

            return null;
        }
        #endregion

        #region Control Canvas
        public void SetEnableMainNodeCanvas(bool isEnable)
        {
            mainNodeCanvas.enabled = isEnable;
        }

        public void SetEnableFloatingNodeCanvas(bool isEnable)
        {
            floatingNodeCanvas.enabled = isEnable;
        }
        #endregion

        #region Load UI from addressables
        private async UniTask<UILayer> LoadUILayer(Type uiType)
        {
            UILayerData sceneUILayerData = UIDataList[uiType];
            GameObject instance = await Addressables.InstantiateAsync(sceneUILayerData.AddressableName, mainNode).ToUniTask(cancellationToken: this.GetCancellationTokenOnDestroy());
            return instance.GetComponent<UILayer>();
        }

        public async UniTask PreloadUILayer(Type uiType)
        {
            UILayerData sceneUILayerData = UIDataList[uiType];
            GameObject instance = await Addressables.InstantiateAsync(sceneUILayerData.AddressableName, recycles).ToUniTask(cancellationToken: this.GetCancellationTokenOnDestroy());
            PoolingUILayer(instance.GetComponent<UILayer>());
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
                if (uiLayerStacks[i].State == UILayerState.Entering || uiLayerStacks[i].State == UILayerState.Exiting)
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
                    uiLayerStacks[index].Layer.OnBackButton(ref isOffPrevUI);
                    if (!isOffPrevUI)
                    {
                        break;
                    }

                    --index;
                }
            }
        }

        #region MainCanvas
        private void SetMainCanvas(GameObject canvasGo)
        {
            mainCanvas = canvasGo.GetComponent<Canvas>();
            mainCanvas.worldCamera = CameraManager.Main;

            Transform canvasTr = canvasGo.transform;
            mainNode = canvasTr.Find("MainNode");
            GameObject mainNodeGo = null;
            if (mainNode == null)
            {
                mainNodeGo = new GameObject("MainNode", typeof(RectTransform));
                var mainNodeRectTr = mainNodeGo.GetComponent<RectTransform>();
                mainNodeRectTr.SetParent(canvasTr, false);
                mainNodeRectTr.anchorMin = Vector2.zero;
                mainNodeRectTr.anchorMax = Vector2.one;
                mainNodeRectTr.anchoredPosition = Vector2.zero;
                mainNodeRectTr.pivot = new Vector2(0.5f, 0.5f);
                mainNodeRectTr.sizeDelta = Vector2.zero;
            }
            else
            {
                mainNodeGo = mainNode.gameObject;
            }

            if ((mainNodeCanvas = mainNodeGo.GetComponent<Canvas>()) == null)
            {
                mainNodeCanvas = mainNodeGo.AddComponent<Canvas>();
            }
            if (mainNodeGo.GetComponent<GraphicRaycaster>() == null)
            {
                mainNodeGo.AddComponent<GraphicRaycaster>();
            }

            floatingNode = canvasTr.Find("FloatingNode");
            GameObject floatingNodeGo = null;
            if (floatingNode == null)
            {
                floatingNodeGo = new GameObject("floatingNode", typeof(RectTransform));
                var floatingNodeRectTr = floatingNodeGo.GetComponent<RectTransform>();
                floatingNodeRectTr.SetParent(canvasTr, false);
                floatingNodeRectTr.anchorMin = Vector2.zero;
                floatingNodeRectTr.anchorMax = Vector2.one;
                floatingNodeRectTr.anchoredPosition = Vector2.zero;
                floatingNodeRectTr.pivot = new Vector2(0.5f, 0.5f);
                floatingNodeRectTr.sizeDelta = Vector2.zero;
            }
            else
            {
                floatingNodeGo = floatingNode.gameObject;
            }

            if ((floatingNodeCanvas = mainNodeGo.GetComponent<Canvas>()) == null)
            {
                floatingNodeCanvas = mainNodeGo.AddComponent<Canvas>();
            }
            if (floatingNodeGo.GetComponent<GraphicRaycaster>() == null)
            {
                floatingNodeGo.AddComponent<GraphicRaycaster>();
            }

            var mainCanvasScaler = mainCanvas.GetComponent<CanvasScaler>();
            var recycleCanvasScaler = recycles.GetComponent<CanvasScaler>();
            recycleCanvasScaler.referenceResolution = mainCanvasScaler.referenceResolution;
        }
        #endregion

        #region Dim Layer
        private Image CreateDimLayer(string name)
        {
            var dimLayerGo = new GameObject(name, typeof(RectTransform));
            var dimLayer = dimLayerGo.AddComponent<Image>();
            dimLayer.color = new Color(0f, 0f, 0f, 0f);
            dimLayer.sprite = Resources.Load<Sprite>("UI/Common/black");
            var dimLayerTr = dimLayerGo.GetComponent<RectTransform>();
            dimLayerTr.SetParent(mainNode, false);
            dimLayerTr.anchorMax = Vector2.one;
            dimLayerTr.anchorMin = Vector2.zero;
            dimLayerTr.sizeDelta = Vector2.zero;

            var btn = dimLayerGo.AddComponent<CAButton>();
            btn.onClick = new Button.ButtonClickedEvent();
            btn.transition = Selectable.Transition.None;
            btn.onClick.AddListener(OnClickDimLayer);

            // if (mainCanvas.renderMode == RenderMode.ScreenSpaceCamera)
            // {
            //     var boxColliderGo = new GameObject("boxCollider", typeof(RectTransform), typeof(BoxCollider));
            //     var boxColliderTr = boxColliderGo.GetComponent<RectTransform>();
            //     var boxCollider = boxColliderGo.GetComponent<BoxCollider>();
            //     boxColliderTr.SetParent(dimLayerTr, false);
            //     boxColliderTr.anchorMin = Vector2.zero;
            //     boxColliderTr.anchorMax = Vector2.one;
            //     boxColliderTr.sizeDelta = Vector2.zero;
            //     boxColliderTr.localScale = Vector3.one;
            //     boxColliderTr.localPosition = new Vector3(0, 0, 1);
            //     Vector2 canvasSize = boxColliderTr.rect.size;
            //     boxCollider.size = canvasSize;
            // }

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
    }
}
