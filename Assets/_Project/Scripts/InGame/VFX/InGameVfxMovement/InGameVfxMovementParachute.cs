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
        private AnimationCurve yCurve;
        private AnimationCurve zCurve;
        private AnimationCurve rotationCurve;
        private bool useCustomCurves = false;

        // CharacterController 추적 설정
        private bool trackCharacter = false;
        private float trackingYOffset = 1.6f;
        private Quaternion initialRotation = Quaternion.identity;

        private Transform _transform;
        private CharacterController _characterController;

        // 기본 커브 생성
        private static AnimationCurve CreateDefaultXCurve() => AnimationCurve.Linear(0, 0, 1, 1);
        private static AnimationCurve CreateDefaultYCurve() => AnimationCurve.EaseInOut(0, 0, 1, 1);
        private static AnimationCurve CreateDefaultZCurve() => AnimationCurve.Linear(0, 0, 1, 1);
        private static AnimationCurve CreateDefaultRotationCurve() => AnimationCurve.Constant(0, 1, 0);

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
            this.trackCharacter = (curveData != null && curveData.trackCharacter) && characterController != null;
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
                yCurve = curveData.yCurve != null && curveData.yCurve.length > 0 
                    ? curveData.yCurve 
                    : CreateDefaultYCurve();
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
                yCurve = CreateDefaultYCurve();
                zCurve = CreateDefaultZCurve();
                rotationCurve = CreateDefaultRotationCurve();
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
            yCurve = null;
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
            
            // 커브를 사용하여 위치 계산
            // Y축은 낙하산이 위에서 아래로 떨어지므로 커브를 반대로 적용 (1 - easedY)
            float easedX = xCurve.Evaluate(t);
            float easedY = 1f - yCurve.Evaluate(t); // 위(1)에서 아래(0)로 떨어지도록 반전
            float easedZ = zCurve.Evaluate(t);
            
            // 위치 업데이트
            prevPos = currPos;
            currPos.x = Mathf.Lerp(srcPos.x, destPos.x, easedX);
            currPos.y = Mathf.Lerp(srcPos.y, destPos.y, easedY);
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
