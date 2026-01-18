using CookApps.TeamBattle.UIManagements;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace CookApps.AutoBattler
{
    public interface TopCurrencyAndMenuBarContainer
    {
        Transform GetTopCurrencyAndMenuBarParent();
    }
    
    public class TopCurrencyAndMenuBar : UILayer
    {
        private static int inc;

        public static void AddToUILayer(UILayer targetUI, params TopPanelType[] ownPanelTypes)
        {
            SceneUILayerManager.Instance.PushUILayerAsync<TopCurrencyAndMenuBar>($"TopCurrencyAndMenuBar_{inc++}", (targetUI, ownPanelTypes)).Forget();
        }

        [SerializeField] private RectTransform panelParent;
        [SerializeField] private LayoutGroup panelParentLayoutGroup;

        private TopPanelType[] usePanelTypes;
        private Vector2[] panelAnchoredPositions;
        public TopPanelType[] UsePanelTypes => usePanelTypes;

        private UILayer targetUI;
        public UILayer TargetUI => targetUI;

        private Transform cachedPerentTr;

        protected override void OnPreEnter(object param)
        {
            base.OnPreEnter(param);
            (targetUI, usePanelTypes) = ((UILayer, TopPanelType[])) param;
            TopPanelSingleUseHelper.Instance.Push(this);
            if (targetUI is TopCurrencyAndMenuBarContainer container)
            {
                cachedPerentTr = panelParent.parent;
                panelParent.SetParent(container.GetTopCurrencyAndMenuBarParent(), false);
            }
            SceneUILayerManager.OnUITransitionEvent += OnUITransitionEvent;
        }

        protected override void OnPreExit()
        {
            base.OnPreExit();
            panelAnchoredPositions = null;
            if (cachedPerentTr != null)
            {
                panelParent.SetParent(cachedPerentTr, false);
            }
            cachedPerentTr = null;
            TopPanelSingleUseHelper.Instance.Pop(this);
            SceneUILayerManager.OnUITransitionEvent -= OnUITransitionEvent;
        }

        private void OnUITransitionEvent(UILayerTransition transaction, string uiKey, UILayer ui, object param)
        {
            if (transaction == UILayerTransition.Exiting && ui == targetUI)
            {
                SceneUILayerManager.Instance.PopUILayer(this);
            }
        }

        protected override void OnBackButton(ref bool offPrevUI)
        {
            offPrevUI = true;
        }

        public void AddPanel(TopPanelType type, RectTransform panel)
        {
            panel.SetParent(panelParent, false);
            if (panelAnchoredPositions == null)
            {
                return;
            }

            for (var i = 0; i < usePanelTypes.Length; i++)
            {
                if (usePanelTypes[i] == type)
                {
                    panel.anchoredPosition = panelAnchoredPositions[i];
                    break;
                }
            }
        }

        public void ForceUpdateLayout()
        {
            panelParentLayoutGroup.enabled = true;
            for (var i = 0; i < usePanelTypes.Length; i++)
            {
                for (var j = 0; j < panelParent.childCount; j++)
                {
                    var panel = panelParent.GetChild(j).GetComponent<TopPanelBase>();
                    if (panel.PanelType == usePanelTypes[i])
                    {
                        panel.CachedTr.SetAsLastSibling();
                        break;
                    }
                }
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(panelParent);
            panelAnchoredPositions = new Vector2[panelParent.childCount];
            for (var i = 0; i < panelParent.childCount; i++)
            {
                panelAnchoredPositions[i] = panelParent.GetChild(i).GetComponent<TopPanelBase>().CachedRectTr.anchoredPosition;
            }

            panelParentLayoutGroup.enabled = false;
        }
    }
}
