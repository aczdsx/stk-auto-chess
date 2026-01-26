using System.Collections.Generic;
using CookApps.TeamBattle;
using R3;
using Tech.Hive.V1;

namespace CookApps.AutoBattler
{
    /// <summary>
    /// 퀘스트 데이터 모델
    /// 일일/주간 퀘스트 정보를 관리
    /// </summary>
    public class QuestModel
    {
        // 퀘스트 데이터 목록 (key: quest_id)
        private readonly Dictionary<uint, QuestData> _quests = new();

        // 다음 리셋 시간
        private ulong _nextResetAt;

        // 날짜 인덱스 (ex. 20251208)
        private uint _dateIndex;

        // R3 이벤트
        public Subject<Unit> OnChanged { get; } = new();
        public readonly Subject<uint> OnQuestUpdated = new();

        /// <summary>
        /// 다음 리셋 시간
        /// </summary>
        public ulong NextResetAt => _nextResetAt;

        /// <summary>
        /// 날짜 인덱스
        /// </summary>
        public uint DateIndex => _dateIndex;

        /// <summary>
        /// 데이터 초기화
        /// </summary>
        public void Reset()
        {
            _quests.Clear();
            _nextResetAt = 0;
            _dateIndex = 0;
            OnChanged.OnNext(Unit.Default);
        }

        /// <summary>
        /// 퀘스트 목록 설정
        /// </summary>
        internal void SetQuests(IEnumerable<QuestData> quests, ulong nextResetAt, uint dateIndex)
        {
            _quests.Clear();
            _nextResetAt = nextResetAt;
            _dateIndex = dateIndex;

            if (quests != null)
            {
                foreach (var questData in quests)
                {
                    _quests[questData.QuestId] = questData;
                }
            }

            OnChanged.OnNext(Unit.Default);
            RefreshQuestBadge();
        }

        /// <summary>
        /// 단일 퀘스트 데이터 업데이트
        /// </summary>
        internal void UpdateQuest(QuestData questData)
        {
            if (questData == null) return;

            _quests[questData.QuestId] = questData;

            OnQuestUpdated.OnNext(questData.QuestId);
            OnChanged.OnNext(Unit.Default);
            RefreshQuestBadge();
        }

        #region 뱃지 갱신

        /// <summary>
        /// Quest 뱃지 갱신
        /// </summary>
        private void RefreshQuestBadge()
        {
            const string path = "Quest";

            if (HasClaimableQuest())
            {
                BadgeManager.Instance.AddBadge(BadgeType.RedDot, path);
            }
            else
            {
                BadgeManager.Instance.RemoveBadge(BadgeType.RedDot, path);
            }
        }

        /// <summary>
        /// 받을 수 있는 보상이 있는 퀘스트가 있는지 확인
        /// </summary>
        private bool HasClaimableQuest()
        {
            foreach (var quest in _quests.Values)
            {
                if (quest.IsCleared && !quest.IsRewarded)
                {
                    return true;
                }
            }
            return false;
        }

        #endregion

        #region 조회 메서드

        /// <summary>
        /// 퀘스트 데이터 조회
        /// </summary>
        public QuestData GetQuest(uint questId)
        {
            return _quests.TryGetValue(questId, out var data) ? data : null;
        }

        /// <summary>
        /// 퀘스트 데이터 조회 (int)
        /// </summary>
        public QuestData GetQuest(int questId)
        {
            return GetQuest((uint)questId);
        }

        /// <summary>
        /// 모든 퀘스트 데이터 조회
        /// </summary>
        public IEnumerable<QuestData> GetAllQuests()
        {
            return _quests.Values;
        }

        /// <summary>
        /// 퀘스트 존재 여부 확인
        /// </summary>
        public bool HasQuest(uint questId)
        {
            return _quests.ContainsKey(questId);
        }

        /// <summary>
        /// 완료된 퀘스트 수 조회
        /// </summary>
        public int GetCompletedQuestCount()
        {
            int count = 0;
            foreach (var quest in _quests.Values)
            {
                if (quest.IsCleared)
                {
                    count++;
                }
            }
            return count;
        }

        /// <summary>
        /// 보상 수령 가능한 퀘스트 수 조회
        /// </summary>
        public int GetClaimableQuestCount()
        {
            int count = 0;
            foreach (var quest in _quests.Values)
            {
                if (quest.IsCleared && quest.IsRewarded)
                {
                    count++;
                }
            }
            return count;
        }

        #endregion
    }
}
