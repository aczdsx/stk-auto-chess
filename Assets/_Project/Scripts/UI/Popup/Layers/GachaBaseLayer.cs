// 서버 문제 시 Mock 모드 활성화 (테스트 완료 후 주석 처리)
// #define GACHA_MOCK_MODE

using System.Collections.Generic;
using CookApps.TeamBattle.UIManagements;
using CookApps.TeamBattle.Utility;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

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

        private List<AsyncOperationHandle<GameObject>> _loadedGachaFxHandles = new List<AsyncOperationHandle<GameObject>>();

        private void OnDestroy()
        {
            foreach (var handle in _loadedGachaFxHandles)
            {
                handle.Release();
            }
            _loadedGachaFxHandles.Clear();
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

            bool isOneTime = gachaCountType == GachaCountType.ONE;
            var handle = Addressables.InstantiateAsync("Gacha_VFX_Ver_Final_01");
            await handle.WaitUntilDone();
            // handle.Result.GetComponent<GachaFxByTen>().SetItem(resultGachaList, isOneTime);
            _loadedGachaFxHandles.Add(handle);

            // 통화 변화는 GachaService에서 자동으로 적용됨 (CurrencyDeltas)

            SoundManager.Instance.StopBGM();
            SoundManager.Instance.IsPlayingGacha = true;

            _parentGachaPopup.SetCanvasTargetDisplay(1);

            // VFX 완료 후 가챠 결과 팝업 표시
            var resultParam = new GachaResultPopupParam
            {
                ResultItems = resultGachaList,
                SpecGachaData = _currentSpecGachaData,
                OnContinueGacha = () => ProcessCharacterGacha(gachaCountType).Forget()
            };
            SceneUILayerManager.Instance.PushUILayerAsync<GachaResultPopup>(resultParam).Forget();
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