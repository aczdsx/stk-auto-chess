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

        public override void OnPreEnter(object param)
        {
            base.OnPreEnter(param);
            TopCurrencyAndMenuBar.AddToUILayer(this, TopPanelType.CloseButton);

            var currentStageId = (int) param;

            SetChapterListUI();
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
