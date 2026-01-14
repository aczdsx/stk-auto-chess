using System;
using System.Collections.Generic;
using UnityEngine;

namespace CookApps.AutoBattler
{
    [CreateAssetMenu(fileName = "InGameTestConfig", menuName = "AutoBattler/Test/InGameTestConfig")]
    public class InGameTestConfig : ScriptableObject
    {
        [Header("스테이지 설정")]
        [Tooltip("스테이지 프리팹 결정용 챕터 ID (1~3)")]
        public int StageChapterId = 1;

        [Header("내 캐릭터")]
        public List<TestCharacterData> PlayerCharacters = new List<TestCharacterData>();

        [Header("적 캐릭터")]
        public List<TestCharacterData> EnemyCharacters = new List<TestCharacterData>();

        [Header("카메라 설정")]
        public float CameraSize = 7.5f;
        public Vector3 CameraPosition = new Vector3(0, 2.0f, -10);

        [Header("전투 설정")]
        [Tooltip("전투 제한 시간 (초)")]
        public float BattleTimeLimit = 60f;

        [Tooltip("전투 종료 후 재시작 대기 시간 (초)")]
        public float RestartDelay = 2f;

        [Header("디버그 설정")]
        [Tooltip("플레이어 무적 (데미지 텍스트는 표시, HP 감소 없음)")]
        public bool PlayerInvincible = false;

        [Tooltip("적 무적 (데미지 텍스트는 표시, HP 감소 없음)")]
        public bool EnemyInvincible = false;
    }

    [Serializable]
    public class TestCharacterData
    {
        [Tooltip("캐릭터/몬스터 ID (SpecData 기준)")]
        public int CharacterId;

        [Tooltip("캐릭터 레벨")]
        public int Level = 1;

        [Tooltip("그리드 X 좌표 (0~4)")]
        [Range(0, 4)]
        public int GridX;

        [Tooltip("그리드 Y 좌표 (0~6, 3은 중립영역)")]
        [Range(0, 6)]
        public int GridY;

        [Tooltip("공격력 배수 (기본 1.0)")]
        public float MultipleAtk = 1f;

        [Tooltip("체력 배수 (기본 1.0)")]
        public float MultipleHp = 1f;
    }
}
