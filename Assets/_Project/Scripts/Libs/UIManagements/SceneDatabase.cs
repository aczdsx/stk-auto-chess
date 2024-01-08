using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CookApps.TeamBattle.UIManagements
{
    [CreateAssetMenu(fileName = "new sceneData", menuName = "ScriptableObjects/SceneData")]
    public class SceneDatabase : ScriptableObject
    {
        [SerializeField] private List<SceneUIManager.SceneData> list;

        public List<SceneUIManager.SceneData> List => list;

        public void Add(Object obj)
        {
            var item = new SceneUIManager.SceneData();
            item.sceneName = obj.name;

            list.Add(item);
        }
    }
}
