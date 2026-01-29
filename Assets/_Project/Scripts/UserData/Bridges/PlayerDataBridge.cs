using R3;

namespace CookApps.AutoBattler
{
    /// <summary>
    /// 플레이어 데이터 브릿지
    /// ServerDataManager와 UI 사이의 중간 레이어
    /// </summary>
    public class PlayerDataBridge : DataBridgeBase
    {
        private PlayerDataModel Model;

        // Public Observable 노출
        public Observable<Unit> OnChanged;
        public Observable<uint> OnLevelChanged;
        public Observable<ulong> OnExpChanged;
        public Observable<string> OnNicknameChanged;
        public Observable<string> OnRepresentativeCharacterChanged;

        public PlayerDataBridge()
        {
            Model = ServerDataManager.Instance.PlayerData;
            OnChanged = Model.OnChanged;
            OnLevelChanged = Model.OnLevelChanged;
            OnExpChanged = Model.OnExpChanged;
            OnNicknameChanged = Model.OnNicknameChanged;
            OnRepresentativeCharacterChanged = Model.OnRepresentativeCharacterChanged;
        }

        /// <summary>
        /// 플레이어 ID
        /// </summary>
        public string PlayerId => Model?.PlayerId ?? string.Empty;

        /// <summary>
        /// 닉네임
        /// </summary>
        public string Nickname => Model?.Nickname ?? string.Empty;

        /// <summary>
        /// 서버 ID
        /// </summary>
        public uint ServerId => Model?.ServerId ?? 0;

        /// <summary>
        /// 레벨
        /// </summary>
        public uint Level => Model?.Level ?? 0;

        /// <summary>
        /// 경험치
        /// </summary>
        public ulong Exp => Model?.Exp ?? 0;

        /// <summary>
        /// 다음 레벨까지 필요한 경험치
        /// </summary>
        public ulong ExpToNextLevel => Model?.ExpToNextLevel ?? 0;

        /// <summary>
        /// 대표 캐릭터 ID
        /// </summary>
        public string RepresentativeCharacterId => Model?.RepresentativeCharacterId ?? string.Empty;

        /// <summary>
        /// 마지막 접속 시간
        /// </summary>
        public ulong LastAccessedAt => Model?.LastAccessedAt ?? 0;

        /// <summary>
        /// 현재 레벨의 경험치 진행률 (0.0 ~ 1.0)
        /// </summary>
        public float ExpProgress => Model?.ExpProgress ?? 0f;

        /// <summary>
        /// 대표 캐릭터가 설정되어 있는지 여부
        /// </summary>
        public bool HasRepresentativeCharacter => Model?.HasRepresentativeCharacter ?? false;
    }
}
