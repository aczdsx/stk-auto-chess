using System;
using System.Collections.Generic;
using CookApps.TeamBattle;
using CookApps.TeamBattle.UIManagements;
using CookApps.TeamBattle.Utility;
using Cysharp.Threading.Tasks;
using R3;
using Tech.Hive.V1;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CookApps.AutoBattler
{
    public class LobbyBuildingInteractionUI : CachedMonoBehaviour
    {
        [SerializeField] private SimpleSwapper iconSwapper;
        [SerializeField] private TMP_Text buildingName;
        [SerializeField] private CAButton button;
        
        private ElpisBuildingBase target;
        private ElpisFacility facilityData;
        private ElpisBuildInfo buildInfo;
        private RectTransform parentRect;
        private bool isInitialize;

        private ElpisDataBridge ElpisDataBridge;
        private readonly List<FacilityInfo> cachedFacilityInfos = new();
        
        private bool isLocked;

        public class FacilityInfo
        {
            public ElpisBuildInfo buildInfo;

            public bool isInstalled;
            public bool isCanInstall;
            public bool isInstalling;
            public bool isJustCompleted;
            public DateTime completionTime;
        }

        private void Awake()
        {
            button
                .OnClickAsObservable()
                .SubscribeAwait(this, (_, self, _) => self.OnClick())
                .AddTo(this);

            ElpisDataBridge = new ElpisDataBridge();
        }
        
        private void LateUpdate()
        {
            if (!isInitialize)
            {
                CachedGo.SetActive(false);
                return;
            }

            UpdatePosition();
        }
        
        public void Initialize(ElpisBuildingBase target, ElpisFacility facilityData)
        {
            parentRect = CachedTr.parent.GetComponent<RectTransform>();
            isInitialize = true;
            
            SetData(target, facilityData);
            
            UpdatePosition();

            buildingName.text = buildInfo.facility_type.ToString(); //TODO : localization
        }

        #region Set
        
        private void UpdateUI()
        {
            var (isInstalling, isInstallFinished, isCanInstall, isFacilityLocked) = GetFacilityStatus();
            var swapType = isInstalling ? SimpleSwapType.Custom_0
                : isInstallFinished ? SimpleSwapType.Custom_1
                : isCanInstall ? SimpleSwapType.Custom_2
                : isFacilityLocked ? SimpleSwapType.Custom_3
                : SimpleSwapType.Disabled;

            if (swapType == SimpleSwapType.Disabled)
            {
                iconSwapper.gameObject.SetActive(false);
            }
            else
            {
                iconSwapper.gameObject.SetActive(true);
                iconSwapper.Swap(swapType);
            }
        }

        #endregion

        #region FacilityInfo

        private void ChangeInfo(ElpisFacility facility)
        {
            facilityData = facility;
            buildInfo = SpecDataManager.Instance.GetBuildInfo((int)facilityData.BuildId);
            
            UpdateFacilityInfos();
            UpdateUI();
        }

        private void SetData(ElpisBuildingBase target, ElpisFacility facilityData)
        {
            this.target = target;
            
            ChangeInfo(facilityData);
        }

        private (bool isInstalling, bool isInstallFinished, bool isCanInstall, bool isLocked) GetFacilityStatus()
        {
            var hasInstalling = false;
            var hasInstallFinished = false;
            var hasCanInstall = false;

            for (var i = 0; i < cachedFacilityInfos.Count; i++)
            {
                var info = cachedFacilityInfos[i];
                if (info.isInstalling)
                    hasInstalling = true;
                if (info.isJustCompleted)
                    hasInstallFinished = true;
                if (info.isCanInstall)
                    hasCanInstall = true;
            }

            return (hasInstalling, hasInstallFinished, hasCanInstall, isLocked);
        }

        private void UpdateFacilityInfos()
        {
            ElpisDataBridge ??= new ElpisDataBridge();
            cachedFacilityInfos.Clear();

            var sameFacilities = SpecDataManager.Instance.GetSameFacilityTypes(buildInfo.facility_type);
            var currentLevel = facilityData?.Level ?? 0;
            var isCurrentlyBuilding = CheckBuildingStatus(out var buildingCompletionTime, out var isBuildingJustCompleted);

            // CanBuild 계산에 필요한 값들 미리 캐싱
            var commandCenterLv = ElpisDataBridge.GetFacilityByType(ElpisFacilityType.FacilityTypeCommandCenter).Level;
            var allBenefits = SpecDataManager.Instance.ElpisCommandCenterBenefit.All;

            for (var i = 0; i < sameFacilities.Count; i++)
            {
                var spec = sameFacilities[i];
                var specLevel = spec.build_lv;

                var isInstalled = false;
                var isInstalling = false;
                var isJustCompleted = false;
                var completionTime = DateTime.MinValue;

                if (specLevel < currentLevel)
                {
                    isInstalled = true;
                }
                else if (specLevel == currentLevel)
                {
                    if (isCurrentlyBuilding)
                    {
                        isInstalling = true;
                        completionTime = buildingCompletionTime;
                    }
                    else if (isBuildingJustCompleted)
                    {
                        isJustCompleted = true;
                    }
                    else
                    {
                        isInstalled = currentLevel > 0;
                    }
                }

                var canBuild = !isInstalled && !isInstalling && CheckCanBuild(spec, commandCenterLv, allBenefits);

                cachedFacilityInfos.Add(new FacilityInfo
                {
                    buildInfo = spec,
                    isInstalled = isInstalled,
                    isInstalling = isInstalling,
                    isJustCompleted = isJustCompleted,
                    completionTime = completionTime,
                    isCanInstall = canBuild
                });
            }

            // 모든 시설이 설치 불가능하면 isLocked = true
            isLocked = true;
            for (var i = 0; i < cachedFacilityInfos.Count; i++)
            {
                var info = cachedFacilityInfos[i];
                if (info.isCanInstall || info.isInstalling || info.isJustCompleted || info.isInstalled)
                {
                    isLocked = false;
                    break;
                }
            }
        }

        private bool CheckBuildingStatus(out DateTime completionTime, out bool isJustCompleted)
        {
            completionTime = DateTime.MinValue;
            isJustCompleted = false;

            if (facilityData == null)
                return false;

            var buildTime = facilityData.BuiltAt;
            if (buildTime == 0)
                return false;

            completionTime = TimeManager.Instance.TimeStampToDateTime((long)buildTime / 1000);
            var now = TimeManager.Instance.UtcNow();

            if (now < completionTime)
                return true;

            isJustCompleted = true;
            return false;
        }

        private bool CheckCanBuild(ElpisBuildInfo targetBuildInfo, uint commandCenterLv, IReadOnlyList<ElpisCommandCenterBenefit> allBenefits)
        {
            var targetBuildId = targetBuildInfo.build_id;

            for (var i = 0; i < allBenefits.Count; i++)
            {
                var benefit = allBenefits[i];
                if (benefit.build_id != targetBuildId || benefit.lv > commandCenterLv)
                    continue;

                var benefitType = benefit.benefit_type;

                if (benefitType == BenefitType.BUILDING || benefitType == BenefitType.MULTI_BUILDING)
                {
                    var serverBuild = ElpisDataBridge.GetFacility((uint)benefit.build_id);
                    if (serverBuild == null || serverBuild.Level <= 0)
                        return true;
                }
                else if (benefitType == BenefitType.MAX_LEVEL_UP)
                {
                    var serverBuild = ElpisDataBridge.GetFacility((uint)benefit.build_id);
                    if (benefit.benefit_key > (serverBuild?.Level ?? 0))
                        return true;
                }
            }

            return false;
        }

        #endregion

        #region Effects

        private void UpdatePosition()
        {
            CachedRectTr.anchoredPosition = MainCameraHolder.WorldPointToLocalPointInRectangle(target.CachedTr.position, parentRect);
        }

        private void CameraFocus()
        {
            var cameraController = MainCameraHolder.CameraGestureController;
            var targetFacilityPosition = target.CachedTr.position;
            var targetZoom = 10.0f;

            cameraController.ZoomAndMoveAsync(targetFacilityPosition, targetZoom, 0.3f).Forget();
        }

        #endregion

        #region OnClick

        private async UniTask OnClick()
        {
            CameraFocus();
            
            var (isInstalling, isInstallFinished, isCanInstall, isFacilityLocked) = GetFacilityStatus();
            
            if(isFacilityLocked)
                return;

            if (isInstallFinished)
            {
                var changedInfo = new ElpisFacility();
                
                if (facilityData.Level > 1)
                {
                    var response = await NetManager.Instance.Elpis.FinishUpgradingFacilityAsync((int)facilityData.BuildId);
                    changedInfo = response.Facility;
                    
                    //TODO : 연출
                }
                else
                {
                    var response = await NetManager.Instance.Elpis.FinishBuildingFacilityAsync((int)facilityData.BuildId);
                    changedInfo = response.Facility;
                    
                    //TODO : 연출
                }
                
                ChangeInfo(changedInfo);
                
                return;
            }

            if (isInstalling || isCanInstall)
            {
                SceneUILayerManager.Instance.PushUILayerAsync<ElpisBuildLayer>((facilityData, target.SlotIndex)).Forget(); //TODO : param 변경 필요
            }
            else
            {
                if (facilityData.Level <= 0)
                    return;
                
                var buildingLayer = SceneUILayerManager.Instance.GetUILayer<ElpisBuildLayer>();
                if (buildingLayer != null)
                    SceneUILayerManager.Instance.PopUILayer(buildingLayer);

                ElpisBuildingPopup.OpenPopup(facilityData).Forget();
            }
        }

        #endregion
    }
}
