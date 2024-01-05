using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using DG.Tweening;
using UnityEditor;
using Object = UnityEngine.Object;

namespace CookApps.TeamBattle.UIManagements
{
    [CreateAssetMenu(fileName = "new sceneData", menuName = "ScriptableObjects/SceneData")]
    public class SceneDatabase : ScriptableObject
    {
        [SerializeField] List<SceneUIManager.SceneData> list;
        public List<SceneUIManager.SceneData> List => list;

        public void Add(Object obj)
        {
            var item = new SceneUIManager.SceneData();
            item.sceneName = obj.name;

            list.Add(item);
        }
    }
}
