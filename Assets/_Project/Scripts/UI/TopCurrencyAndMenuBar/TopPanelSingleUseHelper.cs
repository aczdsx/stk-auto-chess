using System.Collections.Generic;
using System.Linq;
using CookApps.TeamBattle;
using CookApps.TeamBattle.UIManagements;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace CookApps.AutoBattler
{
    public class TopPanelSingleUseHelper : SingletonMonoBehaviour<TopPanelSingleUseHelper>
    {
#if ENABLE_CHEAT
        public void SetActive(bool isActive)
        {
            foreach (TopCurrencyAndMenuBar ui in topUIs)
            {
                ui.CachedGo.SetActive(isActive);
            }
        }
#endif
        private Dictionary<TopPanelType, TopPanelBase> panels = new ();
        private Transform topUIOriginTr;

        private List<TopCurrencyAndMenuBar> topUIs = new ();

        public async UniTask Initialize()
        {
            await SceneUILayerManager.Instance.PreloadUILayer(typeof(TopCurrencyAndMenuBar));
            GameObject topUIOrigin = await Addressables.InstantiateAsync("Prefabs/UI/Top/TopCurrencyAndMenu.prefab", transform);
            topUIOriginTr = topUIOrigin.transform;
            int childCount = topUIOriginTr.childCount;
            for (var i = 0; i < childCount; i++)
            {
                Transform child = topUIOriginTr.GetChild(i);
                var panel = child.GetComponent<TopPanelBase>();
                panels.TryAdd(panel.PanelType, panel);
            }

            topUIOrigin.SetActive(false);
        }

        public void Clear()
        {
            foreach ((_, TopPanelBase ui) in panels)
            {
                ui.CachedRectTr.SetParent(topUIOriginTr);
            }

            Addressables.ReleaseInstance(topUIOriginTr.gameObject);
            Destroy(topUIOriginTr.gameObject);
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
                    topUI.CachedTr.SetParent(topUIOriginTr, false);
                    panel.attachedTopBar = null;
                }
            }
        }
    }
}
