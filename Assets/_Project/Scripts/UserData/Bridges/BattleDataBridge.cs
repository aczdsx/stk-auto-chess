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
    public class BattleDataBridge
    {
        private BattleModel Model;
        // Public Observable 노출
        public Observable<BattleChapterData> OnCurrentChapterChanged;
        public Observable<BattleChapterData> OnChapterUpdated;
        public Observable<BattleStageProgress> OnStageProgressUpdated;

        public BattleDataBridge()
        {
            Model = ServerDataManager.Instance.Battle;
            OnCurrentChapterChanged = Model.OnCurrentChapterChanged;
            OnChapterUpdated = Model.OnChapterUpdated;
            OnStageProgressUpdated = Model.OnStageProgressUpdated;
        }

        /// <summary>
        /// 현재 챕터
        /// </summary>
        public BattleChapterData CurrentChapter => Model?.CurrentChapter;

        /// <summary>
        /// 현재 챕터 ID
        /// </summary>
        public uint CurrentChapterId => Model?.CurrentChapterId ?? 0;

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

        /// <summary>
        /// 스테이지 진행 정보 가져오기
        /// </summary>
        public BattleStageProgress GetStageProgress(uint stageId)
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
        public bool IsStageCleared(uint stageId)
        {
            return Model?.IsStageCleared(stageId) ?? false;
        }

        /// <summary>
        /// 스테이지 최고 별 개수
        /// </summary>
        public uint GetStageBestStars(uint stageId)
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
        public bool IsStageThreeStarCleared(uint stageId)
        {
            return GetStageBestStars(stageId) >= 3;
        }

        /// <summary>
        /// 첫 클리어 보상 수령 여부
        /// </summary>
        public bool IsFirstClearRewarded(uint stageId)
        {
            var progress = GetStageProgress(stageId);
            return progress?.IsCleared ?? false;
        }

        /// <summary>
        /// 3성 보상 수령 여부
        /// </summary>
        public bool IsThreeStarRewarded(uint stageId)
        {
            var progress = GetStageProgress(stageId);
            return (progress?.BestStars ?? 0) >= 3;
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
                var specStage = SpecDataManager.Instance.GetStageData((int)stage.StageId);
                if (stage.IsCleared && specStage.chapter_id == chapterId)
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
                var specStage = SpecDataManager.Instance.GetStageData((int)stage.StageId);
                if (stage.BestStars >= 3 && specStage.chapter_id == chapterId)
                {
                    count++;
                }
            }

            return count;
        }
    }
}
