using Com.Cookapps.Sampleteambattle;
using CookApps.gRPC.Universal;

[UserDataInitializeInfo(DataCategory.UserStage)]
public class UserDataStage : IUserData
{
    private UserStage userStageData;

    public void Initialize(string data)
    {
        userStageData = MessageUtility.FromBase64String<UserStage>(data);
    }
}
