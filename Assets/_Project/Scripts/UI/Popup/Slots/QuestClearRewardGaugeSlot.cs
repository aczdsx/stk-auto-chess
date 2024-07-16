using System.Collections;
using System.Collections.Generic;
using Coffee.UIEffects;
using Cookapps.Autobattleproject.V1;
using CookApps.TeamBattle;
using CookApps.TeamBattle.UIManagements;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CookApps.AutoBattler
{
    public class QuestClearRewardGaugeSlot : CachedMonoBehaviour
    {
        [SerializeField] private CAButton _getRewardButton;
        [SerializeField] private Image _rewardIconImage;
        [SerializeField] private UIShiny _rewardIconUIShiny;
        [SerializeField] private TextMeshProUGUI _clearAmountText;
        [SerializeField] private TextMeshProUGUI _rewardAmountText;

        [Space(10)]
        [SerializeField] private GameObject _activeFrameObject;
        [SerializeField] private GameObject _completeSymbolObject;
        [SerializeField] private Image _completeLayerImage_1;
        [SerializeField] private Image _completeLayerImage_2;

        [SerializeField] private Color _completedColor;

        private SpecQuest _specQuestData;
        private UserQuestData _userQuestData;

        private QuestPopup _parentPopup;

        private List<RewardItem> _questRewardItemList = new List<RewardItem>();

        private bool _isAvailGetReward;
        private bool _isAlreadyGetReward;

        private void Awake()
        {
            _getRewardButton.onClick.AddListener(OnClickGetRewardButton);
        }

        protected override void OnDestroy()
        {
            _getRewardButton.onClick.RemoveListener(OnClickGetRewardButton);
        }

        public void SetQuestGaugeSlot(QuestPopup parent, SpecQuest data)
        {
            if (data == null) return;

            ClearSlot();

            _parentPopup = parent;

            _specQuestData = data;
            _userQuestData = UserDataManager.Instance.GetUserQuestData(_specQuestData.quest_id);

            _clearAmountText.text = _specQuestData.need_count.ToString();

            _rewardIconImage.sprite = ImageManager.Instance.GetItemSprite(_specQuestData.item_type);
            _rewardAmountText.text = $"x{_specQuestData.item_count}";

            // 리워드 데이터 세팅
            var rewardItem = new RewardItem(_specQuestData.item_type, _specQuestData.item_key, _specQuestData.item_count);
            _questRewardItemList.Add(rewardItem);

            RefreshSlot(false);
        }

        public void RefreshSlot(bool needRefreshUserData)
        {
            if (needRefreshUserData)
            {
                _userQuestData = UserDataManager.Instance.GetUserQuestData(_specQuestData.quest_id);
            }

            // 보상 수령 상태에 따른 분기처리
            _isAvailGetReward = _userQuestData.QuestStateType == (int)QuestStateType.REWARD;
            _isAlreadyGetReward = _userQuestData.QuestStateType == (int) QuestStateType.CLEAR;

            _rewardIconUIShiny.Play(_isAvailGetReward && !_isAlreadyGetReward);
            _activeFrameObject.SetActive(_isAvailGetReward && !_isAlreadyGetReward);

            if (_isAlreadyGetReward)
            {
                _rewardIconImage.color = BMUtil.ChangeColorAlpha(_rewardIconImage.color, 60);

                _completeLayerImage_1.color = _completedColor;
                _completeLayerImage_2.color = _completedColor;
            }

            _completeLayerImage_1.gameObject.SetActive(_isAlreadyGetReward);
            _completeSymbolObject.SetActive(_isAlreadyGetReward);
        }

        private void OnClickGetRewardButton()
        {
            // 보상 수령 가능 여부 체크
            if (_isAvailGetReward == false)
            {
                return;
            }

            // 이미 수령한 보상 체크
            if (_isAlreadyGetReward)
            {
                return;
            }

            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_btn_popup);

            // 퀘스트 상태 데이터 저장
            UserDataManager.Instance.SetUserQuestState(_userQuestData.QuestId, QuestStateType.CLEAR, true);

            // 보상 데이터 저장
            UserDataManager.Instance.IncreaseRewardItemList(_questRewardItemList, true);

            SceneUILayerManager.Instance.PushUILayerAsync<RewardResultPopup>(_questRewardItemList).Forget();

            _parentPopup?.RefreshPopup();
        }

        private void ClearSlot()
        {
            _questRewardItemList.Clear();
        }
    }
}
