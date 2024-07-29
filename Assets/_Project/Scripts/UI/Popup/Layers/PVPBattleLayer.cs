using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cookapps.Stkauto.V1;
using CookApps.TeamBattle;
using CookApps.TeamBattle.UIManagements;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CookApps.AutoBattler
{
    public class PVPBattleLayer : CachedMonoBehaviour
    {
        [Header("Common")]
        [SerializeField] private GameObject _emptyLayerObject;  // 매칭리스트에 아무도 없을 경우 노출
        
        [Header("Refresh Maching List")]
        [SerializeField] private CAButton _matchRefreshButton;
        [SerializeField] private Image _matchRefreshItemImage;
        [SerializeField] private TextMeshProUGUI _matchRefreshItemAmountText;
        [SerializeField] private TextMeshProUGUI _matchRefreshRemainTimeText;
        
        [Header("Matching List")]
        [SerializeField] private ScrollRect _matchingScrollRect;
        [SerializeField] private GameObject _matchingSlotObject;
        
        private ArenaMainPopup _parentPopup;

        private UserPVP _currentUserPVPData;
        private List<UserPVPBattleSimpleData> _currentServerMatchingDataList;

        private void Awake()
        {
            _matchRefreshButton.onClick.AddListener(OnClickMatchingRefreshButton);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            
            _matchRefreshButton.onClick.RemoveListener(OnClickMatchingRefreshButton);
        }

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
            CreateMatchingScrollList();
            
            _emptyLayerObject?.SetActive(_currentServerMatchingDataList == null || _currentServerMatchingDataList.Count == 0);
        }
        
        private void CreateMatchingScrollList()
        {
            ClearLayer();

            _currentServerMatchingDataList = UserDataManager.Instance.GetPVPMatchingDataList();
            
            foreach (var matchData in _currentServerMatchingDataList)
            {
                GameObject newSlotObject = Instantiate(_matchingSlotObject, _matchingScrollRect.content);
                var matchingSlot = newSlotObject.GetComponent<ArenaBattleEnemySlot>();
                    
                matchingSlot?.InitSlot(matchData);
            }
        }
        
        private void OnClickMatchingRefreshButton()
        {
            _parentPopup?.RefreshTabLayer(ArenaMainPopupTabType.PVP_BATTLE);
        }
        
        private void ClearLayer()
        {
            BMUtil.RemoveChildObjects(_matchingScrollRect.content);
        }
    }
}