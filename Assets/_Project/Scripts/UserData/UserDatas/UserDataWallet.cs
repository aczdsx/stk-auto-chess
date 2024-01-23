using System;
using Com.Cookapps.Sampleteambattle;
using CookApps.gRPC.Universal;

[UserDataInitializeInfo(DataCategory.UserWallet)]
public class UserDataWallet : IUserData
{
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
