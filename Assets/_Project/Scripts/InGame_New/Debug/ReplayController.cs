using UnityEngine;

namespace CookApps.AutoChess
{
    /// <summary>
    /// 프레임 디버거용 리플레이 컨트롤러.
    /// 전투 시작 시점의 RNG 상태를 저장하고, 목표 프레임까지 고속 재실행하여 상태 복원.
    ///
    /// ■ 되감기 범위
    ///   - 전투(Combat) 페이즈 전체가 대상. 전투 시작 ~ 현재 프레임까지 어디든 이동 가능.
    ///   - 전투 시작 시점의 RNG 상태 1개만 저장하므로 메모리 오버헤드 거의 없음.
    ///   - 되돌리기 한계: 전투 시작 이전(준비 페이즈)으로는 이동 불가.
    ///
    /// ■ 미리보기 범위
    ///   - CombatFrameRecorder에 기록된 스냅샷 범위 내에서만 이동 가능.
    ///   - RecordStartFrame ~ RecordEndFrame 사이의 프레임이 대상.
    ///   - 아직 도달하지 않은 미래 프레임으로의 이동은 불가 (기록된 틱 수까지만).
    ///
    /// ■ 성능
    ///   - 시뮬레이션만 재실행 (View/VFX 없음). 1800틱(60초) 기준 ~5-20ms.
    ///   - 매 Seek마다 전투를 처음부터 재실행하므로, 프레임 수에 비례하여 시간 증가.
    ///
    /// ■ VFX 동기화
    ///   - 투사체: 활성 투사체를 CombatMatchState.Projectiles[]에서 즉시 스폰
    ///   - 상태이펙트: StatusEffect[] + CC 상태에서 CombatVfxType을 역산하여 루프 VFX 복원
    ///   - 타일 이펙트: 마지막 틱 이벤트로 트리거
    ///   - fire-and-forget VFX(피격, 스킬): 마지막 틱 이벤트로만 트리거 (중간 프레임 것은 재현 불가)
    /// </summary>
    public class ReplayController
    {
        private ulong _initialRNGState;
        private bool _hasCaptured;
        private GameWorld _world;
        private View.AutoChessViewBridge _viewBridge;

        public bool HasCapturedState => _hasCaptured;

        public void SetWorld(GameWorld world)
        {
            _world = world;
        }

        public void SetViewBridge(View.AutoChessViewBridge viewBridge)
        {
            _viewBridge = viewBridge;
        }

        /// <summary>
        /// 전투 시작 직전의 RNG 상태 캡처.
        /// RecordFrame 첫 호출 시 한 번만 호출.
        /// </summary>
        public void CaptureInitialState(GameWorld world)
        {
            if (_hasCaptured) return;
            _initialRNGState = world.RNG.State;
            _hasCaptured = true;
            Debug.Log($"[ReplayController] RNG state captured: {_initialRNGState}");
        }

        /// <summary>
        /// 목표 스냅샷 인덱스까지 전투를 처음부터 고속 재실행.
        /// 시뮬레이션은 결정론적(동일 RNG → 동일 결과)이므로 정확한 상태 복원이 보장됨.
        /// </summary>
        /// <param name="targetTickCount">
        /// 목표 틱 수 (0 = 전투 시작 직후, N = N번째 틱 완료 상태).
        /// CombatFrameRecorder의 스냅샷 인덱스와 1:1 대응.
        /// </param>
        /// <param name="recorder">스냅샷 참조용 (현재 미사용, 향후 검증용)</param>
        public void SeekToFrame(int targetTickCount, CombatFrameRecorder recorder)
        {
            if (!_hasCaptured || _world == null)
            {
                Debug.LogWarning("[ReplayController] Cannot seek: no captured state or world.");
                return;
            }

            if (_world.CombatMatchStates[0] == null)
            {
                Debug.LogWarning("[ReplayController] Cannot seek: no match state.");
                return;
            }

            var sw = System.Diagnostics.Stopwatch.StartNew();

            // 1. View 리셋 (투사체/VFX 정리, 유닛 View 유지)
            _viewBridge?.ResetForReplay();

            // 2. RNG 복원 → 동일 전투 재현 보장
            _world.RNG = new DeterministicRNG(_initialRNGState);

            // 3. 전투 재초기화 (스킬/투사체 정리 + 매치 셋업)
            ResetCombatState(_world);

            // 4. 고속 재실행 (View/VFX 없이 시뮬레이션만)
            var state = _world.CombatMatchStates[0];
            for (int tick = 0; tick < targetTickCount; tick++)
            {
                CombatAISystem.Tick(state, ref _world.RNG, _world.TickRate);

                // 마지막 틱 이전: 이벤트 버림
                if (tick < targetTickCount - 1)
                    state.EventQueue?.Clear();
            }

            // 5. View 복원 (유닛 동기화 + 이벤트 디스패치 + 투사체 스폰 + VFX 복원)
            _viewBridge?.SyncForReplay(_world);

            sw.Stop();
            Debug.Log($"[ReplayController] Seeked to tick {targetTickCount} in {sw.ElapsedMilliseconds}ms");
        }

        public void Reset()
        {
            _hasCaptured = false;
            _initialRNGState = 0;
        }

        /// <summary>전투 상태 리셋 (스킬/투사체 정리 + 매치 재셋업)</summary>
        private static void ResetCombatState(GameWorld world)
        {
            for (int i = 0; i < GameWorld.MaxCombatMatches; i++)
            {
                if (world.CombatMatchStates[i] != null)
                    SkillSystem.Cleanup(world.CombatMatchStates[i]);
            }

            world.EventQueue?.Clear();

            SkillFactory.Clear();
            SkillFactory.Initialize(world.TickRate);

            GameLoopSystem.SetupCombatMatches(world);
        }
    }
}
