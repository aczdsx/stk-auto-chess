/*
* Copyright (c) CookApps.
*/

using Grpc.Core.Interceptors;

namespace CookApps.NetLite.Feat.Grpc
{
    /// 실행 순서 보장되는 인터셉터
    internal abstract class GrpcInterceptor : Interceptor
    {
        /// <summary>
        /// 인터셉터 간 실행 순서를 정의합니다.<br/>
        /// </summary>
        public abstract int Order { get; }
    }
}
