/*
* Copyright (c) CookApps.
*/

using System.Runtime.CompilerServices;
using System.Threading;
using CookApps.NetLite.Initialize;
using Grpc.Core;
using UnityEngine;

namespace CookApps.NetLite.Feat.Grpc
{
    public interface IGrpcHeaderProvider
    {
        CallOptions GetClientCallOptions(double? deadline = null, CancellationToken cancellationToken = default);
    }

    internal class GrpcHeaderProvider : IGrpcHeaderProvider
    {
        private readonly CancellationToken _cancellationToken;
        private const string Version = "version";
        private const string SyncManifest = "sync-manifest";
        private const string DeviceId = "device-id";
        private const string Store = "store";
        private const string AppBundleName = "app-bundle-name";
        private const double DefaultDeadline = 5.0;

        private readonly Metadata _defaultMetadata;
        public GrpcHeaderProvider(NetLiteInitializeParam param, CancellationToken cancellationToken)
        {
            _cancellationToken = cancellationToken;
            _defaultMetadata = CreateMetadata(param);
        }

        /// <summary>
        /// 기본 메타데이터를 생성합니다.
        /// </summary>
        private static Metadata CreateMetadata(NetLiteInitializeParam param)
        {
            var metadata = new Metadata
            {
                { Version, Application.version },
                { DeviceId, SystemInfo.deviceUniqueIdentifier },
                { Store, ((int)param.Store).ToString() },
                { AppBundleName, Application.identifier },
            };
            return metadata;
        }

        /// <summary>
        /// 기본 메타데이터를 복사하여 새로운 Metadata 객체를 생성합니다.
        /// </summary>
        private static Metadata CopyDefaultMetadata(Metadata defaultMetadata)
        {
            Metadata metadata = new();
            foreach (Metadata.Entry entry in defaultMetadata)
            {
                metadata.Add(entry);
            }
            return metadata;
        }

        /// <summary>
        /// gRPC 호출 옵션을 생성하여 반환합니다. (Headers는 외부에서 변경할 수 있습니다.)
        /// </summary>
        CallOptions IGrpcHeaderProvider.GetClientCallOptions(double? deadline, CancellationToken cancellationToken)
        {
            CancellationToken ct = LinkIfNeeded(cancellationToken, _cancellationToken);
            Metadata metadata = CopyDefaultMetadata(_defaultMetadata);
            CallOptions options = new();
            options = options.WithHeaders(metadata);
            deadline ??= DefaultDeadline;
            return options
                .WithDeadline(System.DateTime.UtcNow.AddSeconds(deadline.Value))
                .WithCancellationToken(ct);
        }

        /// <summary>
        /// 두 CancellationToken을 연결하여 반환합니다.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static CancellationToken LinkIfNeeded(CancellationToken a, CancellationToken b)
        {
            if (a == b)
            {
                return a;
            }
            if (!a.CanBeCanceled)
            {
                return b;
            }
            if (!b.CanBeCanceled)
            {
                return a;
            }
            var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(a, b);
            return linkedCts.Token;
        }
    }
}
