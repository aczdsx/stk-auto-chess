using System.Collections;
using System.Collections.Generic;
using CookApps.TeamBattle.UIManagements;
using UnityEngine;
using UnityEngine.UI;

namespace CookApps.AutoBattler
{
    [RegisterUILayer(UILayerType.Popup, "Prefabs/UI/01_Pops/Pop_ChapterList.prefab")]
    public class ChapterListPopup : UILayer
    {
        [SerializeField] private ScrollRect _chapterScrollRect;
        [SerializeField] private GameObject _chapterSlotObject;

        private List<ChapterListItemSlot> _chapterSlotList = new();

        int _selectedChapterID = 0;

        public override void OnPreEnter(object param)
        {
            base.OnPreEnter(param);
            TopCurrencyAndMenuBar.AddToUILayer(this, TopPanelType.CloseButton);

            var currentStageId = (int) param;
            _selectedChapterID = UserDataManager.Instance.UserStageGroup.CurrentSelectedChapterId;

            SetChapterListUI();

            RefreshSelectedLayer(_selectedChapterID);
        }

        public void RefreshUI()
        {
            SetChapterListUI();
        }

        public void RefreshSelectedLayer(int targetChapterID)
        {
            if (_chapterSlotList == null || _chapterSlotList.Count <= 0) return;

            // 유저 데이터 처리
            UserDataManager.Instance.SelectUserChapter(targetChapterID);

            // 팝업 관련 처리
            _selectedChapterID = targetChapterID;

            _chapterSlotList.ForEach(slot => slot.SetSelectedLayer(_selectedChapterID));

            // 로비 메인 하단 스테이지 UI 갱신
            var lobbyMain = SceneUILayerManager.Instance.GetUILayer("LobbyMain");
            if (lobbyMain != null)
            {
                lobbyMain.GetComponent<LobbyMain>()?.RefreshUI();
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
