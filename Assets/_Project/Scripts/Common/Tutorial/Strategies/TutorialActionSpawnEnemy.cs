using System.Collections.Generic;
using System.Threading;
using CookApps.BattleSystem;
using Cysharp.Threading.Tasks;
using Unity.Mathematics;
using UnityEngine;

namespace CookApps.AutoBattler
{
    /// <summary>
    /// 적 스폰 튜토리얼 액션.
    /// tutorial_action_key에 지정된 몬스터 ID로 지정된 타일(26, 28, 32)에 적을 순차 스폰합니다.
    /// 0.5~1초 랜덤 간격으로 스폰하며, 모두 완료되면 다음 튜토리얼로 진행됩니다.
    ///
    /// tutorial_action_key 형식:
    /// - "몬스터ID" : 해당 몬스터를 타일 26, 28, 32에 스폰 (레벨 1 고정)
    ///
    /// 스폰이 완료되면 자동으로 다음 튜토리얼로 진행됩니다.
    /// 스폰된 적은 CharacterStateReady 상태로 대기하며, SKILL_READY 튜토리얼 완료 후 Idle로 전환됩니다.
    /// </summary>
    public class TutorialActionSpawnEnemy : ITutorialActionStrategy
    {
        /// <summary>
        /// 스폰 완료 시 호출되는 콜백 (TutorialController에서 설정)
        /// </summary>
        public static System.Action OnSpawnEnemyCompleted;

        /// <summary>
        /// 현재 스폰 튜토리얼 진행 중인지 여부
        /// </summary>
        public static bool IsActive { get; private set; }

        /// <summary>
        /// SPAWN_ENEMY로 인해 게임이 일시 정지되었는지 여부
        /// (TutorialManager.HandleTutorialClose에서 확인하여 재생)
        /// </summary>
        public static bool IsPausedBySpawnEnemy { get; private set; }

        /// <summary>
        /// 스폰된 적 캐릭터 목록 (SKILL_READY 완료 시 Idle로 전환용)
        /// </summary>
        private static List<CookApps.BattleSystem.CharacterController> _spawnedEnemies = new List<CookApps.BattleSystem.CharacterController>();

        private const float MIN_SPAWN_INTERVAL = 0.4f;
        private const float MAX_SPAWN_INTERVAL = 0.6f;
        private const int DEFAULT_MONSTER_LEVEL = 1;
        private const float SPAWN_ACTION_DELAY = 0.3f;

        // 스폰할 타일 ID 목록
        private static readonly int[] SPAWN_TILE_IDS = { 26, 28, 32 };

        private CancellationTokenSource _cts;

        public void OnShow(TutorialActionContext context)
        {
            // 전체 화면 마스크 설정 (HoleRadius=1, 가운데)
            context.SetFullScreenMask();

            // 튜토리얼 UI 숨김 (스폰 중에는 말풍선 등 표시 안함)
            context.ArrowRectTransform.gameObject.SetActive(false);

            // 게임 일시 정지 (CHARACTER_DEAD 트리거 그룹이 끝날 때까지)
            if (InGameMainFlowManager.Instance != null)
            {
                InGameMainFlowManager.Instance.Pause();
                IsPausedBySpawnEnemy = true;
                Debug.LogColor("[TutorialActionSpawnEnemy] 게임 일시 정지", "yellow");
            }

            // tutorial_action_key에서 몬스터 ID 파싱
            if (!TryParseActionKey(context.CurrentTutorial.tutorial_action_key, out int monsterId))
            {
                Debug.LogWarning($"[TutorialActionSpawnEnemy] 몬스터 ID 파싱 실패: {context.CurrentTutorial.tutorial_action_key}");
                // 실패 시 즉시 완료 처리
                OnSpawnEnemyCompleted?.Invoke();
                return;
            }

            IsActive = true;

            // 스폰 시작
            _cts = new CancellationTokenSource();
            SpawnEnemiesAsync(monsterId, _cts.Token).Forget();
        }

        public void OnNext(TutorialActionContext context)
        {
            // 스폰 튜토리얼에서는 "다음" 버튼을 표시하지 않음
            // 스폰이 완료되면 자동으로 진행됨
            context.NextObj.SetActive(false);
        }

        public bool CanProceedOnDimmedClick(TutorialActionContext context)
        {
            // 딤드 클릭으로 진행 불가 - 반드시 스폰이 완료되어야 함
            return false;
        }

        public void OnClear(TutorialActionContext context)
        {
            // 스폰 취소
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;

            // 마스크 복원
            context.RestoreMask();

            // 상태 초기화
            IsActive = false;
            OnSpawnEnemyCompleted = null;
            // IsPausedBySpawnEnemy는 HandleTutorialClose에서 재생 후 초기화됨
        }

        /// <summary>
        /// 게임 재생 및 플래그 초기화 (TutorialManager.HandleTutorialClose에서 호출)
        /// </summary>
        public static void ResumeGameIfPaused()
        {
            if (IsPausedBySpawnEnemy && InGameMainFlowManager.Instance != null)
            {
                InGameMainFlowManager.Instance.Resume();
                Debug.LogColor("[TutorialActionSpawnEnemy] 게임 재생", "yellow");
            }
            IsPausedBySpawnEnemy = false;
        }

        /// <summary>
        /// 스폰된 적 캐릭터들을 Idle 상태로 전환 (TutorialSkillReadyHandler에서 호출)
        /// </summary>
        public static void ActivateSpawnedEnemies()
        {
            ActivateSpawnedEnemiesAsync().Forget();
        }

        private static async UniTaskVoid ActivateSpawnedEnemiesAsync()
        {
            if (_spawnedEnemies.Count == 0)
            {
                return;
            }

            // 0.5초 대기 후 진행
            await UniTask.Delay(300);

            Debug.LogColor($"[TutorialActionSpawnEnemy] 스폰된 적 {_spawnedEnemies.Count}명 Idle 상태로 전환", "yellow");

            foreach (var enemy in _spawnedEnemies)
            {
                if (enemy != null && enemy.IsAlive)
                {
                    enemy.AddNextState<CharacterStateIdle>();

                    enemy.Target = InGameObjectManager.Instance.GetNearestTargetOnce(enemy);
                }
            }

            _spawnedEnemies.Clear();
        }

        /// <summary>
        /// 스폰된 적 목록 초기화
        /// </summary>
        public static void ClearSpawnedEnemies()
        {
            _spawnedEnemies.Clear();
        }

        /// <summary>
        /// tutorial_action_key 파싱
        /// </summary>
        private bool TryParseActionKey(string actionKey, out int monsterId)
        {
            monsterId = 0;

            if (string.IsNullOrEmpty(actionKey))
            {
                return false;
            }

            return int.TryParse(actionKey, out monsterId);
        }

        /// <summary>
        /// 몬스터 순차 스폰 (지정된 타일에)
        /// </summary>
        private async UniTaskVoid SpawnEnemiesAsync(int monsterId, CancellationToken ct)
        {
            int spawnedCount = 0;

            for (int i = 0; i < SPAWN_TILE_IDS.Length; i++)
            {
                if (ct.IsCancellationRequested)
                {
                    Debug.Log("[TutorialActionSpawnEnemy] 스폰 취소됨");
                    return;
                }

                // 지정된 타일에 몬스터 스폰
                int tileId = SPAWN_TILE_IDS[i];
                bool success = await SpawnSingleEnemy(monsterId, tileId, ct);
                if (success)
                {
                    spawnedCount++;
                    Debug.LogColor($"[TutorialActionSpawnEnemy] 몬스터 스폰 {spawnedCount}/{SPAWN_TILE_IDS.Length}: {monsterId} (타일 {tileId})", "cyan");
                }

                // 마지막이 아니면 대기
                if (i < SPAWN_TILE_IDS.Length - 1)
                {
                    float interval = UnityEngine.Random.Range(MIN_SPAWN_INTERVAL, MAX_SPAWN_INTERVAL);
                    int delayMs = Mathf.RoundToInt(interval * 1000);

                    try
                    {
                        await UniTask.Delay(delayMs, cancellationToken: ct);
                    }
                    catch (System.OperationCanceledException)
                    {
                        Debug.Log("[TutorialActionSpawnEnemy] 스폰 대기 중 취소됨");
                        return;
                    }
                }
            }

            Debug.LogColor($"[TutorialActionSpawnEnemy] 스폰 완료: {spawnedCount}개", "green");

            // 스폰 완료 콜백
            IsActive = false;
            OnSpawnEnemyCompleted?.Invoke();
        }

        /// <summary>
        /// 지정된 타일에 단일 몬스터 스폰
        /// </summary>
        private async UniTask<bool> SpawnSingleEnemy(int monsterId, int tileId, CancellationToken ct)
        {
            if (InGameObjectManager.Instance == null)
            {
                Debug.LogWarning("[TutorialActionSpawnEnemy] InGameObjectManager가 없습니다.");
                return false;
            }

            // 지정된 타일 가져오기
            InGameTile tile = InGameObjectManager.Instance.InGameGrid.GetTile(tileId);
            if (tile == null)
            {
                Debug.LogWarning($"[TutorialActionSpawnEnemy] 타일을 찾을 수 없습니다: {tileId}");
                return false;
            }

            // 캐릭터 스탯 데이터 생성 (레벨 1 고정)
            var statData = new CharacterStatData(monsterId, DEFAULT_MONSTER_LEVEL, 0, 0.1f);
            if (statData.Spec == null)
            {
                Debug.LogWarning($"[TutorialActionSpawnEnemy] 몬스터 스펙이 없습니다: {monsterId}");
                return false;
            }

            // 스폰 좌표
            int2 coordinate = new int2(tile.X, tile.Y);

            try
            {
                // 캐릭터 추가 (CharacterStateReady로 시작하여 대기 상태 유지)
                var character = await InGameObjectManager.Instance.AddCharacterToField(
                    statData,
                    coordinate,
                    AllianceType.Enemy,
                    typeof(CharacterStateReady),
                    false
                );

                if (character != null)
                {
                    character.OverrideHp(150);
                    character.OverrideMoveSpeed(character.MoveSpeed * 0.6f);

                    // 스폰된 적 목록에 추가 (SKILL_READY 완료 시 Idle로 전환용)
                    _spawnedEnemies.Add(character);
                }

                return character != null;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[TutorialActionSpawnEnemy] 스폰 실패: {e.Message}");
                return false;
            }
        }
    }
}
