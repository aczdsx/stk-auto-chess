using System.Collections.Generic;
using CookApps.TeamBattle;
using UnityEngine;

namespace CookApps.AutoBattler
{
    public class ElpisSubBlock : CachedMonoBehaviour
    {
        [SerializeField] private GameObject walkPath;
        [SerializeField] private ElpisBuildingBase[] elpisBuildings;

        public IReadOnlyList<ElpisBuildingBase> ElpisBuildings => elpisBuildings;
        
        private void Awake()
        {
            walkPath.layer = LayerMask.NameToLayer("ElpisGround");
        }
    }
}
