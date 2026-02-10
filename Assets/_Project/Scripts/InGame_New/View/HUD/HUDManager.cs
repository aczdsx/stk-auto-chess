using UnityEngine;

namespace CookApps.AutoChess.View
{
    /// <summary>
    /// HUD 표시 관리.
    /// 페이즈, 타이머, 골드, 레벨, HP 등 상단/하단 UI 갱신.
    /// </summary>
    public class HUDManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TMPro.TMP_Text _phaseText;
        [SerializeField] private TMPro.TMP_Text _timerText;
        [SerializeField] private TMPro.TMP_Text _goldText;
        [SerializeField] private TMPro.TMP_Text _levelText;
        [SerializeField] private TMPro.TMP_Text _hpText;
        [SerializeField] private TMPro.TMP_Text _stageText;

        private GamePhase _currentPhase;

        // ── 초기화 ──

        public void Initialize()
        {
            _currentPhase = GamePhase.Preparation;
        }

        // ── 페이즈 ──

        public void OnPhaseChanged(GamePhase newPhase)
        {
            _currentPhase = newPhase;

            if (_phaseText != null)
            {
                _phaseText.text = newPhase switch
                {
                    GamePhase.Preparation => "Preparation",
                    GamePhase.Combat => "Combat",
                    GamePhase.Result => "Result",
                    GamePhase.SharedDraft => "Shared Draft",
                    _ => newPhase.ToString(),
                };
            }
        }

        // ── 타이머 ──

        public void UpdateTimer(float remainingSeconds)
        {
            if (_timerText != null)
            {
                int seconds = Mathf.CeilToInt(remainingSeconds);
                _timerText.text = seconds.ToString();
            }
        }

        // ── 플레이어 정보 ──

        public void UpdatePlayerInfo(GameWorld world, int playerIndex)
        {
            if (playerIndex < 0 || playerIndex >= GameWorld.MaxPlayers) return;

            var player = world.Players[playerIndex];
            var economy = world.Economies[playerIndex];

            if (_goldText != null)
                _goldText.text = economy.Gold.ToString();

            if (_levelText != null)
                _levelText.text = $"Lv.{economy.Level}";

            if (_hpText != null)
                _hpText.text = $"{player.HP}/{player.MaxHP}";

            if (_stageText != null)
                _stageText.text = $"{world.CurrentStage}-{world.CurrentRound}";
        }

        // ── 이벤트 수신 (AutoChessViewBridge에서 호출) ──

        public void OnGoldChanged(int playerIndex, int totalGold, int delta)
        {
            // TODO: 골드 변경 애니메이션 (+/- 플로팅 텍스트)
        }

        public void OnLevelUp(int playerIndex, int newLevel)
        {
            // TODO: 레벨업 연출 (이펙트, 사운드)
        }

        public void OnPlayerEliminated(int playerIndex, int rank)
        {
            // TODO: 탈락 알림 UI
        }

        public void OnCombatResult(int matchIndex, int winner)
        {
            // TODO: 전투 결과 표시 (승리/패배 배너)
        }
    }
}
