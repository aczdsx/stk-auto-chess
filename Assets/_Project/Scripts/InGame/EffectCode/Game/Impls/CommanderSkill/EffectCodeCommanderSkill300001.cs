using System;
using System.Collections.Generic;
using System.Linq;
using CookApps.AutoBattler;
using CookApps.Obfuscator;
using CookApps.TeamBattle.Utility;

// 십자범위 내 적에게 아군의 최고 공격력의 {0}%만큼의 대미지를 준다
namespace CookApps.BattleSystem
{
    [UseEffectCodeIds(CodeId)]
    public class EffectCodeCommanderSkill300001 : EffectCodeCommanderSkillBase
    {
        private ObfuscatorFloat _damageRate;
        private const int CodeId = 300001; // (int)EffectCodeNameType.COMMANDER_SKILL_EXPLOSION;
        
        public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container, IEffectCodeSource source)
        {
            base.Initialize(codeInfo, container, source);
            _tileID = codeInfo.GetCodeStatToInt(0);

            int userCommanderSkillLevel = codeInfo.GetCodeStatToInt(1);
            SpecDataManager specDataManager = SpecDataManager.Instance;
            _specTargetCommanderSkill = specDataManager.GetCommanderSkillListByUserSkillLevel(CodeId, userCommanderSkillLevel);

            var commanderSkillList = specDataManager.GetCommanderSkillDataList(CodeId);
            if (commanderSkillList == null || commanderSkillList.Count <= 0)
            {
                Debug.LogError($"CommanderSkillDataList is null or empty for CodeId: {CodeId}");
                return;
            }

            _damageRate = _specTargetCommanderSkill.base_rate * 0.01f;
            
            PromotionCommanderSkillCheck((PromotionLevelType)codeInfo.GetCodeStatToInt(2), (PromotionLevelType)codeInfo.GetCodeStatToInt(3));
            SkillAction();
        }

        public override void Merge(EffectCodeInfo codeInfo, IEffectCodeSource source)
        {
            base.Merge(codeInfo, source);

            int userCommanderSkillLevel = codeInfo.GetCodeStatToInt(1);
            SpecDataManager specDataManager = SpecDataManager.Instance;
            _specTargetCommanderSkill = specDataManager.GetCommanderSkillListByUserSkillLevel(CodeId, userCommanderSkillLevel);

            var commanderSkillList = specDataManager.GetCommanderSkillDataList(CodeId);
            if (commanderSkillList == null || commanderSkillList.Count <= 0)
            {
                Debug.LogError($"CommanderSkillDataList is null or empty for CodeId: {CodeId}");
                return;
            }

            _damageRate = _specTargetCommanderSkill.base_rate * 0.01f;
            PromotionCommanderSkillCheck((PromotionLevelType)codeInfo.GetCodeStatToInt(2), (PromotionLevelType)codeInfo.GetCodeStatToInt(3));
            SkillAction();
        }

        protected override void SkillAction()
        {
            ObjectRegistry.GetObject<InGameCamera>(RegistryKey.InGameCamera).ShakeCamera(0.4f, 0.15f);
            var inGameTile = InGameObjectManager.Instance.GetInGameTile(_tileID);
            var tileList = InGameObjectManager.Instance.InGameGrid.GetTileListByShapePlusInRange(inGameTile,
            _specTargetCommanderSkill.commander_range_size / 2);

            List<int> targetCharacterList = new();
            foreach (var tile in tileList)
            {
                InGameVfxManager.Instance.AddInGameVfx(InGameVfxNameType.fx_common_commander_skill_01,
                    tile.View.CachedTr.position);

                if (tile.CheckValidTile(AllianceType.Player, false))
                {
                    if (!targetCharacterList.Contains(tile.OccupiedCharacter.CharacterUId))
                    {
                        targetCharacterList.Add(tile.OccupiedCharacter.CharacterUId);
                        var averageAD = InGameObjectManager.Instance.GetAverageAD(AllianceType.Player, true);
                        var resultDamage = InGameCalculator.CalculateDefaultDamage(averageAD * _damageRate, 0, tile.OccupiedCharacter, null);
                        var damageInfo = CharacterController.DamageInfo.Create(resultDamage, codeId, AttackerType.COMMANDER_SKILL);

                        tile.OccupiedCharacter.GetDamaged(damageInfo, null);
                    }
                }
            }
        }
        public override InGameTile GetRecommendedTile(SkillCommander specCommanderSkillData)
        {
            InGameObjectManager inGameObjectManagerInstance = InGameObjectManager.Instance;
            var enemyCharacterList = inGameObjectManagerInstance.GetCharacterList(AllianceType.Enemy);
            if (enemyCharacterList == null || enemyCharacterList.Count <= 0)
                return null;

            return base.GetOptimalTileRangeTypePlus(enemyCharacterList, specCommanderSkillData.commander_range_size);
        }

        protected override void PromotionCommanderSkillCheck(PromotionLevelType firstPromotionLevel, PromotionLevelType secondPromotionLevel)
        {
        }
    }
}
