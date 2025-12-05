using System.Threading;
using CookApps.NetLite;
using Tech.Hive.V1;
using Cysharp.Threading.Tasks;

namespace CookApps.AutoBattler
{
    [GrpcService(typeof(Tech.Hive.V1.PostService.PostServiceClient))]
    public partial class PostService
    {
        /// <summary>
        /// 우편 목록 가져오기
        /// </summary>
        public async UniTask<PostListResponse> ListAsync(CancellationToken cancellationToken = default)
        {
            PostListResponse resp = await ExecuteAsync(
                ServiceClient.ListAsync,
                new PostListRequest(),
                cancellationToken: cancellationToken
            );
            return resp;
        }

        /// <summary>
        /// 우편 읽기
        /// </summary>
        public async UniTask<PostReadResponse> ReadAsync(string postId, CancellationToken cancellationToken = default)
        {
            PostReadResponse resp = await ExecuteAsync(
                ServiceClient.ReadAsync,
                new PostReadRequest { Id = postId },
                cancellationToken: cancellationToken
            );
            return resp;
        }

        /// <summary>
        /// 우편 보상 수령
        /// </summary>
        public async UniTask<PostRewardResponse> ClaimAsync(string postId, CancellationToken cancellationToken = default)
        {
            PostRewardResponse resp = await ExecuteAsync(
                ServiceClient.RewardAsync,
                new PostRewardRequest { Id = postId },
                cancellationToken: cancellationToken
            );
            return resp;
        }
    }
}
