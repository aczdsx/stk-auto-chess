using System.Collections.Generic;
using CookApps.TeamBattle.UIManagements;
using Tech.Hive.V1;
using UnityEngine;

namespace CookApps.AutoBattler
{
    public partial class LobbyMain
    {
        [SerializeField] private LobbyBuildingInteractionUI _slotPrefab;
        [SerializeField] private RectTransform _slotContainer;

        private List<LobbyBuildingInteractionUI> _activeSlots = new List<LobbyBuildingInteractionUI>();

        // 이미 생성된 facility_type 추적용
        private readonly HashSet<ElpisFacilityType> _createdFacilityTypes = new();

        private void CreateWorldInteractionSlots(IReadOnlyList<ElpisBuildingBase> buildingBases)
        {
            // 열려있는 ElpisBuildLayer가 있으면 현재 참조 중인 FacilityType 저장
            var openBuildLayer = SceneUILayerManager.Instance.GetUILayer<ElpisBuildLayer>();
            var previousSlotFacilityType = openBuildLayer?.GetCurrentFacilityType();

            ClearSlots();
            _createdFacilityTypes.Clear();

            LobbyBuildingInteractionUI newSlotForBuildLayer = null;

            var facilities = elpisDataModel.GetAllFacilities();
            for (var i = 0; i < facilities.Count; i++)
            {
                var buildingData = facilities[i];

                // 해금되지 않은 건물은 건너뛰기
                if (buildingData.GridX < 0 || buildingData.GridX >= buildingBases.Count)
                    continue;

                // 같은 facility_type의 slot이 이미 생성되었으면 건너뛰기
                if (!_createdFacilityTypes.Add(buildingData.Type))
                    continue;

                var slot = Instantiate(_slotPrefab, _slotContainer);
                slot.CachedGo.SetActive(true);  // Initialize 전에 활성화해야 코루틴 시작 가능
                slot.Initialize(buildingBases[buildingData.GridX], buildingData);
                _activeSlots.Add(slot);

                // ElpisBuildLayer가 열려있었고, 같은 FacilityType이면 새 슬롯 저장
                if (previousSlotFacilityType.HasValue && buildingData.Type == previousSlotFacilityType.Value)
                {
                    newSlotForBuildLayer = slot;
                }
            }
            
            // 열려있는 ElpisBuildLayer의 참조를 새 슬롯으로 업데이트
            if (openBuildLayer != null && newSlotForBuildLayer != null)
            {
                openBuildLayer.UpdateTargetBuildingUI(newSlotForBuildLayer);
            }
        }
        
        private void ClearSlots()
        {
            for (var i = 0; i < _activeSlots.Count; i++)
            {
                if (_activeSlots[i] != null) Destroy(_activeSlots[i].gameObject);
            }
            _activeSlots.Clear();
        }
    }
}