/*
* Copyright (c) CookApps.
*/

using System;
using System.Buffers.Text;
using Grpc.Core;
using Grpc.Core.Interceptors;

namespace CookApps.NetLite.Feat.Grpc
{
    internal class GrpcSyncManifestInterceptor : GrpcInterceptor
    {
        private readonly GrpcSpecService _specService;
        public override int Order => 0;

        public GrpcSyncManifestInterceptor(GrpcSpecService specService)
        {
            _specService = specService;
        }

        public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(
            TRequest request,
            ClientInterceptorContext<TRequest, TResponse> context,
            AsyncUnaryCallContinuation<TRequest, TResponse> continuation)
            where TRequest : class
        {

            uint gameSpecVersion = _specService.CurrentGameSpecVersion;
            long timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(); // timestamp (초 단위)
            string syncManifest = SyncManifestBase64String(timestamp, gameSpecVersion);
            context.Options.Headers?.Add("sync-manifest", syncManifest);
            return base.AsyncUnaryCall(request, context, continuation);
        }

        private static string SyncManifestBase64String(long t, uint sv)
        {
            // JSON 문자열을 UTF‑8 바이트로 바로 작성할 스택 할당 버퍼 (128글자 넘어가는 경우 없음)
            Span<byte> jsonUtf8 = stackalloc byte[128];
            var pos = 0;

            // '{'
            jsonUtf8[pos++] = (byte)'{';

            // "t":
            jsonUtf8[pos++] = (byte)'"';
            jsonUtf8[pos++] = (byte)'t';
            jsonUtf8[pos++] = (byte)'"';
            jsonUtf8[pos++] = (byte)':';
            if (!Utf8Formatter.TryFormat(t, jsonUtf8[pos..], out int writtenT))
                throw new FormatException("t 값 포맷에 실패했습니다.");
            pos += writtenT;

            // 구분자 ", "
            jsonUtf8[pos++] = (byte)',';
            jsonUtf8[pos++] = (byte)' ';

            // "sv":
            jsonUtf8[pos++] = (byte)'"';
            jsonUtf8[pos++] = (byte)'s';
            jsonUtf8[pos++] = (byte)'v';
            jsonUtf8[pos++] = (byte)'"';
            jsonUtf8[pos++] = (byte)':';
            if (!Utf8Formatter.TryFormat(sv, jsonUtf8[pos..], out int writtenSv))
                throw new FormatException("sv 값 포맷에 실패했습니다.");
            pos += writtenSv;

            // '}'
            jsonUtf8[pos++] = (byte)'}';

            // Base64 인코딩 시 최종 길이 계산 (3바이트마다 4문자)
            int base64Length = ((pos + 2) / 3) * 4;
            Span<char> base64Buffer = base64Length <= 256 ? stackalloc char[base64Length] : new char[base64Length]; // 짧은 문자열이라 256 넘어가는일 없음

            if (!Convert.TryToBase64Chars(jsonUtf8[..pos], base64Buffer, out int _))
                throw new Exception("Base64 인코딩에 실패했습니다.");

            // 최종 결과 문자열 생성 (최종 문자열은 힙 할당되지만, 그 외에는 GC 발생 없음)
            return new string(base64Buffer);
        }
    }
}
