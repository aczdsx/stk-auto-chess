using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Tech.Hive.V1;
using UnityEngine;

namespace CookApps.AutoBattler
{
    /// <summary>
    /// 새로운 데이터 시스템 사용 예제 (NetLite 프레임워크)
    /// Reflection 없음, Linq 지양, 메모리 최적화, for문 최소화
    /// </summary>
    public class DataSystemUsageExample : MonoBehaviour
    {
        private CharacterModel _characterModel;
        private InventoryModel _inventoryModel;

        private async void Start()
        {
            // 1. 시스템 초기화
            InitializeDataSystem();

            // 2. 네트워크 시작
            InitializeNetwork();

            // 3. 데이터 로드
            await LoadGameDataAsync();

            // 4. UI 바인딩 설정
            SetupUIBindings();

            // 5. 게임플레이 예제
            await GameplayExampleAsync();
        }

        /// <summary>
        /// 1. 시스템 초기화
        /// </summary>
        private void InitializeDataSystem()
        {
            Debug.Log("=== 1. 시스템 초기화 ===");

            // 데이터 매니저는 자동으로 모든 모델 초기화
            var dataManager = ServerDataManager.Instance;

            // 모델 참조
            _characterModel = ServerDataManager.Instance.Character;
            _inventoryModel = ServerDataManager.Instance.Inventory;

            Debug.Log("✓ 시스템 초기화 완료");
        }

        /// <summary>
        /// 2. 네트워크 초기화 (NetLite)
        /// </summary>
        private void InitializeNetwork()
        {
            Debug.Log("=== 2. 네트워크 초기화 ===");

            // NetManager를 통해 네트워크 시작
            NetManager.Instance.Startup();

            Debug.Log("✓ 네트워크 초기화 완료");
        }

        /// <summary>
        /// 3. 데이터 로드
        /// </summary>
        private async UniTask LoadGameDataAsync()
        {
            Debug.Log("=== 3. 데이터 로드 ===");

            // NetManager를 통해 캐릭터 목록 가져오기
            var response = await NetManager.Instance.Character.ListAsync();

            if (response != null && response.IsSuccess)
            {
                // 서버 응답으로 로컬 데이터 갱신
                var characterModel = ServerDataManager.Instance.Character;
                characterModel.SetCharacters(response.Characters);
                // ServerDataManager.SetData 호출 제거 (SetCharacters 내부에서 이벤트 발생)

                Debug.Log($"✓ 캐릭터 로드 완료: {response.Characters.Count}명");
            }
            else
            {
                Debug.LogError($"✗ 캐릭터 로드 실패: {response?.Status.Message}");
            }
        }

        /// <summary>
        /// 4. UI 바인딩 설정
        /// </summary>
        private void SetupUIBindings()
        {
            Debug.Log("=== 4. UI 바인딩 설정 ===");

            // // 캐릭터 변경 이벤트 구독
            // _characterModel.OnCharactersChanged += OnCharactersChanged;
            // _characterModel.OnCharacterUpdated += OnCharacterUpdated;
            //
            // // 통화 변경 이벤트 구독
            // _inventoryModel.OnCurrencyChanged += OnCurrencyChanged;
            // _inventoryModel.OnInventoryChanged += OnInventoryChanged;

            Debug.Log("✓ UI 바인딩 설정 완료");
        }

        /// <summary>
        /// 5. 게임플레이 예제
        /// </summary>
        private async UniTask GameplayExampleAsync()
        {
            Debug.Log("=== 5. 게임플레이 예제 ===");

            // 예제 1: 모든 캐릭터 조회
            Example_GetAllCharacters();

            // 예제 2: 조건별 필터링
            Example_FilterCharacters();

            // 예제 3: 캐릭터 레벨업
            await Example_LevelUpCharacterAsync();

            // 예제 4: 통화 확인
            Example_CheckCurrency();
        }

        /// <summary>
        /// 예제 1: 모든 캐릭터 조회
        /// </summary>
        private void Example_GetAllCharacters()
        {
            Debug.Log("--- 예제 1: 모든 캐릭터 조회 ---");

            var characters = new List<CharacterData>();
            _characterModel.GetAllCharacters(characters);

            Debug.Log($"전체 캐릭터 수: {characters.Count}");

            // for문 사용 (Linq 지양)
            for (int i = 0; i < characters.Count && i < 3; i++)
            {
                var character = characters[i];
                Debug.Log($"  [{i}] {character.CharacterId} - Lv.{character.Level}");
            }
        }

        /// <summary>
        /// 예제 2: 조건별 필터링
        /// </summary>
        private void Example_FilterCharacters()
        {
            Debug.Log("--- 예제 2: 조건별 필터링 ---");

            // 레벨 10 이상 캐릭터
            var highLevelCharacters = new List<CharacterData>();
            _characterModel.GetCharactersByLevelRange(highLevelCharacters, 10, 99);
            Debug.Log($"레벨 10 이상 캐릭터: {highLevelCharacters.Count}명");

            // // Guardian 클래스 캐릭터
            // var guardians = new List<CharacterData>();
            // _characterModel.GetCharactersByClass(guardians, ClassType.Guardian);
            // Debug.Log($"Guardian 클래스 캐릭터: {guardians.Count}명");

            // UR 등급 캐릭터
            var urCharacters = new List<CharacterData>();
            _characterModel.GetCharactersByRarity(urCharacters, GradeType.SSR);
            Debug.Log($"UR 등급 캐릭터: {urCharacters.Count}명");
        }

        /// <summary>
        /// 예제 3: 캐릭터 레벨업 (NetManager 사용)
        /// </summary>
        private async UniTask Example_LevelUpCharacterAsync()
        {
            Debug.Log("--- 예제 3: 캐릭터 레벨업 ---");

            var characters = new List<CharacterData>();
            _characterModel.GetAllCharacters(characters);

            if (characters.Count > 0)
            {
                var targetCharacter = characters[0];
                Debug.Log($"레벨업 대상: {targetCharacter.CharacterId} (현재 Lv.{targetCharacter.Level})");

                // NetManager를 통해 레벨업 요청
                var response = await NetManager.Instance.Character.LevelUpAsync(targetCharacter.CharacterId);

                if (response != null && response.IsSuccess)
                {
                    Debug.Log($"✓ 레벨업 성공! 새 레벨: {response.Character.Level}");

                    // 로컬 데이터 갱신
                    ServerDataManager.Instance.Character.UpdateCharacter(response.Character);

                    // 통화 변화 적용
                    if (response.CurrencyDeltas.Count > 0)
                    {
                        ServerDataManager.Instance.Inventory.ApplyCurrencyDeltas(response.CurrencyDeltas);

                        Debug.Log("통화 변화:");
                        for (int i = 0; i < response.CurrencyDeltas.Count; i++)
                        {
                            var delta = response.CurrencyDeltas[i];
                            Debug.Log($"  ItemID {delta.ItemId}: {delta.Before} → {delta.After} ({delta.Delta:+#;-#;0})");
                        }
                    }
                }
                else
                {
                    Debug.LogWarning($"✗ 레벨업 실패: {response?.Status.Message}");
                }
            }
            else
            {
                Debug.LogWarning("캐릭터가 없습니다");
            }
        }

        /// <summary>
        /// 예제 4: 통화 확인
        /// </summary>
        private void Example_CheckCurrency()
        {
            Debug.Log("--- 예제 4: 통화 확인 ---");

            // 특정 통화 조회 (예: 골드 = ItemID 1)
            ulong gold = _inventoryModel.GetCurrency(1);
            Debug.Log($"보유 골드: {gold}");

            // 충분 여부 체크
            bool hasEnough = _inventoryModel.HasEnoughCurrency(1, 1000);
            Debug.Log($"골드 1000 이상 보유: {hasEnough}");

            // 모든 통화 조회
            var allCurrencies = new Dictionary<uint, ulong>();
            _inventoryModel.GetAllCurrencies(allCurrencies);
            Debug.Log($"보유 통화 종류: {allCurrencies.Count}");

            foreach (var kvp in allCurrencies)
            {
                Debug.Log($"  ItemID {kvp.Key}: {kvp.Value}");
            }
        }

        /// <summary>
        /// UI 이벤트 핸들러: 캐릭터 목록 변경
        /// </summary>
        private void OnCharactersChanged()
        {
            Debug.Log("[UI Event] 캐릭터 목록 변경됨 - UI 갱신 필요");
            // 실제 UI 갱신 로직을 여기에 작성
        }

        /// <summary>
        /// UI 이벤트 핸들러: 특정 캐릭터 업데이트
        /// </summary>
        private void OnCharacterUpdated(CharacterData character)
        {
            Debug.Log($"[UI Event] 캐릭터 업데이트: {character.CharacterId} - Lv.{character.Level}");
            // 해당 캐릭터 UI만 갱신
        }

        /// <summary>
        /// UI 이벤트 핸들러: 통화 변경
        /// </summary>
        private void OnCurrencyChanged(uint itemId, ulong newAmount)
        {
            Debug.Log($"[UI Event] 통화 변경: ItemID {itemId} = {newAmount}");
            // 통화 UI 갱신
        }

        /// <summary>
        /// UI 이벤트 핸들러: 인벤토리 전체 변경
        /// </summary>
        private void OnInventoryChanged()
        {
            Debug.Log("[UI Event] 인벤토리 전체 변경됨 - 모든 통화 UI 갱신 필요");
            // 전체 통화 UI 갱신
        }
    }
}
