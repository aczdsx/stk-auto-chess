/*
* Copyright (c) CookApps.
*/

using System;
using System.Text;
using System.Threading.Tasks;
using CookApps.NetLite.Feat.Logger;
using Grpc.Core;
using Grpc.Core.Interceptors;
using UnityEngine.Pool;

namespace CookApps.NetLite.Feat.Grpc
{
    /// <summary>
    /// 마지막 단계 전송 및 로그 출력
    /// </summary>
    internal class GrpcSendAndLoggerInterceptor : GrpcInterceptor
    {
        public override int Order => 10000;
        private readonly NetLogger.TaggedLogger _logger = new(string.Empty);

        public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(
            TRequest request,
            ClientInterceptorContext<TRequest, TResponse> context,
            AsyncUnaryCallContinuation<TRequest, TResponse> continuation)
            where TRequest : class
        {

            AsyncUnaryCall<TResponse> call = continuation(request, context);
            Task<TResponse> wrappedResponse = WrapResponseAsync(call.ResponseAsync, context.Options.Headers);
            return new AsyncUnaryCall<TResponse>(
                wrappedResponse,
                call.ResponseHeadersAsync,
                call.GetStatus,
                call.GetTrailers,
                call.Dispose);

            //----------------------------------------------------------------------

            async Task<TResponse> WrapResponseAsync(Task<TResponse> inner, Metadata optionsHeaders)
            {
                try
                {
                    // 요청 로그
                    _logger.Log($"[Send]: {context.Method.Name}/{request.GetType().Name}, Request: {request}\nMetadata: {MetadataToDebugString(optionsHeaders)}");
                    TResponse response = await inner;
                    // 응답 로그
                    _logger.Log($"[Recv]: {context.Method.Name}/{request.GetType().Name}, Response: {response}");
                    return response;
                }
                catch (RpcException rex)
                {
                    _logger.LogError($"[Error]: {context.Method.Name}/{request.GetType().Name}, RpcException: {rex.Status.StatusCode} - {rex.Status.Detail}");
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError($"[Error]: {context.Method.Name}, Exception: {ex}");
                    throw;
                }
            }
        }

        // Metadata를 Dictionary 형태의 string으로 변환하는 함수
        private static string MetadataToDebugString(Metadata metadata)
        {
            if (metadata == null || metadata.Count == 0)
                return "{}";

            StringBuilder sb = GenericPool<StringBuilder>.Get();
            sb.Clear();
            try
            {
                sb.Append("{ ");
                foreach (var entry in metadata)
                {
                    if (entry.Key == "sync-manifest")
                    {
                        // sync-manifest는 base64 문자열이므로 디코딩하여 출력
                        try
                        {
                            byte[] bytes = Convert.FromBase64String(entry.Value);
                            string decoded = Encoding.UTF8.GetString(bytes);
                            sb.Append($"{entry.Key}: {decoded}, ");
                        }
                        catch
                        {
                            sb.Append($"{entry.Key}: base64 decode error, ");
                        }
                    }
                    else
                    {
                        sb.Append($"{entry.Key}: {entry.Value}, ");
                    }
                }
                if (sb.Length > 2)
                    sb.Length -= 2; // 마지막 ", " 제거
                sb.Append(" }");
                return sb.ToString();
            }
            finally
            {
                GenericPool<StringBuilder>.Release(sb);
            }
        }

    }
}
