using UnityEngine;

namespace CookApps.AutoBattler
{
    [CreateAssetMenu(fileName = "TargetLineConfig", menuName = "AutoBattler/InGame/TargetLineConfig")]
    public class TargetLineConfig : ScriptableObject
    {
        [Header("라인 설정")]
        [Tooltip("포물선 높이")]
        public float Height = 3f;

        [Tooltip("경로 분할 수 (라인 해상도)")]
        public int PositionCount = 30;

        [Tooltip("라인 재생 시간 (초)")]
        public float LineDurationTime = 2f;

        [Tooltip("시작 오프셋 (인덱스)")]
        public int Offset = 4;

        [Header("캐릭터 오프셋")]
        [Tooltip("캐릭터 Y축 높이 오프셋")]
        public float CharacterYOffset = 0.5f;

        [Header("UV 스크롤")]
        [Tooltip("텍스처 스크롤 속도 (음수=반대 방향)")]
        public float ScrollSpeed = 1f;
    }
}
