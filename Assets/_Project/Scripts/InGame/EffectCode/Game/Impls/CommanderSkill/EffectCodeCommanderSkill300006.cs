using System;
using CookApps.AutoBattler;
using CookApps.Obfuscator;
using CookApps.TeamBattle.Utility;

//3*3범위 내의 적의 공격력을 {0}초 동안 {1}% 감소 시킨다.
namespace CookApps.BattleSystem
{
    [UseEffectCodeIds(CodeId)]
    public class EffectCodeCommanderSkill300006 : EffectCodeCommanderSkillBase
    {
        private const int CodeId = 300006; // (int)EffectCodeNameType.COMMANDER_SKILL_CURSE;
        private ObfuscatorFloat _powerRate;

        public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container,
            IEffectCodeSource source)
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

            _powerRate = _specTargetCommanderSkill.base_rate;

            PromotionCommanderSkillCheck((PromotionLevelType)codeInfo.GetCodeStatToInt(2), (PromotionLevelType)codeInfo.GetCodeStatToInt(3));
            SkillAction();
        }

        public override void Merge(EffectCodeInfo codeInfo, IEffectCodeSource source)
        {
            base.Merge(codeInfo, source);

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

            _powerRate = _specTargetCommanderSkill.base_rate;

            PromotionCommanderSkillCheck((PromotionLevelType)codeInfo.GetCodeStatToInt(2), (PromotionLevelType)codeInfo.GetCodeStatToInt(3));
            SkillAction();
        }

        protected override void SkillAction()
        {
            ObjectRegistry.GetObject<InGameCamera>(RegistryKey.InGameCamera).ShakeCamera(0.4f, 0.15f);
            var inGameTile = InGameObjectManager.Instance.GetInGameTile(_tileID);
            var tileList = InGameObjectManager.Instance.InGameGrid.GetTileListByShapeSquare(inGameTile, 1);

            foreach (var tile in tileList)
            {
                InGameVfxManager.Instance.AddInGameVfx(InGameVfxNameType.fx_common_commander_skill_05,
                    tile.View.CachedTr.position);

                if (tile.CheckValidTile(AllianceType.Player, false))
                {
                    Span<double> eccStats = stackalloc double[3];
                    eccStats.Clear();
                    eccStats[0] = codeId;
                    eccStats[1] = 3;
                    eccStats[2] = _powerRate;
                        
                    EffectCodeHelper.AddOrMergeEffectCode(EffectCodeNameType.DEBUFF_AD_PERCENT_DOWN, tile.OccupiedCharacter, eccStats, source);
                }
            }
        }
        public override InGameTile GetRecommendedTile(SkillCommander specCommanderSkillData)
        {
            InGameObjectManager inGameObjectManagerInstance = InGameObjectManager.Instance;
            var enemyList = inGameObjectManagerInstance.GetCharacterListSortedByADDescending(AllianceType.Enemy, false);
            if (enemyList == null || enemyList.Count <= 0)
                return null;
            
            return enemyList[0].CurrentTile;
        }
        protected override void PromotionCommanderSkillCheck(PromotionLevelType firstPromotionLevel, PromotionLevelType secondPromotionLevel)
        {
        }
    }
}
