# 새로운 서버 중심 데이터 관리 시스템 (NetLite)

## 📋 개요

기존 클라이언트 Save/Load 방식에서 **서버 중심의 델타 업데이트** 방식으로 전환한 데이터 관리 시스템입니다.
NetLite 프레임워크를 사용하여 gRPC 통신을 처리합니다.

---

## 🏗️ 아키텍처

```
┌─────────────────────────────────────────────────────────────┐
│                      Client Layer                            │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐      │
│  │ UI Component │  │ UI Component │  │ UI Component │      │
│  └──────┬───────┘  └──────┬───────┘  └──────┬───────┘      │
│         │                  │                  │              │
│         └──────────────────┼──────────────────┘              │
│                            │                                 │
├────────────────────────────┼─────────────────────────────────┤
│                   UI Bridge Layer                            │
│  ┌─────────────────────────▼──────────────────────────┐     │
│  │  CharacterDataBridge  │  WalletDataBridge          │     │
│  └─────────────────────────┬──────────────────────────┘     │
│                            │                                 │
├────────────────────────────┼─────────────────────────────────┤
│                   Data Event Bus                             │
│  ┌─────────────────────────▼──────────────────────────┐     │
│  │  Subscribe/Publish                                 │     │
│  └─────────────────────────┬──────────────────────────┘     │
│                            │                                 │
├────────────────────────────┼─────────────────────────────────┤
│                Server Data Manager                           │
│  ┌─────────────────────────▼──────────────────────────┐     │
│  │  CharacterModel  │  WalletModel  │  ...            │     │
│  │  (Delta Update)  │  (Delta Update)                 │     │
│  └─────────────────────────┬──────────────────────────┘     │
│                            │                                 │
├────────────────────────────┼─────────────────────────────────┤
│                  NetLite Services                            │
│  ┌─────────────────────────▼──────────────────────────┐     │
│  │  NetManager.Character  │  NetManager.Battle  ...   │     │
│  └─────────────────────────┬──────────────────────────┘     │
│                            │                                 │
├────────────────────────────┼─────────────────────────────────┤
│                    NetManager                                │
│  ┌─────────────────────────▼──────────────────────────┐     │
│  │  NetLiteManagerBase (gRPC Channel)                 │     │
│  └─────────────────────────┬──────────────────────────┘     │
│                            │                                 │
└────────────────────────────┼─────────────────────────────────┘
                             │
                             ▼
                    ┌─────────────────┐
                    │  Server (gRPC)  │
                    └─────────────────┘
```

---

## 📁 폴더 구조

```
UserData/
├── Core/                          # 핵심 시스템
│   ├── IDataModel.cs              # 데이터 모델 인터페이스
│   ├── DataEventBus.cs            # 이벤트 버스 (Reflection 없음)
│   └── ServerDataManager.cs      # 서버 데이터 관리자
│
├── Models/                        # 데이터 모델
│   ├── CharacterModel.cs          # 캐릭터 데이터
│   └── WalletModel.cs             # 지갑(통화) 데이터
│
├── Bridges/                       # UI 바인딩 브릿지
│   ├── CharacterDataBridge.cs     # 캐릭터 UI 브릿지
│   └── WalletDataBridge.cs        # 지갑 UI 브릿지
│
├── Examples/                      # 사용 예제
│   └── DataSystemUsageExample.cs  # 전체 시스템 사용 예제
│
└── Legacy/                        # 기존 코드 (참고용)
    ├── UserDataManager.cs
    └── Impls/

gRPC/bm013-grpc/Impls/
├── NetManager.cs                  # NetLite 매니저
└── Services/                      # 서비스 구현
    └── CharacterService.cs        # 캐릭터 서비스
```

---

## 🚀 사용 방법

### 1. 시스템 초기화

```csharp
// 데이터 매니저에 팩토리 등록
var dataManager = ServerDataManager.Instance;
dataManager.RegisterFactory(CharacterModel.CATEGORY_KEY, () => new CharacterModel());
dataManager.RegisterFactory(WalletModel.CATEGORY_KEY, () => new WalletModel());

// UI 브릿지 생성
var characterBridge = new CharacterDataBridge();
var walletBridge = new WalletDataBridge();

// NetManager 시작
NetManager.Instance.Startup();
```

### 2. 서버에서 데이터 가져오기

```csharp
// 캐릭터 목록 가져오기
var response = await NetManager.Instance.Character.ListAsync();

if (response.Status.Code == 0)
{
    // 로컬 데이터 갱신
    var characterModel = ServerDataManager.Instance.GetData<CharacterModel>(CharacterModel.CATEGORY_KEY);
    characterModel.SetCharacters(response.Characters, characterModel.Version + 1);
    ServerDataManager.Instance.SetData(CharacterModel.CATEGORY_KEY, characterModel);
}
```

### 3. UI 바인딩

```csharp
// 데이터 변경 이벤트 구독
characterBridge.OnCharactersChanged += () => {
    Debug.Log("캐릭터 목록 변경됨 - UI 갱신");
    RefreshCharacterListUI();
};

characterBridge.OnCharacterUpdated += (character) => {
    Debug.Log($"캐릭터 업데이트: {character.InstanceId}");
    RefreshCharacterUI(character);
};

walletBridge.OnCurrencyChanged += (itemId, newAmount) => {
    Debug.Log($"통화 변경: ItemID {itemId} = {newAmount}");
    RefreshCurrencyUI(itemId, newAmount);
};
```

### 4. 데이터 조회

```csharp
// 모든 캐릭터 가져오기
var characters = new List<CharacterInfo>();
characterBridge.GetAllCharacters(characters);

// 특정 캐릭터 가져오기
var character = characterBridge.GetCharacter(instanceId);

// 조건별 필터링 (for문 사용, Linq 지양)
var highLevelCharacters = new List<CharacterInfo>();
characterBridge.GetCharactersByLevelRange(highLevelCharacters, 10, 99);

// 통화 조회
ulong gold = walletBridge.GetCurrency(1); // ItemID 1 = Gold
bool hasEnough = walletBridge.HasEnoughCurrency(1, 1000);
```

### 5. 서버 액션 (NetManager 사용)

```csharp
// 캐릭터 레벨업
var response = await NetManager.Instance.Character.LevelUpAsync(instanceId);
if (response.Status.Code == 0)
{
    // 로컬 데이터 갱신
    var characterModel = ServerDataManager.Instance.GetData<CharacterModel>(CharacterModel.CATEGORY_KEY);
    characterModel.UpdateCharacter(response.Character);

    // 통화 델타 적용
    var walletModel = ServerDataManager.Instance.GetData<WalletModel>(WalletModel.CATEGORY_KEY);
    walletModel.ApplyCurrencyDeltas(response.CurrencyDeltas);
}

// 다른 액션들
await NetManager.Instance.Character.PromoteAsync(instanceId);
await NetManager.Instance.Character.TranscendAsync(instanceId);
await NetManager.Instance.Character.AllocateResonanceAsync(instanceId, nodeId, level);
```

---

## 🎯 핵심 개념

### NetLite 서비스

NetLite 프레임워크를 사용하여 gRPC 서비스를 자동으로 생성합니다.

```csharp
// Services/CharacterService.cs
[GrpcService(typeof(Tech.Hive.V1.CharacterService.CharacterServiceClient))]
public partial class CharacterService
{
    // NetLite가 자동으로 ServiceClient와 ExecuteAsync를 생성

    public async UniTask<CharacterListResponse> ListAsync(CancellationToken cancellationToken = default)
    {
        return await ExecuteAsync(
            ServiceClient.ListAsync,
            new CharacterListRequest(),
            cancellationToken: cancellationToken
        );
    }
}
```

### 델타 업데이트 (Delta Update)

서버에서 **변경된 데이터만** 받아서 로컬 데이터를 갱신합니다.

```csharp
// 전체 교체
dataManager.SetData(categoryKey, newData);

// 델타 업데이트 (변경된 부분만)
dataManager.ApplyDelta(categoryKey, deltaData);

// 또는 모델에서 직접
characterModel.UpdateCharacter(newCharacter); // 단일 캐릭터만 갱신
walletModel.ApplyCurrencyDeltas(deltas);      // 변경된 통화만 갱신
```

### 이벤트 기반 UI 갱신

데이터가 변경되면 자동으로 이벤트가 발생하여 UI가 갱신됩니다.

```csharp
// 1. 서버 응답으로 데이터 변경
characterModel.UpdateCharacter(newCharacter);

// 2. 모델에서 이벤트 발생
characterModel.OnCharacterUpdated?.Invoke(character);

// 3. DataEventBus가 변경 이벤트 발행
EventBus.Publish(new DataChangeEvent(...));

// 4. CharacterDataBridge가 이벤트 수신
bridge.OnCharacterUpdated?.Invoke(character);

// 5. UI가 이벤트 수신하여 갱신
RefreshUI();
```

---

## 🔧 확장 방법

### 새로운 서비스 추가

1. **서비스 클래스 작성** (`gRPC/Impls/Services/`)
```csharp
[GrpcService(typeof(Tech.Hive.V1.BattleService.BattleServiceClient))]
public partial class BattleService
{
    public async UniTask<BattleStartResponse> StartAsync(...)
    {
        return await ExecuteAsync(
            ServiceClient.StartAsync,
            new BattleStartRequest { ... },
            cancellationToken: cancellationToken
        );
    }
}
```

2. **NetManager에 등록**
```csharp
public class NetManager : NetLiteManagerBase
{
    public CharacterService Character { get; private set; }
    public BattleService Battle { get; private set; }  // 추가
}
```

### 새로운 데이터 모델 추가

1. **모델 클래스 작성** (`Models/`)
```csharp
public class MyDataModel : IDataModel
{
    public const string CATEGORY_KEY = "my_data";

    public string CategoryKey => CATEGORY_KEY;
    public int Version { get; private set; }

    public void ApplyDelta(IDataModel delta) { /* 구현 */ }
    public void Reset() { /* 구현 */ }
    public bool Validate() { /* 구현 */ }
}
```

2. **UI 브릿지 작성** (`Bridges/`)
```csharp
public class MyDataBridge
{
    private MyDataModel _model;
    public event Action OnDataChanged;

    // UI용 메서드 구현
}
```

---

## 💡 실전 패턴

### 패턴 1: 서버 호출 + 로컬 데이터 갱신

```csharp
// 1. 서버 호출
var response = await NetManager.Instance.Character.LevelUpAsync(instanceId);

// 2. 에러 체크
if (response.Status.Code != 0)
{
    Debug.LogError($"레벨업 실패: {response.Status.Message}");
    return;
}

// 3. 캐릭터 데이터 갱신
var characterModel = ServerDataManager.Instance.GetData<CharacterModel>(CharacterModel.CATEGORY_KEY);
characterModel.UpdateCharacter(response.Character);

// 4. 통화 델타 적용
if (response.CurrencyDeltas.Count > 0)
{
    var walletModel = ServerDataManager.Instance.GetData<WalletModel>(WalletModel.CATEGORY_KEY);
    walletModel.ApplyCurrencyDeltas(response.CurrencyDeltas);
}

// 5. UI는 자동으로 갱신됨 (이벤트를 통해)
```

### 패턴 2: 배치 업데이트

```csharp
// 여러 데이터를 한 번에 업데이트할 때
ServerDataManager.Instance.BeginBatchUpdate();

characterModel.UpdateCharacter(character1);
characterModel.UpdateCharacter(character2);
walletModel.ApplyCurrencyDeltas(deltas);

ServerDataManager.Instance.EndBatchUpdate(); // 이벤트 일괄 발행
```

### 패턴 3: 커스텀 필터링

```csharp
// 복잡한 조건으로 필터링
var filteredCharacters = new List<CharacterInfo>();
characterBridge.GetFilteredCharacters(filteredCharacters, character =>
{
    return character.Level >= 10 &&
           character.Rarity == Rarity.Ur &&
           character.ClassType == ClassType.Guardian;
});
```

---

## ⚠️ 주의사항

1. **Reflection 금지**: 모든 등록은 명시적으로
2. **Linq 지양**: for문과 Dictionary 사용
3. **메모리 고려**: 불필요한 할당 최소화
4. **이벤트 구독 해제**: Dispose() 호출 필수
5. **서버 응답 체크**: Status.Code == 0 확인
6. **로컬 데이터 갱신**: 서버 응답 후 반드시 로컬 데이터 업데이트

---

## 📚 참고 자료

- **예제 코드**: `Examples/DataSystemUsageExample.cs`
- **NetManager**: `gRPC/bm013-grpc/Impls/NetManager.cs`
- **서비스 예제**: `gRPC/bm013-grpc/Impls/Services/CharacterService.cs`
- **프로토콜 명세**: `gRPC/bm013-grpc/Generated/`

---

## 🐛 트러블슈팅

### Q: 데이터가 갱신되지 않아요
A: 서버 호출 후 로컬 데이터를 갱신했는지 확인하세요.
```csharp
var response = await NetManager.Instance.Character.LevelUpAsync(id);
// 이 부분 필수!
characterModel.UpdateCharacter(response.Character);
```

### Q: UI 이벤트가 발생하지 않아요
A: DataBridge가 이벤트를 구독했는지 확인하세요.
```csharp
bridge.OnDataChanged += YourHandler;
```

### Q: NetManager.Character가 null이에요
A: NetManager.Startup()을 호출했는지 확인하세요.
```csharp
NetManager.Instance.Startup();
```

### Q: 메모리 누수가 발생해요
A: Dispose()를 호출하여 이벤트 구독을 해제하세요.
```csharp
bridge.Dispose();
```

---

## 📝 마이그레이션 가이드

### Legacy 코드에서 새 시스템으로

**기존:**
```csharp
// Legacy UserDataManager (Reflection)
UserDataManager.Instance.UserBasicData.Level

// Legacy GrpcManager (Stub)
await GrpcManager.Instance.PlayerData.ListAsync(categories);
```

**새로운:**
```csharp
// 새 시스템 (명시적)
var characterBridge = new CharacterDataBridge();
var character = characterBridge.GetCharacter(instanceId);
character.Level

// NetManager (실제 서버 통신)
await NetManager.Instance.Character.ListAsync();
```

---

## 🎓 학습 순서

1. **예제 실행**: `Examples/DataSystemUsageExample.cs`를 게임 오브젝트에 추가하여 실행
2. **NetManager 이해**: `NetManager.cs`와 `CharacterService.cs` 읽기
3. **데이터 모델 이해**: `CharacterModel.cs`, `WalletModel.cs` 읽기
4. **이벤트 시스템 이해**: `DataEventBus.cs` 읽기
5. **실전 적용**: 기존 UI에 새 시스템 적용
