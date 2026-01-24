#define USE_SERVER_SPEC


using System.Collections.Generic;
using BiniLab;
using CookApps.BattleSystem;
using CookApps.TeamBattle;
using Cysharp.Threading.Tasks;
using Tech.Hive.V1;
using Unity.VisualScripting;
#if USE_SERVER_SPEC
using CookApps.LocalData;
#endif

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
                    json = await NetManager.Instance.Spec.GetSpecJsonAsync(SpecType.Game, serverSpecVersion);
                } while (string.IsNullOrEmpty(json));

                localData.Save(json, "SpecData");
            }
            else
            {
                if (!localData.TryLoad("SpecData", out json))
                {
                    do
                    {
                        json = await NetManager.Instance.Spec.GetSpecJsonAsync(SpecType.Game, serverSpecVersion);
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
            NetManager.Instance.Spec.CurrentGameSpecVersion = NetManager.Instance.Spec.GetCachedSpecVersion(SpecType.Game);
            await UniTask.Yield();
            GenerateCacheSpecData();
            CustomizeSpecData();
            int languageCount = Language.All.Count;
            Debug.Log(languageCount);
        }

        // SpecData Dictionary Cache Data
        // languageDic 제거됨 - Unity Localization으로 대체 (LocalizationLoader 사용)
        private Dictionary<int, List<RewardItem>> chestDic = new();                                // key : chest_id, value : chest list
        private Dictionary<int, List<ChapterInfo>> chapterDic = new();                             // key : chapter_id, value : chapter list
        private Dictionary<DifficultyType, List<ChapterInfo>> chapterDifficultDic = new();         // key : DifficultyType, value : chapter list
        private Dictionary<int, List<StageInfo>> stageChapterDic = new();                          // key : chapter_id, value : stage list
        private Dictionary<DifficultyType, List<StageInfo>> stageDifficultDic = new();             // key : DifficultyType, value : stage list
        private Dictionary<int, List<StageMonster>> stageMonsterDic = new();                       // key : chapter_id, value : stage list
        private Dictionary<int, List<StageReward>> stageRewardDic = new();                         // key : reward_id, value : stage list
        private Dictionary<string, ConfigGame> configDic = new();                                  // key : config_key, value : game config data
        private Dictionary<long, List<SkillActive>> skillDic = new();                              // key : skill_group_id, value : skill list
        private Dictionary<long, List<SkillActive>> skillPrefabIDDic = new();                      // key : prefab_id, value : skill list
        private Dictionary<long, List<SkillPassive>> skillPassiveDic = new();                      // key : passive_group_id, value : SkillPassive
        private Dictionary<long, List<SkillPassive>> skillPassivePrefabIDDic = new();          // key : equipment_id, value : SkillPassive
        private Dictionary<DialogueEventType, Dictionary<string, int>> dialogueHistoryDic = new(); // key1 : DialogueEventType, key2 : sub_key_value, value : dialogue_group_id
        private Dictionary<InGameVfxNameType, InGameVfxMap> inGameVfxDic = new();                             // key : inGameVfxName, value : SpecInGameVfx
        private Dictionary<SynergyType, List<ISpecSynergyData>> synergyDic = new();                // key : SynergyType, value : ISpecSynergyData
        private Dictionary<EffectCodeNameType, List<SkillJob>> skillJobDic = new();             // key : EffectCodeNameType, value : SkillJob
        private Dictionary<int, List<SkillCommander>> commanderSkillDic = new();                   // key : commander_skill_id, value : SpecCommanderSkill
        private Dictionary<int, ISpecItemInfo> itemTableKeyMap = new();                      // key : item_id (currency_id/item_id), value : ISpecItemInfo
        private Dictionary<string, DialogueLanguage> dialogueLanguageDic = new();           // key : text_desc_token, value : DialogueLanguage
        private Dictionary<int, List<TutorialDialogue>> tutorialDialogueDic = new();           // key : tutorial_id, value : TutorialDialogue list
        private bool isItemTableKeyMapInitialized = false;

        private void CustomizeSpecData()
        {
            # region SpecData Dictionary Cache
            // Language - Unity Localization으로 대체됨 (LocalizationLoader 사용)

            // DialogueLanguage
            dialogueLanguageDic.Clear();
            for (int i = 0; i < DialogueLanguage.All.Count; i++)
            {
                var dialogueLanguage = DialogueLanguage.All[i];
                if (!dialogueLanguageDic.ContainsKey(dialogueLanguage.text_desc_token))
                {
                    dialogueLanguageDic.Add(dialogueLanguage.text_desc_token, dialogueLanguage);
                }
            }

            // Chapter
            chapterDic.Clear();
            chapterDifficultDic.Clear();
            for (int i = 0; i < ChapterInfo.All.Count; i++)
            {
                var chapter = ChapterInfo.All[i];
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
            for (int i = 0; i < StageInfo.All.Count; i++)
            {
                var stage = StageInfo.All[i];
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
            for (int i = 0; i < StageMonster.All.Count; i++)
            {
                var stage = StageMonster.All[i];
                if (!stageMonsterDic.TryGetValue(stage.chapter_id, out List<StageMonster> specStageMonster))
                {
                    specStageMonster = new List<StageMonster>();
                    stageMonsterDic.Add(stage.chapter_id, specStageMonster);
                }

                specStageMonster.Add(stage);
            }

            // Stage Reward
            stageRewardDic.Clear();
            for (int i = 0; i < StageReward.All.Count; i++)
            {
                var stage = StageReward.All[i];
                if (!stageRewardDic.TryGetValue(stage.reward_id, out List<StageReward> specStageReward))
                {
                    specStageReward = new List<StageReward>();
                    stageRewardDic.Add(stage.reward_id, specStageReward);
                }

                specStageReward.Add(stage);
            }

            // Game Config
            configDic.Clear();
            for (int i = 0; i < ConfigGame.All.Count; i++)
            {
                var config = ConfigGame.All[i];
                if (!configDic.ContainsKey(config.config_key))
                {
                    configDic.Add(config.config_key, config);
                }
            }

            // Skill
            skillDic.Clear();
            skillPrefabIDDic.Clear();
            for (int i = 0; i < SkillActive.All.Count; i++)
            {
                var skill = SkillActive.All[i];
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

            // Skill Passive
            skillPassiveDic.Clear();
            skillPassivePrefabIDDic.Clear();
            for (int i = 0; i < SkillPassive.All.Count; i++)
            {
                var skillPassive = SkillPassive.All[i];
                if (!skillPassiveDic.TryGetValue(skillPassive.passive_group_id, out List<SkillPassive> skillPassiveList))
                {
                    skillPassiveList = new List<SkillPassive>();
                    skillPassiveDic.Add(skillPassive.passive_group_id, skillPassiveList);
                }
                skillPassiveList.Add(skillPassive);

                if (!skillPassivePrefabIDDic.TryGetValue(skillPassive.prefab_id, out List<SkillPassive> skillPassiveList2))
                {
                    skillPassiveList2 = new List<SkillPassive>();
                    skillPassivePrefabIDDic.Add(skillPassive.prefab_id, skillPassiveList2);
                }
                skillPassiveList2.Add(skillPassive);
            }

            // Dialogue History
            dialogueHistoryDic.Clear();
            for (int i = 0; i < DialogueLanguage.All.Count; i++)
            {
                var dialogue = DialogueLanguage.All[i];
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

            // InGameVfx
            inGameVfxDic.Clear();
            for (int i = 0; i < InGameVfxMap.All.Count; i++)
            {
                var inGameVfx = InGameVfxMap.All[i];
                if (!inGameVfxDic.ContainsKey(inGameVfx.vfx_name_type))
                {
                    inGameVfxDic.Add(inGameVfx.vfx_name_type, inGameVfx);
                }
            }

            // synergyElementDic Dic
            synergyDic.Clear();

            // SynergyElemental과 SynergyStarAsterism을 통합 처리하며, 처음부터 필터링
            for (int i = 0; i < SynergyElemental.All.Count; i++)
            {
                var synergy = SynergyElemental.All[i];
                ISpecSynergyData synergyData = synergy;
                // 유효한 시너지만 추가 (모든 effect_value_type이 NONE이 아닌 경우)
                if (synergyData.effect_value_type_1 != SkillValueType.NONE ||
                    synergyData.effect_value_type_2 != SkillValueType.NONE ||
                    synergyData.effect_value_type_3 != SkillValueType.NONE)
                {
                    if (!synergyDic.TryGetValue(synergy.synergy_type, out var list))
                    {
                        list = new List<ISpecSynergyData>();
                        synergyDic[synergy.synergy_type] = list;
                    }
                    list.Add(synergyData);
                }
            }

            for (int i = 0; i < SynergyStarAsterism.All.Count; i++)
            {
                var synergy = SynergyStarAsterism.All[i];
                ISpecSynergyData synergyData = synergy;
                // 유효한 시너지만 추가 (모든 effect_value_type이 NONE이 아닌 경우)
                if (synergyData.effect_value_type_1 != SkillValueType.NONE ||
                    synergyData.effect_value_type_2 != SkillValueType.NONE ||
                    synergyData.effect_value_type_3 != SkillValueType.NONE)
                {
                    if (!synergyDic.TryGetValue(synergy.synergy_type, out var list))
                    {
                        list = new List<ISpecSynergyData>();
                        synergyDic[synergy.synergy_type] = list;
                    }
                    list.Add(synergyData);
                }
            }


            // skillJobDic Dic
            skillJobDic.Clear();
            for (int i = 0; i < SkillJob.All.Count; i++)
            {
                var skillJob = SkillJob.All[i];
                if (!skillJobDic.TryGetValue(skillJob.passive_skill_type, out var list))
                {
                    list = new List<SkillJob>();
                    skillJobDic.Add(skillJob.passive_skill_type, list);
                }
                list.Add(skillJob);
            }

            // Commander Skill Dic
            commanderSkillDic.Clear();
            for (int i = 0; i < SkillCommander.All.Count; i++)
            {
                var commanderSkill = SkillCommander.All[i];
                if (!commanderSkillDic.TryGetValue(commanderSkill.commander_skill_id, out var list))
                {
                    list = new List<SkillCommander>();
                    commanderSkillDic.Add(commanderSkill.commander_skill_id, list);
                }
                list.Add(commanderSkill);
            }

            #endregion

            // TutorialDialogue
            tutorialDialogueDic.Clear();
            for (int i = 0; i < TutorialDialogue.All.Count; i++)
            {
                var tutorialDialogue = TutorialDialogue.All[i];
                if (!tutorialDialogueDic.TryGetValue(tutorialDialogue.tutorial_id, out var list))
                {
                    list = new List<TutorialDialogue>();
                    tutorialDialogueDic.Add(tutorialDialogue.tutorial_id, list);
                }
                list.Add(tutorialDialogue);
            }
        }

        private void InitializeItemTableKeyMap()
        {
            if (isItemTableKeyMapInitialized) return;

            itemTableKeyMap.Clear();

            for (int i = 0; i < ItemCurrencyTable.All.Count; i++)
            {
                var item = ItemCurrencyTable.All[i];
                if (!itemTableKeyMap.ContainsKey(item.currency_id))
                {
                    itemTableKeyMap.Add(item.currency_id, item);
                }
            }

            for (int i = 0; i < ItemConsumableTable.All.Count; i++)
            {
                var item = ItemConsumableTable.All[i];
                if (!itemTableKeyMap.ContainsKey(item.item_id))
                {
                    itemTableKeyMap.Add(item.item_id, item);
                }
            }

            for (int i = 0; i < ItemMaterialTable.All.Count; i++)
            {
                var item = ItemMaterialTable.All[i];
                if (!itemTableKeyMap.ContainsKey(item.item_id))
                {
                    itemTableKeyMap.Add(item.item_id, item);
                }
            }

            isItemTableKeyMapInitialized = true;
        }

        /// <summary>
        /// [Deprecated] Unity Localization으로 대체됨
        /// 기존 호환성을 위해 LanguageManager로 위임
        /// </summary>
        public string GetDefaultText(string tokenKey, LanguageType targetLanguageType)
        {
            return LanguageManager.Instance.GetDefaultText(tokenKey);
        }

        /// <summary>
        /// [Deprecated] Unity Localization으로 대체됨
        /// 기존 호환성을 위해 LanguageManager로 위임 (Dialogue 테이블 사용)
        /// </summary>
        public string GetDialogueText(string tokenKey, LanguageType targetLanguageType)
        {
            return LanguageManager.Instance.GetDialogueText(tokenKey);
        }

        public T GetGameConfig<T>(string key)
        {
            if (!configDic.TryGetValue(key, out var configData))
            {
                return default;
            }

            if (typeof(T) == typeof(int) && configData.config_value_type == ConfigValueType.INT)
                return int.Parse(configData.config_value).ConvertTo<T>();
            if (typeof(T) == typeof(float) && configData.config_value_type == ConfigValueType.FLOAT)
                return float.Parse(configData.config_value).ConvertTo<T>();
            if (typeof(T) == typeof(string) && configData.config_value_type == ConfigValueType.STRING)
                return configData.config_value.ConvertTo<T>();

            return configData.config_value.ConvertTo<T>();
        }

        public CharacterInfo GetCharacterData(int characterId)
        {
            return CharacterInfo.Get(characterId);
        }

        public List<CharacterInfo> GetCharacterListByCharacterType(CharacterType type)
        {
            var result = new List<CharacterInfo>();
            for (int i = 0; i < CharacterInfo.All.Count; i++)
            {
                var character = CharacterInfo.All[i];
                if (character.character_type == type)
                    result.Add(character);
            }
            return result;
        }

        public CharacterLevelExp GetCharacterNextExceedLevelExpData(uint exceedCount)
        {
            for (int i = 0; i < CharacterLevelExp.All.Count; i++)
            {
                var data = CharacterLevelExp.All[i];
                if (exceedCount == 0 && data.IsExceed)
                {
                    return data;
                }

                if (data.IsExceed)
                    exceedCount--;
            }

            return null;
        }

        public CharacterLevelExp GetCharacterLevelExpData(int level)
        {
            for (int i = 0; i < CharacterLevelExp.All.Count; i++)
            {
                var data = CharacterLevelExp.All[i];
                if (data.level == level)
                    return data;
            }
            return null;
        }

        public List<CharacterLevelExp> GetCharacterLevelExpDataList(int level)
        {
            var result = new List<CharacterLevelExp>();
            for (int i = 0; i < CharacterLevelExp.All.Count; i++)
            {
                var data = CharacterLevelExp.All[i];
                if (data.level <= level)
                    result.Add(data);
            }
            return result;
        }

        public CharacterTranscendence GetCharacterTranscendenceData(GradeType gradeType, int star)
        {
            for (int i = 0; i < CharacterTranscendence.All.Count; i++)
            {
                var data = CharacterTranscendence.All[i];
                if (data.grade_type == gradeType
                    && data.star == star)
                    return data;
            }
            return null;
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
                // ItemType의 삭제로 인해 변경.(new RewardItem(ItemType.GOLD, 0, levelExpData.need_gold_sum))
                RewardItem needGoldItem = new RewardItem(IdMap.Item.Gold, levelExpData.need_gold_sum);
                resultItemList.Add(needGoldItem);

                // ItemType의 삭제로 인해 변경.(new RewardItem(ItemType.CHAR_USER_EXP_ITEM, 0, levelExpData.base_levelup_item_sum))
                RewardItem needExpItem = new RewardItem(IdMap.Item.CharExp, levelExpData.base_levelup_item_sum);
                resultItemList.Add(needExpItem);

                if (levelExpData.sec_levelup_item_sum > 0)
                {
                    // ItemType의 삭제로 인해 변경.(new RewardItem(levelExpData.sec_levelup_item_type, characterID, levelExpData.sec_levelup_item_sum))
                    RewardItem needSecondLevelupItem = new RewardItem(characterID, levelExpData.sec_levelup_item_sum);
                    resultItemList.Add(needSecondLevelupItem);
                }
            }

            return resultItemList;
        }

        public CharacterQuotes GetCharacterQuotesDataByPrefabID(int prefabID)
        {
            for (int i = 0; i < CharacterQuotes.All.Count; i++)
            {
                var data = CharacterQuotes.All[i];
                if (data.prefab_id == prefabID)
                    return data;
            }
            return null;
        }

        public ChapterInfo GetChapterData(int chapterID)
        {
            for (int i = 0; i < ChapterInfo.All.Count; i++)
            {
                var chapter = ChapterInfo.All[i];
                if (chapter.chapter_id == chapterID)
                    return chapter;
            }
            return null;
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
                    foreach (var data in chapterList)
                    {
                        if (data.difficulty_type == specStage.difficulty_type)
                            return data;
                    }
                    return null;
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
                int count = 0;
                foreach (var stage in stageList)
                {
                    if (stage.difficulty_type == type)
                        count++;
                }
                return count * stageStarCount;
            }

            return totalStarCount;
        }

        public List<DialogueLanguage> GetDialogueListByGroupID(int groupID)
        {
            var result = new List<DialogueLanguage>();
            for (int i = 0; i < DialogueLanguage.All.Count; i++)
            {
                var data = DialogueLanguage.All[i];
                if (data.dialouge_group_id == groupID)
                    result.Add(data);
            }
            return result;
        }

        public List<TutorialDialogue> GetTutorialDialogueList(int tutorialID)
        {
            if (tutorialDialogueDic.TryGetValue(tutorialID, out List<TutorialDialogue> tutorialDialogueList))
            {
                return tutorialDialogueList;
            }

            return null;
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
            for (int i = 0; i < StageInfo.All.Count; i++)
            {
                var stage = StageInfo.All[i];
                if (stage.stage_id == stageID)
                    return stage;
            }
            return null;
        }

        public StageInfo GetStageData(int chapterID, int stageNumber, DifficultyType type)
        {
            if (stageChapterDic.TryGetValue(chapterID, out List<StageInfo> stageList))
            {
                foreach (var stage in stageList)
                {
                    if (stage.stage_number == stageNumber && stage.difficulty_type == type)
                        return stage;
                }
                return null;
            }

            return null;
        }

        public StageInfo GetStageData(int chapterID, DifficultyType difficultyType, StageType stageType)
        {
            if (stageChapterDic.TryGetValue(chapterID, out List<StageInfo> stageList))
            {
                foreach (var stage in stageList)
                {
                    if (stage.difficulty_type == difficultyType && stage.stage_type == stageType)
                        return stage;
                }
                return null;
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
                var result = new List<StageInfo>();
                foreach (var stage in stageList)
                {
                    if (stage.difficulty_type == difficulty)
                        result.Add(stage);
                }
                return result;
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
                int count = 0;
                foreach (var stage in stageList)
                {
                    if (stage.difficulty_type == difficulty)
                        count++;
                }
                return count;
            }

            return 0;
        }

        // 해당 챕터의 마지막 스테이지 데이터 반환
        public StageInfo GetLastStageData(int chapterID, DifficultyType difficulty)
        {
            Debug.LogColor($"GetLastStageData chapterID: {chapterID}, difficulty: {difficulty}");
            if (stageChapterDic.TryGetValue(chapterID, out List<StageInfo> stageList))
            {
                var targetStageList = new List<StageInfo>();
                foreach (var stage in stageList)
                {
                    if (stage.difficulty_type == difficulty)
                        targetStageList.Add(stage);
                }

                int maxStageNumber = 0;
                foreach (var stage in targetStageList)
                {
                    if (stage.stage_number > maxStageNumber)
                        maxStageNumber = stage.stage_number;
                }

                foreach (var stage in targetStageList)
                {
                    if (stage.stage_number == maxStageNumber)
                        return stage;
                }
            }

            return null;
        }

        // 가장 마지막 스테이지 데이터 반환
        public StageInfo GetEndStage()
        {
            int lastChapterData = 0;
            for (int i = 0; i < ChapterInfo.All.Count; i++)
            {
                var chapter = ChapterInfo.All[i];
                if (chapter.chapter_id > lastChapterData)
                    lastChapterData = chapter.chapter_id;
            }

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
                foreach (var s in stageMonster)
                {
                    if (s.stage_number == stageNumber && s.difficulty_type == type)
                        return s;
                }
            }

            return null;
        }

        public List<StageMonster> GetStageMonsterList(int chapter, int stageNumber, DifficultyType difficulty)
        {
            if (stageMonsterDic.TryGetValue(chapter, out List<StageMonster> stageMonster))
            {
                var result = new List<StageMonster>();
                foreach (var s in stageMonster)
                {
                    if (s.stage_number == stageNumber && s.difficulty_type == difficulty)
                        result.Add(s);
                }
                return result;
            }

            return null;
        }

        // 해당 챕터에서 받을 수 있는 Idle 보상 리스트 반환 (해당 챕터 이하 리스트 모두 반환)
        public List<IdleReward> GetAllIdleRewardList(int chapterID)
        {
            var result = new List<IdleReward>();
            for (int i = 0; i < IdleReward.All.Count; i++)
            {
                var data = IdleReward.All[i];
                if (data.chapter_id <= chapterID)
                    result.Add(data);
            }
            return result;
        }

        public List<RewardInfo> GetSpecRewardInfoList(int rewardID)
        {
            var result = new List<RewardInfo>();
            for (int i = 0; i < RewardInfo.All.Count; i++)
            {
                var data = RewardInfo.All[i];
                if (data.reward_id == rewardID)
                    result.Add(data);
            }
            return result;
        }

        public List<StageMilestoneReward> GetStageMilestoneRewardList(ContentType contentType, int stageNumber, DifficultyType difficulty)
        {
            var result = new List<StageMilestoneReward>();
            foreach (var data in StageMilestoneReward.All)
            {
                if (data.content_type == contentType && data.content_key_value == stageNumber && data.difficulty_type == difficulty)
                    result.Add(data);
            }

            return result;
        }

        // 보상 데이터 리스트 반환
        public List<RewardInfo> GetSpecRewardInfoList(ContentType contentType, int contentKey, DifficultyType difficultyType)
        {
            var result = new List<RewardInfo>();
            for (int i = 0; i < RewardInfo.All.Count; i++)
            {
                var data = RewardInfo.All[i];
                if (data.content_type == contentType
                    && data.content_key_value == contentKey
                    && data.difficulty_type == difficultyType)
                    result.Add(data);
            }
            return result;
        }

        // 스테이지 보상 데이터를 RewardItem 리스트로 변환
        public List<RewardItem> GetRewardItemListByStageRewardList(List<StageReward> stageRewardList)
        {
            List<RewardItem> rewardItemList = new List<RewardItem>();
            foreach (var stageReward in stageRewardList)
            {
                // ItemType의 삭제로 인해 변경.(new RewardItem(stageReward.item_type, stageReward.item_key, stageReward.item_count))
                rewardItemList.Add(new RewardItem(stageReward.item_id, stageReward.item_count));
            }

            return rewardItemList;
        }

        // 리워드 인포 데이터를 RewardItem 리스트로 변환
        public List<RewardItem> GetRewardItemListByRewadInfoList(List<RewardInfo> rewardInfoList)
        {
            List<RewardItem> rewardItemList = new List<RewardItem>();
            foreach (var rewardInfo in rewardInfoList)
            {
                // ItemType의 삭제로 인해 변경.(new RewardItem(rewardInfo.item_type, rewardInfo.item_key, rewardInfo.item_count))
                rewardItemList.Add(new RewardItem(rewardInfo.item_id, rewardInfo.item_count));
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

        public List<SkillPassive> GetSkillPassiveDataList(long passiveSkillID)
        {
            return skillPassiveDic.GetValueOrDefault(passiveSkillID);
        }

        public List<SkillActive> GetSkillDataListByPrefabID(int prefabID)
        {
            return skillPrefabIDDic.GetValueOrDefault(prefabID);
        }

        public SkillActive GetSkillData(int skillID, SkillValueType type)
        {
            for (int i = 0; i < SkillActive.All.Count; i++)
            {
                var data = SkillActive.All[i];
                if (data.skill_group_id == skillID && data.skill_value_type == type)
                    return data;
            }
            return null;
        }

        public List<SkillPassive> GetSkillPassiveDataList(int passiveGroupID)
        {
            return skillPassiveDic.GetValueOrDefault(passiveGroupID);
        }

        public List<SkillPassive> GetSkillPassiveDataListByPrefabID(int prefabID)
        {
            return skillPassivePrefabIDDic.GetValueOrDefault(prefabID);
        }

        public SkillPassive GetSkillPassiveData(int passiveGroupID, SkillValueType type)
        {
            for (int i = 0; i < SkillPassive.All.Count; i++)
            {
                var data = SkillPassive.All[i];
                if (data.passive_group_id == passiveGroupID && data.passive_value_type == type)
                    return data;
            }
            return null;
        }

        public List<SkillCommander> GetCommanderSkillList(int chapterID)
        {
            var result = new List<SkillCommander>();
            for (int i = 0; i < SkillCommander.All.Count; i++)
            {
                var data = SkillCommander.All[i];
                if (data.open_key_chapter_id == chapterID)
                    result.Add(data);
            }
            return result;
        }

        public List<SkillCommander> GetCommanderSkillIncludeList(int chapterID)
        {
            var result = new List<SkillCommander>();
            for (int i = 0; i < SkillCommander.All.Count; i++)
            {
                var data = SkillCommander.All[i];
                if (data.open_key_chapter_id <= chapterID)
                    result.Add(data);
            }
            return result;
        }

        public int GetFirstCommanderSkillChapter()
        {
            int minChapterID = int.MaxValue;
            for (int i = 0; i < SkillCommander.All.Count; i++)
            {
                var data = SkillCommander.All[i];
                if (data.open_key_chapter_id < minChapterID)
                    minChapterID = data.open_key_chapter_id;
            }
            int openChapterID = minChapterID - 1;

            if (stageChapterDic.TryGetValue(2, out List<StageInfo> stageList) && stageList.Count > 0)
            {
                return stageList[stageList.Count - 1].stage_id;
            }
            return 0;
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
            for (int i = 0; i < ChapterRule.All.Count; i++)
            {
                var data = ChapterRule.All[i];
                if ((int)data.chapter_rule_effect_code_type == chapterRuleID)
                    return data;
            }
            return null;
        }

        public ISpecItemInfo GetSpecItemData(int itemId)
        {
            InitializeItemTableKeyMap();
            return itemTableKeyMap.GetValueOrDefault(itemId);
        }

        public ISpecCharacterInfo GetSpecCharacter(int characterID)
        {
            var character = CharacterInfo.Get(characterID);
            if (character != null)
                return character;

            var monster = MonsterInfo.Get(characterID);
            if (monster != null)
                return monster;

            var battleItem = BattleItem.Get(characterID);
            if (battleItem != null)
                return battleItem;
            // outCharacterInfo = obstacleDic.GetValueOrDefault(characterID);
            // if (outCharacterInfo != null)
            // {
            //     return outCharacterInfo;
            // }

            return null;
        }

        public int GetLeftCharacterID(int characterID, CharacterType characterType)
        {
            var targetCharacterList = new List<CharacterInfo>();
            for (int i = 0; i < CharacterInfo.All.Count; i++)
            {
                var c = CharacterInfo.All[i];
                if (c.character_type == characterType)
                    targetCharacterList.Add(c);
            }

            if (targetCharacterList.Count == 0) return characterID;

            int idx = -1;
            for (int i = 0; i < targetCharacterList.Count; i++)
            {
                if (targetCharacterList[i].id == characterID)
                {
                    idx = i;
                    break;
                }
            }

            if (idx < 0)
                return targetCharacterList[0].id; // 못 찾으면 첫 번째로

            int leftIdx = (idx == 0) ? targetCharacterList.Count - 1 : idx - 1;
            return targetCharacterList[leftIdx].id;
        }

        public int GetRightCharacterID(int characterID, CharacterType characterType)
        {
            var targetCharacterList = new List<CharacterInfo>();
            for (int i = 0; i < CharacterInfo.All.Count; i++)
            {
                var c = CharacterInfo.All[i];
                if (c.character_type == characterType)
                    targetCharacterList.Add(c);
            }

            if (targetCharacterList.Count == 0) return characterID;

            int idx = -1;
            for (int i = 0; i < targetCharacterList.Count; i++)
            {
                if (targetCharacterList[i].id == characterID)
                {
                    idx = i;
                    break;
                }
            }

            if (idx < 0)
                return targetCharacterList[0].id;

            int rightIdx = (idx == targetCharacterList.Count - 1) ? 0 : idx + 1;
            return targetCharacterList[rightIdx].id;
        }

        public AccountLevelExp GetAccountLevelExpDataByLevel(int level)
        {
            for (int i = 0; i < AccountLevelExp.All.Count; i++)
            {
                var data = AccountLevelExp.All[i];
                if (data.lv == level)
                    return data;
            }
            return null;
        }

        public int GetAccountMaxLevel()
        {
            int maxLevel = 0;
            for (int i = 0; i < AccountLevelExp.All.Count; i++)
            {
                var data = AccountLevelExp.All[i];
                if (data.lv > maxLevel)
                    maxLevel = data.lv;
            }
            return maxLevel;
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
            for (int i = 0; i < AccountLevelExp.All.Count; i++)
            {
                var accountData = AccountLevelExp.All[i];
                if (accountData.exp_start > exp)
                {
                    return accountData.lv == 1 ? accountData.lv : accountData.lv - 1;
                }
            }

            return 1;
        }

        public GuideMissionInfo GetGuideMissionDataByOrder(int order)
        {
            for (int i = 0; i < GuideMissionInfo.All.Count; i++)
            {
                var data = GuideMissionInfo.All[i];
                if (data.order == order)
                    return data;
            }
            return null;
        }

        public List<GuideMissionInfo> GetGuideMissionDataList(int order)
        {
            var result = new List<GuideMissionInfo>();
            for (int i = 0; i < GuideMissionInfo.All.Count; i++)
            {
                var data = GuideMissionInfo.All[i];
                if (data.order <= order)
                    result.Add(data);
            }
            return result;
        }

        // 가이드 미션 order 최대치 반환
        public int GetGuideMissionMaxOrder()
        {
            int maxOrder = 0;
            for (int i = 0; i < GuideMissionInfo.All.Count; i++)
            {
                var guide = GuideMissionInfo.All[i];
                if (guide.order > maxOrder)
                    maxOrder = guide.order;
            }
            return maxOrder;
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

        public ISpecSynergyData GetSpecSynergyData(int synergyID)
        {
            for (int i = 0; i < SynergyElemental.All.Count; i++)
            {
                var synergy = SynergyElemental.All[i];
                if (synergy.synergy_group_id == synergyID)
                {
                    return synergy;
                }
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
            foreach (var l in outSynergyList)
            {
                if (l.min_int <= count && l.max_int >= count)
                {
                    outSynergyData = l;
                    break;
                }
            }
            if (outSynergyData == null || outSynergyData.grade < 1)
            {
                return false;
            }
            return true;
        }

        public List<List<SkillJob>> GetJobPassiveList(CharacterPositionType positionType)
        {
            if (positionType == CharacterPositionType.NONE)
            {
                return null;
            }

            List<List<SkillJob>> passiveList = new List<List<SkillJob>>();
            for (int i = 0; i < SkillJobPassive.All.Count; i++)
            {
                var positionPassive = SkillJobPassive.All[i];
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
            for (int i = 0; i < QuestInfo.All.Count; i++)
            {
                var data = QuestInfo.All[i];
                if (data.quest_id == questID)
                    return data;
            }
            return null;
        }

        public List<QuestInfo> GetSpecQuestList(TermType termType, bool isIncludeMilestone)
        {
            var result = new List<QuestInfo>();
            for (int i = 0; i < QuestInfo.All.Count; i++)
            {
                var data = QuestInfo.All[i];
                if (data.term_type == termType)
                {
                    if (isIncludeMilestone)
                    {
                        result.Add(data);
                    }
                    else if (data.quest_type != QuestType.CLEAR_DAILY_QUEST
                             && data.quest_type != QuestType.CLEAR_WEEKLY_QUEST)
                    {
                        result.Add(data);
                    }
                }
            }
            return result;
        }

        public List<QuestInfo> GetSpecQuestList(QuestType questType)
        {
            var result = new List<QuestInfo>();
            for (int i = 0; i < QuestInfo.All.Count; i++)
            {
                var data = QuestInfo.All[i];
                if (data.quest_type == questType)
                    result.Add(data);
            }
            return result;
        }

        public List<QuestInfo> GetSpecQuestList(TermType termType, QuestType questType)
        {
            var result = new List<QuestInfo>();
            for (int i = 0; i < QuestInfo.All.Count; i++)
            {
                var data = QuestInfo.All[i];
                if (data.term_type == termType && data.quest_type == questType)
                    result.Add(data);
            }
            return result;
        }

        public EventInfo GetSpecEventData(int eventID)
        {
            for (int i = 0; i < EventInfo.All.Count; i++)
            {
                var data = EventInfo.All[i];
                if (data.event_id == eventID)
                    return data;
            }
            return null;
        }

        public EventInfo GetSpecEventData(EventType eventType)
        {
            for (int i = 0; i < EventInfo.All.Count; i++)
            {
                var data = EventInfo.All[i];
                if (data.event_type == eventType)
                    return data;
            }
            return null;
        }

        public List<EventInfo> GetSpecEventList(EventType eventType)
        {
            var result = new List<EventInfo>();
            for (int i = 0; i < EventInfo.All.Count; i++)
            {
                var data = EventInfo.All[i];
                if (data.event_type == eventType)
                    result.Add(data);
            }
            return result;
        }

        public List<EventInfo> GetSpecEventList(TermType termType)
        {
            var result = new List<EventInfo>();
            for (int i = 0; i < EventInfo.All.Count; i++)
            {
                var data = EventInfo.All[i];
                if (data.term_type == termType)
                    result.Add(data);
            }
            return result;
        }

        // 기간 제한이 존재하는 이벤트 리스트를 반환
        public List<EventInfo> GetLimitedSpecEventList()
        {
            var result = new List<EventInfo>();
            for (int i = 0; i < EventInfo.All.Count; i++)
            {
                var data = EventInfo.All[i];
                if (data.frequency_type == FrequencyType.ONCE)
                    result.Add(data);
            }
            return result;
        }

        // 기간 제한이 존재하지 않는 이벤트 리스트를 반환 (서비스 중 기간동안 지속 반복)
        public List<EventInfo> GetNoneLimitedSpecEventList()
        {
            var result = new List<EventInfo>();
            for (int i = 0; i < EventInfo.All.Count; i++)
            {
                var data = EventInfo.All[i];
                if (data.frequency_type == FrequencyType.REPEAT)
                    result.Add(data);
            }
            return result;
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
            for (int i = 0; i < EventCondition.All.Count; i++)
            {
                var data = EventCondition.All[i];
                if (data.event_id == eventID && data.event_condition_id == eventConditionID)
                    return data;
            }
            return null;
        }

        public List<EventCondition> GetSpecEventConditionList(int eventID)
        {
            var result = new List<EventCondition>();
            for (int i = 0; i < EventCondition.All.Count; i++)
            {
                var data = EventCondition.All[i];
                if (data.event_id == eventID)
                    result.Add(data);
            }
            return result;
        }

        public DungeonBabelInfo GetSpecDungeonTrialData(int dungeonID)
        {
            for (int i = 0; i < DungeonBabelInfo.All.Count; i++)
            {
                var data = DungeonBabelInfo.All[i];
                if (data.dungeon_id == dungeonID)
                    return data;
            }
            return null;
        }

        public DungeonBabelInfo GetSpecDungeonTrialDataByOrder(int order)
        {
            for (int i = 0; i < DungeonBabelInfo.All.Count; i++)
            {
                var data = DungeonBabelInfo.All[i];
                if (data.order == order)
                    return data;
            }
            return null;
        }

        public List<DungeonBabelInfo> GetSpecDungeonTrialDataList(DungeonType dungeonType)
        {
            var result = new List<DungeonBabelInfo>();
            for (int i = 0; i < DungeonBabelInfo.All.Count; i++)
            {
                var data = DungeonBabelInfo.All[i];
                if (data.dungeon_type == dungeonType)
                    result.Add(data);
            }
            return result;
        }

        public List<DungeonBabelInfo> GetSpecDungeonTrialDataListByStageStar(int stageStar)
        {
            var result = new List<DungeonBabelInfo>();
            for (int i = 0; i < DungeonBabelInfo.All.Count; i++)
            {
                var data = DungeonBabelInfo.All[i];
                if (data.need_star <= stageStar)
                    result.Add(data);
            }
            return result;
        }

        public List<DungeonBabelMonster> GetSpecDungeonMonsterDataList(DungeonType dungeonType, int dungeonID)
        {
            var result = new List<DungeonBabelMonster>();
            for (int i = 0; i < DungeonBabelMonster.All.Count; i++)
            {
                var data = DungeonBabelMonster.All[i];
                if (data.dungeon_type == dungeonType && data.dungeon_id == dungeonID)
                    result.Add(data);
            }
            return result;
        }

        public List<DungeonBabelReward> GetSpecDungeonRewardDataList(DungeonType dungeonType, int dungeonID)
        {
            var result = new List<DungeonBabelReward>();
            for (int i = 0; i < DungeonBabelReward.All.Count; i++)
            {
                var data = DungeonBabelReward.All[i];
                if (data.dungeon_type == dungeonType && data.dungeon_id == dungeonID)
                    result.Add(data);
            }
            return result;
        }

        #region Shop

        public ShopInfo GetShopData(int shopID)
        {
            for (int i = 0; i < ShopInfo.All.Count; i++)
            {
                var data = ShopInfo.All[i];
                if (data.shop_id == shopID)
                    return data;
            }
            return null;
        }

        public List<ShopInfo> GetShopDataList(ShopMainGroupType mainGroupType)
        {
            var result = new List<ShopInfo>();
            for (int i = 0; i < ShopInfo.All.Count; i++)
            {
                var data = ShopInfo.All[i];
                if (data.shop_main_group_type == mainGroupType)
                    result.Add(data);
            }
            return result;
        }

        public List<ShopInfo> GetShopDataList(ShopMainGroupType mainGroupType, ShopSubGroupType subGroupType)
        {
            var result = new List<ShopInfo>();
            for (int i = 0; i < ShopInfo.All.Count; i++)
            {
                var data = ShopInfo.All[i];
                if (data.shop_main_group_type == mainGroupType && data.shop_sub_group_type == subGroupType)
                    result.Add(data);
            }
            return result;
        }

        public ShopBanner GetShopBannerData(int shopID)
        {
            for (int i = 0; i < ShopBanner.All.Count; i++)
            {
                var data = ShopBanner.All[i];
                if (data.shop_id == shopID)
                    return data;
            }
            return null;
        }

        #endregion

        #region Gacha

        public GachaInfo GetGachaData(int gachaID)
        {
            for (int i = 0; i < GachaInfo.All.Count; i++)
            {
                var data = GachaInfo.All[i];
                if (data.gacha_id == gachaID)
                    return data;
            }
            return null;
        }

        public GachaInfo GetGachaData(GachaType gachaType, int gachaCount)
        {
            for (int i = 0; i < GachaInfo.All.Count; i++)
            {
                var data = GachaInfo.All[i];
                if (data.gacha_type == gachaType && data.gacha_count == gachaCount)
                    return data;
            }
            return null;
        }

        public List<GachaInfo> GetGachaDataList(GachaType gachaType)
        {
            var result = new List<GachaInfo>();
            for (int i = 0; i < GachaInfo.All.Count; i++)
            {
                var data = GachaInfo.All[i];
                if (data.gacha_type == gachaType)
                    result.Add(data);
            }
            return result;
        }

        public List<GachaCharacter> GetGachaContentDataList(int gachaGroupID)
        {
            var result = new List<GachaCharacter>();
            for (int i = 0; i < GachaCharacter.All.Count; i++)
            {
                var data = GachaCharacter.All[i];
                if (data.gacha_group_id == gachaGroupID)
                    result.Add(data);
            }
            return result;
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
                        // ItemType의 삭제로 인해 변경.(new RewardItem(selectedData.result_item_key, selectedData.result_item_count))
                        rewardItemList.Add(new RewardItem(selectedData.result_item_key, selectedData.result_item_count));
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
                // ItemType의 삭제로 인해 변경.(new RewardItem(gachaContent.result_item_key, gachaContent.result_item_count))
                rewardItemList.Add(new RewardItem(gachaContent.result_item_key, gachaContent.result_item_count));
            }

            return rewardItemList;
        }

        // 시나리오 가챠 데이터 반환
        public List<GachaScenario> GetGachaScenarioList(int currentCount, int gachaCount)
        {
            int maxCount = GachaScenario.All.Count;
            int resultCount = currentCount + gachaCount > maxCount ? maxCount - currentCount : gachaCount;

            var result = new List<GachaScenario>(resultCount);
            for (int i = currentCount; i < currentCount + resultCount; i++)
            {
                result.Add(GachaScenario.All[i]);
            }
            return result;
        }

        // 시나리오 가챠 데이터를 RewardItem 리스트로 변환
        public List<RewardItem> GetRewardItemListByGachaScenarioList(List<GachaScenario> gachaScenarioList)
        {
            List<RewardItem> rewardItemList = new List<RewardItem>();
            foreach (var gachaScenario in gachaScenarioList)
            {
                // ItemType의 삭제로 인해 변경.(new RewardItem(gachaScenario.item_type, gachaScenario.item_key, gachaScenario.item_count))
                rewardItemList.Add(new RewardItem(gachaScenario.item_id, gachaScenario.item_count));
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
            var guideMission = ServerDataManager.Instance.GuideMission;

            // 모든 가이드 미션 클리어 체크
            if (guideMission.IsAllCompleted)
            {
                return true;
            }

            int currMissionID = (int)guideMission.GuideMissionId;
            OpenCondition openCondition = null;
            for (int i = 0; i < OpenCondition.All.Count; i++)
            {
                var l = OpenCondition.All[i];
                if (l.open_condition_Type == conditionType)
                {
                    openCondition = l;
                    break;
                }
            }
            return openCondition != null && openCondition.guide_mission_id <= currMissionID;
        }

        public ImageInfo GetImageInfoData(int infoID)
        {
            for (int i = 0; i < ImageInfo.All.Count; i++)
            {
                var data = ImageInfo.All[i];
                if (data.image_info_id == infoID)
                    return data;
            }
            return null;
        }

        public List<TileEffectCode> GetSpecTileEffectCodeList()
        {
            var result = new List<TileEffectCode>(TileEffectCode.All.Count);
            for (int i = 0; i < TileEffectCode.All.Count; i++)
            {
                result.Add(TileEffectCode.All[i]);
            }
            return result;
        }

        public ElpisBuildInfo GetElpisBuildInfoData(int uniqueId, int level)
        {
            for (int i = 0; i < ElpisBuildInfo.All.Count; i++)
            {
                var target = ElpisBuildInfo.All[i];
                if (target.build_id == uniqueId && target.build_lv == level)
                    return target;
            }

            return null;
        }

        public ElpisBuildInfo GetBuildInfo(int uniqueId)
        {
            for (int i = 0; i < ElpisBuildInfo.All.Count; i++)
            {
                var target = ElpisBuildInfo.All[i];
                if (target.build_id == uniqueId)
                    return target;
            }

            return null;
        }

        public IReadOnlyList<ElpisBuildInfo> GetBuildInfoList(ElpisFacilityType facilityType)
        {
            var targetType = FacilityType.NONE;

            //TODO : 최적화 필요....
            switch (facilityType)
            {
                case ElpisFacilityType.FacilityTypeCommandCenter:
                    targetType = FacilityType.COMMAND_CENTER;
                    break;
                case ElpisFacilityType.FacilityTypeNest:
                    targetType = FacilityType.NEST;
                    break;
                case ElpisFacilityType.FacilityTypeDimensionLab:
                    targetType = FacilityType.DIMENSION_LAB;
                    break;
                case ElpisFacilityType.FacilityTypeSimulationCenter:
                    targetType = FacilityType.SIMULATION_CENTER;
                    break;
                case ElpisFacilityType.FacilityTypeUnspecified:
                    targetType = FacilityType.NONE;
                    break;
            }

            var result = new List<ElpisBuildInfo>();
            for (int i = 0; i < ElpisBuildInfo.All.Count; i++)
            {
                var target = ElpisBuildInfo.All[i];
                if (target.facility_type == targetType)
                    result.Add(target);
            }

            return result;
        }

        public IReadOnlyList<ElpisDimensionLab> GetAllElpisDimensionLab()
        {
            return ElpisDimensionLab.All;
        }

        public IReadOnlyList<ElpisBuildInfo> GetSameFacilityTypes(FacilityType facilityType)
        {
            var result = new List<ElpisBuildInfo>();
            var allData = ElpisBuildInfo.All;
            foreach (var data in allData)
            {
                if (data.facility_type == facilityType)
                    result.Add(data);
            }

            return result;
        }

        public UserKnightCount GetUserKnightCountByNestCount()
        {
            var nestCount = 0;
            var dataBridge = new ElpisDataBridge();
            var nestFacilityList = GetBuildInfoList(Tech.Hive.V1.ElpisFacilityType.FacilityTypeNest);

            for (int i = 0; i < nestFacilityList.Count; i++)
            {
                var nestFacility = dataBridge.GetFacility((uint)nestFacilityList[i].build_id);
                if (nestFacility != null && nestFacility.Level > 0 && !nestFacility.IsBuilding && !nestFacility.IsJustCompleted)
                {
                    nestCount++;
                }
            }


            var allData = UserKnightCount.All;
            foreach (var data in allData)
            {
                if (data.nest_count == nestCount)
                {
                    return data;
                }
            }

            return null;
        }
    }
}
