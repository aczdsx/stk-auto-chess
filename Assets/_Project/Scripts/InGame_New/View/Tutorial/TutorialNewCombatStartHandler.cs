using System;
using CookApps.AutoBattler;
using UnityEngine;

namespace CookApps.AutoChess.View
{
    /// <summary>
    /// InGame_New 전투 시작 튜토리얼 핸들러.
    /// COMBAT_START 트리거 감지 시 시뮬레이션을 일시정지하고 튜토리얼 표시.
    /// </summary>
    public class TutorialNewCombatStartHandler : IDisposable
    {
        private readonly LocalSimulationRunner _runner;
        private bool _isPaused;

        public TutorialNewCombatStartHandler(LocalSimulationRunner runner)
        {
            _runner = runner;
        }

        public bool TryHandleTutorial()
        {
            if (TutorialManager.Instance == null) return false;
            if (!TutorialManager.Instance.IsTutorialAction(TutorialTriggerType.COMBAT_START))
                return false;

            _isPaused = true;
            _runner.PauseTick();
            TutorialManager.Instance.OnTutorialClosed += ResumeAfterTutorial;
            bool handled = TutorialManager.Instance.HandleTutorialAction(TutorialTriggerType.COMBAT_START, "0");
            if (!handled)
            {
                TutorialManager.Instance.OnTutorialClosed -= ResumeAfterTutorial;
                _isPaused = false;
                _runner.ResumeTick();
                return false;
            }

            Debug.Log("[TutorialNewCombatStartHandler] 시뮬레이션 일시정지 (COMBAT_START)");
            return true;
        }

        private void ResumeAfterTutorial()
        {
            if (!_isPaused) return;
            _isPaused = false;
            _runner.ResumeTick();
            Debug.Log("[TutorialNewCombatStartHandler] 시뮬레이션 재개");
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
