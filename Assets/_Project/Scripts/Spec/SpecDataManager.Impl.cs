#define USE_SERVER_SPEC

#if USE_SERVER_SPEC
using CookApps.LocalData;
#endif
using System.Collections.Generic;
using System.Linq;
using BiniLab;
using CookApps.BattleSystem;
using CookApps.TeamBattle;
using Cysharp.Threading.Tasks;
using Tech.Hive.V1;
using Unity.VisualScripting;
using Unity.Android.Gradle;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Math;

namespace CookApps.AutoBattler
{
    public static class EffectCodeIdGenerator
    {
        public static long GetStatCode(EffectCodeNameType statType, GlobalEffectProviderType globalEffectProviderType, int subId)
        {
            var codeId = (long)globalEffectProviderType * 1000000000 + (long)subId * 1000 + (long)statType;
            if (addedEffectCodeIds.Add(codeId))
            {
                EffectCodePoolManager.Instance.RegisterCodeIdWithBaseCodeId(codeId, (long)statType);
            }
            return codeId;
        }

        private static HashSet<long> addedEffectCodeIds = new();
    }

    public partial class SpecDataManager : SingletonMonoBehaviour<SpecDataManager>
    {
        public async UniTask Initialize(uint serverSpecVersion)
        {
#if USE_SERVER_SPEC
            var localData = new CookAppsLocalData(SecretKey.GetKey());
            string json;
            var localSpecVersion = Preference.LoadPreference(Pref.LOCAL_SPEC_VERSION, 0);
            Debug.Log("localSpecVersion: " + localSpecVersion + " serverSpecVersion: " + serverSpecVersion);
            if (localSpecVersion != serverSpecVersion)
            {
                do
                {
                    json = await NetManager.Instance.Spec.GetSpecDataAsync(SpecType.Game, serverSpecVersion);
                } while (string.IsNullOrEmpty(json));

                localData.Save(json, "SpecData");
            }
            else
            {
                if (!localData.TryLoad("SpecData", out json))
                {
                    do
                    {
                        json = await NetManager.Instance.Spec.GetSpecDataAsync(SpecType.Game, serverSpecVersion);
                    } while (string.IsNullOrEmpty(json));

                    localData.Save(json, "SpecData");
                }
            }
            Preference.SavePreference(Pref.LOCAL_SPEC_VERSION, serverSpecVersion);
#else
            string json = SpecDataResourceLoader.LoadSpecData();
            await UniTask.Yield();
#endif
            bool isLoad = Load(json);
            await UniTask.Yield();
            GenerateCacheSpecData();
            CustomizeSpecData();
            Debug.Log(SpecDataManager.Instance.SpecLanguageList.Count);
        }

        // SpecData Dictionary Cache Data
        private Dictionary<string, Language> languageDic = new();                                  // key : token_key, value : language data
        private Dictionary<int, List<RewardItem>> chestDic = new();                                // key : chest_id, value : chest list
        private Dictionary<int, List<ChapterInfo>> chapterDic = new();                             // key : chapter_id, value : chapter list
        private Dictionary<DifficultyType, List<ChapterInfo>> chapterDifficultDic = new();         // key : DifficultyType, value : chapter list
        private Dictionary<int, List<StageInfo>> stageChapterDic = new();                          // key : chapter_id, value : stage list
        private Dictionary<DifficultyType, List<StageInfo>> stageDifficultDic = new();             // key : DifficultyType, value : stage list
        private Dictionary<int, List<StageMonster>> stageMonsterDic = new();                       // key : chapter_id, value : stage list
        private Dictionary<int, List<StageReward>> stageRewardDic = new();                         // key : reward_id, value : stage list
        private Dictionary<int, List<CharacterInfo>> characterDic = new();                         // key : character_id, value : stage list
        private Dictionary<int, List<MonsterInfo>> monsterDic = new();                             // key : monster_id, value : monster list
        private Dictionary<string, ConfigGame> configDic = new();                                  // key : config_key, value : game config data
        private Dictionary<long, List<SkillActive>> skillDic = new();                              // key : skill_id, value : skill list
        private Dictionary<long, List<SkillActive>> skillPrefabIDDic = new();                      // key : prefab_id, value : skill list
        private Dictionary<DialogueEventType, Dictionary<string, int>> dialogueHistoryDic = new(); // key1 : DialogueEventType, key2 : sub_key_value, value : dialogue_group_id
        private Dictionary<InGameVfxNameType, InGameVfxMap> inGameVfxDic = new();                             // key : inGameVfxName, value : SpecInGameVfx
        private Dictionary<SynergyType, List<ISpecSynergyData>> synergyDic = new();                // key : SynergyType, value : ISpecSynergyData
        private Dictionary<int, List<ObstacleInfo>> obstacleDic = new();                           // key : obstacle_id, value : SpecObstacle
        private Dictionary<EffectCodeNameType, List<SkillJob>> skillJobDic = new();             // key : EffectCodeNameType, value : SkillJob
        private Dictionary<int, List<SkillCommander>> commanderSkillDic = new();                   // key : commander_skill_id, value : SpecCommanderSkill
        private Dictionary<int, BattleItem> battleItemDic = new();                           // key : battle_item_id, value : BattleItem

        private void CustomizeSpecData()
        {
            # region SpecData Dictionary Cache
            // Language
            languageDic.Clear();
            foreach (Language language in Language.All)
            {
                if (!languageDic.ContainsKey(language.token_key))
                {
                    languageDic.Add(language.token_key, language);
                }
            }

            // Chapter
            chapterDic.Clear();
            chapterDifficultDic.Clear();
            foreach (ChapterInfo chapter in ChapterInfo.All)
            {
                if (!chapterDic.TryGetValue(chapter.chapter_id, out List<ChapterInfo> chapterList))
                {
                    chapterList = new List<ChapterInfo>();
                    chapterDic.Add(chapter.chapter_id, chapterList);
                }
                chapterList.Add(chapter);

                if (!chapterDifficultDic.TryGetValue(chapter.difficulty_type, out List<ChapterInfo> chapterDifficultList))
                {
                    chapterDifficultList = new List<ChapterInfo>();
                    chapterDifficultDic.Add(chapter.difficulty_type, chapterDifficultList);
                }
                chapterDifficultList.Add(chapter);
            }

            // Stage
            stageChapterDic.Clear();
            stageDifficultDic.Clear();
            foreach (StageInfo stage in StageInfo.All)
            {
                if (!stageChapterDic.TryGetValue(stage.chapter_id, out List<StageInfo> stageChapterList))
                {
                    stageChapterList = new List<StageInfo>();
                    stageChapterDic.Add(stage.chapter_id, stageChapterList);
                }
                stageChapterList.Add(stage);

                if (!stageDifficultDic.TryGetValue(stage.difficulty_type, out List<StageInfo> stageDiffcultList))
                {
                    stageDiffcultList = new List<StageInfo>();
                    stageDifficultDic.Add(stage.difficulty_type, stageDiffcultList);
                }
                stageDiffcultList.Add(stage);
            }

            // Stage Monster
            stageMonsterDic.Clear();
            foreach (StageMonster stage in StageMonster.All)
            {
                if (!stageMonsterDic.TryGetValue(stage.chapter_id, out List<StageMonster> specStageMonster))
                {
                    specStageMonster = new List<StageMonster>();
                    stageMonsterDic.Add(stage.chapter_id, specStageMonster);
                }

                specStageMonster.Add(stage);
            }

            // Stage Reward
            stageRewardDic.Clear();
            foreach (StageReward stage in StageReward.All)
            {
                if (!stageRewardDic.TryGetValue(stage.reward_id, out List<StageReward> specStageReward))
                {
                    specStageReward = new List<StageReward>();
                    stageRewardDic.Add(stage.reward_id, specStageReward);
                }

                specStageReward.Add(stage);
            }

            // Game Config
            configDic.Clear();
            foreach (ConfigGame config in ConfigGame.All)
            {
                if (!configDic.ContainsKey(config.config_key))
                {
                    configDic.Add(config.config_key, config);
                }
            }

            // Skill
            skillDic.Clear();
            skillPrefabIDDic.Clear();
            foreach (SkillActive skill in SkillActive.All)
            {
                // skillDic
                if (!skillDic.TryGetValue(skill.skill_group_id, out List<SkillActive> skillList1))
                {
                    skillList1 = new List<SkillActive>();
                    skillDic.Add(skill.skill_group_id, skillList1);
                }

                skillList1.Add(skill);

                // skillPrefabIDDic
                if (!skillPrefabIDDic.TryGetValue(skill.prefab_id, out List<SkillActive> skillList2))
                {
                    skillList2 = new List<SkillActive>();
                    skillPrefabIDDic.Add(skill.prefab_id, skillList2);
                }

                skillList2.Add(skill);
            }

            // Dialogue History
            dialogueHistoryDic.Clear();
            foreach (DialogueLanguage dialogue in DialogueLanguage.All)
            {
                if (!dialogueHistoryDic.TryGetValue(dialogue.dialogue_event_type, out Dictionary<string, int> dialogueHistory))
                {
                    dialogueHistory = new Dictionary<string, int>();
                    dialogueHistoryDic.Add(dialogue.dialogue_event_type, dialogueHistory);
                }
                else if (dialogueHistory.ContainsKey(dialogue.sub_key_value) == false)
                {
                    dialogueHistory.Add(dialogue.sub_key_value, dialogue.dialouge_group_id);
                }
            }

            // Character
            characterDic.Clear();
            foreach (CharacterInfo character in CharacterInfo.All)
            {
                if (!characterDic.TryGetValue(character.character_id, out List<CharacterInfo> specCharacter))
                {
                    specCharacter = new List<CharacterInfo>();
                    characterDic.Add(character.character_id, specCharacter);
                }

                specCharacter.Add(character);
            }

            // Monster
            monsterDic.Clear();
            foreach (MonsterInfo monster in MonsterInfo.All)
            {
                if (!monsterDic.TryGetValue(monster.monster_id, out List<MonsterInfo> specMonster))
                {
                    specMonster = new List<MonsterInfo>();
                    monsterDic.Add(monster.monster_id, specMonster);
                }

                specMonster.Add(monster);
            }

            // InGameVfx
            inGameVfxDic.Clear();
            foreach (InGameVfxMap inGameVfx in InGameVfxMap.All)
            {
                if (!inGameVfxDic.ContainsKey(inGameVfx.vfx_name_type))
                {
                    inGameVfxDic.Add(inGameVfx.vfx_name_type, inGameVfx);
                }
            }

            // synergyElementDic Dic
            synergyDic.Clear();
            foreach (SynergyElemental synergy in SynergyElemental.All)
            {
                if (!synergyDic.TryGetValue(synergy.synergy_type, out var list))
                {
                    list = new List<ISpecSynergyData>();
                    synergyDic.Add(synergy.synergy_type, list);
                }

                list.Add(synergy);
            }
            foreach (SynergyStarAsterism synergy in SynergyStarAsterism.All)
            {
                if (!synergyDic.TryGetValue(synergy.synergy_type, out var list))
                {
                    list = new List<ISpecSynergyData>();
                    synergyDic.Add(synergy.synergy_type, list);
                }
                list.Add(synergy);
            }


            // skillJobDic Dic
            skillJobDic.Clear();
            foreach (SkillJob skillJob in SkillJob.All)
            {
                if (!skillJobDic.TryGetValue(skillJob.passive_skill_type, out var list))
                {
                    list = new List<SkillJob>();
                    skillJobDic.Add(skillJob.passive_skill_type, list);
                }
                list.Add(skillJob);
            }

            // Commander Skill Dic
            commanderSkillDic.Clear();
            foreach (SkillCommander commanderSkill in SkillCommander.All)
            {
                if (!commanderSkillDic.TryGetValue(commanderSkill.commander_skill_id, out var list))
                {
                    list = new List<SkillCommander>();
                    commanderSkillDic.Add(commanderSkill.commander_skill_id, list);
                }
                list.Add(commanderSkill);
            }

            #endregion

            obstacleDic.Clear();
            foreach (ObstacleInfo obstacle in ObstacleInfo.All)
            {
                if (!obstacleDic.TryGetValue(obstacle.obstacle_id, out var list))
                {
                    list = new List<ObstacleInfo>();
                    obstacleDic.Add(obstacle.obstacle_id, list);
                }

                list.Add(obstacle);
            }

            battleItemDic.Clear();
            foreach (BattleItem battleItem in BattleItem.All)
            {
                if (!battleItemDic.TryGetValue(battleItem.prefab_id, out var battleItemData))
                {
                    battleItemData = battleItem;
                    battleItemDic.Add(battleItem.prefab_id, battleItem);
                }
            }
        }

        public string GetLanguageText(string tokenKey, LanguageType targetLanguageType)
        {
            if (languageDic.TryGetValue(tokenKey, out var languageData))
            {
                switch (targetLanguageType)
                {
                    case LanguageType.KR:
                        return languageData.language_kr;
                    case LanguageType.EN:
                        return languageData.language_en;
                }
            }

            return string.Empty;
        }

        public T GetGameConfig<T>(string key)
        {
            if (!configDic.TryGetValue(key, out var configData))
            {
                return default;
            }

            if (typeof(T) == typeof(int) && configData.config_value_type == ConfigValueType.INT) return int.Parse(configData.config_value).ConvertTo<T>();
            if (typeof(T) == typeof(float) && configData.config_value_type == ConfigValueType.FLOAT) return float.Parse(configData.config_value).ConvertTo<T>();
            if (typeof(T) == typeof(string) && configData.config_value_type == ConfigValueType.STRING) return configData.config_value.ConvertTo<T>();

            return configData.config_value.ConvertTo<T>();
        }

        public CharacterInfo GetCharacterData(int characterID)
        {
            return SpecCharacterList.Find(character => character.character_id == characterID);
        }

        public BattleItem GetBattleItemData(int battleItemID)
        {
            return battleItemDic.GetValueOrDefault(battleItemID);
        }

        public List<CharacterInfo> GetCharacterListByCharacterType(CharacterType type)
        {
            return SpecCharacterList.FindAll(character => character.character_type == type);
        }

        public int GetCharacterMaxLevel()
        {
            return SpecCharacterLevelExpList.Max(data => data.level);
        }

        public CharacterLevelExp GetCharacterLevelExpData(int level)
        {
            return SpecCharacterLevelExpList.Find(data => data.level == level);
        }

        public List<CharacterLevelExp> GetCharacterLevelExpDataList(int level)
        {
            return SpecCharacterLevelExpList.FindAll(data => data.level <= level);
        }

        public CharacterTranscendence GetCharacterTranscendenceData(SynergyType elementType, GradeType gradeType, int transcendenceLevel)
        {
            return SpecCharacterTranscendenceList.Find(data => data.element_type == elementType
                                                               && data.grade_type == gradeType
                                                               && data.transcendence_lv == transcendenceLevel);
        }

        public List<CharacterTranscendence> GetCharacterTranscendenceDataList(SynergyType elementType, GradeType gradeType)
        {
            return SpecCharacterTranscendenceList.FindAll(data => data.element_type == elementType
                                                                  && data.grade_type == gradeType);
        }

        // 캐릭터 레벨업에 필요한 총 필요 아이템 리스트 반환
        // 7.15 - 현재 10레벨 단위 레벨업 시 조각을 소모하므로 characterID를 인자로 넘겨 받음
        public List<RewardItem> GetCharacterLevelupTotalNeedItemList(int level, int characterID)
        {
            if (level <= 1) return null;

            int targetLevel = level - 1;

            List<RewardItem> resultItemList = new List<RewardItem>();

            var levelExpData = GetCharacterLevelExpData(targetLevel);
            if (levelExpData != null)
            {
                RewardItem needGoldItem = new RewardItem(ItemType.GOLD, 0, levelExpData.need_gold_sum);
                resultItemList.Add(needGoldItem);

                RewardItem needExpItem = new RewardItem(ItemType.CHAR_USER_EXP_ITEM, 0, levelExpData.base_levelup_item_sum);
                resultItemList.Add(needExpItem);

                if (levelExpData.sec_levelup_item_sum > 0)
                {
                    RewardItem needSecondLevelupItem = new RewardItem(levelExpData.sec_levelup_item_type, characterID, levelExpData.sec_levelup_item_sum);
                    resultItemList.Add(needSecondLevelupItem);
                }
            }

            return resultItemList;
        }

        public CharacterQuotes GetCharacterQuotesDataByPrefabID(int prefabID)
        {
            return SpecCharacterQuotesList.Find(data => data.prefab_id == prefabID);
        }

        public ChapterInfo GetChapterData(int chapterID)
        {
            return SpecChapterList.Find(dat => dat.chapter_id == chapterID);
        }

        public ChapterInfo GetChapterData(int chapterID, DifficultyType type)
        {
            if (chapterDic.TryGetValue(chapterID, out List<ChapterInfo> chapterList))
            {
                return chapterList.Find(data => data.difficulty_type == type);
            }

            return null;
        }

        public ChapterInfo GetChapterDataByStageID(int stageID)
        {
            var specStage = GetStageData(stageID);
            if (specStage != null)
            {
                if (chapterDic.TryGetValue(specStage.chapter_id, out List<ChapterInfo> chapterList))
                {
                    return chapterList.Find(data => data.difficulty_type == specStage.difficulty_type);
                }
            }

            return null;
        }

        public List<ChapterInfo> GetChapterList(int chapter)
        {
            if (chapterDic.TryGetValue(chapter, out List<ChapterInfo> chapterList))
            {
                return chapterList;
            }

            return null;
        }

        public List<ChapterInfo> GetChapterList(DifficultyType difficulty)
        {
            if (chapterDifficultDic.TryGetValue(difficulty, out List<ChapterInfo> chapterList))
            {
                return chapterList;
            }

            return null;
        }

        public int GetTotalChapterStarCount(int chapterID, DifficultyType type)
        {
            int totalStarCount = 0;

            int stageStarCount = GetGameConfig<int>("max_stage_star_count");

            if (stageChapterDic.TryGetValue(chapterID, out List<StageInfo> stageList))
            {
                return stageList.FindAll(stage => stage.difficulty_type == type).Count * stageStarCount;
            }

            return totalStarCount;
        }

        public List<DialogueLanguage> GetDialogueListByGroupID(int groupID)
        {
            return SpecDialogueList.FindAll(data => data.dialouge_group_id == groupID);
        }

        public int GetDialgueGroupIDByEventType(DialogueEventType eventType, string subKeyValue)
        {
            int result = 0;

            if (dialogueHistoryDic.TryGetValue(eventType, out Dictionary<string, int> dialogueHistory))
            {
                dialogueHistory.TryGetValue(subKeyValue, out result);
            }

            return result;
        }

        public StageInfo GetStageData(int stageID)
        {
            return SpecStageList.Find(data => data.stage_id == stageID);
        }

        public StageInfo GetStageData(int chapterID, int stageNumber, DifficultyType type)
        {
            if (stageChapterDic.TryGetValue(chapterID, out List<StageInfo> stageList))
            {
                return stageList.Find(stage => stage.stage_number == stageNumber && stage.difficulty_type == type);
            }

            return null;
        }

        public StageInfo GetStageData(int chapterID, DifficultyType difficultyType, StageType stageType)
        {
            if (stageChapterDic.TryGetValue(chapterID, out List<StageInfo> stageList))
            {
                return stageList.Find(stage => stage.difficulty_type == difficultyType && stage.stage_type == stageType);
            }

            return null;
        }


        public List<StageInfo> GetStageList(int chapter)
        {
            if (stageChapterDic.TryGetValue(chapter, out List<StageInfo> stageList))
            {
                return stageList;
            }

            return null;
        }

        public List<StageInfo> GetStageList(int chapter, DifficultyType difficulty)
        {
            if (stageChapterDic.TryGetValue(chapter, out List<StageInfo> stageList))
            {
                return stageList.FindAll(stage => stage.difficulty_type == difficulty);
            }

            return null;
        }

        // 타겟 스테이지 아래의 모든 스테이지 리스트 반환
        public List<StageInfo> GetPrevStageList(int targetStageID)
        {
            StageInfo targetStageData = GetStageData(targetStageID);
            if (targetStageData == null) return null;

            List<StageInfo> resultStageList = new List<StageInfo>();

            var targetChapterData = GetChapterDataByStageID(targetStageID);
            if (targetChapterData == null) return null;

            List<StageInfo> totalStageList = new();
            for (int chapter = 1; chapter <= targetStageData.chapter_id; chapter++)
            {
                var stageList = GetStageList(chapter, targetStageData.difficulty_type);
                totalStageList.AddRange(stageList);
            }

            foreach (var stage in totalStageList)
            {
                if (stage.chapter_id < targetStageData.chapter_id ||
                    (stage.chapter_id == targetStageData.chapter_id && stage.stage_number <= targetStageData.stage_number))
                {
                    resultStageList.Add(stage);
                }
            }

            return resultStageList;
        }

        public int GetStageCount(int chapter, DifficultyType difficulty)
        {
            if (stageChapterDic.TryGetValue(chapter, out List<StageInfo> stageList))
            {
                return stageList.FindAll(stage => stage.difficulty_type == difficulty).Count;
            }

            return 0;
        }

        // 해당 챕터의 마지막 스테이지 데이터 반환
        public StageInfo GetLastStageData(int chapterID, DifficultyType difficulty)
        {
            Debug.LogColor($"GetLastStageData chapterID: {chapterID}, difficulty: {difficulty}");
            if (stageChapterDic.TryGetValue(chapterID, out List<StageInfo> stageList))
            {
                var targetStageList = stageList.FindAll(stage => stage.difficulty_type == difficulty);

                int maxStageNumber = targetStageList.Max(stage => stage.stage_number);
                return targetStageList.Find(stage => stage.stage_number == maxStageNumber);
            }

            return null;
        }

        // 가장 마지막 스테이지 데이터 반환
        public StageInfo GetEndStage()
        {
            var lastChapterData = SpecChapterList.Max(data => data.chapter_id);

            return GetLastStageData(lastChapterData, DifficultyType.NORMAL);
        }

        // 해당 스테이지 데이터 기준 다음 스테이지 정보 반환
        public StageInfo GetNextStageData(int targetStageID)
        {
            StageInfo resultData = null;

            var targetStageData = GetStageData(targetStageID);
            if (targetStageData == null) return resultData;

            var lastSpecStage = GetLastStageData(targetStageData.chapter_id, targetStageData.difficulty_type);
            bool isPlayingLastStage = lastSpecStage != null && lastSpecStage.stage_id == targetStageData.stage_id;
            if (isPlayingLastStage)
            {
                // 다음 챕터 존재 여부 확인 (챕터 데이터가 없을 경우 null을 리턴함)
                int nextChpaterID = lastSpecStage.chapter_id + 1;
                resultData = GetStageData(nextChpaterID, 1, lastSpecStage.difficulty_type);
            }
            else
            {
                // 다음 스테이지 데이터 확인
                int nextStageNumber = targetStageData.stage_number + 1;
                resultData = GetStageData(targetStageData.chapter_id, nextStageNumber, targetStageData.difficulty_type);
            }

            return resultData;
        }

        // 해당 스테이지가 마지막 스테이지인지 체크
        public bool IsLastStage(int stageID)
        {
            StageInfo stageSpecData = GetStageData(stageID);
            StageInfo nextStageSpecData = GetStageData(stageSpecData.chapter_id, stageSpecData.stage_number + 1, stageSpecData.difficulty_type);

            return nextStageSpecData == null;
        }

        public StageMonster GetStageMonsterData(int chapterID, int stageNumber, DifficultyType type)
        {
            if (stageMonsterDic.TryGetValue(chapterID, out List<StageMonster> stageMonster))
            {
                return stageMonster.Find(s => s.stage_number == stageNumber && s.difficulty_type == type);
            }

            return null;
        }

        public List<StageMonster> GetStageMonsterList(int chapter, int stageNumber, DifficultyType difficulty)
        {
            if (stageMonsterDic.TryGetValue(chapter, out List<StageMonster> stageMonster))
            {
                return stageMonster.FindAll(s => s.stage_number == stageNumber && s.difficulty_type == difficulty);
            }

            return null;
        }

        // 해당 챕터에서 받을 수 있는 Idle 보상 리스트 반환 (해당 챕터 이하 리스트 모두 반환)
        public List<IdleReward> GetAllIdleRewardList(int chapterID)
        {
            return SpecIdleRewardList.FindAll(data => data.chapter_id <= chapterID);
        }

        public List<RewardInfo> GetSpecRewardInfoList(int rewardID)
        {
            return SpecRewardInfoList.FindAll(dataa => dataa.reward_id == rewardID);
        }

        // 보상 데이터 리스트 반환
        public List<RewardInfo> GetSpecRewardInfoList(ContentType contentType, int contentKey, DifficultyType difficultyType)
        {
            return SpecRewardInfoList.FindAll(data => data.content_type == contentType
                                                      && data.content_key_value == contentKey
                                                      && data.difficulty_type == difficultyType);
        }

        // 스테이지 보상 데이터를 RewardItem 리스트로 변환
        public List<RewardItem> GetRewardItemListByStageRewardList(List<StageReward> stageRewardList)
        {
            List<RewardItem> rewardItemList = new List<RewardItem>();
            foreach (var stageReward in stageRewardList)
            {
                rewardItemList.Add(new RewardItem(stageReward.item_type, stageReward.item_key, stageReward.item_count));
            }

            return rewardItemList;
        }

        // 리워드 인포 데이터를 RewardItem 리스트로 변환
        public List<RewardItem> GetRewardItemListByRewadInfoList(List<RewardInfo> rewardInfoList)
        {
            List<RewardItem> rewardItemList = new List<RewardItem>();
            foreach (var rewardInfo in rewardInfoList)
            {
                rewardItemList.Add(new RewardItem(rewardInfo.item_type, rewardInfo.item_key, rewardInfo.item_count));
            }

            return rewardItemList;
        }

        public List<RewardItem> GetChestList(int chestId)
        {
            return chestDic.GetValueOrDefault(chestId);
        }

        public List<SkillActive> GetSkillDataList(long skillID)
        {
            return skillDic.GetValueOrDefault(skillID);
        }

        public List<SkillActive> GetSkillDataListByPrefabID(int prefabID)
        {
            return skillPrefabIDDic.GetValueOrDefault(prefabID);
        }

        public SkillActive GetSkillData(int skillID, SkillValueType type)
        {
            return SkillActiveList.Find(data => data.skill_group_id == skillID && data.skill_value_type == type);
        }

        public List<SkillCommander> GetCommanderSkillList(int chapterID)
        {
            return SpecCommanderSkillList.FindAll(data => data.open_key_chapter_id == chapterID);
        }

        public List<SkillCommander> GetCommanderSkillIncludeList(int chapterID)
        {
            return SpecCommanderSkillList.FindAll(data => data.open_key_chapter_id <= chapterID);
        }

        public int GetFirstCommanderSkillChapter()
        {
            int openChapterID = SpecCommanderSkillList.Min(data => data.open_key_chapter_id) - 1;
            return stageChapterDic[2].Last().stage_id;
        }

        public List<SkillCommander> GetCommanderSkillDataList(int commanderSkillID)
        {
            if (commanderSkillDic.TryGetValue(commanderSkillID, out List<SkillCommander> commanderSkillList))
            {
                return commanderSkillList;
            }
            return null;
        }
        public SkillCommander GetCommanderSkillListByUserSkillLevel(int commanderSkillID, int userSkillLevel)
        {
            if (commanderSkillDic.TryGetValue(commanderSkillID, out List<SkillCommander> commanderSkillList))
            {
                foreach (var commanderSkillData in commanderSkillList)
                {
                    //해당 스킬에대한 유저 스킬 레벨 체크
                    if (commanderSkillData.level == userSkillLevel)
                    {
                        return commanderSkillData;
                    }
                }
            }
            return null;
        }
        public List<int> GetCommanderSkillCodeIdList()
        {
            List<int> outCommanderSkillCodeIdList = new List<int>();
            foreach (var commanderSkillPair in commanderSkillDic)
            {
                outCommanderSkillCodeIdList.Add(commanderSkillPair.Key);
            }
            return outCommanderSkillCodeIdList;
        }

        public ChapterRule GetChapterRuleData(int chapterRuleID)
        {
            return SpecChapterRuleList.FirstOrDefault(data => (int)data.chapter_rule_effect_code_type == chapterRuleID);
        }

        public Item GetSpecItemData(ItemType itemType)
        {
            return SpecItemList.Find(data => data.item_type == itemType);
        }

        public ISpecCharacterInfo GetSpecCharacter(int characterID)
        {
            ISpecCharacterInfo outCharacterInfo = SpecCharacterList.FirstOrDefault(data => data.character_id == characterID);
            if (outCharacterInfo != null)
            {
                return outCharacterInfo;
            }

            outCharacterInfo = SpecMonsterList.FirstOrDefault(data => data.monster_id == characterID);
            if (outCharacterInfo != null)
            {
                return outCharacterInfo;
            }

            outCharacterInfo = battleItemDic.GetValueOrDefault(characterID);
            if (outCharacterInfo != null)
            {
                return outCharacterInfo;
            }

            Debug.LogError($"CharacterID: {characterID} not found");
            return null;
        }

        public int GetLeftCharacterID(int characterID, CharacterType characterType)
        {
            if (SpecCharacterList == null || SpecCharacterList.Count == 0) return characterID;

            var targetCharacterList = SpecCharacterList.FindAll(c => c.character_type == characterType);

            int idx = targetCharacterList.FindIndex(c => c.character_id == characterID);
            if (idx < 0)
                return SpecCharacterList.FindAll(c => c.character_type == characterType)[0].character_id; // 못 찾으면 첫 번째로

            int leftIdx = (idx == 0) ? targetCharacterList.Count - 1 : idx - 1;
            return targetCharacterList[leftIdx].character_id;
        }

        public int GetRightCharacterID(int characterID, CharacterType characterType)
        {
            if (SpecCharacterList == null || SpecCharacterList.Count == 0) return characterID;

            var targetCharacterList = SpecCharacterList.FindAll(c => c.character_type == characterType);

            int idx = targetCharacterList.FindIndex(c => c.character_id == characterID);
            if (idx < 0)
                return SpecCharacterList.FindAll(c => c.character_type == characterType)[0].character_id;

            int rightIdx = (idx == targetCharacterList.Count - 1) ? 0 : idx + 1;
            return targetCharacterList[rightIdx].character_id;
        }

        public AccountLevelExp GetAccountLevelExpDataByLevel(int level)
        {
            return SpecAccountLevelExpList.Find(data => data.lv == level);
        }

        public int GetAccountMaxLevel()
        {
            return SpecAccountLevelExpList.Max(data => data.lv);
        }

        public int GetAccountLevelByExp(long exp)
        {
            // 최대 레벨 체크
            int maxLevel = GetAccountMaxLevel();
            if (exp >= AccountLevelExp.Get(maxLevel).exp_last)
            {
                return maxLevel;
            }

            // 나머지 레벨 체크
            foreach (var accountData in SpecAccountLevelExpList)
            {
                if (accountData.exp_start > exp)
                {
                    return accountData.lv == 1 ? accountData.lv : accountData.lv - 1;
                }
            }

            return 1;
        }

        public GuideMissionInfo GetGuideMissionDataByOrder(int order)
        {
            return SpecGuideMissionList.Find(data => data.order == order);
        }

        public List<GuideMissionInfo> GetGuideMissionDataList(int order)
        {
            return SpecGuideMissionList.FindAll(data => data.order <= order);
        }

        // 가이드 미션 order 최대치 반환
        public int GetGuideMissionMaxOrder()
        {
            return SpecGuideMissionList.Max(guide => guide.order);
        }

        public List<StageReward> GetSpecStageReward(int rewardID)
        {
            if (stageRewardDic.TryGetValue(rewardID, out List<StageReward> stageRewardList))
            {
                return stageRewardList;
            }

            return null;
        }

        public InGameVfxMap GetInGameVfxData(InGameVfxNameType vfxNameType)
        {
            return inGameVfxDic.GetValueOrDefault(vfxNameType);
        }

        public List<ISpecSynergyData> GetSpecSynergyList(SynergyType synergyType)
        {
            if (synergyDic.TryGetValue(synergyType, out List<ISpecSynergyData> synergyList))
            {
                return synergyList;
            }
            return null;
        }

        public bool TryGetSynergyDataByCount(SynergyType synergyType, int count,
            out ISpecSynergyData outSynergyData, out List<ISpecSynergyData> outSynergyList)
        {
            outSynergyData = null;
            outSynergyList = GetSpecSynergyList(synergyType);
            if (outSynergyList == null)
            {
                return false;
            }
            outSynergyData = outSynergyList.Find(l => l.min_int <= count && l.max_int >= count);
            if (outSynergyData == null || outSynergyData.grade < 1)
            {
                return false;
            }
            return true;
        }

        public List<List<SkillJob>> GetPassivePositionList(CharacterPositionType positionType)
        {
            if (positionType == CharacterPositionType.NONE)
            {
                return null;
            }

            List<List<SkillJob>> passiveList = new List<List<SkillJob>>();
            foreach (var positionPassive in SkillJobPassive.All)
            {
                if (positionType != positionPassive.position_type)
                {
                    continue;
                }
                if (skillJobDic.TryGetValue(positionPassive.passive_id, out List<SkillJob> passive))
                {
                    passiveList.Add(passive);
                }
            }

            return passiveList;
        }

        public QuestInfo GetSpecQuestData(int questID)
        {
            return SpecQuestList.Find(data => data.quest_id == questID);
        }

        public List<QuestInfo> GetSpecQuestList(TermType termType, bool isIncludeMilestone)
        {
            if (isIncludeMilestone)
            {
                return SpecQuestList.FindAll(data => data.term_type == termType);
            }

            return SpecQuestList.FindAll(data =>
                data.term_type == termType
                && data.quest_type != QuestType.CLEAR_DAILY_QUEST
                && data.quest_type != QuestType.CLEAR_WEEKLY_QUEST);
        }

        public List<QuestInfo> GetSpecQuestList(QuestType questType)
        {
            return SpecQuestList.FindAll(data => data.quest_type == questType);
        }

        public List<QuestInfo> GetSpecQuestList(TermType termType, QuestType questType)
        {
            return SpecQuestList.FindAll(data => data.term_type == termType && data.quest_type == questType);
        }

        public EventInfo GetSpecEventData(int eventID)
        {
            return SpecEventList.Find(data => data.event_id == eventID);
        }

        public EventInfo GetSpecEventData(EventType eventType)
        {
            return SpecEventList.Find(data => data.event_type == eventType);
        }

        public List<EventInfo> GetSpecEventList(EventType eventType)
        {
            return SpecEventList.FindAll(data => data.event_type == eventType);
        }

        public List<EventInfo> GetSpecEventList(TermType termType)
        {
            return SpecEventList.FindAll(data => data.term_type == termType);
        }

        // 기간 제한이 존재하는 이벤트 리스트를 반환
        public List<EventInfo> GetLimitedSpecEventList()
        {
            return SpecEventList.FindAll(data => data.frequency_type == FrequencyType.ONCE);
        }

        // 기간 제한이 존재하지 않는 이벤트 리스트를 반환 (서비스 중 기간동안 지속 반복)
        public List<EventInfo> GetNoneLimitedSpecEventList()
        {
            return SpecEventList.FindAll(data => data.frequency_type == FrequencyType.REPEAT);
        }

        // 현재 시간 기준, 운영 기간에 해당하는 이벤트 데이터를 반환
        public EventInfo GetCurrentSpecEvent(EventType eventType)
        {
            var eventList = GetSpecEventList(eventType);

            foreach (var eventData in eventList)
            {
                var startAtTimeStamp = TimeManager.Instance.ChangeDateStringToTimeStamp(eventData.start_at);
                var endAtTimeStamp = TimeManager.Instance.ChangeDateStringToTimeStamp(eventData.end_at);

                var nowTimeStamp = TimeManager.Instance.UtcNowTimeStampLocal();

                if (startAtTimeStamp <= nowTimeStamp && nowTimeStamp <= endAtTimeStamp)
                {
                    return eventData;
                }
            }

            return null;
        }

        // 현재 시간 기준, 운영 기간에 해당하는 이벤트 데이터 리스트를 반환
        public List<EventInfo> GetCurrentSpecEventList()
        {
            List<EventInfo> resultEventList = new List<EventInfo>();

            // 기간 제한이 없는 이벤트 데이터 처리
            List<EventInfo> noneLimitedSpecEventList = GetNoneLimitedSpecEventList();
            if (noneLimitedSpecEventList != null && noneLimitedSpecEventList.Count > 0)
            {
                resultEventList.AddRange(noneLimitedSpecEventList);
            }

            // 기간 제한이 있는 이벤트 데이터 처리
            List<EventInfo> limitedSpecEventList = GetLimitedSpecEventList();
            foreach (var eventData in limitedSpecEventList)
            {
                var startAtTimeStamp = TimeManager.Instance.ChangeDateStringToTimeStamp(eventData.start_at);
                var endAtTimeStamp = TimeManager.Instance.ChangeDateStringToTimeStamp(eventData.end_at);

                var nowTimeStamp = TimeManager.Instance.UtcNowTimeStampLocal();

                if (startAtTimeStamp <= nowTimeStamp && nowTimeStamp <= endAtTimeStamp)
                {
                    resultEventList.Add(eventData);
                }
            }

            return resultEventList;
        }

        public EventCondition GetSpecEventConditionData(int eventID, int eventConditionID)
        {
            return SpecEventConditionList.Find(data => data.event_id == eventID && data.event_condition_id == eventConditionID);
        }

        public List<EventCondition> GetSpecEventConditionList(int eventID)
        {
            return SpecEventConditionList.FindAll(data => data.event_id == eventID);
        }

        public DungeonBabelInfo GetSpecDungeonTrialData(int dungeonID)
        {
            return SpecDungeonTrialList.Find(data => data.dungeon_id == dungeonID);
        }

        public DungeonBabelInfo GetSpecDungeonTrialDataByOrder(int order)
        {
            return SpecDungeonTrialList.Find(data => data.order == order);
        }

        public List<DungeonBabelInfo> GetSpecDungeonTrialDataList(DungeonType dungeonType)
        {
            return SpecDungeonTrialList.FindAll(data => data.dungeon_type == dungeonType);
        }

        public List<DungeonBabelInfo> GetSpecDungeonTrialDataListByStageStar(int stageStar)
        {
            return SpecDungeonTrialList.FindAll(data => data.need_star <= stageStar);
        }

        public List<DungeonBabelMonster> GetSpecDungeonMonsterDataList(DungeonType dungeonType, int dungeonID)
        {
            return SpecDungeonMonsterList
                .FindAll(data => data.dungeon_type == dungeonType && data.dungeon_id == dungeonID);
        }

        public List<DungeonBabelReward> GetSpecDungeonRewardDataList(DungeonType dungeonType, int dungeonID)
        {
            return SpecDungeonRewardList
                .FindAll(data => data.dungeon_type == dungeonType && data.dungeon_id == dungeonID);
        }

        public List<ObstacleInfo> GetSpecObstacleList(int obstacleID)
        {
            if (obstacleDic.TryGetValue(obstacleID, out List<ObstacleInfo> obstacleList))
            {
                return obstacleList;
            }

            return null;
        }

        #region Shop

        public ShopInfo GetShopData(int shopID)
        {
            return SpecShopList.Find(data => data.shop_id == shopID);
        }

        public List<ShopInfo> GetShopDataList(ShopMainGroupType mainGroupType)
        {
            return SpecShopList.FindAll(data => data.shop_main_group_type == mainGroupType);
        }

        public List<ShopInfo> GetShopDataList(ShopMainGroupType mainGroupType, ShopSubGroupType subGroupType)
        {
            return SpecShopList.FindAll(data => data.shop_main_group_type == mainGroupType && data.shop_sub_group_type == subGroupType);
        }

        public ShopBanner GetShopBannerData(int shopID)
        {
            return SpecShopBannerList.Find(data => data.shop_id == shopID);
        }

        #endregion

        #region Gacha

        public GachaInfo GetGachaData(int gachaID)
        {
            return SpecGachaList.Find(data => data.gacha_id == gachaID);
        }

        public GachaInfo GetGachaData(GachaType gachaType, int gachaCount)
        {
            return SpecGachaList.Find(data => data.gacha_type == gachaType && data.gacha_count == gachaCount);
        }

        public List<GachaInfo> GetGachaDataList(GachaType gachaType)
        {
            return SpecGachaList.FindAll(data => data.gacha_type == gachaType);
        }

        public List<GachaCharacter> GetGachaContentDataList(int gachaGroupID)
        {
            return SpecGachaContentList.FindAll(data => data.gacha_group_id == gachaGroupID);
        }

        // 가챠 항목에서 랜덤으로 아이템을 뽑아 갯수만큼 반환
        public List<GachaCharacter> GetRandomPickGachaContentList(int gachaGroupID, int count)
        {
            List<GachaCharacter> resultList = new List<GachaCharacter>();

            var targetList = GetGachaContentDataList(gachaGroupID);
            if (targetList != null && targetList.Count > 0)
            {
                for (int i = 0; i < count; ++i)
                {
                    GachaCharacter selectedData = targetList.RandomRatePick(content => content.weight);
                    resultList.Add(selectedData);
                }
            }

            return resultList;
        }

        // 가챠 항목에서 랜덤으로 아이템을 뽑아 RewardItem 형태로 반환
        public List<RewardItem> GetRandomPickGachaRewardItemList(int gachaGroupID, int count)
        {
            List<RewardItem> rewardItemList = new List<RewardItem>();

            var targetList = GetGachaContentDataList(gachaGroupID);
            if (targetList != null && targetList.Count > 0)
            {
                for (int i = 0; i < count; ++i)
                {
                    GachaCharacter selectedData = targetList.RandomRatePick(content => content.weight);
                    if (selectedData != null)
                    {
                        rewardItemList.Add(new RewardItem(selectedData.result_item_type, selectedData.result_item_key, selectedData.result_item_count));
                    }
                }
            }

            return rewardItemList;
        }

        // 가챠 결과 데이터를 RewardItem 리스트로 변환
        public List<RewardItem> GetRewardItemListByGachaContentList(List<GachaCharacter> gachaContentList)
        {
            List<RewardItem> rewardItemList = new List<RewardItem>();
            foreach (var gachaContent in gachaContentList)
            {
                rewardItemList.Add(new RewardItem(gachaContent.result_item_type, gachaContent.result_item_key, gachaContent.result_item_count));
            }

            return rewardItemList;
        }

        // 시나리오 가챠 데이터 반환
        public List<GachaScenario> GetGachaScenarioList(int currentCount, int gachaCount)
        {
            int maxCount = SpecGachaScenarioList.Count;
            int resultCount = currentCount + gachaCount > maxCount ? maxCount - currentCount : gachaCount;

            return SpecGachaScenarioList.GetRange(currentCount, resultCount);
        }

        // 시나리오 가챠 데이터를 RewardItem 리스트로 변환
        public List<RewardItem> GetRewardItemListByGachaScenarioList(List<GachaScenario> gachaScenarioList)
        {
            List<RewardItem> rewardItemList = new List<RewardItem>();
            foreach (var gachaScenario in gachaScenarioList)
            {
                rewardItemList.Add(new RewardItem(gachaScenario.item_type, gachaScenario.item_key, gachaScenario.item_count));
            }

            return rewardItemList;
        }

        #endregion

        // public List<SpecSynergy> GetInGameVfxData(InGameVfxNameType vfxNameType)
        // {
        //     return inGameVfxDic.GetValueOrDefault(vfxNameType);
        // }
        //
        // public SpecInGameVfx GetInGameVfxData(InGameVfxNameType vfxNameType)
        // {
        //     return inGameVfxDic.GetValueOrDefault(vfxNameType);
        // }
        public bool GetIsOpenCondition(OpenConditionType conditionType)
        {
            // 모든 가이드 미션 클리어 체크
            if (UserDataManager.Instance.UserMissionData.GuideMissionCurrentOrder > GetGuideMissionMaxOrder())
            {
                return true;
            }

            var guideMissionData = UserDataManager.Instance.GetCurrentGuideMissionData();

            int currMissionID = guideMissionData.MissionId;
            var openCondition = SpecOpenConditionList.Find(l => l.open_condition_Type == conditionType);
            return openCondition != null && openCondition.guide_mission_id <= currMissionID;
        }

        public ImageInfo GetImageInfoData(int infoID)
        {
            return SpecImageInfoList.Find(dat => dat.image_info_id == infoID);
        }

        public List<TileEffectCode> GetSpecTileEffectCodeList()
        {
            return SpecTileEffectCodeList;
        }
    }
}
