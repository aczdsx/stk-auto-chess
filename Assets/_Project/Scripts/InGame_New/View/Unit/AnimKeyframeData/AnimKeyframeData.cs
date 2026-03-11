// AnimKeyframeData — 베이스 선언부 (수동 편집 가능)
// 폴더별 partial 파일이 Register_XXX() 메서드로 데이터를 채움.
// Key = MakeKey(characterId, isFront, clipType) — 순수 int 연산, 런타임 할당 0.
using System.Collections.Generic;
using CookApps.BattleSystem;

namespace CookApps.AutoChess
{
    /// <summary>클립 종류 (ATK, ATK2, SKL, SKL2 등)</summary>
    public enum AnimClipType : byte
    {
        ATK  = 0,
        ATK2 = 1,
        SKL  = 2,
        SKL2 = 3,
    }

    public static partial class AnimKeyframeData
    {
        static AnimKeyframeData()
        {
            Register_Characters();
            Register_Mob();
            Register_Obstacle();
        }

        /// <summary>클립별 첫 Execute 이벤트 시간 (초)</summary>
        public static readonly Dictionary<int, float> ExecuteTimes = new(700);

        /// <summary>클립 전체 길이 (초)</summary>
        public static readonly Dictionary<int, float> ClipLengths = new(700);

        /// <summary>클립별 전체 AnimationEvent 목록 (EventKey, 시간초)</summary>
        public static readonly Dictionary<int, (AnimationEventKey key, float time)[]> ClipEvents = new(700);

        /// <summary>다타 공격 히트 타이밍 배열 초 (hitCount >= 2인 ATK 클립만 등록). 1타면 여기에 없음.</summary>
        public static readonly Dictionary<int, float[]> AttackHitTimes = new(16);

        /// <summary>
        /// 정수 키 생성 — string 할당 0, 해시 함수 0.
        /// characterId: 컨트롤러명에서 파싱한 숫자 (예: 15232101)
        /// isFront: true=Front, false=Back
        /// clipType: ATK=0, ATK2=1, SKL=2, SKL2=3
        /// 레이아웃: [characterId (28bit)] [isFront (1bit)] [clipType (3bit)]
        /// </summary>
        public static int MakeKey(int characterId, bool isFront, AnimClipType clipType)
        {
            return (characterId << 4) | (isFront ? 0x8 : 0) | (int)clipType;
        }
    }
}
