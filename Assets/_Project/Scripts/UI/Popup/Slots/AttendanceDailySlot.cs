using System.Collections;
using System.Collections.Generic;
using Cookapps.Autobattleproject.V1;
using CookApps.TeamBattle;
using CookApps.TeamBattle.UIManagements;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CookApps.AutoBattler
{
    public class AttendanceDailySlot : CachedMonoBehaviour
    {
        [SerializeField] private RewardItemSlot _rewardItemSlot;
        [SerializeField] private CAButton _attendanceButton;
        [SerializeField] private TextMeshProUGUI _dayText;

        [Header("Slot State")]
        [SerializeField] private GameObject _claimBGObject;
        [SerializeField] private GameObject _claimOnObject;
        [SerializeField] private GameObject _claimCheckObject;

        private SpecEventCondition _currentSpecEventConditionData;

        private UserEventData _currentUserEventData;
        private UserEventConditionData _currentUserEventConditionData;

        private void Awake()
        {
            _attendanceButton.onClick.AddListener(OnClickAttendanceButton);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            _attendanceButton.onClick.RemoveListener(OnClickAttendanceButton);
        }

        public void SetAttendanceSlot(UserEventData eventData, UserEventConditionData conditionData)
        {
            if (eventData == null) return;
            if (conditionData == null) return;

            _currentUserEventData = eventData;
            _currentUserEventConditionData = conditionData;

            _currentSpecEventConditionData = SpecDataManager.Instance.GetSpecEventConditionData(_currentUserEventData.EventId, _currentUserEventConditionData.EventConditionId);

            _dayText.text = _currentSpecEventConditionData.need_count.ToString();

            // 리워드 데이터 세팅
            var rewardItem = new RewardItem(_currentSpecEventConditionData.item_type, _currentSpecEventConditionData.item_key, _currentSpecEventConditionData.item_count);
            _rewardItemSlot.SetRewardSlot(rewardItem);

            RefreshSlot();
        }

        public void RefreshSlot()
        {
            // 클레임 상태 세팅
            _claimBGObject.SetActive(_currentUserEventConditionData.EventStateType == (int)EventStateType.REWARD);
            _claimOnObject.SetActive(_currentUserEventConditionData.EventStateType == (int)EventStateType.REWARD);

            _claimCheckObject.SetActive(_currentUserEventConditionData.EventStateType == (int)EventStateType.CLEAR);
        }

        private void OnClickAttendanceButton()
        {

        }
    }
}
