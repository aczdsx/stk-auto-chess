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
                var slot = Instantiate(_slotPrefab, _slotContainer);
                var buildingData = facilities[i];
                slot.Initialize(buildingBases[buildingData.GridX], buildingData);
                slot.CachedGo.SetActive(true);
                _activeSlots.Add(slot);
            }
        }
        
        private void ClearSlots()
        {
            foreach (var slot in _activeSlots)
            {
                if (slot != null) Destroy(slot.gameObject);
            }
            _activeSlots.Clear();
        }
    }
}