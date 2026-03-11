using System;
using CookApps.AutoBattler;
using UnityEngine;

namespace CookApps.AutoChess.View
{
    /// <summary>
    /// InGame_New 전투 종료 튜토리얼 핸들러.
    /// CombatResult 이벤트 감지 시 COMBAT_END 트리거 발동.
    /// </summary>
    public class TutorialNewCombatEndHandler : IDisposable
    {
        private readonly LocalSimulationRunner _runner;
        private bool _isPaused;

        public TutorialNewCombatEndHandler(LocalSimulationRunner runner)
        {
            _runner = runner;
        }

        public bool TryHandleTutorial()
        {
            if (TutorialManager.Instance == null) return false;
            if (!TutorialManager.Instance.IsTutorialAction(TutorialTriggerType.COMBAT_END))
                return false;

            _isPaused = true;
            _runner.PauseTick();
            TutorialManager.Instance.OnTutorialClosed += ResumeAfterTutorial;
            bool handled = TutorialManager.Instance.HandleTutorialAction(TutorialTriggerType.COMBAT_END, "0");
            if (!handled)
            {
                TutorialManager.Instance.OnTutorialClosed -= ResumeAfterTutorial;
                _isPaused = false;
                _runner.ResumeTick();
                return false;
            }

            Debug.Log("[TutorialNewCombatEndHandler] 시뮬레이션 일시정지 (COMBAT_END)");
            return true;
        }

        private void ResumeAfterTutorial()
        {
            if (!_isPaused) return;
            _isPaused = false;
            _runner.ResumeTick();
            Debug.Log("[TutorialNewCombatEndHandler] 시뮬레이션 재개");
        }

        public void Dispose()
        {
            if (_isPaused)
            {
                TutorialManager.Instance?.OnTutorialClosed -= ResumeAfterTutorial;
            }
            _isPaused = false;
        }
    }
}
