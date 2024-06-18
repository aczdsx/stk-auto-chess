using System.Collections;
using System.Collections.Generic;
using CookApps.TeamBattle.UIManagements;
using UnityEngine;

namespace CookApps.AutoBattler
{
    [RegisterUILayer(UILayerType.Popup, "Prefabs/UI/01_Pops/WindowPopup/ChapterClearWindowPopup.prefab")]
    public class ChapterClearWindowPopup : UILayer
    {
        [SerializeField] private CAButton _getRewardButton;

        protected override void Awake()
        {
            base.Awake();

            _getRewardButton.onClick.AddListener(OnClickGetRewardButton);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            _getRewardButton.onClick.RemoveListener(OnClickGetRewardButton);
        }

        protected override void OnPreEnter(object param)
        {
            base.OnPreEnter(param);
            //TopCurrencyAndMenuBar.AddToUILayer(this, TopPanelType.CloseButton);

        }

        private void OnClickGetRewardButton()
        {
            SceneUILayerManager.Instance.PopUILayer(this);
        }
    }
}
