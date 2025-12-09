# 통합 가이드: NetLite + 새로운 데이터 시스템

## 🎯 목표

NetLite 프레임워크와 새로운 서버 중심 데이터 관리 시스템을 통합하여 실제 프로젝트에 적용

---

## 📋 체크리스트

### 1단계: 서비스 구현 ✅

- [x] **CharacterService 구현** (`gRPC/Impls/Services/CharacterService.cs`)
  - NetLite의 `[GrpcService]` 어트리뷰트 사용
  - partial class로 선언
  - ExecuteAsync를 통한 gRPC 호출

- [x] **NetManager에 등록** (`gRPC/Impls/NetManager.cs`)
  ```csharp
  public CharacterService Character { get; private set; }
  ```

### 2단계: 데이터 모델 준비 ✅

- [x] **CharacterModel** - 캐릭터 데이터 관리
- [x] **WalletModel** - 통화 데이터 관리
- [x] **ServerDataManager** - 델타 업데이트 지원
- [x] **DataEventBus** - 이벤트 기반 UI 갱신

### 3단계: UI 브릿지 준비 ✅

- [x] **CharacterDataBridge** - 캐릭터 UI 바인딩
- [x] **WalletDataBridge** - 지갑 UI 바인딩

### 4단계: 실제 적용 (TODO)

- [ ] 기존 UI에 새 시스템 적용
- [ ] Legacy 코드 단계적 제거
- [ ] 추가 서비스 구현 (Battle, Equipment 등)

---

## 🚀 빠른 시작 (3단계)

### Step 1: 시스템 초기화 (게임 시작 시)

```csharp
public class GameInitializer : MonoBehaviour
{
    private void Start()
    {
        InitializeDataSystem();
        InitializeNetwork();
    }

    private void InitializeDataSystem()
    {
        // 데이터 모델 팩토리 등록
        var dataManager = ServerDataManager.Instance;
        dataManager.RegisterFactory(CharacterModel.CATEGORY_KEY, () => new CharacterModel());
        dataManager.RegisterFactory(WalletModel.CATEGORY_KEY, () => new WalletModel());
    }

    private void InitializeNetwork()
    {
        // NetManager 시작
        NetManager.Instance.Startup();
    }
}
```

### Step 2: UI에서 데이터 사용

```csharp
public class CharacterListUI : MonoBehaviour
{
    private CharacterDataBridge _characterBridge;

    private void Start()
    {
        // 브릿지 생성
        _characterBridge = new CharacterDataBridge();

        // 이벤트 구독
        _characterBridge.OnCharactersChanged += RefreshUI;

        // 초기 데이터 로드
        LoadCharactersAsync().Forget();
    }

    private async UniTaskVoid LoadCharactersAsync()
    {
        // 서버에서 캐릭터 목록 가져오기
        var response = await NetManager.Instance.Character.ListAsync();

        if (response.Status.Code == 0)
        {
            // 로컬 데이터 갱신
            var model = ServerDataManager.Instance.GetData<CharacterModel>(CharacterModel.CATEGORY_KEY);
            model.SetCharacters(response.Characters, model.Version + 1);
            ServerDataManager.Instance.SetData(CharacterModel.CATEGORY_KEY, model);
        }
    }

    private void RefreshUI()
    {
        var characters = new List<CharacterInfo>();
        _characterBridge.GetAllCharacters(characters);

        // UI 갱신 로직
        for (int i = 0; i < characters.Count; i++)
        {
            UpdateCharacterSlot(i, characters[i]);
        }
    }

    private void OnDestroy()
    {
        _characterBridge?.Dispose();
    }
}
```

### Step 3: 서버 액션 처리

```csharp
public class CharacterLevelUpButton : MonoBehaviour
{
    private string _characterInstanceId;

    public async void OnClickLevelUp()
    {
        // 서버 호출
        var response = await NetManager.Instance.Character.LevelUpAsync(_characterInstanceId);

        if (response.Status.Code == 0)
        {
            // 로컬 데이터 갱신
            var characterModel = ServerDataManager.Instance.GetData<CharacterModel>(CharacterModel.CATEGORY_KEY);
            characterModel.UpdateCharacter(response.Character);

            // 통화 델타 적용
            if (response.CurrencyDeltas.Count > 0)
            {
                var walletModel = ServerDataManager.Instance.GetData<WalletModel>(WalletModel.CATEGORY_KEY);
                walletModel.ApplyCurrencyDeltas(response.CurrencyDeltas);
            }

            // UI는 이벤트를 통해 자동 갱신됨
        }
        else
        {
            ShowErrorPopup(response.Status.Message);
        }
    }
}
```

---

## 🔄 데이터 흐름

```
[사용자 액션]
    ↓
[UI 버튼 클릭]
    ↓
[NetManager.Character.LevelUpAsync()] ← 서버 호출
    ↓
[서버 응답]
    ↓
[CharacterModel.UpdateCharacter()] ← 로컬 데이터 갱신
    ↓
[DataEventBus.Publish()] ← 이벤트 발행
    ↓
[CharacterDataBridge.OnCharacterUpdated] ← 이벤트 수신
    ↓
[UI 자동 갱신]
```

---

## 💼 실전 예제: 기존 코드 마이그레이션

### Before (Legacy)

```csharp
// Legacy UserDataManager 사용
public class OldCharacterUI : MonoBehaviour
{
    private void Start()
    {
        // Reflection 기반 초기화
        UserDataManager.Instance.Initialize();

        // 직접 데이터 접근
        var level = UserDataManager.Instance.UserBasicData.Level;

        // 수동 저장
        UserDataManager.Instance.SaveUserBasic();
    }
}
```

### After (New System)

```csharp
// 새로운 시스템 사용
public class NewCharacterUI : MonoBehaviour
{
    private CharacterDataBridge _characterBridge;

    private void Start()
    {
        // 명시적 초기화 (Reflection 없음)
        _characterBridge = new CharacterDataBridge();

        // 이벤트 기반 UI 갱신
        _characterBridge.OnCharacterUpdated += (character) =>
        {
            UpdateUI(character);
        };

        // 서버에서 데이터 가져오기 (자동 저장)
        LoadDataAsync().Forget();
    }

    private async UniTaskVoid LoadDataAsync()
    {
        var response = await NetManager.Instance.Character.ListAsync();
        // 데이터는 자동으로 로컬에 저장됨
    }

    private void OnDestroy()
    {
        _characterBridge?.Dispose();
    }
}
```

---

## 📝 추가 서비스 구현 가이드

### 1. BattleService 추가

**1단계: 서비스 클래스 작성**
```csharp
// gRPC/Impls/Services/BattleService.cs
[GrpcService(typeof(Tech.Hive.V1.BattleService.BattleServiceClient))]
public partial class BattleService
{
    public async UniTask<BattleStartResponse> StartAsync(
        string stageId,
        CancellationToken cancellationToken = default)
    {
        return await ExecuteAsync(
            ServiceClient.StartAsync,
            new BattleStartRequest { StageId = stageId },
            cancellationToken: cancellationToken
        );
    }

    public async UniTask<BattleEndResponse> EndAsync(
        string battleId,
        bool isWin,
        CancellationToken cancellationToken = default)
    {
        return await ExecuteAsync(
            ServiceClient.EndAsync,
            new BattleEndRequest { BattleId = battleId, IsWin = isWin },
            cancellationToken: cancellationToken
        );
    }
}
```

**2단계: NetManager에 등록**
```csharp
public class NetManager : NetLiteManagerBase
{
    public CharacterService Character { get; private set; }
    public BattleService Battle { get; private set; } // 추가
}
```

**3단계: 사용**
```csharp
var response = await NetManager.Instance.Battle.StartAsync("stage_1_1");
```

---

## 🎓 모범 사례

### DO ✅

1. **서버 응답 후 항상 로컬 데이터 갱신**
   ```csharp
   var response = await NetManager.Instance.Character.LevelUpAsync(id);
   characterModel.UpdateCharacter(response.Character);
   ```

2. **이벤트로 UI 갱신**
   ```csharp
   characterBridge.OnCharacterUpdated += RefreshUI;
   ```

3. **에러 처리**
   ```csharp
   if (response.Status.Code != 0)
   {
       Debug.LogError($"Error: {response.Status.Message}");
       return;
   }
   ```

4. **Dispose 호출**
   ```csharp
   private void OnDestroy()
   {
       _characterBridge?.Dispose();
   }
   ```

### DON'T ❌

1. **Reflection 사용 금지**
   ```csharp
   // ❌ 금지
   var method = typeof(Manager).GetMethod("Initialize");
   method.Invoke(instance, null);
   ```

2. **Linq 지양**
   ```csharp
   // ❌ 지양
   var filtered = characters.Where(c => c.Level > 10).ToList();

   // ✅ 권장
   var filtered = new List<CharacterInfo>();
   for (int i = 0; i < characters.Count; i++)
   {
       if (characters[i].Level > 10)
           filtered.Add(characters[i]);
   }
   ```

3. **서버 응답 무시 금지**
   ```csharp
   // ❌ 금지 - 로컬 데이터 갱신 없음
   await NetManager.Instance.Character.LevelUpAsync(id);
   // UI에 표시만 하고 끝내면 다음 로드 시 원복됨

   // ✅ 권장 - 로컬 데이터 갱신
   var response = await NetManager.Instance.Character.LevelUpAsync(id);
   characterModel.UpdateCharacter(response.Character);
   ```

---

## 🔍 디버깅 팁

### 1. 데이터가 갱신되지 않을 때

```csharp
// 데이터 확인
var model = ServerDataManager.Instance.GetData<CharacterModel>(CharacterModel.CATEGORY_KEY);
Debug.Log($"Character Count: {model.CharacterCount}");
Debug.Log($"Version: {model.Version}");

// 이벤트 확인
DataEventBus.Instance.Subscribe(CharacterModel.CATEGORY_KEY, (e) =>
{
    Debug.Log($"Data Changed: {e.ChangeType}");
});
```

### 2. 네트워크 응답 확인

```csharp
var response = await NetManager.Instance.Character.ListAsync();
Debug.Log($"Status Code: {response.Status.Code}");
Debug.Log($"Status Message: {response.Status.Message}");
Debug.Log($"Character Count: {response.Characters.Count}");
```

### 3. 메모리 누수 확인

```csharp
// 구독 해제 확인
private void OnDestroy()
{
    Debug.Log("Disposing bridges...");
    _characterBridge?.Dispose();
    _walletBridge?.Dispose();
}
```

---

## 📞 문제 해결

### 문제: NetManager.Character가 null
**해결**: NetManager.Startup() 호출 확인

### 문제: 이벤트가 발생하지 않음
**해결**: Bridge 이벤트 구독 확인

### 문제: 데이터가 원복됨
**해결**: 서버 응답 후 로컬 데이터 갱신 확인

### 문제: 메모리 누수
**해결**: Dispose() 호출 확인

---

## 📊 마일스톤

### Phase 1: 기본 통합 (완료) ✅
- [x] CharacterService 구현
- [x] 데이터 모델 구현
- [x] UI 브릿지 구현
- [x] 예제 코드 작성

### Phase 2: 확장 (진행 중) 🚧
- [ ] BattleService 구현
- [ ] EquipmentService 구현
- [ ] 기존 UI 마이그레이션

### Phase 3: 최적화 (계획) 📅
- [ ] 성능 프로파일링
- [ ] 메모리 최적화
- [ ] 배치 처리 최적화

---

작성일: 2025-12-05
버전: 1.0.0
