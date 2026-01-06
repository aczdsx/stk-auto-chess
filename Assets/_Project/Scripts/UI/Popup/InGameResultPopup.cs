using System;
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
    [Serializable]
    public class InGameResultStarCondition
    {
        public GameObject _starObject;
        public TextMeshProUGUI _conditionText;
    }

    public class InGameResultPopup : UILayer
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

        [SerializeField] private List<GameObject> _starList;
        [SerializeField] private List<InGameResultStarCondition> _starConditionList;

        [SerializeField] private GameObject _characterIllustParentObject;

        private bool _isVictory = false;
        private bool _starTime;
        private bool _starNoDeath;
        private int _star;

        private bool _isPlayingTutorialStage = false;   // 튜토리얼 진행 여부 체크
        private bool _isClearTutorialStage = false;     // 튜토리얼 스테이지 클리어 여부 체크
        private bool _isPlayingLastStage = false;   // 챕터의 마지막 스테이지 체크용
        private bool _isEndChapter = false;         // 게임의 마지막 챕터인지 확인용
        private bool _isEndStage = false;         // 게임의 가장 마지막 스테이지 체크
        private bool _isWaitGuideMissionReward = false;         // 현재 가이드 미션을 클리어한 상태인지 체크

        private CharacterInfo _specCharacter;

        protected override void OnPreEnter(object param)
        {
            base.OnPreEnter(param);

            SoundManager.Instance.StopBGM();

            (_isVictory, _starTime, _starNoDeath, _specCharacter) = ((bool, bool, bool, CharacterInfo))param;

            _failObj.SetActive(!_isVictory);
            _victoryObj.SetActive(_isVictory);

            if (_isVictory)
                _victoryStageText.text = StringUtil.GetStageString(InGameManager.Instance.SpecStage);
            else
                _failStageText.text =  StringUtil.GetStageString(InGameManager.Instance.SpecStage);
            

            _exitButton?.onClick.AddListener(OnExitButtonClicked);
            _nextStageButton?.onClick.AddListener(OnNextStageButtonClicked);
            _retryStageButton?.onClick.AddListener(OnClickRetryStageButton);

            if (_specCharacter != null)
            {
                BMUtil.RemoveChildObjects(_characterIllustParentObject.transform);

                string illustPrefabName = string.Format(Defines.CHARACTER_ILLUST_PREFEAB_NAME_FORMAT, _specCharacter.prefab_id);
                AddressablesUtil.Instantiate(illustPrefabName, _characterIllustParentObject.transform);
            }

            _star = _isVictory ? 1 : 0;
            if (_starTime)
                _star++;
            if (_starNoDeath)
                _star++;
            // 상단 별 상태 갱신
            for (int i = 0; i < _starList.Count; i++)
            {
                _starList[i].SetActive(_star > i);
            }

            // 하단 별+조건 상태 갱신
            if (_isVictory)
            {
                for (int i = 0; i < _starConditionList.Count; i++)
                {
                    string resultToken = string.Format("STAGE_STAR_CONDITON_DESC_{0}", i + 1);
                    _starConditionList[i]._conditionText.text = LanguageManager.Instance.GetLanguageText(resultToken);
                }

                _starConditionList[0]._starObject.SetActive(_isVictory);
                _starConditionList[1]._starObject.SetActive(_starTime);
                _starConditionList[2]._starObject.SetActive(_starNoDeath);
            }
            else
            {
                for (int i = 0; i < _starConditionList.Count; i++)
                {
                    _starConditionList[i]._starObject.SetActive(false);

                    string resultToken = string.Format("STAGE_LOSE_GUIDE_DESC_{0}", i + 1);
                    _starConditionList[i]._conditionText.text = LanguageManager.Instance.GetLanguageText(resultToken);
                }
            }


            // 챕터 1 (튜토리얼 스테이지) 관련 처리
            if (InGameManager.Instance.SpecStage.chapter_id == 1 && !UserDataManager.Instance.IsClearStage(InGameManager.Instance.SpecStage.stage_id))
            {
                _isPlayingTutorialStage = true;
            }

            // 마지막 스테이지 체크
            var lastSpecStage = SpecDataManager.Instance.GetLastStageData(InGameManager.Instance.SpecStage.chapter_id, InGameManager.Instance.SpecStage.difficulty_type);
            _isPlayingLastStage = lastSpecStage != null && lastSpecStage.stage_id == InGameManager.Instance.SpecStage.stage_id;
            if (_isPlayingLastStage)
            {
                int nextChpaterID = lastSpecStage.chapter_id + 1;
                // 다음 챕터 존재 여부 확인
                var nextChapterData = SpecDataManager.Instance.GetStageData(nextChpaterID, 1, lastSpecStage.difficulty_type);
                _isEndChapter = nextChapterData == null;    // 다음 챕터 데이터 없음 (게임의 마지막 챕터)
            }

            _isClearTutorialStage = _isPlayingTutorialStage && _isPlayingLastStage && _isVictory;

            // 승리 시 보상 및 각종 데이터 처리
            if (_isVictory)
            {
                CheckLatestStageClear();
                CreateRewardItems();

                // 퀘스트 데이터 갱신
                UserDataManager.Instance.SetUserQuestActionCount(QuestType.CLEAR_STAGE, 1, true, true);

                SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ingame_result_victory_001);
            }
            else
            {
                SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ingame_result_defeat_001);
            }

            var currentMissionData = UserDataManager.Instance.GetCurrentGuideMissionData();
            if (currentMissionData != null)
            {
                // 가이드 미션이 완료되어 보상 수령 대기상태일 경우 처리
                _isWaitGuideMissionReward = currentMissionData.MissionStateType == (int)MissionStateType.REWARD;
            }

            // 애니메이션 연출 적용
            string animKey = _isVictory ? "InGameResult_Win" : "InGameResult_Lose";
            baseAnimator.SetTrigger(animKey);

            // 버튼 상태 처리
            if (_isWaitGuideMissionReward)
            {
                _retryStageButton.gameObject.SetActive(false);
                _nextStageButton.gameObject.SetActive(false);
                _exitButton.gameObject.SetActive(true);
            }
            else
            {
                _retryStageButton.gameObject.SetActive(!_isPlayingTutorialStage || !_isVictory);
                _nextStageButton.gameObject.SetActive(!_isEndChapter && !_isClearTutorialStage && _isVictory);
                _exitButton.gameObject.SetActive(!_isPlayingTutorialStage || _isClearTutorialStage);
            }

            string buttonStringKey = _isPlayingLastStage ? "UI_CHAPTER_NEXT_MOVE" : "UI_STAGE_NEXT_MOVE";
            _nextStageButtonText.text = LanguageManager.Instance.GetLanguageText(buttonStringKey);
            
            // 앱이벤트 전송
            SendStageEndAppEvent(InGameManager.Instance.AppEventResult, InGameManager.Instance.AppEventReason);
        }

        private void OnExitButtonClicked()
        {
            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_btn_touch);

            int lastPlayStageID = UserDataManager.Instance.GetLastPlayStageID();
            var specLastStageData = SpecDataManager.Instance.GetStageData(lastPlayStageID);

            SceneTransition.Create<SceneTransition_FadeInOut>();
            SceneTransition.FadeInAsync().Forget();
            SceneLoading.GoToNextScene("Lobby", specLastStageData.chapter_id);
        }

        private void OnNextStageButtonClicked()
        {
            // SceneTransition.Create<SceneTransition_FadeInOut>();
SceneTransition.FadeInAsync().Forget();
            // SceneLoading.GoToNextScene("Lobby", (int)InGameManager.Instance.SpecStage.chapter_id, transition).Forget();

            //InGameManager.Instance.EndInGame();

            // 최종 챕터/스테이지 여부 체크
            if (_isEndChapter) return;

            // 행동력 검사
            if (!UserDataManager.Instance.CheckEnoughItem(ItemType.AP, 0, InGameManager.Instance.SpecStage.need_ap, true))
            {
                return;
            }

            int targetChapterID = InGameManager.Instance.SpecStage.chapter_id;
            int targetStageNumber = InGameManager.Instance.SpecStage.stage_number;
            if (_isPlayingLastStage)
            {
                targetChapterID++;
                targetStageNumber = 1;
            }
            else
            {
                targetStageNumber++;
            }

            var nextStageData = SpecDataManager.Instance.GetStageData(targetChapterID, targetStageNumber, InGameManager.Instance.SpecStage.difficulty_type);
            
            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_btn_touch);

            SceneLoading.GoToNextScene("InGame",
                (InGameType.STAGE, (IGameStateUICore) new InGameMainStateStage(), nextStageData.stage_id));
        }

        private void OnClickRetryStageButton()
        {
            // 행동력 검사
            if (!UserDataManager.Instance.CheckEnoughItem(ItemType.AP, 0, InGameManager.Instance.SpecStage.need_ap, true))
            {
                return;
            }

            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_btn_touch);

            //InGameManager.Instance.EndInGame();
            SceneLoading.GoToNextScene("InGame",
                (InGameType.STAGE, (IGameStateUICore) new InGameMainStateStage(), (int)InGameManager.Instance.SpecStage.stage_id));
        }

        // 가장 높은 스테이지 클리어 여부 체크
        private void CheckLatestStageClear()
        {
            var latestStageID = UserDataManager.Instance.GetLatestClearUserStageID();
            var latestStageData = SpecDataManager.Instance.GetStageData(latestStageID);

            if (latestStageData == null) return;

            if (InGameManager.Instance.SpecStage.chapter_id == latestStageData.chapter_id)
            {
                if (InGameManager.Instance.SpecStage.stage_number > latestStageData.stage_number)
                {
                    var nextStageData = SpecDataManager.Instance.GetNextStageData(InGameManager.Instance.SpecStage.stage_id);

                    if (nextStageData != null)
                    {
                        UserDataManager.Instance.SetLastPlayStageID(nextStageData.stage_id, true);
                    }
                }
            }
            else if (InGameManager.Instance.SpecStage.chapter_id > latestStageData.chapter_id)
            {
                var nextStageData = SpecDataManager.Instance.GetNextStageData(InGameManager.Instance.SpecStage.stage_id);

                if (nextStageData != null)
                {
                    UserDataManager.Instance.SetLastPlayStageID(nextStageData.stage_id, true);
                }
            }
        }

        private void CreateRewardItems()
        {
            var userStage = UserDataManager.Instance.GetUserStage(InGameManager.Instance.SpecStage.stage_id);
            var rewardList = SpecDataManager.Instance.GetSpecStageReward(InGameManager.Instance.SpecStage.reward_id)
                .FindAll(l => l.difficulty_type == InGameManager.Instance.SpecStage.difficulty_type);

            List<RewardItem> resultItemList = new List<RewardItem>();   // 보상 지급용 리워드 리스트

            foreach (var rewardItem in rewardList)
            {
                bool shouldCreateRewardItemSlot = false;

                if (rewardItem.frequency_type == FrequencyType.ONCE)
                {
                    if (rewardItem.star_count > _star && (userStage == null || userStage.StarCount > _star))
                    {
                        shouldCreateRewardItemSlot = true;
                    }
                }
                else if (rewardItem.frequency_type == FrequencyType.REPEAT)
                {
                    shouldCreateRewardItemSlot = true;
                }

                if (shouldCreateRewardItemSlot)
                {
                    // ItemType의 삭제로 인해 변경.(new RewardItem(rewardItem.item_type, rewardItem.item_key, rewardItem.item_count))
                    RewardItem newItem = new RewardItem(rewardItem.item_key == 0 ? (int)rewardItem.item_type : rewardItem.item_key, rewardItem.item_count);

                    var rewardItemSlot = Instantiate(_rewardItemSlotObj, _rewardsTransform).GetComponent<RewardItemSlot>();
                    rewardItemSlot.SetRewardSlot(newItem);

                    resultItemList.Add(newItem);
                }
            }

            // 보상 데이터 저장
            if (resultItemList.Count > 0)
            {
                UserDataManager.Instance.IncreaseRewardItemList(resultItemList, true);
            }

            // 별 최고기록일 경우 스테이지 클리어 데이터 저장
            if (userStage == null || _star > userStage.StarCount)
            {
                int currentStageID = InGameManager.Instance.SpecStage.stage_id;

                UserDataManager.Instance.SetUserStage(currentStageID, _star);

                // 가이드 미션 체크
                GuideMissionManager.Instance.AddGuideMissionActionValue(GuideMissionType.CLEAR_STAGE,currentStageID, 1);
            }
        }

        // 앱이벤트 - 스테이지 종료
        private void SendStageEndAppEvent(string result, string reason)
        {
            // 앱 이벤트 처리
            var myDeck = UserDataManager.Instance.GetUserCharacterBattleDeckList(InGameType.STAGE);
            int myDeckPower = UserDataManager.Instance.GetDeckBattlePower(myDeck);
            int enemyPower = (int)InGameObjectManager.Instance.GetStartingEnemiesAttr();
        
            int starNum1 = _isVictory ? 1 : 0;
            int starNum2 = _star >= 2 ? 1 : 0;
            int starNum3 = _star >= 3 ? 1 : 0;
            string clearCondition = AppEventManager.Instance.GetAppEventCustomDataList(starNum1, starNum2, starNum3);

            var battleTime = 60 - InGameMain.GetInGameMain().InGameTime;
            
            AppEventManager.Instance.StageEnd(InGameManager.Instance.SpecStage.id, InGameManager.Instance.SpecStage.stage_id, battleTime, myDeck.Count, 
                myDeckPower, enemyPower, result, reason, clearCondition);
        }
    }
}
