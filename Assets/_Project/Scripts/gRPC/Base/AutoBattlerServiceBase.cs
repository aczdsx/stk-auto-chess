/*
* Copyright (c) CookApps.
*/

using System.Threading;
using System.Threading.Tasks;
using CookApps.NetLite;
using CookApps.NetLite.Feat.Grpc;
using Cysharp.Threading.Tasks;

namespace CookApps.AutoBattler
{
    /// <summary>
    /// 모든 Auto Battler gRPC 서비스의 기본 클래스
    /// ExecuteAsync + 자동 에러 체크 기능을 제공합니다.
    /// </summary>
    public abstract class AutoBattlerServiceBase : GrpcServiceBase
    {
        /// <summary>
        /// ExecuteAsync를 호출하고 자동으로 ThrowIfError를 실행합니다.
        /// </summary>
        protected async UniTask<TResponse> ExecuteWithAutoErrorCheck<TRequest, TResponse>(
            AsyncUnaryCallDelegate<TRequest, TResponse> call,
            TRequest request,
            double? deadline = null,
            CancellationToken cancellationToken = default)
            where TResponse : IGrpcMessageResponse<TResponse>, new()
            where TRequest : IGrpcMessageRequest<TRequest>, new()
        {
            var response = await ExecuteAsync(call, request, deadline, cancellationToken);
            response.ThrowIfError();
            return response;
        }

        /// <summary>
        /// ExecuteAsync를 호출하고 자동으로 ThrowIfError를 실행합니다. (Task 버전)
        /// </summary>
        protected async Task<TResponse> ExecuteWithAutoErrorCheckTask<TRequest, TResponse>(
            AsyncUnaryCallDelegate<TRequest, TResponse> call,
            TRequest request,
            double? deadline = null,
            CancellationToken cancellationToken = default)
            where TResponse : IGrpcMessageResponse<TResponse>, new()
            where TRequest : IGrpcMessageRequest<TRequest>, new()
        {
            var response = await ExecuteAsync(call, request, deadline, cancellationToken);
            response.ThrowIfError();
            return response;
        }
    }

    /// <summary>
    /// 제네릭 서비스 클라이언트를 지원하는 Auto Battler gRPC 서비스 기본 클래스
    /// </summary>
    public abstract class AutoBattlerServiceBase<TService> : AutoBattlerServiceBase
    {
        protected TService ServiceClient { get; set; }

        public void SetService(IAsyncUnaryCallExecutor callExecutor, TService serviceClient)
        {
            CallExecutor = callExecutor;
            ServiceClient = serviceClient;
        }
    }
}