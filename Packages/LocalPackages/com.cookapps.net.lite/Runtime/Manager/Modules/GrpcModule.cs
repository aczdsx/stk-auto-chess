/*
* Copyright (c) CookApps.
*/

using System.Collections.Generic;
using Autofac;
using CookApps.NetLite.Feat.Grpc;
using CookApps.NetLite.Initialize;
using Module = Autofac.Module;

namespace CookApps.NetLite.Manager.Modules
{
    /// <summary>
    /// Grpc 관련 의존성 주입을 담당하는 Autofac 모듈 클래스입니다.
    /// GrpcInterceptor, GrpcHeaderProvider, GrpcChannelProvider, AsyncUnaryCallExecutor 등을 등록합니다.
    /// </summary>
    internal class GrpcModule : Module
    {
        private readonly NetLiteInitializeParam _param;

        public GrpcModule(NetLiteInitializeParam param)
        {
            _param = param;
        }

        protected override void Load(ContainerBuilder cb)
        {
            // GrpcInterceptor 등록
            cb.RegisterType<GrpcSessionInterceptor>().As<GrpcInterceptor>().InstancePerLifetimeScope();
            cb.RegisterType<GrpcRequestIdInterceptor>().As<GrpcInterceptor>().InstancePerLifetimeScope();
            cb.RegisterType<GrpcSendAndLoggerInterceptor>().As<GrpcInterceptor>().InstancePerLifetimeScope();
            cb.RegisterType<GrpcSyncManifestInterceptor>().As<GrpcInterceptor>().InstancePerLifetimeScope();

            // GrpcHeaderProvider 등록
            cb.RegisterType<GrpcHeaderProvider>().AsImplementedInterfaces().InstancePerLifetimeScope();

            // ChannelProvider 등록
            cb.Register(c => new GrpcChannelProvider(_param.Address, c.Resolve<IEnumerable<GrpcInterceptor>>()))
                .As<IChannelProvider>()
                .InstancePerLifetimeScope();

            // AsyncUnaryCallExecutor 등록
            cb.RegisterType<AsyncUnaryCallExecutor>().AsImplementedInterfaces().InstancePerLifetimeScope();
        }
    }
}
