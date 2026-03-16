using System;
using System.Collections.Generic;
using CookApps.BattleSystem;
using CookApps.TeamBattle;
using Cysharp.Threading.Tasks;
using LitMotion;
using Unity.Mathematics;
using UnityEngine;

namespace CookApps.AutoBattler
{
    [Serializable]
    public class SkillViewData
    {
        [Tooltip("스킬 VFX 프리팹. Projectile/SkillPhaseVfx/Persistent 용도에 따라 사용됨")]
        [SerializeField] private GameObject prefab;
        [Tooltip("VFX 생성 위치 (SKILL_ROOT, SKILL_TOP 등). Projectile은 SKILL_PROJECTILE 권장")]
        [SerializeField] private SkillPosition skillPosition;
        [Tooltip("true: 시전자를 따라다니는 VFX (부모로 부착). false: 월드 좌표에 고정 생성")]
        [SerializeField] private bool followable;
        [Tooltip("true: 유닛에 1개만 부착되고 자식 GO on/off로 개수 제어 (루키다 도깨비불 등 누적형 버프 VFX)")]
        [SerializeField] private bool persistent;

        [Header("Directional VFX 회전/플립 커스텀")]
        [Tooltip("LookRotation 후 추가 회전 보정 (기본: 0,-90,0). 테토라 대검 등은 (-90,-90,0)")]
        [SerializeField] private Vector3 rotationOffset = new Vector3(0, -90f, 0);
        [Tooltip("방향 조건 충족 시 적용할 스케일 (기본: 1,1,-1). 테토라 대검 등은 (1,-1,1)")]
        [SerializeField] private Vector3 flipScale = new Vector3(1, 1, -1);
        [Tooltip("true이면 방향 기반 회전/플립을 적용 (false면 기본 동작)")]
        [SerializeField] private bool useCustomRotation;

        public GameObject Prefab => prefab;
        public SkillPosition Position => skillPosition;
        public bool Followable => followable;
        /// <summary>true이면 유닛에 1개만 부착되고 자식 GO on/off로 상태 제어 (루키다 도깨비불 등)</summary>
        public bool Persistent => persistent;
        public bool UseCustomRotation => useCustomRotation;
        public Vector3 RotationOffset => rotationOffset;
        public Vector3 FlipScale => flipScale;
    }

    [Serializable]
    public enum SkillPosition
    {
        CUSTOM,
        SKILL_ROOT,
        SKILL_TOP,
        SKILL_MIDDLE,
        SKILL_BOTTOM,
        SKILL_PROJECTILE,
        SKILL_TILE,
    }
    
}