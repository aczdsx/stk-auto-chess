using System;
using CookApps.BattleSystem;

namespace CookApps.AutoBattler
{
    /// <summary>
    /// SKILL_READY 튜토리얼 트리거 처리를 담당하는 핸들러.
    /// 스킬 준비 시 게임 일시정지, 튜토리얼 표시, 완료 후 스킬 발동을 관리합니다.
    /// </summary>
    public static class TutorialSkillReadyHandler
    {
        /// <summary>
        /// SKILL_READY로 인해 게임이 일시 정지되었는지 여부
        /// </summary>
        public static bool IsPausedBySkillReady { get; private set; }

        /// <summary>
        /// 대기 중인 스킬 발동 콜백
        /// </summary>
        private static Action _pendingSkillActivation;

        /// <summary>
        /// CHARACTER_DEAD 대기로 인해 보류된 스킬 정보
        /// </summary>
        private static int _deferredCharacterId;
        private static Action _deferredSkillActivation;
        private static bool _hasDeferredSkillReady;

        /// <summary>
        /// SKILL_READY 튜토리얼을 처리합니다.
        /// </summary>
        /// <param name="characterId">스킬을 사용할 캐릭터 ID</param>
        /// <param name="onSkillActivate">튜토리얼 완료 후 호출될 스킬 발동 콜백</param>
        /// <returns>튜토리얼이 처리되었으면 true, 아니면 false (바로 스킬 발동 필요)</returns>
        public static bool TryHandleTutorial(int characterId, Action onSkillActivate)
        {
            var tutorialManager = TutorialManager.Instance;
            if (tutorialManager == null || !tutorialManager.IsTutorial)
            {
                return false;
            }

            // SKILL_READY 트리거 확인
            if (!tutorialManager.IsTutorialAction(TutorialTriggerType.SKILL_READY))
            {
                return false;
            }

            // CHARACTER_DEAD 튜토리얼이 아직 처리되지 않았으면 스킬 발동 보류 (seq 순서 보장)
            if (tutorialManager.IsTutorialAction(TutorialTriggerType.CHARACTER_DEAD))
            {
                Debug.LogColor($"[TutorialSkillReadyHandler] CHARACTER_DEAD 대기로 스킬 발동 보류 (characterId: {characterId})", "yellow");
                _deferredCharacterId = characterId;
                _deferredSkillActivation = onSkillActivate;
                _hasDeferredSkillReady = true;

                // CHARACTER_DEAD 튜토리얼 닫힘 시 보류된 SKILL_READY 처리 등록
                tutorialManager.OnTutorialClosed += OnTutorialClosedTryDeferred;

                return true;  // 스킬 발동 지연 (튜토리얼 없이)
            }

            return ProcessSkillReadyTutorial(characterId, onSkillActivate);
        }

        /// <summary>
        /// 실제 SKILL_READY 튜토리얼 처리 로직
        /// </summary>
        private static bool ProcessSkillReadyTutorial(int characterId, Action onSkillActivate)
        {
            var tutorialManager = TutorialManager.Instance;
            if (tutorialManager == null || !tutorialManager.IsTutorial)
            {
                return false;
            }

            // 튜토리얼 닫힘 시 Resume 콜백 등록 (one-shot)
            tutorialManager.OnTutorialClosed += ResumeAndActivateSkill;

            // 튜토리얼 표시 시도
            bool handled = tutorialManager.HandleTutorialAction(
                TutorialTriggerType.SKILL_READY,
                characterId.ToString());

            if (!handled)
            {
                tutorialManager.OnTutorialClosed -= ResumeAndActivateSkill;
                return false;
            }

            // 게임 일시정지
            if (InGameMainFlowManager.Instance != null)
            {
                InGameMainFlowManager.Instance.Pause();
                IsPausedBySkillReady = true;
                Debug.LogColor("[TutorialSkillReadyHandler] 게임 일시 정지", "yellow");
            }

            // 스킬 발동 콜백 저장
            _pendingSkillActivation = onSkillActivate;

            return true;
        }

        /// <summary>
        /// 튜토리얼 완료 후 게임 재개 및 대기 중인 스킬 발동
        /// (TutorialManager.HandleTutorialClose에서 호출)
        /// </summary>
        public static void ResumeAndActivateSkill()
        {
            if (!IsPausedBySkillReady)
                return;
            // 게임 재개
            if (IsPausedBySkillReady && InGameMainFlowManager.Instance != null)
            {
                InGameMainFlowManager.Instance.Resume();
                Debug.LogColor("[TutorialSkillReadyHandler] 게임 재개", "yellow");
            }
            IsPausedBySkillReady = false;

            // SPAWN_ENEMY로 스폰된 적들을 Idle 상태로 전환
            TutorialActionSpawnEnemy.ActivateSpawnedEnemies();

            // 대기 중인 스킬 발동
            var skillActivation = _pendingSkillActivation;
            _pendingSkillActivation = null;

            if (skillActivation != null)
            {
                Debug.LogColor("[TutorialSkillReadyHandler] 대기 스킬 발동", "yellow");
                skillActivation.Invoke();
            }
        }

        /// <summary>
        /// OnTutorialClosed 이벤트용 래퍼 (Action 시그니처)
        /// </summary>
        private static void OnTutorialClosedTryDeferred()
        {
            TryProcessDeferredSkillReady();
        }

        /// <summary>
        /// 보류된 SKILL_READY 튜토리얼 처리 시도
        /// (CHARACTER_DEAD 튜토리얼 완료 후 OnTutorialClosed 이벤트로 호출)
        /// </summary>
        /// <returns>보류된 튜토리얼이 처리되었으면 true</returns>
        public static bool TryProcessDeferredSkillReady()
        {
            if (!_hasDeferredSkillReady)
            {
                return false;
            }

            var tutorialManager = TutorialManager.Instance;
            if (tutorialManager == null || !tutorialManager.IsTutorial)
            {
                ClearDeferred();
                return false;
            }

            // CHARACTER_DEAD가 아직 남아있으면 계속 대기
            if (tutorialManager.IsTutorialAction(TutorialTriggerType.CHARACTER_DEAD))
            {
                return false;
            }

            Debug.LogColor($"[TutorialSkillReadyHandler] 보류된 SKILL_READY 처리 시작 (characterId: {_deferredCharacterId})", "yellow");

            int characterId = _deferredCharacterId;
            Action skillActivation = _deferredSkillActivation;
            ClearDeferred();

            return ProcessSkillReadyTutorial(characterId, skillActivation);
        }

        /// <summary>
        /// 보류된 스킬 정보 초기화
        /// </summary>
        private static void ClearDeferred()
        {
            _hasDeferredSkillReady = false;
            _deferredCharacterId = 0;
            _deferredSkillActivation = null;
        }

        /// <summary>
        /// 상태 초기화 (스테이지 종료 시 등)
        /// </summary>
        public static void Clear()
        {
            IsPausedBySkillReady = false;
            _pendingSkillActivation = null;
            ClearDeferred();
        }
    }
}
