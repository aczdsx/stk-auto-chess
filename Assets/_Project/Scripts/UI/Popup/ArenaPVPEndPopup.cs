using System.Collections;
using System.Collections.Generic;
using CookApps.BattleSystem;
using CookApps.TeamBattle.UIManagements;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CookApps.AutoBattler
{
    [RegisterUILayer(UILayerType.Popup, "Prefabs/UI/01_Pops/ArenaPopup/ArenaPvpEndPopup.prefab")]
    public class ArenaPVPEndPopup : UILayer
    {
        [Header("Common")]
        [SerializeField] private CAButton _okButton;

        [Header("Tier Info Layer")] 
        [SerializeField] private Image _tierImage;
        [SerializeField] private TextMeshProUGUI _tierNameText;
        [SerializeField] private TextMeshProUGUI _tierPointText;
        [SerializeField] private TextMeshProUGUI _tierPointChangeText;
        [SerializeField] private List<GameObject> _tierLevelObjectList;
        
        [Header("Reward Layer")]
        [SerializeField] private GameObject _rewardContentObject;
        [SerializeField] private GameObject _rewardItemSlotObject;
        
        private bool _isVictory = false;
        
        private SpecCharacter _specCharacter;
        
        private void Awake()
        {
            _okButton.onClick.AddListener(OnClickCloseButton);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            _okButton.onClick.RemoveListener(OnClickCloseButton);
        }

        protected override void OnPreEnter(object param)
        {
            base.OnPreEnter(param);
            //TopCurrencyAndMenuBar.AddToUILayer(this, TopPanelType.PVP_Ticket);

            SoundManager.Instance.StopBGM();
            
            (_isVictory, _specCharacter) = ((bool, SpecCharacter))param;
        }
        
        private async void OnClickCloseButton()
        {
            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_btn_touch);
            
            SceneUILayerManager.Instance.PopUILayer(this);
            
            InGameManager.Instance.EndInGame();
            int lastPlayStageID = UserDataManager.Instance.GetLastPlayStageID();
            var specLastStageData = SpecDataManager.Instance.GetStageData(lastPlayStageID);
            var transition = SceneTransition_FadeInOut.Create();
            await SceneLoading.GoToNextScene("Lobby", (int) specLastStageData.chapter_id, transition);
            
            SceneUILayerManager.OnSceneLoadedEvent += OpenArenaMainPopupAction;
        }
    
        private void OpenArenaMainPopupAction(string scenename)
        {
            if (scenename == "Lobby")
            {
                SceneUILayerManager.Instance.PushUILayerAsync<ArenaMainPopup>().Forget();
            }
        }
    }
}