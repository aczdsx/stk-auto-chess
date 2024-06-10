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

            for (int i = 0; i < _starList.Count; i++)
            {
                _starList[i].SetActive(_star > i);
            }

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

            foreach (var rewardItem in rewardList)
            {
                bool shouldCreateRewardItemSlot = false;

                if (rewardItem.frequency_type == FrequencyType.ONCE)
                {
                    if (rewardItem.star_count > _star && userStage.StarCount > _star)
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
                    var rewardItemSlot = Instantiate(_rewardItemSlotObj, _rewardsTransform).GetComponent<RewardItemSlot>();
                    rewardItemSlot.SetRewardItem(new RewardItem(rewardItem.item_type, rewardItem.item_key, rewardItem.item_count));
                }
            }

            if (userStage.StarCount > _star)
            {
                UserDataManager.Instance.SaveUserStage();
            }
        }

    }
}
