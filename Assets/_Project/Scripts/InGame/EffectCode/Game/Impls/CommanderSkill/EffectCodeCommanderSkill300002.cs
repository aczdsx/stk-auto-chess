using System;
using System.Collections.Generic;
using System.Linq;
using CookApps.AutoBattler;
using CookApps.Obfuscator;

//십자범위 내 적을 {0}초 동안 빙결 시킨다.
namespace CookApps.BattleSystem
{
    [UseEffectCodeIds(CodeId)]
    public class EffectCodeCommanderSkill300002 : EffectCodeCommanderSkillBase
    {
        private const int CodeId = 300002; // (int)EffectCodeNameType.COMMANDER_SKILL_FREEZING;

        private ObfuscatorFloat _time;

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

            _time = _specTargetCommanderSkill.base_rate;
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

            _time = _specTargetCommanderSkill.base_rate;

            PromotionCommanderSkillCheck((PromotionLevelType)codeInfo.GetCodeStatToInt(2), (PromotionLevelType)codeInfo.GetCodeStatToInt(3));
            SkillAction();
        }

        protected override void SkillAction()
        {
            var inGameTile = InGameObjectManager.Instance.GetInGameTile(_tileID);
            var tileList = InGameObjectManager.Instance.InGameGrid.GetTileListByShapePlusInRange(inGameTile,
            _specTargetCommanderSkill.commander_range_size / 2);
            foreach (var tile in tileList)
            {
                InGameVfxManager.Instance.AddInGameVfx(InGameVfxNameType.fx_common_commander_skill_04,
                    tile.View.CachedTr.position);

                if (tile.CheckValidTile(AllianceType.Player, false))
                {
                    InGameVfxManager.Instance.AddInGameVfx(InGameVfxNameType.fx_common_trap_ice_02,
                        tile.View.CachedTr.position);

                    Span<double> eccStats = stackalloc double[1];
                    eccStats.Clear();
                    eccStats[0] = _time;

                    EffectCodeHelper.AddOrMergeEffectCode(EffectCodeNameType.CC_STUN, tile.OccupiedCharacter, eccStats, source);

                    tile.OccupiedCharacter.Position3D = tile.OccupiedCharacter.CurrentTile.View.Position;
                    tile.OccupiedCharacter.GetCharacterView().CachedTr.localPosition = tile.OccupiedCharacter.CurrentTile.View.Position;
                    SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_hit_ice);
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
