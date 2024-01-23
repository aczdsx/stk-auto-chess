using Com.Cookapps.Sampleteambattle;
using CookApps.gRPC.Universal;

[UserDataInitializeInfo(DataCategory.UserData)]
public class UserBasicData : IUserData
{
    private UserData userBasicData;

    public void Initialize(string data)
    {
        userBasicData = MessageUtility.FromBase64String<UserData>(data);
    }
}
