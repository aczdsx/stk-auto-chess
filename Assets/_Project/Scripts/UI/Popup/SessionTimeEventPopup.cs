using System.Collections.Generic;
using System.Linq;
using CookApps.TeamBattle.UIManagements;
using R3;
using Tech.Hive.V1;
using UnityEngine;
using UnityEngine.UI;

namespace CookApps.AutoBattler
{
    public class SessionTimeEventPopup : UILayer
    {
        [Header("Common")]
        [SerializeField] private CAButton _closeButton;
        [SerializeField] private CAButton _dimCloseButton;

        [Header("Event Slot")]
        [SerializeField] private Slider _eventProgressBar;
        [SerializeField] private ScrollRect _eventSlotScrollRect;
        [SerializeField] private GameObject _eventSlotObject;

        private List<SessionTimeEventSlot> _sessionTimeEventSlotList = new List<SessionTimeEventSlot>();

        private EventData _currentEventData;

        private EventInfo _specEventData;
        private List<EventCondition> _specEventConditionDataList;

        protected override void Awake()
        {
            base.Awake();
            _closeButton.OnClickAsObservable().Subscribe(this, (_, self) => self.OnClickCloseButton()).AddTo(this);
            _dimCloseButton.OnClickAsObservable().Subscribe(this, (_, self) => self.OnClickCloseButton()).AddTo(this);
        }

        protected override void OnPreEnter(object param)
        {
            base.OnPreEnter(param);

            _currentEventData = param as EventData;

            _specEventData = SpecDataManager.Instance.GetSpecEventData((int)_currentEventData.EventId);
            _specEventConditionDataList = SpecDataManager.Instance.GetSpecEventConditionList((int)_currentEventData.EventId);

            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_btn_popup);

            SetEventPopup();
            SetProgressBar();
        }

        private void SetEventPopup()
        {
            if (_currentEventData == null) return;

            ClearPopup();

            for (int i = 0; i < _currentEventData.Conditions.Count; i++)
            {
                var eventConditionData = _currentEventData.Conditions[i];
                GameObject newEventSlotObject = Instantiate(_eventSlotObject, _eventSlotScrollRect.content);
                SessionTimeEventSlot newEventSlot = newEventSlotObject.GetComponent<SessionTimeEventSlot>();
                newEventSlot.SetEventSlot(_currentEventData, eventConditionData);

                _sessionTimeEventSlotList.Add(newEventSlot);
            }
        }

        private void SetProgressBar()
        {
            _eventProgressBar.minValue = _specEventConditionDataList.Min(data => data.need_count);
            _eventProgressBar.maxValue = _specEventConditionDataList.Max(data => data.need_count);
            _eventProgressBar.value = _currentEventData.CurrentCount;
        }

        private void OnClickCloseButton()
        {
            SceneUILayerManager.Instance.PopUILayer(this);
        }

        private void ClearPopup()
        {
            BMUtil.RemoveChildObjects(_eventSlotScrollRect.content);

            _sessionTimeEventSlotList.Clear();
        }
    }
}
