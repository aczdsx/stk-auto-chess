using MemoryPack;

namespace CookApps.AutoBattler
{
    [MemoryPackable]
    public partial class ClientBasicData : ClientDataBase
    {
        public const string CategoryName = "client_basic";
        public override string Category => CategoryName;

        public static ClientBasicData Get() => ClientDataManager.Instance.GetData<ClientBasicData>(CategoryName);

        [MemoryPackOrder(8)] private bool _isSkipTutorial;

        public bool IsSkipTutorial => _isSkipTutorial;

        public void SetSkipTutorial(bool value)
        {
            _isSkipTutorial = value;
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
