using System;

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
        /// 화면 전체를 덮어 하위 UI를 모두 가리는 레이어 (하위UI는 모두 비활성화됨)
        Cover,
        /// 화면 전체를 덮지만 하위 UI를 가리지 않는 레이어
        Overlay,
        /// 다른 팝업이 떠있을 경우 모든 팝업을 비활성화하는 팝업, 하위에 딤드가 적용됨 
        Popup,
        /// 다른 팝업 위에 떠있을 수 있는 팝업, 하위에 딤드가 적용됨
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
        public UILayerStackData(string key, long inc, UILayer layer, UILayerState state, Action<object> closeCallback)
        {
            Key = key;
            Layer = layer;
            Layer.Key = key;
            State = state;
            CloseCallback = closeCallback;
            Inc = inc;
        }

        public readonly long Inc;
        public readonly string Key;
        public readonly UILayer Layer;

        public UILayerState State { get; set; }

        public readonly Action<object> CloseCallback;

        public static Comparison<UILayerStackData> SortByInc = (x, y) => (int) (x.Inc - y.Inc);
    }
    #endregion
}
