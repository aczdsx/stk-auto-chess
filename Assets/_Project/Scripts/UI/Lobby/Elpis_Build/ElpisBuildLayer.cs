using System;
using System.Collections.Generic;
using CookApps.TeamBattle.UIManagements;
using CookApps.TeamBattle.UI;
using UnityEngine;
using TMPro;
using Cysharp.Threading.Tasks;
using R3;

namespace CookApps.AutoBattler
{
    public class ElpisBuildLayer : UILayer
    {
        [Header("UI Components")]
        [SerializeField] private CAButton closeButton;
        [SerializeField] private Transform contentRoot;
        [SerializeField] private ElpisBuildCell cellPrefab;
        [SerializeField] private TMP_Text installingTimeText;
        [SerializeField] private GameObject installingTimeTextParent;

        private List<ElpisBuildCell> cells = new();
        private ElpisBuildCacheData cachedData;

        public class ElpisBuildCacheData
        {
            public List<LobbyBuildingInteractionUI.FacilityInfo> facilityInfos;
            public LobbyBuildingInteractionUI targetLobbyBuildingUI;
            public int slotIndex;
        }

        protected override void OnPreEnter(object param)
        {
            base.OnPreEnter(param);
            cachedData = param as ElpisBuildCacheData;
            cachedData.targetLobbyBuildingUI.SetCurrentBuildLayer(this);
            RefreshUI();
            LobbyMain.GetLobbyMain().PlayExitAnimation();
        }

        protected override void OnPreExit()
        {
            base.OnPreExit();
            cachedData?.targetLobbyBuildingUI?.SetCurrentBuildLayer(null);
            LobbyMain.GetLobbyMain().PlayEnterAnimation();
        }

        protected override void Awake()
        {
            base.Awake();

            closeButton
                .OnClickAsObservable()
                .Subscribe(this, (_, self) => self.OnCloseClicked())
                .AddTo(this);
        }

        public void RefreshUI()
        {
            var isContainInstalling = IsContainsInstalling();
            installingTimeTextParent.SetActive(isContainInstalling);

            if (isContainInstalling)
            {
                var remainingTime = cachedData.targetLobbyBuildingUI.GetCurrentRemainingTime();
                if (remainingTime.HasValue)
                {
                    UpdateRemainingTime(remainingTime.Value);
                }
            }

            PopulateBuildList();
        }

        public void UpdateRemainingTime(TimeSpan remainingTime)
        {
            installingTimeText.text = remainingTime.ToString(@"mm\:ss");
        }

        private bool IsContainsInstalling()
        {
            foreach (var facilityInfo in cachedData.facilityInfos)
            {
                if (facilityInfo.isInstalling)
                    return true;
            }
            return false;
        }

        private void PopulateBuildList()
        {
            foreach (var cell in cells)
            {
                Destroy(cell.gameObject);
            }
            cells.Clear();

            foreach (var facilityInfo in cachedData.facilityInfos)
            {
                var newCell = Instantiate(cellPrefab, contentRoot);
                newCell.SetData(facilityInfo, OnInstallRequested);
                cells.Add(newCell);
            }
            
            for (int i = 0; i < cells.Count; i++)
            {
                cells[i].transform.SetSiblingIndex(i);
            }
        }

        private void OnInstallRequested(ElpisBuildInfo info)
        {
            InstallBuilding(info).Forget();
        }

        private async UniTaskVoid InstallBuilding(ElpisBuildInfo info)
        {
            if (cachedData.slotIndex < 0)
            {
                Debug.LogError("Slot Index is invalid!");
                return;
            }
            
            //TODO : 업그레이드인지 설치인지 판별 필요.
            
            if (info.build_lv > 1)
            {
                var result = await NetManager.Instance.Elpis.UpgradeFacilityAsync(info.build_id);
                if (result != null && result.IsSuccess)
                {
                    cachedData.targetLobbyBuildingUI.ChangeInfo(result.Facility);
                    CloseThisUILayer();
                }
                else
                {
                    Debug.LogError($"건물 설치 실패: {info.buld_name_token}");
                }
            }
            else
            {
                var result = await NetManager.Instance.Elpis.BuildFacilityAsync(info.build_id, cachedData.slotIndex, 0);
                
                if (result != null && result.IsSuccess)
                {
                    cachedData.targetLobbyBuildingUI.ChangeInfo(result.Facility);
                    CloseThisUILayer();
                }
                else
                {
                    Debug.LogError($"건물 설치 실패: {info.buld_name_token}");
                }
            }
        }

        private void OnCloseClicked()
        {
            SceneUILayerManager.Instance.PopUILayer(this);
        }
    }
}
