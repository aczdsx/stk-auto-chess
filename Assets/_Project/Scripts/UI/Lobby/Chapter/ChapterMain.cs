using CookApps.TeamBattle.UI;
using CookApps.TeamBattle.UIManagements;
using UnityEngine;
using UnityEngine.Pool;

namespace CookApps.AutoBattler
{
    [RegisterUILayer(UILayerType.Cover, "Prefabs/UI/Popup/ChapterMain.prefab")]
    public class ChapterMain : UILayer
    {
        [SerializeField] private TableView tableView;
        [SerializeField] private StageSlot stageSlotOrigin;

        private int currentChapter;
        private ObjectPool<StageSlot> stageSlotPool;

        protected override void Awake()
        {
            base.Awake();
            tableView.OnGetTotalCellItemCount += OnGetTotalTableViewCellItemCount;
            tableView.OnGetCellItemSize += OnGetTableViewCellItemSize;
            tableView.OnReleaseCellItem += OnReleaseTableViewCellItem;
            tableView.OnGetCellItem += OnGetTableViewCellItem;

            stageSlotPool = new ObjectPool<StageSlot>(
                () =>
                {
                    GameObject go = Instantiate(stageSlotOrigin.CachedGo, tableView.content);
                    var slot = go.GetComponent<StageSlot>();
                    return slot;
                },
                slot => slot.CachedGo.SetActive(true),
                slot => slot.CachedGo.SetActive(false),
                slot => Destroy(slot.CachedGo),
                false
            );

            stageSlotOrigin.CachedGo.SetActive(false);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            tableView.OnGetTotalCellItemCount -= OnGetTotalTableViewCellItemCount;
            tableView.OnGetCellItemSize -= OnGetTableViewCellItemSize;
            tableView.OnReleaseCellItem -= OnReleaseTableViewCellItem;
            tableView.OnGetCellItem -= OnGetTableViewCellItem;
            stageSlotPool.Dispose();
        }

        private GameObject OnGetTableViewCellItem(int idx)
        {
            StageSlot slot = stageSlotPool.Get();
            slot.SetStageData(currentChapter, idx);
            return slot.CachedGo;
        }

        private void OnReleaseTableViewCellItem(int idx, GameObject obj)
        {
            stageSlotPool.Release(obj.GetComponent<StageSlot>());
        }

        private Vector2 OnGetTableViewCellItemSize(int idx)
        {
            return stageSlotOrigin.CachedRectTr.sizeDelta;
        }

        private int OnGetTotalTableViewCellItemCount()
        {
            // return specQuests.Count;
            return SpecDataManager.Instance.GetStageCount(currentChapter);
        }

        protected override void OnPreEnter(object param)
        {
            base.OnPreEnter(param);
            TopCurrencyAndMenuBar.AddToUILayer(this, TopPanelType.CloseButton);
            var currentStageId = (int) param;
            SpecStage specStage = SpecDataManager.Instance.SpecStage.Get(currentStageId);
            currentChapter = specStage.chapter_id;
            int focusIndex = SpecDataManager.Instance.GetStageIndex(currentChapter, currentStageId);
            tableView.RefreshAll(true, focusIndex);
        }
    }
}
