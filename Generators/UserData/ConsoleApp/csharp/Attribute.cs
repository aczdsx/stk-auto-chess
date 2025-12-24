
  using System;
  using Cookapps.Stkauto.V1;


  namespace Cookapps.Stkauto.V1{

  public enum DataCategory {
    None = 0,
    UserData = 1,
    UserStage = 2,
    UserTraining = 3,
    UserDeck = 4,
    UserHeroGroup = 5,
    UserEquipmentGroup = 6,
    UserEquipmentSlot = 7,
    UserAbilityGroup = 8,
    UserAwakening = 9,
    UserDungeonGroup = 10,
    UserDailyQuest = 11,
    UserGachaGroup = 12,
    UserLevelPass = 13,
    UserGuideQuest = 14,
    UserAdBuff = 15,
    UserTutorial = 16,
    UserCollectionGroup = 17,
    UserWallet = 18,
    UserRepeatQuest = 19,
    UserGachaCeilingGroup = 21,
    UserShopHistoryGroup = 22,
    UserBeginnerQuest = 23,
    UserSeasonQuest = 24,
    UserTimeBaseEventGroup = 25,
    UserDataSequence = 26,
    UserVillageGroup = 27,
    UserInventoryEquipmentGroup = 28,
    UserOfflineReward = 29,
    UserBadgeGroup = 30,
    UserWandererGroup = 31,
    UserStagePass = 32,
    UserPickUpPass = 33,
    UserStageCollectionGroup = 34,
    UserProfileCollection = 35,
    UserAchievementsQuestGroup = 36,
    PvpProfile = 37,
    UserPvpDeck = 38,
    UserSeasonPassQuestGroup = 39,
    UserSeasonDailyQuest = 40,
    UserSeasonSeasonQuest = 41,
    UserGuildShopGroup = 42,
    UserPvpShopGroup = 43,
    UserIapInfo = 44,
    UserVipInfo = 45,
    UserDeckPresetGroup = 46,
    UserChatBanGroup = 47,
    UserDailyAdReward = 48,
    UserTowerData = 49,
    UserTowerShopGroup = 50,
    UserTowerPassGroup = 51,
    UserRelicGroup = 52,
    UserCraftGroup = 98,
    UserCraftGacha = 99,
    UserPickUpPassGroup = 100,
    UserAppeventData = 1000,
    UserEventType1 = 2001,
  }
  }

/// <summary>
/// 해당 어트리뷰트 사용하는 함수는 반드시 private 로 선언할 것!
/// 가장 첫 초기화입니다.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class InitializeAttribute : Attribute
{
    public int Priority { get; }
    public DataCategory Category { get; }

    public InitializeAttribute(DataCategory category)
    {
        Category = category;
        Priority = 0;
    }
    public InitializeAttribute(DataCategory category, int priority)
    {
        Category = category;
        Priority = priority;
    }
}
    
/// <summary>
/// 해당 어트리뷰트 사용하는 함수는 반드시 private 로 선언할 것!
/// UniTask 비동기 반환 필요합니다.
/// InitializeFirst 다음에 호출됩니다.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class InitializeEffectCodeAttribute : Attribute
{
    public int Priority { get; }
    public InitializeEffectCodeAttribute(int priority = 0)
    {
        Priority = priority;
    }
}

/// <summary>
/// 해당 어트리뷰트 사용하는 함수는 반드시 private 로 선언할 것!
/// InitializeEffectCode 다음에 호출됩니다.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class InitializeOwnContentsAttribute : Attribute
{
    public int Priority { get; }

    public InitializeOwnContentsAttribute(int priority = 0)
    {
        Priority = priority;
    }
}

/// <summary>
/// 해당 어트리뷰트 사용하는 함수는 반드시 private 로 선언할 것!
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class ClearFuncAttribute : Attribute
{ }

/// <summary>
/// 해당 어트리뷰트 사용하는 함수는 반드시 private 로 선언할 것!
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class GenerateUserDataInitializerAttribute : Attribute
{ }
