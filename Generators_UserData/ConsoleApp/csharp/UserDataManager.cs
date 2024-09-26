using System;
using System.Threading.Tasks;
using Cookapps.Stkauto.V1;

[GenerateUserDataInitializer]
public partial class UserDataManager
{
    [Initialize(DataCategory.UserData, 1)]
    public void Initialize(object data)
    {
        Console.WriteLine("UserDataManager Initialize");
    }
    
    [Initialize(DataCategory.UserAwakening, 2)]
    public void Initialize_2(object data)
    {
        Console.WriteLine("UserDataManager Initialize");
    }
    
    [Initialize(DataCategory.UserTraining, -1)]
    public void Initialize_3(object data)
    {
        Console.WriteLine("UserDataManager Initialize"); 
    }

    [Initialize(DataCategory.UserSeasonDailyQuest, 0)]
    public void Initialize_4(object data)
    {
        Console.WriteLine("UserDataManager Initialize"); 
    }

    
    [InitializeOwnContents]
    private void InitializeOwnContents_EventCommon()
    {
        Console.WriteLine("UserDataManager Initialize");
    }
    
    [InitializeEffectCode(1)]
    private void InitializeEffectCode_Collection()
    {
        Console.WriteLine("UserDataManager Initialize");
    }

    
}
