using System;
using System.Collections;
using System.Collections.Generic;
using CookApps.TeamBattle;
using CookApps.TeamBattle.UIManagements;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CookApps.AutoBattler
{
    public class ChapterListItemSlot : CachedMonoBehaviour
    {
        [SerializeField] private GameObject _selectedLayerObject;
        [SerializeField] private GameObject _chapterStarLayerObject;
        [SerializeField] private GameObject _dimmedLayerObject;
        [SerializeField] private CAButton _chapterButton;

        [Space(10)]
        [SerializeField] private Image _chapterImage;

        [Space(10)]
        [SerializeField] private TextMeshProUGUI _chapterNumberText;
        [SerializeField] private TextMeshProUGUI _chapterNameText;
        [SerializeField] private TextMeshProUGUI _chapterStarCountText;

        private SpecChapter _specChapterData;

        public SpecChapter SpecChapterData => _specChapterData;

        private void Awake()
        {
            _chapterButton.onClick.AddListener(OnClickChapter);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            _chapterButton.onClick.RemoveListener(OnClickChapter);
        }

        public void SetChapterItemSlot(SpecChapter data)
        {
            if (data == null) return;

            _specChapterData = data;

            // 기본 데이터 세팅
            _chapterNumberText.text = string.Format("챕터-{0}-{1}", _specChapterData.chapter_id, _specChapterData.difficulty_type);
            _chapterNameText.text = _specChapterData.name_token;

            // 진행 상태에 따른 처리
            bool isPlayableChapter = UserDataManager.Instance.IsChapterOpen(_specChapterData.chapter_id, _specChapterData.difficulty_type);

            _chapterStarLayerObject.SetActive(isPlayableChapter);
            _dimmedLayerObject.SetActive(!isPlayableChapter);

            int currentChapterStarCount = UserDataManager.Instance.GetTotalChapterStarCount(_specChapterData.chapter_id, _specChapterData.difficulty_type);
            int totalChapterStarCount = SpecDataManager.Instance.GetTotalChapterStarCount(_specChapterData.chapter_id, _specChapterData.difficulty_type);

            _chapterStarCountText.text = string.Format("{0}/{1}", currentChapterStarCount, totalChapterStarCount);
        }

        public void SetSelectedLayer(int selectedChapterID)
        {
            if (_specChapterData == null) return;

            _selectedLayerObject.SetActive(_specChapterData.id == selectedChapterID);
        }

        private void OnClickChapter()
        {
            if (_specChapterData == null) return;

            bool isPlayableChapter = UserDataManager.Instance.IsChapterOpen(_specChapterData.chapter_id, _specChapterData.difficulty_type);
            if (isPlayableChapter == false) return;

            var chapterListPop = SceneUILayerManager.Instance.GetUILayer<ChapterListPopup>();
            if (chapterListPop != null)
            {
                chapterListPop.RefreshSelectedLayer(_specChapterData.id, false);
            }
        }
    }
}
