using CookApps.TeamBattle.UIManagements;
using Cysharp.Threading.Tasks;
using Tech.Hive.V1;

namespace CookApps.AutoBattler
{
    public static class ElpisBuildingPopup
    {
        public static async UniTask<UILayer> OpenPopup(ElpisFacility facilityData)
        {
            if (facilityData.Type == ElpisFacilityType.FacilityTypeCommandCenter)
            {
                return await SceneUILayerManager.Instance.PushUILayerAsync<ElpisCommandCenterPopup>(facilityData);
            }
            else if (facilityData.Type == ElpisFacilityType.FacilityTypeNest)
            {
                // return await SceneUILayerManager.Instance.PushUILayerAsync<ElpisNestPopup>(facilityData);
            }
            else if (facilityData.Type == ElpisFacilityType.FacilityTypeDimensionLab)
            {
                return await SceneUILayerManager.Instance.PushUILayerAsync<ElpisCoreResearchLayer>();
            }

            return null;
        }
        
    }
}
