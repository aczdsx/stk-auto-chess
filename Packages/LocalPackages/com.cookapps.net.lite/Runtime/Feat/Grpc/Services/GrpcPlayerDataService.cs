/*
* Copyright (c) CookApps.
*/

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CookApps.NetLite.Utils;
using Google.Protobuf;
using Tech.Hive.V1;

namespace CookApps.NetLite.Feat.Grpc
{
    [GrpcService(typeof(PlayerDataService.PlayerDataServiceClient))]
    public partial class GrpcPlayerDataService
    {
        // 요청 제한 시간
        private const double DeadlineSeconds = 30.0;

        /// <summary>
        /// 플레이어 데이터 목록을 로드합니다.
        /// </summary>
        /// <param name = "categories">카테고리 목록</param>
        /// <returns></returns>
        public async Task<PlayerDataListResponse> ListAsync(IEnumerable<string> categories, double? deadlineSeconds = DeadlineSeconds, CancellationToken cancellationToken = default)
        {
            deadlineSeconds ??= DeadlineSeconds;
            categories ??= System.Array.Empty<string>();
            PlayerDataListRequest request = new();
            foreach (string category in categories)
            {
                request.Categories.Add(category);
            }
            PlayerDataListResponse resp = null; // await ExecuteAsync(ServiceClient.ListAsync, request, deadlineSeconds.Value, cancellationToken);
            return resp;
        }

        /// <summary>
        /// 플레이어 데이터를 저장합니다. 두 개 이상의 데이터를 저장할 때 사용하세요
        /// </summary>
        /// <param name = "categoryData">저장할 데이터들. key에는 category, value에는 저장할 데이터를 설정해주세요.</param>
        /// <returns></returns>
        public async Task<PlayerDataSetResponse> SetAsync(IReadOnlyDictionary<string, string> categoryData, double? deadlineSeconds = DeadlineSeconds, CancellationToken cancellationToken = default)
        {
            deadlineSeconds ??= DeadlineSeconds;
            PlayerDataSetRequest request = new();
            categoryData ??= new Dictionary<string, string>();
            using IEnumerator<KeyValuePair<string, string>> enumerator = categoryData.GetEnumerator();
            while (enumerator.MoveNext())
            {
                string category = enumerator.Current.Key;
                string data = enumerator.Current.Value;
                request.PlayerDatas.Add(category, ByteString.CopyFrom(GZipUtil.CompressFromUtf8String(data)));
            }
            PlayerDataSetResponse resp = null; // await ExecuteAsync(ServiceClient.SetAsync, request, deadlineSeconds.Value, cancellationToken);
            return resp;
        }
    }
}
