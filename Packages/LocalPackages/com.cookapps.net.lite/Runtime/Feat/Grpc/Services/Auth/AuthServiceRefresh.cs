/*
 * Copyright (c) CookApps.
 */

using System;
using System.Collections;
using CookApps.Coroutine;
using Grpc.Core;
using Tech.Hive.V1;
using UnityEngine;

namespace CookApps.NetLite.Feat.Grpc
{
    /// <summary>
    /// uv 정보 갱신을 위한 주기적인 AuthService.Refresh 호출
    /// </summary>
    internal class AuthServiceRefresh
    {
        private readonly GrpcAuthService _grpcAuthService;
        private readonly WaitForSecondsRealtime _refreshDelay = new(5 * 60); // 5분
        private UnityEngine.Coroutine _coroutine;

        public AuthServiceRefresh(GrpcAuthService grpcAuthService)
        {
            _grpcAuthService = grpcAuthService;
        }

        public void Stop()
        {
            if (_coroutine == null)
            {
                return;
            }

            CoroutineUtils.StopCoroutine(_coroutine);
            _coroutine = null;
        }

        public void Start()
        {
            if (_coroutine != null)
                return;

            _coroutine ??= CoroutineUtils.StartCoroutine(RefreshCoroutine());
        }

        // 코루틴으로 주기적으로 호출
        private IEnumerator RefreshCoroutine()
        {
            while (true)
            {
                yield return _refreshDelay;
                Refresh();
            }
            // ReSharper disable once IteratorNeverReturns
        }

        private async void Refresh()
        {
            try
            {
                RefreshResponse resp = await _grpcAuthService.RefreshAsync();
                // PermissionDenied(중복 로그인) 또는 Unauthenticated(인증 실패) 상태 코드일 경우 정지
                // if (resp.Exception is RpcException { StatusCode: StatusCode.PermissionDenied or StatusCode.Unauthenticated })
                // {
                //     Stop();
                // }
            }
            catch (Exception)
            {
                // ignored
            }
        }
    }
}
