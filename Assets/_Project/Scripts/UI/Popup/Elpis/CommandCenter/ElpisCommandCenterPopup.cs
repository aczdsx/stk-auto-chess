using CookApps.TeamBattle.UIManagements;
using CookApps.TeamBattle.UI;
using UnityEngine;
using TMPro;
using System.Collections.Generic;
using CookApps.TeamBattle.Utility;
using Cysharp.Text;
using R3;
using Tech.Hive.V1;
using Cysharp.Threading.Tasks;

namespace CookApps.AutoBattler
{
    public class ElpisCommandCenterPopup : UILayer
    {
        [SerializeField] private CAButton closeButton;
        
        [Header("배경 & NPC")]
        [SerializeField] private CAButton npcAreaButton;
        [SerializeField] private Transform npcDialogueArea;
        [SerializeField] private TextMeshProUGUI npcDialogueText;

        [Header("혜택 영역")]
        [SerializeField] private TextMeshProUGUI benefitsTitle;
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
        private List<ElpisCommandCenterBenefit> currentBenefits = new List<ElpisCommandCenterBenefit>();

        // TableView Controller
        private TableViewController<ElpisCommandCenterBenefit, ElpisCommandCenterBenefitCell> benefitController;

        private ElpisDataBridge dataBridge;
        private InventoryDataBridge inventoryDataBridge;
        
        protected override void Awake()
        {
            base.Awake();

            InitializeTableView();
            
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

            // TableView Controller 정리 (필수!)
            benefitController?.Detach();
        }

        protected override void OnPreEnter(object param)
        {
            base.OnPreEnter(param);

            var facility = param as ElpisFacility;
            
            dataBridge = new ElpisDataBridge();
            inventoryDataBridge = new InventoryDataBridge();
            
            currentElpisLevel = (int)facility.Level;
            currentCoreAmount = (int)inventoryDataBridge.GetCurrency(IdMap.Item.BuildItem);

            LoadElpisData();
            UpdateUI();
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
                    // cell.SetShortcutCallback(() => NavigateToBenefit(data.build_id));
                })
                .OnCellRecycled(cell => cell.ResetState())
                .Build();
        }

        /// <summary>
        /// 혜택 바로가기 네비게이션
        /// </summary>
        private void NavigateToBenefit(int buildGroupId)
        {
            // TODO: 건물/콘텐츠로 이동 구현
            Debug.Log($"혜택 바로가기: {buildGroupId}");
        }

        #region 데이터 로딩

        private void LoadElpisData()
        {
            var commandCenter = dataBridge.GetFacilityByType(ElpisFacilityType.FacilityTypeCommandCenter);
            currentBenefits.Clear();
            for (var i = 0; i < SpecDataManager.Instance.ElpisCommandCenterBenefit.All.Count; i++)
            {
                var benefit = SpecDataManager.Instance.ElpisCommandCenterBenefit.All[i];
                if (benefit.lv != commandCenter.Level + 1)
                    continue;
                
                currentBenefits.Add(benefit);
            }

            hasNextUpgrade = false;
            requiredCoreForUpgrade = 0;
            for (var i = 0; i < SpecDataManager.Instance.ElpisBuildInfo.All.Count; i++)
            {
                var buildInfo = SpecDataManager.Instance.ElpisBuildInfo.All[i];
                if (buildInfo.facility_type.ToServerType() == ElpisFacilityType.FacilityTypeCommandCenter &&
                    buildInfo.build_lv == commandCenter.Level)
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
            if (benefitsTitle != null)
            {
                benefitsTitle.text = $"영지 확장 혜택";
            }

            if (!elpisLevelText)
            {
                elpisLevelText.text = ZString.Concat(currentElpisLevel + 1);
            }
        }

        private void UpdateBenefitsList()
        {
            if (benefitController != null)
            {
                // TableView 데이터 업데이트
                benefitController.SetData(currentBenefits);
            }
        }

        private void UpdateUpgradeButton()
        {
            bool canUpgrade = hasNextUpgrade && currentCoreAmount >= requiredCoreForUpgrade;

            upgradeButton.SetClickableState(canUpgrade);

            if (requiredCoreText != null)
            {
                requiredCoreText.text = requiredCoreForUpgrade.ToString();
                requiredCoreTextSwappers.Swap(currentCoreAmount < requiredCoreForUpgrade ? SimpleSwapType.Impossible : SimpleSwapType.Possible);
            }

            currentCoreText.SetTextFormat("/{0}", currentCoreAmount);
        }

        private void UpdateNPCDialogue()
        {
            if (npcDialogueText != null)
            {
                // TODO: 토큰 시스템에서 다이얼로그 로드
                // 상황에 맞는 다이얼로그 가져오기 (입장, 업그레이드 완료 등)
                npcDialogueText.text = "커멘드 센터에 오신 것을 환영합니다.";
            }
        }

        #endregion

        #region 버튼 핸들러

        private async UniTask OnUpgradeButtonClicked()
        {
            if (currentCoreAmount < requiredCoreForUpgrade)
            {
                Debug.LogWarning("업그레이드를 위한 코어가 부족합니다");
                return;
            }

            if (!hasNextUpgrade)
            {
                Debug.LogWarning("다음 업그레이드가 없습니다");
                return;
            }

            // 업그레이드 실행
            var commandCenter = dataBridge.GetFacilityByType(ElpisFacilityType.FacilityTypeCommandCenter);
            var resp = await NetManager.Instance.Elpis.UpgradeFacilityAsync((int)commandCenter.BuildId);

            
            // 2. 업그레이드 애니메이션 재생

            // - 애니메이션 중 UI 숨김 처리
            // - 확장 비주얼 이펙트 재생
            // - 필요시 영지 프리팹 업데이트

            // 3. 완료 팝업 표시
            // 애니메이션 완료 후 호출됨
            
            // 새 데이터로 UI 갱신
            currentCoreAmount = (int)inventoryDataBridge.GetCurrency(IdMap.Item.BuildItem);
            LoadElpisData();
            UpdateUI();
        }

        private void OnNPCClicked()
        {
            // TODO: NPC 다이얼로그 표시
            // 무작위 또는 상황별 다이얼로그 로드
            // 애니메이션과 함께 다이얼로그 표시
            Debug.Log("NPC 클릭 - 다이얼로그 표시");
        }

        #endregion
    }
}
