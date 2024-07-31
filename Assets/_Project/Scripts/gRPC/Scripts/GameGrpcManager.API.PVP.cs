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
        
        // PVP 프로필 정보를 서버로부터 가져옴
        public async UniTask<GetPvpProfileResponse> GetPvpProfileAsync(string playerID, int profileType)
        {
            using var _ = GenericPool<GetPvpProfileRequest>.Get(out var request);
            request.CommonRequestParam = IsClientOnly ? null : UniversalGrpcManager.Instance.GetCommonRequestParam();
            request.GameRequestParam = IsClientOnly ? null : UniversalGrpcManager.Instance.GetGameRequestParam();
            request.Type = profileType;
            request.OpponentPlayerId = playerID;
            var response = await GameService.GetPvpProfileAsync(request);
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
            request.SimpleInfo = BMUtil.CompressStringToGzip(simpleProfileData);
            request.HeavyInfo = BMUtil.CompressStringToGzip(detailProfileData);

            var response = await GameService.UpdatePvpProfileAsync(request);
            if (response == null)
                throw new System.Exception("response is canceled!");
            return response;
        } 
        
        // PVP 전투 결과 전송
        public async UniTask<MatchPvpResponse> MatchPvp(PvpMatchResult result, string opponentPlayerID, string opponentSimpleData)
        {
            using var _ = GenericPool<MatchPvpRequest>.Get(out var request);
            request.CommonRequestParam = IsClientOnly ? null : UniversalGrpcManager.Instance.GetCommonRequestParam();
            request.GameRequestParam = IsClientOnly ? null : UniversalGrpcManager.Instance.GetGameRequestParam();
            request.MatchResult = result;
            request.OpponentPlayerId = opponentPlayerID;
            request.OpponentSimpleInfo = BMUtil.CompressStringToGzip(opponentSimpleData);

            var response = await GameService.MatchPvpAsync(request);
            if (response == null)
                throw new System.Exception("response is canceled!");
            return response;
        } 
        
        // 상대방 PVP 매칭 데이터가 업데이트 되었는지 체크
        public async UniTask<CheckPvpPowerUpdatedResponse> CheckPvpPowerUpdated(string opponentPlayerID, int opponentBattlePower)
        {
            using var _ = GenericPool<CheckPvpPowerUpdatedRequest>.Get(out var request);
            request.CommonRequestParam = IsClientOnly ? null : UniversalGrpcManager.Instance.GetCommonRequestParam();
            request.GameRequestParam = IsClientOnly ? null : UniversalGrpcManager.Instance.GetGameRequestParam();
            request.OpponentPlayerId = opponentPlayerID;
            request.Power = opponentBattlePower.ToString();

            var response = await GameService.CheckPvpPowerUpdatedAsync(request);
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
