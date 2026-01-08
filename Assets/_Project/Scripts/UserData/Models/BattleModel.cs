using System;
using System.Collections.Generic;
using R3;
using Tech.Hive.V1;
using UnityEngine;

namespace CookApps.AutoBattler
{
    /// <summary>
    /// 전투 데이터 모델
    /// 챕터 및 스테이지 진행 정보 관리
    /// 델타 업데이트 지원
    /// </summary>
    public class BattleModel
    {
        // 현재 챕터 정보
        private BattleChapterData _currentChapter;

        // 챕터 목록 (ChapterId -> ChapterData)
        private readonly Dictionary<uint, BattleChapterData> _chapters = new (32);

        // 스테이지 진행 정보 (StageId -> StageProgress)
        private readonly Dictionary<uint, BattleStageProgress> _stageProgresses = new (128);

        // R3 이벤트
        public Subject<Unit> OnChanged { get; } = new();
        public readonly Subject<BattleChapterData> OnCurrentChapterChanged = new();
        public readonly Subject<BattleChapterData> OnChapterUpdated = new();
        public readonly Subject<BattleStageProgress> OnStageProgressUpdated = new();
        /// <summary>
        /// 데이터 초기화
        /// </summary>
        public void Reset()
        {
            _currentChapter = null;
            _chapters.Clear();
            _stageProgresses.Clear();
            OnChanged.OnNext(Unit.Default);
        }

        /// <summary>
        /// 유효성 검증
        /// </summary>
        public bool Validate()
        {
            // 스테이지 진행 정보 검증
            foreach (var progress in _stageProgresses.Values)
            {
                if (progress.StageId == 0)
                {
                    Debug.LogError("[BattleModel] Invalid stage progress: missing StageId");
                    return false;
                }
            }

            return true;
        }

        #region 현재 챕터 관련

        /// <summary>
        /// 현재 챕터
        /// </summary>
        public BattleChapterData CurrentChapter => _currentChapter;

        /// <summary>
        /// 현재 챕터 ID
        /// </summary>
        public uint CurrentChapterId => _currentChapter?.ChapterId ?? 0;

        #endregion

        #region 챕터 관련

        /// <summary>
        /// 챕터 가져오기
        /// </summary>
        public BattleChapterData GetChapter(uint chapterId)
        {
            return _chapters.GetValueOrDefault(chapterId);
        }

        /// <summary>
        /// 모든 챕터 가져오기
        /// </summary>
        public void GetAllChapters(List<BattleChapterData> output)
        {
            if (output == null) return;

            output.Clear();
            output.Capacity = Math.Max(output.Capacity, _chapters.Count);

            foreach (var chapter in _chapters.Values)
            {
                output.Add(chapter);
            }
        }

        /// <summary>
        /// 챕터 개수
        /// </summary>
        public int ChapterCount => _chapters.Count;

        /// <summary>
        /// 전체 별 개수 (모든 챕터 합계)
        /// </summary>
        public uint TotalStarCount
        {
            get
            {
                uint total = 0;
                foreach (var chapter in _chapters.Values)
                {
                    total += chapter.StarTotalCount;
                }
                return total;
            }
        }

        #endregion

        #region 스테이지 진행 정보 관련

        /// <summary>
        /// 스테이지 진행 정보 가져오기
        /// </summary>
        public BattleStageProgress GetStageProgress(uint stageId)
        {
            return _stageProgresses.GetValueOrDefault(stageId);
        }

        /// <summary>
        /// 모든 스테이지 진행 정보 가져오기
        /// </summary>
        public void GetAllStageProgresses(List<BattleStageProgress> output)
        {
            if (output == null) return;

            output.Clear();
            output.Capacity = Math.Max(output.Capacity, _stageProgresses.Count);

            foreach (var progress in _stageProgresses.Values)
            {
                output.Add(progress);
            }
        }

        /// <summary>
        /// 스테이지 진행 정보 개수
        /// </summary>
        public int StageProgressCount => _stageProgresses.Count;

        /// <summary>
        /// 스테이지 클리어 여부
        /// </summary>
        public bool IsStageCleared(uint stageId)
        {
            var progress = GetStageProgress(stageId);
            return progress?.IsCleared ?? false;
        }

        /// <summary>
        /// 스테이지 최고 별 개수
        /// </summary>
        public uint GetStageBestStars(uint stageId)
        {
            var progress = GetStageProgress(stageId);
            return progress?.BestStars ?? 0;
        }

        /// <summary>
        /// 3성 클리어한 스테이지들 가져오기
        /// </summary>
        public void GetThreeStarStages(List<BattleStageProgress> output)
        {
            if (output == null) return;

            output.Clear();

            foreach (var progress in _stageProgresses.Values)
            {
                if (progress.BestStars >= 3)
                {
                    output.Add(progress);
                }
            }
        }

        /// <summary>
        /// 클리어한 스테이지 개수
        /// </summary>
        public int ClearedStageCount
        {
            get
            {
                int count = 0;
                foreach (var progress in _stageProgresses.Values)
                {
                    if (progress.IsCleared)
                    {
                        count++;
                    }
                }
                return count;
            }
        }

        #endregion

        #region 내부용 메서드

        /// <summary>
        /// 현재 챕터 설정
        /// </summary>
        internal void SetCurrentChapter(BattleChapterData chapter, IReadOnlyList<BattleStageProgress> stages)
        {
            _currentChapter = chapter;

            if (chapter != null)
            {
                _chapters[chapter.ChapterId] = chapter;
            }

            if (stages != null)
            {
                for (var i = 0; i < stages.Count; i++)
                {
                    var stage = stages[i];
                    if (stage.StageId != 0)
                    {
                        _stageProgresses[stage.StageId] = stage;
                    }
                }
            }

            OnCurrentChapterChanged.OnNext(chapter);
            OnChanged.OnNext(Unit.Default);
        }

        /// <summary>
        /// 챕터 목록 설정
        /// </summary>
        internal void SetChapters(IReadOnlyList<BattleChapterData> chapters)
        {
            _chapters.Clear();

            if (chapters != null)
            {
                for (var i = 0; i < chapters.Count; i++)
                {
                    var chapter = chapters[i];
                    _chapters[chapter.ChapterId] = chapter;
                }
            }

            OnChanged.OnNext(Unit.Default);
        }

        /// <summary>
        /// 스테이지 목록 설정
        /// </summary>
        internal void SetStages(IReadOnlyList<BattleStageProgress> stages)
        {
            if (stages != null)
            {
                for (var i = 0; i < stages.Count; i++)
                {
                    var stage = stages[i];
                    if (stage.StageId != 0)
                    {
                        _stageProgresses[stage.StageId] = stage;
                    }
                }
            }

            OnChanged.OnNext(Unit.Default);
        }

        /// <summary>
        /// 스테이지 진행 정보 업데이트
        /// </summary>
        internal void UpdateStageProgress(BattleStageProgress progress)
        {
            if (progress == null || progress.StageId != 0)
            {
                Debug.LogError("[BattleModel] Invalid stage progress data");
                return;
            }

            _stageProgresses[progress.StageId] = progress;
            OnStageProgressUpdated.OnNext(progress);
        }

        /// <summary>
        /// 챕터 업데이트
        /// </summary>
        internal void UpdateChapter(BattleChapterData chapter)
        {
            if (chapter == null)
            {
                Debug.LogError("[BattleModel] Invalid chapter data");
                return;
            }

            _chapters[chapter.ChapterId] = chapter;
            OnChapterUpdated.OnNext(chapter);
        }

        #endregion
    }
}
