using System.Collections.Generic;
using CookApps.TeamBattle.UIManagements;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace CookApps.AutoBattler
{
    public partial class LobbyMain
    {
        [SerializeField] private LobbyBuildingInteractionUI _slotPrefab;
        [SerializeField] private RectTransform _slotContainer;

        private List<LobbyBuildingInteractionUI> _activeSlots = new List<LobbyBuildingInteractionUI>();

        private void CreateWorldInteractionSlots(IReadOnlyList<ElpisBuildingBase> buildingBases)
        {
            ClearSlots();

            var facilities = elpisDataBridge.GetAllFacilities();
            for (var i = 0; i < facilities.Count; i++)
            {
                var buildingData = facilities[i];

                // 해금되지 않은 건물은 건너뛰기
                if (buildingData.GridX < 0 || buildingData.GridX >= buildingBases.Count)
                    continue;

                var slot = Instantiate(_slotPrefab, _slotContainer);
                slot.Initialize(buildingBases[buildingData.GridX], buildingData);
                slot.CachedGo.SetActive(true);
                _activeSlots.Add(slot);
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