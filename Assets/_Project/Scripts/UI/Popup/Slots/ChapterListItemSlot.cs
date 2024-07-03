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
        [SerializeField] private Animator _baseAnimator;
        [SerializeField] private GameObject _selectedLayerObject;
        [SerializeField] private GameObject _chapterStarLayerObject;
        [SerializeField] private GameObject _dimmedLayerObject;
        [SerializeField] private CAButton _chapterButton;
        [SerializeField] private CAButton _dimmedButton;

        [Space(10)]
        [SerializeField] private Image _chapterImage;

        [Space(10)]
        [SerializeField] private TextMeshProUGUI _chapterNumberText;
        [SerializeField] private TextMeshProUGUI _chapterNameText;
        [SerializeField] private TextMeshProUGUI _chapterStarCountText;

        private SpecChapter _specChapterData;
        public SpecChapter SpecChapterData => _specChapterData;

        private ChapterListPopup _parentPopup;
        private bool _isPlayableChapter = false;

        private void Awake()
        {
            _chapterButton.onClick.AddListener(OnClickChapter);
            _dimmedButton.onClick.AddListener(OnClickDimmedLayerButton);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            _chapterButton.onClick.RemoveListener(OnClickChapter);
            _dimmedButton.onClick.RemoveListener(OnClickDimmedLayerButton);
        }

        public void SetChapterItemSlot(SpecChapter data, ChapterListPopup parent)
        {
            if (data == null) return;

            _specChapterData = data;
            _parentPopup = parent;

            // 기본 데이터 세팅
            _chapterNumberText.text = string.Format("챕터-{0}-{1}", _specChapterData.chapter_id, _specChapterData.difficulty_type);
            _chapterNameText.text = LanguageManager.Instance.GetLanguageText(_specChapterData.name_token);

            _chapterImage.sprite = ImageManager.Instance.GetChapterIconSprite(_specChapterData.chapter_id);

            // 진행 상태에 따른 처리
            _isPlayableChapter = UserDataManager.Instance.IsChapterOpen(_specChapterData.chapter_id, _specChapterData.difficulty_type);

            if (_isPlayableChapter == false)
            {
                _baseAnimator.SetTrigger("SetLock");
            }
            else
            {
                SetSelectedLayer(_parentPopup.SelectedChapterData.id);
            }

            //_chapterStarLayerObject.SetActive(_isPlayableChapter);
            //_dimmedLayerObject.SetActive(!_isPlayableChapter);

            int currentChapterStarCount = UserDataManager.Instance.GetTotalChapterStarCount(_specChapterData.chapter_id, _specChapterData.difficulty_type);
            int totalChapterStarCount = SpecDataManager.Instance.GetTotalChapterStarCount(_specChapterData.chapter_id, _specChapterData.difficulty_type);

            _chapterStarCountText.text = string.Format("{0}/{1}", currentChapterStarCount, totalChapterStarCount);
        }

        public void SetSelectedLayer(int selectedChapterID)
        {
            if (_specChapterData == null) return;

            if (_isPlayableChapter == false)
            {
                _baseAnimator.SetTrigger("SetLock");
            }
            else if (_specChapterData.id == selectedChapterID)
            {
                _baseAnimator.SetTrigger("SetActive");
            }
            else
            {
                _baseAnimator.SetTrigger("SetDefault");
            }
        }

        private void OnClickChapter()
        {
            if (_specChapterData == null) return;
            if (_isPlayableChapter == false) return;

            if (_parentPopup != null)
            {
                _parentPopup.SetSelectedChapterData(_specChapterData.id, false);
            }

            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_btn_touch);
        }

        private void OnClickDimmedLayerButton()
        {
            ToastManager.Instance.ShowToastByTokenKey("MSG_LOCK_CHAPTER");
        }
    }
}
