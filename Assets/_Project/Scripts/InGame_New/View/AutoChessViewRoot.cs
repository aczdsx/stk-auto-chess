using CookApps.AutoBattler;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace CookApps.AutoChess.View
{
    /// <summary>
    /// AutoChess 뷰 레이어 루트.
    /// Addressables 리소스 로드 + 뷰 매니저 동적 생성 + 와이어링을 담당.
    /// </summary>
    public class AutoChessViewRoot : MonoBehaviour
    {
        private GameObject _stagePrefab;
        private GameObject _unitViewPrefab;
        private GameObject _autoChessUIPrefab;
        private GameObject _hpBarPrefab;
        private GameObject _damageTextPrefab;
        private BuffIconConfigSO _buffIconConfig;
        private GameObject _stageInstance;
        private int _stageId;
        private LocalSimulationRunner _runner;
        private AutoChessViewBridge _viewBridge;
        private UnitViewManager _unitViewManager;
        private CombatViewManager _combatViewManager;
        private CombatVfxManager _combatVfxManager;
        private BuffIconTracker _buffIconTracker;
        private BoardGridView _boardGridView;
        private TileEffectManager _tileEffectManager;
        private TargetLineManager _targetLineManager;

        public ISimulationRunner Runner => _runner;
        public AutoChessViewBridge ViewBridge => _viewBridge;
        public UnitViewManager UnitViewManager => _unitViewManager;
        public BoardGridView BoardGridView => _boardGridView;
        public TileEffectManager TileEffectManager => _tileEffectManager;
        public TargetLineManager TargetLineManager => _targetLineManager;
        public GameObject AutoChessUIPrefab => _autoChessUIPrefab;

        // ── Addressables 리소스 로드 ──

        public async UniTask LoadResources(int stageId, GameModeType gameMode)
        {
            _stageId = stageId;
            var stageInfo = SpecDataManager.Instance.GetStageData(stageId);
            int chapterId = stageInfo?.chapter_id ?? stageId;

            _stagePrefab = await Addressables.LoadAssetAsync<GameObject>(
                $"Prefabs/Stages/Ingame_New/Stage{chapterId}.prefab");
            _unitViewPrefab = await Addressables.LoadAssetAsync<GameObject>(
                "Prefabs/InGame/UnitView.prefab");

            // 모드별 UI 프리팹 로드
            string uiPath = gameMode switch
            {
                GameModeType.PvECampaign => "Prefabs/UI/InGame/AutoChessUI_Campaign.prefab",
                GameModeType.Competitive => "Prefabs/UI/InGame/AutoChessUI_Competitive.prefab",
                _ => "Prefabs/UI/InGame/AutoChessUI_Classic.prefab",
            };
            _autoChessUIPrefab = await Addressables.LoadAssetAsync<GameObject>(uiPath);

            _hpBarPrefab = await Addressables.LoadAssetAsync<GameObject>(
                "Prefabs/InGame/FloatingHpBar.prefab");
            _damageTextPrefab = await Addressables.LoadAssetAsync<GameObject>(
                "Prefabs/InGame/DamageText.prefab");

            // BuffIconConfigSO는 SoDataProvider에서 가져옴 (프리팹은 풀에서 직접 로드)
            _buffIconConfig = SoDataProvider.Instance.Get<BuffIconConfigSO>();
        }

        // ── 동적 초기화 (LoadResources 후 호출) ──

        public async UniTask Initialize()
        {
            // 스테이지 인스턴스화
            _stageInstance = Instantiate(_stagePrefab, transform);
            _boardGridView = _stageInstance.GetComponentInChildren<BoardGridView>();

            // 시뮬레이션 러너
            var runnerObj = new GameObject("SimulationRunner");
            runnerObj.transform.SetParent(transform);
            _runner = runnerObj.AddComponent<LocalSimulationRunner>();

            // 뷰 매니저
            _unitViewManager = CreateChild<UnitViewManager>("UnitViewManager");
            _unitViewManager.SetPrefab(_unitViewPrefab.GetComponent<UnitView>());
            _combatViewManager = CreateChild<CombatViewManager>("CombatViewManager");

            // TileEffectManager
            _tileEffectManager = CreateChild<TileEffectManager>("TileEffectManager");
            _tileEffectManager.Initialize();

            // CombatViewManager에 의존성 전달
            _combatViewManager.SetTileEffectManager(_tileEffectManager);
            _combatViewManager.SetUnitViewManager(_unitViewManager);

            // TargetLineManager (독립적으로 시뮬레이션 이벤트 구독)
            _targetLineManager = CreateChild<TargetLineManager>("TargetLineManager");
            _targetLineManager.Initialize(_runner, _unitViewManager);

            // HP 바 풀 초기화
            if (_hpBarPrefab != null)
                InGameHpBarViewPool.Instance.Initialize(_hpBarPrefab);

            // 데미지 텍스트 풀 초기화
            if (_damageTextPrefab != null)
                InGameTextViewPool.Instance.InitializePool(_damageTextPrefab);

            // 버프/디버프 아이콘 풀 초기화 (SO에서 직접 프리팹 로드)
            InGameBuffDebuffPool.Instance.Initialize();

            // CombatVfxManager (SoDataProvider에서 SO 가져오기)
            var combatVfxConfig = CookApps.AutoBattler.SoDataProvider.Instance.Get<CombatVfxConfigSO>();
            if (combatVfxConfig != null)
            {
                _combatVfxManager = new CombatVfxManager(combatVfxConfig, _unitViewManager);
            }

            // BuffIconTracker
            var tickRate = _runner.GetWorld()?.TickRate ?? 60;
            _buffIconTracker = new BuffIconTracker(_unitViewManager, tickRate, _buffIconConfig);

            // ViewBridge 와이어링
            _viewBridge = CreateChild<AutoChessViewBridge>("ViewBridge");
            _viewBridge.Setup(_runner, _unitViewManager, _combatViewManager, _boardGridView,
                _combatVfxManager, _buffIconTracker);

            // 시너지 달성 VFX 설정
            var synergyVfxConfig = SoDataProvider.Instance.Get<SynergyVfxConfigSO>();
            _viewBridge.SetSynergyVfxConfig(synergyVfxConfig);

            // 튜토리얼 초기화 + 브릿지 연결
            if (TutorialManager.Instance != null && !TutorialManager.IsSkipTutorial)
            {
                await TutorialManager.Instance.CheckAndInitTutorial(_stageId);
                TutorialManager.Instance.HandleTutorialAction(TutorialTriggerType.GAME_START, "0");
            }
            var tutorialBridge = new TutorialSimBridge(_runner);
            tutorialBridge.SetBoardGridView(_boardGridView);
            _viewBridge.SetTutorialBridge(tutorialBridge);
        }

        // ── 정리 ──

        public void Cleanup()
        {
            TutorialSimBridge.Instance?.Dispose();

            InGameHpBarViewPool.Instance.Clear();
            InGameTextViewPool.Instance.ReleasePool();
            InGameBuffDebuffPool.Instance.Clear();

            if (_stageInstance != null)
                Destroy(_stageInstance);
            if (_stagePrefab != null)
            {
                Addressables.Release(_stagePrefab);
                _stagePrefab = null;
            }
            if (_unitViewPrefab != null)
            {
                Addressables.Release(_unitViewPrefab);
                _unitViewPrefab = null;
            }
            if (_autoChessUIPrefab != null)
            {
                Addressables.Release(_autoChessUIPrefab);
                _autoChessUIPrefab = null;
            }
            if (_hpBarPrefab != null)
            {
                Addressables.Release(_hpBarPrefab);
                _hpBarPrefab = null;
            }
            if (_damageTextPrefab != null)
            {
                Addressables.Release(_damageTextPrefab);
                _damageTextPrefab = null;
            }
        }

        private T CreateChild<T>(string name) where T : Component
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(transform);
            return obj.AddComponent<T>();
        }
    }
}
