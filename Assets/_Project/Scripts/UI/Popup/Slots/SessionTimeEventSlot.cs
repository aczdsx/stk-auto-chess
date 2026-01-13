using System.Collections;
using System.Collections.Generic;
using Cookapps.Stkauto.V1;
using CookApps.TeamBattle;
using CookApps.TeamBattle.UIManagements;
using Cysharp.Threading.Tasks;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

namespace CookApps.AutoBattler
{
    public class SessionTimeEventSlot : CachedMonoBehaviour
    {
        [SerializeField] private CAButton _getRewardButton;
        [SerializeField] private RewardItemSlot _rewardItemSlot;
        [SerializeField] private TextMeshProUGUI _sessionTimeText;

        [Header("Slot State")]
        [SerializeField] private GameObject _activeCircleObject;
        [SerializeField] private GameObject _claimBGObject;
        [SerializeField] private GameObject _claimOnObject;
        [SerializeField] private GameObject _claimCheckObject;


        private EventCondition _specEventConditionData;

        private UserEventData _currentUserEventData;
        private UserEventConditionData _currentUserEventConditionData;

        private List<RewardItem> _eventRewardItemList = new List<RewardItem>();

        private void Awake()
        {
            _getRewardButton.OnClickAsObservable().Subscribe(this, (_, self) => self.OnClickGetRewardButton()).AddTo(this);
        }

        public void SetEventSlot(UserEventData eventData, UserEventConditionData conditionData)
        {
            if (eventData == null) return;
            if (conditionData == null) return;

            _currentUserEventData = eventData;
            _currentUserEventConditionData = conditionData;

            _specEventConditionData = SpecDataManager.Instance.GetSpecEventConditionData(_currentUserEventData.EventId, _currentUserEventConditionData.EventConditionId);

            _sessionTimeText.text = _specEventConditionData.need_count.ToString();

            // 리워드 데이터 세팅
            // ItemType의 삭제로 인해 변경.(new RewardItem(_specEventConditionData.item_type, _specEventConditionData.item_key, _specEventConditionData.item_count))
            RewardItem newRewardItem = new RewardItem(_specEventConditionData.item_id, _specEventConditionData.item_count);
            _rewardItemSlot.SetRewardSlot(newRewardItem);

            _eventRewardItemList.Add(newRewardItem);

            RefreshSlot(false);
        }

        public void RefreshSlot(bool needDataRefresh)
        {
            if (needDataRefresh)
            {
                _currentUserEventConditionData = UserDataManager.Instance.GetUserEventConditionData(_currentUserEventData.EventId, _currentUserEventConditionData.EventConditionId);
            }

            bool isAvailGetReward = _currentUserEventConditionData.EventStateType == (int) EventStateType.REWARD;
            bool isAlreadyGetReward = _currentUserEventConditionData.EventStateType == (int) EventStateType.CLEAR;

            // 클레임 상태 세팅
            _claimBGObject.SetActive(isAvailGetReward);
            _claimOnObject.SetActive(isAvailGetReward);

            _activeCircleObject.SetActive(isAvailGetReward || isAlreadyGetReward);
            _claimCheckObject.SetActive(isAlreadyGetReward);
        }

        private void OnClickGetRewardButton()
        {
            if (_currentUserEventData == null) return;
            if (_currentUserEventConditionData.EventStateType != (int)EventStateType.REWARD) return;
            if (_eventRewardItemList == null || _eventRewardItemList.Count <= 0) return;

            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_btn_popup);

            // 세션 이벤트 상태 데이터 저장
            UserDataManager.Instance.SetUserEventConditionState(_currentUserEventData.EventId, _currentUserEventConditionData.EventConditionId, EventStateType.CLEAR, true);

            // 보상 데이터 저장
            UserDataManager.Instance.IncreaseRewardItemList(_eventRewardItemList, true);

            SceneUILayerManager.Instance.PushUILayerAsync<RewardResultPopup>(("REWARD_TITLE", _eventRewardItemList)).Forget();

            RefreshSlot(true);
        }
    }
}
