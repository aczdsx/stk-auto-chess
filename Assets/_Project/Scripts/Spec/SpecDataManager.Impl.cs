// #define USE_SERVER_SPEC

#if USE_SERVER_SPEC
using CookApps.gRPC.Universal;
using CookApps.LocalData;
#endif
using System.Collections.Generic;
using System.Linq;
using CookApps.BattleSystem;
using CookApps.TeamBattle;
using Cysharp.Threading.Tasks;
using Unity.VisualScripting;

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

        private static HashSet<long> addedEffectCodeIds = new ();
    }

    public partial class SpecDataManager : SingletonMonoBehaviour<SpecDataManager>
    {
        public async UniTask Initialize()
        {
            await UniTask.Yield();
#if USE_SERVER_SPEC
        var localData = new CookAppsLocalData(SecretKey.GetKey());
        string json;
        if (UniversalGrpcManager.Instance.IsSpecUpdated)
        {
            do
            {
                json = await UniversalGrpcManager.Instance.GetSpecDataAsync();
            } while (string.IsNullOrEmpty(json));

            localData.SaveString(json, "SpecData");
        }
        else
        {
            if (!localData.TryLoadString("SpecData", out json))
            {
                do
                {
                    json = await UniversalGrpcManager.Instance.GetSpecDataAsync();
                } while (string.IsNullOrEmpty(json));

                localData.SaveString(json, "SpecData");
            }
        }
#else
            string json = SpecDataResourceLoader.LoadSpecData();
            await UniTask.Yield();
#endif
            Load(json);
            await UniTask.Yield();
            GenerateCacheSpecData();
            CustomizeSpecData();
        }

        // SpecData Dictionary Cache Data
        private Dictionary<string, SpecLanguage> languageDic = new (); // key : token_key, value : language data
        private Dictionary<int, List<RewardItem>> chestDic = new (); // key : chest_id, value : chest list
        private Dictionary<int, List<SpecChapter>> chapterDic = new (); // key : chapter_id, value : chapter list
        private Dictionary<DifficultyType, List<SpecChapter>> chapterDifficultDic = new (); // key : DifficultyType, value : chapter list
        private Dictionary<int, List<SpecStage>> stageChapterDic = new (); // key : chapter_id, value : stage list
        private Dictionary<DifficultyType, List<SpecStage>> stageDifficultDic = new (); // key : DifficultyType, value : stage list
        private Dictionary<int, List<SpecStageMonster>> stageMonsterDic = new (); // key : chapter_id, value : stage list
        private Dictionary<int, List<SpecStageReward>> stageRewardDic = new (); // key : reward_id, value : stage list
        private Dictionary<int, List<SpecCharacter>> characterDic = new (); // key : character_id, value : stage list
        private Dictionary<string, SpecGameConfig> configDic = new (); // key : config_key, value : game config data
        private Dictionary<long, List<SpecSkill>> skillDic = new (); // key : skill_id, value : skill list
        private Dictionary<long, List<SpecSkill>> skillPrefabIDDic = new (); // key : prefab_id, value : skill list
        private Dictionary<DialogueEventType, Dictionary<string, int>> dialogueHistoryDic = new (); // key1 : DialogueEventType, key2 : sub_key_value, value : dialogue_group_id
        private Dictionary<InGameVfxNameType, SpecInGameVfx> inGameVfxDic = new (); // key : inGameVfxName, value : SpecInGameVfx
        private Dictionary<CharacterPositionType, List<SpecSynergy>> positionSynergyDic = new (); // key : CharacterPositionType, value : SpecSynergy
        private Dictionary<ElementType, List<SpecSynergy>> elementSynergyDic = new (); // key : ElementType, value : SpecSynergy



        private void CustomizeSpecData()
        {
            # region SpecData Dictionary Cache
            // Language
            languageDic.Clear();
            foreach (SpecLanguage language in SpecLanguage.All)
            {
                if (!languageDic.ContainsKey(language.token_key))
                {
                    languageDic.Add(language.token_key, language);
                }
            }

            // Chapter
            chapterDic.Clear();
            chapterDifficultDic.Clear();
            foreach (SpecChapter chapter in SpecChapter.All)
            {
                if (!chapterDic.TryGetValue(chapter.chapter_id, out List<SpecChapter> chapterList))
                {
                    chapterList = new List<SpecChapter>();
                    chapterDic.Add(chapter.chapter_id, chapterList);
                }
                chapterList.Add(chapter);

                if (!chapterDifficultDic.TryGetValue(chapter.difficulty_type, out List<SpecChapter> chapterDifficultList))
                {
                    chapterDifficultList = new List<SpecChapter>();
                    chapterDifficultDic.Add(chapter.difficulty_type, chapterDifficultList);
                }
                chapterDifficultList.Add(chapter);
            }

            // Stage
            stageChapterDic.Clear();
            stageDifficultDic.Clear();
            foreach (SpecStage stage in SpecStage.All)
            {
                if (!stageChapterDic.TryGetValue(stage.chapter_id, out List<SpecStage> stageChapterList))
                {
                    stageChapterList = new List<SpecStage>();
                    stageChapterDic.Add(stage.chapter_id, stageChapterList);
                }
                stageChapterList.Add(stage);

                if (!stageDifficultDic.TryGetValue(stage.difficulty_type, out List<SpecStage> stageDiffcultList))
                {
                    stageDiffcultList = new List<SpecStage>();
                    stageDifficultDic.Add(stage.difficulty_type, stageDiffcultList);
                }
                stageDiffcultList.Add(stage);
            }

            // Stage Monster
            stageMonsterDic.Clear();
            foreach (SpecStageMonster stage in SpecStageMonster.All)
            {
                if (!stageMonsterDic.TryGetValue(stage.chapter_id, out List<SpecStageMonster> specStageMonster))
                {
                    specStageMonster = new List<SpecStageMonster>();
                    stageMonsterDic.Add(stage.chapter_id, specStageMonster);
                }

                specStageMonster.Add(stage);
            }

            // Stage Reward
            stageRewardDic.Clear();
            foreach (SpecStageReward stage in SpecStageReward.All)
            {
                if (!stageRewardDic.TryGetValue(stage.reward_id, out List<SpecStageReward> specStageReward))
                {
                    specStageReward = new List<SpecStageReward>();
                    stageRewardDic.Add(stage.reward_id, specStageReward);
                }

                specStageReward.Add(stage);
            }

            // Game Config
            configDic.Clear();
            foreach (SpecGameConfig config in SpecGameConfig.All)
            {
                if (!configDic.ContainsKey(config.config_key))
                {
                    configDic.Add(config.config_key, config);
                }
            }

            // Skill
            skillDic.Clear();
            skillPrefabIDDic.Clear();
            foreach (SpecSkill skill in SpecSkill.All)
            {
                // skillDic
                if (!skillDic.TryGetValue(skill.skill_id, out List<SpecSkill> skillList1))
                {
                    skillList1 = new List<SpecSkill>();
                    skillDic.Add(skill.skill_id, skillList1);
                }

                skillList1.Add(skill);

                // skillPrefabIDDic
                if (!skillPrefabIDDic.TryGetValue(skill.prefab_id, out List<SpecSkill> skillList2))
                {
                    skillList2 = new List<SpecSkill>();
                    skillPrefabIDDic.Add(skill.prefab_id, skillList2);
                }

                skillList2.Add(skill);
            }

            // Dialogue History
            dialogueHistoryDic.Clear();
            foreach (SpecDialogue dialogue in SpecDialogue.All)
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
            foreach (SpecCharacter character in SpecCharacter.All)
            {
                if (!characterDic.TryGetValue(character.character_id, out List<SpecCharacter> specCharacter))
                {
                    specCharacter = new List<SpecCharacter>();
                    characterDic.Add(character.character_id, specCharacter);
                }

                specCharacter.Add(character);
            }

            // InGameVfx
            inGameVfxDic.Clear();
            foreach (SpecInGameVfx inGameVfx in SpecInGameVfx.All)
            {
                if (!inGameVfxDic.ContainsKey(inGameVfx.vfx_name_type))
                {
                    inGameVfxDic.Add(inGameVfx.vfx_name_type, inGameVfx);
                }
            }

            // Element Synergy Dic
            elementSynergyDic.Clear();
            foreach (SpecSynergy synergy in SpecSynergy.All)
            {
                if (!elementSynergyDic.TryGetValue(synergy.element_type, out var list))
                {
                    list = new List<SpecSynergy>();
                    elementSynergyDic.Add(synergy.element_type, list);
                }

                list.Add(synergy);
            }

            // Position Synergy Dic
            positionSynergyDic.Clear();
            foreach (SpecSynergy synergy in SpecSynergy.All)
            {
                if (!positionSynergyDic.TryGetValue(synergy.character_position_type, out var list))
                {
                    list = new List<SpecSynergy>();
                    positionSynergyDic.Add(synergy.character_position_type, list);
                }

                list.Add(synergy);
            }
            #endregion


        }

        public string GetLanguageText(string tokenKey)
        {
            if (languageDic.TryGetValue(tokenKey, out SpecLanguage languageData))
            {
                return languageData.language_kr;
            }

            return string.Empty;
        }

        public T GetGameConfig<T>(string key)
        {
            if (!configDic.TryGetValue(key, out SpecGameConfig configData))
            {
                return default;
            }

            if (typeof(T) == typeof(int) && configData.config_value_type == ConfigValueType.INT) return int.Parse(configData.config_value).ConvertTo<T>();
            if (typeof(T) == typeof(float) && configData.config_value_type == ConfigValueType.FLOAT) return float.Parse(configData.config_value).ConvertTo<T>();
            if (typeof(T) == typeof(string) && configData.config_value_type == ConfigValueType.STRING) return configData.config_value.ConvertTo<T>();

            return configData.config_value.ConvertTo<T>();
        }

        public SpecCharacter GetCharacterData(int characterID)
        {
            return SpecCharacterList.Find(character => character.character_id == characterID);
        }

        public List<SpecCharacter> GetCharacterListByCharacterType(CharacterType type)
        {
            return SpecCharacterList.FindAll(character => character.character_type == type);
        }

        public int GetCharacterMaxLevel()
        {
            return SpecCharacterLevelExpList.Max(data => data.level);
        }

        public SpecCharacterLevelExp GetCharacterLevelExpData(int level)
        {
            return SpecCharacterLevelExpList.Find(data => data.level == level);
        }

        public List<SpecCharacterLevelExp> GetCharacterLevelExpDataList(int level)
        {
            return SpecCharacterLevelExpList.FindAll(data => data.level <= level);
        }

        public SpecCharacterTranscendence GetCharacterTranscendenceData(ElementType elementType, GradeType gradeType, int transcendenceLevel)
        {
            return SpecCharacterTranscendenceList.Find(data => data.element_type == elementType
                                                                    && data.grade_type == gradeType
                                                                    && data.transcendence_lv == transcendenceLevel);
        }

        public List<SpecCharacterTranscendence> GetCharacterTranscendenceDataList(ElementType elementType, GradeType gradeType)
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

        public SpecCharacterQuotes GetCharacterQuotesDataByPrefabID(int prefabID)
        {
            return SpecCharacterQuotesList.Find(data => data.prefab_id == prefabID);
        }

        public SpecChapter GetChapterData(int chapterID)
        {
            return SpecChapterList.Find(dat => dat.chapter_id == chapterID);
        }

        public SpecChapter GetChapterData(int chapterID, DifficultyType type)
        {
            if (chapterDic.TryGetValue(chapterID, out List<SpecChapter> chapterList))
            {
                return chapterList.Find(data => data.difficulty_type == type);
            }

            return null;
        }

        public SpecChapter GetChapterDataByStageID(int stageID)
        {
            var specStage = GetStageData(stageID);
            if (specStage != null)
            {
                if (chapterDic.TryGetValue(specStage.chapter_id, out List<SpecChapter> chapterList))
                {
                    return chapterList.Find(data => data.difficulty_type == specStage.difficulty_type);
                }
            }

            return null;
        }

        public List<SpecChapter> GetChapterList(int chapter)
        {
            if (chapterDic.TryGetValue(chapter, out List<SpecChapter> chapterList))
            {
                return chapterList;
            }

            return null;
        }

        public List<SpecChapter> GetChapterList(DifficultyType difficulty)
        {
            if (chapterDifficultDic.TryGetValue(difficulty, out List<SpecChapter> chapterList))
            {
                return chapterList;
            }

            return null;
        }

        public int GetTotalChapterStarCount(int chapterID, DifficultyType type)
        {
            int totalStarCount = 0;

            int stageStarCount = GetGameConfig<int>("max_stage_star_count");

            if (stageChapterDic.TryGetValue(chapterID, out List<SpecStage> stageList))
            {
                return stageList.FindAll(stage => stage.difficulty_type == type).Count * stageStarCount;
            }

            return totalStarCount;
        }

        public List<SpecDialogue> GetDialogueListByGroupID(int groupID)
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

        public SpecStage GetStageData(int stageID)
        {
            return SpecStageList.Find(data => data.stage_id == stageID);
        }

        public SpecStage GetStageData(int chapterID, int stageNumber, DifficultyType type)
        {
            if (stageChapterDic.TryGetValue(chapterID, out List<SpecStage> stageList))
            {
                return stageList.Find(stage => stage.stage_number == stageNumber && stage.difficulty_type == type);
            }

            return null;
        }

        public SpecStage GetStageData(int chapterID, DifficultyType difficultyType, StageType stageType)
        {
            if (stageChapterDic.TryGetValue(chapterID, out List<SpecStage> stageList))
            {
                return stageList.Find(stage => stage.difficulty_type == difficultyType && stage.stage_type == stageType);
            }

            return null;
        }


        public List<SpecStage> GetStageList(int chapter)
        {
            if (stageChapterDic.TryGetValue(chapter, out List<SpecStage> stageList))
            {
                return stageList;
            }

            return null;
        }

        public List<SpecStage> GetStageList(int chapter, DifficultyType difficulty)
        {
            if (stageChapterDic.TryGetValue(chapter, out List<SpecStage> stageList))
            {
                return stageList.FindAll(stage => stage.difficulty_type == difficulty);
            }

            return null;
        }

        public int GetStageCount(int chapter, DifficultyType difficulty)
        {
            if (stageChapterDic.TryGetValue(chapter, out List<SpecStage> stageList))
            {
                return stageList.FindAll(stage => stage.difficulty_type == difficulty).Count;
            }

            return 0;
        }

        // 해당 챕터의 마지막 스테이지 데이터 반환
        public SpecStage GetLastStageData(int chapterID, DifficultyType difficulty)
        {
            if (stageChapterDic.TryGetValue(chapterID, out List<SpecStage> stageList))
            {
                var targetStageList = stageList.FindAll(stage => stage.difficulty_type == difficulty);

                int maxStageNumber = targetStageList.Max(stage => stage.stage_number);
                return targetStageList.Find(stage => stage.stage_number == maxStageNumber);
            }

            return null;
        }

        // 가장 마지막 스테이지 데이터 반환
        public SpecStage GetEndStage()
        {
            var lastChapterData = SpecChapterList.Max(data => data.chapter_id);

            return GetLastStageData(lastChapterData, DifficultyType.NORMAL);
        }

        // 해당 스테이지 데이터 기준 다음 스테이지 정보 반환
        public SpecStage GetNextStageData(int targetStageID)
        {
            SpecStage resultData = null;

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
            SpecStage stageSpecData = GetStageData(stageID);
            SpecStage nextStageSpecData = GetStageData(stageSpecData.chapter_id, stageSpecData.stage_number + 1, stageSpecData.difficulty_type);

            return nextStageSpecData == null;
        }

        public SpecStageMonster GetStageMonsterData(int chapterID, int stageNumber, DifficultyType type)
        {
            if (stageMonsterDic.TryGetValue(chapterID, out List<SpecStageMonster> stageMonster))
            {
                return stageMonster.Find(s => s.stage_number == stageNumber &&  s.difficulty_type == type);
            }

            return null;
        }

        public List<SpecStageMonster> GetStageMonsterList(int chapter, int stageNumber, DifficultyType difficulty)
        {
            if (stageMonsterDic.TryGetValue(chapter, out List<SpecStageMonster> stageMonster))
            {
                return stageMonster.FindAll(s => s.stage_number == stageNumber &&  s.difficulty_type == difficulty);
            }

            return null;
        }

        // 해당 챕터에서 받을 수 있는 Idle 보상 리스트 반환 (해당 챕터 이하 리스트 모두 반환)
        public List<SpecIdleReward> GetAllIdleRewardList(int chapterID)
        {
            return SpecIdleRewardList.FindAll(data => data.chapter_id <= chapterID);
        }

        public List<SpecRewardInfo> GetSpecRewardInfoList(int rewardID)
        {
            return SpecRewardInfoList.FindAll(dataa => dataa.reward_id == rewardID);
        }

        // 보상 데이터 리스트 반환
        public List<SpecRewardInfo> GetSpecRewardInfoList(ContentType contentType, int contentKey, DifficultyType difficultyType)
        {
            return SpecRewardInfoList.FindAll(data => data.content_type == contentType
                                                      && data.content_key_value == contentKey
                                                      && data.difficulty_type == difficultyType);
        }

        // 시나리오 가챠 데이터 반환
        public List<SpecGachaScenario> GetGachaScenarioList(int currentCount, int gachaCount)
        {
            int maxCount = SpecGachaScenarioList.Count;
            int resultCount = currentCount + gachaCount > maxCount ? maxCount - currentCount : gachaCount;

            return SpecGachaScenarioList.GetRange(currentCount, resultCount);
        }

        // 시나리오 가챠 데이터를 RewardItem 리스트로 변환
        public List<RewardItem> GetRewardItemListByGachaScenarioList(List<SpecGachaScenario> gachaScenarioList)
        {
            List<RewardItem> rewardItemList = new List<RewardItem>();
            foreach (var gachaScenario in gachaScenarioList)
            {
                rewardItemList.Add(new RewardItem(gachaScenario.item_type, gachaScenario.item_key, gachaScenario.item_count));
            }

            return rewardItemList;
        }

        // 스테이지 보상 데이터를 RewardItem 리스트로 변환
        public List<RewardItem> GetRewardItemListByStageRewardList(List<SpecStageReward> stageRewardList)
        {
            List<RewardItem> rewardItemList = new List<RewardItem>();
            foreach (var stageReward in stageRewardList)
            {
                rewardItemList.Add(new RewardItem(stageReward.item_type, stageReward.item_key, stageReward.item_count));
            }

            return rewardItemList;
        }

        // 리워드 인포 데이터를 RewardItem 리스트로 변환
        public List<RewardItem> GetRewardItemListByRewadInfoList(List<SpecRewardInfo> rewardInfoList)
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

        public List<SpecSkill> GetSkillDataList(long skillID)
        {
            return skillDic.GetValueOrDefault(skillID);
        }

        public List<SpecSkill> GetSkillDataListByPrefabID(int prefabID)
        {
            return skillPrefabIDDic.GetValueOrDefault(prefabID);
        }

        public SpecSkill GetSkillData(int skillID, SkillValueType type)
        {
            return SpecSkillList.Find(data => data.skill_id == skillID && data.skill_value_type == type);
        }

        public List<SpecCommanderSkill> GetCommanderSkillList(int chapterID)
        {
            return SpecCommanderSkillList.FindAll(data => data.open_key_chapter_id <= chapterID);
        }

        public int GetFirstCommanderSkillChapter()
        {
            int openChapterID = SpecCommanderSkillList.Min(data => data.open_key_chapter_id) - 1;
            return stageChapterDic[openChapterID].Last().stage_id;
        }

        public SpecCommanderSkill GetCommanderSkillData(int skillID)
        {
            return SpecCommanderSkillList.FirstOrDefault(data => data.commander_skill_id == skillID);
        }

        public SpecCommanderSkill GetCommanderSkillData(int commanderSkillID, SkillValueType type)
        {
            return SpecCommanderSkillList.Find(data => data.commander_skill_id == commanderSkillID && data.skill_value_type == type);
        }

        public SpecCharacter GetSpecCharacter(int characterID)
        {
            return SpecCharacterList.FirstOrDefault(data => data.character_id == characterID);
        }
        public SpecAccountLevelExp GetAccountLevelExpDataByLevel(int level)
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
            if (exp >= SpecAccountLevelExp.Get(maxLevel).exp_last)
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

        public SpecGuideMission GetGuideMissionDataByOrder(int order)
        {
            return SpecGuideMissionList.Find(data => data.order == order);
        }

        // 가이드 미션 order 최대치 반환
        public int GetGuideMissionMaxOrder()
        {
            return SpecGuideMissionList.Max(guide => guide.order);
        }

        public List<SpecStageReward> GetSpecStageReward(int rewardID)
        {
            if (stageRewardDic.TryGetValue(rewardID, out List<SpecStageReward> stageRewardList))
            {
                return stageRewardList;
            }

            return null;
        }

        public SpecInGameVfx GetInGameVfxData(InGameVfxNameType vfxNameType)
        {
            return inGameVfxDic.GetValueOrDefault(vfxNameType);
        }

        public List<SpecSynergy> GetSpecSynergyList(ElementType elementType)
        {
            if (elementSynergyDic.TryGetValue(elementType, out List<SpecSynergy> synergyList))
            {
                return synergyList;
            }

            return null;
        }

        public List<SpecSynergy> GetSpecSynergyList(CharacterPositionType positionType)
        {
            if (positionSynergyDic.TryGetValue(positionType, out List<SpecSynergy> synergyList))
            {
                return synergyList;
            }

            return null;
        }

        public SpecQuest GetSpecQuestData(int questID)
        {
            return SpecQuestList.Find(data => data.quest_id == questID);
        }

        public List<SpecQuest> GetSpecQuestList(TermType termType, bool isIncludeMilestone)
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

        public List<SpecQuest> GetSpecQuestList(QuestType questType)
        {
            return SpecQuestList.FindAll(data => data.quest_type == questType);
        }

        public List<SpecQuest> GetSpecQuestList(TermType termType, QuestType questType)
        {
            return SpecQuestList.FindAll(data => data.term_type == termType && data.quest_type == questType);
        }

        public SpecEvent GetSpecEventData(int eventID)
        {
            return SpecEventList.Find(data => data.event_id == eventID);
        }

        public SpecEvent GetSpecEventData(EventType eventType)
        {
            return SpecEventList.Find(data => data.event_type == eventType);
        }

        public List<SpecEvent> GetSpecEventList(EventType eventType)
        {
            return SpecEventList.FindAll(data => data.event_type == eventType);
        }

        public List<SpecEvent> GetSpecEventList(TermType termType)
        {
            return SpecEventList.FindAll(data => data.term_type == termType);
        }

        // 기간 제한이 존재하는 이벤트 리스트를 반환
        public List<SpecEvent> GetLimitedSpecEventList()
        {
            return SpecEventList.FindAll(data => data.frequency_type == FrequencyType.ONCE);
        }

        // 기간 제한이 존재하지 않는 이벤트 리스트를 반환 (서비스 중 기간동안 지속 반복)
        public List<SpecEvent> GetNoneLimitedSpecEventList()
        {
            return SpecEventList.FindAll(data => data.frequency_type == FrequencyType.REPEAT);
        }

        // 현재 시간 기준, 운영 기간에 해당하는 이벤트 데이터를 반환
        public SpecEvent GetCurrentSpecEvent(EventType eventType)
        {
            var eventList = GetSpecEventList(eventType);

            foreach (var eventData in eventList)
            {
                var startAtTimeStamp = TimeManager.Instance.ChangeDateStringToTimeStamp(eventData.start_at);
                var endAtTimeStamp = TimeManager.Instance.ChangeDateStringToTimeStamp(eventData.end_at);

                var nowTimeStamp = TimeManager.Instance.UtcNowTimeStamp();

                if (startAtTimeStamp <= nowTimeStamp && nowTimeStamp <= endAtTimeStamp)
                {
                    return eventData;
                }
            }

            return null;
        }

        // 현재 시간 기준, 운영 기간에 해당하는 이벤트 데이터 리스트를 반환
        public List<SpecEvent> GetCurrentSpecEventList()
        {
            List<SpecEvent> resultEventList = new List<SpecEvent>();

            // 기간 제한이 없는 이벤트 데이터 처리
            List<SpecEvent> noneLimitedSpecEventList = GetNoneLimitedSpecEventList();
            if (noneLimitedSpecEventList != null && noneLimitedSpecEventList.Count > 0)
            {
                resultEventList.AddRange(noneLimitedSpecEventList);
            }

            // 기간 제한이 있는 이벤트 데이터 처리
            List<SpecEvent> limitedSpecEventList = GetLimitedSpecEventList();
            foreach (var eventData in limitedSpecEventList)
            {
                var startAtTimeStamp = TimeManager.Instance.ChangeDateStringToTimeStamp(eventData.start_at);
                var endAtTimeStamp = TimeManager.Instance.ChangeDateStringToTimeStamp(eventData.end_at);

                var nowTimeStamp = TimeManager.Instance.UtcNowTimeStamp();

                if (startAtTimeStamp <= nowTimeStamp && nowTimeStamp <= endAtTimeStamp)
                {
                    resultEventList.Add(eventData);
                }
            }

            return resultEventList;
        }

        public SpecEventCondition GetSpecEventConditionData(int eventID, int eventConditionID)
        {
            return SpecEventConditionList.Find(data => data.event_id == eventID && data.event_condition_id == eventConditionID);
        }

        public List<SpecEventCondition> GetSpecEventConditionList(int eventID)
        {
            return SpecEventConditionList.FindAll(data => data.event_id == eventID);
        }

        public SpecDungeonTrial GetSpecDungeonTrialData(int dungeonID)
        {
            return SpecDungeonTrialList.Find(data => data.dungeon_id == dungeonID);
        }

        public SpecDungeonTrial GetSpecDungeonTrialDataByOrder(int order)
        {
            return SpecDungeonTrialList.Find(data => data.order == order);
        }

        public List<SpecDungeonTrial> GetSpecDungeonTrialDataList(DungeonType dungeonType)
        {
            return SpecDungeonTrialList.FindAll(data => data.dungeon_type == dungeonType);
        }

        public List<SpecDungeonMonster> GetSpecDungeonMonsterDataList(DungeonType dungeonType, int dungeonID)
        {
            return SpecDungeonMonsterList
                .FindAll(data => data.dungeon_type == dungeonType && data.dungeon_id == dungeonID);
        }

        public List<SpecDungeonReward> GetSpecDungeonRewardDataList(DungeonType dungeonType, int dungeonID)
        {
            return SpecDungeonRewardList
                .FindAll(data => data.dungeon_type == dungeonType && data.dungeon_id == dungeonID);
        }

        #region PVP

        public SpecPVPTier GetPVPTierData(int ranking_id)
        {
            return SpecPVPTierList.Find(data => data.ranking_id == ranking_id);
        }

        public List<SpecPVPTier> GetPVPTierDataList(RankingType rankingType)
        {
            return SpecPVPTierList.FindAll(data => data.ranking_type == rankingType);
        }

        public SpecPVPRanking GetPVPRankingData(int ranking)
        {
            return SpecPVPRankingList.Find(data => data.rank_range_start <= ranking && data.rank_range_end <= ranking);
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
    }
}
