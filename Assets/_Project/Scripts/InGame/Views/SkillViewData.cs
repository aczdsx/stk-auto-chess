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
        [SerializeField] private GameObject prefab;
        [SerializeField] private SkillPosition skillPosition;
        [SerializeField] private bool followable;

        public GameObject Prefab => prefab;
        public SkillPosition Position => skillPosition;
        public bool Followable => followable;
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