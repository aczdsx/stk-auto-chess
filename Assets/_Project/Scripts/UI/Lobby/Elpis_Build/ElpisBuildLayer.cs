using System;
using System.Collections.Generic;
using CookApps.TeamBattle.UIManagements;
using Cysharp.Threading.Tasks;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI.Extensions;

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
        [SerializeField] private UICircle progressCircle;

        private List<ElpisBuildCell> cells = new();
        private ElpisBuildCacheData cachedData;
        private LobbyBuildingInteractionUI.FacilityInfo installingFacility;

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
            InitUI();
            LobbyMain.GetLobbyMain().PlayExitAnimation();

            Debug.Log($"[ElpisBuildLayer] OnPreEnter: {GetType().Name}");
            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_btn_popup);
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

        /// <summary>
        /// 최초 진입 시 호출 - 셀 생성 포함
        /// </summary>
        public void InitUI()
        {
            PopulateBuildList();
            RefreshUI();

            // LayoutGroup 계산 강제 업데이트 (튜토리얼에서 정확한 위치를 가져오기 위해)
            Canvas.ForceUpdateCanvases();

            TutorialManager.Instance.HandleTutorialAction(TutorialTriggerType.LAYER_ENTER, GetType().Name);
        }

        /// <summary>
        /// 데이터 갱신 시 호출 - 셀 재생성 없이 상태만 업데이트
        /// </summary>
        public void RefreshUI()
        {
            installingFacility = GetInstallingFacility();
            var isContainInstalling = installingFacility != null;
            installingTimeTextParent.SetActive(isContainInstalling);

            if (isContainInstalling)
            {
                var remainingTime = cachedData.targetLobbyBuildingUI.GetCurrentRemainingTime();
                if (remainingTime.HasValue)
                {
                    UpdateRemainingTime(remainingTime.Value);
                }
            }

            // 각 셀 상태 갱신
            for (int i = 0; i < cells.Count && i < cachedData.facilityInfos.Count; i++)
            {
                cells[i].SetData(cachedData.facilityInfos[i], OnInstallRequested);
            }
        }

        public void UpdateRemainingTime(TimeSpan remainingTime)
        {
            if (installingFacility != null)
            {
                var totalBuildTime = installingFacility.buildInfo.build_time;
                if (totalBuildTime > 0)
                {
                    var progress = (float)remainingTime.TotalSeconds / totalBuildTime;
                    progressCircle.SetProgress(progress);
                }
            }

            installingTimeText.text = remainingTime.ToString(@"mm\:ss");
        }

        private LobbyBuildingInteractionUI.FacilityInfo GetInstallingFacility()
        {
            if (cachedData?.facilityInfos == null) return null;

            foreach (var facilityInfo in cachedData.facilityInfos)
            {
                if (facilityInfo.isInstalling)
                    return facilityInfo;
            }

            return null;
        }

        private void PopulateBuildList()
        {
            foreach (var cell in cells)
            {
                Destroy(cell.gameObject);
            }
            cells.Clear();

            var isTutorial = TutorialManager.Instance.IsTutorial;
            var index = 0;

            foreach (var facilityInfo in cachedData.facilityInfos)
            {
                var newCell = Instantiate(cellPrefab, contentRoot);
                newCell.SetData(facilityInfo, OnInstallRequested);
                cells.Add(newCell);

                // 튜토리얼 모드일 때 TutorialTarget 동적 등록
                if (isTutorial)
                {
                    var tutorialTarget = newCell.gameObject.GetComponent<TutorialTarget>()
                                         ?? newCell.gameObject.AddComponent<TutorialTarget>();
                    tutorialTarget.SetTargetId($"BuildingListItemSlot_{index}");
                }

                index++;
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

        public void Close()
        {
            CloseThisUILayer();
        }

        private async UniTaskVoid InstallBuilding(ElpisBuildInfo info)
        {
            if (cachedData.slotIndex < 0)
            {
                Debug.LogError("Slot Index is invalid!");
                return;
            }

            TutorialManager.Instance.HandleTutorialAction(TutorialTriggerType.SELECT_BUILD, info.build_id.ToString());

            if (info.build_lv > 1)
            {
                var result = await NetManager.Instance.Elpis.UpgradeFacilityAsync(info.build_id);
                if (result != null && result.IsSuccess)
                {
                    if (result.Facility.IsUpgrading) //TODO : is_upgrading 으로 변경해야됨
                    {
                        result.Facility.Level++;
                    }

                    cachedData.targetLobbyBuildingUI.ChangeInfo(result.Facility);
                    cachedData.targetLobbyBuildingUI.StartConstructionEffect();

                    // 건물 짓는 중 상태로 UI 갱신
                    cachedData.facilityInfos = cachedData.targetLobbyBuildingUI.CachedFacilityInfos;
                    RefreshUI();
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
                    cachedData.targetLobbyBuildingUI.StartConstructionEffect();

                    // 건물 짓는 중 상태로 UI 갱신
                    cachedData.facilityInfos = cachedData.targetLobbyBuildingUI.CachedFacilityInfos;
                    RefreshUI();
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

        /// <summary>
        /// 현재 참조 중인 시설의 FacilityType을 반환합니다.
        /// </summary>
        public Tech.Hive.V1.ElpisFacilityType? GetCurrentFacilityType()
        {
            if (cachedData?.facilityInfos == null || cachedData.facilityInfos.Count == 0)
                return null;

            return cachedData.facilityInfos[0].buildInfo.facility_type.ToServerType();
        }

        /// <summary>
        /// LobbyBuildingInteractionUI 참조를 새 슬롯으로 업데이트합니다.
        /// 커맨드센터 업그레이드 등으로 슬롯이 재생성될 때 호출됩니다.
        /// </summary>
        public void UpdateTargetBuildingUI(LobbyBuildingInteractionUI newBuildingUI)
        {
            if (cachedData == null || newBuildingUI == null)
                return;

            // 이전 참조 해제
            cachedData.targetLobbyBuildingUI?.SetCurrentBuildLayer(null);

            // 새 참조로 업데이트
            cachedData.targetLobbyBuildingUI = newBuildingUI;
            cachedData.targetLobbyBuildingUI.SetCurrentBuildLayer(this);

            // 새 슬롯의 데이터로 갱신
            cachedData.facilityInfos = newBuildingUI.CachedFacilityInfos;
            cachedData.slotIndex = newBuildingUI.SlotIndex;

            // UI 재초기화 (facilityInfos가 바뀌므로 셀 재생성 필요)
            InitUI();
        }
    }
}
