using Cysharp.Threading.Tasks;
using Tech.Hive.V1;

namespace CookApps.AutoBattler
{
    public partial class NetManager
    {
        private UniTaskCompletionSource _elpisInitComplete;

        /// <summary>
        /// Elpis 초기화 완료를 대기
        /// </summary>
        public UniTask WaitForElpisInitializationAsync()
        {
            if (_elpisInitComplete == null)
            {
                _elpisInitComplete = new UniTaskCompletionSource();
            }
            return _elpisInitComplete.Task;
        }

        public async UniTask Initialize_Elpis()
        {
            if (_elpisInitComplete == null)
            {
                _elpisInitComplete = new UniTaskCompletionSource();
            }

            await Elpis.GetInfoAsync();
            if (ServerDataManager.Instance.Elpis.FacilityCount == 0)
            {
                var commandCenter = SpecDataManager.Instance.GetBuildInfo(IdMap.ElpisBuild.CommandCenter);
                await Elpis.BuildFacilityAsync(commandCenter.build_id, 0, 0);
                await Elpis.FinishBuildingFacilityAsync(commandCenter.build_id);
            }

            _elpisInitComplete.TrySetResult();
        }
    }
}
