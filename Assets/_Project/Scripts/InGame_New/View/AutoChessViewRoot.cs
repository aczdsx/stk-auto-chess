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
        private GameObject _stageInstance;
        private LocalSimulationRunner _runner;
        private AutoChessViewBridge _viewBridge;
        private UnitViewManager _unitViewManager;
        private CombatViewManager _combatViewManager;
        private HUDManager _hudManager;
        private BoardGridView _boardGridView;

        public LocalSimulationRunner Runner => _runner;
        public AutoChessViewBridge ViewBridge => _viewBridge;

        // ── Addressables 리소스 로드 ──

        public async UniTask LoadResources(int stageId)
        {
            _stagePrefab = await Addressables.LoadAssetAsync<GameObject>(
                $"Prefabs/Stages/Ingame/Stage{stageId}.prefab");
            _unitViewPrefab = await Addressables.LoadAssetAsync<GameObject>(
                "Prefabs/InGame/UnitView.prefab");
        }

        // ── 동적 초기화 (LoadResources 후 호출) ──

        public void Initialize()
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

            // HUD (스테이지에 포함 or 별도)
            _hudManager = _stageInstance.GetComponentInChildren<HUDManager>();
            if (_hudManager == null)
                _hudManager = CreateChild<HUDManager>("HUDManager");

            // ViewBridge 와이어링
            _viewBridge = CreateChild<AutoChessViewBridge>("ViewBridge");
            _viewBridge.Setup(_runner, _unitViewManager, _combatViewManager, _hudManager, _boardGridView);
        }

        // ── 정리 ──

        public void Cleanup()
        {
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
        }

        private T CreateChild<T>(string name) where T : Component
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(transform);
            return obj.AddComponent<T>();
        }
    }
}
