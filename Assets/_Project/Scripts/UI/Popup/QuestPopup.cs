using System.Collections.Generic;
using System.Linq;
using CookApps.TeamBattle.UIManagements;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;
using UnityEngine.UI;

namespace CookApps.AutoBattler
{
    public class QuestPopup : UILayerPopupBase
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

        [Header("MileStone Layer")]
        [SerializeField] private Slider _mileStoneSlider;
        [SerializeField] private List<QuestClearRewardGaugeSlot> _mileStoneRewardSlotList;

        private List<QuestInfo> _specNormalQuestDataList;
        private List<QuestInfo> _specMileStoneQuestDataList;
        private List<QuestSlot> _questSlotList = new List<QuestSlot>();

        private TermType _currentTabTermType = TermType.DAILY;

        protected override void Awake()
        {
            base.Awake();

            _closeButton.OnClickAsObservable().Subscribe(this, (_, self) => self.OnClickCloseButton()).AddTo(this);
            _dimCloseButton.OnClickAsObservable().Subscribe(this, (_, self) => self.OnClickCloseButton()).AddTo(this);
            _dailyQuestTabButton.OnClickAsObservable().Subscribe(this, (_, self) => self.OnClickDailyQuestButton()).AddTo(this);
            _weeklyQuestTabButton.OnClickAsObservable().Subscribe(this, (_, self) => self.OnClickWeeklyQuestButton()).AddTo(this);
        }

        protected override void OnPreEnter(object param)
        {
            base.OnPreEnter(param);
            //TopCurrencyAndMenuBar.AddToUILayer(this, TopPanelType.CloseButton);

            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_btn_popup);

            _currentTabTermType = TermType.DAILY;

            // 서버에서 퀘스트 데이터 갱신
            RefreshQuestDataAsync().Forget();
        }

        private async UniTaskVoid RefreshQuestDataAsync()
        {
            await NetManager.Instance.Quest.ListDailyQuestAsync();
            SetQuestPopup(_currentTabTermType);
        }

        public void RefreshPopup()
        {
            if (_questSlotList == null || _questSlotList.Count <= 0) return;
            if (_mileStoneRewardSlotList == null || _mileStoneRewardSlotList.Count <= 0) return;

            _questSlotList.ForEach(slot => slot.RefreshQuestSlot(true));
            _mileStoneRewardSlotList.ForEach(slot => slot.RefreshSlot(true));

            SetQuestMileStoneSlider();
        }

        private void SetQuestPopup(TermType termType)
        {
            _specNormalQuestDataList = SpecDataManager.Instance.GetSpecQuestList(termType, false);

            QuestType targetMilestoneQuestType = termType == TermType.DAILY ? QuestType.CLEAR_DAILY_QUEST : QuestType.CLEAR_WEEKLY_QUEST;
            _specMileStoneQuestDataList = SpecDataManager.Instance.GetSpecQuestList(termType, targetMilestoneQuestType);

            SetQuestSlotList();
            SetQuestMileStoneLayer();
            SetQuestMileStoneSlider();

            UpdateTabButton();

            _questSlotScrollRect.verticalNormalizedPosition = 1;
        }

        private void SetQuestSlotList()
        {
            if (_specNormalQuestDataList == null || _specNormalQuestDataList.Count <= 0) return;

            ClearLayer();

            foreach (var specData in _specNormalQuestDataList)
            {
                GameObject newSlotObject = Instantiate(_questSlotPrefabObject, _questSlotScrollRect.content);
                QuestSlot newSlot = newSlotObject.GetComponent<QuestSlot>();
                newSlot.SetQuestSlot(this, specData);

                _questSlotList.Add(newSlot);
            }
        }

        private void SetQuestMileStoneLayer()
        {
            if (_mileStoneRewardSlotList == null || _mileStoneRewardSlotList.Count <= 0) return;
            if (_specMileStoneQuestDataList == null || _specMileStoneQuestDataList.Count <= 0) return;

            if (_mileStoneRewardSlotList.Count != _specMileStoneQuestDataList.Count) return;

            for (int i = 0; i < _specMileStoneQuestDataList.Count; ++i)
            {
                _mileStoneRewardSlotList[i].SetQuestGaugeSlot(this, _specMileStoneQuestDataList[i]);
            }
        }

        private void SetQuestMileStoneSlider()
        {
            if (_specMileStoneQuestDataList == null || _specMileStoneQuestDataList.Count <= 0) return;

            // 슬라이더 세팅
            _mileStoneSlider.maxValue = _specMileStoneQuestDataList.Max(data => data.need_count);

            // 마일스톤 퀘스트의 CurrentCount를 사용하여 클리어 카운트 가져오기
            var firstMilestoneQuest = ServerDataManager.Instance.Quest.GetQuest(_specMileStoneQuestDataList[0].quest_id);
            _mileStoneSlider.value = firstMilestoneQuest?.CurrentCount ?? 0;
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
