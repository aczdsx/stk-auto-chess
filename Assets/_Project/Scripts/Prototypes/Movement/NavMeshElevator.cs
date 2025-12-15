using System.Collections;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

namespace Prototypes.Movement
{
    /// <summary>
    /// A simple elevator system using NavMeshLink.
    /// 1. Connects two floors with a NavMeshLink.
    /// 2. When an Agent uses the link, it pauses the agent, moves the platform, and then resumes the agent.
    /// </summary>
    [RequireComponent(typeof(NavMeshLink))]
    public class NavMeshElevator : MonoBehaviour
    {
        [Header("Elevator Settings")]
        [SerializeField] private Transform _platform;
        [SerializeField] private float _moveDuration = 2.0f;
        [SerializeField] private float _waitDuration = 1.0f;

        private NavMeshLink _link;
        private bool _isMoving = false;

        private void Awake()
        {
            _link = GetComponent<NavMeshLink>();
        }

        private IEnumerator Start()
        {
            // Continuously check for agents trying to traverse this link
            while (true)
            {
                yield return new WaitForSeconds(0.1f);
                CheckForPassengers();
            }
        }

        private void CheckForPassengers()
        {
            // Find all agents in the scene (In a real game, manage a list or use triggers)
            var agents = FindObjectsByType<NavMeshAgent>(FindObjectsSortMode.None);

            foreach (var agent in agents)
            {
                if (agent.isOnOffMeshLink && !_isMoving)
                {
                    // Check if this agent is on OUR link
                    var data = agent.currentOffMeshLinkData;
                    if (data.offMeshLink == null) continue; // It's a generated link, not ours (or NavMeshLink is not OffMeshLink component)

                    // Note: NavMeshLink creates internal OffMeshLinks. Matching positions is safer.
                    if (Vector3.Distance(transform.position, data.startPos) < 1f ||
                        Vector3.Distance(transform.position, data.endPos) < 1f)
                    {
                        StartCoroutine(ProcessElevatorRide(agent));
                        return; // Handle one at a time for this prototype
                    }
                }
            }
        }

        private IEnumerator ProcessElevatorRide(NavMeshAgent agent)
        {
            _isMoving = true;

            // 1. Determine direction (Start -> End or End -> Start)
            Vector3 startPos = agent.transform.position;
            Vector3 endPos = Vector3.Distance(startPos, _link.startPoint) < Vector3.Distance(startPos, _link.endPoint)
                ? transform.TransformPoint(_link.endPoint)
                : transform.TransformPoint(_link.startPoint);

            // 2. Parent agent to platform so it moves with it
            agent.transform.SetParent(_platform);

            // 3. Move Platform
            float time = 0;
            Vector3 platformStart = _platform.position;
            Vector3 platformEnd = new Vector3(platformStart.x, endPos.y, platformStart.z); // Simple vertical move

            while (time < _moveDuration)
            {
                _platform.position = Vector3.Lerp(platformStart, platformEnd, time / _moveDuration);
                time += Time.deltaTime;
                yield return null;
            }
            _platform.position = platformEnd;

            // 4. Wait a bit
            yield return new WaitForSeconds(_waitDuration);

            // 5. Unparent and Complete Link
            agent.transform.SetParent(null);
            agent.CompleteOffMeshLink();

            _isMoving = false;
        }
    }
}
