using System.Collections.Generic;
using CookApps.BattleSystem;
using CookApps.TeamBattle.UIManagements;
using Cysharp.Threading.Tasks;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CookApps.AutoBattler
{
    public class ChapterListPopup : UILayerPopupBase
    {
        [Header("Stage Milestone")]
        [SerializeField] private StageMilestonePanel stageMilestonePanel;
        public StageMilestonePanel StageMilestonePanel => stageMilestonePanel;
        
        [SerializeField] private CAButton _closeButton;
        [SerializeField] private CAButton _dimCloseButton;
        [SerializeField] private CAButton _moveChapterButton;

        [Header("Chapter List Layer")]
        [SerializeField] private ScrollRect _chapterScrollRect;
        [SerializeField] private GameObject _chapterSlotObject;

        [Header("Chapter Progress Layer")]
        [SerializeField] private TextMeshProUGUI _chapterNumberText;
        [SerializeField] private TextMeshProUGUI _chapterNameText;

        private ChapterInfo _currentChapterData;  // 현재 팝업에서 선택한 챕터 데이터 (팝업만)
        private ChapterInfo _selectedChapterData; // 현재 선택된 챕터 데이터 (스테이지)
        public ChapterInfo SelectedChapterData => _selectedChapterData;
        
        private List<ChapterListItemSlot> _chapterSlotList = new();


        protected override void Awake()
        {
            base.Awake();

            _closeButton.OnClickAsObservable()
                .Subscribe(this, (_, self) => self.OnClickCloseButton())
                .AddTo(this);
            
            _dimCloseButton.OnClickAsObservable()
                .Subscribe(this, (_, self) => self.OnClickCloseButton())
                .AddTo(this);
            
            _moveChapterButton.OnClickAsObservable()
                .SubscribeAwait(this, (_, self, _) => self.OnClickMoveChapterButtonAsync(), AwaitOperation.Drop)
                .AddTo(this);
        }

        protected override void OnPreEnter(object param)
        {
            base.OnPreEnter(param);
            //TopCurrencyAndMenuBar.AddToUILayer(this, TopPanelType.CloseButton);
            
            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_btn_popup);

            var currentStageId = (int)LocalDataManager.Instance.GetLastPlayStageId();
            _selectedChapterData = SpecDataManager.Instance.GetChapterDataByStageID(currentStageId);

            // Stage Progress 로드
            LoadStageProgressAsync((uint)_selectedChapterData.chapter_id).Forget();
            SetStageMilestoneUI(currentStageId);
            SetSelectedChapterData(_selectedChapterData.chapter_id, true);
            SetChapterListUI();

            _chapterScrollRect.verticalNormalizedPosition = 1;

            // 연출 적용
            baseAnimator.SetTrigger("SetEntry");
        }

        private async UniTaskVoid LoadStageProgressAsync(uint chapterId)
        {
            await NetManager.Instance.Battle.ListStageAsync(chapterId);
        }

        // public void RefreshChapterListReddot()
        // {
        //     _chapterSlotList.ForEach(slot => slot.UpdateReddotState());
        // }

        private void SetStageMilestoneUI(int stageId)
        {
            var specChapterData = SpecDataManager.Instance.GetChapterDataByStageID(stageId);
            stageMilestonePanel.SetChapterData(specChapterData);
        }

        public void SetSelectedChapterData(int targetChapterID, bool isFirstInit)
        {
            _currentChapterData = SpecDataManager.Instance.GetChapterData(targetChapterID);
            _chapterSlotList.ForEach(slot => slot.SetSelectedLayer(_currentChapterData.chapter_id));
            
            // UI Popup 갱신
            RefreshSelectedLayer(isFirstInit);

            // 버튼 상태 갱신
            _moveChapterButton.gameObject.SetActive(_selectedChapterData.chapter_id != _currentChapterData.chapter_id);
        }

        public void RefreshSelectedLayer(bool isFirstInit)
        {
            if (_currentChapterData == null) return;
            if (_chapterSlotList == null || _chapterSlotList.Count <= 0) return;

            // 유저 데이터 처리 (현재는 챕터 이동 시 무조건 첫번째 스테이지만 저장)
            if (isFirstInit == false)
            {
                // 연출 적용
                baseAnimator.SetTrigger("SetSelect");
            }

            // 팝업 관련 처리 (하단 정보, 슬라이더)
            string chapterString = LanguageManager.Instance.GetDefaultText("UI_CHAPTER");
            _chapterNumberText.text = $"{chapterString}-{_currentChapterData.chapter_id}-{_currentChapterData.difficulty_type}";
            _chapterNameText.text = LanguageManager.Instance.GetDefaultText(_currentChapterData.name_token);

            // 보상 슬롯 관련 처리
            SetStageMilestoneUI((int)LocalDataManager.Instance.GetLastPlayStageId());
        }

        private async UniTask OnClickMoveChapterButtonAsync()
        {
            if (_currentChapterData == null) return;

            // 선택된 챕터 데이터 갱신
            _selectedChapterData = SpecDataManager.Instance.GetChapterData(_currentChapterData.chapter_id);

            // 유저 챕터 선택 데이터 저장
            var lastestStageID = (int)ServerDataManager.Instance.Battle.GetLatestClearedStageId();
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
            LocalDataManager.Instance.SetLastPlayStageId((uint)targetSpecStage.stage_id);

            OnClickCloseButton();
            
            SceneTransition.Create<SceneTransition_SubTransition>(SubTransition_Animator.Address);
            await SceneTransition.FadeInAsync();
            InGameManager.Instance.EndInGame();
            
            SceneLoading.GoToNextScene("BattleReady", _selectedChapterData.chapter_id);

            // 로비 메인 하단 스테이지 UI 갱신
            // var battleReadyMain = SceneUILayerManager.Instance.GetUILayer<BattleReadyMain>();
            // if (battleReadyMain != null)
            // {
            //     battleReadyMain.RefreshUI(LobbyMainRefreshType.STAGE);
            // }
        }

        private void OnClickCloseButton()
        {
            var battleReadyMainStageMilestone = SceneUILayerManager.Instance.GetUILayer<BattleReadyMain>().StageMilestonePanel;
            battleReadyMainStageMilestone.RefreshRewardLayer();
            
            SceneUILayerManager.Instance.PopUILayer(this);
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
            SetStageMilestoneUI((int)LocalDataManager.Instance.GetLastPlayStageId());
        }
    }
}
