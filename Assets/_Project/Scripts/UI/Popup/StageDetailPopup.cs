using System.Collections.Generic;
using Cookapps.Stkauto.V1;
using CookApps.TeamBattle.UI;
using CookApps.TeamBattle.UIManagements;
using Cysharp.Threading.Tasks;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.Pool;

namespace CookApps.AutoBattler
{
    public class StageRewardData
    {
        public int targetStarCount { init; get; }
        public RewardItem rewardItem { init; get; }
    }

    public class StageDetailPopup : UILayer
    {
        [SerializeField] private TMP_Text stageNameText;
        [SerializeField] private GameObject[] starObjs;
        [SerializeField] private TableView tableView;
        [SerializeField] private StageRewardSlot rewardSlotOrigin;
        [SerializeField] private CAButton startButton;

        private int chapter;
        private int stageIndex;

        private List<StageRewardData> rewards = new ();

        private ObjectPool<StageRewardSlot> rewardSlotPool;

        protected override void Awake()
        {
            base.Awake();
            startButton.OnClickAsObservable().Subscribe(this, (_, self) => self.OnStartButtonClicked()).AddTo(this);
            tableView.OnGetTotalCellItemCount += OnGetTotalTableViewCellItemCount;
            tableView.OnGetCellItemSize += OnGetTableViewCellItemSize;
            tableView.OnReleaseCellItem += OnReleaseTableViewCellItem;
            tableView.OnGetCellItem += OnGetTableViewCellItem;

            rewardSlotPool = new ObjectPool<StageRewardSlot>(
                () =>
                {
                    GameObject go = Instantiate(rewardSlotOrigin.CachedGo, tableView.content);
                    var slot = go.GetComponent<StageRewardSlot>();
                    return slot;
                },
                slot => slot.CachedGo.SetActive(true),
                slot => slot.CachedGo.SetActive(false),
                slot => Destroy(slot.CachedGo),
                false
            );

            rewardSlotOrigin.CachedGo.SetActive(false);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            tableView.OnGetTotalCellItemCount -= OnGetTotalTableViewCellItemCount;
            tableView.OnGetCellItemSize -= OnGetTableViewCellItemSize;
            tableView.OnReleaseCellItem -= OnReleaseTableViewCellItem;
            tableView.OnGetCellItem -= OnGetTableViewCellItem;
            rewardSlotPool.Dispose();
        }

        private GameObject OnGetTableViewCellItem(int idx)
        {
            StageRewardSlot slot = rewardSlotPool.Get();
            slot.SetReward(rewards[idx]);
            return slot.CachedGo;
        }

        private void OnReleaseTableViewCellItem(int idx, GameObject obj)
        {
            rewardSlotPool.Release(obj.GetComponent<StageRewardSlot>());
        }

        private Vector2 OnGetTableViewCellItemSize(int idx)
        {
            return rewardSlotOrigin.CachedRectTr.sizeDelta;
        }

        private int OnGetTotalTableViewCellItemCount()
        {
            return rewards.Count;
        }

        protected override void OnPreEnter(object param)
        {
            base.OnPreEnter(param);
            TopCurrencyAndMenuBar.AddToUILayer(this, TopPanelType.CloseButton);
            (chapter, stageIndex) = ((int, int)) param;
            SetPopupInfo();
        }

        private void SetPopupInfo()
        {
            // stageNameText.SetText("{0}-{1}", chapter, stageIndex + 1);
            // SpecStage specStage = SpecDataManager.Instance.GetSpecStage(chapter, stageIndex);
            // UserStage userStage = UserDataManager.Instance.GetUserStage(specStage.stage_id);
            // for (var i = 0; i < starObjs.Length; i++)
            // {
            //     starObjs[i].SetActive(i < userStage.StarCount);
            // }
            //
            // RewardItem[] starReward = specStage.GetStarRewards();
            // for (var i = 0; i < starReward.Length; i++)
            // {
            //     if (i < userStage.StarCount)
            //     {
            //         continue;
            //     }
            //
            //     var reward = new StageRewardData
            //     {
            //         targetStarCount = i + 1,
            //         rewardItem = starReward[i],
            //     };
            //     rewards.Add(reward);
            // }
            //
            // List<RewardItem> specChests = SpecDataManager.Instance.GetChestList(specStage.chest_id);
            //
            // foreach (RewardItem rewardItem in specChests)
            // {
            //     var reward = new StageRewardData
            //     {
            //         targetStarCount = 0,
            //         rewardItem = rewardItem,
            //     };
            //     rewards.Add(reward);
            // }
            //
            // tableView.RefreshAll();
        }

        private void OnStartButtonClicked()
        {
            //SceneUILayerManager.Instance.PushUILayerAsync<ReadyMain>((chapter, stageIndex)).Forget();
        }
    }
}
