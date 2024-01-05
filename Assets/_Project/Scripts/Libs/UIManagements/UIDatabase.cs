using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using DG.Tweening;
using Object = UnityEngine.Object;

namespace CookApps.TeamBattle.UIManagements
{
    [CreateAssetMenu(fileName = "new uiData", menuName = "ScriptableObjects/UIData")]
    public class UIDatabase : ScriptableObject
    {
        [SerializeField] List<SceneUIManager.UIData> list;
        public List<SceneUIManager.UIData> List => list;

#if UNITY_EDITOR
        public void Add(Object obj)
        {
            var item = new SceneUIManager.UIData();

            item.uiName = obj.name;

            var path = UnityEditor.AssetDatabase.GetAssetPath(obj);
            var projectStr = "_Project";
            var idx = path.IndexOf(projectStr);
            path = path.Substring(idx + projectStr.Length + 1);

            item.assetName = path;

            list.Add(item);
        }
#endif
    }
}
