using R3;
using Tech.Hive.V1;

namespace CookApps.AutoBattler
{
    /// <summary>
    /// 플레이어 데이터 모델
    /// 플레이어의 기본 정보(레벨, 경험치, 닉네임 등)를 관리
    /// </summary>
    public class PlayerDataModel
    {
        // 플레이어 기본 정보
        private string _playerId = string.Empty;
        private string _nickname = string.Empty;
        private uint _serverId;
        private uint _level;
        private ulong _exp;
        private ulong _expToNextLevel;
        private string _representativeCharacterId = string.Empty;
        private ulong _lastAccessedAt;
        private uint _vipLevel;
        private uint _vipExp;

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
            _level = 0;
            _exp = 0;
            _expToNextLevel = 0;
            _representativeCharacterId = string.Empty;
            _lastAccessedAt = 0;
            _vipLevel = 0;
            _vipExp = 0;

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

            var levelChanged = _level != data.Level;
            var expChanged = _exp != data.Exp;
            var nicknameChanged = _nickname != data.Nickname;
            var representativeCharacterChanged = _representativeCharacterId != data.RepresentativeCharacterId;

            _playerId = data.PlayerId;
            _nickname = data.Nickname;
            _serverId = data.ServerId;
            _level = data.Level;
            _exp = data.Exp;
            _expToNextLevel = data.ExpToNextLevel;
            _representativeCharacterId = data.RepresentativeCharacterId;
            _lastAccessedAt = data.LastAccessedAt;
            _vipLevel = data.VipLevel;
            _vipExp = data.VipExp;

            // 변경 이벤트 발생
            if (levelChanged)
                OnLevelChanged.OnNext(_level);

            if (expChanged)
                OnExpChanged.OnNext(_exp);

            if (nicknameChanged)
                OnNicknameChanged.OnNext(_nickname);

            if (representativeCharacterChanged)
                OnRepresentativeCharacterChanged.OnNext(_representativeCharacterId);

            OnChanged.OnNext(Unit.Default);
        }

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
        /// 레벨
        /// </summary>
        public uint Level => _level;

        /// <summary>
        /// 경험치
        /// </summary>
        public ulong Exp => _exp;

        /// <summary>
        /// 다음 레벨까지 필요한 경험치
        /// </summary>
        public ulong ExpToNextLevel => _expToNextLevel;

        /// <summary>
        /// 대표 캐릭터 ID
        /// </summary>
        public string RepresentativeCharacterId => _representativeCharacterId;

        /// <summary>
        /// 마지막 접속 시간
        /// </summary>
        public ulong LastAccessedAt => _lastAccessedAt;

        /// <summary>
        /// VIP 레벨
        /// </summary>
        public uint VipLevel => _vipLevel;

        /// <summary>
        /// VIP 경험치
        /// </summary>
        public uint VipExp => _vipExp;

        /// <summary>
        /// 현재 레벨의 경험치 진행률 (0.0 ~ 1.0)
        /// </summary>
        public float ExpProgress
        {
            get
            {
                if (_expToNextLevel == 0)
                    return 0f;

                return (float)_exp / (float)_expToNextLevel;
            }
        }

        /// <summary>
        /// 대표 캐릭터가 설정되어 있는지 여부
        /// </summary>
        public bool HasRepresentativeCharacter => !string.IsNullOrEmpty(_representativeCharacterId);
    }
}
