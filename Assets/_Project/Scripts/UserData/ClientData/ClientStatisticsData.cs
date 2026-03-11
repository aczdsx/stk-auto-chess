using MemoryPack;

namespace CookApps.AutoBattler
{
    [MemoryPackable]
    public partial class ClientStatisticsData : ClientDataBase
    {
        public const string CategoryName = "client_statistics";
        public override string Category => CategoryName;

        public static ClientStatisticsData Get() => ClientDataManager.Instance.GetData<ClientStatisticsData>(CategoryName);

        [MemoryPackInclude, MemoryPackOrder(0)] private int _userStageLoseCount;
        [MemoryPackInclude, MemoryPackOrder(1)] private int _userDungeonLoseCount;
        [MemoryPackInclude, MemoryPackOrder(2)] private int _totalGachaCount;

        public int UserStageLoseCount => _userStageLoseCount;
        public int UserDungeonLoseCount => _userDungeonLoseCount;
        public int TotalGachaCount => _totalGachaCount;

        [MemoryPackOnDeserialized]
        private void OnDeserialized()
        {
            Debug.Log($"[ClientStatisticsData] Deserialized - StageLoseCount: {_userStageLoseCount}, DungeonLoseCount: {_userDungeonLoseCount}, GachaCount: {_totalGachaCount}");
        }

        public void IncrementUserStageLoseCount()
        {
            _userStageLoseCount++;
            SetDirty();
        }

        public void IncrementTotalGachaCount()
        {
            _totalGachaCount++;
            SetDirty();
        }
    }
}
