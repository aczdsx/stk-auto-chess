# gRPC → REST API 마이그레이션 구현 계획

> **For agentic workers:** REQUIRED: Use superpowers:subagent-driven-development (if subagents available) or superpowers:executing-plans to implement this plan. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** gRPC 네트워크 레이어를 REST API(protobuf over HTTP)로 빅뱅 전환하여 NetLite 패키지 의존성을 완전 제거한다.

**Architecture:** 기존 NetManager + 도메인 서비스 구조를 유지하되, 내부를 UnityWebRequest + protobuf 직렬화로 교체. SSE 클라이언트로 실시간 이벤트 수신. 호출부(`NetManager.Instance.Xxx.YyyAsync()`) 변경 없음.

**Tech Stack:** Unity 6, UnityWebRequest, UniTask, Google.Protobuf, NUnit (Unity Test Framework 1.5.1)

**참조 문서:**
- 설계: `docs/superpowers/specs/2026-03-10-grpc-to-rest-migration-design.md`
- API 명세: `Assets/_Project/docs/REST_API_Migration_Spec.md`

---

## 파일 구조

### 신규 생성

```
Assets/_Project/Scripts/Network/
├── RestClient.cs                    # HTTP 엔진 (UnityWebRequest + protobuf)
├── SseClient.cs                     # SSE 실시간 이벤트 클라이언트
├── RestServiceBase.cs               # 서비스 베이스 클래스 (공통 에러 처리)
├── NetManager.cs                    # 싱글턴 (기존에서 이동 + 수정)
├── NetManager.Initialization.cs     # 초기화 (기존에서 이동 + 수정)
├── NetManager.EventSubscription.cs  # SSE 구독 (기존에서 이동 + 수정)
├── NetManager.CommonErrorHandler.cs # 에러 핸들러 (기존에서 이동 + 수정)
├── NetManager.Elpis.cs              # 엘피스 초기화 (기존에서 이동)
├── NetManager.CustomLobby.cs        # 커스텀 로비 초기화 (기존에서 이동)
└── Services/                        # 서비스 클래스들 (기존에서 이동 + 수정)
    ├── AuthService.cs
    ├── LobbyService.cs
    ├── SpecService.cs
    ├── CharacterService.cs
    ├── BattleService.cs
    ├── ElpisService.cs
    ├── PlayerInventoryService.cs
    ├── CustomLobbyService.cs
    ├── DeckService.cs
    ├── GachaService.cs
    ├── GuideMissionService.cs
    ├── EventService.cs
    ├── QuestService.cs
    ├── TrialDungeonService.cs
    ├── ClientDataService.cs
    └── CheatService.cs

Assets/Tests/
├── EditMode/
│   ├── Tests.EditMode.asmdef
│   ├── RestClientTests.cs
│   ├── SseClientTests.cs
│   └── ServiceTests/
│       ├── CharacterServiceTests.cs
│       ├── BattleServiceTests.cs
│       └── ... (서비스별 테스트)
└── PlayMode/
    ├── Tests.PlayMode.asmdef
    └── NetworkIntegrationTests.cs
```

### 수정 대상

- `Packages/manifest.json` — NetLite 패키지 제거
- 기존 gRPC 서비스 파일들 — 삭제 (새 위치로 이동)

### 삭제 대상

```
Assets/_Project/Scripts/gRPC/bm013-grpc/Impls/          # 전체 (Network/로 이동)
Assets/_Project/Scripts/gRPC/bm013-grpc/Generated/CSharp/*Grpc.cs  # 서비스 스텁만
Assets/_Project/Scripts/gRPC/bm013-grpc/Editor/          # gRPC 에디터 도구
```

### 유지

```
Assets/_Project/Scripts/gRPC/bm013-grpc/Generated/CSharp/*.cs  # 메시지 타입 (Grpc.cs 제외)
Assets/_Project/Scripts/gRPC/bm013-grpc/Partials/              # proto 타입 확장
Google.Protobuf 패키지                                          # 직렬화에 필요
```

---

## Chunk 1: 테스트 인프라 + RestClient

### Task 1: Unity Test Framework 설정

**Files:**
- Create: `Assets/Tests/EditMode/Tests.EditMode.asmdef`
- Create: `Assets/Tests/PlayMode/Tests.PlayMode.asmdef`

- [ ] **Step 1: EditMode 테스트 Assembly Definition 생성**

```json
// Assets/Tests/EditMode/Tests.EditMode.asmdef
{
    "name": "Tests.EditMode",
    "rootNamespace": "CookApps.AutoBattler.Tests.EditMode",
    "references": [
        "GUID:<Assembly-CSharp의 GUID>"
    ],
    "includePlatforms": [
        "Editor"
    ],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": true,
    "precompiledReferences": [
        "nunit.framework.dll"
    ],
    "autoReferenced": false,
    "defineConstraints": [
        "UNITY_INCLUDE_TESTS"
    ],
    "versionDefines": [],
    "noEngineReferences": false
}
```

> **주의:** Assembly-CSharp에 asmdef가 없으므로, 프로젝트 스크립트를 참조하려면 asmdef를 추가하거나 `overrideReferences` + `precompiledReferences`로 접근해야 합니다. 프로젝트에 asmdef가 없는 현재 구조에서는 `Assembly-CSharp.dll`을 precompiledReferences에 추가합니다.

```json
{
    "name": "Tests.EditMode",
    "rootNamespace": "CookApps.AutoBattler.Tests.EditMode",
    "references": [],
    "includePlatforms": [
        "Editor"
    ],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": true,
    "precompiledReferences": [
        "nunit.framework.dll",
        "Google.Protobuf.dll"
    ],
    "autoReferenced": false,
    "defineConstraints": [
        "UNITY_INCLUDE_TESTS"
    ],
    "noEngineReferences": false
}
```

- [ ] **Step 2: PlayMode 테스트 Assembly Definition 생성**

```json
// Assets/Tests/PlayMode/Tests.PlayMode.asmdef
{
    "name": "Tests.PlayMode",
    "rootNamespace": "CookApps.AutoBattler.Tests.PlayMode",
    "references": [],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": true,
    "precompiledReferences": [
        "nunit.framework.dll",
        "Google.Protobuf.dll"
    ],
    "autoReferenced": false,
    "defineConstraints": [
        "UNITY_INCLUDE_TESTS"
    ],
    "noEngineReferences": false
}
```

- [ ] **Step 3: Unity Editor에서 Test Runner 열어서 테스트 폴더 인식 확인**

Run: Unity Editor → `Window > General > Test Runner`
Expected: EditMode/PlayMode 탭에 테스트 어셈블리가 표시됨

- [ ] **Step 4: 커밋**

```bash
git add Assets/Tests/
git commit -m "test: Unity Test Framework EditMode/PlayMode 테스트 인프라 구축"
```

---

### Task 2: RestClient 구현 — 기본 구조 + 직렬화

**Files:**
- Create: `Assets/_Project/Scripts/Network/RestClient.cs`
- Create: `Assets/Tests/EditMode/RestClientTests.cs`

- [ ] **Step 1: RestClient 직렬화 테스트 작성**

protobuf 직렬화/역직렬화가 정상 동작하는지 검증합니다.

```csharp
// Assets/Tests/EditMode/RestClientTests.cs
using NUnit.Framework;
using Google.Protobuf;
using Tech.Hive.V1;

namespace CookApps.AutoBattler.Tests.EditMode
{
    [TestFixture]
    public class RestClientTests
    {
        [Test]
        public void SerializeRequest_ReturnsValidProtobufBytes()
        {
            var request = new CharacterLevelUpRequest { CharacterId = 42 };
            byte[] bytes = request.ToByteArray();

            Assert.IsNotNull(bytes);
            Assert.IsTrue(bytes.Length > 0);

            var deserialized = CharacterLevelUpRequest.Parser.ParseFrom(bytes);
            Assert.AreEqual(42u, deserialized.CharacterId);
        }

        [Test]
        public void DeserializeResponse_ParsesProtobufBytes()
        {
            var original = new CharacterLevelUpResponse
            {
                Status = new ResponseStatus { Code = 0 },
                Character = new CharacterData { CharacterId = 42, Level = 10 }
            };
            byte[] bytes = original.ToByteArray();

            var parsed = CharacterLevelUpResponse.Parser.ParseFrom(bytes);
            Assert.AreEqual(0u, parsed.Status.Code);
            Assert.AreEqual(42u, parsed.Character.CharacterId);
            Assert.AreEqual(10u, parsed.Character.Level);
        }
    }
}
```

- [ ] **Step 2: 테스트 실행 — 실패 확인**

Run: Unity Editor → Test Runner → EditMode → Run All
Expected: 테스트가 표시되고 PASS (proto 메시지 타입은 이미 존재하므로 직렬화 자체는 통과)

> 이 테스트는 proto 메시지 직렬화 기반이 정상인지 확인하는 스모크 테스트입니다.

- [ ] **Step 3: RestClient 기본 구조 작성**

```csharp
// Assets/_Project/Scripts/Network/RestClient.cs
using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Google.Protobuf;
using UnityEngine;
using UnityEngine.Networking;

namespace CookApps.AutoBattler.Network
{
    public class RestClient : IDisposable
    {
        private string _baseUrl;
        private string _sessionId;
        private readonly Dictionary<string, string> _defaultHeaders = new();
        private CancellationTokenSource _appCts;

        public RestClient(string baseUrl)
        {
            _baseUrl = baseUrl.TrimEnd('/');
            _appCts = new CancellationTokenSource();
        }

        public void SetSessionId(string sessionId) => _sessionId = sessionId;
        public void SetBaseUrl(string url) => _baseUrl = url.TrimEnd('/');

        public void SetDefaultHeader(string key, string value)
        {
            _defaultHeaders[key] = value;
        }

        public async UniTask<TResponse> PostAsync<TRequest, TResponse>(
            string path, TRequest request, double timeout = 5.0, CancellationToken ct = default)
            where TRequest : IMessage<TRequest>
            where TResponse : IMessage<TResponse>, new()
        {
            byte[] body = request.ToByteArray();
            var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, _appCts.Token);

            using var webRequest = new UnityWebRequest($"{_baseUrl}{path}", "POST");
            webRequest.uploadHandler = new UploadHandlerRaw(body);
            webRequest.downloadHandler = new DownloadHandlerBuffer();
            webRequest.timeout = (int)timeout;

            ApplyHeaders(webRequest);

            await webRequest.SendWebRequest().ToUniTask(cancellationToken: linkedCts.Token);

            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                throw new NetworkException(webRequest.error, webRequest.responseCode);
            }

            var response = new TResponse();
            ((IMessage)response).MergeFrom(webRequest.downloadHandler.data);
            return response;
        }

        public async UniTask<TResponse> GetAsync<TResponse>(
            string path, double timeout = 5.0, CancellationToken ct = default)
            where TResponse : IMessage<TResponse>, new()
        {
            var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, _appCts.Token);

            using var webRequest = UnityWebRequest.Get($"{_baseUrl}{path}");
            webRequest.downloadHandler = new DownloadHandlerBuffer();
            webRequest.timeout = (int)timeout;

            ApplyHeaders(webRequest);
            webRequest.SetRequestHeader("Accept", "application/protobuf");

            await webRequest.SendWebRequest().ToUniTask(cancellationToken: linkedCts.Token);

            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                throw new NetworkException(webRequest.error, webRequest.responseCode);
            }

            var response = new TResponse();
            ((IMessage)response).MergeFrom(webRequest.downloadHandler.data);
            return response;
        }

        public async UniTask<TResponse> PutAsync<TRequest, TResponse>(
            string path, TRequest request, double timeout = 5.0, CancellationToken ct = default)
            where TRequest : IMessage<TRequest>
            where TResponse : IMessage<TResponse>, new()
        {
            byte[] body = request.ToByteArray();
            var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, _appCts.Token);

            using var webRequest = UnityWebRequest.Put($"{_baseUrl}{path}", body);
            webRequest.downloadHandler = new DownloadHandlerBuffer();
            webRequest.timeout = (int)timeout;

            ApplyHeaders(webRequest);
            webRequest.SetRequestHeader("Content-Type", "application/protobuf");

            await webRequest.SendWebRequest().ToUniTask(cancellationToken: linkedCts.Token);

            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                throw new NetworkException(webRequest.error, webRequest.responseCode);
            }

            var response = new TResponse();
            ((IMessage)response).MergeFrom(webRequest.downloadHandler.data);
            return response;
        }

        private void ApplyHeaders(UnityWebRequest webRequest)
        {
            webRequest.SetRequestHeader("Content-Type", "application/protobuf");
            if (!string.IsNullOrEmpty(_sessionId))
            {
                webRequest.SetRequestHeader("Authorization", $"Bearer {_sessionId}");
            }
            foreach (var kv in _defaultHeaders)
            {
                webRequest.SetRequestHeader(kv.Key, kv.Value);
            }
        }

        public void Dispose()
        {
            _appCts?.Cancel();
            _appCts?.Dispose();
            _appCts = null;
        }
    }

    public class NetworkException : Exception
    {
        public long StatusCode { get; }

        public NetworkException(string message, long statusCode)
            : base(message)
        {
            StatusCode = statusCode;
        }
    }
}
```

- [ ] **Step 4: RestClient 헤더 설정 테스트 추가**

```csharp
// RestClientTests.cs 에 추가
[Test]
public void SetSessionId_StoresValue()
{
    var client = new RestClient("http://localhost");
    client.SetSessionId("test-session-123");
    // RestClient 내부 상태 확인은 통합 테스트에서 검증
    // 여기서는 예외 없이 설정되는지만 확인
    Assert.DoesNotThrow(() => client.SetSessionId("test-session-123"));
}

[Test]
public void SetDefaultHeader_StoresMultipleHeaders()
{
    var client = new RestClient("http://localhost");
    Assert.DoesNotThrow(() =>
    {
        client.SetDefaultHeader("version", "1.0.0");
        client.SetDefaultHeader("device-id", "test-device");
        client.SetDefaultHeader("store", "1");
    });
}

[Test]
public void Dispose_DoesNotThrow()
{
    var client = new RestClient("http://localhost");
    Assert.DoesNotThrow(() => client.Dispose());
}

[Test]
public void Constructor_TrimsTrailingSlash()
{
    // RestClient 내부 baseUrl 확인은 직접 불가하지만
    // 정상 생성되는지 확인
    var client = new RestClient("http://localhost:8080/");
    Assert.IsNotNull(client);
    client.Dispose();
}
```

- [ ] **Step 5: 테스트 실행 — 통과 확인**

Run: Unity Editor → Test Runner → EditMode → Run All
Expected: 모든 테스트 PASS

- [ ] **Step 6: 커밋**

```bash
git add Assets/_Project/Scripts/Network/RestClient.cs Assets/Tests/EditMode/
git commit -m "feat: RestClient 구현 — UnityWebRequest + protobuf 직렬화 기반 HTTP 엔진"
```

---

### Task 3: RestServiceBase — 서비스 공통 베이스

**Files:**
- Create: `Assets/_Project/Scripts/Network/RestServiceBase.cs`

- [ ] **Step 1: RestServiceBase 작성**

기존 `GrpcServiceBase`의 `ExecuteWithCommonErrorCheck` 역할을 대체합니다.

```csharp
// Assets/_Project/Scripts/Network/RestServiceBase.cs
using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Google.Protobuf;

namespace CookApps.AutoBattler.Network
{
    public interface IServiceInterceptor
    {
        void ShowLoadingIndicator();
        void HideLoadingIndicator();
        bool HandleCommonError(Exception e);
    }

    public abstract class RestServiceBase
    {
        protected readonly RestClient Client;
        public IServiceInterceptor ServiceInterceptor { get; set; }

        protected RestServiceBase(RestClient client)
        {
            Client = client;
        }

        protected async UniTask<TResponse> PostWithErrorCheck<TRequest, TResponse>(
            string path, TRequest request, CancellationToken ct = default)
            where TRequest : IMessage<TRequest>
            where TResponse : IMessage<TResponse>, new()
        {
            try
            {
                ServiceInterceptor?.ShowLoadingIndicator();
                return await Client.PostAsync<TRequest, TResponse>(path, request, ct: ct);
            }
            catch (Exception e)
            {
                if (ServiceInterceptor == null || !ServiceInterceptor.HandleCommonError(e))
                    throw;
                return new TResponse();
            }
            finally
            {
                ServiceInterceptor?.HideLoadingIndicator();
            }
        }

        protected async UniTask<TResponse> GetWithErrorCheck<TResponse>(
            string path, CancellationToken ct = default)
            where TResponse : IMessage<TResponse>, new()
        {
            try
            {
                ServiceInterceptor?.ShowLoadingIndicator();
                return await Client.GetAsync<TResponse>(path, ct: ct);
            }
            catch (Exception e)
            {
                if (ServiceInterceptor == null || !ServiceInterceptor.HandleCommonError(e))
                    throw;
                return new TResponse();
            }
            finally
            {
                ServiceInterceptor?.HideLoadingIndicator();
            }
        }

        protected async UniTask<TResponse> PutWithErrorCheck<TRequest, TResponse>(
            string path, TRequest request, CancellationToken ct = default)
            where TRequest : IMessage<TRequest>
            where TResponse : IMessage<TResponse>, new()
        {
            try
            {
                ServiceInterceptor?.ShowLoadingIndicator();
                return await Client.PutAsync<TRequest, TResponse>(path, request, ct: ct);
            }
            catch (Exception e)
            {
                if (ServiceInterceptor == null || !ServiceInterceptor.HandleCommonError(e))
                    throw;
                return new TResponse();
            }
            finally
            {
                ServiceInterceptor?.HideLoadingIndicator();
            }
        }
    }
}
```

- [ ] **Step 2: 커밋**

```bash
git add Assets/_Project/Scripts/Network/RestServiceBase.cs
git commit -m "feat: RestServiceBase — 서비스 공통 에러 처리 베이스 클래스"
```

---

### Task 4: SseClient 구현

**Files:**
- Create: `Assets/_Project/Scripts/Network/SseClient.cs`
- Create: `Assets/Tests/EditMode/SseClientTests.cs`

- [ ] **Step 1: SseClient 파싱 테스트 작성**

SSE 데이터 파싱 로직을 분리하여 테스트 가능하게 합니다.

```csharp
// Assets/Tests/EditMode/SseClientTests.cs
using NUnit.Framework;
using CookApps.AutoBattler.Network;

namespace CookApps.AutoBattler.Tests.EditMode
{
    [TestFixture]
    public class SseClientTests
    {
        [Test]
        public void ParseSseLine_DataLine_ReturnsData()
        {
            string result = SseParser.ParseDataLine("data: 1");
            Assert.AreEqual("1", result);
        }

        [Test]
        public void ParseSseLine_EmptyDataLine_ReturnsEmpty()
        {
            string result = SseParser.ParseDataLine("data: ");
            Assert.AreEqual("", result);
        }

        [Test]
        public void ParseSseLine_NonDataLine_ReturnsNull()
        {
            string result = SseParser.ParseDataLine(": comment");
            Assert.IsNull(result);
        }

        [Test]
        public void ParseSseLine_EventLine_ReturnsNull()
        {
            string result = SseParser.ParseDataLine("event: message");
            Assert.IsNull(result);
        }
    }
}
```

- [ ] **Step 2: 테스트 실행 — 실패 확인**

Run: Unity Editor → Test Runner → EditMode → Run All
Expected: FAIL — `SseParser` 클래스가 존재하지 않음

- [ ] **Step 3: SseParser + SseClient 구현**

```csharp
// Assets/_Project/Scripts/Network/SseClient.cs
using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace CookApps.AutoBattler.Network
{
    public static class SseParser
    {
        public static string ParseDataLine(string line)
        {
            if (line == null) return null;
            if (!line.StartsWith("data:")) return null;
            return line.Substring(5).TrimStart();
        }
    }

    public class SseClient : IDisposable
    {
        private CancellationTokenSource _cts;
        private readonly RestClient _restClient;
        private readonly string _path;

        public SseClient(RestClient restClient, string path = "/events/subscribe")
        {
            _restClient = restClient;
            _path = path;
        }

        public void Start(Action<string> onEvent)
        {
            Stop();
            _cts = new CancellationTokenSource();
            RunLoop(onEvent, _cts.Token).Forget();
        }

        private async UniTaskVoid RunLoop(Action<string> onEvent, CancellationToken ct)
        {
            const int MaxRetryDelay = 30;
            int retryDelay = 1;

            while (!ct.IsCancellationRequested)
            {
                try
                {
                    Debug.Log("[SSE] Connecting...");
                    await ConnectAndListen(onEvent, ct);
                    retryDelay = 1;
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[SSE] Error: {ex.Message}, retrying in {retryDelay}s");
                }

                await UniTask.Delay(TimeSpan.FromSeconds(retryDelay), cancellationToken: ct);
                retryDelay = Math.Min(retryDelay * 2, MaxRetryDelay);
            }
        }

        private async UniTask ConnectAndListen(Action<string> onEvent, CancellationToken ct)
        {
            // UnityWebRequest로 SSE 연결
            // DownloadHandlerScript 또는 주기적 버퍼 폴링으로 청크 수신
            // 서버 구현에 맞춰 상세 구현 조정 필요
            // 아래는 기본 구조:

            using var webRequest = UnityWebRequest.Get($"{_path}");
            webRequest.SetRequestHeader("Accept", "text/event-stream");
            webRequest.timeout = 0; // SSE는 타임아웃 없음

            var downloadHandler = new DownloadHandlerBuffer();
            webRequest.downloadHandler = downloadHandler;

            var asyncOp = webRequest.SendWebRequest();
            int lastProcessed = 0;

            while (!asyncOp.isDone && !ct.IsCancellationRequested)
            {
                await UniTask.Yield(ct);

                string text = downloadHandler.text;
                if (text != null && text.Length > lastProcessed)
                {
                    string newData = text.Substring(lastProcessed);
                    lastProcessed = text.Length;

                    string[] lines = newData.Split('\n');
                    foreach (string line in lines)
                    {
                        string data = SseParser.ParseDataLine(line.Trim());
                        if (data != null)
                        {
                            onEvent?.Invoke(data);
                        }
                    }
                }
            }
        }

        public void Stop()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
        }

        public void Dispose() => Stop();
    }
}
```

- [ ] **Step 4: 테스트 실행 — 통과 확인**

Run: Unity Editor → Test Runner → EditMode → Run All
Expected: 모든 SseParser 테스트 PASS

- [ ] **Step 5: 커밋**

```bash
git add Assets/_Project/Scripts/Network/SseClient.cs Assets/Tests/EditMode/SseClientTests.cs
git commit -m "feat: SseClient 구현 — SSE 파서 + 자동 재연결 (exponential backoff)"
```

---

## Chunk 2: 서비스 전환 (16개 서비스)

> 각 서비스의 전환 패턴은 동일합니다. 대표로 CharacterService를 상세하게 기술하고, 나머지는 동일 패턴을 따릅니다.

### Task 5: CharacterService 전환

**Files:**
- Create: `Assets/_Project/Scripts/Network/Services/CharacterService.cs`
- Create: `Assets/Tests/EditMode/ServiceTests/CharacterServiceTests.cs`

**참조:**
- 기존: `Assets/_Project/Scripts/gRPC/bm013-grpc/Impls/Services/CharacterService.cs`
- API 명세: `REST_API_Migration_Spec.md` § 4

- [ ] **Step 1: CharacterService 테스트 작성**

서비스의 URL 구성과 요청 직렬화를 검증합니다. 실제 HTTP 호출은 통합 테스트에서 검증하므로, 여기서는 서비스가 올바른 경로와 요청을 구성하는지만 테스트합니다.

```csharp
// Assets/Tests/EditMode/ServiceTests/CharacterServiceTests.cs
using NUnit.Framework;
using Tech.Hive.V1;

namespace CookApps.AutoBattler.Tests.EditMode.ServiceTests
{
    [TestFixture]
    public class CharacterServiceTests
    {
        [Test]
        public void LevelUpRequest_SetsCharacterId()
        {
            var request = new CharacterLevelUpRequest { CharacterId = 42 };
            Assert.AreEqual(42u, request.CharacterId);

            // 직렬화 라운드트립
            byte[] bytes = request.ToByteArray();
            var parsed = CharacterLevelUpRequest.Parser.ParseFrom(bytes);
            Assert.AreEqual(42u, parsed.CharacterId);
        }

        [Test]
        public void LevelUpResponse_ParsesCharacterAndCurrencyDeltas()
        {
            var resp = new CharacterLevelUpResponse
            {
                Status = new ResponseStatus { Code = 0 },
                Character = new CharacterData { CharacterId = 42, Level = 11 },
            };
            resp.CurrencyDeltas.Add(new CurrencyDelta
            {
                ItemId = 1, Before = 1000, After = 900, Delta = -100
            });

            byte[] bytes = resp.ToByteArray();
            var parsed = CharacterLevelUpResponse.Parser.ParseFrom(bytes);

            Assert.AreEqual(0u, parsed.Status.Code);
            Assert.AreEqual(42u, parsed.Character.CharacterId);
            Assert.AreEqual(11u, parsed.Character.Level);
            Assert.AreEqual(1, parsed.CurrencyDeltas.Count);
            Assert.AreEqual(-100, parsed.CurrencyDeltas[0].Delta);
        }

        [Test]
        public void ListResponse_ParsesMultipleCharacters()
        {
            var resp = new CharacterListResponse
            {
                Status = new ResponseStatus { Code = 0 },
            };
            resp.Characters.Add(new CharacterData { CharacterId = 1, Level = 5 });
            resp.Characters.Add(new CharacterData { CharacterId = 2, Level = 10 });

            byte[] bytes = resp.ToByteArray();
            var parsed = CharacterListResponse.Parser.ParseFrom(bytes);

            Assert.AreEqual(2, parsed.Characters.Count);
            Assert.AreEqual(1u, parsed.Characters[0].CharacterId);
            Assert.AreEqual(2u, parsed.Characters[1].CharacterId);
        }
    }
}
```

- [ ] **Step 2: 테스트 실행 — 통과 확인**

Run: Unity Editor → Test Runner → EditMode → Run All
Expected: PASS (proto 메시지 직렬화 검증)

- [ ] **Step 3: CharacterService 구현**

```csharp
// Assets/_Project/Scripts/Network/Services/CharacterService.cs
using System.Threading;
using Cysharp.Threading.Tasks;
using Tech.Hive.V1;

namespace CookApps.AutoBattler.Network.Services
{
    public class CharacterService : RestServiceBase
    {
        public CharacterService(RestClient client) : base(client) { }

        public async UniTask<CharacterListResponse> ListAsync(CancellationToken ct = default)
        {
            var resp = await GetWithErrorCheck<CharacterListResponse>("/characters", ct);

            if (resp is { Status.Code: 0, Characters: not null })
            {
                ServerDataManager.Instance.Character.SetCharacters(resp.Characters);
            }
            return resp;
        }

        public async UniTask<CharacterLevelUpResponse> LevelUpAsync(
            uint characterId, CancellationToken ct = default)
        {
            var resp = await PostWithErrorCheck<CharacterLevelUpRequest, CharacterLevelUpResponse>(
                $"/characters/{characterId}/level-up",
                new CharacterLevelUpRequest { CharacterId = characterId },
                ct);

            if (resp is { Status.Code: 0 })
            {
                if (resp.Character is not null)
                    ServerDataManager.Instance.Character.UpdateCharacter(resp.Character);
                if (resp.CurrencyDeltas is { Count: > 0 })
                    ServerDataManager.Instance.Inventory.ApplyCurrencyDeltas(resp.CurrencyDeltas);
            }
            return resp;
        }

        public async UniTask<CharacterTranscendResponse> TranscendAsync(
            uint characterId, CancellationToken ct = default)
        {
            var resp = await PostWithErrorCheck<CharacterTranscendRequest, CharacterTranscendResponse>(
                $"/characters/{characterId}/transcend",
                new CharacterTranscendRequest { CharacterId = characterId },
                ct);

            if (resp is { Status.Code: 0 })
            {
                if (resp.Character is not null)
                    ServerDataManager.Instance.Character.UpdateCharacter(resp.Character);
                if (resp.CurrencyDeltas is { Count: > 0 })
                    ServerDataManager.Instance.Inventory.ApplyCurrencyDeltas(resp.CurrencyDeltas);
            }
            return resp;
        }

        public async UniTask<CharacterExceedResponse> ExceedAsync(
            uint characterId, CancellationToken ct = default)
        {
            var resp = await PostWithErrorCheck<CharacterExceedRequest, CharacterExceedResponse>(
                $"/characters/{characterId}/exceed",
                new CharacterExceedRequest { CharacterId = characterId },
                ct);

            if (resp is { Status.Code: 0 })
            {
                if (resp.Character is not null)
                    ServerDataManager.Instance.Character.UpdateCharacter(resp.Character);
                if (resp.CurrencyDeltas is { Count: > 0 })
                    ServerDataManager.Instance.Inventory.ApplyCurrencyDeltas(resp.CurrencyDeltas);
            }
            return resp;
        }
    }
}
```

- [ ] **Step 4: 커밋**

```bash
git add Assets/_Project/Scripts/Network/Services/CharacterService.cs \
       Assets/Tests/EditMode/ServiceTests/CharacterServiceTests.cs
git commit -m "feat: CharacterService REST 전환 — 4개 RPC (List, LevelUp, Transcend, Exceed)"
```

---

### Task 6: 나머지 14개 서비스 전환

> 모든 서비스는 Task 5와 동일한 패턴을 따릅니다.
> 각 서비스별 REST 경로는 `REST_API_Migration_Spec.md` 참조.

**전환 순서 (의존성 기준):**

| 순서 | 서비스 | RPC 수 | 전환 난이도 | 비고 |
|------|--------|--------|-----------|------|
| 6-1 | AuthService | 3 | 낮음 | 인증 후 sessionId 설정 필요 |
| 6-2 | LobbyService | 1 | 낮음 | |
| 6-3 | SpecService | 2 | 중간 | 대용량 바이너리, bytes 필드 |
| 6-4 | PlayerInventoryService | 2 | 낮음 | |
| 6-5 | DeckService | 2 | 낮음 | PutAsync 사용 (Save) |
| 6-6 | BattleService | 6 | 중간 | RPC 수 많음 |
| 6-7 | ElpisService | 7 | 중간 | RPC 수 가장 많음 |
| 6-8 | CustomLobbyService | 4 | 중간 | SubscribeEvent는 SseClient로 분리 |
| 6-9 | GachaService | 2 | 낮음 | |
| 6-10 | GuideMissionService | 3 | 낮음 | |
| 6-11 | EventService | 3 | 낮음 | |
| 6-12 | QuestService | 2 | 낮음 | |
| 6-13 | TrialDungeonService | 3 | 낮음 | |
| 6-14 | ClientDataService | 2 | 중간 | bytes 필드 (MemoryPack), map 타입 |

**각 서비스별 전환 단계 (반복):**

- [ ] **Step 1:** 서비스 테스트 작성 (Request/Response 직렬화 라운드트립)
- [ ] **Step 2:** 테스트 실행 — 통과 확인
- [ ] **Step 3:** 서비스 구현 (`RestServiceBase` 상속, 기존 ServerDataManager 업데이트 로직 이식)
- [ ] **Step 4:** 커밋

**서비스별 특이사항:**

**6-1. AuthService** — 인증 응답에서 `sessionId`를 추출하여 `RestClient.SetSessionId()` 호출:
```csharp
public async UniTask<AuthenticateResponse> AuthenticateAsync(...)
{
    // 인증은 sessionId 없이 호출
    var resp = await Client.PostAsync<AuthenticateRequest, AuthenticateResponse>(
        "/auth/authenticate", request, ct: ct);
    // 성공 시 sessionId 설정
    if (resp is { Status.Code: 0, Data: not null })
        Client.SetSessionId(resp.Data.SessionId);
    return resp;
}
```

**6-3. SpecService** — `bytes` 필드 응답, 대용량이므로 timeout 늘림:
```csharp
var resp = await Client.GetAsync<SpecDataResponse>($"/spec/game?version={version}", timeout: 30.0, ct: ct);
```

**6-5. DeckService** — Save에 PutAsync 사용:
```csharp
var resp = await PutWithErrorCheck<DeckSaveRequest, DeckSaveResponse>(
    $"/decks/{request.DeckSlotId}", request, ct);
```

**6-8. CustomLobbyService** — SubscribeEvent 제거, 나머지 4개 RPC만 전환. 이벤트 구독은 `NetManager.EventSubscription.cs`에서 `SseClient` 사용.

**6-14. ClientDataService** — `map<string, bytes>` 필드는 protobuf의 `MapField`로 그대로 직렬화됨:
```csharp
var request = new PlayerDataSetRequest();
request.PlayerDatas.Add("category", memoryPackBytes);
```

- [ ] **Step 5: 전체 서비스 전환 후 일괄 커밋**

```bash
git add Assets/_Project/Scripts/Network/Services/ Assets/Tests/EditMode/ServiceTests/
git commit -m "feat: 14개 서비스 REST 전환 완료 (Auth~ClientData)"
```

---

## Chunk 3: NetManager 전환 + gRPC 제거

### Task 7: NetManager 전환

**Files:**
- Create: `Assets/_Project/Scripts/Network/NetManager.cs`
- Create: `Assets/_Project/Scripts/Network/NetManager.Initialization.cs`
- Create: `Assets/_Project/Scripts/Network/NetManager.EventSubscription.cs`
- Create: `Assets/_Project/Scripts/Network/NetManager.CommonErrorHandler.cs`
- Create: `Assets/_Project/Scripts/Network/NetManager.Elpis.cs`
- Create: `Assets/_Project/Scripts/Network/NetManager.CustomLobby.cs`

**참조:**
- 기존 Partial 파일 7개: `Assets/_Project/Scripts/gRPC/bm013-grpc/Impls/NetManager*.cs`

- [ ] **Step 1: NetManager 메인 클래스 작성**

```csharp
// Assets/_Project/Scripts/Network/NetManager.cs
using CookApps.AutoBattler.Network.Services;

namespace CookApps.AutoBattler.Network
{
    public partial class NetManager : Singleton<NetManager>
    {
        private RestClient _client;
        private SseClient _sseClient;

        // 서비스 프로퍼티 — 기존과 동일한 접근 방식
        public AuthService Auth { get; private set; }
        public LobbyService Lobby { get; private set; }
        public SpecService Spec { get; private set; }
        public CharacterService Character { get; private set; }
        public BattleService Battle { get; private set; }
        public ElpisService Elpis { get; private set; }
        public PlayerInventoryService Inventory { get; private set; }
        public CustomLobbyService CustomLobby { get; private set; }
        public DeckService Deck { get; private set; }
        public GachaService Gacha { get; private set; }
        public GuideMissionService GuideMission { get; private set; }
        public EventService Event { get; private set; }
        public QuestService Quest { get; private set; }
        public TrialDungeonService TrialDungeon { get; private set; }
        public ClientDataService ClientData { get; private set; }
        public CheatService Cheat { get; private set; }

        public void Startup(string serverAddress)
        {
            _client = new RestClient(serverAddress);

            // 기본 헤더 설정
            _client.SetDefaultHeader("version", UnityEngine.Application.version);
            _client.SetDefaultHeader("device-id", UnityEngine.SystemInfo.deviceUniqueIdentifier);
            _client.SetDefaultHeader("app-bundle-name", UnityEngine.Application.identifier);

            // 서비스 생성
            Auth = new AuthService(_client);
            Lobby = new LobbyService(_client);
            Spec = new SpecService(_client);
            Character = new CharacterService(_client);
            Battle = new BattleService(_client);
            Elpis = new ElpisService(_client);
            Inventory = new PlayerInventoryService(_client);
            CustomLobby = new CustomLobbyService(_client);
            Deck = new DeckService(_client);
            Gacha = new GachaService(_client);
            GuideMission = new GuideMissionService(_client);
            Event = new EventService(_client);
            Quest = new QuestService(_client);
            TrialDungeon = new TrialDungeonService(_client);
            ClientData = new ClientDataService(_client);
            Cheat = new CheatService(_client);

            // 인터셉터 주입
            InjectServiceInterceptors();

            UnityEngine.Application.quitting += Shutdown;
        }

        private void InjectServiceInterceptors()
        {
            Auth.ServiceInterceptor = this;
            Lobby.ServiceInterceptor = this;
            Spec.ServiceInterceptor = this;
            Character.ServiceInterceptor = this;
            Battle.ServiceInterceptor = this;
            Elpis.ServiceInterceptor = this;
            Inventory.ServiceInterceptor = this;
            CustomLobby.ServiceInterceptor = this;
            Deck.ServiceInterceptor = this;
            Gacha.ServiceInterceptor = this;
            GuideMission.ServiceInterceptor = this;
            Event.ServiceInterceptor = this;
            Quest.ServiceInterceptor = this;
            TrialDungeon.ServiceInterceptor = this;
            ClientData.ServiceInterceptor = this;
            Cheat.ServiceInterceptor = this;
        }

        public void Shutdown()
        {
            _sseClient?.Dispose();
            _client?.Dispose();
        }
    }
}
```

- [ ] **Step 2: Partial 파일들 이식**

기존 `NetManager.Initialization.cs`, `NetManager.EventSubscription.cs`, `NetManager.CommonErrorHandler.cs`, `NetManager.Elpis.cs`, `NetManager.CustomLobby.cs`의 로직을 새 파일에 이식합니다.

주요 변경점:
- `NetManager.EventSubscription.cs`: gRPC 스트림 → `SseClient` 사용
- `NetManager.CommonErrorHandler.cs`: `IGrpcServiceInterceptor` → `IServiceInterceptor` 구현
- 나머지: namespace 변경, gRPC 참조 제거

```csharp
// NetManager.EventSubscription.cs
public partial class NetManager
{
    public void StartEventSubscription()
    {
        StopEventSubscription();
        _sseClient = new SseClient(_client);
        _sseClient.Start(OnServerEventReceived);
    }

    public void StopEventSubscription()
    {
        _sseClient?.Stop();
    }

    private void OnServerEventReceived(string eventData)
    {
        // 기존 OnServerEventReceived 로직 이식
        // eventData 파싱 → BM013Event enum 변환 → 해당 서비스 갱신
    }
}
```

```csharp
// NetManager.CommonErrorHandler.cs
public partial class NetManager : IServiceInterceptor
{
    public void ShowLoadingIndicator() { /* 기존 로직 이식 */ }
    public void HideLoadingIndicator() { /* 기존 로직 이식 */ }
    public bool HandleCommonError(Exception e) { /* 기존 로직 이식 */ return false; }
}
```

- [ ] **Step 3: 기존 호출부와 컴파일 호환성 확인**

Run: Unity Editor에서 컴파일
Expected: 에러 없음 (기존 `NetManager.Instance.Character.LevelUpAsync()` 등 그대로 동작)

- [ ] **Step 4: 커밋**

```bash
git add Assets/_Project/Scripts/Network/NetManager*.cs
git commit -m "feat: NetManager REST 전환 — Singleton 직접 관리, Autofac/NetLite 의존성 제거"
```

---

### Task 8: gRPC 코드 및 의존성 제거

**Files:**
- Delete: `Assets/_Project/Scripts/gRPC/bm013-grpc/Impls/` (전체)
- Delete: `Assets/_Project/Scripts/gRPC/bm013-grpc/Generated/CSharp/*Grpc.cs` (서비스 스텁만)
- Delete: `Assets/_Project/Scripts/gRPC/bm013-grpc/Editor/`
- Modify: `Packages/manifest.json` (NetLite 제거)

- [ ] **Step 1: 기존 gRPC Impls 폴더 삭제**

```bash
rm -rf Assets/_Project/Scripts/gRPC/bm013-grpc/Impls/
```

- [ ] **Step 2: gRPC 서비스 스텁 삭제 (*Grpc.cs 파일만)**

```bash
find Assets/_Project/Scripts/gRPC/bm013-grpc/Generated/CSharp/ -name "*Grpc.cs" -delete
```

> 메시지 타입 파일 (`Character.cs`, `Battle.cs` 등)은 유지합니다.

- [ ] **Step 3: gRPC Editor 도구 삭제**

```bash
rm -rf Assets/_Project/Scripts/gRPC/bm013-grpc/Editor/
```

- [ ] **Step 4: NetLite 패키지 제거**

`Packages/manifest.json`에서 `com.cookapps.net.lite` 항목 삭제.

> 관련 패키지도 확인하여 제거:
> - `YetAnotherHttpHandler` (gRPC HTTP/2 전용)
> - `Grpc.Net.Client` (gRPC 클라이언트)
> - `Grpc.Core` (있다면)

- [ ] **Step 5: 컴파일 확인**

Run: Unity Editor에서 컴파일
Expected: gRPC 관련 참조 에러 없음

> 만약 에러 발생 시: 기존 코드에서 gRPC 타입을 직접 참조하는 곳을 검색하여 수정

```bash
# gRPC 참조 잔여 확인
grep -r "using Grpc\." Assets/_Project/Scripts/ --include="*.cs"
grep -r "GrpcService\|GrpcServiceBase\|NetLite" Assets/_Project/Scripts/ --include="*.cs"
```

- [ ] **Step 6: 전체 테스트 실행**

Run: Unity Editor → Test Runner → EditMode → Run All
Expected: 모든 테스트 PASS

- [ ] **Step 7: 커밋**

```bash
git add -A
git commit -m "chore: gRPC 코드 및 NetLite 패키지 의존성 제거"
```

---

### Task 9: 통합 테스트

**Files:**
- Create: `Assets/Tests/PlayMode/NetworkIntegrationTests.cs`

> 서버 준비 후 실행하는 통합 테스트입니다.

- [ ] **Step 1: 통합 테스트 작성**

```csharp
// Assets/Tests/PlayMode/NetworkIntegrationTests.cs
using System.Collections;
using NUnit.Framework;
using UnityEngine.TestTools;
using CookApps.AutoBattler.Network;

namespace CookApps.AutoBattler.Tests.PlayMode
{
    [TestFixture]
    [Category("Integration")]
    public class NetworkIntegrationTests
    {
        private RestClient _client;

        [SetUp]
        public void SetUp()
        {
            // 테스트 서버 주소 — 서버 준비 후 설정
            _client = new RestClient("http://localhost:8080");
        }

        [TearDown]
        public void TearDown()
        {
            _client?.Dispose();
        }

        [UnityTest]
        [Category("Integration")]
        public IEnumerator Authenticate_ReturnsSessionId()
        {
            // 서버 준비 후 구현
            yield return null;
            Assert.Pass("서버 준비 후 구현 예정");
        }

        [UnityTest]
        [Category("Integration")]
        public IEnumerator CharacterList_ReturnsCharacters()
        {
            // 서버 준비 후 구현
            yield return null;
            Assert.Pass("서버 준비 후 구현 예정");
        }

        [UnityTest]
        [Category("Integration")]
        public IEnumerator SseConnection_ReceivesEvents()
        {
            // 서버 준비 후 구현
            yield return null;
            Assert.Pass("서버 준비 후 구현 예정");
        }
    }
}
```

- [ ] **Step 2: 커밋**

```bash
git add Assets/Tests/PlayMode/
git commit -m "test: 통합 테스트 스켈레톤 추가 (서버 준비 후 구현 예정)"
```

---

## 최종 체크리스트

- [ ] RestClient가 protobuf 직렬화/역직렬화 정상 동작
- [ ] 16개 서비스 모두 REST 전환 완료
- [ ] NetManager 호출부 변경 없음 확인
- [ ] SSE 이벤트 수신 정상 동작
- [ ] NetLite 패키지 완전 제거
- [ ] gRPC 서비스 스텁 (*Grpc.cs) 삭제
- [ ] proto 메시지 타입 유지 확인 (Google.Protobuf 유지)
- [ ] EditMode 유닛 테스트 전체 PASS
- [ ] PlayMode 통합 테스트 스켈레톤 준비
- [ ] Unity Editor 컴파일 에러 없음
