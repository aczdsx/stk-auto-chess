using System;
using System.Collections.Generic;
using Cookapps.Stkauto.V1;
using Cysharp.Threading.Tasks;
using Google.Protobuf;
using Tech.Hive.V1;

namespace CookApps.gRPC
{
    public interface IResponseStatus
    {
        bool IsError { get; }
    }
    
    public static class GrpcConsts
    {
        public const int ResponseStatusSuccess = 0;
    }
    
    // 간단한 채널/스토어 설정 더미
    public enum ChannelCredentials
    {
        Insecure,
        SecureSSL
    }

    public enum StoreMap
    {
        GooglePlay,
        AppleAppStore
    }

    public class GrpcInitializeParam
    {
        public string Host { get; }
        public int Port { get; }
        public ChannelCredentials Credentials { get; }
        public int VersionCode { get; }
        public StoreMap Store { get; }

        public Action OnSuccess { get; }
        public Action<ResponseStatus, Action<GrpcFailAction>> OnServerError { get; }
        public Action<StatusCode, string, string, Action<GrpcFailAction>> OnGrpcError { get; }
        public bool UseWebSocket { get; }

        public GrpcInitializeParam(string host, int port, ChannelCredentials credentials, int versionCode, StoreMap store,
            Action onSuccess, Action<ResponseStatus, Action<GrpcFailAction>> onServerError,
            Action<StatusCode, string, string, Action<GrpcFailAction>> onGrpcError, bool useWebSocket)
        {
            Host = host;
            Port = port;
            Credentials = credentials;
            VersionCode = versionCode;
            Store = store;
            OnSuccess = onSuccess;
            OnServerError = onServerError;
            OnGrpcError = onGrpcError;
            UseWebSocket = useWebSocket;
        }
    }

    public enum StatusCode
    {
        Unknown = 0
    }

    public enum GrpcFailAction
    {
        Skip,
        Retry,
        CancelAll
    }

    // 기존 gRPC 유틸 확장 더미
    public static class DataCategoryExtensions
    {
        public static string ToCategoryString(this DataCategory category) => category.ToString();
    }
}

namespace Tech.Hive.V1
{
    // 간단한 버전 체크/UUID 응답 더미
    public sealed class CheckVersionData
    {
        public uint SpecVersion { get; set; }
    }

    public sealed class GenerateUuidResponse
    {
        public string Uuid { get; set; } = string.Empty;
        public bool IsError => false;
    }

    public sealed partial class PlayerCreateResponse
    {
        public bool IsError => Status?.Code != 0;
    }

    public sealed partial class GetLastPlayerResponse
    {
        public bool IsError => false; // Status?.Code != 0;
    }
}

namespace CookApps.AutoBattler
{
    using CookApps.gRPC;

    public enum DataLoadingMethod
    {
        Grpc,
        Local
    }

    // gRPC 호출부 더미 매니저
    public class GrpcManager
    {
        private static readonly Lazy<GrpcManager> _instance = new(() => new GrpcManager());
        public static GrpcManager Instance => _instance.Value;

        public AuthService Auth { get; }
        public LobbyService Lobby { get; }
        public PlayerDataService PlayerData { get; }
        public PlayerService Player { get; }
        public SpecService Spec { get; }

        private GrpcManager()
        {
            Auth = new AuthService();
            Lobby = new LobbyService();
            PlayerData = new PlayerDataService();
            Player = new PlayerService();
            Spec = new SpecService();
        }

        public void StartUp(GrpcInitializeParam _)
        {
            // 통신 미사용 스텁
        }

        public void Shutdown()
        {
        }
    }

    public class AuthService
    {
        public AuthenticateData AuthenticateData { get; } = new();

        public bool IsLoggedIn(AuthPlatform _) => false;

        public UniTask<bool> CreateAsync(AuthPlatform _, string __) => UniTask.FromResult(false);

        public UniTask<bool> AuthenticateAsync() => UniTask.FromResult(false);

        public void Logout()
        {
        }
    }

    public class AuthenticateData
    {
        public uint Uid { get; set; } = 0;
    }

    public class LobbyService
    {
        public UniTask<CheckVersionResponse> CheckVersionAsync()
        {
            return UniTask.FromResult(new CheckVersionResponse
            {
                Status = new ResponseStatus { Code = 1, Message = "gRPC disabled" }
            });
        }

        public UniTask<GenerateUuidResponse> GenerateUuidAsync()
        {
            return UniTask.FromResult(new GenerateUuidResponse
            {
                Uuid = Guid.NewGuid().ToString()
            });
        }
    }

    public class PlayerDataService
    {
        public UniTask<PlayerDataListResponse> ListAsync(IEnumerable<string> _)
        {
            var response = new PlayerDataListResponse
            {
                Status = new ResponseStatus { Code = 0 },
                Data = new PlayerDataList
                {
                    ItemList = { }
                }
            };
            return UniTask.FromResult(response);
        }

        public UniTask<PlayerDataSetResponse> SetAsync(string _, IMessage __)
        {
            var response = new PlayerDataSetResponse
            {
                Status = new ResponseStatus { Code = 0 }
            };
            return UniTask.FromResult(response);
        }
    }

    public class PlayerService
    {
        public UniTask<GetLastPlayerResponse> GetLastSelectedAsync()
        {
            var response = new GetLastPlayerResponse
            {
                // Status = new ResponseStatus { Code = 1 }
            };
            return UniTask.FromResult(response);
        }

        public UniTask<PlayerCreateResponse> CreateAsync(uint _, string __)
        {
            var response = new PlayerCreateResponse
            {
                Status = new ResponseStatus { Code = 1 }
            };
            return UniTask.FromResult(response);
        }
    }

    public class SpecService
    {
        public UniTask<string> GetSpecDataAsync(uint _, DataLoadingMethod __ = DataLoadingMethod.Grpc)
        {
            return UniTask.FromResult("{}");
        }
    }

    public static class GrpcExceptionHandler
    {
        public static void HandleSuccess()
        {
        }

        public static void HandleServerException(ResponseStatus _, Action<GrpcFailAction> callback)
        {
            callback?.Invoke(GrpcFailAction.Skip);
        }

        public static void HandleGrpcException(StatusCode _, string __, string ___, Action<GrpcFailAction> callback)
        {
            callback?.Invoke(GrpcFailAction.Skip);
        }
    }
}
