using System.Collections;
using System.Collections.Generic;
using CookApps.BattleSystem;
using CookApps.TeamBattle.UIManagements;
using Cysharp.Threading.Tasks;
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
        
        [Header("Dungeon Info")]
        
        
        private bool _isVictory = false;
        
        private SpecCharacter _specCharacter;
        
        private void Awake()
        {
            _exitButton?.onClick.AddListener(OnClickExitButton);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            
            _exitButton?.onClick.RemoveListener(OnClickExitButton);
        }

        protected override void OnPreEnter(object param)
        {
            base.OnPreEnter(param);
            //TopCurrencyAndMenuBar.AddToUILayer(this, TopPanelType.PVP_Ticket);

            SoundManager.Instance.StopBGM();
            
            (_isVictory, _specCharacter) = ((bool, SpecCharacter))param;
            
            _failObj.SetActive(!_isVictory);
            _victoryObj.SetActive(_isVictory);
            
            if (_isVictory)
                _victoryStageText.text = StringUtil.GetStageString(InGameManager.Instance.SpecStage);
            else
                _failStageText.text =  StringUtil.GetStageString(InGameManager.Instance.SpecStage);

            if (_specCharacter != null)
            {
                BMUtil.RemoveChildObjects(_characterIllustParentObject.transform);

                string illustPrefabName = string.Format(Defines.CHARACTER_ILLUST_PREFEAB_NAME_FORMAT, _specCharacter.prefab_id);
                AddressablesUtil.Instantiate(illustPrefabName, _characterIllustParentObject.transform);
            }

            // 애니메이션 연출 적용
            string animKey = _isVictory ? "InGameResult_Win" : "InGameResult_Lose";
            baseAnimator.SetTrigger(animKey);
        }

        private void OnClickExitButton()
        {
            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_btn_touch);
            
            int lastPlayStageID = UserDataManager.Instance.GetLastPlayStageID();
            var specLastStageData = SpecDataManager.Instance.GetStageData(lastPlayStageID);
            
            var transition = SceneTransition_FadeInOut.Create();
            SceneLoading.GoToNextScene("Lobby",  (int)specLastStageData.chapter_id, transition).Forget();
            
            SceneUILayerManager.OnSceneLoadedEvent += OpenDungeonTrialPopupAction;
        }
        
        private void OpenDungeonTrialPopupAction(string scenename)
        {
            if (scenename == "Lobby")
            {
                SceneUILayerManager.Instance.PushUILayerAsync<DungeonTrialPopup>().Forget();
            }
        }
    }
}