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
    
    [SerializeField] private GameObject benefitCellPrefab;
    [SerializeField] private RectTransform benefitsScrollViewContent;
    private List<ElpisCommandCenterBenefit> currentBenefits = new List<ElpisCommandCenterBenefit>();
    private List<ElpisCommandCenterBenefitCell> spawnedCells = new List<ElpisCommandCenterBenefitCell>();

    protected override void OnPreEnter(object param)
    {
        base.OnPreEnter(param);
        
        closeButton.OnClickAsObservable()
            .Subscribe(this, (_, self) => self.CloseThisUILayer())
            .AddTo(this);
        
        var elpisModel = ServerDataManager.Instance.Elpis;

        var currentLevel = elpisModel.GetFacilityLevel(ElpisFacilityType.FacilityTypeCommandCenter);
        levelText.text = ZString.Concat(currentLevel);
        
        currentBenefits = (List<ElpisCommandCenterBenefit>)param;
        InitializeTableView();
    }

    private void InitializeTableView()
    {
        if (!benefitCellPrefab || !benefitsScrollViewContent)
        {
            Debug.LogError("Cell Prefab 또는 ScrollViewContent가 설정되지 않았습니다.");
            return;
        }

        ClearSpawnedCells();

        for (int i = 0; i < currentBenefits.Count; i++)
        {
            var data = currentBenefits[i];
            var cellObject = Instantiate(benefitCellPrefab, benefitsScrollViewContent);
            var cell = cellObject.GetComponent<ElpisCommandCenterBenefitCell>();

            if (cell != null)
            {
                cell.SetData(data, i);
                cell.SetShortcutCallback(() => NavigateToBenefit(data.build_id));
                spawnedCells.Add(cell);
                cell.gameObject.SetActive(true);
            }
        }
    }

    private void ClearSpawnedCells()
    {
        foreach (var cell in spawnedCells)
        {
            if (cell != null)
            {
                Destroy(cell.gameObject);
            }
        }
        spawnedCells.Clear();
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
