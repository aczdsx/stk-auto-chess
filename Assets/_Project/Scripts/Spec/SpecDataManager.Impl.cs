// #define USE_SERVER_SPEC

#if USE_SERVER_SPEC
using CookApps.gRPC.Universal;
using CookApps.LocalData;
#endif
using System.Collections.Generic;
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

        private Dictionary<int, List<SpecStage>> specStageDic = new ();  // key : chapter, value : stage list
        private Dictionary<int, List<RewardItem>> chestDic = new (); // key : chest_id, value : chest list
        private Dictionary<int, List<Stage>> stageChapterDic = new (); // key : chapter_id, value : stage list
        private Dictionary<string, GameConfig> configDic = new (); // key : config_key, value : game config data
        private Dictionary<int, List<Skill>> skillDic = new (); // key : skill_id, value : skill list

        private void CustomizeSpecData()
        {
            // SpecStage
            specStageDic.Clear();
            foreach (SpecStage specStage in SpecStage.All)
            {
                if (!specStageDic.TryGetValue(specStage.chapter_id, out List<SpecStage> stageList))
                {
                    stageList = new List<SpecStage>();
                    specStageDic.Add(specStage.chapter_id, stageList);
                }

                stageList.Add(specStage);
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

            // Stage
            stageChapterDic.Clear();
            foreach (Stage stage in Stage.All)
            {
                if (!stageChapterDic.TryGetValue(stage.chapter_id, out List<Stage> stageList))
                {
                    stageList = new List<Stage>();
                    stageChapterDic.Add(stage.chapter_id, stageList);
                }

                stageList.Add(stage);
            }

            // Game Config
            configDic.Clear();
            foreach (GameConfig config in GameConfig.All)
            {
                if (!configDic.ContainsKey(config.config_key))
                {
                    configDic.Add(config.config_key, config);
                }
            }

            // Game Config
            skillDic.Clear();
            foreach (Skill skill in Skill.All)
            {
                if (!skillDic.TryGetValue(skill.skill_id, out List<Skill> skillList))
                {
                    skillList = new List<Skill>();
                    skillDic.Add(skill.skill_id, skillList);
                }

                skillList.Add(skill);
            }
        }

        public T GetGameConfig<T>(string key)
        {
            if (!configDic.TryGetValue(key, out GameConfig configData))
            {
                return default;
            }

            if (typeof(T) == typeof(int) && configData.config_value_type == ConfigValueType.INT) return int.Parse(configData.config_value).ConvertTo<T>();
            if (typeof(T) == typeof(float) && configData.config_value_type == ConfigValueType.FLOAT) return float.Parse(configData.config_value).ConvertTo<T>();

            return configData.config_value.ConvertTo<T>();
        }

        public List<Stage> GetStageList(int chapter)
        {
            if (stageChapterDic.TryGetValue(chapter, out List<Stage> stageList))
            {
                return stageList;
            }

            return new List<Stage>();
        }

        public List<Stage> GetStageList(int chapter, DifficultyType difficulty)
        {
            if (stageChapterDic.TryGetValue(chapter, out List<Stage> stageList))
            {
                return stageList.FindAll(stage => stage.difficulty == difficulty);
            }

            return new List<Stage>();
        }

        public int GetStageCount(int chapter, DifficultyType difficulty)
        {
            if (stageChapterDic.TryGetValue(chapter, out List<Stage> stageList))
            {
                return stageList.FindAll(stage => stage.difficulty == difficulty).Count;
            }

            return 0;
        }

        public int GetStageCount(int chapter)
        {
            if (specStageDic.TryGetValue(chapter, out List<SpecStage> stageList))
            {
                return stageList.Count;
            }

            return 0;
        }

        public SpecStage GetSpecStage(int chapter, int stageIdx)
        {
            if (specStageDic.TryGetValue(chapter, out List<SpecStage> stageList))
            {
                return stageList[stageIdx];
            }

            return null;
        }

        public int GetStageIndex(int chapter, int stageId)
        {
            if (specStageDic.TryGetValue(chapter, out List<SpecStage> stageList))
            {
                for (var i = 0; i < stageList.Count; i++)
                {
                    if (stageList[i].stage_id == stageId)
                    {
                        return i;
                    }
                }
            }

            return -1;
        }

        public List<RewardItem> GetChestList(int chestId)
        {
            return chestDic.GetValueOrDefault(chestId);
        }

        public List<Skill> GetSkillDataList(int skillID)
        {
            return skillDic.GetValueOrDefault(skillID);
        }
    }
}
