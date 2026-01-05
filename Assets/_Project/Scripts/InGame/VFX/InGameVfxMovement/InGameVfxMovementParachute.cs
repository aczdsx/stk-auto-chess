using UnityEngine;

namespace CookApps.BattleSystem
{
    /// <summary>
    /// 패러슈트 VFX 이동 클래스
    /// XYZ 커브를 사용하여 이동과 회전을 완전히 제어
    /// </summary>
    public class InGameVfxMovementParachute : InGameVfxMovementBase
    {
        private float elapsedTime = 0f;
        public float duration { get; private set; } = 2.0f;
        
        // 커브 데이터 (null이면 기본 커브 사용)
        private ParachuteCurveData curveData = null;
        private AnimationCurve xCurve;
        private AnimationCurve ySpeedCurve; // Y축 속도 커브
        private AnimationCurve zCurve;
        private AnimationCurve rotationCurve;
        private bool useCustomCurves = false;
        
        // Y축 속도 적분을 위한 변수
        private float yProgress = 0f;
        private float ySpeedCurveIntegral = 1f; // 속도 커브의 총 적분값 (정규화용)
        private float ySpeedScaleFactor = 1f; // 속도 커브의 스케일 팩터 (절대값 반영용)

        // CharacterController 추적 설정
        private bool trackCharacter = false;
        private float trackingYOffset = 1.6f;
        private Quaternion initialRotation = Quaternion.identity;

        private Transform _transform;
        private CharacterController _characterController;

        // 기본 커브 생성
        private static AnimationCurve CreateDefaultXCurve() => AnimationCurve.Linear(0, 0, 1, 1);
        private static AnimationCurve CreateDefaultYSpeedCurve() => CreateDefaultElasticSpeedCurve(); // 탄성 효과 기본 커브
        private static AnimationCurve CreateDefaultZCurve() => AnimationCurve.Linear(0, 0, 1, 1);
        private static AnimationCurve CreateDefaultRotationCurve() => AnimationCurve.Constant(0, 1, 0);
        
        /// <summary>
        /// 탄성 효과를 위한 기본 속도 커브 생성 (공중에서 튕기고 천천히 떨어짐)
        /// </summary>
        private static AnimationCurve CreateDefaultElasticSpeedCurve()
        {
            AnimationCurve curve = new AnimationCurve();
            // 0.0 ~ 0.1: 공중에서 위로 튕김 (-4.0)
            curve.AddKey(0.0f, -4.0f);
            curve.AddKey(0.1f, -4.0f);
            // 0.1 ~ 0.9: 천천히 떨어짐 (0.3)
            curve.AddKey(0.1f, 0.3f);
            curve.AddKey(0.9f, 0.3f);
            // 0.9 ~ 1.0: 바닥에서 튕김 (-1.5)
            curve.AddKey(0.9f, -1.5f);
            curve.AddKey(1.0f, -1.5f);
            return curve;
        }

        public override void SetData(Vector3 srcPos, Vector3 destPos, float speed)
        {
            base.SetData(srcPos, destPos, speed);
            this.duration = 2.0f;
            this.curveData = null;
            this.useCustomCurves = false;
            InitializeCurves();
            InitializeParachuteData();
        }

        /// <summary>
        /// Transform을 사용하는 오버로드 (테스터용)
        /// </summary>
        public void SetData(Transform transform, Vector3 srcPos, Vector3 destPos, float duration = 2.0f)
        {
            base.SetData(srcPos, destPos, 1.0f);
            this._transform = transform;
            this._characterController = null;
            this.duration = duration;
            this.curveData = null;
            this.useCustomCurves = false;
            this.trackCharacter = false;
            InitializeCurves();
            InitializeParachuteData();
        }

        /// <summary>
        /// CharacterController를 추적하는 오버로드
        /// </summary>
        public void SetData(CharacterController characterController, Vector3 srcPos, float duration = 2.0f)
        {
            base.SetData(srcPos, characterController != null ? characterController.Position3D : srcPos, 1.0f);
            this._characterController = characterController;
            this._transform = null;
            this.duration = duration;
            this.curveData = null;
            this.useCustomCurves = false;
            this.trackCharacter = characterController != null;
            this.trackingYOffset = 1.6f;
            InitializeCurves();
            InitializeParachuteData();
        }

        /// <summary>
        /// ParachuteCurveData를 사용하는 오버로드
        /// </summary>
        public void SetData(ParachuteCurveData curveData, Vector3 srcPos, Vector3 destPos, float duration = -1f)
        {
            base.SetData(srcPos, destPos, 1.0f);
            this._transform = null;
            this._characterController = null;
            this.curveData = curveData;
            this.duration = duration > 0
                ? duration
                : ((curveData != null && curveData.duration > 0f) ? curveData.duration : 2.0f);
            this.useCustomCurves = curveData != null;
            this.trackCharacter = curveData != null && curveData.trackCharacter;
            this.trackingYOffset = curveData != null ? curveData.trackingYOffset : 1.6f;
            InitializeCurves();
            InitializeParachuteData();
        }

        /// <summary>
        /// ParachuteCurveData와 Transform을 사용하는 오버로드 (테스터용)
        /// </summary>
        public void SetData(ParachuteCurveData curveData, Transform transform, Vector3 srcPos, Vector3 destPos, float duration = -1f)
        {
            base.SetData(srcPos, destPos, 1.0f);
            this._transform = transform;
            this._characterController = null;
            this.curveData = curveData;
            this.duration = duration > 0
                ? duration
                : ((curveData != null && curveData.duration > 0f) ? curveData.duration : 2.0f);
            this.useCustomCurves = curveData != null;
            this.trackCharacter = false; // Transform 사용 시 추적 안 함
            InitializeCurves();
            InitializeParachuteData();
        }

        /// <summary>
        /// ParachuteCurveData와 CharacterController를 사용하는 오버로드
        /// </summary>
        public void SetData(ParachuteCurveData curveData, CharacterController characterController, Vector3 srcPos, float duration = -1f)
        {
            base.SetData(srcPos, characterController != null ? characterController.Position3D : srcPos, 1.0f);
            this._characterController = characterController;
            this._transform = null;
            this.curveData = curveData;
            this.duration = duration > 0
                ? duration
                : ((curveData != null && curveData.duration > 0f) ? curveData.duration : 2.0f);
            this.useCustomCurves = curveData != null;
            this.trackCharacter = true;
            this.trackingYOffset = curveData != null ? curveData.trackingYOffset : 1.6f;
            InitializeCurves();
            InitializeParachuteData();
        }

        private void InitializeCurves()
        {
            if (useCustomCurves && curveData != null)
            {
                xCurve = curveData.xCurve != null && curveData.xCurve.length > 0 
                    ? curveData.xCurve 
                    : CreateDefaultXCurve();
                ySpeedCurve = curveData.yCurve != null && curveData.yCurve.length > 0 
                    ? curveData.yCurve 
                    : CreateDefaultYSpeedCurve();
                zCurve = curveData.zCurve != null && curveData.zCurve.length > 0 
                    ? curveData.zCurve 
                    : CreateDefaultZCurve();
                rotationCurve = curveData.rotationCurve != null && curveData.rotationCurve.length > 0 
                    ? curveData.rotationCurve 
                    : CreateDefaultRotationCurve();
            }
            else
            {
                xCurve = CreateDefaultXCurve();
                ySpeedCurve = CreateDefaultYSpeedCurve();
                zCurve = CreateDefaultZCurve();
                rotationCurve = CreateDefaultRotationCurve();
            }
            
            // Y축 속도 커브의 총 적분값 계산 (정규화용)
            CalculateYSpeedCurveIntegral();
        }
        
        /// <summary>
        /// Y축 속도 커브의 총 적분값을 계산하여 정규화에 사용
        /// duration 끝에 목표 위치에 도달하도록 스케일링 팩터 계산
        /// </summary>
        private void CalculateYSpeedCurveIntegral()
        {
            ySpeedCurveIntegral = 0f;
            float maxSpeed = 0f;
            int integrationSteps = 100;
            float stepSize = 1f / integrationSteps;
            
            for (int i = 0; i < integrationSteps; i++)
            {
                float t = (i + 0.5f) * stepSize; // 중점 법칙
                float speed = ySpeedCurve.Evaluate(t);
                ySpeedCurveIntegral += speed * stepSize;
                maxSpeed = Mathf.Max(maxSpeed, speed);
            }
            
            // 적분값이 0이면 기본값 1 사용 (속도 커브가 모두 0인 경우 방지)
            if (ySpeedCurveIntegral <= 0f)
            {
                ySpeedCurveIntegral = 1f;
                ySpeedScaleFactor = 1f;
            }
            else
            {
                // 속도 커브의 최대값을 스케일 팩터로 사용 (절대값 반영)
                // 최대값이 높을수록 더 빠르게 움직임
                ySpeedScaleFactor = Mathf.Max(1f, maxSpeed);
            }
        }

        private void InitializeParachuteData()
        {
            // duration이 0 이하로 내려가는 상황 방지 (커브 데이터가 0이거나 미설정인 경우)
            if (duration <= 0f)
            {
                duration = (curveData != null && curveData.duration > 0f) ? curveData.duration : 2.0f;
            }

            elapsedTime = 0;
            yProgress = 0f; // Y 진행률 초기화
            currPos = srcPos;
            
            if (_transform != null)
            {
                initialRotation = _transform.rotation;
            }
        }

        public override void Clear()
        {
            base.Clear();
            _transform = null;
            _characterController = null;
            curveData = null;
            xCurve = null;
            ySpeedCurve = null;
            zCurve = null;
            rotationCurve = null;
        }

        public override void ManagedUpdate(float dt)
        {
            // CharacterController를 추적하는 경우 destPos를 업데이트
            if (trackCharacter && _characterController != null)
            {
                if (_characterController.IsAlive == false)
                {
                    _characterController = null;
                }
                else
                {
                    destPos = _characterController.Position3D + Vector3.up * trackingYOffset;
                }
            }

            elapsedTime += dt;
            
            // duration 후에 종료
            if (elapsedTime >= duration)
            {
                currPos = destPos;
                if (_transform != null)
                {
                    _transform.rotation = initialRotation;
                }

                InvokeReachedTarget();
                return;
            }

            // 정규화된 시간 (0~1)
            float t = Mathf.Clamp01(elapsedTime / duration);
            
            // X, Z는 위치 커브 사용
            float easedX = xCurve.Evaluate(t);
            float easedZ = zCurve.Evaluate(t);
            
            // Y축은 속도 커브 사용 (속도를 적분하여 진행률 계산)
            // 속도 커브의 절대값을 반영하면서 duration 끝에 도달하도록 정규화
            // 음수 속도는 위로 올라가는 효과 (탄성, 튕김)
            float ySpeed = ySpeedCurve.Evaluate(t);
            
            // 음수 속도 처리: 위로 올라가는 효과
            if (ySpeed < 0f)
            {
                // 음수 속도는 진행률을 감소시킴 (위로 올라감)
                // 절대값을 사용하되 정규화 적용
                float absSpeed = Mathf.Abs(ySpeed);
                float normalizedSpeed = absSpeed / ySpeedCurveIntegral;
                float scaledSpeed = normalizedSpeed * ySpeedScaleFactor;
                yProgress -= scaledSpeed * dt / duration; // 진행률 감소 (위로 올라감)
            }
            else
            {
                // 양수 속도: 아래로 떨어지는 효과
                float normalizedSpeed = ySpeed / ySpeedCurveIntegral;
                float scaledSpeed = normalizedSpeed * ySpeedScaleFactor;
                yProgress += scaledSpeed * dt / duration; // 진행률 증가 (아래로 떨어짐)
            }
            
            yProgress = Mathf.Clamp01(yProgress);
            
            // 위치 업데이트
            prevPos = currPos;
            currPos.x = Mathf.Lerp(srcPos.x, destPos.x, easedX);
            currPos.y = Mathf.Lerp(srcPos.y, destPos.y, yProgress); // 속도 기반 진행률 사용
            currPos.z = Mathf.Lerp(srcPos.z, destPos.z, easedZ);

            // 회전 계산 (커브에서 각도 가져오기)
            float rotationAngle = rotationCurve.Evaluate(t);

            // 트랜스폼 적용
            if (_transform != null)
            {
                _transform.position = currPos;
                // 기존 rotation에 커브에서 가져온 회전을 추가
                _transform.rotation = initialRotation * Quaternion.Euler(0, 0, rotationAngle);
            }
        }
    }
}
