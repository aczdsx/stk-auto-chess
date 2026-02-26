using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace CookApps.AutoChess.View
{
    /// <summary>
    /// Competitive(4인 PVP) 모드용 UI.
    /// Campaign과 동일한 상점/경제/시너지에 더해 관전 + SharedDraft 지원.
    ///
    /// 프리팹: Prefabs/UI/InGame/AutoChessUI_Competitive.prefab
    /// GameConfig 조건: EnableShop=true, EnableEconomy=true, EnableSynergy=true,
    ///                  EnableMatchmaking=true, PreparationDuration>0
    ///
    /// HUD(타이머/골드/레벨/HP/스테이지)는 AutoChessUIBase에서 자동 갱신됨.
    /// 아래는 Competitive 모드 전용 추가 UI 요소:
    ///
    /// Campaign과의 차이점:
    /// - 관전 UI (다른 플레이어 보드 전환 버튼)
    ///   → ViewBridge.SetSpectateBoard(boardIndex)
    /// - SharedDraft 페이즈 대응
    ///   → GamePhase.SharedDraft 시 별도 UI 표시 (캐러셀)
    /// - 공유 풀 (SharedPool=true) — 같은 챔피언 풀을 4명이 공유
    /// - 상대 HP/레벨 미니맵 표시
    ///
    /// TODO 구현 목록:
    /// - 상점, XP 구매, 시너지 (Campaign과 동일)
    /// - 관전 버튼 (플레이어 아이콘 x4, 클릭 시 보드 전환)
    /// - 상대 정보 미니 패널 (HP 바, 레벨, 연승/연패)
    /// - SharedDraft 페이즈 UI (캐러셀 선택)
    /// </summary>
    public class CompetitiveAutoChessUI : AutoChessUIBase
    {
        // ── 상점 (Campaign과 동일) ──

        [Header("Shop")]
        [SerializeField] private GameObject _shopPanel;
        // TODO: [SerializeField] private ShopSlotUI[] _shopSlots;
        [SerializeField] private Button _rerollButton;
        [SerializeField] private TMP_Text _rerollCostText;
        [SerializeField] private Button _lockButton;

        // ── 경제 (Campaign과 동일) ──

        [Header("Economy")]
        [SerializeField] private Button _buyXPButton;
        [SerializeField] private TMP_Text _xpText;
        [SerializeField] private Image _xpFillBar;

        // ── 시너지 (Campaign과 동일) ──

        [Header("Synergy")]
        [SerializeField] private GameObject _synergyPanel;

        // ── 관전 (Competitive 전용) ──

        [Header("Spectate")]
        [SerializeField] private GameObject _spectatePanel;
        // TODO: [SerializeField] private Button[] _playerBoardButtons;

        // ── 상대 정보 (Competitive 전용) ──

        [Header("Opponents")]
        [SerializeField] private GameObject _opponentInfoPanel;
        // TODO: 상대 HP바, 레벨, 연승/연패 표시 슬롯

        protected override void OnInitialize()
        {
            // TODO: 상점/경제 버튼 리스너 (Campaign과 동일)
            // TODO: 관전 버튼 리스너
            //   _playerBoardButtons[i].onClick.AddListener(() => OnSpectateClicked(i));
        }

        protected override void OnSyncState(GameWorld world)
        {
            // TODO: 매 틱마다 호출
            // UpdateShopUI(world);
            // UpdateXPUI(world);
            // UpdateSynergyUI(world);
            // UpdateOpponentInfo(world);  ← Competitive 전용
        }

        protected override void OnCleanup()
        {
            // TODO: 리스너 해제
        }

        // ── 관전 ──

        // private void OnSpectateClicked(int boardIndex)
        // {
        //     ViewBridge?.SetSpectateBoard(boardIndex);
        // }

        // ── 상대 정보 ──

        // private void UpdateOpponentInfo(GameWorld world)
        // {
        //     for (int p = 0; p < world.Config.PlayerCount; p++)
        //     {
        //         if (p == PlayerIndex) continue;
        //         // world.Players[p].HP, world.Economies[p].Level, 연승 등
        //     }
        // }
    }
}
