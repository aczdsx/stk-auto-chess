using System.Collections;
using System.Collections.Generic;
using CookApps.TeamBattle;
using CookApps.TeamBattle.UIManagements;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CookApps.AutoBattler
{
    public class PVPMyInfoLayer : CachedMonoBehaviour
    {
        [SerializeField] private Image _myTierImage;
        [SerializeField] private Slider _myTierSlider;
        [SerializeField] private TextMeshProUGUI _myTierNameText;
        [SerializeField] private List<GameObject> _myTierLevelObjectList;
        
        [SerializeField] private TextMeshProUGUI _myRankingText;
        [SerializeField] private TextMeshProUGUI _myRankingPointText;
        [SerializeField] private TextMeshProUGUI _myBattlePointText;

        [Space(10)] 
        [SerializeField] private List<RewardItemSlot> _mySeasonRewardItemSlotList;

        [Space(10)] 
        [SerializeField] private CAButton _settingDefenseDeckButton;
    }
}