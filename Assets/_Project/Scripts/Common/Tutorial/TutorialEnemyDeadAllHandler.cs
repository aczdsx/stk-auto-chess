using System;
using CookApps.BattleSystem;
using UnityEngine;

namespace CookApps.AutoBattler
{
    /// <summary>
    /// ENEMY_DEAD_ALL 튜토리얼 트리거 처리를 담당하는 핸들러.
    /// 적 전멸 시 게임 일시정지, 튜토리얼 표시, 완료 후 전투 종료를 관리합니다.
    /// </summary>
    public static class TutorialEnemyDeadAllHandler
    {
        /// <summary>
        /// ENEMY_DEAD_ALL로 인해 게임이 일시 정지되었는지 여부
        /// </summary>
        public static bool IsPausedByEnemyDeadAll { get; private set; }

        /// <summary>
        /// 대기 중인 전투 종료 콜백
        /// </summary>
        private static Action _pendingCombatEnd;

        /// <summary>
        /// ENEMY_DEAD_ALL 튜토리얼을 처리합니다.
        /// </summary>
        /// <param name="onCombatEnd">튜토리얼 완료 후 호출될 전투 종료 콜백</param>
        /// <returns>튜토리얼이 처리되었으면 true, 아니면 false (바로 전투 종료 필요)</returns>
        public static bool TryHandleTutorial(Action onCombatEnd)
        {
            var tutorialManager = TutorialManager.Instance;
            if (tutorialManager == null || !tutorialManager.IsTutorial)
            {
                return false;
            }

            // ENEMY_DEAD_ALL 트리거 확인
            if (!tutorialManager.IsTutorialAction(TutorialTriggerType.ENEMY_DEAD_ALL))
            {
                return false;
            }

            // 튜토리얼 표시 시도
            bool handled = tutorialManager.HandleTutorialAction(
                TutorialTriggerType.ENEMY_DEAD_ALL,
                "0");

            if (!handled)
            {
                return false;
            }

            // 게임 일시정지
            if (InGameMainFlowManager.Instance != null)
            {
                InGameMainFlowManager.Instance.Pause();
                IsPausedByEnemyDeadAll = true;
                Debug.LogColor("[TutorialEnemyDeadAllHandler] 게임 일시 정지", "yellow");
            }

            // 전투 종료 콜백 저장
            _pendingCombatEnd = onCombatEnd;

            return true;
        }

        /// <summary>
        /// 튜토리얼 완료 후 게임 재개 및 전투 종료 처리
        /// (TutorialManager.HandleTutorialClose에서 호출)
        /// </summary>
        public static void ResumeAndEndCombat()
        {
            // 게임 재개
            if (IsPausedByEnemyDeadAll && InGameMainFlowManager.Instance != null)
            {
                InGameMainFlowManager.Instance.Resume();
                Debug.LogColor("[TutorialEnemyDeadAllHandler] 게임 재개", "yellow");
            }
            IsPausedByEnemyDeadAll = false;

            // 전투 종료 콜백 호출
            var combatEnd = _pendingCombatEnd;
            _pendingCombatEnd = null;

            if (combatEnd != null)
            {
                Debug.LogColor("[TutorialEnemyDeadAllHandler] 전투 종료 처리", "yellow");
                combatEnd.Invoke();
            }
        }

        /// <summary>
        /// 상태 초기화 (스테이지 종료 시 등)
        /// </summary>
        public static void Clear()
        {
            IsPausedByEnemyDeadAll = false;
            _pendingCombatEnd = null;
        }
    }
}
