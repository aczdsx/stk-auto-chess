using System.Collections.Generic;
using Cysharp.Text;
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

        [Header("Star Count Text Colors")]
        [SerializeField] private Color currentStarColor = new Color(0.271f, 0.298f, 0.396f); // #454C65
        [SerializeField] private Color totalStarColor = new Color(0.490f, 0.502f, 0.557f);   // #7D808E

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

            var currentHex = ColorUtility.ToHtmlStringRGB(currentStarColor);
            var totalHex = ColorUtility.ToHtmlStringRGB(totalStarColor);
            chapterStarCountText.text = ZString.Format("<size=34><color=#{0}>{1}</color></size><size=30><color=#{2}>/{3}</color></size>",
                currentHex, currentChapterStarCount, totalHex, totalChapterStarCount);

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
