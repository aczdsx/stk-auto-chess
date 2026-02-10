using System;
using System.Collections.Generic;
using CookApps.TeamBattle;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json.Linq;
using R3;
using Tech.Hive.V1;

namespace CookApps.AutoBattler
{
    public class ActionPoint
    {
        public const long MaxActionPoint = 20; // 이 행동력 이상이면 충전안됨
        public const long ActionPointRecoveryIntervalSeconds = 600; // 행동력 1회복까지 걸리는 시간(초)

        public int Current;
        public long LastSyncAt; // 서버에서 내려준 충전 시작 시간 (밀리초)

        private UniTask? _syncTask;

        public void Reset()
        {
            Current = 0;
            LastSyncAt = 0;
            _syncTask = null;
        }

        /// <summary>
        /// 서버에서 ActionPoint 동기화 (중복 호출 방지)
        /// </summary>
        public void RequestSync()
        {
            if (_syncTask is { Status: UniTaskStatus.Pending })
                return;

            _syncTask = SyncAsync();
        }

        /// <summary>
        /// 충전이 필요한 시간이 지났으면 동기화 요청
        /// </summary>
        public void RequestSyncIfNeeded()
        {
            // 이미 최대치면 동기화 불필요
            if (Current >= MaxActionPoint)
                return;

            // LastSyncAt이 0이면 아직 초기화 안됨
            if (LastSyncAt == 0)
                return;

            var nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var elapsedMs = nowMs - LastSyncAt;
            var recoveryIntervalMs = ActionPointRecoveryIntervalSeconds * 1000;

            // 충전 시간이 지났으면 동기화 요청
            if (elapsedMs >= recoveryIntervalMs)
            {
                RequestSync();
            }
        }

        private async UniTask SyncAsync()
        {
            await NetManager.Instance.Inventory.GetAsync((uint)IdMap.Item.ActionPoint.Value);
        }
    }

    /// <summary>
    /// 인벤토리 데이터 모델 (통화 관리)
    /// CurrencyDelta를 사용한 델타 업데이트 지원
    /// </summary>
    public class InventoryModel
    {
        // 통화 데이터 (ItemId -> Amount)
        private readonly Dictionary<uint, ulong> _currencies = new (16);

        // ActionPoint 데이터
        public ActionPoint ActionPoint { get; } = new();

        // R3 이벤트
        public Subject<Unit> OnChanged { get; } = new();
        public readonly Subject<(uint itemId, ulong oldAmount, ulong newAmount)> OnCurrencyChanged = new();

        /// <summary>
        /// 데이터 초기화
        /// </summary>
        public void Reset()
        {
            _currencies.Clear();
            ActionPoint.Reset();
            OnChanged.OnNext(Unit.Default);
        }

        /// <summary>
        /// 유효성 검증
        /// </summary>
        public bool Validate()
        {
            // 음수 통화 체크
            foreach (var amount in _currencies.Values)
            {
                if (amount < 0)
                {
                    Debug.LogError("[InventoryModel] Invalid currency: negative amount");
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// 통화 가져오기
        /// </summary>
        public ulong GetCurrency(uint itemId)
        {
            // ActionPoint는 ActionPoint 객체에서 반환
            if (itemId == (uint)IdMap.Item.ActionPoint.Value)
            {
                ActionPoint.RequestSyncIfNeeded();
                return (ulong)ActionPoint.Current;
            }

            return _currencies.TryGetValue(itemId, out var amount) ? amount : 0;
        }

        public ulong GetCurrency(int itemId)
        {
            if (itemId < 0)
            {
                Debug.LogError("[InventoryModel] Invalid currency: negative item id");
                return 0L;
            }

            return GetCurrency((uint)itemId);
        }

        /// <summary>
        /// 통화 설정 (내부용)
        /// </summary>
        internal void SetCurrency(uint itemId, ulong amount, string metadata = null)
        {
            // ActionPoint는 ActionPoint 객체에만 저장
            if (itemId == (uint)IdMap.Item.ActionPoint.Value)
            {
                var oldAmount = (ulong)ActionPoint.Current;
                long lastSyncAt = 0;
                if (!string.IsNullOrEmpty(metadata))
                {
                    try
                    {
                        var json = JObject.Parse(metadata);
                        lastSyncAt = json.Value<long>("lastSyncAt");
                    }
                    catch
                    {
                        // metadata 파싱 실패 시 무시
                    }
                }
                ActionPoint.Current = (int)amount;
                ActionPoint.LastSyncAt = lastSyncAt;
                OnCurrencyChanged.OnNext((itemId, oldAmount, amount));
                return;
            }

            _currencies.TryGetValue(itemId, out var old);
            _currencies[itemId] = amount;
            OnCurrencyChanged.OnNext((itemId, old, amount));
        }

        /// <summary>
        /// CurrencyDelta 배열로 업데이트
        /// </summary>
        internal void ApplyCurrencyDeltas(IReadOnlyList<CurrencyDelta> deltas)
        {
            if (deltas == null) return;

            var needsApSync = false;

            for (var i = 0; i < deltas.Count; i++)
            {
                var delta = deltas[i];
                ItemId itemId = (int)delta.ItemId;

                // 캐릭터인 경우 CharacterModel에 추가
                if (itemId.IsCharacter())
                {
                    itemId.GetCharacterId(out var charIndex);
                    ServerDataManager.Instance.Character.AddCharacterById((uint)charIndex);
                    continue;
                }

                // ActionPoint인 경우 서버에서 다시 가져와야 함 (metadata 필요)
                if (delta.ItemId == (uint)IdMap.Item.ActionPoint.Value)
                {
                    needsApSync = true;
                    continue;
                }

                // 일반 통화 처리
                var oldAmount = _currencies.TryGetValue(delta.ItemId, out var current) ? current : 0;
                _currencies[delta.ItemId] = delta.After;

                OnCurrencyChanged.OnNext((delta.ItemId, oldAmount, delta.After));
            }

            // ActionPoint는 metadata가 필요하므로 서버에서 다시 가져옴
            if (needsApSync)
            {
                ActionPoint.RequestSync();
            }

            OnChanged.OnNext(Unit.Default);
            RefreshSummonBadge();
        }

        #region 뱃지 갱신

        /// <summary>
        /// Summon 뱃지 갱신
        /// </summary>
        public void RefreshSummonBadge()
        {
            const string path = "Summon";

            if (HasCharacterTicket())
            {
                BadgeManager.Instance.AddBadge(BadgeType.RedDot, path);
            }
            else
            {
                BadgeManager.Instance.RemoveBadge(BadgeType.RedDot, path);
            }
        }

        /// <summary>
        /// 캐릭터 티켓 보유 여부 확인
        /// </summary>
        private bool HasCharacterTicket()
        {
            return HasCurrency((uint)IdMap.Item.CharacterTicket.Value);
        }

        #endregion

        /// <summary>
        /// 모든 통화 가져오기
        /// </summary>
        public void GetAllCurrencies(Dictionary<uint, ulong> output)
        {
            if (output == null) return;

            output.Clear();
            foreach (var kvp in _currencies)
            {
                output[kvp.Key] = kvp.Value;
            }
        }

        /// <summary>
        /// 통화 개수
        /// </summary>
        public int CurrencyCount => _currencies.Count;

        /// <summary>
        /// 특정 통화 보유 여부
        /// </summary>
        public bool HasCurrency(uint itemId)
        {
            return _currencies.ContainsKey(itemId) && _currencies[itemId] > 0;
        }

        /// <summary>
        /// 통화 충분 여부 체크
        /// </summary>
        public bool HasEnoughCurrency(uint itemId, ulong requiredAmount)
        {
            return GetCurrency(itemId) >= requiredAmount;
        }

        public bool HasEnoughCurrency(int itemId, ulong requiredAmount)
        {
            if (itemId < 0)
            {
                Debug.LogError("[InventoryModel] Invalid currency: negative item id");
                return false;
            }

            return HasEnoughCurrency((uint)itemId, requiredAmount);
        }

        /// <summary>
        /// 통화 충분 여부 (부족 시 토스트 표시)
        /// </summary>
        /// [변경 이력] InventoryDataBridge 제거: HasEnoughCurrency(+Toast)를 Model로 이동
        public bool HasEnoughCurrency(uint itemId, ulong requiredAmount, bool showToast)
        {
            bool hasEnough = GetCurrency(itemId) >= requiredAmount;
            if (!hasEnough && showToast)
            {
                string toastKey = GetNotEnoughCurrencyToastKey(itemId);
                ToastManager.Instance.ShowToastByTokenKey(toastKey);
            }
            return hasEnough;
        }

        private static string GetNotEnoughCurrencyToastKey(uint itemId)
        {
            ItemId id = (int)itemId;
            if (id == IdMap.Item.Gold) return "MSG_NOT_ENOUGH_GOLD";
            if (id == IdMap.Item.ActionPoint) return "MSG_NOT_ENOUGH_AP";
            if (id == IdMap.Item.Jewel) return "MSG_NOT_ENOUGH_GACHA_JEWEL";
            if (id == IdMap.Item.CharacterTicket) return "MSG_NOT_ENOUGH_GACHA_C_TICKET";
            if (id == IdMap.Item.CharExp) return "MSG_NOT_ENOUGH_CHAR_EXP";
            if (id == IdMap.Item.Soul) return "MSG_NOT_ENOUGH_CHAR_EXP_2";
            if (id.IsCharacterPiece()) return "MSG_NOT_ENOUGH_CHAR_PIECE";
            return "MSG_NOT_ENOUGH_CURRENCY";
        }
    }
}
