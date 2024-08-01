using System.Collections;
using System.Collections.Generic;
using CookApps.TeamBattle.UIManagements;
using TMPro;
using UnityEngine;

namespace CookApps.AutoBattler
{
    [RegisterUILayer(UILayerType.Popup, "Prefabs/UI/01_Pops/InGameDungeonTrialResultPopup.prefab")]
    public class InGameDungeonTrialResultPopup : UILayer
    {
        [SerializeField] private CAButton _exitButton;
        [SerializeField] private CAButton _nextStageButton;
        [SerializeField] private CAButton _retryStageButton;

        [SerializeField] private GameObject _failObj;
        [SerializeField] private GameObject _victoryObj;

        [SerializeField] private TextMeshProUGUI _victoryStageText;
        [SerializeField] private TextMeshProUGUI _failStageText;
        [SerializeField] private TextMeshProUGUI _nextStageButtonText;

        [SerializeField] private Transform _rewardsTransform;
        [SerializeField] private GameObject _rewardItemSlotObj;

        [SerializeField] private GameObject _characterIllustParentObject;
        
        private void Awake()
        {
            
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
        }

        protected override void OnPreEnter(object param)
        {
            base.OnPreEnter(param);
            //TopCurrencyAndMenuBar.AddToUILayer(this, TopPanelType.PVP_Ticket);

            SoundManager.Instance.StopBGM();
        }
    }
}