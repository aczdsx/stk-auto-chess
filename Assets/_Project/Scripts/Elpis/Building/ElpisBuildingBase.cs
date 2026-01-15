using System.Collections;
using CookApps.TeamBattle;
using Tech.Hive.V1;
using UnityEngine;

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

        public int SlotIndex { get; private set; }
        public ElpisFacilityType BuildingType { get; private set; }

        private UnityEngine.Coroutine constructionCoroutine;

        public void Initialize(int slotIndex)
        {
            SlotIndex = slotIndex;
        }

        /// <summary>
        /// 건설/업그레이드 애니메이션을 시작합니다.
        /// </summary>
        /// <param name="remainingTime">남은 건설 시간 (초)</param>
        /// <param name="totalBuildTime">총 건설 시간 (초)</param>
        public void StartConstructionAnimation(float remainingTime, float totalBuildTime)
        {
            StopConstructionAnimation();
            constructionCoroutine = StartCoroutine(PlayConstructionSequence(remainingTime, totalBuildTime));
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
        /// Disappear 애니메이션을 재생합니다.
        /// </summary>
        public void PlayDisappearAnimation()
        {
            StopConstructionAnimation();

            if (buildingAnimator != null)
            {
                buildingAnimator.Play(AnimationState.Disappear);
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

        private IEnumerator PlayConstructionSequence(float remainingTime, float totalBuildTime)
        {
            if (buildingAnimator == null)
                yield break;

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

            // Finish 루프로 전환
            buildingAnimator.CrossFadeInFixedTime(AnimationState.Finish, TransitionDuration);

            constructionCoroutine = null;
        }
    }
}