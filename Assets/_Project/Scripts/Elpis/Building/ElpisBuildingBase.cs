using System.Collections;
using System.Collections.Generic;
using CookApps.TeamBattle;
using Cysharp.Threading.Tasks;
using Tech.Hive.V1;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace CookApps.AutoBattler
{
    public class ElpisBuildingBase : CachedMonoBehaviour
    {
        private static class AnimationState
        {
            public const string Level1 = "Level1";
            public const string Level2 = "Level2";
            public const string Level3 = "Level3";
            public const string Finish = "Finish";
            public const string Disappear = "Disappear";
        }

        private const float TransitionDuration = 0.3f;

        [SerializeField] private Animator buildingAnimator;
        [SerializeField] private Transform[] spawnPoints;

        private Dictionary<Transform, bool> isSpawned;

        public int SlotIndex { get; private set; }
        public ElpisFacilityType BuildingType { get; private set; }

        private UnityEngine.Coroutine constructionCoroutine;

        public void Initialize(int slotIndex)
        {
            SlotIndex = slotIndex;
        }

        private void InitializeSpawnPoints()
        {
            isSpawned = new Dictionary<Transform, bool>();
            foreach (var spawnPoint in spawnPoints)
            {
                isSpawned.Add(spawnPoint, false);
            }
        }

        /// <summary>
        /// 건설/업그레이드 애니메이션을 시작합니다.
        /// </summary>
        /// <param name="remainingTime">남은 건설 시간 (초)</param>
        /// <param name="totalBuildTime">총 건설 시간 (초)</param>
        /// <param name="isUpgrade">업그레이드 여부 (true면 Level3만 실행)</param>
        public void StartConstructionAnimation(float remainingTime, float totalBuildTime, bool isUpgrade = false)
        {
            StopConstructionAnimation();
            constructionCoroutine = StartCoroutine(PlayConstructionSequence(remainingTime, totalBuildTime, isUpgrade));
        }

        /// <summary>
        /// 건설 애니메이션을 중지합니다.
        /// </summary>
        public void StopConstructionAnimation()
        {
            if (constructionCoroutine != null)
            {
                StopCoroutine(constructionCoroutine);
                constructionCoroutine = null;
            }
        }

        /// <summary>
        /// 건물 프리팹을 생성하고 Disappear 애니메이션을 재생합니다.
        /// </summary>
        /// <param name="buildPrefabPath">Addressable 프리팹 경로</param>
        public async UniTask PlayDisappearAnimationAsync(string buildPrefabPath)
        {
            StopConstructionAnimation();

            // 건물 프리팹 생성
            //if (!string.IsNullOrEmpty(buildPrefabPath))
            //{
            //    var handle = Addressables.InstantiateAsync(buildPrefabPath, CachedTr.position, Quaternion.identity, CachedTr);
            //    await handle.ToUniTask();
            //}

            // Disappear 애니메이션 재생
            if (buildingAnimator != null)
            {
                buildingAnimator.Play(AnimationState.Disappear);
            }
        }

        /// <summary>
        /// 여러 건물 프리팹을 소환합니다. 이미 스폰된 건물은 건너뜁니다.
        /// </summary>
        /// <param name="buildPrefabPaths">Addressable 프리팹 경로 배열</param>
        public async UniTask SpawnMultiBuildingAsync(string[] buildPrefabPaths)
        {
            // spawnPoints가 없으면 단일 건물만 소환
            if (spawnPoints.Length == 0)
            {
                await SpawnBuildingAsync(buildPrefabPaths[0]);
                return;
            }

            if (isSpawned == null)
                InitializeSpawnPoints();

            // 이미 스폰된 개수만큼 건너뛰고 새로운 것만 스폰
            var alreadySpawnedCount = 0;
            foreach (var kvp in isSpawned)
            {
                if (kvp.Value) alreadySpawnedCount++;
            }

            for (var i = alreadySpawnedCount; i < buildPrefabPaths.Length; i++)
            {
                await SpawnBuildingAsync(buildPrefabPaths[i]);
            }
        }

        /// <summary>
        /// 건물 프리팹만 소환합니다. (이미 설치된 건물용)
        /// </summary>
        /// <param name="buildPrefabPath">Addressable 프리팹 경로</param>
        public async UniTask SpawnBuildingAsync(string buildPrefabPath)
        {
            if (string.IsNullOrEmpty(buildPrefabPath))
                return;

            if (spawnPoints.Length > 0)
            {
                foreach (var spawnPoint in spawnPoints)
                {
                    if(!isSpawned.TryGetValue(spawnPoint, out var spawned) || spawned)
                        continue;
                    
                    isSpawned[spawnPoint] = true;
                    await Addressables.InstantiateAsync(buildPrefabPath, spawnPoint.position, Quaternion.identity, spawnPoint).ToUniTask();
                    break;
                }
            }
            else
            {
                await Addressables.InstantiateAsync(buildPrefabPath, CachedTr.position, Quaternion.identity, CachedTr).ToUniTask();
            }
        }

        /// <summary>
        /// Finish 루프 상태로 전환합니다.
        /// </summary>
        public void PlayFinishLoop()
        {
            StopConstructionAnimation();

            if (buildingAnimator != null)
            {
                buildingAnimator.Play(AnimationState.Finish);
            }
        }

        private IEnumerator PlayConstructionSequence(float remainingTime, float totalBuildTime, bool isUpgrade)
        {
            if (buildingAnimator == null)
                yield break;

            // 업그레이드면 Level3만 실행
            if (isUpgrade)
            {
                buildingAnimator.CrossFadeInFixedTime(AnimationState.Level3, TransitionDuration);
                yield return new WaitForSeconds(remainingTime);
            }
            else
            {
                // Level1, Level2, Level3에 1:1:1 비율로 배분
                var levelTime = totalBuildTime / 3f;

                // 현재 진행률 계산 (0 = 시작, 1 = 완료)
                var progress = 1f - (remainingTime / totalBuildTime);

                // 어느 단계부터 시작해야 하는지 계산
                var currentPhase = Mathf.FloorToInt(progress * 3f);
                var phaseProgress = (progress * 3f) - currentPhase;

                // Level1 (phase 0)
                if (currentPhase <= 0)
                {
                    buildingAnimator.CrossFadeInFixedTime(AnimationState.Level1, TransitionDuration);
                    var timeInPhase = currentPhase == 0 ? levelTime * (1f - phaseProgress) : levelTime;
                    yield return new WaitForSeconds(timeInPhase);
                    currentPhase = 1;
                    phaseProgress = 0f;
                }

                // Level2 (phase 1)
                if (currentPhase <= 1)
                {
                    buildingAnimator.CrossFadeInFixedTime(AnimationState.Level2, TransitionDuration);
                    var timeInPhase = currentPhase == 1 ? levelTime * (1f - phaseProgress) : levelTime;
                    yield return new WaitForSeconds(timeInPhase);
                    currentPhase = 2;
                    phaseProgress = 0f;
                }

                // Level3 (phase 2)
                if (currentPhase <= 2)
                {
                    buildingAnimator.CrossFadeInFixedTime(AnimationState.Level3, TransitionDuration);
                    var timeInPhase = currentPhase == 2 ? levelTime * (1f - phaseProgress) : levelTime;
                    yield return new WaitForSeconds(timeInPhase);
                }
            }

            // Finish 루프로 전환
            buildingAnimator.CrossFadeInFixedTime(AnimationState.Finish, TransitionDuration);

            constructionCoroutine = null;
        }
    }
}