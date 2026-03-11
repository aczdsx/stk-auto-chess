namespace CookApps.AutoChess
{
    /// <summary>
    /// ATK 키프레임 캐시 구조체.
    /// Awake에서 1회 resolve하여 보관. 런타임에서는 필드 읽기만.
    /// </summary>
    public struct AnimKeyframeInfo
    {
        public float FrontExecTime;
        public float BackExecTime;
        public int FrontHitCount;
        public int BackHitCount;
        public float[] FrontHitTimes; // null이면 단타
        public float[] BackHitTimes;  // null이면 단타

        /// <summary>첫 Execute 이벤트 시간 (초)</summary>
        public float GetExecTime(bool isFront) => isFront ? FrontExecTime : BackExecTime;

        /// <summary>총 히트 수 (Execute1Per2 → 2, Execute1Per1 → 1)</summary>
        public int GetHitCount(bool isFront) => isFront ? FrontHitCount : BackHitCount;

        /// <summary>각 히트 타이밍 배열 (초). 단타면 null.</summary>
        public float[] GetHitTimes(bool isFront) => isFront ? FrontHitTimes : BackHitTimes;
    }

    /// <summary>
    /// AnimKeyframeData(baked) 조회 헬퍼.
    /// 캐릭터 ID로 ATK execute time, 다타 히트 정보를 제공.
    /// Unity 의존성 없음 — RuntimeAnimatorController 불필요.
    /// </summary>
    public static class AnimKeyframeHelper
    {
        /// <summary>
        /// 캐릭터 ID로 front/back ATK 정보를 일괄 조회.
        /// Awake에서 1회 호출하여 캐싱용.
        /// </summary>
        public static AnimKeyframeInfo Resolve(int characterId)
        {
            var info = new AnimKeyframeInfo
            {
                FrontHitCount = 1,
                BackHitCount = 1,
            };

            if (characterId <= 0) return info;

            int frontKey = AnimKeyframeData.MakeKey(characterId, true, AnimClipType.ATK);
            int backKey = AnimKeyframeData.MakeKey(characterId, false, AnimClipType.ATK);

            AnimKeyframeData.ExecuteTimes.TryGetValue(frontKey, out info.FrontExecTime);
            AnimKeyframeData.ExecuteTimes.TryGetValue(backKey, out info.BackExecTime);

            if (AnimKeyframeData.AttackHitTimes.TryGetValue(frontKey, out var frontTimes))
            {
                info.FrontHitCount = frontTimes.Length;
                info.FrontHitTimes = frontTimes;
            }

            if (AnimKeyframeData.AttackHitTimes.TryGetValue(backKey, out var backTimes))
            {
                info.BackHitCount = backTimes.Length;
                info.BackHitTimes = backTimes;
            }

            return info;
        }

        /// <summary>컨트롤러명 앞부분 연속 숫자를 파싱 (예: "15232101_AnimationController" → 15232101)</summary>
        public static int ParseCharacterId(string controllerName)
        {
            int end = 0;
            while (end < controllerName.Length && char.IsDigit(controllerName[end]))
                end++;
            if (end > 0 && int.TryParse(controllerName.Substring(0, end), out int id))
                return id;
            return 0;
        }
    }
}
