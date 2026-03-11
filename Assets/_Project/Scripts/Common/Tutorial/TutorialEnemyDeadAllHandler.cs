using System;
using CookApps.BattleSystem;
using Cysharp.Threading.Tasks;
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
        /// 슬로우 모션 시작 전 원래 플레이 속도
        /// </summary>
        private static float _originalPlaySpeed = 1f;

        // 슬로우 모션 설정
        private const float SLOWMO_DURATION = 2.0f;  // 슬로우 모션 지속 시간
        private const float SLOWMO_START_SPEED = 0.4f;  // 시작 속도
        private const float SLOWMO_END_SPEED = 1.0f;   // 최종 속도

        /// <summary>
        /// ENEMY_DEAD_ALL 튜토리얼을 처리합니다.
        /// </summary>
        /// <param name="onCombatEnd">튜토리얼 완료 후 호출될 전투 종료 콜백</param>
        /// <returns>튜토리얼이 처리되었으면 true, 아니면 false (바로 전투 종료 필요)</returns>
        public static bool TryHandleTutorial(Action onCombatEnd)
        {
            // 이미 처리 중이면 true 반환 (중복 호출 방지, EndCombat 직접 호출 방지)
            if (IsPausedByEnemyDeadAll)
            {
                return true;
            }

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

            // 슬로우 모션 시작 전에 플래그 설정 (중복 호출 방지)
            IsPausedByEnemyDeadAll = true;

            // 전투 종료 콜백 저장
            _pendingCombatEnd = onCombatEnd;

            // 슬로우 모션 후 튜토리얼 표시
            SlowMotionThenShowTutorial().Forget();

            return true;
        }

        /// <summary>
        /// 슬로우 모션 효과 후 튜토리얼 표시
        /// </summary>
        private static async UniTaskVoid SlowMotionThenShowTutorial()
        {
            if (InGameMainFlowManager.Instance == null) return;

            // IsPausedByEnemyDeadAll은 TryHandleTutorial에서 이미 설정됨
            _originalPlaySpeed = InGameMainFlowManager.Instance.FastForwardRate;
            Debug.LogColor($"[TutorialEnemyDeadAllHandler] 슬로우 모션 시작 (원래 속도: {_originalPlaySpeed})", "yellow");

            // 슬로우 모션 효과 (점진적으로 느려짐)
            float elapsed = 0f;
            while (elapsed < SLOWMO_DURATION)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / SLOWMO_DURATION;
                float speed = Mathf.Lerp(SLOWMO_START_SPEED, SLOWMO_END_SPEED, t);
                InGameMainFlowManager.Instance.SetPlaySpeed(speed);
                await UniTask.Yield(PlayerLoopTiming.Update);
            }

            InGameMainFlowManager.Instance.SetPlaySpeed(_originalPlaySpeed);

            // 완전 정지
            InGameMainFlowManager.Instance.Pause();
            Debug.LogColor("[TutorialEnemyDeadAllHandler] 게임 일시 정지", "yellow");

            // 튜토리얼 닫힘 시 Resume 콜백 등록 (one-shot)
            if (TutorialManager.Instance != null)
            {
                TutorialManager.Instance.OnTutorialClosed += ResumeAndEndCombat;
            }

            // 튜토리얼 표시
            TutorialManager.Instance?.HandleTutorialAction(
                TutorialTriggerType.ENEMY_DEAD_ALL,
                "0");
        }

        /// <summary>
        /// 튜토리얼 완료 후 게임 재개 및 전투 종료 처리
        /// (TutorialManager.HandleTutorialClose에서 호출)
        /// </summary>
        public static void ResumeAndEndCombat()
        {
            // 게임 재개 및 속도 복원
            if (IsPausedByEnemyDeadAll && InGameMainFlowManager.Instance != null)
            {
                InGameMainFlowManager.Instance.Resume();
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
            _originalPlaySpeed = 1f;
        }
    }
}
