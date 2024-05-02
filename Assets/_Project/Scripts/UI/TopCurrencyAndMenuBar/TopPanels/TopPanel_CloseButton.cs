using System;
using CookApps.TeamBattle.UIManagements;
using UnityEngine;

namespace CookApps.SampleTeamBattle
{
    public class TopPanel_CloseButton : TopPanelBase
    {
        public override TopPanelType PanelType => TopPanelType.CloseButton;

        [SerializeField] private CAButton button;

        private void Awake()
        {
            button.onClick.AddListener(OnClick);
        }

        private void OnClick()
        {
            SceneUILayerManager.Instance.PopUILayer(attachedTopBar.TargetUI);
        }
    }
}
