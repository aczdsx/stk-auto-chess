using System.Collections.Generic;
using System.Threading;
using CookApps.NetLite;
using Cysharp.Threading.Tasks;
using Google.Protobuf;
using Tech.Hive.V1;

namespace CookApps.AutoBattler
{
    /// <summary>
    /// ClientData 서비스 커스텀 구현
    /// NetLite의 GrpcPlayerDataService와 동일한 RPC를 사용하지만
    /// MemoryPack으로 직렬화하여 데이터 저장/조회
    /// </summary>
    [GrpcService(typeof(Tech.Hive.V1.PlayerDataService.PlayerDataServiceClient))]
    public partial class ClientDataService
    {
        /// <summary>
        /// 플레이어 데이터 목록을 로드합니다.
        /// </summary>
        public async UniTask<PlayerDataListResponse> ListAsync(
            IEnumerable<string> categories = null,
            CancellationToken cancellationToken = default)
        {
            categories ??= System.Array.Empty<string>();

            var request = new PlayerDataListRequest();
            foreach (string category in categories)
            {
                request.Categories.Add(category);
            }

            PlayerDataListResponse resp = await ExecuteWithCommonErrorCheck(
                ServiceClient.ListAsync,
                request,
                cancellationToken: cancellationToken
            );

            // ClientDataManager에 데이터 연동
            if (resp is { IsSuccess: true, Data: not null })
            {
                var itemList = resp.Data.ItemList;
                for (int i = 0; i < itemList.Count; i++)
                {
                    var item = itemList[i];
                    ClientDataManager.Instance.SetData(item.Category, item.Data.ToByteArray());
                }
            }

            return resp;
        }

        /// <summary>
        /// 플레이어 데이터를 조회합니다.
        /// </summary>
        public async UniTask<PlayerDataGetResponse> GetAsync(
            string category,
            CancellationToken cancellationToken = default)
        {
            var request = new PlayerDataGetRequest { Category = category };

            PlayerDataGetResponse resp = await ExecuteWithCommonErrorCheck(
                ServiceClient.GetAsync,
                request,
                cancellationToken: cancellationToken
            );

            // ClientDataManager에 데이터 연동
            if (resp is { IsSuccess: true, Data: { Item: not null } })
            {
                var item = resp.Data.Item;
                ClientDataManager.Instance.SetData(item.Category, item.Data.ToByteArray());
            }

            return resp;
        }

        /// <summary>
        /// 여러 카테고리 데이터를 일괄 저장합니다.
        /// </summary>
        public async UniTask<PlayerDataSetResponse> SetAsync(
            Dictionary<string, byte[]> categoryData,
            CancellationToken cancellationToken = default)
        {
            var request = new PlayerDataSetRequest();
            categoryData ??= new Dictionary<string, byte[]>();

            foreach (var kvp in categoryData)
            {
                request.PlayerDatas.Add(kvp.Key, ByteString.CopyFrom(kvp.Value));
            }

            PlayerDataSetResponse resp = await ExecuteWithCommonErrorCheck(
                ServiceClient.SetAsync,
                request,
                cancellationToken: cancellationToken
            );

            return resp;
        }
    }
}
