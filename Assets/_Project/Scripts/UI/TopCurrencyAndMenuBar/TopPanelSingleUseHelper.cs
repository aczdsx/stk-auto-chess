using System.Collections.Generic;
using System.Linq;
using CookApps.TeamBattle;
using CookApps.TeamBattle.Utility;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace CookApps.AutoBattler
{
    public class TopPanelSingleUseHelper : SingletonMonoBehaviour<TopPanelSingleUseHelper>
    {
        private Dictionary<TopPanelType, TopPanelBase> panels = new ();
        private AsyncOperationHandle<GameObject> topUIOriginHandle;
        private Transform topUIOriginTr;

        private List<TopCurrencyAndMenuBar> topUIs = new ();
        private bool _isInitialized = false;

        public async UniTask Initialize()
        {
            if (_isInitialized) return;

            topUIOriginHandle = Addressables.InstantiateAsync("Prefabs/UI/Top/TopCurrencyAndMenu.prefab", transform);
            await topUIOriginHandle.WaitUntilDone();
            topUIOriginTr = topUIOriginHandle.Result.transform;
            int childCount = topUIOriginTr.childCount;
            for (var i = 0; i < childCount; i++)
            {
                Transform child = topUIOriginTr.GetChild(i);
                var panel = child.GetComponent<TopPanelBase>();
                panels.TryAdd(panel.PanelType, panel);
            }

            topUIOriginHandle.Result.SetActive(false);
            _isInitialized = true;
        }

        public void Clear()
        {
            if (!_isInitialized) return;

            foreach ((_, TopPanelBase ui) in panels)
            {
                if (ui == null) continue;

                ui.CachedRectTr.SetParent(topUIOriginTr);
            }
            panels.Clear();

            topUIOriginHandle.Release();
            _isInitialized = false;
        }

        public TopPanelBase GetPanel(TopPanelType type)
        {
            return panels[type];
        }

        public void Push(TopCurrencyAndMenuBar topUI)
        {
            topUIs.Add(topUI);
            for (var i = 0; i < topUI.UsePanelTypes.Length; i++)
            {
                TopPanelType type = topUI.UsePanelTypes[i];
                topUI.AddPanel(type, panels[type].CachedRectTr);
                panels[type].attachedTopBar = topUI;
            }

            topUI.ForceUpdateLayout();
        }

        public void Pop(TopCurrencyAndMenuBar topUI)
        {
            topUIs.Remove(topUI);
            for (var i = 0; i < topUI.UsePanelTypes.Length; i++)
            {
                TopPanelType type = topUI.UsePanelTypes[i];
                TopPanelBase panel = panels[type];
                var isOccupied = false;
                for (int j = topUIs.Count - 1; j >= 0; j--)
                {
                    if (topUIs[j].UsePanelTypes.Contains(type))
                    {
                        topUIs[j].AddPanel(type, panel.CachedRectTr);
                        panel.attachedTopBar = topUIs[j];
                        isOccupied = true;
                        break;
                    }
                }

                if (!isOccupied)
                {
                    panel.CachedTr.SetParent(topUIOriginTr, false);
                    panel.attachedTopBar = null;
                }
            }
        }
    }
}
