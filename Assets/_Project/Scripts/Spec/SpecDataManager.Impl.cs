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
        }

        // Lazy Cached Dictionaries (초기화: SpecDataManager.Customize.cs)
        private Dictionary<int, List<ChapterInfo>> chapterDic = new();                             // key: chapter_id
        private Dictionary<DifficultyType, List<ChapterInfo>> chapterDifficultDic = new();         // key: DifficultyType
        private Dictionary<int, List<StageInfo>> stageChapterDic = new();                          // key: chapter_id
        private Dictionary<DifficultyType, List<StageInfo>> stageDifficultDic = new();             // key: DifficultyType
        private Dictionary<int, List<StageMonster>> stageMonsterDic = new();                       // key: chapter_id
        private Dictionary<int, List<StageReward>> stageRewardDic = new();                         // key: reward_id
        private Dictionary<string, ConfigGame> configDic = new();                                  // key: config_key
        private Dictionary<long, List<SkillActive>> skillDic = new();                              // key: skill_group_id
        private Dictionary<long, List<SkillActive>> skillPrefabIDDic = new();                      // key: prefab_id
        private Dictionary<long, List<SkillPassive>> skillPassiveDic = new();                      // key: passive_group_id
        private Dictionary<long, List<SkillPassive>> skillPassivePrefabIDDic = new();              // key: prefab_id
        private Dictionary<DialogueEventType, Dictionary<string, int>> dialogueHistoryDic = new(); // key: DialogueEventType > sub_key_value
        private Dictionary<InGameVfxNameType, InGameVfxMap> inGameVfxDic = new();                  // key: InGameVfxNameType
        private Dictionary<SynergyType, List<ISpecSynergyData>> synergyDic = new();               // key: SynergyType
        private Dictionary<SynergyType, List<CharacterInfo>> charactersBySynergyDic;               // key: SynergyType
        private static readonly List<CharacterInfo> _reusableEmptyCharacterList = new();
        private Dictionary<EffectCodeNameType, List<SkillJob>> skillJobDic = new();                // key: EffectCodeNameType
        private Dictionary<int, List<SkillCommander>> commanderSkillDic = new();                   // key: commander_skill_id
        private Dictionary<int, ISpecItemInfo> itemTableKeyMap = new();                            // key: item_id
        private Dictionary<int, List<TutorialDialogue>> tutorialDialogueDic = new();               // key: tutorial_id


        public T GetGameConfig<T>(string key)
        {
            EnsureConfigCache();
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

        public List<CharacterInfo> GetCharacterListBySynergyType(SynergyType synergyType)
        {
            EnsureCharacterBySynergyCache();
            return charactersBySynergyDic.TryGetValue(synergyType, out var result)
                ? result
                : _reusableEmptyCharacterList;
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

        public CharacterTranscendence GetCharacterTranscendenceData(GradeType gradeType, int star)
        {
            CharacterTranscendence minData = null;
            CharacterTranscendence maxData = null;

            for (int i = 0; i < CharacterTranscendence.All.Count; i++)
            {
                var data = CharacterTranscendence.All[i];
                if (data.grade_type != gradeType)
                    continue;

                // 정확히 일치하면 바로 반환
                if (data.star == star)
                    return data;

                // min/max 추적
                if (minData == null || data.star < minData.star)
                    minData = data;
                if (maxData == null || data.star > maxData.star)
                    maxData = data;
            }

            // 범위를 벗어나면 min/max 반환
            if (minData != null && star < minData.star)
                return minData;
            if (maxData != null && star > maxData.star)
                return maxData;

            return null;
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
            EnsureChapterCache();
            if (chapterDic.TryGetValue(chapterID, out List<ChapterInfo> chapterList))
            {
                return chapterList.Find(data => data.difficulty_type == type);
            }

            return null;
        }

        public ChapterInfo GetChapterDataByStageID(int stageID)
        {
            EnsureChapterCache();
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
                }
            }

            return null;
        }

        public List<ChapterInfo> GetChapterList(DifficultyType difficulty)
        {
            EnsureChapterCache();
            if (chapterDifficultDic.TryGetValue(difficulty, out List<ChapterInfo> chapterList))
            {
                return chapterList;
            }

            return null;
        }

        public int GetTotalChapterStarCount(int chapterID, DifficultyType type)
        {
            EnsureStageCache();
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
            EnsureTutorialDialogueCache();
            if (tutorialDialogueDic.TryGetValue(tutorialID, out List<TutorialDialogue> tutorialDialogueList))
            {
                return tutorialDialogueList;
            }

            return null;
        }

        public int GetDialgueGroupIDByEventType(DialogueEventType eventType, string subKeyValue)
        {
            EnsureDialogueHistoryCache();
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
            EnsureStageCache();
            if (stageChapterDic.TryGetValue(chapterID, out List<StageInfo> stageList))
            {
                foreach (var stage in stageList)
                {
                    if (stage.stage_number == stageNumber && stage.difficulty_type == type)
                        return stage;
                }
            }

            return null;
        }

        public StageInfo GetStageData(int chapterID, DifficultyType difficultyType, StageType stageType)
        {
            EnsureStageCache();
            if (stageChapterDic.TryGetValue(chapterID, out List<StageInfo> stageList))
            {
                foreach (var stage in stageList)
                {
                    if (stage.difficulty_type == difficultyType && stage.stage_type == stageType)
                        return stage;
                }
            }

            return null;
        }
        

        public List<StageInfo> GetStageList(int chapter, DifficultyType difficulty)
        {
            EnsureStageCache();
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

        public int GetStageCount(int chapter, DifficultyType difficulty)
        {
            EnsureStageCache();
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
            EnsureStageCache();
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

        public List<StageMonster> GetStageMonsterList(int chapter, int stageNumber, DifficultyType difficulty)
        {
            EnsureStageMonsterCache();
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

        public RewardInfo GetSpecRewardInfo(ContentType contentType, int contentKey, DifficultyType difficultyType)
        {
            for (int i = 0; i < RewardInfo.All.Count; i++)
            {
                var data = RewardInfo.All[i];
                if (data.content_type == contentType
                    && data.content_key_value == contentKey
                    && data.difficulty_type == difficultyType)
                    return data;
            }
            return null;
        }

        public List<SkillActive> GetSkillDataList(long skillID)
        {
            EnsureSkillCache();
            return skillDic.GetValueOrDefault(skillID);
        }

        public List<SkillPassive> GetSkillPassiveDataList(long passiveSkillID)
        {
            EnsureSkillPassiveCache();
            return skillPassiveDic.GetValueOrDefault(passiveSkillID);
        }

        public List<SkillActive> GetSkillDataListByPrefabID(int prefabID)
        {
            EnsureSkillCache();
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
            EnsureSkillPassiveCache();
            return skillPassiveDic.GetValueOrDefault(passiveGroupID);
        }

        public List<SkillPassive> GetSkillPassiveDataListByPrefabID(int prefabID)
        {
            EnsureSkillPassiveCache();
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
            EnsureStageRewardCache();
            if (stageRewardDic.TryGetValue(rewardID, out List<StageReward> stageRewardList))
            {
                return stageRewardList;
            }

            return null;
        }

        public InGameVfxMap GetInGameVfxData(InGameVfxNameType vfxNameType)
        {
            EnsureInGameVfxCache();
            return inGameVfxDic.GetValueOrDefault(vfxNameType);
        }

        public List<ISpecSynergyData> GetSpecSynergyList(SynergyType synergyType)
        {
            EnsureSynergyCache();
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
            for (int i = 0; i < SynergyStarAsterism.All.Count; i++)
            {
                var synergy = SynergyStarAsterism.All[i];
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

            EnsureSkillJobCache();
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

       
        #endregion

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
            var elpisModel = ServerDataManager.Instance.Elpis;
            var nestFacilityList = GetBuildInfoList(Tech.Hive.V1.ElpisFacilityType.FacilityTypeNest);

            for (int i = 0; i < nestFacilityList.Count; i++)
            {
                var nestFacility = elpisModel.GetFacility((uint)nestFacilityList[i].build_id);
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
