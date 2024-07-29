using CookApps.AutoBattler;
using CookApps.gRPC.Universal;
using Cookapps.Stkauto.V1;
using Cysharp.Threading.Tasks;
using Tech.Universal.V2;
using UnityEngine.Pool;

namespace GrpcGame
{
    public partial class GameGrpcManager
    {
        // PVP 정보를 서버로부터 가져옴
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
        
        // PVP 매칭 리스트를 서버로부터 가져옴
        public async UniTask<GetPvpMatchListResponse> GetPvpMatchListAsync()
        {
            using var _ = GenericPool<GetPvpMatchListRequest>.Get(out var request);
            request.CommonRequestParam = IsClientOnly ? null : UniversalGrpcManager.Instance.GetCommonRequestParam();
            request.GameRequestParam = IsClientOnly ? null : UniversalGrpcManager.Instance.GetGameRequestParam();
            var response = await GameService.GetPvpMatchListAsync(request);
            if (response == null)
                throw new System.Exception("response is canceled!");
            return response;
        } 
        
        // PVP 랭킹 리스트를 서버로부터 가져옴
        public async UniTask<GetPvpRankListResponse> GetPvpRankListAsync(int showRankCount)
        {
            using var _ = GenericPool<GetPvpRankListRequest>.Get(out var request);
            request.CommonRequestParam = IsClientOnly ? null : UniversalGrpcManager.Instance.GetCommonRequestParam();
            request.GameRequestParam = IsClientOnly ? null : UniversalGrpcManager.Instance.GetGameRequestParam();
            request.RankerCount = showRankCount;
            var response = await GameService.GetPvpRankListAsync(request);
            if (response == null)
                throw new System.Exception("response is canceled!");
            return response;
        } 
        
        // PVP 전투 히스토리 리스트를 서버로부터 가져옴
        public async UniTask<GetPvpMatchHistoryResponse> GetPvpMatchHistory(int showCount)
        {
            using var _ = GenericPool<GetPvpMatchHistoryRequest>.Get(out var request);
            request.CommonRequestParam = IsClientOnly ? null : UniversalGrpcManager.Instance.GetCommonRequestParam();
            request.GameRequestParam = IsClientOnly ? null : UniversalGrpcManager.Instance.GetGameRequestParam();
            request.HistoryCount = showCount;
            var response = await GameService.GetPvpMatchHistoryAsync(request);
            if (response == null)
                throw new System.Exception("response is canceled!");
            return response;
        } 
        
        // PVP 프로필 정보 업데이트
        public async UniTask<UpdatePvpProfileResponse> UpdatePvpProfile(int battlePower, string simpleProfileData, string detailProfileData)
        {
            using var _ = GenericPool<UpdatePvpProfileRequest>.Get(out var request);
            request.CommonRequestParam = IsClientOnly ? null : UniversalGrpcManager.Instance.GetCommonRequestParam();
            request.GameRequestParam = IsClientOnly ? null : UniversalGrpcManager.Instance.GetGameRequestParam();
            request.Power = battlePower.ToString();
            request.SimpleInfo = simpleProfileData;
            request.HeavyInfo = detailProfileData;

            var response = await GameService.UpdatePvpProfileAsync(request);
            if (response == null)
                throw new System.Exception("response is canceled!");
            return response;
        } 
        
        // 방어덱 세팅 정보를 서버에 업데이트
        // public async UniTask<UpdatePvpProfileResponse> UpdatePvpProfileAsync(int showRankCount)
        // {
        //     using var _ = GenericPool<UpdatePvpProfileRequest>.Get(out var request);
        //     request.CommonRequestParam = IsClientOnly ? null : UniversalGrpcManager.Instance.GetCommonRequestParam();
        //     request.GameRequestParam = IsClientOnly ? null : UniversalGrpcManager.Instance.GetGameRequestParam();
        //     request.RankerCount = showRankCount;
        //     var response = await GameService.UpdatePvpProfileAsync(request);
        //     if (response == null)
        //         throw new System.Exception("response is canceled!");
        //     return response;
        // } 
    }   
}
