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

        public GameObject Prefab => prefab;
        public SkillPosition Position => skillPosition;
        public bool Followable => followable;
        /// <summary>true이면 유닛에 1개만 부착되고 자식 GO on/off로 상태 제어 (루키다 도깨비불 등)</summary>
        public bool Persistent => persistent;
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