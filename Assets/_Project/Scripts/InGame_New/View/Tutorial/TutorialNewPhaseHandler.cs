using System;
using CookApps.AutoBattler;
using UnityEngine;

namespace CookApps.AutoChess.View
{
    /// <summary>
    /// InGame_New 범용 페이즈 튜토리얼 핸들러.
    /// PREPARATION_START, SHOP_PURCHASE, UNIT_PLACED, SYNERGY_ACTIVATED 등
    /// 다양한 트리거 타입을 처리.
    /// </summary>
    public class TutorialNewPhaseHandler : IDisposable
    {
        private readonly LocalSimulationRunner _runner;
        private bool _isPaused;

        public TutorialNewPhaseHandler(LocalSimulationRunner runner)
        {
            _runner = runner;
        }

        public bool TryHandleTutorial(TutorialTriggerType triggerType)
        {
            if (!TutorialManager.Instance.IsTutorialAction(triggerType))
                return false;

            _isPaused = true;
            _runner.PauseTick();
            TutorialManager.Instance.OnTutorialClosed += ResumeAfterTutorial;
            TutorialManager.Instance.HandleTutorialAction(triggerType, "0");

            Debug.Log($"[TutorialNewPhaseHandler] 시뮬레이션 일시정지 ({triggerType})");
            return true;
        }

        private void ResumeAfterTutorial()
        {
            if (!_isPaused) return;
            _isPaused = false;
            _runner.ResumeTick();
            Debug.Log("[TutorialNewPhaseHandler] 시뮬레이션 재개");
        }

        public void Dispose()
        {
            if (_isPaused)
            {
                TutorialManager.Instance.OnTutorialClosed -= ResumeAfterTutorial;
            }
            _isPaused = false;
        }
    }
}
