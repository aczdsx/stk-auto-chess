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

        // 경험치 변경 시 실시간 레벨 변경 감지용 (항상 현재 레벨과 동일하게 갱신)
        private uint _cachedLevel;

        // 스테이지 진입 시점의 레벨 스냅샷 (전투 후 로비 복귀 시 레벨업 팝업 표시 비교용)
        public uint PrevAccountLevel { get; set; }

        // InventoryModel 구독
        private IDisposable _inventorySubscription;
        private const string TranscendenceBadgePath = "CharacterInfo/Transcendence";
        private const string LevelUpBadgePath = "CharacterInfo/LevelUp";

        public static string GetTranscendenceBadgePath(int characterId)
        {
            return $"{TranscendenceBadgePath}/{characterId}";
        }

        public static string GetLevelUpBadgePath(int characterId)
        {
            return $"{LevelUpBadgePath}/{characterId}";
        }
        
        private readonly List<CharacterData> _reUseableCharacterList = new();
        private HashSet<uint> _levelUpItemIds;

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

            // 초기 레벨 캐시 리셋 (인벤토리 로드 시 HandleUserExpChanged에서 갱신)
            _cachedLevel = 0;
            PrevAccountLevel = 0;

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
                    
                    // 재화 변경 시 레벨업 뱃지 갱신
                    self._levelUpItemIds ??= BuildLevelUpItemIds();
                    if (self._levelUpItemIds.Contains(data.itemId))
                        self.RefreshLevelUpBadge();

                    // 조각 변경 시 초월 뱃지 갱신
                    ItemId itemId = (int)data.itemId;
                    if (itemId.IsCharacterPiece())
                        self.RefreshTranscendenceBadge();

                    
                });

        }

        private void HandleUserExpChanged()
        {
            OnExpChanged.OnNext(Exp);

            var newLevel = Level;
            if (_cachedLevel != newLevel)
            {
                // 최초 인벤토리 로드 시 PrevAccountLevel 동기화
                // (SetPlayerData가 Inventory보다 먼저 완료되면 Level이 0이므로 여기서 보정)
                if (_cachedLevel == 0)
                    PrevAccountLevel = newLevel;

                _cachedLevel = newLevel;
                OnLevelChanged.OnNext(newLevel);
            }

            OnChanged.OnNext(Unit.Default);
        }

        #region 뱃지 갱신

        private static HashSet<uint> BuildLevelUpItemIds()
        {
            var ids = new HashSet<uint>();
            var allData = SpecDataManager.Instance.CharacterLevelExp.All;
            for (var i = 0; i < allData.Count; i++)
            {
                var data = allData[i];
                if (data.need_gold > 0)
                    ids.Add((uint)IdMap.Item.Gold.Value);
                if (data.base_levelup_item_id != 0)
                    ids.Add((uint)data.base_levelup_item_id);
                if (data.sec_levelup_item_id != 0)
                    ids.Add((uint)data.sec_levelup_item_id);
            }
            return ids;
        }


        public void RefreshTranscendenceBadge()
        {
            var characterModel = ServerDataManager.Instance.Character;
            var inventoryModel = ServerDataManager.Instance.Inventory;

            characterModel.GetAllCharacters(_reUseableCharacterList);

            bool anyCanTranscend = false;

            foreach (var character in _reUseableCharacterList)
            {
                var specCharacter = SpecDataManager.Instance.GetCharacterData((int)character.CharacterId);
                if (specCharacter == null)
                    continue;

                // CharacterDetailGrowLayer.SetTranscendenceLayer 참고
                var transcendLevel = (int)(character.TranscendLevel > 0 ? character.TranscendLevel : 1);
                var transcendData = SpecDataManager.Instance.GetCharacterTranscendenceData(
                    specCharacter.grade_type, transcendLevel);

                var perCharPath = GetTranscendenceBadgePath(specCharacter.id);

                // 최대 초월 레벨이면 스킵
                if (transcendData == null || transcendData.piece == 0)
                {
                    BadgeManager.Instance.RemoveBadge(BadgeType.RedDot, perCharPath);
                    continue;
                }

                // 조각 충분한지 확인
                var pieceItemId = ItemIdExtensions.GetCharacterPieceId(specCharacter.id);
                var pieceCount = inventoryModel.GetCurrency((uint)pieceItemId);

                if (pieceCount >= (ulong)transcendData.piece)
                {
                    BadgeManager.Instance.AddBadge(BadgeType.RedDot, perCharPath);
                    anyCanTranscend = true;
                }
                else
                {
                    BadgeManager.Instance.RemoveBadge(BadgeType.RedDot, perCharPath);
                }
            }

            if (anyCanTranscend)
                BadgeManager.Instance.AddBadge(BadgeType.RedDot, TranscendenceBadgePath);
            else
                BadgeManager.Instance.RemoveBadge(BadgeType.RedDot, TranscendenceBadgePath);
        }

        public void RefreshLevelUpBadge()
        {
            var characterModel = ServerDataManager.Instance.Character;
            var inventoryModel = ServerDataManager.Instance.Inventory;

            characterModel.GetAllCharacters(_reUseableCharacterList);

            bool anyCanLevelUp = false;

            foreach (var character in _reUseableCharacterList)
            {
                var specCharacter = SpecDataManager.Instance.GetCharacterData((int)character.CharacterId);
                if (specCharacter == null)
                    continue;

                var perCharPath = GetLevelUpBadgePath(specCharacter.id);

                var exceedLevel = character.ExceedLevel;
                var userLevel = Math.Max(1, (int)character.Level);
                var nextExceedLevelExpData = SpecDataManager.Instance.GetCharacterNextExceedLevelExpData(exceedLevel);
                var levelExpData = SpecDataManager.Instance.GetCharacterLevelExpData(userLevel);

                // exceed 조건이면 exceed 데이터 사용
                if (userLevel >= (nextExceedLevelExpData?.level ?? int.MaxValue))
                    levelExpData = nextExceedLevelExpData;

                if (levelExpData == null)
                {
                    BadgeManager.Instance.RemoveBadge(BadgeType.RedDot, perCharPath);
                    continue;
                }

                var canLevelUp = true;
                if (levelExpData.need_gold > 0 && levelExpData.need_gold > (int)inventoryModel.GetCurrency(IdMap.Item.Gold))
                    canLevelUp = false;
                if (canLevelUp && levelExpData.base_levelup_item_id != 0 && levelExpData.base_levelup_item_count > (int)inventoryModel.GetCurrency(levelExpData.base_levelup_item_id))
                    canLevelUp = false;
                if (canLevelUp && levelExpData.sec_levelup_item_id != 0 && levelExpData.sec_levelup_item_count > (int)inventoryModel.GetCurrency(levelExpData.sec_levelup_item_id))
                    canLevelUp = false;

                if (canLevelUp)
                {
                    BadgeManager.Instance.AddBadge(BadgeType.RedDot, perCharPath);
                    anyCanLevelUp = true;
                }
                else
                {
                    BadgeManager.Instance.RemoveBadge(BadgeType.RedDot, perCharPath);
                }
            }

            if (anyCanLevelUp)
                BadgeManager.Instance.AddBadge(BadgeType.RedDot, LevelUpBadgePath);
            else
                BadgeManager.Instance.RemoveBadge(BadgeType.RedDot, LevelUpBadgePath);
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
