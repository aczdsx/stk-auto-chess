/*
* Copyright (c) CookApps.
*/

using Grpc.Core;
using Grpc.Core.Interceptors;
using Grpc.Net.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using CookApps.NetLite.Feat.Logger;

namespace CookApps.NetLite.Feat.Grpc
{
    /// <summary>
    /// gRPC 채널과 인터셉터가 적용된 CallInvoker를 제공하는 역할을 추상화합니다.
    /// </summary>
    public interface IChannelProvider
    {
        /// <summary>
        /// 생성된 gRPC 채널 원본입니다.
        /// </summary>
        GrpcChannel Channel { get; }
        CallInvoker CallInvoker { get; }
    }

    /// <summary>
    /// gRPC 채널과 인터셉터가 적용된 CallInvoker를 생성하고 관리합니다.
    /// IDisposable을 구현하여 NetLiteManagerBase의 LifetimeScope가 종료될 때 채널 리소스를 안전하게 해제합니다.
    /// </summary>
    internal class GrpcChannelProvider : IChannelProvider, IDisposable
    {
        private bool _disposed;
        private static readonly string Tag = $"[{nameof(GrpcChannelProvider)}]";

        /// <summary>
        /// 생성된 gRPC 채널 원본입니다.
        /// </summary>
        public GrpcChannel Channel { get; }

        /// <summary>
        /// 모든 인터셉터가 적용된 CallInvoker입니다. gRPC 클라이언트는 이 CallInvoker를 통해 서버와 통신합니다.
        /// </summary>
        public CallInvoker CallInvoker { get; }

        /// <summary>
        /// GrpcChannelProvider를 초기화합니다.
        /// </summary>
        /// <param name="address">gRPC 서버 주소</param>
        /// <param name="interceptors">DI 컨테이너에 등록된 모든 인터셉터 컬렉션</param>
        public GrpcChannelProvider(string address, IEnumerable<GrpcInterceptor> interceptors)
        {
#if UNITY_WEBGL
            var httpMessageHandler = new GrpcWebSocketBridge.Client.GrpcWebSocketBridgeHandler();
#else
            var httpMessageHandler = new Cysharp.Net.Http.YetAnotherHttpHandler
            {
                Http2Only = true,
                Http2KeepAliveTimeout = TimeSpan.FromSeconds(5),
                Http2KeepAliveInterval = TimeSpan.FromSeconds(20),
                Http2KeepAliveWhileIdle = true,
            };
#endif

            address = address.Trim();
            ChannelCredentials credentials = address.StartsWith("https", StringComparison.OrdinalIgnoreCase)
                ? ChannelCredentials.SecureSsl
                : ChannelCredentials.Insecure;

            Channel = GrpcChannel.ForAddress(address,  new GrpcChannelOptions
            {
                DisposeHttpClient = true,
                HttpHandler = httpMessageHandler,
                Credentials = credentials == ChannelCredentials.Insecure ? ChannelCredentials.Insecure : ChannelCredentials.SecureSsl,
            });

            // 인터셉터를 Order 속성 기준으로 정렬하고 CallInvoker에 적용
            Interceptor[] orderInterceptor = interceptors.OrderBy(x => x.Order).OfType<Interceptor>().ToArray();
            CallInvoker = Channel.Intercept(orderInterceptor);
        }

        /// <summary>
        /// 채널 리소스를 정리합니다. Autofac의 LifetimeScope가 종료될 때 자동으로 호출됩니다.
        /// </summary>
        void IDisposable.Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                // 관리되는 리소스 (Managed Resources) 정리
                try
                {
                    Channel?.Dispose();
                }
                catch (Exception ex)
                {
                    NetLogger.LogError(Tag, $"Failed to dispose GrpcChannel: {ex.Message}");
                }
            }

            _disposed = true;
        }
    }
}
