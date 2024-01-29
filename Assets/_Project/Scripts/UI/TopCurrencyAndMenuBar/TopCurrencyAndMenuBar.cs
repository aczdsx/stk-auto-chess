using System;
using CookApps.TeamBattle.UIManagements;
using UnityEngine;
using UnityEngine.UI;

public class TopCurrencyAndMenuBar : UILayer
{
    private static int inc;

    public static void AddToUILayer(UILayer targetUI, params TopPanelType[] ownPanelTypes)
    {
        SceneUIManager.Instance.PushUILayerWithKey("TopCurrencyAndMenuBar", $"TopCurrencyAndMenuBar_{inc++}", (targetUI, ownPanelTypes));
    }

    [SerializeField] private RectTransform panelParent;
    [SerializeField] private LayoutGroup panelParentLayoutGroup;

    private TopPanelType[] usePanelTypes;
    private Vector2[] panelAnchoredPositions;
    public TopPanelType[] UsePanelTypes => usePanelTypes;

    private UILayer targetUI;
    public UILayer TargetUI => targetUI;

    public override void OnPreEnter(object param)
    {
        base.OnPreEnter(param);
        (targetUI, usePanelTypes) = ((UILayer, TopPanelType[])) param;
        TopPanelSingleUseHelper.Instance.Push(this);
        SceneUIManager.OnUITransitionEvent += OnUITransitionEvent;
    }

    public override void OnPreExit()
    {
        base.OnPreExit();
        panelAnchoredPositions = null;
        TopPanelSingleUseHelper.Instance.Pop(this);
        SceneUIManager.OnUITransitionEvent -= OnUITransitionEvent;
    }

    private void OnUITransitionEvent(SceneUIManager.UITransition transaction, string uiKey, UILayer ui)
    {
        if (transaction == SceneUIManager.UITransition.Exiting && ui == targetUI)
        {
            SceneUIManager.Instance.PopUILayer(this);
        }
    }

    public override void OnBackButton(ref bool offPrevUI)
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
