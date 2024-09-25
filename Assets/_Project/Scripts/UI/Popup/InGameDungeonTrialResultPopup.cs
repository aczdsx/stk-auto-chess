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

        [SerializeField] private GameObject _characterIllustParentObject;
        
        [Space]
        [SerializeField] private GameObject _rewardObj;
        [SerializeField] private Transform _rewardsTransform;
        [SerializeField] private GameObject _rewardItemSlotObj;
        
        [SerializeField] private GameObject _gradeUpObj;
        [SerializeField] private GameObject _afterObj;
        [SerializeField] private GameObject _beforeObj;
        [SerializeField] private Image _beforeGradeImage;
        [SerializeField] private Image _afterGradeImage;
        [SerializeField] private GameObject _beforeLoseObj;
        [SerializeField] private GameObject _afterLoseObj;
        
        [SerializeField] private GameObject _beforeArrow;
        [SerializeField] private GameObject _afterArrow;
        [SerializeField] private TextMeshProUGUI _beforeGradeText;
        [SerializeField] private TextMeshProUGUI _afterGradeText;
        
        [Header("Dungeon Info")]
        
        
        private bool _isVictory = false;
        private SpecCharacter _specCharacter;
        private SpecDungeonTrial _currentSpecDungeonTrial;
        
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

            _afterLoseObj.SetActive(!_isVictory);
            _beforeLoseObj.SetActive(!_isVictory);
            if (_isVictory)
            {
                string successString = LanguageManager.Instance.GetLanguageText("TIER_UPGRADE_SUCCESS_MSG");
                _victoryStageText.text =
                    StringUtil.GetTrialDungeonString(InGameManager.Instance.SpecDungeonTrial) + " " + successString;

                SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ingame_result_victory_001);
                
                SetRewardInfo();
            }
            else
            {
                string failString = LanguageManager.Instance.GetLanguageText("TIER_UPGRADE_FAIL_MSG");
                _failStageText.text =
                    StringUtil.GetTrialDungeonString(InGameManager.Instance.SpecDungeonTrial)+ " " + failString;
                
                SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ingame_result_defeat_001);
            }

            if (_specCharacter != null)
            {
                BMUtil.RemoveChildObjects(_characterIllustParentObject.transform);

                string illustPrefabName = string.Format(Defines.CHARACTER_ILLUST_PREFEAB_NAME_FORMAT, _specCharacter.prefab_id);
                AddressablesUtil.Instantiate(illustPrefabName, _characterIllustParentObject.transform);
            }

            // 애니메이션 연출 적용
            string animKey = _isVictory ? "InGameResult_Win" : "InGameResult_Lose";
            baseAnimator.SetTrigger(animKey);
            // _rewardObj.SetActive(!InGameManager.Instance.SpecDungeonTrial.is_grade_up);

            _currentSpecDungeonTrial = SpecDataManager.Instance.GetSpecDungeonTrialData(InGameManager.Instance.SpecDungeonTrial.dungeon_id - 1);
            var nextDungeonTrialData = InGameManager.Instance.SpecDungeonTrial;

            _beforeArrow.SetActive(_isVictory);
            _afterObj.SetActive(_isVictory);
            _beforeObj.SetActive(_currentSpecDungeonTrial != null);
            if (_currentSpecDungeonTrial != null)
            {
                _beforeGradeImage.sprite = ImageManager.Instance.GetDungeonTrialClassSprite(_currentSpecDungeonTrial.trial_type, false);
                _beforeGradeText.text = StringUtil.GetTrialDungeonString(_currentSpecDungeonTrial);
            }

            _afterGradeImage.sprite = ImageManager.Instance.GetDungeonTrialClassSprite(nextDungeonTrialData.trial_type, false);
            _afterGradeText.text = StringUtil.GetTrialDungeonString(nextDungeonTrialData);
            _afterArrow.SetActive(_beforeObj.activeSelf);
            
            // 앱이벤트 전송
            SendDungeonEndAppEvent(InGameManager.Instance.AppEventResult, InGameManager.Instance.AppEventReason);
        }

        private void OnClickExitButton()
        {
            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_btn_touch);
            
            int lastPlayStageID = UserDataManager.Instance.GetLastPlayStageID();
            var specLastStageData = SpecDataManager.Instance.GetStageData(lastPlayStageID);
            
            var transition = SceneTransition_FadeInOut.Create();
            SceneLoading.GoToNextScene("Lobby",  (int)specLastStageData.chapter_id, transition).Forget();

            var userGuideMissionData = UserDataManager.Instance.GetCurrentGuideMissionData();
            if (userGuideMissionData.MissionStateType != (int)MissionStateType.REWARD)
                SceneUILayerManager.OnSceneLoadedEvent += OpenDungeonTrialPopupAction;
        }
        
        private void OpenDungeonTrialPopupAction(string scenename)
        {
            if (scenename == "Lobby")
            {
                SceneUILayerManager.Instance.PushUILayerAsync<DungeonTrialPopup>().Forget();
            
                SceneUILayerManager.OnSceneLoadedEvent -= OpenDungeonTrialPopupAction;
            }
            
        }
        
        private void SetRewardInfo()
        {
            if (InGameManager.Instance.SpecDungeonTrial == null) return;

            BMUtil.RemoveChildObjects(_rewardsTransform);

            _gradeUpObj.SetActive(_isVictory && InGameManager.Instance.SpecDungeonTrial.is_grade_up);
            if (!InGameManager.Instance.SpecDungeonTrial.is_grade_up)
            {
                List<RewardItem> resultItemList = new List<RewardItem>();   // 보상 지급용 리워드 리스트
                var rewardDataList = SpecDataManager.Instance.GetSpecDungeonRewardDataList(DungeonType.TRIAL, InGameManager.Instance.SpecDungeonTrial.dungeon_id);

                foreach (var rewardData in rewardDataList)
                {
                    GameObject newSlotObject = Instantiate(_rewardItemSlotObj, _rewardsTransform);
                    RewardItemSlot newSlot = newSlotObject.GetComponent<RewardItemSlot>();

                    RewardItem newRewardItem = new RewardItem(rewardData.item_type, rewardData.item_key, rewardData.item_count);
                    resultItemList.Add(newRewardItem);
                    newSlot?.SetRewardSlot(newRewardItem);
                }
                
                // 보상 데이터 저장
                if (rewardDataList.Count > 0)
                {
                    UserDataManager.Instance.IncreaseRewardItemList(resultItemList, true);
                }
            }
        }
        
        // 앱이벤트 - 던전 종료
        private void SendDungeonEndAppEvent(string result, string reason)
        {
            // 앱 이벤트 처리
            var myDeck = UserDataManager.Instance.GetUserCharacterBattleDeckList(InGameType.TRIAL);
            int myDeckPower = UserDataManager.Instance.GetDeckBattlePower(myDeck);
            int enemyPower = (int)InGameObjectManager.Instance.GetStartingEnemiesAttr();
        
            int starNum1 = _isVictory ? 1 : 0;
            string clearCondition = AppEventManager.Instance.GetAppEventCustomDataList(starNum1);
            
            var battleTime = 60 - InGameMain.GetInGameMain().InGameTime;
            
            AppEventManager.Instance.DungeonEnd(InGameManager.Instance.SpecDungeonTrial.dungeon_type, InGameManager.Instance.SpecDungeonTrial.id, battleTime, myDeck.Count, 
                myDeckPower, enemyPower, result, reason, clearCondition);
        }
    }
}