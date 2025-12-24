using UnityEngine;

namespace CookApps.BattleSystem
{
    /// <summary>
    /// 패러슈트 이동 커브 데이터를 저장하는 ScriptableObject
    /// 에디터에서 커브를 편집하고 테스트할 수 있음
    /// </summary>
    [CreateAssetMenu(fileName = "ParachuteCurveData", menuName = "BattleSystem/ParachuteCurveData")]
    public class ParachuteCurveData : ScriptableObject
    {
        [Header("Duration")]
        [Tooltip("이동 시간 (초)")]
        public float duration = 2.0f;

        [Header("Position Curves")]
        [Tooltip("X축 이동 커브 (0~1 범위)")]
        public AnimationCurve xCurve = AnimationCurve.Linear(0, 0, 1, 1);

        [Tooltip("Y축 속도 커브 (양수: 아래로 떨어짐, 음수: 위로 올라감, 탄성 효과 가능)")]
        public AnimationCurve yCurve = CreateDefaultElasticCurve();
        
        /// <summary>
        /// 탄성 효과를 위한 기본 커브 생성 (공중에서 튕기고 천천히 떨어짐)
        /// </summary>
        private static AnimationCurve CreateDefaultElasticCurve()
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

        [Tooltip("Z축 이동 커브 (0~1 범위)")]
        public AnimationCurve zCurve = AnimationCurve.Linear(0, 0, 1, 1);

        [Header("Rotation Curve")]
        [Tooltip("회전 커브 (Z축 회전 각도, 도 단위)")]
        public AnimationCurve rotationCurve = AnimationCurve.Constant(0, 1, 0);

        [Header("Character Tracking")]
        [Tooltip("CharacterController를 추적할지 여부")]
        public bool trackCharacter = false;

        [Tooltip("추적 시 Y축 오프셋")]
        public float trackingYOffset = 1.6f;

        private void OnValidate()
        {
            // 커브가 null이거나 비어있으면 기본값 설정
            if (xCurve == null || xCurve.length == 0)
                xCurve = AnimationCurve.Linear(0, 0, 1, 1);
            if (yCurve == null || yCurve.length == 0)
                yCurve = CreateDefaultElasticCurve(); // 탄성 효과 기본 커브
            if (zCurve == null || zCurve.length == 0)
                zCurve = AnimationCurve.Linear(0, 0, 1, 1);
            if (rotationCurve == null || rotationCurve.length == 0)
                rotationCurve = AnimationCurve.Constant(0, 1, 0);
        }
    }
}

