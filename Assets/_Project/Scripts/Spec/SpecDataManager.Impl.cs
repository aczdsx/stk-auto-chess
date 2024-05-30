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

        private Dictionary<int, List<RewardItem>> chestDic = new (); // key : chest_id, value : chest list
        private Dictionary<int, List<SpecChapter>> chapterDic = new (); // key : chapter_id, value : chapter list
        private Dictionary<int, List<SpecStage>> stageChapterDic = new (); // key : chapter_id, value : stage list
        private Dictionary<string, SpecGameConfig> configDic = new (); // key : config_key, value : game config data
        private Dictionary<int, List<SpecSkill>> skillDic = new (); // key : skill_id, value : skill list

        private void CustomizeSpecData()
        {
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
                return chapterList.FindAll(stage => stage.difficulty == difficulty);
            }

            return null;
        }

        public int GetTotalChapterStarCount(int chapterID, DifficultyType type)
        {
            int totalStarCount = 0;

            int stageStarCount = GetGameConfig<int>("max_stage_star_count");

            if (stageChapterDic.TryGetValue(chapterID, out List<SpecStage> stageList))
            {
                return stageList.FindAll(stage => stage.difficulty == type).Count * stageStarCount;
            }

            return totalStarCount;
        }

        public SpecStage GetStageData(int chapterID, int stageNumber, DifficultyType type)
        {
            if (stageChapterDic.TryGetValue(chapterID, out List<SpecStage> stageList))
            {
                return stageList.Find(stage => stage.stage_number == stageNumber && stage.difficulty == type);
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
                return stageList.FindAll(stage => stage.difficulty == difficulty);
            }

            return null;
        }

        public int GetStageCount(int chapter, DifficultyType difficulty)
        {
            if (stageChapterDic.TryGetValue(chapter, out List<SpecStage> stageList))
            {
                return stageList.FindAll(stage => stage.difficulty == difficulty).Count;
            }

            return 0;
        }

        // 해당 챕터의 마지막 스테이지 데이터 반환
        public SpecStage GetLastStageData(int chapterID, DifficultyType difficulty)
        {
            if (stageChapterDic.TryGetValue(chapterID, out List<SpecStage> stageList))
            {
                var targetStageList = stageList.FindAll(stage => stage.difficulty == difficulty);

                int maxStageNumber = targetStageList.Max(stage => stage.stage_number);
                return targetStageList.Find(stage => stage.stage_number == maxStageNumber);
            }

            return null;
        }

        // 해당 스테이지가 마지막 스테이지인지 체크
        public bool IsLastStage(int stageID)
        {
            SpecStage stageSpecData = SpecStage.Get(stageID);
            SpecStage nextStageSpecData = GetStageData(stageSpecData.chapter_id, stageSpecData.stage_number + 1, stageSpecData.difficulty);

            return nextStageSpecData == null;
        }

        public List<RewardItem> GetChestList(int chestId)
        {
            return chestDic.GetValueOrDefault(chestId);
        }

        public List<SpecSkill> GetSkillDataList(int skillID)
        {
            return skillDic.GetValueOrDefault(skillID);
        }
    }
}
