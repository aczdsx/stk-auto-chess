using Cysharp.Threading.Tasks;
using Elpis.Agent;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

namespace Prototypes.Movement
{
    /// <summary>
    /// NavMeshLink 위에 배치하여 엘리베이터 동작을 처리
    /// </summary>
    [RequireComponent(typeof(NavMeshLink))]
    public class ElevatorLink : MonoBehaviour
    {
        [SerializeField] private NavMeshLink link;
        [SerializeField] private Transform platform; // 엘리베이터 플랫폼
        [SerializeField] private Transform platformCenterPoint;
        [SerializeField] private Transform startPoint;
        [SerializeField] private Transform startPlatformPoint;
        [SerializeField] private Transform endPoint;
        [SerializeField] private Transform endPlatformPoint;

        [Header("Timing Settings")]
        [SerializeField] private float platformArriveDuration = 1f; // 플랫폼이 Agent에게 오는 시간
        [SerializeField] private float boardingDuration = 0.5f; // Agent가 플랫폼에 탑승하는 시간
        [SerializeField] private float moveDuration = 2f; // 엘리베이터 이동 시간
        [SerializeField] private float exitDuration = 0.5f; // Agent가 플랫폼에서 하차하는 시간

        private bool isMoving;

        private void Awake()
        {
            link.startTransform = startPoint;
            link.endTransform = endPoint;
        }

        public void ActivateElevator()
        {
            link.activated = true;
        }
        
        /// <summary>
        /// Agent를 엘리베이터로 이동시킴
        /// </summary>
        public async UniTask TransportAgent(NavMeshAgent agent, SpriteAgentView view)
        {
            if (isMoving)
                return;

            isMoving = true;

            var agentTransform = agent.transform;
            var agentStartPos = agentTransform.position;

            // 목적지 결정 (가까운 쪽에서 먼 쪽으로)
            var distToStart = Vector3.Distance(agentStartPos, startPoint.position);
            var distToEnd = Vector3.Distance(agentStartPos, endPoint.position);
            bool isStartNear = distToStart < distToEnd;
            var farPoint = isStartNear ? endPoint : startPoint;
            var nearPlatformLocalPoint = isStartNear ? startPlatformPoint : endPlatformPoint;
            var farPlatformLocalPoint = isStartNear ? endPlatformPoint : startPlatformPoint;

            // Agent의 NavMesh 업데이트 비활성화 (수동으로 위치 제어)
            agent.updatePosition = false;
            agent.updateRotation = false;
            agent.velocity = Vector3.zero;

            // 대기 상태 애니메이션 (IDLE)
            view.PlayIdleAnimation();

            // 1. 플랫폼이 Agent 위치(가까운 층)로 이동
            var platformStartPos = platform.position;
            var platformArrivePos = nearPlatformLocalPoint.position;

            if (Vector3.Distance(platformStartPos, platformArrivePos) > 0.1f)
            {
                await MovePlatform(platformStartPos, platformArrivePos, platformArriveDuration);
            }

            // 2. Agent가 플랫폼 중앙으로 탑승 (MOVE)
            var boardingTargetPos = platformCenterPoint.position; // 수정: 플랫폼 중앙으로 이동
            var boardingDir = (boardingTargetPos - agentStartPos).normalized;
            if (boardingDir != Vector3.zero)
                view.LookAt(new Vector3(boardingDir.x, boardingDir.z, 0f));

            view.PlayMoveAnimation();
            await MoveAgent(agentTransform, agentStartPos, boardingTargetPos, boardingDuration);

            // 3. 플랫폼과 Agent가 함께 목적지로 이동 (IDLE)
            view.PlayIdleAnimation();
            var platformTargetPos = farPlatformLocalPoint.position;
            await MovePlatformWithAgent(agentTransform, platform.position, platformTargetPos, moveDuration);

            // 4. Agent가 플랫폼에서 하차 (MOVE)
            var exitTargetPos = farPoint.position;
            var currentAgentPos = agentTransform.position;
            var exitDir = (exitTargetPos - currentAgentPos).normalized;
            if (exitDir != Vector3.zero)
                view.LookAt(new Vector3(exitDir.x, exitDir.z, 0f));

            view.PlayMoveAnimation();
            await MoveAgent(agentTransform, currentAgentPos, exitTargetPos, exitDuration);

            // 최종 위치 보정
            agentTransform.position = exitTargetPos;

            // Agent의 NavMesh 위치 동기화 및 업데이트 재활성화
            agent.nextPosition = exitTargetPos;
            agent.updatePosition = true;
            agent.updateRotation = false; // 회전은 계속 수동 (SpriteAgentView에서 관리)

            isMoving = false;
        }

        private async UniTask MovePlatform(Vector3 from, Vector3 to, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                platform.position = Vector3.Lerp(from, to, t);
                await UniTask.Yield();
            }
            platform.position = to;
        }

        private async UniTask MoveAgent(Transform agentTransform, Vector3 from, Vector3 to, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                agentTransform.position = Vector3.Lerp(from, to, t);
                await UniTask.Yield();
            }
            agentTransform.position = to;
        }

        private async UniTask MovePlatformWithAgent(Transform agentTransform, Vector3 platformFrom, Vector3 platformTo, float duration)
        {
            float elapsed = 0f;
            var agentOffset = agentTransform.position - platform.position;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);

                platform.position = Vector3.Lerp(platformFrom, platformTo, t);
                agentTransform.position = platform.position + agentOffset;

                await UniTask.Yield();
            }

            platform.position = platformTo;
            agentTransform.position = platformTo + agentOffset;
        }
    }
}
