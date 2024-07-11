using System.Collections;
using System.Collections.Generic;
using Cookapps.Autobattleproject.V1;
using CookApps.TeamBattle.UIManagements;
using UnityEngine;

namespace CookApps.AutoBattler
{
    [RegisterUILayer(UILayerType.Popup, "Prefabs/UI/01_Pops/WindowPopup/AttendancePopup.prefab")]
    public class AttendancePopup : UILayer
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

            _closeButton.onClick.AddListener(OnClickCloseButton);
            _dimCloseButton.onClick.AddListener(OnClickCloseButton);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            _closeButton.onClick.RemoveListener(OnClickCloseButton);
            _dimCloseButton.onClick.RemoveListener(OnClickCloseButton);
        }

        protected override void OnPreEnter(object param)
        {
            base.OnPreEnter(param);
            //TopCurrencyAndMenuBar.AddToUILayer(this, TopPanelType.CloseButton);

            _currentUserEventData = param as UserEventData;

            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_btn_popup);

            SetAttendancePopup();
        }

        private void SetAttendancePopup()
        {
            if (_currentUserEventData == null) return;

            _currentUserEventConditionDataList = UserDataManager.Instance.GetUserEventConditionDataList(_currentUserEventData.EventId);

            // 데이터 & 슬롯 갯수 체크
            if (_currentUserEventConditionDataList.Count != _attendanceDailySlotList.Count) return;

            for (int i = 0; i < _currentUserEventConditionDataList.Count; ++i)
            {
                _attendanceDailySlotList[i].SetAttendanceSlot(_currentUserEventData, _currentUserEventConditionDataList[i]);
            }
        }

        private void OnClickCloseButton()
        {
            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_btn_confirm);

            SceneUILayerManager.Instance.PopUILayer(this);
        }
    }
}
