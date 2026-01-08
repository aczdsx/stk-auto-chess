using System;
using System.Collections;
using System.Collections.Generic;
using CookApps.TeamBattle;
using CookApps.TeamBattle.UIManagements;
using Cysharp.Threading.Tasks;
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
        [SerializeField] private GameObject _reddotObject;

        [Space(10)]
        [SerializeField] private Image _chapterImage;
        [SerializeField] private SpriteLoader _chapterSpriteLoader;

        [Space(10)]
        [SerializeField] private TextMeshProUGUI _chapterNumberText;
        [SerializeField] private TextMeshProUGUI _chapterNameText;
        [SerializeField] private TextMeshProUGUI _chapterStarCountText;

        private ChapterInfo _specChapterData;
        public ChapterInfo SpecChapterData => _specChapterData;

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

        public void SetChapterItemSlot(ChapterInfo data, ChapterListPopup parent)
        {
            if (data == null) return;

            _specChapterData = data;
            _parentPopup = parent;

            // 기본 데이터 세팅
            string chapterString = LanguageManager.Instance.GetLanguageText("UI_CHAPTER");
            _chapterNumberText.text = $"{chapterString}-{_specChapterData.chapter_id}-{_specChapterData.difficulty_type}";
            _chapterNameText.text = LanguageManager.Instance.GetLanguageText(_specChapterData.name_token);

            _chapterSpriteLoader.SetSprite(SpriteNameParser.GetChapterIcon(_specChapterData.chapter_id)).Forget();

            // 진행 상태에 따른 처리
            _isPlayableChapter = ServerDataManager.Instance.Battle.IsChapterOpen((uint)_specChapterData.chapter_id, _specChapterData.difficulty_type);

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

            int currentChapterStarCount = (int)ServerDataManager.Instance.Battle.GetTotalChapterStarCount((uint)_specChapterData.chapter_id, _specChapterData.difficulty_type);
            int totalChapterStarCount = SpecDataManager.Instance.GetTotalChapterStarCount(_specChapterData.chapter_id, _specChapterData.difficulty_type);

            _chapterStarCountText.text = string.Format("{0}/{1}", currentChapterStarCount, totalChapterStarCount);

            // 레드닷 세팅
            UpdateReddotState();
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

        public void UpdateReddotState()
        {
            bool isAvailGetChapterReward = false;

            int totalChapterStarCount = (int)ServerDataManager.Instance.Battle.GetTotalChapterStarCount((uint)_specChapterData.chapter_id, _specChapterData.difficulty_type);
            if (totalChapterStarCount > 0)
            {
                var rewardInfoList = SpecDataManager.Instance.GetSpecRewardInfoList(ContentType.STAGE_STAR, _specChapterData.chapter_id, _specChapterData.difficulty_type);
                foreach (var rewardInfoData in rewardInfoList)
                {
                    bool checkGetReward = totalChapterStarCount >= rewardInfoData.sub_value;

                    bool checkAlreadyGetReward = ServerDataManager.Instance.Battle.IsGetStageAccReward(rewardInfoData.content_key_value,
                        rewardInfoData.difficulty_type, rewardInfoData.sub_value);

                    if (checkGetReward && !checkAlreadyGetReward)
                    {
                        isAvailGetChapterReward = true;
                        break;
                    }
                }
            }

            _reddotObject.SetActive(isAvailGetChapterReward);
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
