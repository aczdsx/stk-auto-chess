using System;
using Com.Cookapps.Sampleteambattle;
using CookApps.gRPC.Universal;

namespace CookApps.SampleTeamBattle
{
    // event 연결
    public partial class UserDataManager
    {
        public static event Action<int> OnBreadChanged
        {
            add => Get<UserDataWallet>(DataCategory.UserWallet).OnBreadChanged += value;
            remove => Get<UserDataWallet>(DataCategory.UserWallet).OnBreadChanged -= value;
        }
    }

    public class UserDataWallet : IUserData
    {
        DataCategory IUserData.DataCategory => DataCategory.UserWallet;
        int IUserData.Priority => 0;

        private UserWallet userWalletData;

        public event Action<int> OnBreadChanged;

        public void Initialize(string data)
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
