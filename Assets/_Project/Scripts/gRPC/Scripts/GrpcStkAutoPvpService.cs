using CookApps.AutoBattler;
using CookApps.gRPC;
using CookApps.TeamBattle.UIManagements;
using Cysharp.Threading.Tasks;
using Tech.Hive.V1;
using UnityEngine.Pool;

namespace CookApps.gRPC
{
    [GrpcService(typeof(StkautoPvpService.StkautoPvpServiceClient))]
    public partial class GrpcStkAutoPvpService
    {
        // PVP 정보를 서버로부터 가져옴
        public async UniTask<PvpGetInfoResponse> GetPvpInfoAsync()
        {
            using var _ = GenericPool<PvpGetInfoRequest>.Get(out var request);
            var response = await GetInfoAsync(request);
            if (response == null)
                throw new System.Exception("response is canceled!");

            // 벤 유저 체크
            if (response.Status.Code == Defines.UNIVERSAL_RESPONSE_CODE_BANNED)
            {
                ToastManager.Instance.ShowToastByTokenKey("BANNED_USER_ALERT");

                var transition = SceneTransition_FadeInOut.Create();
                await SceneLoading.GoToNextScene("Title", null, transition);
                return null;
            }

            return response;
        }

        // PVP 프로필 정보를 서버로부터 가져옴
        public async UniTask<PvpGetProfileResponse> GetPvpProfileAsync(string playerID, int profileType)
        {
            using var _ = GenericPool<PvpGetProfileRequest>.Get(out var request);
            request.Type = profileType;
            request.OpponentPlayerId = playerID;
            var response = await GetProfileAsync(request);
            if (response == null)
                throw new System.Exception("response is canceled!");

            // 벤 유저 체크
            if (response.Status.Code == Defines.UNIVERSAL_RESPONSE_CODE_BANNED)
            {
                ToastManager.Instance.ShowToastByTokenKey("BANNED_USER_ALERT");

                var transition = SceneTransition_FadeInOut.Create();
                await SceneLoading.GoToNextScene("Title", null, transition);
                return null;
            }

            return response;
        }

        // PVP 매칭 리스트를 서버로부터 가져옴
        public async UniTask<PvpListMatchResponse> GetPvpMatchListAsync()
        {
            using var _ = GenericPool<PvpListMatchRequest>.Get(out var request);
            var response = await ListMatchAsync(request);
            if (response == null)
                throw new System.Exception("response is canceled!");

            // 벤 유저 체크
            if (response.Status.Code == Defines.UNIVERSAL_RESPONSE_CODE_BANNED)
            {
                ToastManager.Instance.ShowToastByTokenKey("BANNED_USER_ALERT");

                var transition = SceneTransition_FadeInOut.Create();
                await SceneLoading.GoToNextScene("Title", null, transition);
                return null;
            }

            return response;
        }

        // PVP 랭킹 리스트를 서버로부터 가져옴
        public async UniTask<PvpListPvpRankResponse> ListPvpRankAsync(int showRankCount)
        {
            using var _ = GenericPool<PvpListPvpRankRequest>.Get(out var request);
            var response = await ListPvpRankAsync(request);
            if (response == null)
                throw new System.Exception("response is canceled!");

            // 벤 유저 체크
            if (response.Status.Code == Defines.UNIVERSAL_RESPONSE_CODE_BANNED)
            {
                ToastManager.Instance.ShowToastByTokenKey("BANNED_USER_ALERT");

                var transition = SceneTransition_FadeInOut.Create();
                await SceneLoading.GoToNextScene("Title", null, transition);
                return null;
            }

            return response;
        }

        // PVP 전투 히스토리 리스트를 서버로부터 가져옴
        public async UniTask<PvpListMatchHistoryResponse> GetPvpMatchHistory(int showCount)
        {
            using var _ = GenericPool<PvpListMatchHistoryRequest>.Get(out var request);
            request.HistoryCount = showCount;
            var response = await ListMatchHistoryAsync(request);
            if (response == null)
                throw new System.Exception("response is canceled!");

            // 벤 유저 체크
            if (response.Status.Code == Defines.UNIVERSAL_RESPONSE_CODE_BANNED)
            {
                ToastManager.Instance.ShowToastByTokenKey("BANNED_USER_ALERT");

                var transition = SceneTransition_FadeInOut.Create();
                await SceneLoading.GoToNextScene("Title", null, transition);
                return null;
            }

            return response;
        }

        // PVP 프로필 정보 업데이트
        public async UniTask<PvpUpdateProfileResponse> UpdatePvpProfile(int battlePower, string simpleProfileData, string detailProfileData)
        {
            using var _ = GenericPool<PvpUpdateProfileRequest>.Get(out var request);
            request.Power = battlePower.ToString();
            request.SimpleInfo = BMUtil.CompressStringToGzip(simpleProfileData);
            request.HeavyInfo = BMUtil.CompressStringToGzip(detailProfileData);

            var response = await UpdateProfileAsync(request);
            if (response == null)
                throw new System.Exception("response is canceled!");

            // 벤 유저 체크
            if (response.Status.Code == Defines.UNIVERSAL_RESPONSE_CODE_BANNED)
            {
                ToastManager.Instance.ShowToastByTokenKey("BANNED_USER_ALERT");

                var transition = SceneTransition_FadeInOut.Create();
                await SceneLoading.GoToNextScene("Title", null, transition);
                return null;
            }

            return response;
        }

        // PVP 전투 결과 전송
        public async UniTask<PvpMatchResponse> MatchPvp(PvpMatchResult result, string opponentPlayerID, string opponentSimpleData, string matchID)
        {
            using var _ = GenericPool<PvpMatchRequest>.Get(out var request);
            request.MatchResult = result;
            request.OpponentPlayerId = opponentPlayerID;
            var userPVPSimpleData = UserDataManager.Instance.GetCurrentPVPSimpleProfileData(false);
            request.MySimpleInfo = BMUtil.ConvertToJsonSerialize(userPVPSimpleData);
            if (string.IsNullOrWhiteSpace(matchID) == false) request.MatchId = matchID;
            request.OpponentSimpleInfo = BMUtil.CompressStringToGzip(opponentSimpleData);

            var response = await MatchAsync(request);
            if (response == null)
                throw new System.Exception("response is canceled!");

            // 벤 유저 체크
            if (response.Status.Code == Defines.UNIVERSAL_RESPONSE_CODE_BANNED)
            {
                ToastManager.Instance.ShowToastByTokenKey("BANNED_USER_ALERT");

                var transition = SceneTransition_FadeInOut.Create();
                await SceneLoading.GoToNextScene("Title", null, transition);
                return null;
            }

            return response;
        }

        // 상대방 PVP 매칭 데이터가 업데이트 되었는지 체크
        // public async UniTask<CheckPvpPowerUpdatedResponse> CheckPvpPowerUpdated(string opponentPlayerID, int opponentBattlePower)
        // {
        //     using var _ = GenericPool<CheckPvpPowerUpdatedRequest>.Get(out var request);
        //     request.CommonRequestParam = IsClientOnly ? null : UniversalGrpcManager.Instance.GetCommonRequestParam();
        //     request.GameRequestParam = IsClientOnly ? null : UniversalGrpcManager.Instance.GetGameRequestParam();
        //     request.OpponentPlayerId = opponentPlayerID;
        //     request.Power = opponentBattlePower.ToString();
        //
        //     var response = await GameService.CheckPvpPowerUpdatedAsync(request);
        //     if (response == null)
        //         throw new System.Exception("response is canceled!");
        //
        //     // 벤 유저 체크
        //     if (response.CommonResponseData.StatusCode == Defines.UNIVERSAL_RESPONSE_CODE_BANNED)
        //     {
        //         ToastManager.Instance.ShowToastByTokenKey("BANNED_USER_ALERT");
        //
        //         var transition = SceneTransition_FadeInOut.Create();
        //         await SceneLoading.GoToNextScene("Title", null, transition);
        //         return null;
        //     }
        //
        //     return response;
        // }

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