using UnityEngine;

namespace CookApps.BattleSystem
{
    public class InGameVfxMovementBezier : InGameVfxMovementBase
    {
        private Vector3[] points = new Vector3[4];

        private float time = 0;
        private float totalDistance = 0;

        public override void SetData(Vector3 srcPos, Vector3 destPos, float speed)
        {
            base.SetData(srcPos, destPos, speed);
            InitializeBezierData();
        }

        private void InitializeBezierData()
        {
            time = 0;
            totalDistance = Mathf.Abs(Vector3.Distance(srcPos, destPos));

            // 시작 지점.
            points[0] = srcPos;

            // 최대점 높이.
            float h = srcPos.y + 5.0f; // 최대 높이

            // 시작 지점을 기준으로 포인트 지정.
            points[1] = new Vector3(srcPos.x, h, srcPos.z);

            // 도착 지점을 기준으로 포인트 지정.
            points[2] = new Vector3(destPos.x, h, destPos.z);

            // 도착 지점.
            points[3] = destPos;
        }

        public override void ManagedUpdate(float dt)
        {
            prevPos = currPos;
            if (time > 1)
            {
                currPos = destPos;
                InvokeReachedTarget();
                return;
            }

            // 경과 시간 계산.
            time += dt * speed / totalDistance;
            // 베지어 곡선으로 X,Y,Z 좌표 얻기.
            currPos = new Vector3(
                CubicBezierCurve(points[0].x, points[1].x, points[2].x, points[3].x),
                CubicBezierCurve(points[0].y, points[1].y, points[2].y, points[3].y),
                CubicBezierCurve(points[0].z, points[1].z, points[2].z, points[3].z)
            );
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
            float t = time;

            float ab = Mathf.Lerp(a, b, t);
            float bc = Mathf.Lerp(b, c, t);
            float cd = Mathf.Lerp(c, d, t);

            float abbc = Mathf.Lerp(ab, bc, t);
            float bccd = Mathf.Lerp(bc, cd, t);

            return Mathf.Lerp(abbc, bccd, t);
        }
    }
}
