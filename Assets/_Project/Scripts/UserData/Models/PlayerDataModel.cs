using System;
using System.Collections.Generic;
using CookApps.TeamBattle;
using R3;
using Tech.Hive.V1;

namespace CookApps.AutoBattler
{
    /// <summary>
    /// 플레이어 데이터 모델
    /// 플레이어의 기본 정보(레벨, 경험치, 닉네임 등)를 관리
    /// Level/Exp는 InventoryModel의 UserExp 기반으로 SpecData에서 계산
    /// </summary>
    public class PlayerDataModel
    {
        // 플레이어 기본 정보
        private string _playerId = string.Empty;
        private string _nickname = string.Empty;
        private uint _serverId;
        private string _representativeCharacterId = string.Empty;
        private ulong _lastAccessedAt;

        // 레벨 변경 감지용 캐시
        private uint _cachedLevel;

        // InventoryModel 구독
        private IDisposable _inventorySubscription;

        // R3 이벤트
        public Subject<Unit> OnChanged { get; } = new();
        public readonly Subject<uint> OnLevelChanged = new();
        public readonly Subject<ulong> OnExpChanged = new();
        public readonly Subject<string> OnNicknameChanged = new();
        public readonly Subject<string> OnRepresentativeCharacterChanged = new();

        /// <summary>
        /// 데이터 초기화
        /// </summary>
        public void Reset()
        {
            _playerId = string.Empty;
            _nickname = string.Empty;
            _serverId = 0;
            _representativeCharacterId = string.Empty;
            _lastAccessedAt = 0;
            _cachedLevel = 0;

            _inventorySubscription?.Dispose();
            _inventorySubscription = null;

            OnChanged.OnNext(Unit.Default);
        }

        /// <summary>
        /// CustomPlayerData로부터 데이터 설정
        /// </summary>
        public void SetPlayerData(CustomPlayerData data)
        {
            if (data == null)
            {
                Debug.LogWarning("[PlayerDataModel] CustomPlayerData is null");
                return;
            }

            var nicknameChanged = _nickname != data.Nickname;
            var representativeCharacterChanged = _representativeCharacterId != data.RepresentativeCharacterId;

            _playerId = data.PlayerId;
            _nickname = data.Nickname;
            _serverId = data.ServerId;
            _representativeCharacterId = data.RepresentativeCharacterId;
            _lastAccessedAt = data.LastAccessedAt;

            // InventoryModel 구독 시작 (Level/Exp 변경 감지)
            EnsureSubscribed();

            // 초기 레벨 캐시
            _cachedLevel = Level;

            // 변경 이벤트 발생
            if (nicknameChanged)
                OnNicknameChanged.OnNext(_nickname);

            if (representativeCharacterChanged)
                OnRepresentativeCharacterChanged.OnNext(_representativeCharacterId);

            OnChanged.OnNext(Unit.Default);
        }

        /// <summary>
        /// 닉네임 변경
        /// </summary>
        public void ChangeNickName(string nickname)
        {
            if (_nickname == nickname)
                return;

            _nickname = nickname;
            OnNicknameChanged.OnNext(_nickname);
            OnChanged.OnNext(Unit.Default);
        }

        /// <summary>
        /// InventoryModel 구독
        /// </summary>
        private void EnsureSubscribed()
        {
            if (_inventorySubscription != null)
                return;

            _inventorySubscription = ServerDataManager.Instance.Inventory.OnCurrencyChanged
                .Subscribe(this, (data, self) =>
                {
                    // UserExp 변경
                    if (data.itemId == (uint)IdMap.Item.UserExp.Value)
                        self.HandleUserExpChanged();

                    // 조각 변경 시 초월 뱃지 갱신
                    ItemId itemId = (int)data.itemId;
                    if (itemId.IsCharacterPiece())
                        self.RefreshTranscendenceBadge();
                });

            RefreshTranscendenceBadge();
        }

        private void HandleUserExpChanged()
        {
            OnExpChanged.OnNext(Exp);

            var newLevel = Level;
            if (_cachedLevel != newLevel)
            {
                _cachedLevel = newLevel;
                OnLevelChanged.OnNext(newLevel);
            }

            OnChanged.OnNext(Unit.Default);
        }

        #region 뱃지 갱신

        private const string TranscendenceBadgePath = "CharacterInfo/Transcendence";
        private readonly List<CharacterData> _reUseableCharacterList = new();

        private void RefreshTranscendenceBadge()
        {
            var characterModel = ServerDataManager.Instance.Character;
            var inventoryModel = ServerDataManager.Instance.Inventory;

            characterModel.GetAllCharacters(_reUseableCharacterList);

            foreach (var character in _reUseableCharacterList)
            {
                var specCharacter = SpecDataManager.Instance.GetCharacterData((int)character.CharacterId);
                if (specCharacter == null)
                    continue;

                // CharacterDetailGrowLayer.SetTranscendenceLayer 참고
                var transcendLevel = (int)(character.TranscendLevel > 0 ? character.TranscendLevel : 1);
                var transcendData = SpecDataManager.Instance.GetCharacterTranscendenceData(
                    specCharacter.grade_type, transcendLevel);

                // 최대 초월 레벨이면 스킵
                if (transcendData == null || transcendData.piece == 0)
                    continue;

                // 조각 충분한지 확인
                var pieceItemId = ItemIdExtensions.GetCharacterPieceId(specCharacter.id);
                var pieceCount = inventoryModel.GetCurrency((uint)pieceItemId);

                if (pieceCount >= (ulong)transcendData.piece)
                {
                    BadgeManager.Instance.AddBadge(BadgeType.RedDot, TranscendenceBadgePath);
                    return;
                }
            }

            BadgeManager.Instance.RemoveBadge(BadgeType.RedDot, TranscendenceBadgePath);
        }

        #endregion

        /// <summary>
        /// 플레이어 ID
        /// </summary>
        public string PlayerId => _playerId;

        /// <summary>
        /// 닉네임
        /// </summary>
        public string Nickname => _nickname;

        /// <summary>
        /// 서버 ID
        /// </summary>
        public uint ServerId => _serverId;

        /// <summary>
        /// 레벨 (InventoryModel의 UserExp와 AccountLevelExp 스펙 데이터로 계산)
        /// </summary>
        public uint Level
        {
            get
            {
                var totalExp = (long)ServerDataManager.Instance.Inventory.GetCurrency((uint)IdMap.Item.UserExp.Value);
                return (uint)SpecDataManager.Instance.GetAccountLevelByExp(totalExp);
            }
        }

        /// <summary>
        /// 현재 레벨 내 경험치 (현재 레벨 시작 경험치를 뺀 값)
        /// </summary>
        public ulong Exp
        {
            get
            {
                var totalExp = (long)ServerDataManager.Instance.Inventory.GetCurrency((uint)IdMap.Item.UserExp.Value);
                var level = SpecDataManager.Instance.GetAccountLevelByExp(totalExp);
                var levelData = SpecDataManager.Instance.GetAccountLevelExpDataByLevel(level);
                if (levelData == null)
                    return 0;

                return (ulong)(totalExp - levelData.exp_start);
            }
        }

        /// <summary>
        /// 다음 레벨까지 필요한 경험치
        /// </summary>
        public ulong ExpToNextLevel
        {
            get
            {
                var totalExp = (long)ServerDataManager.Instance.Inventory.GetCurrency((uint)IdMap.Item.UserExp.Value);
                var level = SpecDataManager.Instance.GetAccountLevelByExp(totalExp);
                var levelData = SpecDataManager.Instance.GetAccountLevelExpDataByLevel(level);
                if (levelData == null)
                    return 0;

                return (ulong)levelData.exp_need;
            }
        }

        /// <summary>
        /// 대표 캐릭터 ID
        /// </summary>
        public string RepresentativeCharacterId => _representativeCharacterId;

        /// <summary>
        /// 마지막 접속 시간
        /// </summary>
        public ulong LastAccessedAt => _lastAccessedAt;

        /// <summary>
        /// 현재 레벨의 경험치 진행률 (0.0 ~ 1.0)
        /// </summary>
        public float ExpProgress
        {
            get
            {
                var expToNext = ExpToNextLevel;
                if (expToNext == 0)
                    return 0f;

                return Exp / (float)expToNext;
            }
        }

        /// <summary>
        /// 대표 캐릭터가 설정되어 있는지 여부
        /// </summary>
        public bool HasRepresentativeCharacter => !string.IsNullOrEmpty(_representativeCharacterId);
    }
}
