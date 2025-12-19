using System;
using System.Collections.Generic;
using Tech.Hive.V1;
using UnityEngine;

namespace CookApps.AutoBattler
{
    /// <summary>
    /// 전투 데이터 모델
    /// 챕터 및 스테이지 진행 정보 관리
    /// 델타 업데이트 지원
    /// </summary>
    public class BattleModel : IDataModel
    {
        public const string CATEGORY_KEY = "battle";

        // 현재 챕터 정보
        private BattleChapterData _currentChapter;

        // 챕터 목록 (ChapterId -> ChapterData)
        private readonly Dictionary<uint, BattleChapterData> _chapters;

        // 스테이지 진행 정보 (StageId -> StageProgress)
        private readonly Dictionary<string, BattleStageProgress> _stageProgresses;

        // 버전 정보
        private int _version;

        public string CategoryKey => CATEGORY_KEY;
        public int Version => _version;

        // 이벤트
        public event Action OnChanged;
        public event Action<BattleChapterData> OnCurrentChapterChanged;
        public event Action<BattleChapterData> OnChapterUpdated;
        public event Action<BattleStageProgress> OnStageProgressUpdated;

        public BattleModel()
        {
            _chapters = new Dictionary<uint, BattleChapterData>(32);
            _stageProgresses = new Dictionary<string, BattleStageProgress>(128);
            _version = 0;
        }

        /// <summary>
        /// 델타 업데이트 적용
        /// </summary>
        public void ApplyDelta(IDataModel delta)
        {
            if (delta is not BattleModel battleDelta)
            {
                Debug.LogError("[BattleModel] Invalid delta type");
                return;
            }

            // 현재 챕터 변경
            if (battleDelta._currentChapter != null)
            {
                _currentChapter = battleDelta._currentChapter;
                OnCurrentChapterChanged?.Invoke(_currentChapter);
            }

            // 챕터 업데이트
            foreach (var kvp in battleDelta._chapters)
            {
                _chapters[kvp.Key] = kvp.Value;
                OnChapterUpdated?.Invoke(kvp.Value);
            }

            // 스테이지 진행 정보 업데이트
            foreach (var kvp in battleDelta._stageProgresses)
            {
                _stageProgresses[kvp.Key] = kvp.Value;
                OnStageProgressUpdated?.Invoke(kvp.Value);
            }

            _version = battleDelta._version;
            OnChanged?.Invoke();
        }

        /// <summary>
        /// 데이터 초기화
        /// </summary>
        public void Reset()
        {
            _currentChapter = null;
            _chapters.Clear();
            _stageProgresses.Clear();
            _version = 0;
            OnChanged?.Invoke();
        }

        /// <summary>
        /// 유효성 검증
        /// </summary>
        public bool Validate()
        {
            // 스테이지 진행 정보 검증
            foreach (var progress in _stageProgresses.Values)
            {
                if (string.IsNullOrEmpty(progress.StageId))
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
            return _chapters.TryGetValue(chapterId, out var chapter) ? chapter : null;
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
        public BattleStageProgress GetStageProgress(string stageId)
        {
            return _stageProgresses.TryGetValue(stageId, out var progress) ? progress : null;
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
        public bool IsStageCleared(string stageId)
        {
            var progress = GetStageProgress(stageId);
            return progress?.IsCleared ?? false;
        }

        /// <summary>
        /// 스테이지 최고 별 개수
        /// </summary>
        public uint GetStageBestStars(string stageId)
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
        /// 현재 챕터 설정 (내부용)
        /// </summary>
        internal void SetCurrentChapter(BattleChapterData chapter, IEnumerable<BattleStageProgress> stages)
        {
            _currentChapter = chapter;

            if (chapter != null)
            {
                _chapters[chapter.ChapterId] = chapter;
            }

            if (stages != null)
            {
                foreach (var stage in stages)
                {
                    if (!string.IsNullOrEmpty(stage.StageId))
                    {
                        _stageProgresses[stage.StageId] = stage;
                    }
                }
            }

            _version++;
            OnCurrentChapterChanged?.Invoke(chapter);
            OnChanged?.Invoke();
        }

        /// <summary>
        /// 챕터 목록 설정 (내부용)
        /// </summary>
        internal void SetChapters(IEnumerable<BattleChapterData> chapters, int version)
        {
            _chapters.Clear();

            if (chapters != null)
            {
                foreach (var chapter in chapters)
                {
                    _chapters[chapter.ChapterId] = chapter;
                }
            }

            _version = version;
            OnChanged?.Invoke();
        }

        /// <summary>
        /// 스테이지 목록 설정 (내부용)
        /// </summary>
        internal void SetStages(IEnumerable<BattleStageProgress> stages)
        {
            if (stages != null)
            {
                foreach (var stage in stages)
                {
                    if (!string.IsNullOrEmpty(stage.StageId))
                    {
                        _stageProgresses[stage.StageId] = stage;
                    }
                }
            }

            _version++;
            OnChanged?.Invoke();
        }

        /// <summary>
        /// 스테이지 진행 정보 업데이트 (내부용)
        /// </summary>
        internal void UpdateStageProgress(BattleStageProgress progress)
        {
            if (progress == null || string.IsNullOrEmpty(progress.StageId))
            {
                Debug.LogError("[BattleModel] Invalid stage progress data");
                return;
            }

            _stageProgresses[progress.StageId] = progress;
            OnStageProgressUpdated?.Invoke(progress);
            _version++;
        }

        /// <summary>
        /// 챕터 업데이트 (내부용)
        /// </summary>
        internal void UpdateChapter(BattleChapterData chapter)
        {
            if (chapter == null)
            {
                Debug.LogError("[BattleModel] Invalid chapter data");
                return;
            }

            _chapters[chapter.ChapterId] = chapter;
            OnChapterUpdated?.Invoke(chapter);
            _version++;
        }

        #endregion
    }
}
