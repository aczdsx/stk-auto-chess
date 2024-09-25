/*
* Copyright (c) CookApps.
*/

using System.Threading.Tasks;
using Tech.Hive.V1;
using UnityEngine.Pool;

namespace CookApps.AutoBattler
{
    public partial class GrpcServerService
    {
        /// <summary>
        /// 전체 서버리스트와 서버의 플레이어 정보를 반환합니다.
        /// </summary>
        /// <param name="uid"></param>
        /// <returns></returns>
        public async Task<ServerListResponse> ListAsync(uint uid)
        {
            using PooledObject<ServerListRequest> _ = GenericPool<ServerListRequest>.Get(out ServerListRequest request);
            ServerListResponse response = await ListAsync(request);
            return response;
        }

        /// <summary>
        /// 서버와 플레이어를 선택합니다.
        /// </summary>
        /// <param name="serverId"></param>
        /// <param name="playerId"></param>
        /// <returns></returns>
        public async Task<ServerJoinResponse> JoinAsync(uint serverId, string playerId)
        {
            using PooledObject<ServerJoinRequest> _ = GenericPool<ServerJoinRequest>.Get(out ServerJoinRequest request);
            request.ServerId = serverId;
            request.PlayerId = playerId;
            ServerJoinResponse response = await JoinAsync(request);
            return response;
        }
    }
}