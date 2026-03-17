using System.Collections.Generic;
using CookApps.AutoBattler;
using UnityEngine;

namespace CookApps.AutoChess
{
    /// <summary>
    /// SkillActive 스펙 리스트에서 타입별 값을 읽는 헬퍼.
    /// 기존 SkillSpecAdapter.GetSpecRate + SecondsToFrames를 통합하여
    /// 각 커스텀 스킬이 자체 스펙 파싱에 사용.
    /// </summary>
    public static class SkillSpecHelper
    {
        /// <summary>specList[index].base_rate 원본 값 (소수). 퍼센트 배율 등에 사용.</summary>
        public static float GetRawRate(List<SkillActive> specList, int index, float fallback = 0f)
        {
            if (specList != null && index < specList.Count)
                return specList[index].base_rate;
            return fallback;
        }

        /// <summary>specList[index].base_rate를 반올림한 정수. 카운트/퍼센트 정수값에 사용.</summary>
        public static int GetInt(List<SkillActive> specList, int index, float fallback = 0f)
        {
            return Mathf.RoundToInt(GetRawRate(specList, index, fallback));
        }

        /// <summary>specList[index].base_rate를 초 단위로 읽어 프레임으로 변환.</summary>
        public static int GetFrames(List<SkillActive> specList, int index, float fallbackSec, int tickRate)
        {
            float sec = GetRawRate(specList, index, fallbackSec);
            return SecondsToFrames(sec, tickRate);
        }

        public static int SecondsToFrames(float seconds, int tickRate)
        {
            return (int)(seconds * tickRate + 0.5f);
        }
    }
}
