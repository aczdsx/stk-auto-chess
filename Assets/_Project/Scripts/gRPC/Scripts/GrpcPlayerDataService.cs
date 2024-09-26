/*
* Copyright (c) CookApps.
*/

using System.Collections.Generic;
using System.Threading.Tasks;
using CookApps.gRPC;
using Google.Protobuf;
using Tech.Hive.V1;
using UnityEngine.Pool;

namespace CookApps.AutoBattler
{
    public partial class GrpcPlayerDataService
    {
        /// <summary>
        /// 유저데이터 목록을 로드합니다.
        /// </summary>
        /// <param name="categories"></param>
        /// <returns></returns>
        public async Task<PlayerDataListResponse> ListAsync(IEnumerable<string> categories)
        {
            using PooledObject<PlayerDataListRequest> _ = GenericPool<PlayerDataListRequest>.Get(out PlayerDataListRequest request);
            request.Categories.Clear();
            foreach (string category in categories)
            {
                request.Categories.Add(category);
            }

            PlayerDataListResponse response = await ListAsync(request);
            return response;
        }

        /// <summary>
        /// 유저데이터를 로드합니다.
        /// </summary>
        /// <param name="category"></param>
        /// <returns></returns>
        public async Task<PlayerDataGetResponse> GetAsync(string category)
        {
            using PooledObject<PlayerDataGetRequest> _ = GenericPool<PlayerDataGetRequest>.Get(out PlayerDataGetRequest request);
            request.Category = category;
            PlayerDataGetResponse response = await GetAsync(request);
            return response;
        }

        /// <summary>
        /// 유저데이터를 저장합니다. 1개의 데이터만 저장합니다.
        /// </summary>
        /// <param name="category"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public async Task<PlayerDataSetResponse> SetAsync(string category, IMessage data)
        {
            using PooledObject<PlayerDataSetRequest> _ = GenericPool<PlayerDataSetRequest>.Get(out PlayerDataSetRequest request);
            request.PlayerDatas.Clear();
            request.PlayerDatas.Add(category, MessageUtility.ToBase64String(data));
            PlayerDataSetResponse response = await SetAsync(request);
            return response;
        }

        /// <summary>
        /// 유저데이터를 저장합니다. 1개 이상의 데이터를 저장할 때 사용하세요
        /// </summary>
        /// <param name="datas">저장할 데이터들. key에는 category, value에는 저장할 데이터를 set해주세요.</param>
        /// <returns></returns>
        public async Task<PlayerDataSetResponse> SetAsync(IReadOnlyDictionary<string, string> datas)
        {
            using PooledObject<PlayerDataSetRequest> _ = GenericPool<PlayerDataSetRequest>.Get(out PlayerDataSetRequest request);
            request.PlayerDatas.Clear();

            using var enumerator = datas.GetEnumerator();
            while (enumerator.MoveNext())
            {
                var category = enumerator.Current.Key;
                var data = enumerator.Current.Value;
                request.PlayerDatas.Add(category, data);
            }

            PlayerDataSetResponse response = await SetAsync(request);
            return response;
        }
    }
}