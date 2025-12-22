using System;
using System.Collections.Generic;
using R3;
using Tech.Hive.V1;
using UnityEngine.Pool;

namespace CookApps.AutoBattler
{
    /// <summary>
    /// 전투 데이터 브릿지
    /// ServerDataManager와 UI 사이의 중간 레이어
    /// UI가 직접 데이터 모델을 접근하지 않고 브릿지를 통해 접근
    /// </summary>
    public class BattleDataBridge : DataBridgeBase<BattleModel>
    {
        // R3 이벤트
        public readonly Subject<Unit> OnBattleDataChanged = new();
        public readonly Subject<BattleChapterData> OnCurrentChapterChanged = new();
        public readonly Subject<BattleChapterData> OnChapterUpdated = new();
        public readonly Subject<BattleStageProgress> OnStageProgressUpdated = new();

        public BattleDataBridge()
            : base(ServerDataManager.Instance.Battle, BattleModel.CATEGORY_KEY)
        {
        }

        /// <summary>
        /// 모델 이벤트 구독
        /// </summary>
        protected override void SubscribeModelEvents()
        {
            Model.OnCurrentChapterChanged.Subscribe(this, (chapter, self) =>
            {
                self.OnCurrentChapterChanged.OnNext(chapter);
                self.OnBattleDataChanged.OnNext(Unit.Default);
            }).AddTo(ref disposableBag);

            Model.OnChapterUpdated.Subscribe(this, (chapter, self) => self.OnChapterUpdated.OnNext(chapter)).AddTo(ref disposableBag);
            Model.OnStageProgressUpdated.Subscribe(this, (progress, self) => self.OnStageProgressUpdated.OnNext(progress)).AddTo(ref disposableBag);
        }

        /// <summary>
        /// 모델 변경 감지 (전체 갱신)
        /// </summary>
        protected override void OnModelChanged()
        {
            OnBattleDataChanged.OnNext(Unit.Default);
        }

        #region 현재 챕터 관련

        /// <summary>
        /// 현재 챕터
        /// </summary>
        public BattleChapterData CurrentChapter => Model?.CurrentChapter;

        /// <summary>
        /// 현재 챕터 ID
        /// </summary>
        public uint CurrentChapterId => Model?.CurrentChapterId ?? 0;

        #endregion

        #region 챕터 관련

        /// <summary>
        /// 챕터 가져오기
        /// </summary>
        public BattleChapterData GetChapter(uint chapterId)
        {
            return Model?.GetChapter(chapterId);
        }

        /// <summary>
        /// 모든 챕터 가져오기
        /// </summary>
        public void GetAllChapters(List<BattleChapterData> output)
        {
            Model?.GetAllChapters(output);
        }

        /// <summary>
        /// 챕터 개수
        /// </summary>
        public int ChapterCount => Model?.ChapterCount ?? 0;

        /// <summary>
        /// 전체 별 개수 (모든 챕터 합계)
        /// </summary>
        public uint TotalStarCount => Model?.TotalStarCount ?? 0;

        #endregion

        #region 스테이지 진행 정보 관련

        /// <summary>
        /// 스테이지 진행 정보 가져오기
        /// </summary>
        public BattleStageProgress GetStageProgress(string stageId)
        {
            return Model?.GetStageProgress(stageId);
        }

        /// <summary>
        /// 모든 스테이지 진행 정보 가져오기
        /// </summary>
        public void GetAllStageProgresses(List<BattleStageProgress> output)
        {
            Model?.GetAllStageProgresses(output);
        }

        /// <summary>
        /// 스테이지 진행 정보 개수
        /// </summary>
        public int StageProgressCount => Model?.StageProgressCount ?? 0;

        /// <summary>
        /// 스테이지 클리어 여부
        /// </summary>
        public bool IsStageCleared(string stageId)
        {
            return Model?.IsStageCleared(stageId) ?? false;
        }

        /// <summary>
        /// 스테이지 최고 별 개수
        /// </summary>
        public uint GetStageBestStars(string stageId)
        {
            return Model?.GetStageBestStars(stageId) ?? 0;
        }

        /// <summary>
        /// 3성 클리어한 스테이지들 가져오기
        /// </summary>
        public void GetThreeStarStages(List<BattleStageProgress> output)
        {
            Model?.GetThreeStarStages(output);
        }

        /// <summary>
        /// 클리어한 스테이지 개수
        /// </summary>
        public int ClearedStageCount => Model?.ClearedStageCount ?? 0;

        /// <summary>
        /// 스테이지 3성 클리어 여부
        /// </summary>
        public bool IsStageThreeStarCleared(string stageId)
        {
            return GetStageBestStars(stageId) >= 3;
        }

        /// <summary>
        /// 첫 클리어 보상 수령 여부
        /// </summary>
        public bool IsFirstClearRewarded(string stageId)
        {
            var progress = GetStageProgress(stageId);
            return progress?.IsFirstClearRewarded ?? false;
        }

        /// <summary>
        /// 3성 보상 수령 여부
        /// </summary>
        public bool IsThreeStarRewarded(string stageId)
        {
            var progress = GetStageProgress(stageId);
            return progress?.IsThreeStarRewarded ?? false;
        }

        /// <summary>
        /// 특정 챕터의 클리어한 스테이지 개수
        /// </summary>
        public int GetChapterClearedStageCount(uint chapterId)
        {
            if (Model == null) return 0;

            using var _ = ListPool<BattleStageProgress>.Get(out var allStages);
            Model.GetAllStageProgresses(allStages);

            int count = 0;
            for (int i = 0; i < allStages.Count; i++)
            {
                var stage = allStages[i];
                // StageId 형식: "chapterId-stageNumber" 예: "1-1", "1-2"
                if (stage.IsCleared && stage.StageId.StartsWith($"{chapterId}-"))
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>
        /// 특정 챕터의 3성 클리어한 스테이지 개수
        /// </summary>
        public int GetChapterThreeStarStageCount(uint chapterId)
        {
            if (Model == null) return 0;

            using var _ = ListPool<BattleStageProgress>.Get(out var allStages);
            Model.GetAllStageProgresses(allStages);

            int count = 0;
            for (int i = 0; i < allStages.Count; i++)
            {
                var stage = allStages[i];
                if (stage.BestStars >= 3 && stage.StageId.StartsWith($"{chapterId}-"))
                {
                    count++;
                }
            }

            return count;
        }

        #endregion
    }
}
