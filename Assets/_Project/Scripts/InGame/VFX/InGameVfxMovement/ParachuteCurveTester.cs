using UnityEngine;

namespace CookApps.BattleSystem
{
    /// <summary>
    /// 에디터에서 패러슈트 커브를 테스트하기 위한 컴포넌트
    /// 플레이 모드 없이도 시뮬레이션 가능
    /// </summary>
    [ExecuteAlways]
    public class ParachuteCurveTester : MonoBehaviour
    {
        [Header("Curve Data")]
        [Tooltip("테스트할 커브 데이터")]
        public ParachuteCurveData curveData;

        [Header("Test Positions")]
        [Tooltip("시작 위치")]
        public Vector3 startPosition = new Vector3(0, 5, 0);

        [Tooltip("목표 위치")]
        public Vector3 targetPosition = new Vector3(0, 0, 0);

        [Header("Runtime Info")]
        [SerializeField, Tooltip("현재 테스트 중인지")]
        public bool isTesting = false;

        [SerializeField, Tooltip("경과 시간")]
        public float _testDuration = 0f;

        [Header("Scrub")]
        [Tooltip("테스트 중지 상태에서 타임라인 스크럽 (0~1)")]
        public float scrubT = 0f;

        private InGameVfxMovementParachute _movement;
        private Quaternion _initialRotation;
        private bool _hasInitialRotation = false;

#if UNITY_EDITOR
        private double _lastUpdateTime = 0.0;
        private bool _isEditorUpdateRegistered = false;

        private void OnEnable()
        {
            RegisterEditorUpdate();
        }

        private void OnDisable()
        {
            UnregisterEditorUpdate();
        }

        private void RegisterEditorUpdate()
        {
            if (!_isEditorUpdateRegistered)
            {
                UnityEditor.EditorApplication.update += EditorUpdate;
                _isEditorUpdateRegistered = true;
                _lastUpdateTime = UnityEditor.EditorApplication.timeSinceStartup;
            }
        }

        private void UnregisterEditorUpdate()
        {
            if (_isEditorUpdateRegistered)
            {
                UnityEditor.EditorApplication.update -= EditorUpdate;
                _isEditorUpdateRegistered = false;
            }
        }

        private void EditorUpdate()
        {
            if (isTesting && _movement != null && transform != null)
            {
                double currentTime = UnityEditor.EditorApplication.timeSinceStartup;
                float deltaTime = (float)(currentTime - _lastUpdateTime);
                
                // 첫 프레임이거나 deltaTime이 너무 크면 제한
                if (deltaTime <= 0f || deltaTime > 0.1f)
                {
                    deltaTime = 0.016f; // 약 60fps
                }
                
                _lastUpdateTime = currentTime;

                // 이동 업데이트 (InGameVfxMovementParachute가 _transform을 직접 업데이트함)
                _movement.ManagedUpdate(deltaTime);
                _testDuration += deltaTime;

                // 씬 뷰 리페인트 (매 프레임)
                UnityEditor.SceneView.RepaintAll();
                UnityEditor.EditorUtility.SetDirty(this);

                // 목표 도달 확인
                if (_movement.duration > 0f && _testDuration >= _movement.duration)
                {
                    StopTest();
                }
            }
            else if (!isTesting)
            {
                // 테스트가 아닐 때는 시간만 업데이트
                _lastUpdateTime = UnityEditor.EditorApplication.timeSinceStartup;
            }
        }
#endif

        /// <summary>
        /// 테스트 시작
        /// </summary>
        public void StartTest()
        {
            if (curveData == null)
            {
                Debug.LogWarning("ParachuteCurveTester: curveData가 설정되지 않았습니다.");
                return;
            }

            if (_movement == null)
            {
                _movement = new InGameVfxMovementParachute();
            }

            _initialRotation = transform.rotation;
            _hasInitialRotation = true;
            _movement.SetData(curveData, transform, startPosition, targetPosition);
            _movement.OnReachedTarget += StopTest;

            transform.position = startPosition;
            transform.rotation = _initialRotation;
            isTesting = true;
            _testDuration = 0f;
            scrubT = 0f;

#if UNITY_EDITOR
            // EditorUpdate 등록 확인
            RegisterEditorUpdate();
            _lastUpdateTime = UnityEditor.EditorApplication.timeSinceStartup;
            
            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.SceneView.RepaintAll();
            
            Debug.Log($"ParachuteCurveTester: 테스트 시작 - duration={_movement.duration}, curveData.duration={curveData.duration}");
#endif
        }

        /// <summary>
        /// 테스트 중지
        /// </summary>
        public void StopTest()
        {
            if (_movement != null)
            {
                _movement.OnReachedTarget -= StopTest;
            }

            isTesting = false;
            _testDuration = 0f;
            scrubT = 0f;

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.SceneView.RepaintAll();
#endif
        }

        /// <summary>
        /// 테스트 리셋 (시작 위치로 복귀)
        /// </summary>
        public void ResetTest()
        {
            StopTest();
            transform.position = startPosition;
            transform.rotation = _initialRotation;
            scrubT = 0f;

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.SceneView.RepaintAll();
#endif
        }

        /// <summary>
        /// 테스트 중지 상태에서 커브 위치를 스크럽해 미리보기
        /// </summary>
        /// <param name="t">0~1 구간</param>
        public void ApplyScrub(float t)
        {
#if UNITY_EDITOR
            if (curveData == null || transform == null)
                return;

            // 진행 중이면 스크럽 금지
            if (isTesting)
                return;

            scrubT = Mathf.Clamp01(t);

            if (!_hasInitialRotation)
            {
                _initialRotation = transform.rotation;
                _hasInitialRotation = true;
            }

            // 커브 fallback
            AnimationCurve xCurve = (curveData.xCurve != null && curveData.xCurve.length > 0)
                ? curveData.xCurve : AnimationCurve.Linear(0, 0, 1, 1);
            AnimationCurve yCurve = (curveData.yCurve != null && curveData.yCurve.length > 0)
                ? curveData.yCurve : AnimationCurve.EaseInOut(0, 0, 1, 1);
            AnimationCurve zCurve = (curveData.zCurve != null && curveData.zCurve.length > 0)
                ? curveData.zCurve : AnimationCurve.Linear(0, 0, 1, 1);
            AnimationCurve rCurve = (curveData.rotationCurve != null && curveData.rotationCurve.length > 0)
                ? curveData.rotationCurve : AnimationCurve.Constant(0, 1, 0);

            float easedX = xCurve.Evaluate(scrubT);
            float easedY = 1f - yCurve.Evaluate(scrubT); // 위(1)에서 아래(0)로 떨어지도록 반전
            float easedZ = zCurve.Evaluate(scrubT);
            float rotZ = rCurve.Evaluate(scrubT);

            Vector3 pos;
            pos.x = Mathf.Lerp(startPosition.x, targetPosition.x, easedX);
            pos.y = Mathf.Lerp(startPosition.y, targetPosition.y, easedY);
            pos.z = Mathf.Lerp(startPosition.z, targetPosition.z, easedZ);

            transform.position = pos;
            transform.rotation = _initialRotation * Quaternion.Euler(0f, 0f, rotZ);

            UnityEditor.SceneView.RepaintAll();
            UnityEditor.EditorUtility.SetDirty(transform);
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }

        private void OnDestroy()
        {
            StopTest();
#if UNITY_EDITOR
            UnregisterEditorUpdate();
#endif
            if (_movement != null)
            {
                _movement.Clear();
                _movement = null;
            }
        }
    }
}

