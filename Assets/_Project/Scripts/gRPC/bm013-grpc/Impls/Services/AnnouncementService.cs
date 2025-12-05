using System.Threading;
using CookApps.NetLite;
using Tech.Hive.V1;
using Cysharp.Threading.Tasks;

namespace CookApps.AutoBattler
{
    [GrpcService(typeof(Tech.Hive.V1.AnnouncementService.AnnouncementServiceClient))]
    public partial class AnnouncementService
    {
        /// <summary>
        /// 공지사항 목록 조회
        /// </summary>
        public async UniTask<AnnouncementListResponse> ListAsync(CancellationToken cancellationToken = default)
        {
            AnnouncementListResponse resp = await ExecuteAsync(
                ServiceClient.ListAsync,
                new AnnouncementListRequest(),
                cancellationToken: cancellationToken
            );
            return resp;
        }
    }
}
