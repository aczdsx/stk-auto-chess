using System.Collections.Generic;
using CookApps.BattleSystem;
using CookApps.TeamBattle;

namespace CookApps.AutoBattler
{
    public partial class SpecDataManager
    {
        // Lazy initialization flags
        private bool _isItemTableKeyMap = false;
        private bool _isChapterCache = false;
        private bool _isStageCache = false;
        private bool _isStageMonsterCache = false;
        private bool _isStageRewardCache = false;
        private bool _isConfigCache = false;
        private bool _isSkillCache = false;
        private bool _isSkillPassiveCache = false;
        private bool _isDialogueHistoryCache = false;
        private bool _isInGameVfxCache = false;
        private bool _isSynergyCache  = false;
        private bool _isSkillJobCache  = false;
        private bool _isCommanderSkillCache = false;
        private bool _isTutorialDialogueCache = false;
        private bool _isCharacterBySynergyCache = false;

        // charactersBySynergyDic: key = SynergyType, value = CharacterInfo
        private void EnsureCharacterBySynergyCache()
        {
            if (_isCharacterBySynergyCache) return;
            _isCharacterBySynergyCache = true;

            charactersBySynergyDic = new Dictionary<SynergyType, List<CharacterInfo>>();
            for (int i = 0; i < CharacterInfo.All.Count; i++)
            {
                var character = CharacterInfo.All[i];
                if (character.character_type != CharacterType.CHARACTER) continue;

                // element
                if (character.character_element_type != SynergyType.NONE)
                {
                    if (!charactersBySynergyDic.TryGetValue(character.character_element_type, out var elementList))
                    {
                        elementList = new List<CharacterInfo>();
                        charactersBySynergyDic[character.character_element_type] = elementList;
                    }
                    elementList.Add(character);
                }

                // stella
                if (character.character_stella_type != SynergyType.NONE)
                {
                    if (!charactersBySynergyDic.TryGetValue(character.character_stella_type, out var stellaList))
                    {
                        stellaList = new List<CharacterInfo>();
                        charactersBySynergyDic[character.character_stella_type] = stellaList;
                    }
                    stellaList.Add(character);
                }
            }
        }

        // itemTableKeyMap: key = item_id (currency_id/item_id), value = ISpecItemInfo
        private void InitializeItemTableKeyMap()
        {
            if (_isItemTableKeyMap) return;

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

            _isItemTableKeyMap = true;
        }

        // chapterDic: key = chapter_id, value = chapter list
        // chapterDifficultDic: key = DifficultyType, value = chapter list
        private void EnsureChapterCache()
        {
            if (_isChapterCache) return;
            _isChapterCache = true;

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
        }

        // stageChapterDic: key = chapter_id, value = stage list
        // stageDifficultDic: key = DifficultyType, value = stage list
        private void EnsureStageCache()
        {
            if (_isStageCache) return;
            _isStageCache = true;

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
        }

        // stageMonsterDic: key = chapter_id, value = stage monster list
        private void EnsureStageMonsterCache()
        {
            if (_isStageMonsterCache) return;
            _isStageMonsterCache = true;

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
        }

        // stageRewardDic: key = reward_id, value = stage reward list
        private void EnsureStageRewardCache()
        {
            if (_isStageRewardCache) return;
            _isStageRewardCache = true;

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
        }

        // configDic: key = config_key, value = game config data
        private void EnsureConfigCache()
        {
            if (_isConfigCache) return;
            _isConfigCache = true;

            configDic.Clear();
            for (int i = 0; i < ConfigGame.All.Count; i++)
            {
                var config = ConfigGame.All[i];
                if (!configDic.ContainsKey(config.config_key))
                {
                    configDic.Add(config.config_key, config);
                }
            }
        }

        // skillDic: key = skill_group_id, value = skill list
        // skillPrefabIDDic: key = prefab_id, value = skill list
        private void EnsureSkillCache()
        {
            if (_isSkillCache) return;
            _isSkillCache = true;

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
        }

        // skillPassiveDic: key = passive_group_id, value = SkillPassive
        // skillPassivePrefabIDDic: key = prefab_id, value = SkillPassive
        private void EnsureSkillPassiveCache()
        {
            if (_isSkillPassiveCache) return;
            _isSkillPassiveCache = true;

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
        }

        // dialogueHistoryDic: key1 = DialogueEventType, key2 = sub_key_value, value = dialogue_group_id
        private void EnsureDialogueHistoryCache()
        {
            if (_isDialogueHistoryCache) return;
            _isDialogueHistoryCache = true;

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
        }

        // inGameVfxDic: key = InGameVfxNameType, value = InGameVfxMap
        private void EnsureInGameVfxCache()
        {
            if (_isInGameVfxCache) return;
            _isInGameVfxCache = true;

            inGameVfxDic.Clear();
            for (int i = 0; i < InGameVfxMap.All.Count; i++)
            {
                var inGameVfx = InGameVfxMap.All[i];
                if (!inGameVfxDic.ContainsKey(inGameVfx.vfx_name_type))
                {
                    inGameVfxDic.Add(inGameVfx.vfx_name_type, inGameVfx);
                }
            }
        }

        // synergyDic: key = SynergyType, value = ISpecSynergyData
        private void EnsureSynergyCache()
        {
            if (_isSynergyCache) return;
            _isSynergyCache = true;

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
        }

        // skillJobDic: key = EffectCodeNameType, value = SkillJob
        private void EnsureSkillJobCache()
        {
            if (_isSkillJobCache) return;
            _isSkillJobCache = true;

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
        }

        // commanderSkillDic: key = commander_skill_id, value = SkillCommander
        private void EnsureCommanderSkillCache()
        {
            if (_isCommanderSkillCache) return;
            _isCommanderSkillCache = true;

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
        }

        // tutorialDialogueDic: key = tutorial_id, value = TutorialDialogue list
        private void EnsureTutorialDialogueCache()
        {
            if (_isTutorialDialogueCache) return;
            _isTutorialDialogueCache = true;

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
    }
}
