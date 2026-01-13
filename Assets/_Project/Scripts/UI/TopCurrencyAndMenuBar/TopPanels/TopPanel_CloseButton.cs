using System;
using CookApps.TeamBattle.UIManagements;
using R3;
using UnityEngine;

namespace CookApps.AutoBattler
{
    public class TopPanel_CloseButton : TopPanelBase
    {
        public override TopPanelType PanelType => TopPanelType.CloseButton;

        [SerializeField] private CAButton button;

        private void Awake()
        {
            button.OnClickAsObservable().Subscribe(this, (_, self) => self.OnClick()).AddTo(this);
        }

        private void OnClick()
        {
            SceneUILayerManager.Instance.PopUILayer(attachedTopBar.TargetUI);
        }
    }
}
