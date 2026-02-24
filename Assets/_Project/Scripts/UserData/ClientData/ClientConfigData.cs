using MemoryPack;

namespace CookApps.AutoBattler
{
    [MemoryPackable]
    public partial class ClientBasicData : ClientDataBase
    {
        public const string CategoryName = "client_basic";
        public override string Category => CategoryName;

        public static ClientBasicData Get() => ClientDataManager.Instance.GetData<ClientBasicData>(CategoryName);

        [MemoryPackOrder(0)] private int _totalPlayTime;
        [MemoryPackOrder(1)] private int _dailyVisitCount;
        [MemoryPackOrder(2)] private long _dailyVisitCountTimestamp;
        [MemoryPackOrder(3)] private long _userInstallDate;
        [MemoryPackOrder(4)] private int _maxSquadCount;
        [MemoryPackOrder(5)] private int _resetCharacterCount;
        [MemoryPackOrder(6)] private long _resetCharacterCountTimestamp;
        [MemoryPackOrder(7)] private long _lastLoginTimestamp;

        public int TotalPlayTime => _totalPlayTime;
        public int DailyVisitCount => _dailyVisitCount;
        public long DailyVisitCountTimestamp => _dailyVisitCountTimestamp;
        public long UserInstallDate => _userInstallDate;
        public int MaxSquadCount => _maxSquadCount;
        public int ResetCharacterCount => _resetCharacterCount;
        public long ResetCharacterCountTimestamp => _resetCharacterCountTimestamp;
        public long LastLoginTimestamp => _lastLoginTimestamp;

        public void AddTotalPlayTime(int minutes)
        {
            _totalPlayTime += minutes;
            SetDirty();
        }

        public void SetUserInstallDate(long timestamp)
        {
            if (_userInstallDate == 0)
            {
                _userInstallDate = timestamp;
                SetDirty();
            }
        }

        public void UpdateDailyVisitCount()
        {
            long now = TimeManager.Instance.UtcNowTimeStampLocal();
            if (_dailyVisitCountTimestamp <= now)
            {
                _dailyVisitCount++;
                _dailyVisitCountTimestamp = TimeManager.Instance.TommorrowTimeStampLocal();
                SetDirty();
            }
        }

        public void SetMaxSquadCount(int count)
        {
            if (_maxSquadCount < count)
            {
                _maxSquadCount = count;
                SetDirty();
            }
        }

        public void IncrementResetCharacterCount()
        {
            long now = TimeManager.Instance.UtcNowTimeStampLocal();
            if (_resetCharacterCountTimestamp <= now)
            {
                _resetCharacterCount = 0;
                _resetCharacterCountTimestamp = TimeManager.Instance.TommorrowTimeStampLocal();
            }
            _resetCharacterCount++;
            SetDirty();
        }

        public int GetTodayResetCharacterCount()
        {
            long now = TimeManager.Instance.UtcNowTimeStampLocal();
            if (_resetCharacterCountTimestamp <= now)
            {
                return 0;
            }
            return _resetCharacterCount;
        }

        public void RefreshLastLoginTimestamp()
        {
            _lastLoginTimestamp = TimeManager.Instance.UtcNowTimeStampLocal();
            SetDirty();
        }

        [MemoryPackOnDeserialized]
        private void OnDeserialized()
        {
            if (_maxSquadCount == 0)
            {
                _maxSquadCount = SpecDataManager.Instance.GetGameConfig<int>("default_max_squad_count");
            }
        }
    }
}
