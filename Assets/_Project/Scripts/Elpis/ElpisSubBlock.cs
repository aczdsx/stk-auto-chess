using System;
using CookApps.TeamBattle;
using UnityEngine;

namespace CookApps.AutoBattler
{
    public class ElpisSubBlock : CachedMonoBehaviour
    {
        [SerializeField] private GameObject walkPath;

        private void Awake()
        {
            walkPath.layer = LayerMask.NameToLayer("ElpisGround");
            walkPath.AddComponent<BoxCollider>();
        }
    }
}
