using CookApps.gRPC.Universal;
using Cookapps.Stkauto.V1;
using Cysharp.Threading.Tasks;
using Tech.Universal.V2;
using UnityEngine.Pool;

namespace GrpcGame
{
    public partial class GameGrpcManager
    {
        public async UniTask<GetPvpInfoResponse> GetPvpInfoAsync()
        {
            using var _ = GenericPool<GetPvpInfoRequest>.Get(out var request);
            request.CommonRequestParam = IsClientOnly ? null : UniversalGrpcManager.Instance.GetCommonRequestParam();
            request.GameRequestParam = IsClientOnly ? null : UniversalGrpcManager.Instance.GetGameRequestParam();
            var response = await GameService.GetPvpInfoAsync(request);
            if (response == null)
                throw new System.Exception("response is canceled!");
            return response;
        } 
    }   
}
