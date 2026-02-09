using System.Collections.Generic;
using CookApps.AutoBattler;
using CookApps.TeamBattle;
using CookApps.TeamBattle.UIManagements;
using CookApps.TeamBattle.Utility;
using Cysharp.Text;
using R3;
using R3.Triggers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;

public class ElpisCoreCategory : CachedMonoBehaviour
{
    [SerializeField] private Badge canUpgradeBadge;
    [SerializeField] private CAToggle toggle;
    private HashSet<int> _reuseableBadgeHashSet = new();

    public void Initialize(DimensionType dimensionType, List<CoreResearchCacheData> coreDataList, System.Action<DimensionType> onToggleClicked)
    {
        toggle.OnPointerClickAsObservable()
            .Subscribe(dimensionType, (_, type) => onToggleClicked?.Invoke(type));

        InitBadgePath(coreDataList);
    }

    private void InitBadgePath(List<CoreResearchCacheData> coreDataList)
    {
        canUpgradeBadge.Clear();
        _reuseableBadgeHashSet.Clear();
        
        foreach (var coreData in coreDataList)
        {
            if (!_reuseableBadgeHashSet.Add(coreData.Data.item_id))
                continue;

            canUpgradeBadge.AddBadgePath(BadgeType.RedDot, $"{ElpisCoreItem.BadgePathPrefix}/{coreData.Data.item_id}");
        }
    }
}