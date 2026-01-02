using System.Collections.Generic;
using CookApps.TeamBattle.UIManagements;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace CookApps.AutoBattler
{
    public class LobbyWorldInteraction : UILayer
    {
        [SerializeField] private UIElpisBuildSlot _slotPrefab;
        [SerializeField] private RectTransform _slotContainer;

        private List<UIElpisBuildSlot> _activeSlots = new List<UIElpisBuildSlot>();

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