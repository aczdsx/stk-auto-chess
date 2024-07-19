
namespace CookApps.gRPC.Editor
{
    using UnityEditor;
    using Cookapps.Stkauto.V1;
    [CustomPropertyDrawer(typeof(Reward))]
    internal class RewardDrawer : GrpcMessagePropertyDrawer<Reward> { }
    [CustomPropertyDrawer(typeof(UserDataSequence))]
    internal class UserDataSequenceDrawer : GrpcMessagePropertyDrawer<UserDataSequence> { }
    [CustomPropertyDrawer(typeof(UserBasicData))]
    internal class UserBasicDataDrawer : GrpcMessagePropertyDrawer<UserBasicData> { }
    [CustomPropertyDrawer(typeof(UserMissionData))]
    internal class UserMissionDataDrawer : GrpcMessagePropertyDrawer<UserMissionData> { }
    [CustomPropertyDrawer(typeof(UserGuideMission))]
    internal class UserGuideMissionDrawer : GrpcMessagePropertyDrawer<UserGuideMission> { }
    [CustomPropertyDrawer(typeof(UserQuest))]
    internal class UserQuestDrawer : GrpcMessagePropertyDrawer<UserQuest> { }
    [CustomPropertyDrawer(typeof(UserQuestData))]
    internal class UserQuestDataDrawer : GrpcMessagePropertyDrawer<UserQuestData> { }
    [CustomPropertyDrawer(typeof(UserEvent))]
    internal class UserEventDrawer : GrpcMessagePropertyDrawer<UserEvent> { }
    [CustomPropertyDrawer(typeof(UserEventData))]
    internal class UserEventDataDrawer : GrpcMessagePropertyDrawer<UserEventData> { }
    [CustomPropertyDrawer(typeof(UserEventConditionData))]
    internal class UserEventConditionDataDrawer : GrpcMessagePropertyDrawer<UserEventConditionData> { }
    [CustomPropertyDrawer(typeof(UserDungeon))]
    internal class UserDungeonDrawer : GrpcMessagePropertyDrawer<UserDungeon> { }
    [CustomPropertyDrawer(typeof(UserTrialDungeonData))]
    internal class UserTrialDungeonDataDrawer : GrpcMessagePropertyDrawer<UserTrialDungeonData> { }
    [CustomPropertyDrawer(typeof(UserWallet))]
    internal class UserWalletDrawer : GrpcMessagePropertyDrawer<UserWallet> { }
    [CustomPropertyDrawer(typeof(UserStageGroup))]
    internal class UserStageGroupDrawer : GrpcMessagePropertyDrawer<UserStageGroup> { }
    [CustomPropertyDrawer(typeof(UserStage))]
    internal class UserStageDrawer : GrpcMessagePropertyDrawer<UserStage> { }
    [CustomPropertyDrawer(typeof(UserStageAccRewardDic))]
    internal class UserStageAccRewardDicDrawer : GrpcMessagePropertyDrawer<UserStageAccRewardDic> { }
    [CustomPropertyDrawer(typeof(UserStageAccRewardList))]
    internal class UserStageAccRewardListDrawer : GrpcMessagePropertyDrawer<UserStageAccRewardList> { }
    [CustomPropertyDrawer(typeof(UserCharacterGroup))]
    internal class UserCharacterGroupDrawer : GrpcMessagePropertyDrawer<UserCharacterGroup> { }
    [CustomPropertyDrawer(typeof(UserCharacter))]
    internal class UserCharacterDrawer : GrpcMessagePropertyDrawer<UserCharacter> { }
    [CustomPropertyDrawer(typeof(UserCharacterBattle))]
    internal class UserCharacterBattleDrawer : GrpcMessagePropertyDrawer<UserCharacterBattle> { }
    [CustomPropertyDrawer(typeof(UserCharacterBattleDeck))]
    internal class UserCharacterBattleDeckDrawer : GrpcMessagePropertyDrawer<UserCharacterBattleDeck> { }
    [CustomPropertyDrawer(typeof(UserIdleData))]
    internal class UserIdleDataDrawer : GrpcMessagePropertyDrawer<UserIdleData> { }
    [CustomPropertyDrawer(typeof(UserCommanderSkillData))]
    internal class UserCommanderSkillDataDrawer : GrpcMessagePropertyDrawer<UserCommanderSkillData> { }
    [CustomPropertyDrawer(typeof(UserCommanderSkill))]
    internal class UserCommanderSkillDrawer : GrpcMessagePropertyDrawer<UserCommanderSkill> { }
    [CustomPropertyDrawer(typeof(UserDeck))]
    internal class UserDeckDrawer : GrpcMessagePropertyDrawer<UserDeck> { }
    [CustomPropertyDrawer(typeof(UserDeckLine))]
    internal class UserDeckLineDrawer : GrpcMessagePropertyDrawer<UserDeckLine> { }
}