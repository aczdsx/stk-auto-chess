# gRPC → REST API 마이그레이션 설계

> 클라이언트 네트워크 레이어를 gRPC에서 REST API(protobuf over HTTP)로 빅뱅 전환하기 위한 설계 문서

## 배경 및 목적

- 서버 팀이 gRPC에서 REST로 전환 (서버 주도)
- gRPC 라이브러리(NetLite, YetAnotherHttpHandler 등) 의존성 경감
- 서버 타임라인 미정 — 클라이언트 설계만 먼저 확정

## 설계 원칙

- **호출부 변경 제로**: `NetManager.Instance.Xxx.YyyAsync()` 패턴 그대로 유지
- **기존 구조 미러링**: NetManager + 도메인 서비스 구조 유지, 내부 구현만 교체
- **proto 메시지 재사용**: JSON 전환 없이 `application/protobuf` (또는 `application/octet-stream`)로 protobuf 바이너리 직렬화 유지
- **최소 변경**: 새 추상화 레이어나 인터페이스 추가 없음

## 전환 범위

- 총 46개 RPC + SSE 1개 (16개 서비스)
- 상세 명세: `Assets/_Project/docs/REST_API_Migration_Spec.md`

---

## 1. RestClient — 핵심 HTTP 엔진

`GrpcServiceBase` + `GrpcDirectCallExecutor` + `GrpcHeaderProvider`를 대체하는 단일 클래스.

```csharp
public class RestClient : IDisposable
{
    private string _baseUrl;
    private string _sessionId;
    private readonly Dictionary<string, string> _defaultHeaders;
    private CancellationTokenSource _appCts;

    public async UniTask<TResponse> PostAsync<TRequest, TResponse>(
        string path, TRequest request, double timeout = 5.0, CancellationToken ct = default)
        where TRequest : IMessage<TRequest>
        where TResponse : IMessage<TResponse>, new()
    {
        byte[] body = request.ToByteArray();
        using var webRequest = new UnityWebRequest($"{_baseUrl}{path}", "POST");
        webRequest.uploadHandler = new UploadHandlerRaw(body);
        webRequest.downloadHandler = new DownloadHandlerBuffer();
        webRequest.SetRequestHeader("Content-Type", "application/protobuf");
        webRequest.SetRequestHeader("Authorization", $"Bearer {_sessionId}");
        // _defaultHeaders 적용
        webRequest.timeout = (int)timeout;

        await webRequest.SendWebRequest().ToUniTask(cancellationToken: ct);

        TResponse resp = new TResponse();
        resp.MergeFrom(webRequest.downloadHandler.data);
        return resp;
    }

    public async UniTask<TResponse> GetAsync<TResponse>(
        string path, double timeout = 5.0, CancellationToken ct = default)
        where TResponse : IMessage<TResponse>, new()
    { /* body 없는 GET 요청 */ }

    public void SetSessionId(string sessionId) => _sessionId = sessionId;
    public void SetBaseUrl(string url) => _baseUrl = url;
    public void Dispose() { _appCts?.Cancel(); _appCts?.Dispose(); }
}
```

### 기존 대비 매핑

| 기존 (gRPC) | 신규 (REST) |
|---|---|
| `GrpcChannelProvider` (채널/CallInvoker) | `RestClient._baseUrl` + `UnityWebRequest` |
| `GrpcHeaderProvider` (메타데이터) | `RestClient._defaultHeaders` + `Authorization` 헤더 |
| `GrpcDirectCallExecutor.ExecuteAsync()` | `RestClient.PostAsync()` / `GetAsync()` |
| `GrpcServiceBase.ExecuteWithCommonErrorCheck()` | 서비스 내부에서 `RestClient` 호출 + 에러 처리 |
| 7개 인터셉터 체인 | `RestClient` 내부에서 헤더/로깅으로 단순화 |

### 에러 처리

- HTTP 상태 코드 체크 (4xx/5xx)
- protobuf 응답 내 `ResponseStatus.code`로 비즈니스 에러 판별
- 기존 `IGrpcServiceInterceptor.HandleCommonError` → `RestClient`에서 콜백으로 처리

---

## 2. 서비스 클래스 전환

호출부 시그니처 동일 유지, 내부만 교체.

**Before (gRPC):**
```csharp
[GrpcService(typeof(Tech.Hive.V1.CharacterService.CharacterServiceClient))]
public partial class CharacterService
{
    public async UniTask<CharacterLevelUpResponse> LevelUpAsync(uint characterId, CancellationToken ct = default)
    {
        return await ExecuteWithCommonErrorCheck(
            ServiceClient.LevelUpAsync,
            new CharacterLevelUpRequest { CharacterId = characterId },
            cancellationToken: ct);
    }
}
```

**After (REST):**
```csharp
public class CharacterService
{
    private readonly RestClient _client;

    public CharacterService(RestClient client) => _client = client;

    public async UniTask<CharacterLevelUpResponse> LevelUpAsync(uint characterId, CancellationToken ct = default)
    {
        return await _client.PostAsync<CharacterLevelUpRequest, CharacterLevelUpResponse>(
            $"/characters/{characterId}/level-up",
            new CharacterLevelUpRequest { CharacterId = characterId },
            ct: ct);
    }
}
```

### 변경 포인트

- `[GrpcService]` 어트리뷰트 제거, `partial` 제거
- `GrpcServiceBase<T>` 상속 제거 → `RestClient`를 생성자 주입
- `ExecuteWithCommonErrorCheck(ServiceClient.Xxx, request)` → `_client.PostAsync/GetAsync(path, request)`
- proto 메시지 타입(`*Request/*Response`)은 그대로 재사용

### HTTP 메서드 매핑 규칙

- 조회 (`List`, `Get`) → `GetAsync`
- 변경 (`LevelUp`, `Start`, `Draw`, `Claim` 등) → `PostAsync`
- 덱 저장 (`Save`) → `PutAsync`

### ServerDataManager 업데이트 로직

기존 응답 처리 코드(`if (resp is { IsSuccess: true })` 블록)는 변경 없이 유지.

---

## 3. NetManager 전환

`NetLiteManagerBase` 상속 + Autofac DI 제거, `RestClient` 직접 관리.

```csharp
public partial class NetManager : Singleton<NetManager>
{
    private RestClient _client;

    public CharacterService Character { get; private set; }
    public BattleService Battle { get; private set; }
    // ... 15개 서비스 속성 그대로 유지

    public void Startup(string serverAddress)
    {
        _client = new RestClient(serverAddress);
        Character = new CharacterService(_client);
        Battle = new BattleService(_client);
        // ... 모든 서비스에 RestClient 주입
    }

    public void SetSessionId(string sessionId)
    {
        _client.SetSessionId(sessionId);
    }

    public void Shutdown()
    {
        _client?.Dispose();
    }
}
```

### 변경 포인트

| 항목 | 제거 | 대체 |
|---|---|---|
| `NetLiteManagerBase` 상속 | O | `Singleton<NetManager>` |
| Autofac DI 컨테이너 | O | 직접 `new` 생성 |
| `InjectServiceInterceptors()` | O | `RestClient` 내부에서 통합 처리 |
| `VerifyGrpcServiceInjection()` | O | 불필요 (컴파일 타임 보장) |
| `GrpcChannelProvider` | O | `RestClient._baseUrl` |

호출부 변경 없음: `await NetManager.Instance.Character.LevelUpAsync(characterId);`

---

## 4. SSE 클라이언트 (SubscribeEvent 대체)

gRPC 양방향 스트리밍 → SSE 단방향 수신.

```csharp
public class SseClient : IDisposable
{
    private CancellationTokenSource _cts;
    private readonly RestClient _restClient;

    public void Start(Action<BM013Event> onEvent)
    {
        Stop();
        _cts = new CancellationTokenSource();
        RunLoop(onEvent, _cts.Token).Forget();
    }

    private async UniTaskVoid RunLoop(Action<BM013Event> onEvent, CancellationToken ct)
    {
        const int MaxRetryDelay = 30;
        int retryDelay = 1;

        while (!ct.IsCancellationRequested)
        {
            try
            {
                await ConnectAndListen(onEvent, ct);
                retryDelay = 1;
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SSE] Error: {ex.Message}, retrying in {retryDelay}s");
            }

            await UniTask.Delay(TimeSpan.FromSeconds(retryDelay), cancellationToken: ct);
            retryDelay = Math.Min(retryDelay * 2, MaxRetryDelay);
        }
    }

    public void Stop() { _cts?.Cancel(); _cts?.Dispose(); }
    public void Dispose() => Stop();
}
```

### 기존 대비 매핑

| 기존 (gRPC) | 신규 (SSE) |
|---|---|
| `SubscribeEventAsync()` 양방향 스트림 | `GET /events/subscribe` + `text/event-stream` |
| 클라이언트 5초 ping | 불필요 (SSE는 서버 단방향) |
| `OnServerEventReceived(BM013Event)` | 동일 콜백 유지 |
| Exponential backoff (1s~30s) | 동일 로직 유지 |

---

## 5. 제거 및 유지 대상

### 제거

| 대상 | 경로/항목 |
|---|---|
| NetLite 패키지 | `com.cookapps.net.lite` |
| gRPC 생성 클라이언트 | `Generated/CSharp/*Grpc.cs` (서비스 스텁) |
| Proto 원본 | `Generated/Proto/` (참조용 보관 가능) |
| `[GrpcService]` Source Generator | 관련 코드 전부 |
| Autofac DI | NetManager 초기화에서 제거 |
| YetAnotherHttpHandler | gRPC HTTP/2 전용이므로 제거 |
| Grpc.Net.Client | gRPC 클라이언트 라이브러리 제거 |

### 유지

| 대상 | 이유 |
|---|---|
| `Generated/CSharp/*.cs` (메시지 타입) | Request/Response proto 메시지 재사용 |
| `Google.Protobuf` | `ToByteArray()`, `MergeFrom()` 직렬화에 필요 |
| 15개 서비스 클래스 (내부 교체) | 호출부 호환 유지 |
| `ServerDataManager` 업데이트 로직 | 변경 없음 |

### 신규 폴더 구조

```
Assets/_Project/Scripts/Network/
├── RestClient.cs
├── SseClient.cs
├── NetManager.cs
├── NetManager.*.cs        # Partial 파일들 (단순화)
└── Services/
    ├── CharacterService.cs
    ├── BattleService.cs
    └── ...
```
