using UnityEngine;

namespace CookApps.BattleSystem
{
    public class InGameVfxMovementParachute : InGameVfxMovementBase
    {
        private float elapsedTime = 0f;
        private float duration = 2.0f; // 2초 고정

        [Header("Swing Settings")]
        private float swingFrequency = 5f; // 흔들림 속도
        private float maxSwingAngle = 50f;  // 최대 흔들림 각도

        private Transform _transform;
        private CharacterController _characterController;

        public override void SetData(Vector3 srcPos, Vector3 destPos, float speed)
        {
            // speed 대신 2초 고정 시간을 사용하기 위해 duration은 별도 관리 가능
            base.SetData(srcPos, destPos, speed);
            InitializeParachuteData();
        }

        public void SetData(Transform transform, Vector3 srcPos, Vector3 destPos, float duration = 2.0f)
        {
            base.SetData(srcPos, destPos, 1.0f); // 베이스 speed는 사용하지 않으므로 기본값
            this._transform = transform;
            this._characterController = null;
            this.duration = duration;
            InitializeParachuteData();
        }

        /// <summary>
        /// CharacterController를 받아서 그 캐릭터의 포지션을 추적하는 오버로드
        /// </summary>
        public void SetData(CharacterController characterController, Vector3 srcPos, float duration = 2.0f)
        {
            base.SetData(srcPos, characterController != null ? characterController.Position3D : srcPos, 1.0f);
            this._characterController = characterController;
            this._transform = null;
            this.duration = duration;
            InitializeParachuteData();
        }

        private void InitializeParachuteData()
        {
            elapsedTime = 0;
            currPos = srcPos;
            
            if (_transform != null)
                _transform.rotation = Quaternion.identity;
        }
        
        /// <summary>
        /// EaseInOutCirc 커브: 0~1 사이의 t값을 받아서 0~1 사이의 값을 반환
        /// </summary>
        private float EaseInOutCirc(float t)
        {
            if (t < 0.5f)
            {
                // EaseInCirc: 0~0.5 범위를 0~0.5로 매핑
                return (1f - Mathf.Sqrt(1f - Mathf.Pow(2f * t, 2f))) / 2f;
            }
            else
            {
                // EaseOutCirc: 0.5~1 범위를 0.5~1로 매핑
                return (Mathf.Sqrt(1f - Mathf.Pow(-2f * t + 2f, 2f)) + 1f) / 2f;
            }
        }

        public override void Clear()
        {
            base.Clear();
            _transform = null;
            _characterController = null;
        }

        public override void ManagedUpdate(float dt)
        {
            // CharacterController를 추적하는 경우 destPos를 업데이트
            if (_characterController != null)
            {
                if (_characterController.IsAlive == false)
                {
                    _characterController = null;
                }
                else
                {
                    destPos = _characterController.Position3D + Vector3.up * 1.6f;
                }
            }

            elapsedTime += dt;
            
            // 정확히 duration(2초) 후에 종료
            if (elapsedTime >= duration)
            {
                currPos = destPos;
                if (_transform != null)
                    _transform.rotation = Quaternion.identity;

                InvokeReachedTarget();
                return;
            }

            // 정규화된 시간 (0~1)
            float t = elapsedTime / duration;
            
            // EaseInOutCirc 커브로 Y값만 제어
            float easedT = EaseInOutCirc(t);
            
            // 위치 업데이트
            prevPos = currPos;
            
            // X, Z는 선형 보간, Y만 EaseInOutCirc로 보간
            currPos.x = Mathf.Lerp(srcPos.x, destPos.x, t);
            currPos.z = Mathf.Lerp(srcPos.z, destPos.z, t);
            currPos.y = Mathf.Lerp(srcPos.y, destPos.y, easedT);

            // 좌우 흔들림 (Z축 회전)
            float swingDamping = 1f - t; // 지면에 가까워질수록 흔들림 감쇠
            float swingAngle = Mathf.Sin(elapsedTime * Mathf.PI * swingFrequency) * maxSwingAngle * swingDamping;

            // 7. 트랜스폼 적용
            if (_transform != null)
            {
                _transform.position = currPos;
                _transform.rotation = Quaternion.Euler(0, 0, swingAngle);
            }
        }
    }
}
