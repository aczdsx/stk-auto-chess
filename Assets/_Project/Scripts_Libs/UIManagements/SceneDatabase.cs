using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CookApps.TeamBattle.UIManagements
{
    [CreateAssetMenu(fileName = "new Scene Data", menuName = "ScriptableObjects/SceneData")]
    public class SceneDatabase : ScriptableObject
    {
        [SerializeField] private List<SceneData> list;

        public List<SceneData> List => list;
    }
}
