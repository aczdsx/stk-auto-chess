using System;
using System.Collections.Generic;
using UnityEngine;

namespace CookApps.TeamBattle.UIManagements
{
    #region enum
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

    internal enum UILayerState
    {
        Initialized,
        Entering,
        Entered,
        Exiting,
        Hiding, // use only popup
    }
    #endregion

    #region DataStructures
    [Serializable]
    internal class UILayerStackData
    {
        public UILayerStackData(Type layerType, string key, long inc, UILayer layer, UILayerState state, Action<object> closeCallback)
        {
            LayerType = layerType;
            Key = key;
            Layer = layer;
            Layer.Key = key;
            State = state;
            CloseCallback = closeCallback;
            Inc = inc;
        }

        public readonly long Inc;
        public readonly Type LayerType;
        public readonly string Key;
        public readonly UILayer Layer;

        public UILayerState State { get; private set; }

        public void SetState(UILayerState state)
        {
            State = state;
        }

        public readonly Action<object> CloseCallback;

        public static Comparison<UILayerStackData> SortByInc = (x, y) => (int) (x.Inc - y.Inc);
    }

    [Serializable]
    public class UILayerData
    {
        public readonly UILayerType LayerType;
        public readonly string AddressableName;

        public UILayerData(UILayerType layerType, string addressableName)
        {
            LayerType = layerType;
            AddressableName = addressableName;
        }
    }

    [Serializable]
    public class SceneData
    {
        [SerializeField] private string sceneName;
        public string SceneName => sceneName;
        [SerializeField] private string addressableName;
        public string AddressableName => addressableName;
        private List<Type> defaultUILayers = new List<Type>();
        internal void AddDefaultUILayer(Type uiType) => defaultUILayers.Add(uiType);
        public IReadOnlyList<Type> DefaultUILayers => defaultUILayers;
    }
    #endregion
}
