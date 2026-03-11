using System;
using CookApps.AutoBattler;
using UnityEngine;

namespace CookApps.AutoChess.View
{
    /// <summary>
    /// InGame_New 스킬 준비 튜토리얼 핸들러.
    /// ManaFull 이벤트 감지 시 SKILL_READY 트리거 발동.
    /// CHARACTER_DEAD 트리거가 대기 중이면 지연 처리.
    /// </summary>
    public class TutorialNewSkillReadyHandler : IDisposable
    {
        private readonly LocalSimulationRunner _runner;
        private bool _isPaused;
        private int _deferredEntityId = -1;

        public TutorialNewSkillReadyHandler(LocalSimulationRunner runner)
        {
            _runner = runner;
        }

        public bool TryHandleTutorial(int entityId)
        {
            if (TutorialManager.Instance == null) return false;

            // CHARACTER_DEAD 트리거가 대기 중이면 스킬 준비 트리거를 지연
            if (TutorialManager.Instance.IsTutorialAction(TutorialTriggerType.CHARACTER_DEAD))
            {
                _deferredEntityId = entityId;
                return true;
            }

            if (!TutorialManager.Instance.IsTutorialAction(TutorialTriggerType.SKILL_READY))
                return false;

            _isPaused = true;
            _runner.PauseTick();
            TutorialManager.Instance.OnTutorialClosed += ResumeAfterTutorial;
            bool handled = TutorialManager.Instance.HandleTutorialAction(TutorialTriggerType.SKILL_READY, entityId.ToString());
            if (!handled)
            {
                TutorialManager.Instance.OnTutorialClosed -= ResumeAfterTutorial;
                _isPaused = false;
                _runner.ResumeTick();
                return false;
            }

            Debug.Log($"[TutorialNewSkillReadyHandler] 시뮬레이션 일시정지 (SKILL_READY, entityId={entityId})");
            return true;
        }

        public void TryProcessDeferred()
        {
            if (_deferredEntityId < 0) return;
            int entityId = _deferredEntityId;
            _deferredEntityId = -1;
            TryHandleTutorial(entityId);
        }

        private void ResumeAfterTutorial()
        {
            if (!_isPaused) return;
            _isPaused = false;
            _runner.ResumeTick();
            Debug.Log("[TutorialNewSkillReadyHandler] 시뮬레이션 재개");
        }

        public void Dispose()
        {
            if (_isPaused)
            {
                TutorialManager.Instance?.OnTutorialClosed -= ResumeAfterTutorial;
            }
            _isPaused = false;
            _deferredEntityId = -1;
        }
    }
}
