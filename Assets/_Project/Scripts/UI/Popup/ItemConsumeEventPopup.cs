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
        [SerializeField] private CAButton _closeButton;
        [SerializeField] private CAButton _dimCloseButton;
        [SerializeField] private TextMeshProUGUI _eventTitleText;
        [SerializeField] private TextMeshProUGUI _eventDescText;
        [SerializeField] private TextMeshProUGUI _currentConsumeAmountText;

        [Header("Event Slot")]
        [SerializeField] private ScrollRect _eventSlotScrollRect;
        [SerializeField] private GameObject _eventSlotObject;

        private List<ConsumeEventSlot> _consumeEventSlotList = new List<ConsumeEventSlot>();

        private EventData _currentEventData;

        private EventInfo _specEventData;
        private List<EventCondition> _specEventConditionDataList;

        protected override void Awake()
        {
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
            SetEventSlotList();
        }

        private void SetEventPopup()
        {
            _currentConsumeAmountText.text = $"x{_currentEventData.CurrentCount}";
            _eventTitleText.text = LanguageManager.Instance.GetDefaultText(_specEventData.name_token);
            _eventDescText.text = LanguageManager.Instance.GetDefaultText(_specEventData.desc_token);
        }

        private void SetEventSlotList()
        {
            if (_currentEventData == null) return;

            ClearPopup();

            for (int i = 0; i < _currentEventData.Conditions.Count; i++)
            {
                var eventConditionData = _currentEventData.Conditions[i];
                GameObject newEventSlotObject = Instantiate(_eventSlotObject, _eventSlotScrollRect.content);
                ConsumeEventSlot newEventSlot = newEventSlotObject.GetComponent<ConsumeEventSlot>();
                newEventSlot.SetEventSlot(_currentEventData, eventConditionData);

                _consumeEventSlotList.Add(newEventSlot);
            }

            _eventSlotScrollRect.horizontalNormalizedPosition = 0;
        }

        private void OnClickCloseButton()
        {
            SceneUILayerManager.Instance.PopUILayer(this);
        }

        private void ClearPopup()
        {
            BMUtil.RemoveChildObjects(_eventSlotScrollRect.content);

            _consumeEventSlotList.Clear();
        }
    }
}
