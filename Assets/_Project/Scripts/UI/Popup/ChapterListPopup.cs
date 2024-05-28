using System.Collections;
using System.Collections.Generic;
using CookApps.TeamBattle.UIManagements;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CookApps.AutoBattler
{
    [RegisterUILayer(UILayerType.Popup, "Prefabs/UI/01_Pops/Pop_ChapterList.prefab")]
    public class ChapterListPopup : UILayer
    {
        [Header("Chapter List Layer")]
        [SerializeField] private ScrollRect _chapterScrollRect;
        [SerializeField] private GameObject _chapterSlotObject;

        [Header("Chapter Progress Layer")]
        [SerializeField] private TextMeshProUGUI _chapterNumberText;
        [SerializeField] private TextMeshProUGUI _chapterNameText;
        [SerializeField] private TextMeshProUGUI _chapterStarCountText;

        [Space(10)]
        [SerializeField] private Slider _chapterProgressSlider;

        private List<ChapterListItemSlot> _chapterSlotList = new();

        private Chapter _selectedChapterData;

        protected override void OnPreEnter(object param)
        {
            base.OnPreEnter(param);
            TopCurrencyAndMenuBar.AddToUILayer(this, TopPanelType.CloseButton);

            var currentChapterId = UserDataManager.Instance.UserStageGroup.CurrentSelectedChapterId;
            _selectedChapterData = SpecDataManager.Instance.Chapter.Get(currentChapterId);

            SetChapterListUI();

            RefreshSelectedLayer(_selectedChapterData.id);

            _chapterScrollRect.verticalNormalizedPosition = 1;
        }

        public void RefreshUI()
        {
            SetChapterListUI();
        }

        public void RefreshSelectedLayer(int targetID)
        {
            if (_chapterSlotList == null || _chapterSlotList.Count <= 0) return;

            // 유저 데이터 처리
            UserDataManager.Instance.SelectUserChapter(targetID);

            // 팝업 관련 처리
            _selectedChapterData = SpecDataManager.Instance.Chapter.Get(targetID);;

            _chapterNumberText.text = string.Format("챕터-{0}-{1}", _selectedChapterData.chapter_id, _selectedChapterData.difficulty);
            _chapterNameText.text = _selectedChapterData.name_token;

            int currentChapterStarCount = UserDataManager.Instance.GetTotalChapterStarCount(_selectedChapterData.chapter_id, _selectedChapterData.difficulty);
            int totalChapterStarCount = SpecDataManager.Instance.GetTotalChapterStarCount(_selectedChapterData.chapter_id, _selectedChapterData.difficulty);

            _chapterStarCountText.text = string.Format("{0}/{1}", currentChapterStarCount, totalChapterStarCount);

            _chapterSlotList.ForEach(slot => slot.SetSelectedLayer(_selectedChapterData.id));

            // 로비 메인 하단 스테이지 UI 갱신
            var lobbyMain = SceneUILayerManager.Instance.GetUILayer<LobbyMain>();
            if (lobbyMain != null)
            {
                lobbyMain.RefreshUI();
            }
        }

        private void SetChapterListUI()
        {
            ClearList();

            foreach (var chapterData in SpecDataManager.Instance.Chapter.All)
            {
                GameObject newChapterObject = Instantiate(_chapterSlotObject, _chapterScrollRect.content);
                ChapterListItemSlot chapterSlot = newChapterObject.GetComponent<ChapterListItemSlot>();
                chapterSlot.SetChapterItemSlot(chapterData);

                _chapterSlotList.Add(chapterSlot);
            }
        }

        private void ClearList()
        {
            _chapterSlotList.Clear();

            BMUtil.RemoveChildObjects(_chapterScrollRect.content);
        }
    }
}
