using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cookapps.Stkauto.V1;
using CookApps.TeamBattle;
using CookApps.TeamBattle.UIManagements;
using Tech.Hive.V1;
using UnityEngine;
using UnityEngine.UI;

namespace CookApps.AutoBattler
{
    public class PVPBattleLogLayer : CachedMonoBehaviour
    {
        [Header("Common")] [SerializeField] private GameObject _emptyLayerObject; // 로그리스트에 아무도 없을 경우 노출

        [Header("Log List")] [SerializeField] private ScrollRect _logScrollRect;
        [SerializeField] private GameObject _logSlotObject;

        private ArenaMainPopup _parentPopup;

        private UserPVP _currentUserPVPData;
        private List<PvpMatchHistoryData> _currentServerLogDataList = new();

        public void InitLayer(ArenaMainPopup parent)
        {
            _parentPopup = parent;

            _currentUserPVPData = UserDataManager.Instance.UserPVP;

            CreateLogScrollList();

            _emptyLayerObject?.SetActive(_currentServerLogDataList == null || _currentServerLogDataList.Count == 0);

            _logScrollRect.verticalNormalizedPosition = 1;
        }

        public void RefreshLayer()
        {
        }

        private void CreateLogScrollList()
        {
            ClearLayer();

            var logInfo = PVPManager.Instance.CurrentPVPHistoryListData;
            if (logInfo != null)
            {
                _currentServerLogDataList = logInfo.PvpMatchHistories.ToList();

                foreach (var logData in _currentServerLogDataList)
                {
                    var newSlotObject = Instantiate(_logSlotObject, _logScrollRect.content);
                    var logSlot = newSlotObject.GetComponent<ArenaBattleEnemySlot>();

                    logSlot?.InitBattleLogSlot(logData);
                }
            }
        }

        private void OnClickRevengeButton()
        {
        }

        private void ClearLayer()
        {
            _currentServerLogDataList.Clear();

            BMUtil.RemoveChildObjects(_logScrollRect.content);
        }
    }
}