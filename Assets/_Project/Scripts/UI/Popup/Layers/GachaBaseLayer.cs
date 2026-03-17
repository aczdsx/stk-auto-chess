// 서버 문제 시 Mock 모드 활성화 (테스트 완료 후 주석 처리)
// #define GACHA_MOCK_MODE

using System.Collections.Generic;
using CookApps.TeamBattle.UIManagements;
using CookApps.TeamBattle.Utility;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.EventSystems;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

namespace CookApps.AutoBattler
{
    public enum GachaCountType
    {
        ONE,
        TEN,
    }

    public class GachaBaseLayer : MonoBehaviour
    {
        [SerializeField] private GachaType gachaType;

        protected GachaInfo _specGachaDataOneTime;
        protected GachaInfo _specGachaDataTenTime;

        protected GachaPopup _parentGachaPopup;


        private GachaInfo _currentSpecGachaData;

        public GachaType CurrentGachaType => gachaType;

        private AsyncOperationHandle<SceneInstance> _gachaSceneHandle;
        private Camera _previousMainCamera;
        private Canvas _mainCanvas;

        private void OnDestroy()
        {
            UnloadGachaScene();
        }

        private void UnloadGachaScene()
        {
            // MainCanvas 복원
            if (_mainCanvas != null)
            {
                _mainCanvas.gameObject.SetActive(true);
                _mainCanvas = null;
            }

            // 기존 메인 카메라 복원
            if (_previousMainCamera != null)
            {
                _previousMainCamera.gameObject.SetActive(true);
                _previousMainCamera = null;
            }

            if (_gachaSceneHandle.IsValid() && _gachaSceneHandle.IsDone
                && _gachaSceneHandle.Status == AsyncOperationStatus.Succeeded
                && _gachaSceneHandle.Result.Scene.isLoaded)
            {
                Addressables.UnloadSceneAsync(_gachaSceneHandle);
            }
            _gachaSceneHandle = default;
        }

        private async UniTask UnloadGachaSceneAsync()
        {
            // MainCanvas 복원
            if (_mainCanvas != null)
            {
                _mainCanvas.gameObject.SetActive(true);
                _mainCanvas = null;
            }

            // 기존 메인 카메라 복원
            if (_previousMainCamera != null)
            {
                _previousMainCamera.gameObject.SetActive(true);
                _previousMainCamera = null;
            }

            if (_gachaSceneHandle.IsValid() && _gachaSceneHandle.IsDone
                && _gachaSceneHandle.Status == AsyncOperationStatus.Succeeded
                && _gachaSceneHandle.Result.Scene.isLoaded)
            {
                await Addressables.UnloadSceneAsync(_gachaSceneHandle);
            }
            _gachaSceneHandle = default;
        }

        // 유효 기간이 있는 가챠 타입인 경우 체크
        public bool CheckValidGachaPeriod(GachaCountType gachaCountType)
        {
            SetGachaData(gachaCountType);

            if (_currentSpecGachaData == null) return false;

            // 기간 타입 체크
            if (_currentSpecGachaData.gacha_term_type != GachaTermType.PERIOD) return true;

            // 유효 기간 체크
            var startTimeStamp = TimeManager.Instance.ChangeDateStringToTimeStamp(_currentSpecGachaData.start_at);
            var endTimeStamp = TimeManager.Instance.ChangeDateStringToTimeStamp(_currentSpecGachaData.end_at);

            return TimeManager.Instance.IsValidTimeNow(startTimeStamp, endTimeStamp);
        }

        // 캐릭터 가챠 프로세스 진행
        protected async UniTask ProcessCharacterGacha(GachaCountType gachaCountType)
        {
            SetGachaData(gachaCountType);

            if (_currentSpecGachaData == null) return;

#if !GACHA_MOCK_MODE
            // 재화 검사 (ServerDataManager 사용)
            ItemId costItemId = _currentSpecGachaData.gacha_cost_item_id;
            if (!ServerDataManager.Instance.Inventory.HasEnoughCurrency(costItemId, (ulong)_currentSpecGachaData.gacha_cost))
            {
                // 재화 부족 토스트 메시지
                ToastManager.Instance.ShowToastByTokenKey("MSG_NOT_ENOUGH_CURRENCY");
                return;
            }

            // 서버 가챠 API 호출
            var response = await NetManager.Instance.Gacha.DrawAsync(_currentSpecGachaData.gacha_id.ToString());

            if (response == null || !response.IsSuccess)
            {
                // 가챠 실패 처리
                return;
            }

            // 서버 응답의 가챠 결과를 RewardItem 리스트로 변환
            var resultGachaList = new List<RewardItem>();
            for (int i = 0; i < response.Results.Count; i++)
            {
                var result = response.Results[i];
                resultGachaList.Add(new RewardItem
                {
                    Id = (int)result.ItemId,
                    Count = (int)result.Count
                });
            }

            if (resultGachaList.Count <= 0) return;
#else
            // ========== MOCK MODE ==========
            Debug.Log("[GachaBaseLayer] MOCK MODE 활성화 - 서버 호출 생략");

            var resultGachaList = GenerateMockGachaResult(gachaCountType);
            await UniTask.Delay(500); // 서버 응답 시뮬레이션
            // ================================
#endif

            // 기존 씬 정리 (다시모집 시) — 언로드 완료 대기 후 새 씬 로드
            await UnloadGachaSceneAsync();

            // Additive 씬 로드
            _gachaSceneHandle = Addressables.LoadSceneAsync("Assets/_Project/Addressables/Remote/0. Scenes/Gacha_New.unity", LoadSceneMode.Additive);
            await _gachaSceneHandle;

            if (!_gachaSceneHandle.IsValid() || !_gachaSceneHandle.Result.Scene.isLoaded)
            {
                Debug.LogError("[GachaBaseLayer] Failed to load Gacha_New scene.");
                var fallbackParam = new GachaResultPopupParam
                {
                    ResultItems = resultGachaList,
                    SpecGachaData = _currentSpecGachaData,
                    OnContinueGacha = () => ProcessCharacterGacha(gachaCountType).Forget()
                };
                SceneUILayerManager.Instance.PushUILayerAsync<GachaResultPopup>(fallbackParam).Forget();
                return;
            }

            // 씬에서 GachaNewController 찾기 + Additive 씬 충돌 컴포넌트 비활성화
            GachaNewController controller = null;
            var rootObjects = _gachaSceneHandle.Result.Scene.GetRootGameObjects();
            for (int i = 0; i < rootObjects.Length; i++)
            {
                // Additive 씬의 EventSystem 비활성화 (원본 씬과 충돌 방지)
                var eventSystem = rootObjects[i].GetComponent<EventSystem>();
                if (eventSystem != null)
                {
                    eventSystem.gameObject.SetActive(false);
                    continue;
                }

                // Additive 씬의 AudioListener 비활성화 (중복 경고 방지)
                var listener = rootObjects[i].GetComponentInChildren<AudioListener>();
                if (listener != null)
                    listener.enabled = false;

                if (controller == null)
                    controller = rootObjects[i].GetComponentInChildren<GachaNewController>();
            }

            if (controller == null)
            {
                Debug.LogError("[GachaBaseLayer] GachaNewController not found in Gacha_New scene.");
                UnloadGachaScene();
                return;
            }

            // 기존 메인 카메라 비활성화 (Additive 씬 카메라와 겹침 방지)
            _previousMainCamera = Camera.main;
            if (_previousMainCamera != null)
                _previousMainCamera.gameObject.SetActive(false);

            // 통화 변화는 GachaService에서 자동으로 적용됨 (CurrencyDeltas)

            SoundManager.Instance.StopBGM();
            SoundManager.Instance.IsPlayingGacha = true;

            // MainCanvas 비활성화 (GraphicRaycaster가 터치 이벤트 가로채는 것 방지)
            _mainCanvas = SceneUILayerManager.Instance.MainCanvas;
            if (_mainCanvas != null)
                _mainCanvas.gameObject.SetActive(false);

            // Additive 씬 로드 후 Canvas 강제 리빌드 (Graphic.depth=-1 방지)
            Canvas.ForceUpdateCanvases();

            controller.SetItem(resultGachaList, _currentSpecGachaData,
                () => ProcessCharacterGacha(gachaCountType).Forget(),
                () => UnloadGachaScene());
        }

        private void SetGachaData(GachaCountType gachaCountType)
        {
            switch (gachaCountType)
            {
                case GachaCountType.ONE:
                    _currentSpecGachaData = _specGachaDataOneTime;
                    break;
                case GachaCountType.TEN:
                    _currentSpecGachaData = _specGachaDataTenTime;
                    break;
            }
        }

#if GACHA_MOCK_MODE
        /// <summary>
        /// Mock 가챠 결과 생성 (서버 문제 시 테스트용)
        /// </summary>
        private List<RewardItem> GenerateMockGachaResult(GachaCountType gachaCountType)
        {
            var result = new List<RewardItem>();

            // 캐릭터 조각 ID 풀 (character_id % 10000 = 실제 캐릭터 ID)
            // Epic 캐릭터 조각
            int[] epicPieceIds = { 1032101, 1032102, 1032201, 1032202, 1032301, 1032302, 1032401, 1032501 };
            // Legendary 캐릭터 조각 (SSR 연출 테스트용)
            int[] legendaryPieceIds = { 1053101, 1053102, 1053103, 1053201, 1053301, 1053302, 1053303, 1053401 };

            int count = gachaCountType == GachaCountType.ONE ? 1 : 10;

            for (int i = 0; i < count; i++)
            {
                int pieceId;
                int pieceCount;

                // 10% 확률로 레전더리
                if (UnityEngine.Random.value < 0.1f)
                {
                    pieceId = legendaryPieceIds[UnityEngine.Random.Range(0, legendaryPieceIds.Length)];
                    pieceCount = 20; // 신규 캐릭터 획득
                }
                else
                {
                    pieceId = epicPieceIds[UnityEngine.Random.Range(0, epicPieceIds.Length)];
                    pieceCount = UnityEngine.Random.value < 0.3f ? 20 : UnityEngine.Random.Range(5, 15);
                }

                result.Add(new RewardItem(pieceId, pieceCount));

                Debug.Log($"[Mock Gacha] [{i}] PieceId: {pieceId}, Count: {pieceCount}");
            }

            return result;
        }
#endif
    }
}