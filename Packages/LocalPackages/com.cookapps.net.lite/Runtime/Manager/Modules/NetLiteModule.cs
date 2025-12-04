/*
* Copyright (c) CookApps.
*/

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using Autofac;
using Autofac.Core;
using CookApps.NetLite.Feat.Grpc;
using CookApps.NetLite.Initialize;
using Grpc.Core;
using Module = Autofac.Module;

namespace CookApps.NetLite.Manager.Modules
{
    /// <summary>
    /// NetLiteModule 클래스는 Autofac의 Module을 상속받아 네트워크 및 데이터베이스 관련 서비스들을 DI 컨테이너에 등록하는 역할을 합니다.
    /// 생성자에서 NetLiteInitializeParam과 CancellationToken을 받아 저장하며,
    /// Load 메서드에서 GrpcInterceptor, GrpcHeaderProvider, ChannelProvider, DB 등 다양한 의존성을 등록합니다.
    /// </summary>
    internal class NetLiteModule : Module
    {
        private readonly NetLiteInitializeParam _param;
        private readonly CancellationToken _token;
        private readonly NetLiteManagerBase _managerInstance;

        public NetLiteModule(NetLiteInitializeParam param, CancellationToken token, NetLiteManagerBase managerInstance)
        {
            _param = param;
            _token = token;
            _managerInstance = managerInstance;
        }

        protected override void Load(ContainerBuilder cb)
        {
            cb.RegisterInstance(_param).AsSelf();

            // CancellationToken 등록
            cb.Register(c => _token)
                .AsSelf()
                .InstancePerLifetimeScope();

            // 모듈 등록
            cb.RegisterModule(new GrpcModule(_param));
            cb.RegisterModule(new DatabaseModule());

            // 서비스 등록
            InstallServicesBindings(cb, _managerInstance);
        }

        /// <summary>
        /// NetLiteManagerBase의 모든 gRPC 서비스를 자동으로 탐색하여 DI 컨테이너에 등록합니다.
        /// </summary>
        private static void InstallServicesBindings(ContainerBuilder cb, NetLiteManagerBase managerInstance)
        {
            foreach ((PropertyInfo property, FieldInfo backingField) in GetGrpcServiceProperties(managerInstance))
            {
                Type serviceWrapperType = property.PropertyType;
                Type grpcClientType = serviceWrapperType.GetCustomAttribute<GrpcServiceAttribute>().ServiceType;

                RegisterGrpcClient(cb, grpcClientType);
                RegisterServiceWrapper(cb, serviceWrapperType, grpcClientType, managerInstance, backingField);
            }
        }

        /// <summary>
        /// gRPC 클라이언트를 DI 컨테이너에 등록합니다.
        /// 예: Tech.Hive.V1.LobbyService.LobbyServiceClient
        /// </summary>
        private static void RegisterGrpcClient(ContainerBuilder cb, Type grpcClientType)
        {
            cb.RegisterType(grpcClientType)
                .AsSelf()
                .WithParameter(new ResolvedParameter(
                    (pi, ctx) => pi.ParameterType == typeof(CallInvoker),
                    (pi, ctx) => ctx.Resolve<IChannelProvider>().CallInvoker))
                .InstancePerLifetimeScope();
        }

        /// <summary>
        /// gRPC 서비스 래퍼를 DI 컨테이너에 등록하고, 매니저 프로퍼티에 주입합니다.
        /// 예: GrpcLobbyService
        /// </summary>
        private static void RegisterServiceWrapper(
            ContainerBuilder cb,
            Type serviceWrapperType,
            Type grpcClientType,
            NetLiteManagerBase managerInstance,
            FieldInfo backingField)
        {
            cb.RegisterType(serviceWrapperType)
                .AsSelf()
                .AsImplementedInterfaces()
                .InstancePerLifetimeScope()
                .AutoActivate()
                .OnActivated(e =>
                {
                    // 매니저의 프로퍼티에 서비스 인스턴스를 주입
                    backingField.SetValue(managerInstance, e.Instance);

                    if (e.Instance is not GrpcServiceBase service)
                    {
                        throw new InvalidOperationException(
                            $"Grpc service '{serviceWrapperType.Name}' must inherit GrpcServiceBase.");
                    }

                    // GrpcServiceBase에 필요한 의존성 주입
                    SetupGrpcService(service, grpcClientType, e.Context);
                })
                .OnRelease(_ =>
                {
                    // 매니저의 프로퍼티를 null로 초기화
                    backingField.SetValue(managerInstance, null);
                });
        }

        /// <summary>
        /// GrpcServiceBase의 SetService 메서드를 리플렉션을 통해 호출하여 의존성을 주입합니다.
        /// </summary>
        private static void SetupGrpcService(GrpcServiceBase service, Type grpcClientType, IComponentContext context)
        {
            // GrpcServiceBase<TService>.SetService(IAsyncUnaryCallExecutor, TService) 메서드 찾기
            MethodInfo setServiceMethod = typeof(GrpcServiceBase<>)
                .MakeGenericType(grpcClientType)
                .GetMethod(nameof(GrpcServiceBase<object>.SetService), BindingFlags.Public | BindingFlags.Instance);

            if (setServiceMethod == null)
            {
                throw new InvalidOperationException(
                    $"SetService method not found in GrpcServiceBase<{grpcClientType.Name}>.");
            }

            var asyncUnaryCallExecutor = context.Resolve<IAsyncUnaryCallExecutor>();
            object grpcClient = context.Resolve(grpcClientType);

            setServiceMethod.Invoke(service, new[] { asyncUnaryCallExecutor, grpcClient });
        }

        /// <summary>
        /// NetLiteManagerBase에서 GrpcServiceAttribute가 적용된 모든 프로퍼티를 찾습니다.
        /// </summary>
        private static IEnumerable<(PropertyInfo property, FieldInfo backingField)> GetGrpcServiceProperties(
            NetLiteManagerBase managerInstance)
        {
            foreach (PropertyInfo property in managerInstance.GetType()
                .GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic))
            {
                Type propertyType = property.PropertyType;

                // GrpcServiceAttribute가 없으면 스킵
                if (!Attribute.IsDefined(propertyType, typeof(GrpcServiceAttribute)))
                    continue;

                // GrpcServiceBase를 상속하지 않으면 스킵
                if (!typeof(GrpcServiceBase).IsAssignableFrom(propertyType))
                    continue;

                // auto-property의 backing field 찾기
                FieldInfo backingField = property.DeclaringType?
                    .GetField($"<{property.Name}>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance);

                if (backingField == null)
                    continue;

                yield return (property, backingField);
            }
        }
    }
}
