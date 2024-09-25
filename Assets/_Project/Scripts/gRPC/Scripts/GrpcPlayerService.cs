/*
* Copyright (c) CookApps.
*/

using System;
using System.Threading.Tasks;
using CookApps.gRPC;
using Tech.Hive.V1;
using UnityEngine.Pool;

namespace CookApps.AutoBattler
{
    public partial class GrpcPlayerService
    {
        /// <summary>
        /// 특정 서버에서 내가 생성한 플레이어들의 정보를 반환합니다.
        /// </summary>
        /// <param name="page">0부터 시작합니다.</param>
        /// <param name="limit">일반적으로 25~50사이로 설정해주세요. 한 화면에 들어가는 갯수 * 1.5 정도가 적당합니다. <see cref="GrpcConsts.PaginationLimit"/>을 초과하면 예외가 발생합니다.</param>
        /// <returns></returns>
        /// <exception cref="Exception"><paramref name="limit"/>가 <see cref="GrpcConsts.PaginationLimit"/>을 초과하는 경우 예외가 발생합니다.</exception>
        public async Task<PlayerListResponse> ListAsync(uint page, uint limit)
        {
            if (limit > GrpcConsts.PaginationLimit)
            {
                throw new ArgumentException($"{limit}는 {GrpcConsts.PaginationLimit}개를 넘어갈 수 없습니다.");
            }

            using PooledObject<PlayerListRequest> _ = GenericPool<PlayerListRequest>.Get(out PlayerListRequest request);
            using PooledObject<PaginationInfo> __ = GenericPool<PaginationInfo>.Get(out PaginationInfo paginationInfo);
            paginationInfo.Page = page;
            paginationInfo.Limit = limit;
            request.Pagination = paginationInfo;
            PlayerListResponse response = await ListAsync(request);
            return response;
        }

        /// <summary>
        /// 마지막에 선택한 플레이어 정보를 반환합니다. 이 메서드를 호출하기 전에 꼭 로그인까지 호출되어 있어야 합니다ㅏ.
        /// </summary>
        /// <exception cref="Exception">Uid가 0입니다. 최소 1개의 플랫폼에 로그인해야 합니다.</exception>
        /// <returns></returns>
        public async Task<GetLastSelectedResponse> GetLastSelectedAsync()
        {
            using PooledObject<GetLastSelectedRequest> _ =
                GenericPool<GetLastSelectedRequest>.Get(out GetLastSelectedRequest request);
            GetLastSelectedResponse response = await GetLastSelectedAsync(request);
            return response;
        }

        /// <summary>
        /// 플레이어 정보를 반환합니다.
        /// </summary>
        /// <param name="playerId"></param>
        /// <returns></returns>
        public async Task<PlayerGetResponse> GetAsync(string playerId)
        {
            using PooledObject<PlayerGetRequest> _ = GenericPool<PlayerGetRequest>.Get(out PlayerGetRequest request);
            request.PlayerId = playerId;
            PlayerGetResponse response = await GetAsync(request);
            return response;
        }

        /// <summary>
        /// 플레이어를 <paramref name="serverId"/> 서버에 생성합니다.
        /// </summary>
        /// <param name="serverId"></param>
        /// <param name="nickname">플레이어 닉네임(ex : 쿡이)</param>
        /// <returns></returns>
        public async Task<PlayerCreateResponse> CreateAsync(uint serverId, string nickname)
        {
            using PooledObject<PlayerCreateRequest> _ =
                GenericPool<PlayerCreateRequest>.Get(out PlayerCreateRequest request);
            request.ServerId = serverId;
            request.Nickname = nickname;
            PlayerCreateResponse response = await CreateAsync(request);
            return response;
        }

        /// <summary>
        /// <paramref name="serverId"/> 서버에서 <paramref name="playerId"/> 플레이어를 삭제합니다.
        /// </summary>
        /// <param name="serverId"></param>
        /// <param name="playerId"></param>
        /// <returns></returns>
        public async Task<PlayerDeleteResponse> DeleteAsync(uint serverId, string playerId)
        {
            using PooledObject<PlayerDeleteRequest> _ =
                GenericPool<PlayerDeleteRequest>.Get(out PlayerDeleteRequest request);
            request.ServerId = serverId;
            request.PlayerId = playerId;
            PlayerDeleteResponse response = await DeleteAsync(request);
            return response;
        }

        /// <summary>
        /// TODO-tech : 주석필요
        /// </summary>
        /// <param name="serverId"></param>
        /// <param name="playerId"></param>
        /// <param name="nickname"></param>
        /// <returns></returns>
        public async Task<PlayerPatchResponse> PatchAsync(uint serverId, string playerId, string nickname)
        {
            using PooledObject<PlayerPatchRequest> _ =
                GenericPool<PlayerPatchRequest>.Get(out PlayerPatchRequest request);
            request.ServerId = serverId;
            request.PlayerId = playerId;
            request.Nickname = nickname;
            PlayerPatchResponse response = await PatchAsync(request);
            return response;
        }

        /// <summary>
        /// 닉네임 중복검사를 합니다.
        /// </summary>
        /// <param name="nickname">중복체크할 닉네임</param>
        /// <param name="serverId">체크할 닉네임의 서버 id</param>
        /// <returns></returns>
        public async Task<PlayerCheckNicknameResponse> CheckNicknameAsync(string nickname, uint serverId)
        {
            using PooledObject<PlayerCheckNicknameRequest> _ =
                GenericPool<PlayerCheckNicknameRequest>.Get(out PlayerCheckNicknameRequest request);
            request.ServerId = serverId;
            request.Nickname = nickname;
            PlayerCheckNicknameResponse response = await CheckNicknameAsync(request);
            return response;
        }
    }
}