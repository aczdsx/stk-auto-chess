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

            // 기존 테스트가 진행 중이면 먼저 중지
            if (isTesting)
            {
                StopTest();
            }

            // Movement 초기화 (기존 것이 있으면 정리)
            if (_movement != null)
            {
                _movement.OnReachedTarget -= StopTest;
                _movement.Clear();
            }
            _movement = new InGameVfxMovementParachute();

            // 위치와 회전을 먼저 시작 위치로 초기화
            _initialRotation = transform.rotation;
            _hasInitialRotation = true;
            transform.position = startPosition;
            transform.rotation = _initialRotation;

            // Movement 데이터 설정
            _movement.SetData(curveData, transform, startPosition, targetPosition);
            _movement.OnReachedTarget += StopTest;

            // 테스트 상태 초기화
            isTesting = true;
            _testDuration = 0f;
            scrubT = 0f;

#if UNITY_EDITOR
            // EditorUpdate 등록 확인
            RegisterEditorUpdate();
            _lastUpdateTime = UnityEditor.EditorApplication.timeSinceStartup;
            
            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.SceneView.RepaintAll();
            
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
            AnimationCurve ySpeedCurve = (curveData.yCurve != null && curveData.yCurve.length > 0)
                ? curveData.yCurve : AnimationCurve.Constant(0, 1, 1f);
            AnimationCurve zCurve = (curveData.zCurve != null && curveData.zCurve.length > 0)
                ? curveData.zCurve : AnimationCurve.Linear(0, 0, 1, 1);
            AnimationCurve rCurve = (curveData.rotationCurve != null && curveData.rotationCurve.length > 0)
                ? curveData.rotationCurve : AnimationCurve.Constant(0, 1, 0);

            float easedX = xCurve.Evaluate(scrubT);
            float easedZ = zCurve.Evaluate(scrubT);
            float rotZ = rCurve.Evaluate(scrubT);

            // Y축 속도 커브를 적분하여 진행률 계산
            // 먼저 총 적분값 계산 (정규화용, 절대값 사용)
            float totalIntegral = 0f;
            int integrationSteps = 100;
            float totalStepSize = 1f / integrationSteps;
            for (int i = 0; i < integrationSteps; i++)
            {
                float timeValue = (i + 0.5f) * totalStepSize;
                float speed = ySpeedCurve.Evaluate(timeValue);
                totalIntegral += Mathf.Abs(speed) * totalStepSize; // 절대값 사용
            }
            if (totalIntegral <= 0f)
                totalIntegral = 1f;
            
            // scrubT까지의 적분값 계산 (음수 속도는 위로 올라감)
            float yProgress = 0f;
            float stepSize = scrubT / integrationSteps;
            for (int i = 0; i < integrationSteps; i++)
            {
                float timeStep = (i + 0.5f) * stepSize;
                float speed = ySpeedCurve.Evaluate(timeStep);
                if (speed < 0f)
                {
                    // 음수 속도: 위로 올라감 (진행률 감소)
                    yProgress -= Mathf.Abs(speed) * stepSize;
                }
                else
                {
                    // 양수 속도: 아래로 떨어짐 (진행률 증가)
                    yProgress += speed * stepSize;
                }
            }
            
            // 정규화하여 항상 scrubT=1일 때 yProgress=1이 되도록
            yProgress = Mathf.Clamp01(yProgress / totalIntegral);

            Vector3 pos;
            pos.x = Mathf.Lerp(startPosition.x, targetPosition.x, easedX);
            pos.y = Mathf.Lerp(startPosition.y, targetPosition.y, yProgress);
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

