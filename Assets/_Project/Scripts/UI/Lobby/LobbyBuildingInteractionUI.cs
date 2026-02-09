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
using UniTask = Cysharp.Threading.Tasks.UniTask;

namespace CookApps.AutoBattler
{
    public class LobbyBuildingInteractionUI : CachedMonoBehaviour
    {
        [SerializeField] private SimpleSwapper iconSwapper;
        [SerializeField] private TMP_Text buildingName;
        [SerializeField] private CAButton button;
        [SerializeField] private CAButton nameTagButton;
        [SerializeField] private TutorialTarget tutorialTarget;

        [Header("줌 관련")] [SerializeField] private RectTransform statusIcon;
        [SerializeField] private CanvasGroup lobbyNameTag;
        [SerializeField] private Badge canUpgradeBadge;

        private const float DisappearZoomRatio = 0.8f;
        private const float AlphaFadeSpeed = 5f;

        private Vector2 statusIconDefaultSize;
        private float targetNameTagAlpha = 1f;

        private ElpisBuildingBase target;
        private ElpisFacility facilityData;
        private ElpisBuildInfo buildInfo;
        private RectTransform parentRect;
        private bool isInitialize;

        private GuideMissionDataBridge GuideMissionDataBridge;

        private ElpisDataBridge ElpisDataBridge;
        private readonly List<FacilityInfo> cachedFacilityInfos = new();

        private bool isLocked;

        public Vector3 TargetWorldPosition => target != null ? target.CachedTr.position : Vector3.zero;

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

            nameTagButton
                .OnClickAsObservable()
                .SubscribeAwait(this, (_, self, _) => self.OnClick())
                .AddTo(this);

            ElpisDataBridge = new ElpisDataBridge();
            GuideMissionDataBridge = new GuideMissionDataBridge();
        }

        private void LateUpdate()
        {
            if (!isInitialize)
            {
                CachedGo.SetActive(false);
                return;
            }

            UpdatePosition();
            UpdateZoomBasedUI();
        }

        public void Initialize(ElpisBuildingBase target, ElpisFacility facilityData)
        {
            parentRect = CachedTr.parent.GetComponent<RectTransform>();
            isInitialize = true;

            // statusIcon 기본 크기 저장
            if (statusIcon != null)
            {
                statusIconDefaultSize = statusIcon.sizeDelta;
            }

            SetData(target, facilityData);

            UpdatePosition();

            buildingName.text = LanguageManager.Instance.GetDefaultText(buildInfo.buld_name_token);
            tutorialTarget.SetTargetId($"Building_{buildInfo.facility_type}");
            InitializeByFacilityType();
        }

        private void InitializeByFacilityType()
        {
            if (buildInfo.facility_type is not FacilityType.DIMENSION_LAB) return;

            var specElpisDimensionLabs = SpecDataManager.Instance.GetAllElpisDimensionLab();
            var upgradeItemIdHashSet = new HashSet<int>();
            for (int i = 0; i < specElpisDimensionLabs.Count; ++i)
            {
                upgradeItemIdHashSet.Add(specElpisDimensionLabs[i].item_id);
            }
            
            canUpgradeBadge.Clear();   
            foreach (var itemId in upgradeItemIdHashSet)
            {
                canUpgradeBadge.AddBadgePath(BadgeType.RedDot,
                    $"{ElpisCoreItem.BadgePathPrefix}/{itemId}");
            }
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

        /// <summary>
        /// 캐시된 시설 정보 목록을 반환합니다.
        /// </summary>
        public List<FacilityInfo> CachedFacilityInfos => cachedFacilityInfos;

        /// <summary>
        /// 슬롯 인덱스를 반환합니다.
        /// </summary>
        public int SlotIndex => buildInfo?.slot_index ?? -1;

        public void ChangeInfo(ElpisFacility facility)
        {
            if (facility == null)
                return;

            facilityData = facility;

            if (facility.Level >= 1)
            {
                buildInfo = SpecDataManager.Instance.GetElpisBuildInfoData((int)facilityData.BuildId,
                    (int)facility.Level);
            }
            else
            {
                buildInfo = SpecDataManager.Instance.GetBuildInfo((int)facilityData.BuildId);
            }


            if (buildInfo == null)
                return;

            UpdateFacilityInfos();
            UpdateUI();
            StartInstallingTimer();

            // 이미 건설 중인 상태라면 애니메이션 시작
            var installingFacility = GetInstallingFacility();
            if (installingFacility != null)
            {
                StartConstructionEffect();
            }
            // 건설 완료 대기 상태라면 Finish 루프 재생
            else if (GetJustCompletedFacility() != null)
            {
                target?.PlayFinishLoop();
            }
            // 이미 설치 완료된 상태라면 건물 프리팹 소환
            else if (facility.Level > 0 && buildInfo.facility_type != FacilityType.COMMAND_CENTER)
            {
                if (!target)
                    return;

                var buildPathes = new List<string>();

                foreach (var facilityInfo in cachedFacilityInfos)
                {
                    if (facilityInfo.isInstalled)
                    {
                        buildPathes.Add(facilityInfo.buildInfo.build_prefab);
                    }
                }

                target.SpawnMultiBuildingAsync(buildPathes.ToArray()).Forget();
            }
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
            GuideMissionDataBridge ??= new GuideMissionDataBridge();
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
                var canBuild = !isInstalled && !isInstalling && !isPreviousLevelRequired && !isAnotherBuilding &&
                               CheckCanBuild(spec, commandCenterLv, allBenefits);

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

        private bool CheckCanBuild(ElpisBuildInfo targetBuildInfo, uint commandCenterLv,
            IReadOnlyList<ElpisCommandCenterBenefit> allBenefits)
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

        private FacilityInfo GetJustCompletedFacility()
        {
            foreach (var info in cachedFacilityInfos)
            {
                if (info.isJustCompleted)
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

            // Finish 루프 상태로 전환 (애니메이션 시퀀스가 끝나면 자동으로 Finish가 재생되지만, 명시적으로 호출)
            target?.PlayFinishLoop();

            UpdateUI();
            currentBuildLayer?.Close();

            // 튜토리얼 트리거: 건물 완성
            Debug.Log($"[LobbyBuildingInteractionUI] OnInstallTimerCompleted: {installingFacility.buildInfo.build_id}");
            TutorialManager.Instance.HandleTutorialAction(
                TutorialTriggerType.BUILDING_COMPLETE,
                installingFacility.buildInfo.build_id.ToString()
            );
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

        private void UpdateZoomBasedUI()
        {
            var cameraController = MainCameraHolder.CameraGestureController;
            if (cameraController == null)
                return;

            var zoomRatio = cameraController.ZoomRatio;

            // statusIcon 크기 조절 (줌이 클수록 작아짐)
            UpdateStatusIconSize(zoomRatio);

            // lobbyNameTag alpha 조절
            UpdateNameTagAlpha(zoomRatio);
        }

        private void UpdateStatusIconSize(float zoomRatio)
        {
            if (statusIcon == null || statusIconDefaultSize == Vector2.zero)
                return;

            // 줌이 클수록 (zoomRatio가 1에 가까울수록) 아이콘이 작아짐
            // 1 - zoomRatio로 반전: 줌 아웃 시 크고, 줌 인 시 작게
            var scale = 1f - zoomRatio * 0.5f; // 0.5 ~ 1.0 범위
            scale = Mathf.Clamp(scale, 0.5f, 1f);

            statusIcon.sizeDelta = statusIconDefaultSize * scale;
        }

        private void UpdateNameTagAlpha(float zoomRatio)
        {
            if (lobbyNameTag == null)
                return;

            // 줌 비율이 DisappearZoomRatio를 넘으면 alpha → 0, 아니면 → 1
            targetNameTagAlpha = zoomRatio >= DisappearZoomRatio ? 0f : 1f;

            // 부드럽게 전환
            var currentAlpha = lobbyNameTag.alpha;
            if (!Mathf.Approximately(currentAlpha, targetNameTagAlpha))
            {
                lobbyNameTag.alpha =
                    Mathf.MoveTowards(currentAlpha, targetNameTagAlpha, AlphaFadeSpeed * Time.deltaTime);
            }

            // alpha가 0이면 비활성화, 아니면 활성화 (상태 변경 시에만 SetActive 호출)
            var shouldBeActive = lobbyNameTag.alpha > 0f;
            if (lobbyNameTag.gameObject.activeSelf != shouldBeActive)
            {
                lobbyNameTag.gameObject.SetActive(shouldBeActive);
            }
        }

        public void StartConstructionEffect()
        {
            var installingFacility = GetInstallingFacility();
            if (installingFacility == null || target == null)
                return;

            var isUpgrade = installingFacility.buildInfo.build_lv > 1;
            var totalBuildTime = installingFacility.buildInfo.build_time;
            var remainingTime = (float)(installingFacility.completionTime - TimeManager.Instance.UtcNow()).TotalSeconds;
            remainingTime = Mathf.Max(0f, remainingTime);

            target.StartConstructionAnimation(remainingTime, totalBuildTime, isUpgrade);
        }

        private void UpdatePosition()
        {
            CachedRectTr.anchoredPosition =
                MainCameraHolder.WorldPointToLocalPointInRectangle(target.CachedTr.position, parentRect);
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

            if (isFacilityLocked)
                return;

            if (isInstallFinished)
            {
                // 건물 생성 후 Disappear 애니메이션 재생
                var completedFacility = GetJustCompletedFacility();
                var prefabPath = completedFacility?.buildInfo?.build_prefab;
                if (target != null)
                {
                    await target.PlayDisappearAnimationAsync(prefabPath);
                }

                ElpisFacility changedInfo = null;

                if (facilityData.Level >= 1 &&
                    facilityData
                        .IsUpgrading) //TODO : upgrade 인지 체크해야 됨. facilityData.Level >= 1 && facilityData.Is_upgrading 으로 변경 필요
                {
                    var response =
                        await NetManager.Instance.Elpis.FinishUpgradingFacilityAsync((int)facilityData.BuildId);
                    if (response != null && response.IsSuccess)
                    {
                        changedInfo = response.Facility;
                    }
                }
                else
                {
                    var response =
                        await NetManager.Instance.Elpis.FinishBuildingFacilityAsync((int)facilityData.BuildId);
                    if (response != null && response.IsSuccess)
                    {
                        changedInfo = response.Facility;
                    }
                }

                if (changedInfo != null)
                {
                    ChangeInfo(changedInfo);
                }

                // await GuideMissionDataBridge.AddActionAsync(GuideMissionType.INSTALL_BUILDING, 1, (int)facilityData.BuildId);
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