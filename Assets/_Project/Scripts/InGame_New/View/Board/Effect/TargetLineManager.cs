using CookApps.AutoBattler;
using CookApps.BattleSystem;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace CookApps.AutoChess.View
{
    /// <summary>
    /// Preparation 페이즈 타겟 라인 시각화.
    /// ISimulationRunner를 직접 구독하여 독립적으로 동작.
    /// Idle/Focused 상태를 전환하며 렌더링 위임.
    /// </summary>
    public class TargetLineManager : MonoBehaviour
    {
        private ISimulationRunner _runner;
        private TargetLineConfig _config;

        private TargetLineIdleState _idleState;
        private TargetLineFocusedState _focusedState;
        private TargetLineStateBase _currentState;

        private bool _isActive;

        // ── 초기화 ──

        public void Initialize(ISimulationRunner runner, UnitViewManager unitViewManager)
        {
            _runner = runner;
            SoDataProvider.Instance.TryGet(out _config);

            float yOffset = _config != null ? _config.CharacterYOffset : 0.5f;
            var targetLinePrefabRef = _config != null ? _config.TargetLinePrefabRef : null;

            _idleState = new TargetLineIdleState(unitViewManager, yOffset, targetLinePrefabRef);
            // tileSpacing은 StartDrawing에서 지연 초기화
            _focusedState = new TargetLineFocusedState(unitViewManager, runner, yOffset, 0f, targetLinePrefabRef);

            _runner.OnTick += HandleTick;
            _runner.OnPhaseChanged += HandlePhaseChanged;
        }

        private void OnDestroy()
        {
            if (_runner != null)
            {
                _runner.OnTick -= HandleTick;
                _runner.OnPhaseChanged -= HandlePhaseChanged;
            }
            _isActive = false;
            _idleState?.Clear();
            _focusedState?.Clear();
        }

        // ── 시뮬레이션 이벤트 ──

        private void HandleTick(GameWorld world)
        {
            if (!world.IsCombatActive)
                StartDrawing();
        }

        private void HandlePhaseChanged(GamePhase prevPhase, GamePhase newPhase)
        {
            switch (newPhase)
            {
                case GamePhase.Preparation:
                    StartDrawing();
                    break;
                case GamePhase.Combat:
                case GamePhase.Result:
                    StopDrawing();
                    break;
            }
        }

        // ── 시작/정지 ──

        private void StartDrawing()
        {
            if (_isActive) return;
            _isActive = true;

            // tileSpacing 지연 초기화 (BoardWorldHelper 초기화 이후)
            if (_focusedState != null && !_focusedState.IsInitialized)
            {
                var p0 = BoardWorldHelper.BoardGridToWorld(0, 0, 0);
                var p1 = BoardWorldHelper.BoardGridToWorld(0, 1, 0);
                _focusedState.SetTileSpacing(Vector3.Distance(p0, p1));
            }

            SwitchState(_idleState);
            DrawLoopAsync().Forget();
        }

        private void StopDrawing()
        {
            _isActive = false;
            _currentState?.Exit();
            _currentState = null;
            _idleState?.Clear();
            _focusedState?.Clear();
        }

        // ── 상태 전환 ──

        private void SwitchState(TargetLineStateBase newState)
        {
            if (_currentState == newState) return;
            _currentState?.Exit();
            _currentState = newState;
            _currentState.Enter();
        }

        // ── 포커스 (외부에서 호출) ──

        public void SetFocusedUnit(int entityId)
        {
            if (!_isActive) return;
            SwitchState(_focusedState);
            _focusedState.SetFocus(entityId);
        }

        public void ClearFocusedUnit()
        {
            if (!_isActive) return;
            _focusedState.ClearFocus();
            SwitchState(_idleState);
        }

        /// <summary>드래그 중 그리드 변경 시 즉시 재계산</summary>
        public void RefreshFocusedLines()
        {
            if (!_isActive || _currentState != _focusedState) return;
            _focusedState.Refresh();
        }

        // ── 비동기 루프 ──

        private async UniTaskVoid DrawLoopAsync()
        {
            float duration = _config != null ? _config.LineDurationTime : 2f;
            float gap = _config != null ? _config.GapTime : 1f;

            while (_isActive && this != null)
            {
                _currentState?.Draw();
                // 애니메이션 재생 시간 대기
                await UniTask.Delay(System.TimeSpan.FromSeconds(duration), ignoreTimeScale: true);
                if (!_isActive || this == null) break;
                // 공백 시간 (라인 없이 대기)
                await UniTask.Delay(System.TimeSpan.FromSeconds(gap), ignoreTimeScale: true);
            }
        }
    }
}
