/*
 * Copyright (c) CookApps.
 */

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using CookApps.NetLite.Feat.DB;
using CookApps.NetLite.Utils;
using Tech.Hive.V1;
using UnityEngine;

namespace CookApps.NetLite.Feat.Grpc
{
    [GrpcService(typeof(SpecService.SpecServiceClient))]
    public partial class GrpcSpecService
    {
        // spec 요청 제한 시간
        private const double DeadlineSeconds = 30.0;
        private readonly CommonDB _commonDB;

        // Spec 버전 캐시 (SpecType 별로 캐싱) (스레드 안전, 읽기가 많고 쓰기가 적은 경우에 적합)
        private ImmutableDictionary<SpecType, uint> _specVersionCache = ImmutableDictionary<SpecType, uint>.Empty;

        public GrpcSpecService(CommonDB commonDB)
        {
            _commonDB = commonDB;
        }

        /// 지금 사용중인 Game Spec 버전
        public uint CurrentGameSpecVersion { get; set; }

        /// <summary>
        /// Spec 데이터를 반환합니다.
        /// </summary>
        /// <param name="specType">스펙 종류</param>
        /// <param name="version">해당 스펙의 버전</param>
        /// <returns>해당 스펙 (오류 시 string.Empty 반환)</returns>
        /// <remarks>해당 스펙의 버전이 로컬 디스크에 캐싱 되어있으면 로컬 디스크에서 읽어 옵니다.</remarks>
        public async Task<string> GetSpecDataAsync(SpecType specType, uint version, double? deadlineSeconds = DeadlineSeconds, CancellationToken cancellationToken = default)
        {
            deadlineSeconds ??= DeadlineSeconds;
            return specType switch
            {
                // SpecType.Game => await GetCommonDataAsync(SpecType.Game, version, ServiceClient.GetSpecDataAsync, deadlineSeconds, cancellationToken),
                // SpecType.EtcSpec => await GetCommonDataAsync(SpecType.EtcSpec, version, ServiceClient.GetEtcSpecDataAsync, deadlineSeconds, cancellationToken),
                // SpecType.Language => await GetCommonDataAsync(SpecType.Language, version, ServiceClient.GetLanguageDataAsync, deadlineSeconds, cancellationToken),
                _ => string.Empty,
            };
        }

        /// <summary>
        /// 케싱되어 있는 Spec 버전을 반환합니다.
        /// </summary>
        public uint GetCachedSpecVersion(SpecType specType)
        {
            if (specType == SpecType.Unspecified)
                return 0;

            var snap = Volatile.Read(ref _specVersionCache);
            if(snap.TryGetValue(specType, out uint version))
            {
                return version;
            }
            version = DeserializeSpecVersion(specType);
            AddOrReplaceCacheVersion(specType, version);
            return version;
        }

        /// <summary>
        /// 케싱되어 있는 Spec 데이터를 반환합니다.
        /// </summary>
        /// <param name="specType">스펙 종류</param>
        /// <returns>해당 스펙 (오류 시 string.Empty 반환)</returns>
        public async Task<string> GetCachedSpecDataAsync(SpecType specType)
        {
            await Awaitable.BackgroundThreadAsync();
            CommonDBSpec spec = DeserializeSpec(specType);
            string result = spec != null ? GZipUtil.DecompressToUtf8String(spec.Data) : string.Empty;
            await Awaitable.MainThreadAsync();
            return result;
        }

        private async Task<string> GetCommonDataAsync(
            SpecType specType,
            uint version,
            AsyncUnaryCallDelegate<SpecDataRequest, SpecDataResponse> serviceMethod,
            double? deadlineSeconds = null,
            CancellationToken cancellationToken = default)
        {
            if (version <= 0)
            {
                return string.Empty;
            }


            // 먼저 백그라운드로 전환하여 로컬 캐시 확인 및 로컬에서의 복원(무거운 IO/메모리 작업) 수행
            await Awaitable.BackgroundThreadAsync();

            // 로컬에 같은 버전이 있으면 백그라운드에서 압축 해제 후 메인으로 전환하여 반환
            if (GetCachedSpecVersion(specType) == version)
            {
                CommonDBSpec spec = DeserializeSpec(specType);
                string deserializeJson = GZipUtil.DecompressToUtf8String(spec.Data);
                await Awaitable.MainThreadAsync(); // 반환은 메인 스레드에서
                return deserializeJson;
            }

            // 원격 호출(ExecuteAsync)은 메인 스레드에서 실행되어야 할 수 있으므로 메인으로 전환
            await Awaitable.MainThreadAsync();

            deadlineSeconds ??= DeadlineSeconds;
            SpecDataRequest request = new()
            {
                Version = version,
            };
            SpecDataResponse resp = null; // await ExecuteAsync(serviceMethod, request, deadlineSeconds.Value, cancellationToken);

            // if (!resp.IsSuccess)
            // {
            //     // 실패 응답이면 메인 스레드에서 빈 문자열 반환
            //     return string.Empty;
            // }

            // 성공 응답이면 다시 백그라운드로 전환하여 디스크 저장 및 압축 해제 같은 무거운 작업 수행
            await Awaitable.BackgroundThreadAsync();
            SerializeSpec(specType, version, resp.Data.Spec.Memory);
            string result = GZipUtil.DecompressToUtf8String(resp.Data.Spec.Memory);

            // 최종 반환은 메인 스레드에서
            await Awaitable.MainThreadAsync();
            return result;
        }

        private uint DeserializeSpecVersion(SpecType specType)
        {
            uint? version = _commonDB.Find<CommonDBSpec>(x => x.SpecType == specType)?.Version;
            return version ?? 0;
        }

        private CommonDBSpec DeserializeSpec(SpecType specType)
        {
            var dbSpec = _commonDB.FindById<CommonDBSpec>(specType);
            return dbSpec;
        }

        private void SerializeSpec(SpecType specType, uint version, ReadOnlyMemory<byte> data)
        {
            var dbSpec = new CommonDBSpec
            {
                SpecType = specType,
                Version = version,
                Data = data.ToArray(),
            };
            _commonDB.InsertOrReplace(dbSpec);
            AddOrReplaceCacheVersion(specType, version);
        }

        // 스레드 안전하게 Spec 버전 캐시 추가 또는 교체
        private void AddOrReplaceCacheVersion(SpecType key, uint value)
        {
            while (true)
            {
                var snapshot = Volatile.Read(ref _specVersionCache);
                var updated = snapshot.SetItem(key, value);
                if (Interlocked.CompareExchange(ref _specVersionCache, updated, snapshot) == snapshot)
                    break; // 성공적으로 교체
            }
        }
    }
}
