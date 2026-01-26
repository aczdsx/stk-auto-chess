using System.Collections.Generic;
using CookApps.TeamBattle;
using R3;
using Tech.Hive.V1;

namespace CookApps.AutoBattler
{
    /// <summary>
    /// 인벤토리 데이터 모델 (통화 관리)
    /// CurrencyDelta를 사용한 델타 업데이트 지원
    /// </summary>
    public class InventoryModel
    {
        // 통화 데이터 (ItemId -> Amount)
        private readonly Dictionary<uint, ulong> _currencies = new (16);

        // R3 이벤트
        public Subject<Unit> OnChanged { get; } = new();
        public readonly Subject<(uint itemId, ulong oldAmount, ulong newAmount)> OnCurrencyChanged = new();

        /// <summary>
        /// 데이터 초기화
        /// </summary>
        public void Reset()
        {
            _currencies.Clear();
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
            return _currencies.TryGetValue(itemId, out var amount) ? amount : 0;
        }

        /// <summary>
        /// 통화 설정 (내부용)
        /// </summary>
        internal void SetCurrency(uint itemId, ulong amount)
        {
            _currencies.TryGetValue(itemId, out var oldAmount);
            _currencies[itemId] = amount;

            OnCurrencyChanged.OnNext((itemId, oldAmount, amount));
        }

        /// <summary>
        /// CurrencyDelta 배열로 업데이트
        /// </summary>
        internal void ApplyCurrencyDeltas(IReadOnlyList<CurrencyDelta> deltas)
        {
            if (deltas == null) return;

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

                // 일반 통화 처리
                var oldAmount = _currencies.TryGetValue(delta.ItemId, out var current) ? current : 0;
                _currencies[delta.ItemId] = delta.After;

                OnCurrencyChanged.OnNext((delta.ItemId, oldAmount, delta.After));
            }

            OnChanged.OnNext(Unit.Default);
            RefreshSummonBadge();
        }

        #region 뱃지 갱신

        /// <summary>
        /// Summon 뱃지 갱신
        /// </summary>
        private void RefreshSummonBadge()
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
    }
}
