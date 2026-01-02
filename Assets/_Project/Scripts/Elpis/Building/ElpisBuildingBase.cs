using CookApps.TeamBattle;
using Tech.Hive.V1;
using UnityEngine;

namespace CookApps.AutoBattler
{
    public class ElpisBuildingBase : CachedMonoBehaviour
    {
        public int SlotIndex { get; private set; }
        public ElpisFacilityType BuildingType { get; private set; }
        
        public void Initialize(int slotIndex)
        {
            SlotIndex = slotIndex;
        }
    }
}