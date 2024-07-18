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
		public List<SpecChest> SpecChestList {get; private set;}
		public List<SpecAccountLevelExp> SpecAccountLevelExpList {get; private set;}
		public List<SpecChapter> SpecChapterList {get; private set;}
		public List<SpecCharacter> SpecCharacterList {get; private set;}
		public List<SpecCharacterLevelExp> SpecCharacterLevelExpList {get; private set;}
		public List<SpecCharacterTranscendence> SpecCharacterTranscendenceList {get; private set;}
		public List<SpecCharacterEnhance> SpecCharacterEnhanceList {get; private set;}
		public List<SpecCharacterQuotes> SpecCharacterQuotesList {get; private set;}
		public List<SpecCommanderSkill> SpecCommanderSkillList {get; private set;}
		public List<SpecDialogue> SpecDialogueList {get; private set;}
		public List<SpecEvent> SpecEventList {get; private set;}
		public List<SpecEventCondition> SpecEventConditionList {get; private set;}
		public List<SpecLanguage> SpecLanguageList {get; private set;}
		public List<SpecGameConfig> SpecGameConfigList {get; private set;}
		public List<SpecGachaScenario> SpecGachaScenarioList {get; private set;}
		public List<SpecGuideMission> SpecGuideMissionList {get; private set;}
		public List<SpecQuest> SpecQuestList {get; private set;}
		public List<SpecSkill> SpecSkillList {get; private set;}
		public List<SpecStage> SpecStageList {get; private set;}
		public List<SpecStageMonster> SpecStageMonsterList {get; private set;}
		public List<SpecStageReward> SpecStageRewardList {get; private set;}
		public List<SpecDungeonTrial> SpecDungeonTrialList {get; private set;}
		public List<SpecDungeonMonster> SpecDungeonMonsterList {get; private set;}
		public List<SpecDungeonReward> SpecDungeonRewardList {get; private set;}
		public List<SpecSynergy> SpecSynergyList {get; private set;}
		public List<SpecTutorial> SpecTutorialList {get; private set;}
		public List<SpecItem> SpecItemList {get; private set;}
		public List<SpecIdleReward> SpecIdleRewardList {get; private set;}
		public List<SpecInGameVfx> SpecInGameVfxList {get; private set;}
		public List<SpecRewardInfo> SpecRewardInfoList {get; private set;}
		public List<SpecPVPConfig> SpecPVPConfigList {get; private set;}
		public List<SpecPVPTier> SpecPVPTierList {get; private set;}
		public List<SpecPVPRanking> SpecPVPRankingList {get; private set;}
		public List<SpecPVPDummy> SpecPVPDummyList {get; private set;}
		public List<SpecReward> SpecRewardList {get; private set;}

	private void GenerateCacheSpecData()
	{
		SpecChestList = SpecChest.All.ToList();
		SpecAccountLevelExpList = SpecAccountLevelExp.All.ToList();
		SpecChapterList = SpecChapter.All.ToList();
		SpecCharacterList = SpecCharacter.All.ToList();
		SpecCharacterLevelExpList = SpecCharacterLevelExp.All.ToList();
		SpecCharacterTranscendenceList = SpecCharacterTranscendence.All.ToList();
		SpecCharacterEnhanceList = SpecCharacterEnhance.All.ToList();
		SpecCharacterQuotesList = SpecCharacterQuotes.All.ToList();
		SpecCommanderSkillList = SpecCommanderSkill.All.ToList();
		SpecDialogueList = SpecDialogue.All.ToList();
		SpecEventList = SpecEvent.All.ToList();
		SpecEventConditionList = SpecEventCondition.All.ToList();
		SpecLanguageList = SpecLanguage.All.ToList();
		SpecGameConfigList = SpecGameConfig.All.ToList();
		SpecGachaScenarioList = SpecGachaScenario.All.ToList();
		SpecGuideMissionList = SpecGuideMission.All.ToList();
		SpecQuestList = SpecQuest.All.ToList();
		SpecSkillList = SpecSkill.All.ToList();
		SpecStageList = SpecStage.All.ToList();
		SpecStageMonsterList = SpecStageMonster.All.ToList();
		SpecStageRewardList = SpecStageReward.All.ToList();
		SpecDungeonTrialList = SpecDungeonTrial.All.ToList();
		SpecDungeonMonsterList = SpecDungeonMonster.All.ToList();
		SpecDungeonRewardList = SpecDungeonReward.All.ToList();
		SpecSynergyList = SpecSynergy.All.ToList();
		SpecTutorialList = SpecTutorial.All.ToList();
		SpecItemList = SpecItem.All.ToList();
		SpecIdleRewardList = SpecIdleReward.All.ToList();
		SpecInGameVfxList = SpecInGameVfx.All.ToList();
		SpecRewardInfoList = SpecRewardInfo.All.ToList();
		SpecPVPConfigList = SpecPVPConfig.All.ToList();
		SpecPVPTierList = SpecPVPTier.All.ToList();
		SpecPVPRankingList = SpecPVPRanking.All.ToList();
		SpecPVPDummyList = SpecPVPDummy.All.ToList();
		SpecRewardList = SpecReward.All.ToList();
	}
	}
}
