using System.Collections.Generic;
using Cookapps.Stkauto.V1;

namespace CookApps.AutoBattler
{
    public partial class UserDataManager
    {
        private UserWallet userWallet;

        public UserWallet UserWallet => userWallet;

        public void IncreaseRewardItemList(List<RewardItem> rewardList, bool isSave)
        {
            if (rewardList == null || rewardList.Count == 0) return;

            // 리워드 적용
            // foreach (var reward in rewardList) IncreaseItem(reward.Id, reward.Count, false, false);

            // 메인 로비 레드닷 갱신
            // var lobbyMain = SceneUILayerManager.Instance.GetUILayer<LobbyMain>();
            // if (lobbyMain != null) lobbyMain.RefreshUI(LobbyMainRefreshType.REDDOT);
        }

    }
}