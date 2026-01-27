using System.Collections.Generic;
using CookApps.TeamBattle.UIManagements;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;
using UnityEngine.UI;

namespace CookApps.AutoBattler
{
    public class QuestPopup : UILayerPopupBase
    {
        [SerializeField] private CAButton closeButton;
        [SerializeField] private CAButton dimCloseButton;

        [Space]
        [SerializeField] private CAButton dailyQuestTabButton;
        [SerializeField] private GameObject dailyTabButtonNormalObject;
        [SerializeField] private GameObject dailyTabButtonActiveObject;
        [SerializeField] private CAButton weeklyQuestTabButton;
        [SerializeField] private GameObject weeklyTabButtonNormalObject;
        [SerializeField] private GameObject weeklyTabButtonActiveObject;

        [Space]
        [SerializeField] private ScrollRect questSlotScrollRect;
        [SerializeField] private GameObject questSlotPrefabObject;

        [Header("MileStone Layer")]
        [SerializeField] private Slider mileStoneSlider;
        [SerializeField] private List<QuestClearRewardGaugeSlot> mileStoneRewardSlotList;

        private List<QuestInfo> specNormalQuestDataList;
        private List<QuestInfo> specMileStoneQuestDataList;
        private List<QuestSlot> questSlotList = new List<QuestSlot>();

        private TermType currentTabTermType = TermType.DAILY;

        protected override void Awake()
        {
            base.Awake();

            closeButton.OnClickAsObservable().Subscribe(this, (_, self) => self.OnClickCloseButton()).AddTo(this);
            dimCloseButton.OnClickAsObservable().Subscribe(this, (_, self) => self.OnClickCloseButton()).AddTo(this);
            dailyQuestTabButton.OnClickAsObservable().Subscribe(this, (_, self) => self.OnClickDailyQuestButton()).AddTo(this);
            weeklyQuestTabButton.OnClickAsObservable().Subscribe(this, (_, self) => self.OnClickWeeklyQuestButton()).AddTo(this);
        }

        protected override void OnPreEnter(object param)
        {
            base.OnPreEnter(param);
            //TopCurrencyAndMenuBar.AddToUILayer(this, TopPanelType.CloseButton);

            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_btn_popup);

            currentTabTermType = TermType.DAILY;

            SetQuestPopup(currentTabTermType);
        }

        public void RefreshPopup()
        {
            if (questSlotList == null || questSlotList.Count <= 0) return;
            if (mileStoneRewardSlotList == null || mileStoneRewardSlotList.Count <= 0) return;

            for (int i = 0; i < questSlotList.Count; i++)
            {
                questSlotList[i].RefreshQuestSlot(true);
            }

            for (int i = 0; i < mileStoneRewardSlotList.Count; i++)
            {
                mileStoneRewardSlotList[i].RefreshSlot(true);
            }

            SetQuestMileStoneSlider();
        }

        private void SetQuestPopup(TermType termType)
        {
            specNormalQuestDataList = SpecDataManager.Instance.GetSpecQuestList(termType, false);

            QuestType targetMilestoneQuestType = termType == TermType.DAILY ? QuestType.CLEAR_DAILY_QUEST : QuestType.CLEAR_WEEKLY_QUEST;
            specMileStoneQuestDataList = SpecDataManager.Instance.GetSpecQuestList(termType, targetMilestoneQuestType);

            SetQuestSlotList();
            SetQuestMileStoneLayer();
            SetQuestMileStoneSlider();

            UpdateTabButton();

            questSlotScrollRect.verticalNormalizedPosition = 1;
        }

        private void SetQuestSlotList()
        {
            if (specNormalQuestDataList == null || specNormalQuestDataList.Count <= 0) return;

            ClearLayer();

            foreach (var specData in specNormalQuestDataList)
            {
                GameObject newSlotObject = Instantiate(questSlotPrefabObject, questSlotScrollRect.content);
                QuestSlot newSlot = newSlotObject.GetComponent<QuestSlot>();
                newSlot.SetQuestSlot(this, specData);

                questSlotList.Add(newSlot);
            }
        }

        private void SetQuestMileStoneLayer()
        {
            if (mileStoneRewardSlotList == null || mileStoneRewardSlotList.Count <= 0) return;
            if (specMileStoneQuestDataList == null || specMileStoneQuestDataList.Count <= 0) return;

            if (mileStoneRewardSlotList.Count != specMileStoneQuestDataList.Count) return;

            for (int i = 0; i < specMileStoneQuestDataList.Count; ++i)
            {
                mileStoneRewardSlotList[i].SetQuestGaugeSlot(this, specMileStoneQuestDataList[i]);
            }
        }

        private void SetQuestMileStoneSlider()
        {
            if (specMileStoneQuestDataList == null || specMileStoneQuestDataList.Count <= 0) return;

            // 슬라이더 세팅 - Max 값 계산
            int maxNeedCount = specMileStoneQuestDataList[0].need_count;
            for (int i = 1; i < specMileStoneQuestDataList.Count; i++)
            {
                if (specMileStoneQuestDataList[i].need_count > maxNeedCount)
                {
                    maxNeedCount = specMileStoneQuestDataList[i].need_count;
                }
            }
            mileStoneSlider.maxValue = maxNeedCount;

            // 마일스톤 퀘스트의 CurrentCount를 사용하여 클리어 카운트 가져오기
            var firstMilestoneQuest = ServerDataManager.Instance.Quest.GetQuest(specMileStoneQuestDataList[0].quest_id);
            mileStoneSlider.value = firstMilestoneQuest?.CurrentCount ?? 0;
        }

        private void UpdateTabButton()
        {
            dailyTabButtonNormalObject.SetActive(currentTabTermType != TermType.DAILY);
            dailyTabButtonActiveObject.SetActive(currentTabTermType == TermType.DAILY);

            weeklyTabButtonNormalObject.SetActive(currentTabTermType != TermType.WEEKLY);
            weeklyTabButtonActiveObject.SetActive(currentTabTermType == TermType.WEEKLY);
        }

        private void OnClickDailyQuestButton()
        {
            if (currentTabTermType == TermType.DAILY) return;

            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_btn_popup);

            currentTabTermType = TermType.DAILY;

            SetQuestPopup(currentTabTermType);
        }

        private void OnClickWeeklyQuestButton()
        {
            if (currentTabTermType == TermType.WEEKLY) return;

            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_btn_popup);

            currentTabTermType = TermType.WEEKLY;

            RefreshWeeklyQuestDataAsync().Forget();
        }

        private async UniTaskVoid RefreshWeeklyQuestDataAsync()
        {
            await NetManager.Instance.Quest.ListDailyQuestAsync();
            SetQuestPopup(currentTabTermType);
        }

        private void OnClickCloseButton()
        {
            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_btn_confirm);

            SceneUILayerManager.Instance.PopUILayer(this);
        }

        private void ClearLayer()
        {
            BMUtil.RemoveChildObjects(questSlotScrollRect.content);

            questSlotList.Clear();
        }
    }
}
