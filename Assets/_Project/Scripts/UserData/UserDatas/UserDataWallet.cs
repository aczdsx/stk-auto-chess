using System;
using Com.Cookapps.Sampleteambattle;
using CookApps.gRPC.Universal;

namespace CookApps.SampleTeamBattle
{
    // userWallet에 쉽게 접근하기위해
    public partial class UserDataManager
    {
        public static UserDataWallet UserWallet => Get<UserDataWallet>(DataCategory.UserWallet);
    }

    public class UserDataWallet : IUserData
    {
        DataCategory IUserData.DataCategory => DataCategory.UserWallet;
        int IUserData.Priority => 0;

        private UserWallet userWalletData;

        public static event Action<int> OnBreadChanged;

        public void SetDataFromServer(string data)
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
