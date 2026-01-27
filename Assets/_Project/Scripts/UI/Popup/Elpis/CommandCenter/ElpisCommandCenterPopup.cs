using System.Collections.Generic;
using CookApps.TeamBattle.UI;
using CookApps.TeamBattle.UIManagements;
using CookApps.TeamBattle.Utility;
using Cysharp.Text;
using Cysharp.Threading.Tasks;
using R3;
using Tech.Hive.V1;
using TMPro;
using UnityEngine;

namespace CookApps.AutoBattler
{
    public class ElpisCommandCenterPopup : UILayerPopupBase
    {
        [SerializeField] private CAButton closeButton;
            
        [Header("배경 & NPC")]
        [SerializeField] private CAButton npcAreaButton;
        [SerializeField] private Transform npcDialogueArea;
        [SerializeField] private TextMeshProUGUI npcDialogueText;

        [Header("혜택 영역")]
        [SerializeField] private TextMeshProUGUI elpisLevelText;
        [SerializeField] private TableView benefitsTableView;
        [SerializeField] private GameObject benefitCellPrefab;

        [Header("업그레이드 버튼")]
        [SerializeField] private CAButton upgradeButton;
        [SerializeField] private TextMeshProUGUI requiredCoreText;
        [SerializeField] private SimpleSwapper[] requiredCoreTextSwappers;
        [SerializeField] private TextMeshProUGUI currentCoreText;

        private int currentElpisLevel;
        private int currentCoreAmount;
        private int requiredCoreForUpgrade;
        private bool hasNextUpgrade;
        private bool isUpgrading;
        private List<ElpisCommandCenterBenefit> currentBenefits = new List<ElpisCommandCenterBenefit>();

        // TableView Controller
        private TableViewController<ElpisCommandCenterBenefit, ElpisCommandCenterBenefitCell> benefitController;

        private ElpisDataBridge dataBridge;
        private InventoryDataBridge inventoryDataBridge;
        
        private LobbyMain lobbyMain;
        
        protected override void Awake()
        {
            base.Awake();
            
            lobbyMain = LobbyMain.GetLobbyMain();
            
            InitializeTableView();
            SubscribeButtons();
        }

        protected override void OnBackButton(ref bool offPrevUI)
        {
            if (isUpgrading)
                return;

            base.OnBackButton(ref offPrevUI);
        }

        private void SubscribeButtons()
        {
            closeButton.OnClickAsObservable()
                .Subscribe(this, (_, self) => self.CloseThisUILayer())
                .AddTo(this);

            upgradeButton.OnClickAsObservable()
                .SubscribeAwait(this, (_, self, token) => self.OnUpgradeButtonClicked(), AwaitOperation.Drop)
                .AddTo(this);

            npcAreaButton.OnClickAsObservable()
                .Subscribe(this, (_, self) => self.OnNPCClicked())
                .AddTo(this);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            benefitController?.Detach();
        }

        protected override void OnPreEnter(object param)
        {
            base.OnPreEnter(param);


            var gdb = new GuideMissionDataBridge();

            // ! GUIDE_TODO
            // ! 401	14	USE_BUILDING	GUIDE_MISSION_NAME_401	커멘더 센터 이동 가이드 미션	0	GUIDE_MISSION_DESC_401	0	1	GOLD	210001	200											            
            // ! USE_BUILDING
            if(gdb.GuideMissionId == GuideMissionConstants.커맨드센터들어간가이드미션ID)
            {
                gdb.AddAction(GuideMissionType.USE_BUILDING, 1);
            }

            lobbyMain.PlayExitAnimation();
            
            dataBridge = new ElpisDataBridge();
            inventoryDataBridge = new InventoryDataBridge();
            
            currentElpisLevel = (int)((ElpisFacility)param).Level;
            currentCoreAmount = (int)inventoryDataBridge.GetCurrency(IdMap.Item.엘피스코어);

            LoadElpisData();
            UpdateUI();
            
            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_btn_popup);
        }
        
        protected override void OnPreExit()
        {
            base.OnPostExit();
            
            lobbyMain.PlayEnterAnimation();
        }
        
        private void InitializeTableView()
        {
            if (benefitsTableView == null || benefitCellPrefab == null)
            {
                Debug.LogError("TableView 또는 Cell Prefab이 설정되지 않았습니다.");
                return;
            }

            benefitController = benefitsTableView.CreateController<ElpisCommandCenterBenefit, ElpisCommandCenterBenefitCell>()
                .WithData(currentBenefits)
                .WithCellPrefab(benefitCellPrefab)
                .WithCellSize(benefitCellPrefab.GetComponent<RectTransform>().rect.size)
                .OnBind((cell, data, index) =>
                {
                    cell.SetData(data, index);
                    cell.SetShortcutCallback(() => NavigateToBenefit(data.build_id));
                })
                .OnCellRecycled(cell => cell.ResetState())
                .Build();
        }

        /// <summary>
        /// 혜택 바로가기 네비게이션
        /// </summary>
        private void NavigateToBenefit(int buildId)
        {
            var buildInfo = SpecDataManager.Instance.GetBuildInfo(buildId);
            if (buildInfo == null)
            {
                Debug.LogWarning($"BuildInfo not found: {buildId}");
                return;
            }

            var facilityType = buildInfo.facility_type.ToServerType();
            var building = FindBuildingByType(facilityType);
            if (building == null)
            {
                Debug.LogWarning($"Building not found: {facilityType}");
                return;
            }

            // 팝업 닫고 건물 위치로 카메라 이동
            CloseThisUILayer();
            var cameraController = MainCameraHolder.CameraGestureController;
            cameraController.MoveAsync(building.CachedTr.position, 0.5f).Forget();
        }

        private ElpisBuildingBase FindBuildingByType(ElpisFacilityType facilityType)
        {
            var buildings = lobbyMain.MainBlock.ElpisBuildings;
            for (var i = 0; i < buildings.Count; i++)
            {
                if (buildings[i].BuildingType == facilityType)
                    return buildings[i];
            }
            return null;
        }

        #region 데이터 로딩

        private void LoadElpisData()
        {
            var commandCenter = dataBridge.GetFacilityByType(ElpisFacilityType.FacilityTypeCommandCenter);
            currentBenefits.Clear();
            var uiLevel = (int)commandCenter.Level + 1;
            for (var i = 0; i < SpecDataManager.Instance.ElpisCommandCenterBenefit.All.Count; i++)
            {
                var benefit = SpecDataManager.Instance.ElpisCommandCenterBenefit.All[i];
                if (benefit.lv != uiLevel)
                    continue;
                
                currentBenefits.Add(benefit);
            }

            hasNextUpgrade = false;
            requiredCoreForUpgrade = 0;
            for (var i = 0; i < SpecDataManager.Instance.ElpisBuildInfo.All.Count; i++)
            {
                var buildInfo = SpecDataManager.Instance.ElpisBuildInfo.All[i];
                if (buildInfo.facility_type.ToServerType() == ElpisFacilityType.FacilityTypeCommandCenter && buildInfo.build_lv == uiLevel)
                {
                    requiredCoreForUpgrade = buildInfo.item_INT;
                    hasNextUpgrade = true;
                }
            }
        }

        #endregion

        #region UI 업데이트

        private void UpdateUI()
        {
            UpdateLevelDisplay();
            UpdateBenefitsList();
            UpdateUpgradeButton();
            UpdateNPCDialogue();
        }

        private void UpdateLevelDisplay()
        {
            if (elpisLevelText)
                elpisLevelText.text = ZString.Concat(currentElpisLevel);
        }

        private void UpdateBenefitsList()
        {
            benefitController?.SetData(currentBenefits);
        }

        private void UpdateUpgradeButton()
        {
            var canUpgrade = hasNextUpgrade && currentCoreAmount >= requiredCoreForUpgrade;

            //upgradeButton.SetClickableState(canUpgrade);

            requiredCoreText.text = requiredCoreForUpgrade.ToString();
            requiredCoreTextSwappers.Swap(canUpgrade ? SimpleSwapType.Possible : SimpleSwapType.Impossible);
            
            currentCoreText.SetTextFormat("/{0}", currentCoreAmount);
        }

        private void UpdateNPCDialogue()
        {
            ShowRandomNPCDialogue();
        }

        #endregion

        #region 버튼 핸들러

        private async UniTask OnUpgradeButtonClicked()
        {
            if (!hasNextUpgrade || isUpgrading)
            {
                if (!hasNextUpgrade)
                    Debug.LogWarning("다음 업그레이드가 없습니다");
                return;
            }

            if(currentCoreAmount < requiredCoreForUpgrade)
                return;

            isUpgrading = true;

            try
            {
                var commandCenter = dataBridge.GetFacilityByType(ElpisFacilityType.FacilityTypeCommandCenter);
                var response = await NetManager.Instance.Elpis.UpgradeFacilityAsync((int)commandCenter.BuildId);
                if (!response.IsSuccess)
                {
                    Debug.LogError($"Command Center Upgrade Error:{response.Exception}");
                    return;
                }
                
                var gdb = new GuideMissionDataBridge();
                var edb = new ElpisDataBridge();
                // ! GUIDE_TODO
                // ! 403	16	CLEAR_TUTORIAL	GUIDE_MISSION_NAME_403	함선확장 가이드 미션	30002	GUIDE_MISSION_DESC_403	0	1	GOLD	210001	200											
                // ! UPGRADE_BUILDING_FOR_COMMEND_CENTER_2
                if(gdb.GuideMissionId == 403 && edb.GetFacilityLevel(Tech.Hive.V1.ElpisFacilityType.FacilityTypeCommandCenter) > 1)
                {
                    await gdb.AddActionAsync(GuideMissionType.CLEAR_TUTORIAL, 1);
                }

                currentElpisLevel = (int)response.Facility.Level;

                var subBlockIndex = currentElpisLevel - 2; // 레벨2 -> 인덱스0, 레벨3 -> 인덱스1
                if (subBlockIndex >= 0 && subBlockIndex <= 1)
                {
                    var mainBlock = lobbyMain.MainBlock;
                    var cameraController = MainCameraHolder.CameraGestureController;
                    var mainCamera = MainCameraHolder.MainCamera;
                    var offsetZoom = mainCamera.orthographicSize;
                    var offsetPosition = mainCamera.transform.position;
                    
                    cameraController.SetCanInteractCamera(false);

                    await PlayExitAnimationAsync();

                    var subBlock = mainBlock.GetSubBlockInfo(subBlockIndex);
                    
                    await cameraController.ZoomAndMoveAsync(subBlock.LastAnimationPosition, 14.1f, 0.3f);
                    //cameraController.SetFollowTarget(subBlock.SubBlock.CachedTr, 2.0f);
                    await mainBlock.AttachSubBlock(subBlockIndex, true);

                    mainBlock.RebuildNavMesh();

                    //cameraController.SetFollowTarget(null, 1.0f);
                    cameraController.MoveAsync(offsetPosition, 0.3f).Forget();
                    await cameraController.ZoomAsync(offsetZoom, 0.3f);
                    
                    cameraController.SetCanInteractCamera(true);
                }

                currentCoreAmount = (int)inventoryDataBridge.GetCurrency(IdMap.Item.엘피스코어);

                // 결과 팝업에 보여줄 현재 레벨(방금 업그레이드된 레벨)의 benefit 수집
                var upgradedBenefits = new List<ElpisCommandCenterBenefit>();
                for (var i = 0; i < SpecDataManager.Instance.ElpisCommandCenterBenefit.All.Count; i++)
                {
                    var benefit = SpecDataManager.Instance.ElpisCommandCenterBenefit.All[i];
                    if (benefit.lv == currentElpisLevel)
                        upgradedBenefits.Add(benefit);
                }

                LoadElpisData();
                UpdateUI();

                var resultPopup = await SceneUILayerManager.Instance.PushUILayerAsync<ElpisCommandCenterResultPopup>(upgradedBenefits);
                await resultPopup.WaitForExit();
                await PlayEnterAnimationAsync();

                lobbyMain.RefreshWorldInteractionSlots((uint)currentElpisLevel);
            }
            finally
            {
                isUpgrading = false;
            }
        }

        private UniTask PlayExitAnimationAsync()
        {
            var tcs = new UniTaskCompletionSource();
            StartExitAnimation(_ => tcs.TrySetResult());
            return tcs.Task;
        }

        private UniTask PlayEnterAnimationAsync()
        {
            var tcs = new UniTaskCompletionSource();
            StartEnterAnimation(_ => tcs.TrySetResult());
            return tcs.Task;
        }

        private void OnNPCClicked()
        {
            ShowRandomNPCDialogue();
        }

        private static readonly string[] npcDialoguesKeys =
        {
            "NPC_TEXT_10001_1",
            "NPC_TEXT_10001_2",
            "NPC_TEXT_10001_3",
            "NPC_TEXT_10001_4",
            "NPC_TEXT_10001_5",
        };

        private void ShowRandomNPCDialogue()
        {
            if (npcDialogueText == null) return;

            var randomIndex = UnityEngine.Random.Range(0, npcDialoguesKeys.Length);
            npcDialogueText.text = LanguageManager.Instance.GetDefaultText(npcDialoguesKeys[randomIndex]);
        }

        #endregion
    }
}
