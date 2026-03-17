using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace CookApps.AutoChess.View
{
    /// <summary>
    /// PvECampaign 모드용 UI.
    /// ClassicBattle과 달리 상점/경제/시너지 시스템이 활성화되며, 준비 타이머가 있음.
    ///
    /// 프리팹: Prefabs/UI/InGame/AutoChessUI_Campaign.prefab
    /// GameConfig 조건: EnableShop=true, EnableEconomy=true, EnableSynergy=true, PreparationDuration>0
    ///
    /// HUD(타이머/골드/레벨/HP/스테이지)는 AutoChessUIBase에서 자동 갱신됨.
    /// 아래는 Campaign 모드 전용 추가 UI 요소:
    ///
    /// TODO 구현 목록:
    /// - 상점 UI (ShopSlotUI x5, 리롤 버튼, 잠금 버튼)
    ///   → BuyUnit(playerIndex, shopSlotIndex), RerollShop(playerIndex), LockShop(playerIndex)
    /// - 경제 UI (XP 바 + 구매 버튼)
    ///   → BuyXP(playerIndex), world.Economies[playerIndex] 참조
    /// - 시너지 UI (활성 시너지 아이콘 리스트)
    ///   → world.SynergyStates[playerIndex] 참조
    /// </summary>
    public class CampaignAutoChessUI : AutoChessUIBase
    {
        // ── 상점 ──

        [Header("Shop")]
        [SerializeField] private GameObject _shopPanel;
        // TODO: [SerializeField] private ShopSlotUI[] _shopSlots;
        [SerializeField] private Button _rerollButton;
        [SerializeField] private TMP_Text _rerollCostText;
        [SerializeField] private Button _lockButton;

        // ── 경제 (XP 구매 — 골드/레벨은 Base의 HUD에서 처리) ──

        [Header("Economy")]
        [SerializeField] private Button _buyXPButton;
        [SerializeField] private TMP_Text _xpText;          // "4/10"
        [SerializeField] private Image _xpFillBar;

        // ── 시너지 ──

        [Header("Synergy")]
        [SerializeField] private GameObject _synergyPanel;
        // TODO: [SerializeField] private Transform _synergyIconContainer;

        protected override void OnInitialize()
        {
            // TODO: 상점/경제 버튼 리스너 등록
            // _rerollButton?.onClick.AddListener(OnRerollClicked);
            // _lockButton?.onClick.AddListener(OnLockClicked);
            // _buyXPButton?.onClick.AddListener(OnBuyXPClicked);
        }

        protected override void OnSyncState(GameWorld world)
        {
            // TODO: 매 틱마다 호출 — 상점/XP/시너지 갱신
            // UpdateShopUI(world);
            // UpdateXPUI(world);
            // UpdateSynergyUI(world);
        }

        protected override void OnCleanup()
        {
            // TODO: 리스너 해제
            // _rerollButton?.onClick.RemoveListener(OnRerollClicked);
            // _lockButton?.onClick.RemoveListener(OnLockClicked);
            // _buyXPButton?.onClick.RemoveListener(OnBuyXPClicked);
        }

        // ── 상점 커맨드 ──

        // private void OnRerollClicked()
        //     => ViewBridge?.SendCommand(GameCommand.RerollShop(PlayerIndex));

        // private void OnLockClicked()
        //     => ViewBridge?.SendCommand(GameCommand.LockShop(PlayerIndex));

        // private void OnBuyXPClicked()
        //     => ViewBridge?.SendCommand(GameCommand.BuyXP(PlayerIndex));
    }
}
