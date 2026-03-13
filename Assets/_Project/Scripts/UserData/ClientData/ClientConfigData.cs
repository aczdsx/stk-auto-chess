using MemoryPack;

namespace CookApps.AutoBattler
{
    [MemoryPackable]
    public partial class ClientConfigData : ClientDataBase
    {
        public const string CategoryName = "client_config";
        public override string Category => CategoryName;

        public static ClientConfigData Get() => ClientDataManager.Instance.GetData<ClientConfigData>(CategoryName);

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

        }
    }
}
