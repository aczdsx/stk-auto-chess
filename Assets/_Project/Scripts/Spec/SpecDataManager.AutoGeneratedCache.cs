//=====================================================
//  자동 생성 코드입니다. 수정하지 마세요
//  made by 김윤하, mail : yhkim2@cookapps.com 
//=====================================================
using System.Collections.Generic;
using System.Linq;
namespace CookApps.AutoBattler
{
	public partial class SpecDataManager
	{
		public List<AccountLevelExp> SpecAccountLevelExpList {get; private set;}
		public List<ChapterInfo> SpecChapterList {get; private set;}
		public List<CharacterInfo> SpecCharacterList {get; private set;}
		public List<CharacterLevelExp> SpecCharacterLevelExpList {get; private set;}
		public List<CharacterTranscendence> SpecCharacterTranscendenceList {get; private set;}
		public List<CharacterEnhance> SpecCharacterEnhanceList {get; private set;}
		public List<CharacterQuotes> SpecCharacterQuotesList {get; private set;}
		public List<ChapterRule> SpecChapterRuleList {get; private set;}
		public List<SkillCommander> SpecCommanderSkillList {get; private set;}
		public List<MonsterInfo> SpecMonsterList {get; private set;}
		public List<DialogueLanguage> SpecDialogueList { get; private set; }
		public List<TutorialDialogue> SpecTutorialDialogueList {get; private set;}
		public List<DungeonBabelInfo> SpecDungeonTrialList {get; private set;}
		public List<DungeonBabelMonster> SpecDungeonMonsterList {get; private set;}
		public List<DungeonBabelReward> SpecDungeonRewardList {get; private set;}
		public List<EventInfo> SpecEventList {get; private set;}
		public List<EventCondition> SpecEventConditionList {get; private set;}
		public List<Language> SpecLanguageList {get; private set;}
		public List<ConfigGame> SpecGameConfigList {get; private set;}
		public List<GachaInfo> SpecGachaList {get; private set;}
		public List<GachaCharacter> SpecGachaContentList {get; private set;}
		public List<GachaScenario> SpecGachaScenarioList {get; private set;}
		public List<GuideMissionInfo> SpecGuideMissionList {get; private set;}
		public List<QuestInfo> SpecQuestList {get; private set;}
		public List<StageInfo> SpecStageList {get; private set;}
		public List<StageMonster> SpecStageMonsterList {get; private set;}
		public List<TileEffectCode> SpecTileEffectCodeList {get; private set;}
		public List<SkillActive> SkillActiveList {get; private set;}
		public List<ShopInfo> SpecShopList {get; private set;}
		public List<ShopBanner> SpecShopBannerList {get; private set;}
		public List<Item> SpecItemList {get; private set;}
		public List<IdleReward> SpecIdleRewardList {get; private set;}
		public List<RewardInfo> SpecRewardInfoList {get; private set;}
		public List<OpenCondition> SpecOpenConditionList {get; private set;}
		public List<ImageInfo> SpecImageInfoList {get; private set;}

		private void GenerateCacheSpecData()
		{
			SpecAccountLevelExpList = AccountLevelExp.All.ToList();
			SpecChapterList = ChapterInfo.All.ToList();
			SpecCharacterList = CharacterInfo.All.ToList();
			SpecCharacterLevelExpList = CharacterLevelExp.All.ToList();
			SpecCharacterTranscendenceList = CharacterTranscendence.All.ToList();
			SpecCharacterEnhanceList = CharacterEnhance.All.ToList();
			SpecCharacterQuotesList = CharacterQuotes.All.ToList();
			SpecChapterRuleList = ChapterRule.All.ToList();
			SpecCommanderSkillList = SkillCommander.All.ToList();
			SpecDialogueList = DialogueLanguage.All.ToList();
			SpecTutorialDialogueList = TutorialDialogue.All.ToList();
			SpecDungeonTrialList = DungeonBabelInfo.All.ToList();
			SpecDungeonMonsterList = DungeonBabelMonster.All.ToList();
			SpecDungeonRewardList = DungeonBabelReward.All.ToList();
			SpecEventList = EventInfo.All.ToList();
			SpecEventConditionList = EventCondition.All.ToList();
			SpecLanguageList = Language.All.ToList();
			SpecGameConfigList = ConfigGame.All.ToList();
			SpecGachaList = GachaInfo.All.ToList();
			SpecGachaContentList = GachaCharacter.All.ToList();
			SpecGachaScenarioList = GachaScenario.All.ToList();
			SpecGuideMissionList = GuideMissionInfo.All.ToList();
			SpecQuestList = QuestInfo.All.ToList();
			SpecStageList = StageInfo.All.ToList();
			SpecStageMonsterList = StageMonster.All.ToList();
			SpecTileEffectCodeList = TileEffectCode.All.ToList();
			SkillActiveList = SkillActive.All.ToList();
			SpecShopList = ShopInfo.All.ToList();
			SpecShopBannerList = ShopBanner.All.ToList();
			SpecItemList = Item.All.ToList();
			SpecIdleRewardList = IdleReward.All.ToList();
			SpecRewardInfoList = RewardInfo.All.ToList();
			SpecOpenConditionList = OpenCondition.All.ToList();
			SpecImageInfoList = ImageInfo.All.ToList();
			SpecMonsterList = MonsterInfo.All.ToList();
		}
	}
}
