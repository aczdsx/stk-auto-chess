using System.Collections;
using System.Collections.Generic;
using CookApps.BattleSystem;
using CookApps.TeamBattle.UIManagements;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CookApps.AutoBattler
{
    [RegisterUILayer(UILayerType.Popup, "Prefabs/UI/01_Pops/ChapterListPopup.prefab")]
    public class ChapterListPopup : UILayer
    {
        [SerializeField] private CAButton _closeButton;
        [SerializeField] private CAButton _dimCloseButton;
        [SerializeField] private CAButton _moveChapterButton;

        [Header("Chapter List Layer")]
        [SerializeField] private ScrollRect _chapterScrollRect;
        [SerializeField] private GameObject _chapterSlotObject;

        [Header("Chapter Progress Layer")]
        [SerializeField] private Slider _chapterProgressSlider;
        [SerializeField] private TextMeshProUGUI _chapterNumberText;
        [SerializeField] private TextMeshProUGUI _chapterNameText;
        [SerializeField] private TextMeshProUGUI _chapterStarCountText;

        [Header("Chapter Star Reward Layer")]
        [SerializeField] private List<ChapterListStarGaugeSlot> _chapterStarRewardSlotList;

        private List<ChapterListItemSlot> _chapterSlotList = new();

        private SpecChapter _selectedChapterData;
        public SpecChapter SelectedChapterData => _selectedChapterData;

        private int _targetChapterID;

        protected override void Awake()
        {
            base.Awake();

            _closeButton.onClick.AddListener(OnClickCloseButton);
            _dimCloseButton.onClick.AddListener(OnClickCloseButton);
            _moveChapterButton.onClick.AddListener(OnClickMoveChapterButton);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            _closeButton.onClick.RemoveListener(OnClickCloseButton);
            _dimCloseButton.onClick.RemoveListener(OnClickCloseButton);
            _moveChapterButton.onClick.RemoveListener(OnClickMoveChapterButton);
        }

        protected override void OnPreEnter(object param)
        {
            base.OnPreEnter(param);
            //TopCurrencyAndMenuBar.AddToUILayer(this, TopPanelType.CloseButton);

            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_btn_popup);

            var currentStageId = UserDataManager.Instance.GetLastPlayStageID();
            _selectedChapterData = SpecDataManager.Instance.GetChapterDataByStageID(currentStageId);

            SetChapterListUI();

            SetSelectedChapterData(_selectedChapterData.chapter_id);
            RefreshSelectedLayer(true);

            _chapterScrollRect.verticalNormalizedPosition = 1;

            // 연출 적용
            baseAnimator.SetTrigger("SetEntry");
        }

        public void RefreshUI()
        {

        }

        public void RefreshRewardLayer()
        {
            var rewardInfoList = SpecDataManager.Instance.GetSpecRewardInfoList(ContentType.STAGE_STAR, _selectedChapterData.chapter_id, _selectedChapterData.difficulty_type);
            if (rewardInfoList != null)
            {
                for (int i = 0; i < _chapterStarRewardSlotList.Count; ++i)
                {
                    _chapterStarRewardSlotList[i].SetStarGaugeSlot(rewardInfoList[i]);
                }
            }
        }

        public void SetSelectedChapterData(int targetChapterID)
        {
            _targetChapterID = targetChapterID;

            _moveChapterButton.gameObject.SetActive(_selectedChapterData != null && _selectedChapterData.id != _targetChapterID);

            // 슬롯 레이어 갱신 처리
            _chapterSlotList.ForEach(slot => slot.SetSelectedLayer(_targetChapterID));

            // UI Popup 갱신
            RefreshSelectedLayer(false);
        }

        public void RefreshSelectedLayer(bool isFirstInit)
        {
            if (_chapterSlotList == null || _chapterSlotList.Count <= 0) return;

            _selectedChapterData = SpecDataManager.Instance.SpecChapter.Get(_targetChapterID);
            if (_selectedChapterData == null) return;

            // 유저 데이터 처리 (현재는 챕터 이동 시 무조건 첫번째 스테이지만 저장)
            if (isFirstInit == false)
            {
                var firstSpecStage = SpecDataManager.Instance.GetStageData(_selectedChapterData.chapter_id, 1, _selectedChapterData.difficulty_type);
                UserDataManager.Instance.SetLastPlayStageID(firstSpecStage.stage_id, true);

                // 연출 적용
                baseAnimator.SetTrigger("SetSelect");
            }

            // 팝업 관련 처리 (하단 정보, 슬라이더)
            _chapterNumberText.text = string.Format("챕터-{0}-{1}", _selectedChapterData.chapter_id, _selectedChapterData.difficulty_type);
            _chapterNameText.text = LanguageManager.Instance.GetLanguageText(_selectedChapterData.name_token);

            int currentChapterStarCount = UserDataManager.Instance.GetTotalChapterStarCount(_selectedChapterData.chapter_id, _selectedChapterData.difficulty_type);
            int totalChapterStarCount = SpecDataManager.Instance.GetTotalChapterStarCount(_selectedChapterData.chapter_id, _selectedChapterData.difficulty_type);

            _chapterStarCountText.text = string.Format("{0}/{1}", currentChapterStarCount, totalChapterStarCount);

            _chapterProgressSlider.maxValue = totalChapterStarCount;
            _chapterProgressSlider.value = currentChapterStarCount;

            // 보상 슬롯 관련 처리
            RefreshRewardLayer();
        }

        private void SetChapterListUI()
        {
            ClearList();

            var chapterList = SpecDataManager.Instance.GetChapterList(DifficultyType.NORMAL);

            foreach (var chapterData in chapterList)
            {
                GameObject newChapterObject = Instantiate(_chapterSlotObject, _chapterScrollRect.content);
                ChapterListItemSlot chapterSlot = newChapterObject.GetComponent<ChapterListItemSlot>();
                chapterSlot.SetChapterItemSlot(chapterData, this);

                _chapterSlotList.Add(chapterSlot);
            }
        }

        private void ClearList()
        {
            _chapterSlotList.Clear();

            BMUtil.RemoveChildObjects(_chapterScrollRect.content);
        }

        private void OnClickMoveChapterButton()
        {
            if (_selectedChapterData == null) return;

            OnClickCloseButton();

            // 로비 배경 전환
            InGameManager.Instance.EndInGame();
            var transition = SceneTransition_FadeInOut.Create();
            SceneLoading.GoToNextScene("Lobby",  (int)_selectedChapterData.chapter_id, transition).Forget();

            // 로비 메인 하단 스테이지 UI 갱신
            var lobbyMain = SceneUILayerManager.Instance.GetUILayer<LobbyMain>();
            if (lobbyMain != null)
            {
                lobbyMain.RefreshUI(LobbyMainRefreshType.STAGE);
            }

            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_btn_touch);
        }

        private void OnClickCloseButton()
        {
            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_btn_touch);

            SceneUILayerManager.Instance.PopUILayer(this);
        }
    }
}
