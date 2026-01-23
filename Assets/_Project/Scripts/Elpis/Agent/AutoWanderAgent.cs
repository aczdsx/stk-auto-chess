using Cysharp.Threading.Tasks;
using Elpis.Agent;
using UnityEngine;
using UnityEngine.AI;

namespace Prototypes.Movement
{
    /// <summary>
    /// Controls the NavMeshAgent to automatically wander within a radius.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class AutoWanderAgent : MonoBehaviour
    {
        [SerializeField] private SpriteAgentView _view;

        [Header("Movement Settings")]
        [SerializeField] private float _moveSpeed = 5f;
        [SerializeField] private float _stoppingDistance = 0.1f;
        [SerializeField] private float _elevatorDetectRadius = 1.5f;

        [Header("Auto Wander Settings")]
        [SerializeField] private float _wanderRadius = 5f;
        [SerializeField] private float _minWaitTime = 2f;
        [SerializeField] private float _maxWaitTime = 5f;

        private NavMeshAgent _agent;
        private bool _isMoving;
        private bool _isOnElevator;
        private bool _isWaiting;
        private float _waitTimer;
        private Vector3 _lastDirection;
        private Vector3 _currentTargetPosition;

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            _agent.autoTraverseOffMeshLink = false; // 수동으로 OffMeshLink 처리
            _agent.updateRotation = false;
            _agent.speed = _moveSpeed;
            _agent.acceleration = 1000f;
            _agent.angularSpeed = 0f;
            _agent.stoppingDistance = _stoppingDistance;
            _agent.avoidancePriority = Random.Range(30, 70);
        }

        private bool TryGetRandomWanderPosition(out Vector3 result)
        {
            // 현재 위치 기준 랜덤 방향과 거리
            var randomDirection = Random.insideUnitSphere * _wanderRadius;
            var randomPosition = transform.position + randomDirection;

            // NavMesh 위의 유효한 위치 찾기
            if (NavMesh.SamplePosition(randomPosition, out var hit, _wanderRadius * 2f, NavMesh.AllAreas))
            {
                result = hit.position;
                return true;
            }

            result = Vector3.zero;
            return false;
        }

        private void Update()
        {
            _agent.transform.rotation = Quaternion.identity;

            // 엘리베이터 타는 중이면 무시
            if (_isOnElevator)
            {
                return;
            }

            // OffMeshLink 위에 도착하면 엘리베이터 처리
            if (_agent.isOnOffMeshLink)
            {
                HandleOffMeshLink().Forget();
                return;
            }

            UpdateAutoWander();
            UpdateMovementState();
        }

        private void UpdateAutoWander()
        {
            // 대기 중이면 타이머 감소
            if (_isWaiting)
            {
                _waitTimer -= Time.deltaTime;
                if (_waitTimer <= 0f)
                {
                    _isWaiting = false;
                    SetRandomDestination();
                }
                return;
            }

            // 이동 완료 체크 (OffMeshLink 앞에서 멈추지 않도록)
            bool hasOffMeshLinkAhead = _agent.nextOffMeshLinkData.valid;
            bool hasArrived = !_agent.pathPending &&
                              !_agent.isOnOffMeshLink &&
                              !hasOffMeshLinkAhead &&
                              (!_agent.hasPath || _agent.remainingDistance <= _stoppingDistance);

            if (hasArrived)
            {
                // 대기 시작
                _isWaiting = true;
                _waitTimer = Random.Range(_minWaitTime, _maxWaitTime);
                _agent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;
            }
        }

        private void SetRandomDestination()
        {
            if (TryGetRandomWanderPosition(out var targetPosition))
            {
                _currentTargetPosition = targetPosition;
                _agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
                _agent.SetDestination(targetPosition);
            }
        }

        private void UpdateMovementState()
        {
            // Agent가 이동 중인지 확인
            bool isAgentMoving = _agent.hasPath && _agent.remainingDistance > _stoppingDistance;

            if (isAgentMoving)
            {
                // 이동 방향 계산
                var velocity = _agent.velocity;
                if (velocity.sqrMagnitude > 0.01f)
                {
                    _lastDirection = velocity.normalized;
                    _view.LookAt(new Vector3(_lastDirection.x, _lastDirection.z, 0f));
                }

                if (!_isMoving)
                {
                    _view.PlayMoveAnimation();
                    _isMoving = true;
                }
            }
            else
            {
                if (_isMoving)
                {
                    _view.PlayIdleAnimation();
                    _isMoving = false;
                }
            }
        }

        private async UniTaskVoid HandleOffMeshLink()
        {
            _isOnElevator = true;

            var linkData = _agent.currentOffMeshLinkData;

            // OffMeshLink에 ElevatorLink 컴포넌트가 있는지 확인
            var linkComponent = linkData.owner as Component;
            var elevator = linkComponent?.GetComponent<ElevatorLink>();

            if (elevator != null)
            {
                // 엘리베이터로 이동
                await elevator.TransportAgent(_agent, _view);
            }
            else
            {
                // 일반 OffMeshLink: 단순히 끝점으로 이동
                var endPos = linkData.endPos;
                await MoveToPosition(endPos);
            }

            // OffMeshLink 통과 완료
            _agent.CompleteOffMeshLink();
            _isOnElevator = false;
        }

        private async UniTask MoveToPosition(Vector3 targetPos)
        {
            float duration = 0.3f;
            float elapsed = 0f;
            var startPos = transform.position;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                transform.position = Vector3.Lerp(startPos, targetPos, t);
                await UniTask.Yield();
            }

            transform.position = targetPos;
        }
    }
}
