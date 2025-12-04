/*
* Copyright (c) CookApps.
*/

using System;
using System.Threading.Tasks;
using Grpc.Core;
using UnityEngine;

namespace CookApps.NetLite.Utils
{
    public class GrpcCallHelper
    {
        /// gRPC 호출을 실행하고, RPC 오류(Unavailable, DeadlineExceeded)가 발생하면 재시도합니다.
        public static async Task<T> CallWithRetry<T>(
            Func<Task<T>> call,
            int maxRetries = 3,
            float delaySecond = 1f)
            where T : IGrpcMessageResponse<T>
        {
            T lastResponse = default;
            for (var attempt = 0; attempt < maxRetries; attempt++)
            {
                try
                {
                    lastResponse = await call();
                    lastResponse.ThrowIfError();  // 응답 오류 확인 및 예외 발생
                    return lastResponse;
                }
                catch (RpcException ex) when (IsRetryable(ex.StatusCode) && attempt < maxRetries - 1)
                {
                    await Awaitable.WaitForSecondsAsync(delaySecond * (attempt + 1));
                }
                catch (Exception)
                {
                    return lastResponse;
                }
            }

            return lastResponse;
        }

        private static bool IsRetryable(StatusCode statusCode)
        {
            return statusCode is StatusCode.Unavailable or StatusCode.DeadlineExceeded;
        }
    }
}
