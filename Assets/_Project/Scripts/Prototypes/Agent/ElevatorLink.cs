using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AI;

namespace Prototypes.Movement
{
    /// <summary>
    /// NavMeshLink 위에 배치하여 엘리베이터 동작을 처리
    /// </summary>
    public class ElevatorLink : MonoBehaviour
    {
        [SerializeField] private Transform _platform; // 엘리베이터 플랫폼
        [SerializeField] private Transform _startPoint;
        [SerializeField] private Transform _endPoint;
        [SerializeField] private float _moveDuration = 2f;

        private bool _isMoving;

        /// <summary>
        /// 현재 위치에서 반대편 목적지를 반환
        /// </summary>
        public Vector3 GetTargetPosition(Vector3 currentPos)
        {
            var distToStart = Vector3.Distance(currentPos, _startPoint.position);
            var distToEnd = Vector3.Distance(currentPos, _endPoint.position);
            return distToStart < distToEnd ? _endPoint.position : _startPoint.position;
        }

        /// <summary>
        /// Agent를 엘리베이터로 이동시킴
        /// </summary>
        public async UniTask TransportAgent(NavMeshAgent agent)
        {
            if (_isMoving)
                return;

            _isMoving = true;

            // Agent의 NavMesh 업데이트 비활성화 (수동으로 위치 제어)
            agent.updatePosition = false;
            agent.updateRotation = false;

            // Agent를 플랫폼에 붙이기
            var agentTransform = agent.transform;
            var startPos = agent.transform.position;

            // 목적지 결정 (가까운 쪽에서 먼 쪽으로)
            var distToStart = Vector3.Distance(startPos, _startPoint.position);
            var distToEnd = Vector3.Distance(startPos, _endPoint.position);
            var targetPoint = distToStart < distToEnd ? _endPoint : _startPoint;

            // 엘리베이터 이동
            float elapsed = 0f;
            var platformStartPos = _platform.position;
            var platformEndPos = targetPoint.position;

            // 플랫폼 시작 위치로 이동 (Agent 위치 기준)
            _platform.position = new Vector3(_platform.position.x, startPos.y, _platform.position.z);

            while (elapsed < _moveDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / _moveDuration);

                // 플랫폼 이동
                var newY = Mathf.Lerp(startPos.y, targetPoint.position.y, t);
                _platform.position = new Vector3(_platform.position.x, newY, _platform.position.z);

                // Agent도 함께 이동
                agentTransform.position = new Vector3(agentTransform.position.x, newY, agentTransform.position.z);

                await UniTask.Yield();
            }

            // 최종 위치 보정
            agentTransform.position = new Vector3(agentTransform.position.x, targetPoint.position.y, agentTransform.position.z);

            // Agent의 NavMesh 위치 동기화 및 업데이트 재활성화
            agent.nextPosition = agentTransform.position;
            agent.updatePosition = true;
            agent.updateRotation = false; // 회전은 계속 수동

            _isMoving = false;
        }
    }
}
