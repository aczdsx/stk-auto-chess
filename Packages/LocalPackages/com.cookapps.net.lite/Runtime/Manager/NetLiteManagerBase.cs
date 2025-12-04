/*
* Copyright (c) CookApps.
*/

using System;
using System.Collections.Generic;
using Autofac;
using CookApps.NetLite.Feat.Grpc;
using CookApps.NetLite.Initialize;
using System.Threading;
using CookApps.NetLite.Feat.Logger;
using CookApps.NetLite.Manager.Modules;
using UnityEngine.Assertions;
// ReSharper disable UnusedAutoPropertyAccessor.Local
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable MemberCanBeProtected.Global

namespace CookApps.NetLite.Manager
{
    /// <summary>
    /// NetLite 네트워크 관리자의 기본 클래스입니다.
    /// 서비스들의 생명주기를 관리합니다.
    /// </summary>
    [UnityEngine.Scripting.Preserve]
    public class NetLiteManagerBase
    {
        /// <summary>
        /// Auth 서비스
        /// </summary>
        public GrpcAuthService Auth { get; private set; }

        /// <summary>
        /// Spec(Game, Language, ETC) 서비스
        /// </summary>
        public GrpcSpecService Spec { get; private set; }

        /// <summary>
        /// Lobby 서비스
        /// </summary>
        public GrpcLobbyService Lobby { get; private set; }

        /// <summary>
        /// PlayerData 서비스
        /// </summary>
        public GrpcPlayerDataService PlayerData { get; private set; }

        /// <summary>
        /// Shop 서비스
        /// </summary>
        public GrpcShopService Shop { get; private set; }

        /// <summary>
        /// Dependency Injection 컨테이너입니다.
        /// </summary>
        private IContainer _container;

        /// <summary>
        /// 비동기 작업 취소를 위한 토큰 소스입니다.
        /// </summary>
        private CancellationTokenSource _cancellationTokenSource;

        /// <summary>
        /// 매니저의 시작 여부를 가져옵니다.
        /// <see cref="Startup"/> 호출 시 <c>true</c>, <see cref="Shutdown"/> 호출 시 <c>false</c>가 됩니다.
        /// </summary>
        public bool IsStarted { get; private set; }

        /// <summary>
        /// NetLite 매니저를 초기화하고 시작합니다.
        /// 모든 서비스를 준비하고 사용 가능한 상태로 만듭니다.
        /// </summary>
        /// <param name="param">초기화 파라미터</param>
        /// <exception cref="ArgumentNullException"><paramref name="param"/>이 <c>null</c>인 경우</exception>
        /// <exception cref="ArgumentException"><paramref name="param"/>이 유효하지 않은 경우</exception>
        public void Startup(NetLiteInitializeParam param)
        {
            if (param == null)
                throw new ArgumentNullException(nameof(param));

            if (!param.IsValid())
                throw new ArgumentException("Invalid NetLiteInitializeParam.", nameof(param));

            Assert.IsFalse(IsStarted, $"{GetType().Name} is already started.");

            // 이미 시작된 경우 무시
            if (IsStarted)
                return;

            NetLogger.SetLogEnabled(param.EnabledLog); // 로그 설정
            _cancellationTokenSource = new CancellationTokenSource();

            // 컨테이너 빌더 생성 및 모듈 등록
            var builder = new ContainerBuilder();
            // NetLite 모듈 등록
            builder.RegisterModule(new NetLiteModule(param, _cancellationTokenSource.Token, this));

            // 추가 모듈 등록
            foreach (Module module in GetAdditionalModules())
            {
                builder.RegisterModule(module);
            }

            // 컨테이너 빌드
            _container = builder.Build();

            // 앱 종료 시 정리 이벤트 등록
            UnityEngine.Application.quitting -= Shutdown;
            UnityEngine.Application.quitting += Shutdown;

            IsStarted = true;
            NetLogger.Log($"{GetType().Name} is started.");
        }

        /// <summary>
        /// NetLite 매니저를 종료하고 리소스를 정리합니다.
        /// 앱 종료 시 자동으로 호출되며, 수동으로도 호출할 수 있습니다.
        /// </summary>
        public void Shutdown()
        {
            // 이미 종료된 경우 무시
            if (!IsStarted)
                return;

            UnityEngine.Application.quitting -= Shutdown;

            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;

            _container?.Dispose();
            _container = null;

            IsStarted = false;
            NetLogger.Log($"{GetType().Name} is shutdown.");
        }

        /// <summary>
        /// 파생 클래스에서 추가 Autofac 모듈을 등록할 수 있도록 합니다.
        /// <see cref="Startup"/> 중에 호출되며, 기본 구현은 빈 시퀀스를 반환합니다.
        /// </summary>
        /// <returns>등록할 추가 모듈의 시퀀스</returns>
        protected internal virtual IEnumerable<Module> GetAdditionalModules()
        {
            yield break;
        }
    }
}
