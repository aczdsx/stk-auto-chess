using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CookApps.TeamBattle;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace CookApps.AutoBattler
{
    public class PVPSeasonRewardLayer : CachedMonoBehaviour
    {
        [Header("Season Info List")]
        [SerializeField] private ScrollRect _seasonInfoScrollRect;
        [SerializeField] private GameObject _seasonInfoSlotObject;
        
        private ArenaMainPopup _parentPopup;

        public void InitLayer(ArenaMainPopup parent)
        {
            _parentPopup = parent;

            CreateSeasonScrollList();
        }
        
        public void RefreshLayer()
        {
            
        }

        private void CreateSeasonScrollList()
        {
            ClearLayer();

            var tierDataList = SpecDataManager.Instance.GetPVPTierDataList(RankingType.SCORE);
            tierDataList = tierDataList.OrderBy(data => data.order).ToList();
            
            foreach (var tierData in tierDataList)
            {
                GameObject newSlotObject = Instantiate(_seasonInfoSlotObject, _seasonInfoScrollRect.content);
                var seasonInfoSlot = newSlotObject.GetComponent<ArenaSeasonRewardSlot>();
                    
                seasonInfoSlot?.InitSlot(tierData);
            }

            _seasonInfoScrollRect.horizontalNormalizedPosition = 0;
        }
        
        public void ClearLayer()
        {
            BMUtil.RemoveChildObjects(_seasonInfoScrollRect.content);
        }
    }
}