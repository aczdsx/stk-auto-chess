/*
* Copyright (c) CookApps.
*/

using CookApps.NetLite.Utils;
using Grpc.Core;
using Grpc.Core.Interceptors;

namespace CookApps.NetLite.Feat.Grpc
{
    /// <summary>
    /// Request ID를 생성하여 gRPC 메타데이터에 추가하는 인터셉터
    /// </summary>
    internal class GrpcRequestIdInterceptor : GrpcInterceptor
    {
        public override int Order  => 0;

        public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(
            TRequest request,
            ClientInterceptorContext<TRequest, TResponse> context,
            AsyncUnaryCallContinuation<TRequest, TResponse> continuation)
            where TRequest : class
        {
            string requestId = Ulid.NewOrderedUlid();
            context.Options.Headers?.Add("request-id", requestId);
            return base.AsyncUnaryCall(request, context, continuation);
        }
    }
}
