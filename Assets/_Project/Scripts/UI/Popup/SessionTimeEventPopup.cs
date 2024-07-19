using System.Collections.Generic;
using System.Linq;
using Cookapps.Stkauto.V1;
using CookApps.TeamBattle.UIManagements;
using UnityEngine;
using UnityEngine.UI;

namespace CookApps.AutoBattler
{
    [RegisterUILayer(UILayerType.Popup, "Prefabs/UI/01_Pops/WindowPopup/SessionTimeEventPopup.prefab")]
    public class SessionTimeEventPopup : UILayer
    {
        [Header("Common")]
        [SerializeField] private CAButton _closeButton;

        [Header("Event Slot")]
        [SerializeField] private Slider _eventProgressBar;
        [SerializeField] private ScrollRect _eventSlotScrollRect;
        [SerializeField] private GameObject _eventSlotObject;

        private List<SessionTimeEventSlot> _sessionTimeEventSlotList = new List<SessionTimeEventSlot>();

        private UserEventData _currentUserEventData;
        private List<UserEventConditionData> _currentUserEventConditionDataList;

        private SpecEvent _specEventData;
        private List<SpecEventCondition> _specEventConditionDataList;

        private void Awake()
        {
            _closeButton.onClick.AddListener(OnClickCloseButton);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            _closeButton.onClick.RemoveListener(OnClickCloseButton);
        }

        protected override void OnPreEnter(object param)
        {
            base.OnPreEnter(param);

            _currentUserEventData = param as UserEventData;

            _specEventData = SpecDataManager.Instance.GetSpecEventData(_currentUserEventData.EventId);
            _specEventConditionDataList = SpecDataManager.Instance.GetSpecEventConditionList(_currentUserEventData.EventId);

            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_btn_popup);

            UpdateEventData();
            SetEventPopup();
            SetProgressBar();
        }

        private void UpdateEventData()
        {
            if (_currentUserEventData == null) return;

            if (_currentUserEventData.EventRefreshTimestamp < TimeManager.Instance.UtcNowTimeStamp())
            {
                UserDataManager.Instance.ResetEventData(_currentUserEventData.EventId, true);
                UserDataManager.Instance.UpdateEventTimeData(_currentUserEventData.EventId);
            }
        }

        private void SetEventPopup()
        {
            if (_currentUserEventData == null) return;

            ClearPopup();

            _currentUserEventConditionDataList = UserDataManager.Instance.GetUserEventConditionDataList(_currentUserEventData.EventId);

            foreach (var eventConditionData in _currentUserEventConditionDataList)
            {
                GameObject newEventSlotObject = Instantiate(_eventSlotObject, _eventSlotScrollRect.content);
                SessionTimeEventSlot newEventSlot = newEventSlotObject.GetComponent<SessionTimeEventSlot>();
                newEventSlot.SetEventSlot(_currentUserEventData, eventConditionData);

                _sessionTimeEventSlotList.Add(newEventSlot);
            }
        }

        private void SetProgressBar()
        {
            _eventProgressBar.maxValue = _specEventConditionDataList.Max(data => data.need_count);
            _eventProgressBar.value = _currentUserEventData.ActionCount;
        }

        private void OnClickCloseButton()
        {
            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_btn_touch);

            SceneUILayerManager.Instance.PopUILayer(this);
        }

        private void ClearPopup()
        {
            BMUtil.RemoveChildObjects(_eventSlotScrollRect.content);

            _sessionTimeEventSlotList.Clear();
        }
    }
}
