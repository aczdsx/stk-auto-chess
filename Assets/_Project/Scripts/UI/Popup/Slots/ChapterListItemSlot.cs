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

        private SpecChapter _chapterSpecData;

        public SpecChapter ChapterData => _chapterSpecData;

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

            _chapterSpecData = data;

            // 기본 데이터 세팅
            _chapterNumberText.text = string.Format("챕터-{0}-{1}", _chapterSpecData.chapter_id, _chapterSpecData.difficulty_type);
            _chapterNameText.text = _chapterSpecData.name_token;

            // 진행 상태에 따른 처리
            bool isPlayableChapter = false;

            int lastStageID = UserDataManager.Instance.GetLastStageId(_chapterSpecData.difficulty_type);
            if (lastStageID > 0)
            {
                SpecStage lastStageSpecData = SpecDataManager.Instance.SpecStage.Get(lastStageID);

                isPlayableChapter = _chapterSpecData.chapter_id <= lastStageSpecData.chapter_id;
            }

            _chapterStarLayerObject.SetActive(isPlayableChapter);
            _dimmedLayerObject.SetActive(!isPlayableChapter);

            int currentChapterStarCount = UserDataManager.Instance.GetTotalChapterStarCount(_chapterSpecData.chapter_id, _chapterSpecData.difficulty_type);
            int totalChapterStarCount = SpecDataManager.Instance.GetTotalChapterStarCount(_chapterSpecData.chapter_id, _chapterSpecData.difficulty_type);

            _chapterStarCountText.text = string.Format("{0}/{1}", currentChapterStarCount, totalChapterStarCount);

            // var lastStageData = SpecDataManager.Instance.GetLastStageData(_chapterSpecData.chapter_id, _chapterSpecData.difficulty);
            // var userStageData = UserDataManager.Instance.GetUserStage(lastStageData.id);
            //

            //
            // userStageData.StarCount > 0
        }

        public void SetSelectedLayer(int selectedChapterID)
        {
            if (_chapterSpecData == null) return;

            _selectedLayerObject.SetActive(_chapterSpecData.id == selectedChapterID);
        }

        private void OnClickChapter()
        {
            if (_chapterSpecData == null) return;

            var chapterListPop = SceneUILayerManager.Instance.GetUILayer<ChapterListPopup>();
            if (chapterListPop != null)
            {
                chapterListPop.RefreshSelectedLayer(_chapterSpecData.id);
            }
        }
    }
}
