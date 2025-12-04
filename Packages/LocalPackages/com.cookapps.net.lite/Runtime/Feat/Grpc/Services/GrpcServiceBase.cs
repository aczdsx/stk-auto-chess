/*
* Copyright (c) CookApps.
*/

using System.Threading;
using System.Threading.Tasks;

namespace CookApps.NetLite.Feat.Grpc
{
    public abstract class GrpcServiceBase
    {
        protected IAsyncUnaryCallExecutor CallExecutor { get; set; }

        protected Task<TResponse> ExecuteAsync<TRequest, TResponse>(
            AsyncUnaryCallDelegate<TRequest, TResponse> call, TRequest request,
            double? deadline = null,
            CancellationToken cancellationToken = default)
            where TResponse : IGrpcMessageResponse<TResponse>, new()
            where TRequest : IGrpcMessageRequest<TRequest>, new()
        {
            return CallExecutor.ExecuteAsync(call, request, deadline, cancellationToken);
        }
    }

    public abstract class GrpcServiceBase<TService> : GrpcServiceBase
    {
        protected TService ServiceClient { get; set; }

        public void SetService(IAsyncUnaryCallExecutor callExecutor, TService serviceClient)
        {
            CallExecutor = callExecutor;
            ServiceClient = serviceClient;
        }
    }
}
