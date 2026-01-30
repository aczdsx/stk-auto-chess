using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Tech.Hive.V1;

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
                CustomLobby.SyncApAsync(cancellationToken),
                Inventory.ListAsync(cancellationToken),
                Character.ListAsync(cancellationToken),
                Deck.ListAsync(cancellationToken),
                Battle.ListChapterAsync(cancellationToken),
                Event.ListAsync(cancellationToken),
                Initialize_Elpis(),
                TrialDungeon.GetAsync(cancellationToken),
                GuideMission.GetAsync(cancellationToken),
                // Commander.ListSkillAsync(cancellationToken),
                ClientData.ListAsync(new []
                {
                    ClientBasicData.CategoryName,
                    ClientShopPurchaseData.CategoryName,
                    ClientProgressData.CategoryName
                }, cancellationToken)
            );

            // 2. 모든 챕터의 스테이지 진행 정보 로드
            await LoadAllChapterStagesAsync(cancellationToken);

            // 3. 이벤트 스트림 구독 시작
            StartEventSubscription();
        }

        /// <summary>
        /// 모든 챕터의 스테이지 진행 정보 병렬 로드
        /// </summary>
        private async UniTask LoadAllChapterStagesAsync(CancellationToken cancellationToken)
        {
            List<BattleChapterData> chapters = new();
            ServerDataManager.Instance.Battle.GetAllChapters(chapters);

            if (chapters.Count == 0) return;

            UniTask[] tasks = new UniTask[chapters.Count];
            for (int i = 0; i < chapters.Count; i++)
            {
                tasks[i] = Battle.ListStageAsync(chapters[i].ChapterId, cancellationToken);
            }

            await UniTask.WhenAll(tasks);
        }
    }
}
