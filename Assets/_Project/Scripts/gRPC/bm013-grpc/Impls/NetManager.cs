// #define 재상_로컬
using CookApps.NetLite.Constants;
using CookApps.NetLite.Initialize;
using CookApps.NetLite.Manager;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace CookApps.AutoBattler
{
    /// 네트워크 매니저
    public partial class NetManager : NetLiteManagerBase
    {
        private static NetManager _instance;
        public static NetManager Instance => _instance ??= new NetManager();

        // 커스텀 서비스를 속성으로 추가
        public CharacterService Character { get; private set; }
        public BattleService Battle { get; private set; }
        public ElpisService Elpis { get; private set; }
        public PlayerInventoryService Inventory { get; private set; }
        public PostService Post { get; private set; }
        public AnnouncementService Announcement { get; private set; }
        public CustomLobbyService CustomLobby { get; private set; }
        public CommanderService Commander { get; private set; }
        public DeckService Deck { get; private set; }
        public GuideMissionService GuideMission { get; private set; }
        public ClientDataService ClientData { get; private set; }
        public CheatService Cheat { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ReloadDomain()
        {
            _instance = null;
        }

        public void Startup()
        {
#if __DEV
#if 재상_로컬
            var serverAddress = "http://100.91.148.60:50051";
#else
            var serverAddress = "https://gwbm013-grpc.dev.cookappsgames.com:443";
#endif
#else
            var serverAddress = "https://gwbm013-grpc.cookappsgames.com:443";
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
        
        
        private void InjectServiceInterceptors()
        {
            // Custom services (프로젝트 내에서 정의된 서비스만)
            Character.ServiceInterceptor = this;
            Battle.ServiceInterceptor = this;
            Elpis.ServiceInterceptor = this;
            Inventory.ServiceInterceptor = this;
            Post.ServiceInterceptor = this;
            Announcement.ServiceInterceptor = this;
            CustomLobby.ServiceInterceptor = this;
            Commander.ServiceInterceptor = this;
            Deck.ServiceInterceptor = this;
            GuideMission.ServiceInterceptor = this;
            ClientData.ServiceInterceptor = this;
            Cheat.ServiceInterceptor = this;

            // Base class services는 외부 패키지에 있어서 Source Generator가 적용되지 않을 수 있음
            // Auth.ServiceInterceptor = this;
            // Spec.ServiceInterceptor = this;
            // Lobby.ServiceInterceptor = this;
            // Shop.ServiceInterceptor = this;
        }

    }
}
