using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CookApps.AutoBattler
{
    public class StageMilestonePanel : MonoBehaviour
    {
        [Header("Chapter Progress")]
        [SerializeField] private Slider chapterProgressSlider;
        [SerializeField] private TextMeshProUGUI chapterStarCountText;

        [Header("Chapter Star Reward")]
        [SerializeField] private List<ChapterListStarGaugeSlot> chapterStarRewardSlotList;

        private ChapterInfo currentChapterData;

        public void SetChapterData(ChapterInfo chapterData)
        {
            currentChapterData = chapterData;
            RefreshUI();
        }

        public void RefreshUI()
        {
            if (currentChapterData == null) return;

            RefreshProgressUI();
            RefreshRewardLayer();
        }

        private void RefreshProgressUI()
        {
            int currentChapterStarCount = (int)ServerDataManager.Instance.Battle.GetTotalChapterStarCount(
                (uint)currentChapterData.chapter_id, currentChapterData.difficulty_type);
            int totalChapterStarCount = SpecDataManager.Instance.GetTotalChapterStarCount(
                currentChapterData.chapter_id, currentChapterData.difficulty_type);

            chapterStarCountText.text = $"<size=34><color=#454C65>{currentChapterStarCount}</color></size><size=30><color=#7D808E>/{totalChapterStarCount}</color></size>";

            chapterProgressSlider.maxValue = totalChapterStarCount;
            chapterProgressSlider.value = currentChapterStarCount;
        }

        public void RefreshRewardLayer()
        {
            if (currentChapterData == null) return;

            var rewardInfoList = SpecDataManager.Instance.GetStageMilestoneRewardList(
                ContentType.STAGE_STAR, currentChapterData.chapter_id, currentChapterData.difficulty_type);

            if (rewardInfoList != null)
            {
                int count = Mathf.Min(chapterStarRewardSlotList.Count, rewardInfoList.Count);
                for (int i = 0; i < count; ++i)
                {
                    chapterStarRewardSlotList[i].SetStarGaugeSlot(rewardInfoList[i]);
                }
            }
        }
    }
}
