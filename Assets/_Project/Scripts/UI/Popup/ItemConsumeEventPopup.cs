using System.Collections;
using System.Collections.Generic;
using Cookapps.Stkauto.V1;
using CookApps.TeamBattle.UIManagements;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CookApps.AutoBattler
{
    [RegisterUILayer(UILayerType.Popup, "Prefabs/UI/01_Pops/WindowPopup/ItemConsumeEventPopup.prefab")]
    public class ItemConsumeEventPopup : UILayer
    {
        [Header("Common")]
        [SerializeField] private CAButton _closeButton;
        [SerializeField] private CAButton _dimCloseButton;
        [SerializeField] private TextMeshProUGUI _eventTitleText;
        [SerializeField] private TextMeshProUGUI _eventDescText;

        [Header("Event Slot")]
        [SerializeField] private ScrollRect _eventSlotScrollRect;
        [SerializeField] private GameObject _eventSlotObject;

        private List<ConsumeEventSlot> _consumeEventSlotList = new List<ConsumeEventSlot>();

        private UserEventData _currentUserEventData;
        private List<UserEventConditionData> _currentUserEventConditionDataList;

        private SpecEvent _specEventData;
        private List<SpecEventCondition> _specEventConditionDataList;

        private void Awake()
        {
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

            _currentUserEventData = param as UserEventData;

            _specEventData = SpecDataManager.Instance.GetSpecEventData(_currentUserEventData.EventId);
            _specEventConditionDataList = SpecDataManager.Instance.GetSpecEventConditionList(_currentUserEventData.EventId);

            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_btn_popup);

            UpdateEventData();
            SetEventPopup();
            SetEventSlotList();
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
            _eventTitleText.text = LanguageManager.Instance.GetLanguageText(_specEventData.name_token);
            _eventDescText.text = LanguageManager.Instance.GetLanguageText(_specEventData.desc_token);
        }

        private void SetEventSlotList()
        {
            if (_currentUserEventData == null) return;

            ClearPopup();

            _currentUserEventConditionDataList = UserDataManager.Instance.GetUserEventConditionDataList(_currentUserEventData.EventId);

            foreach (var eventConditionData in _currentUserEventConditionDataList)
            {
                GameObject newEventSlotObject = Instantiate(_eventSlotObject, _eventSlotScrollRect.content);
                ConsumeEventSlot newEventSlot = newEventSlotObject.GetComponent<ConsumeEventSlot>();
                newEventSlot.SetEventSlot(_currentUserEventData, eventConditionData);

                _consumeEventSlotList.Add(newEventSlot);
            }

            _eventSlotScrollRect.horizontalNormalizedPosition = 0;
        }

        private void OnClickCloseButton()
        {
            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_btn_touch);

            SceneUILayerManager.Instance.PopUILayer(this);
        }

        private void ClearPopup()
        {
            BMUtil.RemoveChildObjects(_eventSlotScrollRect.content);

            _consumeEventSlotList.Clear();
        }
    }
}
