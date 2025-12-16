using CookApps.AutoBattler;
using Cysharp.Threading.Tasks;
using Elpis.Agent;
using UnityEngine;
using UnityEngine.AI;

namespace Prototypes.Movement
{
    /// <summary>
    /// Controls the NavMeshAgent using mouse click input.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class AutoWanderAgent : MonoBehaviour
    {
        [SerializeField] private SpriteAgentView _view;
        [SerializeField] private Camera _camera;

        [Header("Movement Settings")]
        [SerializeField] private float _moveSpeed = 5f;
        [SerializeField] private float _stoppingDistance = 0.1f;
        [SerializeField] private float _elevatorDetectRadius = 1.5f;
        [SerializeField] private LayerMask _groundLayer = -1;

        private NavMeshAgent _agent;
        private bool _isMoving;
        private bool _isOnElevator;
        private Vector3 _lastDirection;

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            _agent.autoTraverseOffMeshLink = false; // 수동으로 OffMeshLink 처리
            _agent.updateRotation = false;
            _agent.speed = _moveSpeed;
            _agent.acceleration = 1000f;
            _agent.angularSpeed = 0f;
            _agent.stoppingDistance = _stoppingDistance;

            if (_camera == null)
                _camera = Camera.main;
        }

        private void Update()
        {
            _agent.transform.rotation = Quaternion.identity;
            // 엘리베이터 타는 중이면 입력 무시
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

            // 마우스 클릭으로 이동
            if (Input.GetMouseButtonDown(0))
            {
                TrySetDestination();
            }

            UpdateMovementState();
        }

        private void TrySetDestination()
        {
            var ray = MainCameraHolder.MainCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out var hit, 1000f, _groundLayer))
            {
                _agent.SetDestination(hit.point);
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
