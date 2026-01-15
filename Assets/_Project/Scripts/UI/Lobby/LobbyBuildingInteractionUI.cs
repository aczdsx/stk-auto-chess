using System;
using System.Collections;
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

        private UnityEngine.Coroutine checkInstallingTimeCoroutine;
        private ElpisBuildLayer currentBuildLayer;

        public class FacilityInfo
        {
            public ElpisBuildInfo buildInfo;

            public bool isInstalled;
            public bool isCanInstall;
            public bool isInstalling;
            public bool isJustCompleted;
            public bool isPreviousLevelRequired;
            public bool isAnotherBuilding;
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
        
        public void UpdateUI()
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

        public void ChangeInfo(ElpisFacility facility)
        {
            if (facility == null)
                return;

            facilityData = facility;
            buildInfo = SpecDataManager.Instance.GetBuildInfo((int)facilityData.BuildId);

            UpdateFacilityInfos();
            UpdateUI();
            StartInstallingTimer();
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

            // CanBuild 계산에 필요한 값들 미리 캐싱
            var commandCenterLv = ElpisDataBridge.GetFacilityByType(ElpisFacilityType.FacilityTypeCommandCenter).Level;
            var allBenefits = SpecDataManager.Instance.ElpisCommandCenterBenefit.All;

            // 건설 중인 건물이 있는지 체크 (같은 타입 내에서)
            var hasAnyBuilding = false;
            for (var i = 0; i < sameFacilities.Count; i++)
            {
                var specFacility = ElpisDataBridge.GetFacility((uint)sameFacilities[i].build_id);
                if (specFacility != null && specFacility.IsBuilding)
                {
                    hasAnyBuilding = true;
                    break;
                }
            }

            for (var i = 0; i < sameFacilities.Count; i++)
            {
                var spec = sameFacilities[i];
                var specLevel = spec.build_lv;

                // 해당 build_id의 서버 데이터 가져오기
                var specFacilityData = ElpisDataBridge.GetFacility((uint)spec.build_id);
                var specCurrentLevel = specFacilityData?.Level ?? 0;
                var isSpecBuilding = specFacilityData?.IsBuilding ?? false;
                var isSpecJustCompleted = specFacilityData?.IsJustCompleted ?? false;
                var specCompletionTime = specFacilityData?.BuildCompleteTime ?? DateTime.MinValue;

                var isInstalled = false;
                var isInstalling = false;
                var isJustCompleted = false;
                var completionTime = DateTime.MinValue;

                if (specLevel < specCurrentLevel)
                {
                    isInstalled = true;
                }
                else if (specLevel == specCurrentLevel)
                {
                    if (isSpecBuilding)
                    {
                        isInstalling = true;
                        completionTime = specCompletionTime;
                    }
                    else if (isSpecJustCompleted)
                    {
                        isJustCompleted = true;
                    }
                    else
                    {
                        isInstalled = specCurrentLevel > 0;
                    }
                }

                // 이전 레벨이 설치되지 않은 경우 (specLevel > specCurrentLevel + 1)
                var isPreviousLevelRequired = specLevel > specCurrentLevel + 1;
                // 다른 건물이 건설 중인 경우
                var isAnotherBuilding = hasAnyBuilding && !isInstalling;
                var canBuild = !isInstalled && !isInstalling && !isPreviousLevelRequired && !isAnotherBuilding && CheckCanBuild(spec, commandCenterLv, allBenefits);

                cachedFacilityInfos.Add(new FacilityInfo
                {
                    buildInfo = spec,
                    isInstalled = isInstalled,
                    isInstalling = isInstalling,
                    isJustCompleted = isJustCompleted,
                    isPreviousLevelRequired = isPreviousLevelRequired,
                    isAnotherBuilding = isAnotherBuilding,
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

            if (facilityData.BuiltAt == 0)
                return false;

            completionTime = facilityData.BuildCompleteTime;

            if (facilityData.IsBuilding)
                return true;

            isJustCompleted = facilityData.IsJustCompleted;
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

        #region Installing Timer

        private void StartInstallingTimer()
        {
            StopInstallingTimer();

            var installingFacility = GetInstallingFacility();
            if (installingFacility != null)
            {
                checkInstallingTimeCoroutine = StartCoroutine(CheckInstallingTime(installingFacility));
            }
        }

        private void StopInstallingTimer()
        {
            if (checkInstallingTimeCoroutine != null)
            {
                StopCoroutine(checkInstallingTimeCoroutine);
                checkInstallingTimeCoroutine = null;
            }
        }

        private FacilityInfo GetInstallingFacility()
        {
            foreach (var info in cachedFacilityInfos)
            {
                if (info.isInstalling)
                    return info;
            }
            return null;
        }

        private IEnumerator CheckInstallingTime(FacilityInfo installingFacility)
        {
            var completionTime = installingFacility.completionTime;
            var currentTime = TimeManager.Instance.UtcNow();

            // completionTime이 유효하지 않거나 이미 과거면 종료
            if (completionTime == DateTime.MinValue || currentTime >= completionTime)
            {
                yield break;
            }

            while (installingFacility.isInstalling && currentTime < completionTime)
            {
                var remainingTime = completionTime - currentTime;

                currentBuildLayer?.UpdateRemainingTime(remainingTime);

                yield return null;

                currentTime = TimeManager.Instance.UtcNow();
            }

            // 타이머 완료 처리
            if (installingFacility.isInstalling)
            {
                OnInstallTimerCompleted(installingFacility);
            }
        }

        private void OnInstallTimerCompleted(FacilityInfo installingFacility)
        {
            installingFacility.isInstalling = false;
            installingFacility.isJustCompleted = true;

            UpdateUI();
            currentBuildLayer?.RefreshUI();
        }

        public void SetCurrentBuildLayer(ElpisBuildLayer layer)
        {
            currentBuildLayer = layer;
        }

        public TimeSpan? GetCurrentRemainingTime()
        {
            var installingFacility = GetInstallingFacility();
            if (installingFacility == null)
                return null;

            var currentTime = TimeManager.Instance.UtcNow();
            return installingFacility.completionTime - currentTime;
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
                ElpisFacility changedInfo = null;

                if (facilityData.Level > 1)
                {
                    var response = await NetManager.Instance.Elpis.FinishUpgradingFacilityAsync((int)facilityData.BuildId);
                    if (response != null && response.IsSuccess)
                    {
                        changedInfo = response.Facility;
                    }

                    //TODO : 연출
                }
                else
                {
                    var response = await NetManager.Instance.Elpis.FinishBuildingFacilityAsync((int)facilityData.BuildId);
                    if (response != null && response.IsSuccess)
                    {
                        changedInfo = response.Facility;
                    }

                    //TODO : 연출
                }

                if (changedInfo != null)
                {
                    ChangeInfo(changedInfo);
                }

                return;
            }

            if (isInstalling || isCanInstall)
            {
                var newParam = new ElpisBuildLayer.ElpisBuildCacheData
                {
                    facilityInfos = cachedFacilityInfos,
                    targetLobbyBuildingUI = this,
                    slotIndex = buildInfo.slot_index
                };
                
                SceneUILayerManager.Instance.PushUILayerAsync<ElpisBuildLayer>(newParam).Forget(); //TODO : param 변경 필요
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
