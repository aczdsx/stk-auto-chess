/*
* Copyright (c) CookApps.
*/

using System;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;

namespace CookApps.NetLite.Feat.Grpc
{
    /// <summary>
    /// gRPC 비동기 단일 호출을 실행하는 Executor 인터페이스입니다.
    /// </summary>
    public interface IAsyncUnaryCallExecutor
    {
        Task<TResponse> ExecuteAsync<TRequest, TResponse>(
            AsyncUnaryCallDelegate<TRequest, TResponse> call, TRequest request, double? deadline = null, CancellationToken cancellationToken = default)
            where TResponse : IGrpcMessageResponse<TResponse>, new()
            where TRequest : IGrpcMessageRequest<TRequest>, new();
    }


    /// <summary>
    /// gRPC 비동기 단일 호출을 실행하는 Executor 클래스입니다.
    /// </summary>
    public class AsyncUnaryCallExecutor : IAsyncUnaryCallExecutor
    {
        private readonly IGrpcHeaderProvider _headerProvider;

        public AsyncUnaryCallExecutor(IGrpcHeaderProvider headerProvider)
        {
            _headerProvider = headerProvider;
        }

        public async Task<TResponse> ExecuteAsync<TRequest, TResponse>(
            AsyncUnaryCallDelegate<TRequest, TResponse> call, TRequest request, double? deadline, CancellationToken cancellationToken)
            where TResponse : IGrpcMessageResponse<TResponse>, new()
            where TRequest : IGrpcMessageRequest<TRequest>, new()
        {
            try
            {
                CallOptions callOptions = _headerProvider.GetClientCallOptions(deadline, cancellationToken);
                TResponse response = await call(request, callOptions);
                return response;
            }
            catch (RpcException rpcEx)
            {
                var response = new TResponse
                {
                    Exception = rpcEx,
                };
                return response;
            }
            catch (Exception e)
            {
                var response = new TResponse
                {
                    Exception = e,
                };
                return response;
            }
        }
    }
}
