using System.Collections;
using System.Collections.Generic;
using Cookapps.Stkauto.V1;
using CookApps.TeamBattle;
using CookApps.TeamBattle.UIManagements;
using Cysharp.Threading.Tasks;
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

        private AttendancePopup _parentAttendancePopup;

        private SpecEventCondition _currentSpecEventConditionData;

        private UserEventData _currentUserEventData;
        private UserEventConditionData _currentUserEventConditionData;

        private List<RewardItem> _attendanceRewardItemList = new List<RewardItem>();

        private void Awake()
        {
            _attendanceButton.onClick.AddListener(OnClickAttendanceButton);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            _attendanceButton.onClick.RemoveListener(OnClickAttendanceButton);
        }

        public void SetAttendanceSlot(AttendancePopup parent, UserEventData eventData, UserEventConditionData conditionData)
        {
            if (parent == null) return;
            if (eventData == null) return;
            if (conditionData == null) return;

            _parentAttendancePopup = parent;

            _currentUserEventData = eventData;
            _currentUserEventConditionData = conditionData;

            _currentSpecEventConditionData = SpecDataManager.Instance.GetSpecEventConditionData(_currentUserEventData.EventId, _currentUserEventConditionData.EventConditionId);

            _dayText.text = _currentSpecEventConditionData.need_count.ToString();

            // 리워드 데이터 세팅
            var rewardItem = new RewardItem(_currentSpecEventConditionData.item_type, _currentSpecEventConditionData.item_key, _currentSpecEventConditionData.item_count);
            _rewardItemSlot.SetRewardSlot(rewardItem);

            _attendanceRewardItemList.Add(rewardItem);

            RefreshSlot(false);
        }

        public void RefreshSlot(bool needDataRefresh)
        {
            if (needDataRefresh)
            {
                _currentUserEventConditionData = UserDataManager.Instance.GetUserEventConditionData(_currentUserEventData.EventId, _currentUserEventConditionData.EventConditionId);
            }

            // 클레임 상태 세팅
            _claimBGObject.SetActive(_currentUserEventConditionData.EventStateType == (int)EventStateType.REWARD);
            _claimOnObject.SetActive(_currentUserEventConditionData.EventStateType == (int)EventStateType.REWARD);

            _claimCheckObject.SetActive(_currentUserEventConditionData.EventStateType == (int)EventStateType.CLEAR);
        }

        private void OnClickAttendanceButton()
        {
            if (_currentUserEventData == null) return;
            if (_currentUserEventConditionData.EventStateType != (int)EventStateType.REWARD) return;
            if (_attendanceRewardItemList == null || _attendanceRewardItemList.Count <= 0) return;

            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_btn_popup);

            // 출석 체크 상태 데이터 저장
            UserDataManager.Instance.SetUserEventConditionState(_currentUserEventData.EventId, _currentUserEventConditionData.EventConditionId, EventStateType.CLEAR, true);

            // 보상 데이터 저장
            UserDataManager.Instance.IncreaseRewardItemList(_attendanceRewardItemList, true);

            SceneUILayerManager.Instance.PushUILayerAsync<RewardResultPopup>(_attendanceRewardItemList).Forget();

            RefreshSlot(true);

            //_parentAttendancePopup?.RefreshPopup();
        }
    }
}
