using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cookapps.Stkauto.V1;
using CookApps.TeamBattle;
using UnityEngine;
using UnityEngine.UI;

namespace CookApps.AutoBattler
{
    public class PVPBattleLayer : CachedMonoBehaviour
    {
        [Header("Common")]
        [SerializeField] private GameObject _emptyLayerObject;  // 매칭리스트에 아무도 없을 경우 노출
        
        [Header("Matching List")]
        [SerializeField] private ScrollRect _matchingScrollRect;
        [SerializeField] private GameObject _matchingSlotObject;
        
        private ArenaMainPopup _parentPopup;

        private UserPVP _currentUserPVPData;
        private List<PvpMatchOpponentData> _currentServerMatchingDataList;
        
        public void InitLayer(ArenaMainPopup parent)
        {
            _parentPopup = parent;

            _currentUserPVPData = UserDataManager.Instance.UserPVP;
            
            CreateMatchingScrollList();
            
            _emptyLayerObject?.SetActive(_currentServerMatchingDataList == null || _currentServerMatchingDataList.Count == 0);
            
            _matchingScrollRect.verticalNormalizedPosition = 1;
        }
        
        public void RefreshLayer()
        {
            
        }
        
        private void CreateMatchingScrollList()
        {
            ClearLayer();

            var matchInfo = PVPManager.Instance.CurrentPVPMatchListData;
            if (matchInfo != null)
            {
                _currentServerMatchingDataList = matchInfo.PvpMatchOpponents.ToList();

                foreach (var rankData in _currentServerMatchingDataList)
                {
                    GameObject newSlotObject = Instantiate(_matchingSlotObject, _matchingScrollRect.content);
                    var matchingSlot = newSlotObject.GetComponent<ArenaBattleEnemySlot>();
                    
                    matchingSlot?.InitSlot();
                }
            }
        }
        
        public void ClearLayer()
        {
            BMUtil.RemoveChildObjects(_matchingScrollRect.content);
        }
    }
}