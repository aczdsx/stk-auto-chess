using CookApps.NetLite.Constants;
using CookApps.NetLite.Initialize;
using CookApps.NetLite.Manager;
using UnityEngine;

namespace CookApps.AutoBattler
{
    /// 네트워크 매니저
    public class NetManager : NetLiteManagerBase
    {
        private static NetManager _instance;
        public static NetManager Instance => _instance ??= new NetManager();

        // 커스텀 서비스를 속성으로 추가
        public CharacterService Character { get; private set; }
        public BattleService Battle { get; private set; }
        public EquipmentService Equipment { get; private set; }
        public ElpisService Elpis { get; private set; }
        public PlayerInventoryService Inventory { get; private set; }
        public PostService Post { get; private set; }
        public AnnouncementService Announcement { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ReloadDomain()
        {
            _instance = null;
        }

        public void Startup()
        {
#if SERVER_REAL
            var serverAddress = "https://gwbm013-grpc.cookappsgames.com:443";
#else
            var serverAddress = "https://gwbm013-grpc.dev.cookappsgames.com:443";
#endif
#if UNITY_IOS
            var store = StoreMap.AppleAppStore;
#else
            var store = StoreMap.GooglePlay;
#endif

            var param = new NetLiteInitializeParam
            {
                Address = serverAddress,
                Store = store,
                EnabledLog = true,
            };
            // 앱의 파라메타 및 환경에 맞게 수정 필요
            Startup(param);
        }
    }
}
