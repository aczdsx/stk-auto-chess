using CookApps.TeamBattle.UI;
using CookApps.TeamBattle.UIManagements;
using UnityEngine;
using UnityEngine.Pool;

public class StageSelectMain : UILayer
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
        tableView.OnGetTotalCellItemCount -= OnGetTotalTableViewCellItemCount;
        tableView.OnGetCellItemSize -= OnGetTableViewCellItemSize;
        tableView.OnReleaseCellItem -= OnReleaseTableViewCellItem;
        tableView.OnGetCellItem -= OnGetTableViewCellItem;
        stageSlotPool.Dispose();
    }

    private RectTransform OnGetTableViewCellItem(int idx)
    {
        StageSlot slot = stageSlotPool.Get();
        // slot.SetBeginnerQuestData(specQuests[idx]);
        return slot.CachedRectTr;
    }

    private void OnReleaseTableViewCellItem(int idx, Transform obj)
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

    public override void OnPreEnter(object param)
    {
        base.OnPreEnter(param);
        currentChapter = (int) param;
        tableView.RefreshAll();
    }
}
