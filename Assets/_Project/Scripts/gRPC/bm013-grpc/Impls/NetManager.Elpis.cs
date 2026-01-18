using Cysharp.Threading.Tasks;

namespace CookApps.AutoBattler
{
    public partial class NetManager
    {
        public async UniTask Initialize_Elpis()
        {
            await Elpis.GetInfoAsync();
            if (ServerDataManager.Instance.Elpis.FacilityCount == 0)
            {
                var commandCenter = SpecDataManager.Instance.GetBuildInfo(IdMap.ElpisBuild.CommandCenter);
                await Elpis.BuildFacilityAsync(commandCenter.build_id, 0, 0);
                await Elpis.FinishBuildingFacilityAsync(commandCenter.build_id);
            }
        }
    }
}
