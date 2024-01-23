using System;
using Com.Cookapps.Playgrounds.Heroidle;
using CookApps.gRPC.Universal;

public class UserDataWallet : IUserData
{
    private UserWallet userWalletData;
    public static DataCategory Category => DataCategory.UserWallet;

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
