using UnityEngine;
using UnityEngine.UI;

namespace CookApps.AutoChess.View
{
    /// <summary>
    /// ClassicBattle 모드용 UI.
    /// 수동 전투 시작 버튼만 추가. 상점/경제/시너지 없음.
    /// </summary>
    public class ClassicAutoChessUI : AutoChessUIBase
    {
        [Header("Classic Mode")]
        [SerializeField] private Button _startBattleButton;

        protected override void OnInitialize()
        {
            _startBattleButton?.onClick.AddListener(OnStartBattleClicked);
        }

        private void OnStartBattleClicked()
        {
            var cmd = GameCommand.Ready(PlayerIndex);
            ViewBridge?.SendCommand(cmd);
        }

        protected override void OnCleanup()
        {
            _startBattleButton?.onClick.RemoveListener(OnStartBattleClicked);
        }
    }
}
