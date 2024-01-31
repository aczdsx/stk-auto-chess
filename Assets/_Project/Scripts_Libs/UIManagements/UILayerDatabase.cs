using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace CookApps.TeamBattle.UIManagements
{
    [CreateAssetMenu(fileName = "new UILayer Data", menuName = "ScriptableObjects/UILayerData")]
    public class UILayerDatabase : ScriptableObject
    {
        [SerializeField] private List<SceneUILayerManager.UILayerData> list;

        public List<SceneUILayerManager.UILayerData> List => list;
    }
}
