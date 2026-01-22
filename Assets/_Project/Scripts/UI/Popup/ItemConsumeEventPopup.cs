using System.Collections.Generic;
using CookApps.TeamBattle.UIManagements;
using R3;
using Tech.Hive.V1;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CookApps.AutoBattler
{
    public class ItemConsumeEventPopup : UILayerPopupBase
    {
        [Header("Common")]
        [SerializeField] private CAButton closeButton;
        [SerializeField] private CAButton dimCloseButton;
        [SerializeField] private TextMeshProUGUI eventTitleText;
        [SerializeField] private TextMeshProUGUI eventDescText;
        [SerializeField] private TextMeshProUGUI currentConsumeAmountText;

        [Header("Event Slot")]
        [SerializeField] private ScrollRect eventSlotScrollRect;
        [SerializeField] private GameObject eventSlotObject;

        private List<ConsumeEventSlot> consumeEventSlotList = new List<ConsumeEventSlot>();

        private uint eventId;
        private EventData currentEventData;
        private EventInfo specEventData;

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
                Debug.LogWarning($"[ItemConsumeEventPopup] EventData not found for eventId: {eventId}");
                return;
            }

            specEventData = SpecDataManager.Instance.GetSpecEventData((int)eventId);

            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_btn_popup);

            SetEventPopup();
            SetEventSlotList();
        }

        private void OnEventDataUpdated(uint updatedEventId)
        {
            if (updatedEventId != eventId) return;

            currentEventData = ServerDataManager.Instance.Event.GetEvent(eventId);
            if (currentEventData == null) return;

            SetEventPopup();
            RefreshSlots();
        }

        private void SetEventPopup()
        {
            currentConsumeAmountText.text = $"x{currentEventData.CurrentCount}";
            eventTitleText.text = LanguageManager.Instance.GetDefaultText(specEventData.name_token);
            eventDescText.text = LanguageManager.Instance.GetDefaultText(specEventData.desc_token);
        }

        private void SetEventSlotList()
        {
            if (currentEventData == null) return;

            ClearPopup();

            for (int i = 0; i < currentEventData.Conditions.Count; i++)
            {
                var eventConditionData = currentEventData.Conditions[i];
                var newEventSlotObject = Instantiate(eventSlotObject, eventSlotScrollRect.content);
                var newEventSlot = newEventSlotObject.GetComponent<ConsumeEventSlot>();
                newEventSlot.SetEventSlot(currentEventData, eventConditionData);

                consumeEventSlotList.Add(newEventSlot);
            }

            eventSlotScrollRect.horizontalNormalizedPosition = 0;
        }

        private void RefreshSlots()
        {
            if (currentEventData == null) return;

            for (int i = 0; i < consumeEventSlotList.Count; i++)
            {
                var slot = consumeEventSlotList[i];
                if (i < currentEventData.Conditions.Count)
                {
                    slot.SetEventSlot(currentEventData, currentEventData.Conditions[i]);
                }
            }
        }

        private void OnClickCloseButton()
        {
            SceneUILayerManager.Instance.PopUILayer(this);
        }

        private void ClearPopup()
        {
            BMUtil.RemoveChildObjects(eventSlotScrollRect.content);

            consumeEventSlotList.Clear();
        }
    }
}
