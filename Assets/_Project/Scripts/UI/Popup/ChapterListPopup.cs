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

        private SpecChapter _currentChapterData;   // 현재 팝업에서 선택한 챕터 데이터 (팝업만)
        private SpecChapter _selectedChapterData;   // 현재 선택된 챕터 데이터 (스테이지)
        public SpecChapter SelectedChapterData => _selectedChapterData;

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

            SetSelectedChapterData(_selectedChapterData.chapter_id, true);

            _chapterScrollRect.verticalNormalizedPosition = 1;

            // 연출 적용
            baseAnimator.SetTrigger("SetEntry");
        }

        public void RefreshUI()
        {

        }

        public void RefreshRewardLayer()
        {
            var rewardInfoList = SpecDataManager.Instance.GetSpecRewardInfoList(ContentType.STAGE_STAR, _currentChapterData.chapter_id, _currentChapterData.difficulty_type);
            if (rewardInfoList != null)
            {
                for (int i = 0; i < _chapterStarRewardSlotList.Count; ++i)
                {
                    _chapterStarRewardSlotList[i].SetStarGaugeSlot(rewardInfoList[i]);
                }
            }
        }
        
        public void RefreshChapterListReddot()
        {
            _chapterSlotList.ForEach(slot => slot.UpdateReddotState());
        }

        public void SetSelectedChapterData(int targetChapterID, bool isFirstInit)
        {
            _currentChapterData = SpecDataManager.Instance.GetChapterData(targetChapterID);

            // UI Popup 갱신
            RefreshSelectedLayer(isFirstInit);

            // 슬롯 레이어 갱신 처리
            _chapterSlotList.ForEach(slot => slot.SetSelectedLayer(_currentChapterData.chapter_id));

            // 버튼 상태 갱신
            _moveChapterButton.gameObject.SetActive(_selectedChapterData.chapter_id != _currentChapterData.chapter_id);
        }

        public void RefreshSelectedLayer(bool isFirstInit)
        {
            if (_chapterSlotList == null || _chapterSlotList.Count <= 0) return;
            if (_currentChapterData == null) return;

            // 유저 데이터 처리 (현재는 챕터 이동 시 무조건 첫번째 스테이지만 저장)
            if (isFirstInit == false)
            {
                // 연출 적용
                baseAnimator.SetTrigger("SetSelect");
            }

            // 팝업 관련 처리 (하단 정보, 슬라이더)
            string chapterString = LanguageManager.Instance.GetLanguageText("UI_CHAPTER");
            _chapterNumberText.text = $"{chapterString}-{_currentChapterData.chapter_id}-{_currentChapterData.difficulty_type}";
            _chapterNameText.text = LanguageManager.Instance.GetLanguageText(_currentChapterData.name_token);

            int currentChapterStarCount = UserDataManager.Instance.GetTotalChapterStarCount(_currentChapterData.chapter_id, _currentChapterData.difficulty_type);
            int totalChapterStarCount = SpecDataManager.Instance.GetTotalChapterStarCount(_currentChapterData.chapter_id, _currentChapterData.difficulty_type);

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
            if (_currentChapterData == null) return;

            // 선택된 챕터 데이터 갱신
            _selectedChapterData = SpecDataManager.Instance.GetChapterData(_currentChapterData.chapter_id);

            // 유저 챕터 선택 데이터 저장
            var lastestStageID = UserDataManager.Instance.GetLatestClearUserStageID();
            var lastestSpecStageData = SpecDataManager.Instance.GetStageData(lastestStageID);
            var nextStageData = SpecDataManager.Instance.GetNextStageData(lastestStageID);

            // 가장 최신 챕터를 확인하고 플레이 가능한 최대 스테이지 넘버로 이동
            int targetStageNumber = 1;
            if (lastestSpecStageData != null && lastestSpecStageData.chapter_id == _currentChapterData.chapter_id)
            {
                if (nextStageData != null)
                {
                    targetStageNumber = nextStageData.stage_number;
                }
            }
            
            // 스테이지 데이터 세팅
            var targetSpecStage = SpecDataManager.Instance.GetStageData(_currentChapterData.chapter_id, targetStageNumber, _currentChapterData.difficulty_type);
            UserDataManager.Instance.SetLastPlayStageID(targetSpecStage.stage_id, true);

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
