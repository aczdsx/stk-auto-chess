using System.Collections.Generic;
using CookApps.TeamBattle.UIManagements;
using R3;
using Tech.Hive.V1;
using UnityEngine;
using UnityEngine.UI;

namespace CookApps.AutoBattler
{
    public class SessionTimeEventPopup : UILayerPopupBase
    {
        [Header("Common")]
        [SerializeField] private CAButton closeButton;
        [SerializeField] private CAButton dimCloseButton;

        [Header("Event Slot")]
        [SerializeField] private Slider eventProgressBar;
        [SerializeField] private ScrollRect eventSlotScrollRect;
        [SerializeField] private GameObject eventSlotObject;

        private List<SessionTimeEventSlot> sessionTimeEventSlotList = new List<SessionTimeEventSlot>();

        private uint eventId;
        private EventData currentEventData;
        private List<EventCondition> specEventConditionDataList;
        private Dictionary<uint, EventCondition> conditionLookup;

        protected override void Awake()
        {
            base.Awake();
            closeButton.OnClickAsObservable().Subscribe(this, (_, self) => self.OnClickCloseButton()).AddTo(this);
            dimCloseButton.OnClickAsObservable().Subscribe(this, (_, self) => self.OnClickCloseButton()).AddTo(this);

            // 이벤트 데이터 갱신 구독
            ServerDataManager.Instance.Event.OnEventUpdated
                .Subscribe(this, (updatedEventId, self) => self.OnEventDataUpdated(updatedEventId))
                .AddTo(this);
        }

        protected override void OnPreEnter(object param)
        {
            base.OnPreEnter(param);

            eventId = (uint)param;
            currentEventData = ServerDataManager.Instance.Event.GetEvent(eventId);

            if (currentEventData == null)
            {
                Debug.LogWarning($"[SessionTimeEventPopup] EventData not found for eventId: {eventId}");
                return;
            }

            specEventConditionDataList = SpecDataManager.Instance.GetSpecEventConditionList((int)eventId);

            // Dictionary로 조회 최적화
            conditionLookup = new Dictionary<uint, EventCondition>(specEventConditionDataList.Count);
            for (int i = 0; i < specEventConditionDataList.Count; i++)
            {
                var condition = specEventConditionDataList[i];
                conditionLookup[(uint)condition.event_condition_id] = condition;
            }

            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_btn_popup);

            SetEventPopup();
            SetProgressBar();
        }

        private void OnEventDataUpdated(uint updatedEventId)
        {
            if (updatedEventId != eventId) return;

            currentEventData = ServerDataManager.Instance.Event.GetEvent(eventId);
            if (currentEventData == null) return;

            RefreshSlots();
            SetProgressBar();
        }

        private void SetEventPopup()
        {
            if (currentEventData == null) return;

            ClearPopup();

            for (int i = 0; i < currentEventData.Conditions.Count; i++)
            {
                var eventConditionData = currentEventData.Conditions[i];
                conditionLookup.TryGetValue(eventConditionData.EventConditionId, out var targetConditionData);

                var newEventSlotObject = Instantiate(eventSlotObject, eventSlotScrollRect.content);
                var newEventSlot = newEventSlotObject.GetComponent<SessionTimeEventSlot>();
                newEventSlot.SetEventSlot(currentEventData, eventConditionData, targetConditionData);

                sessionTimeEventSlotList.Add(newEventSlot);
            }
        }

        private void RefreshSlots()
        {
            if (currentEventData == null) return;

            for (int i = 0; i < sessionTimeEventSlotList.Count; i++)
            {
                var slot = sessionTimeEventSlotList[i];
                if (i < currentEventData.Conditions.Count)
                {
                    var eventConditionData = currentEventData.Conditions[i];
                    conditionLookup.TryGetValue(eventConditionData.EventConditionId, out var targetConditionData);
                    slot.SetEventSlot(currentEventData, eventConditionData, targetConditionData);
                }
            }
        }

        private void SetProgressBar()
        {
            if (specEventConditionDataList.Count == 0) return;

            var minCount = specEventConditionDataList[0].need_count;
            var maxCount = minCount;

            for (int i = 1; i < specEventConditionDataList.Count; i++)
            {
                var needCount = specEventConditionDataList[i].need_count;
                if (needCount < minCount) minCount = needCount;
                if (needCount > maxCount) maxCount = needCount;
            }

            eventProgressBar.minValue = minCount;
            eventProgressBar.maxValue = maxCount;
            eventProgressBar.value = currentEventData.CurrentCount;
        }

        private void OnClickCloseButton()
        {
            SceneUILayerManager.Instance.PopUILayer(this);
        }

        private void ClearPopup()
        {
            BMUtil.RemoveChildObjects(eventSlotScrollRect.content);

            sessionTimeEventSlotList.Clear();
        }
    }
}
