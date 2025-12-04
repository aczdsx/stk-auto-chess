/*
* Copyright (c) CookApps.
*/

using Grpc.Core;
using Grpc.Core.Interceptors;

namespace CookApps.NetLite.Feat.Grpc
{
    /// <summary>
    /// gRPC 세션 인터셉터
    /// </summary>
    internal class GrpcSessionInterceptor : GrpcInterceptor
    {
        private readonly IGrpcAuthServiceSession _grpcAuthServiceSession;
        public override int Order => 100;

        public GrpcSessionInterceptor(IGrpcAuthServiceSession grpcAuthServiceSession)
        {
            _grpcAuthServiceSession = grpcAuthServiceSession;
        }

        public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(
            TRequest request,
            ClientInterceptorContext<TRequest, TResponse> context,
            AsyncUnaryCallContinuation<TRequest, TResponse> continuation)
            where TRequest : class
        {

            string requestId = _grpcAuthServiceSession.SessionId;
            context.Options.Headers?.Add("session-id", requestId);
            return base.AsyncUnaryCall(request, context, continuation);
        }
    }
}
