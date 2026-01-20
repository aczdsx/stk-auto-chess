using System.Threading;
using Cysharp.Threading.Tasks;

namespace CookApps.AutoBattler
{
    public partial class NetManager
    {
        /// <summary>
        /// 인증 후 모든 서버 데이터 초기화
        /// TitleMain에서 한 번만 호출
        /// </summary>
        public async UniTask InitializeAsync(CancellationToken cancellationToken = default)
        {
            // 1. 기본 데이터 병렬 로드
            await UniTask.WhenAll(
                CustomLobby.GetMyPlayerDataAsync(cancellationToken),
                Inventory.ListAsync(cancellationToken),
                Character.ListAsync(cancellationToken),
                Deck.ListAsync(cancellationToken),
                Battle.GetCurrentChapterAsync(cancellationToken),
                Battle.ListChapterAsync(cancellationToken),
                Event.ListAsync(cancellationToken),
                Initialize_Elpis(),
                TrialDungeon.GetAsync(cancellationToken),
                GuideMission.GetAsync(cancellationToken)
            );

            // 2. 이벤트 스트림 구독 시작
            StartEventSubscription();
        }
    }
}
