using CookApps.AutoBattler;
using CookApps.TeamBattle;
using CookApps.TeamBattle.UIManagements;
using CookApps.TeamBattle.Utility;
using Cysharp.Text;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;

public class ElpisCoreItem : CachedMonoBehaviour
{
    [SerializeField] private CAButton button;
    [SerializeField] private SimpleGameObjectActiveSwapper selectedSwapper;
    [SerializeField] private Image[] iconImages;
    [SerializeField] private Image[] highlightImages;
    [SerializeField] private Gradient2 highlightGradient; 
    [SerializeField] private TMP_Text[] levelTexts;
    [SerializeField] private SimpleImageSwapper[] iconSwappers;
    [SerializeField] private Badge canUpgradeBadge;

    private CoreResearchCacheData cachedData;
    private ElpisCoreResearchLayer dimensionLabPopup;
    public static readonly string BadgePathPrefix = "CoreResearch";
    
    public ElpisDimensionLab Data => cachedData.Data;
    public CoreResearchCacheData CachedData => cachedData;
    
    private bool isSelected;

    public bool IsSelected
    {
        get => isSelected;
        set
        {
            isSelected = value;
            selectedSwapper.Swap(isSelected ? SimpleSwapType.Selected : SimpleSwapType.Normal);
        }
    }

    public void SetHighlight(bool isHighlight)
    {
        IsSelected = isHighlight;
    }

    private void SetIcon()
    {
        var currentType = Data.core_research_type;

        if (currentType == CoreResearchType.NONE)
            return;

        var swapType = (SimpleSwapType)((int)SimpleSwapType.Custom_0 + (int)currentType - 1);
        foreach (var iconSwapper in iconSwappers)
            iconSwapper.Swap(swapType);
    }

    private void Awake()
    {
        button.OnClickAsObservable()
            .Subscribe(this, (_, self) => self.OnClick())
            .AddTo(this);
    }

    private void SetLevelText()
    {
        if (cachedData.IsMax)
        {
            foreach (var levelText in levelTexts)
            {
                levelText.text = "MAX";
            }
            return;
        }

        foreach (var levelText in levelTexts)
        {
            levelText.text = ZString.Format("Lv.{0}", cachedData.Data.lv - 1);
        }
    }

    public void UpdateData(CoreResearchCacheData cachedData)
    {
        this.cachedData = cachedData;

        SetLevelText();
    }

    public void InitBadgePath()
    {
        canUpgradeBadge.Clear();
        canUpgradeBadge.AddBadgePath(BadgeType.RedDot, $"{BadgePathPrefix}/{cachedData.Data.item_id}");
    }

    public void SetUp(CoreResearchCacheData cachedData, ElpisCoreResearchLayer dimensionLabPopup)
    {
        this.cachedData = cachedData;
        this.dimensionLabPopup = dimensionLabPopup;

        SetHighlightColor();
        SetLevelText();
        SetIcon();
        InitBadgePath();

        IsSelected = false;
    }

    private void SetHighlightColor()
    {
        dimensionLabPopup.iconColors.TryGetValue(cachedData.Data.core_research_type, out var highlightColor);
        dimensionLabPopup.iconGradients.TryGetValue(cachedData.Data.core_research_type, out var gradient);

        foreach (var highlightImage in highlightImages)
            highlightImage.color = highlightColor;

        levelTexts[1].color = highlightColor;
        highlightGradient.EffectGradient = gradient;
    }

    private void OnClick()
    {
        if(isSelected)
            return;
        
        dimensionLabPopup.CoreSelected(this);
    }
}