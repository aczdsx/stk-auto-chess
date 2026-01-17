using System.Collections.Generic;
using CookApps.AutoBattler;
using CookApps.TeamBattle.UI;
using CookApps.TeamBattle.UIManagements;
using Cysharp.Text;
using R3;
using Tech.Hive.V1;
using TMPro;
using UnityEngine;

public class ElpisCommandCenterResultPopup : UILayerPopupBase
{
    [SerializeField] private CAButton closeButton;
    [SerializeField] private TextMeshProUGUI levelText;
    
    [SerializeField] private TableView benefitsTableView;
    [SerializeField] private GameObject benefitCellPrefab; 
    private TableViewController<ElpisCommandCenterBenefit, ElpisCommandCenterBenefitCell> benefitController;
    private List<ElpisCommandCenterBenefit> currentBenefits = new List<ElpisCommandCenterBenefit>();

    protected override void OnPreEnter(object param)
    {
        base.OnPreEnter(param);
        
        closeButton.OnClickAsObservable()
            .Subscribe(this, (_, self) => self.CloseThisUILayer())
            .AddTo(this);
        
        var facilityDataBridge = new ElpisDataBridge();

        var currentLevel = facilityDataBridge.GetFacilityLevel(ElpisFacilityType.FacilityTypeCommandCenter);
        levelText.text = ZString.Concat(currentLevel);
        
        currentBenefits = (List<ElpisCommandCenterBenefit>)param;
        InitializeTableView();
    }

    private void InitializeTableView()
    {
        if (!benefitsTableView || !benefitCellPrefab)
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
    
    private void NavigateToBenefit(int buildId)
    {
        // var buildInfo = SpecDataManager.Instance.GetBuildInfo(buildId);
        // if (buildInfo == null)
        // {
        //     Debug.LogWarning($"BuildInfo not found: {buildId}");
        //     return;
        // }
        //
        // var facilityType = buildInfo.facility_type.ToServerType();
        // var building = FindBuildingByType(facilityType);
        // if (building == null)
        // {
        //     Debug.LogWarning($"Building not found: {facilityType}");
        //     return;
        // }
        //
        // // 팝업 닫고 건물 위치로 카메라 이동
        // CloseThisUILayer();
        // var cameraController = MainCameraHolder.CameraGestureController;
        // cameraController.MoveAsync(building.CachedTr.position, 0.5f).Forget();
    }
}
