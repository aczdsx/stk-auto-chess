/*
* Copyright (c) CookApps.
*/

using Grpc.Core;

namespace CookApps.NetLite
{
    public delegate AsyncUnaryCall<TResponse> AsyncUnaryCallDelegate<in TRequest, TResponse>(TRequest request, CallOptions options);
}
