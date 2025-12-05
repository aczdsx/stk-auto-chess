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
        
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ReloadDomain()
        {
            _instance = null;
        }

        public void Startup()
        {
            // 앱의 파라메타 및 환경에 맞게 수정 필요
            Startup(new NetLiteInitializeParam
            {
                Address = "https://hive-grpc.dev.cookappsgames.com:443",
                Store = StoreMap.GooglePlay,
                EnabledLog = true,
            });
        }
    }
}
