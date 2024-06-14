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

        [SerializeField] private GameObject _failObj;
        [SerializeField] private GameObject _victoryObj;

        [SerializeField] private TextMeshProUGUI _victoryStageText;
        [SerializeField] private TextMeshProUGUI _failStageText;

        [SerializeField] private Transform _rewardsTransform;
        [SerializeField] private GameObject _rewardItemSlotObj;

        [SerializeField] private List<GameObject> _starList;
        [SerializeField] private List<InGameResultStarCondition> _starConditionList;

        [SerializeField] private Image _illustImage;

        private bool _isVictory = false;
        private int _star = 0;

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
            _illustImage.sprite = ImageManager.Instance.GetCharacterIllustSprite(40101); // [TODO] MVP 관리 필요

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

            // 가이드 미션 상태에 따른 버튼 분기처리


            if (_isVictory)
                CreateRewardItems();
        }

        private void OnExitButtonClicked()
        {
            var transition = SceneTransition_FadeInOut.Create();
            SceneLoading.GoToNextScene("Lobby", null, transition).Forget();
        }

        private void OnNextStageButtonClicked()
        {
            var transition = SceneTransition_FadeInOut.Create();
            SceneLoading.GoToNextScene("Lobby", null, transition).Forget();
        }

        private void CreateRewardItems()
        {
            var userStage = UserDataManager.Instance.GetUserStage(InGameManager.Instance.SpecStage.id);
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
                    rewardItemSlot.SetRewardItem(newItem);

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
                int currentStageID = InGameManager.Instance.SpecStage.id;

                UserDataManager.Instance.SetUserStage(currentStageID, _star);

                GuideMissionManager.Instance.AddGuideMissionActionValue(GuideMissionType.CLEAR_STAGE,currentStageID, 1);
            }
        }

    }
}
