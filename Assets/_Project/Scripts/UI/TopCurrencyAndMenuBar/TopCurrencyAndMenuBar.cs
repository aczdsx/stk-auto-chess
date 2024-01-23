using System;
using CookApps.TeamBattle.UIManagements;
using UnityEngine;
using UnityEngine.UI;

public class TopCurrencyAndMenuBar : UILayer
{
    private static int inc;

    public static void AddToUI(UILayer targetUI, TopPanelType[] ownPanelTypes)
    {
        SceneUIManager.Instance.RequestPushUIWithKey("TopCurrencyAndMenuBar", $"TopCurrencyAndMenuBar_{inc++}", (targetUI, ownPanelTypes));
    }

    [SerializeField] private RectTransform panelParent;
    [SerializeField] private LayoutGroup panelParentLayoutGroup;

    private TopPanelType[] usePanelFlags;
    private Vector2[] panelAnchoredPositions;
    public TopPanelType[] UsePanelFlags => usePanelFlags;

    private UILayer targetUI;

    public override void OnPreEnter(object param)
    {
        base.OnPreEnter(param);
        var data = ((UILayer, TopPanelType[])) param;
        targetUI = data.Item1;

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
            SceneUIManager.Instance.RequestPopUI(this);
        }
    }

    public override void OnBackButton(ref bool offPrevUI)
    {
        offPrevUI = true;
    }

    public void AddPanel(TopPanelType type, RectTransform panel)
    {
        int index = -1;
        for (var i = 0; i < usePanelFlags.Length; i++)
        {
            if (usePanelFlags[i] == type)
            {
                index = i;
                break;
            }
        }

        if (index == -1)
        {
            Debug.LogError($"Not found panel type: {type}");
            return;
        }

        panel.SetParent(panelParent, false);
        panel.SetSiblingIndex(index);
        if (panelAnchoredPositions != null && panelAnchoredPositions.Length > index)
        {
            panel.anchoredPosition = panelAnchoredPositions[index];
        }
    }

    public void ForceUpdateLayout()
    {
        panelParentLayoutGroup.enabled = true;
        LayoutRebuilder.ForceRebuildLayoutImmediate(panelParent);
        panelAnchoredPositions = new Vector2[panelParent.childCount];
        for (var i = 0; i < panelParent.childCount; i++)
        {
            panelAnchoredPositions[i] = panelParent.GetChild(i).GetComponent<RectTransform>().anchoredPosition;
        }

        panelParentLayoutGroup.enabled = false;
    }
}
