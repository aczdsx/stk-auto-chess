using Com.Cookapps.Playgrounds.Heroidle;
using CookApps.gRPC.Universal;

public class UserDataStage : IUserData
{
    private UserStage userStageData;
    public static DataCategory Category => DataCategory.UserStage;

    public void Initialize(string data)
    {
        userStageData = MessageUtility.FromBase64String<UserStage>(data);
    }
}
