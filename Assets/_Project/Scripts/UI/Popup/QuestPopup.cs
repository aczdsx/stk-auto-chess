using System.Collections;
using System.Collections.Generic;
using CookApps.TeamBattle.UIManagements;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

namespace CookApps.AutoBattler
{
    [RegisterUILayer(UILayerType.Popup, "Prefabs/UI/01_Pops/WindowPopup/QuestPopup.prefab")]
    public class QuestPopup : UILayer
    {
        [SerializeField] private CAButton _closeButton;
        [SerializeField] private CAButton _dimCloseButton;

        [Space]
        [SerializeField] private CAButton _dailyQuestTabButton;
        [SerializeField] private GameObject _dailyTabButtonNormalObject;
        [SerializeField] private GameObject _dailyTabButtonActiveObject;
        [SerializeField] private CAButton _weeklyQuestTabButton;
        [SerializeField] private GameObject _weeklyTabButtonNormalObject;
        [SerializeField] private GameObject _weeklyTabButtonActiveObject;

        [Space]
        [SerializeField] private ScrollRect _questSlotScrollRect;
        [SerializeField] private GameObject _questSlotPrefabObject;

        private List<SpecQuest> _specQuestDataList;
        private List<QuestSlot> _questSlotList = new List<QuestSlot>();

        private TermType _currentTabTermType = TermType.DAILY;

        protected override void Awake()
        {
            base.Awake();

            _closeButton.onClick.AddListener(OnClickCloseButton);
            //_dimCloseButton.onClick.AddListener(OnClickCloseButton);
            _dailyQuestTabButton.onClick.AddListener(OnClickDailyQuestButton);
            _weeklyQuestTabButton.onClick.AddListener(OnClickWeeklyQuestButton);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            _closeButton.onClick.RemoveListener(OnClickCloseButton);
            //_dimCloseButton.onClick.RemoveListener(OnClickCloseButton);
            _dailyQuestTabButton.onClick.RemoveListener(OnClickDailyQuestButton);
            _weeklyQuestTabButton.onClick.RemoveListener(OnClickWeeklyQuestButton);
        }

        protected override void OnPreEnter(object param)
        {
            base.OnPreEnter(param);
            //TopCurrencyAndMenuBar.AddToUILayer(this, TopPanelType.CloseButton);

            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_btn_popup);

            _currentTabTermType = TermType.DAILY;

            CheckQuestRefreshState();

            SetQuestPopup(_currentTabTermType);
        }

        public void RefreshPopup()
        {
            if (_questSlotList == null || _questSlotList.Count <= 0) return;

            _questSlotList.ForEach(slot => slot.RefreshQuestSlot(true));
        }

        // 퀘스트 초기화 시간이 지났는지 체크
        private void CheckQuestRefreshState()
        {
            // 일일 퀘스트 체크
            if (UserDataManager.Instance.UserQuest.NextDailyRefreshTimestamp <= TimeManager.Instance.UtcNowTimeStamp())
            {
                UserDataManager.Instance.UpdateNextRefreshTimeStamp(TermType.DAILY, true);
            }

            // 주간 퀘스트 체크
            if (UserDataManager.Instance.UserQuest.NextWeeklyRefreshTimestamp <= TimeManager.Instance.UtcNowTimeStamp())
            {
                UserDataManager.Instance.UpdateNextRefreshTimeStamp(TermType.WEEKLY, true);
            }
        }

        private void SetQuestPopup(TermType termType)
        {
            _specQuestDataList = SpecDataManager.Instance.GetSpecQuestList(termType);

            SetQuestSlotList();

            UpdateTabButton();

            _questSlotScrollRect.verticalNormalizedPosition = 1;
        }

        private void SetQuestSlotList()
        {
            if (_specQuestDataList == null || _specQuestDataList.Count <= 0) return;

            ClearLayer();

            foreach (var specData in _specQuestDataList)
            {
                GameObject newSlotObject = Instantiate(_questSlotPrefabObject, _questSlotScrollRect.content);
                QuestSlot newSlot = newSlotObject.GetComponent<QuestSlot>();
                newSlot.SetQuestSlot(this, specData);

                _questSlotList.Add(newSlot);
            }
        }

        private void UpdateTabButton()
        {
            _dailyTabButtonNormalObject.SetActive(_currentTabTermType != TermType.DAILY);
            _dailyTabButtonActiveObject.SetActive(_currentTabTermType == TermType.DAILY);

            _weeklyTabButtonNormalObject.SetActive(_currentTabTermType != TermType.WEEKLY);
            _weeklyTabButtonActiveObject.SetActive(_currentTabTermType == TermType.WEEKLY);
        }

        private void OnClickDailyQuestButton()
        {
            if (_currentTabTermType == TermType.DAILY) return;

            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_btn_popup);

            _currentTabTermType = TermType.DAILY;

            SetQuestPopup(_currentTabTermType);
        }

        private void OnClickWeeklyQuestButton()
        {
            if (_currentTabTermType == TermType.WEEKLY) return;

            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_btn_popup);

            _currentTabTermType = TermType.WEEKLY;

            SetQuestPopup(_currentTabTermType);
        }

        private void OnClickCloseButton()
        {
            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_btn_confirm);

            SceneUILayerManager.Instance.PopUILayer(this);
        }

        private void ClearLayer()
        {
            BMUtil.RemoveChildObjects(_questSlotScrollRect.content);

            _questSlotList.Clear();
        }
    }
}
