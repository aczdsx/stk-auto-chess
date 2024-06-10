// #define USE_SERVER_SPEC

#if USE_SERVER_SPEC
using CookApps.gRPC.Universal;
using CookApps.LocalData;
#endif
using System.Collections.Generic;
using System.Linq;
using Cookapps.Autobattleproject.V1;
using CookApps.TeamBattle;
using Cysharp.Threading.Tasks;
using Unity.VisualScripting;


namespace CookApps.AutoBattler
{
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
            CustomizeSpecData();
        }

        private Dictionary<string, SpecLanguage> languageDic = new (); // key : token_key, value : language data
        private Dictionary<int, List<RewardItem>> chestDic = new (); // key : chest_id, value : chest list
        private Dictionary<int, List<SpecChapter>> chapterDic = new (); // key : chapter_id, value : chapter list
        private Dictionary<int, List<SpecStage>> stageChapterDic = new (); // key : chapter_id, value : stage list
        private Dictionary<int, List<SpecStageMonster>> stageMonsterDic = new (); // key : chapter_id, value : stage list
        private Dictionary<int, List<SpecStageReward>> stageRewardDic = new (); // key : reward_id, value : stage list
        private Dictionary<int, List<SpecCharacter>> characterDic = new (); // key : character_id, value : stage list
        private Dictionary<string, SpecGameConfig> configDic = new (); // key : config_key, value : game config data
        private Dictionary<int, List<SpecSkill>> skillDic = new (); // key : skill_id, value : skill list
        private Dictionary<DialogueEventType, Dictionary<string, int>> dialogueHistoryDic = new (); // key1 : DialogueEventType, key2 : sub_key_value, value : dialogue_group_id
        private Dictionary<InGameVfxNameType, SpecInGameVfx> inGameVfxDic = new (); // key : inGameVfxName, value : SpecInGameVfx

        private void CustomizeSpecData()
        {
            // Language
            languageDic.Clear();
            foreach (SpecLanguage language in SpecLanguage.All)
            {
                if (!languageDic.ContainsKey(language.token_key))
                {
                    languageDic.Add(language.token_key, language);
                }
            }

            // Chest
            chestDic.Clear();
            foreach (SpecChest chest in SpecChest.All)
            {
                if (!chestDic.TryGetValue(chest.chest_id, out List<RewardItem> chestList))
                {
                    chestList = new List<RewardItem>();
                    chestDic.Add(chest.chest_id, chestList);
                }

                chestList.Add(chest.ToRewardItem());
            }

            // Chapter
            chapterDic.Clear();
            foreach (SpecChapter chapter in SpecChapter.All)
            {
                if (!chapterDic.TryGetValue(chapter.chapter_id, out List<SpecChapter> chapterList))
                {
                    chapterList = new List<SpecChapter>();
                    chapterDic.Add(chapter.chapter_id, chapterList);
                }

                chapterList.Add(chapter);
            }

            // Stage
            stageChapterDic.Clear();
            foreach (SpecStage stage in SpecStage.All)
            {
                if (!stageChapterDic.TryGetValue(stage.chapter_id, out List<SpecStage> stageList))
                {
                    stageList = new List<SpecStage>();
                    stageChapterDic.Add(stage.chapter_id, stageList);
                }

                stageList.Add(stage);
            }

            // Stage
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

            // Stage
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
            foreach (SpecSkill skill in SpecSkill.All)
            {
                if (!skillDic.TryGetValue(skill.skill_id, out List<SpecSkill> skillList))
                {
                    skillList = new List<SpecSkill>();
                    skillDic.Add(skill.skill_id, skillList);
                }

                skillList.Add(skill);
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

            return configData.config_value.ConvertTo<T>();
        }

        public SpecCharacter GetCharacterData(int prefabID)
        {
            return SpecCharacter.All.ToList().Find(character => character.prefab_id == prefabID);
        }

        public List<SpecCharacter> GetCharacterListByCharacterType(CharacterType type)
        {
            return SpecCharacter.All.ToList().FindAll(character => character.character_type == type);
        }

        public List<SpecChapter> GetChapterList(int chapter)
        {
            if (chapterDic.TryGetValue(chapter, out List<SpecChapter> chapterList))
            {
                return chapterList;
            }

            return null;
        }

        public List<SpecChapter> GetChapterList(int chapter, DifficultyType difficulty)
        {
            if (chapterDic.TryGetValue(chapter, out List<SpecChapter> chapterList))
            {
                return chapterList.FindAll(stage => stage.difficulty_type == difficulty);
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
            return SpecDialogue.All.ToList().FindAll(data => data.dialouge_group_id == groupID);
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

        public SpecStage GetStageData(int chapterID, int stageNumber, DifficultyType type)
        {
            if (stageChapterDic.TryGetValue(chapterID, out List<SpecStage> stageList))
            {
                return stageList.Find(stage => stage.stage_number == stageNumber && stage.difficulty_type == type);
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

        // 해당 스테이지가 마지막 스테이지인지 체크
        public bool IsLastStage(int stageID)
        {
            SpecStage stageSpecData = SpecStage.Get(stageID);
            SpecStage nextStageSpecData = GetStageData(stageSpecData.chapter_id, stageSpecData.stage_number + 1, stageSpecData.difficulty_type);

            return nextStageSpecData == null;
        }

        public List<SpecStageMonster> GetStageMonsterList(int chapter, int stageNumber, DifficultyType difficulty)
        {
            if (stageMonsterDic.TryGetValue(chapter, out List<SpecStageMonster> stageMonster))
            {
                return stageMonster.FindAll(s => s.stage_number == stageNumber &&  s.difficulty_type == difficulty);
            }

            return null;
        }

        public List<RewardItem> GetChestList(int chestId)
        {
            return chestDic.GetValueOrDefault(chestId);
        }

        public List<SpecSkill> GetSkillDataList(int skillID)
        {
            return skillDic.GetValueOrDefault(skillID);
        }

        public SpecCharacter GetSpecCharacter(int characterID)
        {
            return SpecCharacter.All.FirstOrDefault(data => data.character_id == characterID);
        }
        public SpecAccountLevelExp GetAccountLevelExpDataByLevel(int level)
        {
            return SpecAccountLevelExp.All.ToList().Find(data => data.lv == level);
        }

        public int GetAccountMaxLevel()
        {
            return SpecAccountLevelExp.All.Max(data => data.lv);
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
            foreach (var accountData in SpecAccountLevelExp.All)
            {
                if (accountData.exp_start > exp)
                {
                    return accountData.lv == 1 ? accountData.lv : accountData.lv - 1;
                }
            }

            return 1;
        }

        // 가이드 미션 order 최대치 반환
        public int GetGuideMissionMaxOrder()
        {
            return SpecGuideMission.All.Max(guide => guide.order);
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
    }
}
