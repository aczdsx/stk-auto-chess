using Cysharp.Threading.Tasks;

namespace CookApps.AutoBattler
{
    public partial class NetManager
    {
        /// <summary>
        /// CustomLobby 초기화
        /// </summary>
        public async UniTask Initialize_CustomLobby()
        {
            // CustomLobby 플레이어 데이터 가져오기
            await CustomLobby.GetMyPlayerDataAsync();
        }
    }
}
