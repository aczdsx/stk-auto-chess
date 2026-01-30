using System.Collections.Generic;
using System.Linq;
using CookApps.TeamBattle;
using CookApps.TeamBattle.UIManagements;

namespace CookApps.AutoBattler
{
    /// <summary>
    /// 나니노벨 트리거 매니저
    /// 조건에 따라 실행할 나니노벨 스크립트를 검색하고 실행 이력을 관리
    /// </summary>
    public class NaninovelTriggerManager : Singleton<NaninovelTriggerManager>
    {
        // 실행된 트리거 ID 목록 (is_once 처리용 - 추후 UserData 연동)
        private HashSet<int> _executedTriggerIds = new();

        /// <summary>
        /// 초기화 - SceneLoading 델리게이트 연동
        /// </summary>
        public void Initialize()
        {
            SceneLoading.OnGetNaninovelTrigger = GetTriggerOnSceneEnter;
            SceneLoading.OnGetSpecialNaninovelTrigger = GetSpecialTrigger;
            SceneLoading.OnGetStageClearNaninovelTrigger = GetTriggerOnStageClear;
            SceneLoading.OnGetStageEnterNaninovelTrigger = GetTriggerOnStageEnter;
            Debug.Log("[NaninovelTriggerManager] 초기화 완료 - SceneLoading 연동");
        }

        /// <summary>
        /// 씬 진입 시 트리거 검색
        /// </summary>
        /// <param name="sceneName">진입하는 씬 이름</param>
        /// <returns>실행할 나니노벨 스크립트 이름 (없으면 null)</returns>
        public string GetTriggerOnSceneEnter(string sceneName)
        {
            return GetTrigger(NaninovelTriggerType.SCENE_ENTER, sceneName);
        }

        /// <summary>
        /// 나니노벨 스크립트 종료 시 다음 트리거 검색
        /// </summary>
        /// <param name="endedScriptName">종료된 스크립트 이름</param>
        /// <returns>다음에 실행할 나니노벨 스크립트 이름 (없으면 null)</returns>
        public string GetTriggerOnNaninovelEnd(string endedScriptName)
        {
            return GetTrigger(NaninovelTriggerType.NANINOVEL_END, endedScriptName);
        }

        /// <summary>
        /// 스테이지 클리어 시 트리거 검색
        /// </summary>
        /// <param name="stageId">클리어한 스테이지 ID</param>
        /// <returns>실행할 나니노벨 스크립트 이름 (없으면 null)</returns>
        public string GetTriggerOnStageClear(int stageId)
        {
            return GetTrigger(NaninovelTriggerType.STAGE_CLEAR_NANI, stageId.ToString());
        }

        /// <summary>
        /// 스테이지 진입 시 트리거 검색
        /// </summary>
        /// <param name="stageId">진입할 스테이지 ID</param>
        /// <returns>실행할 나니노벨 스크립트 이름 (없으면 null)</returns>
        public string GetTriggerOnStageEnter(int stageId)
        {
            return GetTrigger(NaninovelTriggerType.STAGE_ENTER_NANI, stageId.ToString());
        }

        /// <summary>
        /// 챕터 클리어 시 트리거 검색
        /// </summary>
        /// <param name="chapterId">클리어한 챕터 ID</param>
        /// <returns>실행할 나니노벨 스크립트 이름 (없으면 null)</returns>
        public string GetTriggerOnChapterClear(int chapterId)
        {
            return GetTrigger(NaninovelTriggerType.CHAPTER_CLEAR, chapterId.ToString());
        }

        /// <summary>
        /// 가이드 미션 완료 시 트리거 검색
        /// </summary>
        /// <param name="missionId">완료한 가이드 미션 ID</param>
        /// <returns>실행할 나니노벨 스크립트 이름 (없으면 null)</returns>
        public string GetTriggerOnGuideComplete(int missionId)
        {
            return GetTrigger(NaninovelTriggerType.GUIDE_TUTORIAL_COMPLETE, missionId.ToString());
        }

        /// <summary>
        /// SPECIAL 타입 트리거 검색 (일회성 특수 트리거)
        /// </summary>
        /// <param name="triggerKey">trigger_key (예: PrologueStart, PrologueEnd)</param>
        /// <returns>실행할 나니노벨 스크립트 이름 (없으면 null)</returns>
        public string GetSpecialTrigger(string triggerKey)
        {
            return GetTrigger(NaninovelTriggerType.SPECIAL, triggerKey);
        }

        /// <summary>
        /// 트리거 조건에 맞는 나니노벨 스크립트 검색
        /// </summary>
        private string GetTrigger(NaninovelTriggerType triggerType, string triggerKey)
        {
            if (SpecDataManager.Instance.NaninovelData.All == null || SpecDataManager.Instance.NaninovelData.All.Count == 0)
            {
                return null;
            }

            // 현재 가이드 미션 ID
            var currentGuideMissionId = (int)ServerDataManager.Instance.GuideMission.GuideMissionId;

            var trigger = SpecDataManager.Instance.NaninovelData.All
                .Where(t => t.naninovel_trigger_type == triggerType && t.trigger_key == triggerKey) // 이미 실행된 트리거 제외
                // .Where(t => t.guide_mission_id == 0 || t.guide_mission_id == currentGuideMissionId) // 가이드 미션 조건 확인
                .FirstOrDefault(t => !_executedTriggerIds.Contains(t.id));

            if (trigger != null)
            {
                Debug.Log($"[NaninovelTrigger] 트리거 발동: {trigger.naninovel_name} (type: {triggerType}, key: {triggerKey}, guideMissionId: {trigger.guide_mission_id})");
                return trigger.naninovel_name;
            }

            return null;
        }

        /// <summary>
        /// 트리거 실행 완료 처리 (is_once 용)
        /// </summary>
        /// <param name="scriptName">실행 완료된 스크립트 이름</param>
        public void MarkTriggerExecuted(string scriptName)
        {
            if (SpecDataManager.Instance.NaninovelData.All == null) return;

            var trigger = SpecDataManager.Instance.NaninovelData.All.FirstOrDefault(t => t.naninovel_name == scriptName);
            if (trigger != null)
            {
                _executedTriggerIds.Add(trigger.id);
                Debug.Log($"[NaninovelTrigger] 트리거 실행 완료 기록: {scriptName} (id: {trigger.id})");
            }
        }

        /// <summary>
        /// 실행 이력 초기화 (디버그/테스트용)
        /// </summary>
        public void ClearExecutedHistory()
        {
            _executedTriggerIds.Clear();
            Debug.Log("[NaninovelTrigger] 실행 이력 초기화");
        }

        /// <summary>
        /// 저장된 실행 이력 로드 (추후 UserData 연동)
        /// </summary>
        public void LoadExecutedHistory(IEnumerable<int> executedIds)
        {
            _executedTriggerIds = new HashSet<int>(executedIds);
        }

        /// <summary>
        /// 현재 실행 이력 가져오기 (추후 UserData 저장용)
        /// </summary>
        public IReadOnlyCollection<int> GetExecutedHistory()
        {
            return _executedTriggerIds;
        }
    }
}
