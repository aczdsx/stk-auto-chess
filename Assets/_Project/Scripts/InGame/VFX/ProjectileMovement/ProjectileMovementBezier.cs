using UnityEngine;

namespace CookApps.BattleSystem
{
    public class ProjectileMovementBezier : ProjectileMovementBase
    {
        public static ProjectileMovementBezier Create()
        {
            return new ProjectileMovementBezier();
        }

        private Vector3[] m_points = new Vector3[4];

        private float m_timerCurrent = 0;
        private float m_speed = 1;
        private float _newPointDistanceFromStartTr = 6;
        private float _newPointDistanceFromEndTr = 2;
        private Vector3 direction;

        public override void SetData(InGameEffectViewProjectile effectView, Vector3 srcPos, Vector3 destPos, float speed)
        {
            base.SetData(effectView, srcPos, destPos, speed);
            InitializeBezierData();
        }

        private void InitializeBezierData()
        {
            m_timerCurrent = 0;

            // 시작 지점.
            m_points[0] = srcPos;

            // TODO: 시작 지점을 기준으로 랜덤 포인트 지정.
            m_points[1] = srcPos;

            // TODO: 도착 지점을 기준으로 랜덤 포인트 지정.
            m_points[2] = destPos;

            // 도착 지점.
            m_points[3] = destPos;
            EffectView.CachedTr.localPosition = srcPos;
        }

        public override void ManagedUpdate(float dt)
        {
            if (m_timerCurrent > 1)
            {
                InvokeReachedTarget();
                return;
            }

            // 경과 시간 계산.
            m_timerCurrent += dt * m_speed;
            // 베지어 곡선으로 X,Y,Z 좌표 얻기.
            var pos = new Vector3(
                CubicBezierCurve(m_points[0].x, m_points[1].x, m_points[2].x, m_points[3].x),
                CubicBezierCurve(m_points[0].y, m_points[1].y, m_points[2].y, m_points[3].y)
            );
            direction = (pos - EffectView.CachedTr.position).normalized;
            SetRotation(direction);
            EffectView.CachedTr.position = pos;
        }

        private void SetRotation(Vector2 direction)
        {
            float angle = Vector3.Angle(Vector3.left, direction);

            EffectView.CachedTr.rotation = Quaternion.Euler(0, 0, angle + 180);
        }

        /// <summary>
        /// 3차 베지어 곡선.
        /// </summary>
        /// <param name="a">시작 위치</param>
        /// <param name="b">시작 위치에서 얼마나 꺾일 지 정하는 위치</param>
        /// <param name="c">도착 위치에서 얼마나 꺾일 지 정하는 위치</param>
        /// <param name="d">도착 위치</param>
        /// <returns></returns>
        private float CubicBezierCurve(float a, float b, float c, float d)
        {
            float t = m_timerCurrent;

            float ab = Mathf.Lerp(a, b, t);
            float bc = Mathf.Lerp(b, c, t);
            float cd = Mathf.Lerp(c, d, t);

            float abbc = Mathf.Lerp(ab, bc, t);
            float bccd = Mathf.Lerp(bc, cd, t);

            return Mathf.Lerp(abbc, bccd, t);
        }
    }
}
