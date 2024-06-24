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

    [RegisterUILayer(UILayerType.Popup, "Prefabs/UI/01_Pops/InGameResultPopup.prefab")]
    public class InGameResultPopup : UILayer
    {
        [SerializeField] private CAButton _exitButton;
        [SerializeField] private CAButton _nextStageButton;
        [SerializeField] private CAButton _retryStageButton;

        [SerializeField] private GameObject _failObj;
        [SerializeField] private GameObject _victoryObj;

        [SerializeField] private TextMeshProUGUI _victoryStageText;
        [SerializeField] private TextMeshProUGUI _failStageText;

        [SerializeField] private Transform _rewardsTransform;
        [SerializeField] private GameObject _rewardItemSlotObj;

        [SerializeField] private List<GameObject> _starList;
        [SerializeField] private List<InGameResultStarCondition> _starConditionList;

        [SerializeField] private GameObject _characterIllustParentObject;

        private bool _isVictory = false;
        private int _star = 0;

        private bool _isPlayingTutorialStage = false;   // 튜토리얼 진행 여부 체크
        private bool _isClearTutorialStage = false;     // 튜토리얼 스테이지 클리어 여부 체크
        private bool _isPlayingLastStage = false;   // 챕터의 마지막 스테이지 체크용
        private bool _isEndChapter = false;         // 게임의 마지막 챕터인지 확인용

        protected override void OnPreEnter(object param)
        {
            base.OnPreEnter(param);
            (_isVictory, _star) = ((bool, int))param;
            _failObj.SetActive(!_isVictory);
            _victoryObj.SetActive(_isVictory);

            if (_isVictory)
                _victoryStageText.text = StringUtil.GetStageString(InGameManager.Instance.SpecStage);
            else
                _failStageText.text =  StringUtil.GetStageString(InGameManager.Instance.SpecStage);

            _exitButton?.onClick.AddListener(OnExitButtonClicked);
            _nextStageButton?.onClick.AddListener(OnNextStageButtonClicked);
            _retryStageButton?.onClick.AddListener(OnClickRetryStageButton);

            var playerCharacterList = InGameObjectManager.Instance.GetCharacterList(AllianceType.Player);
            if (playerCharacterList != null && playerCharacterList.Count > 0)
            {
                BMUtil.RemoveChildObjects(_characterIllustParentObject.transform);

                var _specCharacterData = SpecDataManager.Instance.GetCharacterData(playerCharacterList[0].CharacterId);

                string illustPrefabName = string.Format(Defines.CHARACTER_ILLUST_PREFEAB_NAME_FORMAT, _specCharacterData.prefab_id);
                AddressablesUtil.Instantiate(illustPrefabName, _characterIllustParentObject.transform);
            }

            // 상단 별 상태 갱신
            for (int i = 0; i < _starList.Count; i++)
            {
                _starList[i].SetActive(_star > i);
            }

            // 하단 별+조건 상태 갱신
            for (int i = 0; i < _starConditionList.Count; i++)
            {
                _starConditionList[i]._starObject.SetActive(_star > i);

                string resultToken = string.Format("STAGE_STAR_CONDITON_DESC_{0}", i + 1);
                _starConditionList[i]._conditionText.text = LanguageManager.Instance.GetLanguageText(resultToken);
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

            _retryStageButton.gameObject.SetActive(!_isPlayingTutorialStage);
            _nextStageButton.gameObject.SetActive(!_isEndChapter && !_isClearTutorialStage);
            _exitButton.gameObject.SetActive(!_isPlayingTutorialStage || _isClearTutorialStage);

            if (_isVictory)
                CreateRewardItems();

            // 애니메이션 연출 적용
            string animKey = _isVictory ? "InGameResult_Win" : "InGameResult_Lose";
            baseAnimator.SetTrigger(animKey);
        }

        private void OnExitButtonClicked()
        {
            var transition = SceneTransition_FadeInOut.Create();
            SceneLoading.GoToNextScene("Lobby",  (int)InGameManager.Instance.SpecStage.chapter_id, transition).Forget();
        }

        private void OnNextStageButtonClicked()
        {
            // var transition = SceneTransition_FadeInOut.Create();
            // SceneLoading.GoToNextScene("Lobby", (int)InGameManager.Instance.SpecStage.chapter_id, transition).Forget();

            //InGameManager.Instance.EndInGame();

            // 최종 챕터/스테이지 여부 체크
            if (_isEndChapter) return;

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

            SceneLoading.GoToNextScene("InGame", (targetChapterID, targetStageNumber, InGameManager.Instance.SpecStage.difficulty_type)).Forget();
        }

        private void OnClickRetryStageButton()
        {
            //InGameManager.Instance.EndInGame();
            SceneLoading.GoToNextScene("InGame", ((int)InGameManager.Instance.SpecStage.chapter_id, (int)InGameManager.Instance.SpecStage.stage_number, InGameManager.Instance.SpecStage.difficulty_type)).Forget();
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
                    RewardItem newItem = new RewardItem(rewardItem.item_type, rewardItem.item_key, rewardItem.item_count);

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
    }
}
