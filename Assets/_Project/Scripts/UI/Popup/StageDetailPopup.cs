using System.Collections.Generic;
using Com.Cookapps.Sampleteambattle;
using CookApps.TeamBattle.UI;
using CookApps.TeamBattle.UIManagements;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.Serialization;

namespace CookApps.SampleTeamBattle
{
    public class StageReward
    {
        public int targetStarCount;
        public Reward reward;
    }

    public class StageDetailPopup : UILayer
    {
        [SerializeField] private TableView tableView;
        [SerializeField] private StageRewardSlot rewardSlotOrigin;
        [SerializeField] private CAButton startButton;

        private int chapter;
        private int stageIndex;

        private List<StageReward> rewards = new ();

        private ObjectPool<StageRewardSlot> rewardSlotPool;

        protected override void Awake()
        {
            base.Awake();
            startButton.onClick.AddListener(OnStartButtonClicked);
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
            startButton.onClick.RemoveListener(OnStartButtonClicked);
        }

        private RectTransform OnGetTableViewCellItem(int idx)
        {
            StageRewardSlot slot = rewardSlotPool.Get();
            slot.SetReward(rewards[idx]);
            return slot.CachedRectTr;
        }

        private void OnReleaseTableViewCellItem(int idx, Transform obj)
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

        public override void OnPreEnter(object param)
        {
            base.OnPreEnter(param);
            TopCurrencyAndMenuBar.AddToUILayer(this, TopPanelType.CloseButton, TopPanelType.Bread);

            (chapter, stageIndex) = ((int, int)) param;
        }

        private void SetPopupInfo()
        {
            SpecStage specStage = SpecDataManager.Instance.GetSpecStage(chapter, stageIndex);
            UserStage userStage = UserDataManager.UserStage.GetUserStage(specStage.stage_id);
            Reward[] starReward = specStage.GetStarRewards();
            for (var i = 0; i < starReward.Length; i++)
            {
                if (i < userStage.StarCount)
                {
                    continue;
                }

                var reward = new StageReward();
                reward.targetStarCount = i + 1;
                reward.reward = starReward[i];
                rewards.Add(reward);
            }

            tableView.RefreshAll();
        }

        private void OnStartButtonClicked()
        {
            SceneUIManager.Instance.PushUILayer("ReadyMain", (chapter, stageIndex));
        }
    }
}
