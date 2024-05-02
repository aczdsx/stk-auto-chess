using System;
using Com.Cookapps.Sampleteambattle;
using CookApps.gRPC.Universal;

namespace CookApps.SampleTeamBattle
{
    public partial class UserDataManager
    {
        private UserWallet userWalletData;

        public static event Action<int> OnBreadChanged;

        [Initialize(DataCategory.UserWallet)]
        private void Initialize_Wallet(string data)
        {
            userWalletData = MessageUtility.FromBase64String<UserWallet>(data);
        }

        public void AddBread(int amount)
        {
            userWalletData.Bread += amount;
            OnBreadChanged?.Invoke(userWalletData.Bread);
        }
    }
}
