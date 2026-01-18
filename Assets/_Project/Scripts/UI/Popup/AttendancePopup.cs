using System.Collections.Generic;
using Cookapps.Stkauto.V1;
using CookApps.TeamBattle.UIManagements;
using R3;
using UnityEngine;

namespace CookApps.AutoBattler
{
    public class AttendancePopup : UILayerPopupBase
    {
        [SerializeField] private CAButton _closeButton;
        [SerializeField] private CAButton _dimCloseButton;

        [Header("Attendance Slot")]
        [SerializeField] private List<AttendanceDailySlot> _attendanceDailySlotList;

        private UserEventData _currentUserEventData;
        private List<UserEventConditionData> _currentUserEventConditionDataList;

        protected override void Awake()
        {
            base.Awake();

            _closeButton.OnClickAsObservable().Subscribe(this, (_, self) => self.OnClickCloseButton()).AddTo(this);
            _dimCloseButton.OnClickAsObservable().Subscribe(this, (_, self) => self.OnClickCloseButton()).AddTo(this);

            // 가이드 미션 체크
            ServerDataManager.Instance.GuideMission.AddActionValue(GuideMissionType.CLICK_ATTENDANCE);
        }

        protected override void OnPreEnter(object param)
        {
            base.OnPreEnter(param);
            //TopCurrencyAndMenuBar.AddToUILayer(this, TopPanelType.CloseButton);

            _currentUserEventData = param as UserEventData;

            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_btn_popup);

            UpdateAttendanceData();
            SetAttendancePopup();
        }

        public void RefreshPopup()
        {
            if (_attendanceDailySlotList == null || _attendanceDailySlotList.Count <= 0) return;

            _attendanceDailySlotList.ForEach(slot => slot.RefreshSlot(true));
        }

        private void UpdateAttendanceData()
        {
            if (_currentUserEventData == null) return;

            if (_currentUserEventData.EventExtraRefreshTimestamp < TimeManager.Instance.UtcNowTimeStampLocal())
            {
                UserDataManager.Instance.SetUserEventActionCount(_currentUserEventData.EventId, 1, true, true);
                UserDataManager.Instance.UpdateEventTimeData(_currentUserEventData.EventId);
            }
        }

        private void SetAttendancePopup()
        {
            if (_currentUserEventData == null) return;

            _currentUserEventConditionDataList = UserDataManager.Instance.GetUserEventConditionDataList(_currentUserEventData.EventId);

            // 데이터 & 슬롯 갯수 체크
            if (_currentUserEventConditionDataList.Count != _attendanceDailySlotList.Count) return;

            for (int i = 0; i < _currentUserEventConditionDataList.Count; ++i)
            {
                _attendanceDailySlotList[i].SetAttendanceSlot(this, _currentUserEventData, _currentUserEventConditionDataList[i]);
            }
        }

        private void OnClickCloseButton()
        {
            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_btn_confirm);

            SceneUILayerManager.Instance.PopUILayer(this);
        }
    }
}
