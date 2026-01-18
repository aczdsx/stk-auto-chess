using System;
using CookApps.AutoBattler;
using CookApps.Obfuscator;

//선택한 적 1명을 {0}초 동안 에어본한다.
namespace CookApps.BattleSystem
{
    [UseEffectCodeIds(CodeId)]
    public class EffectCodeCommanderSkill300003 : EffectCodeCommanderSkillBase
    {
        private const int CodeId = 300003; // (int)EffectCodeNameType.COMMANDER_SKILL_AIRBORNE;
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

            SkillAction();
        }

        protected override void SkillAction()
        {
            var inGameTile = InGameObjectManager.Instance.GetInGameTile(_tileID);

            if (inGameTile.OccupiedCharacter != null)
            {
                {
                    Span<double> eccStats = stackalloc double[3];
                    eccStats.Clear();
                    eccStats[0] = codeId;
                    eccStats[1] = _time;
                    eccStats[2] = 1.0f;
                    EffectCodeHelper.AddOrMergeEffectCode(EffectCodeNameType.CC_TARGET_IMPOSSIBLE, inGameTile.OccupiedCharacter, eccStats, source);
                }

                {
                    Span<double> eccStats = stackalloc double[3];
                    eccStats.Clear();
                    eccStats[0] = _time;
                    eccStats[1] = 4.0f;
                    eccStats[2] = inGameTile.View.ID;
                    EffectCodeHelper.AddOrMergeEffectCode(EffectCodeNameType.CC_AIRBORNE, inGameTile.OccupiedCharacter, eccStats, source);
                }

                {
                    Span<double> eccStats = stackalloc double[3];
                    eccStats.Clear();
                    eccStats[0] = codeId;
                    eccStats[1] = _time;
                    eccStats[2] = 0;

                    EffectCodeHelper.AddOrMergeEffectCode(EffectCodeNameType.DEBUFF_AIRBORNE, inGameTile.OccupiedCharacter, eccStats, source);
                }
            }
        }
        public override InGameTile GetRecommendedTile(SkillCommander specCommanderSkillData)
        {
            InGameObjectManager inGameObjectManagerInstance = InGameObjectManager.Instance;
            var enemyCharacterList = inGameObjectManagerInstance.GetCharacterListSortedByADDescending(AllianceType.Enemy, false);
            if (enemyCharacterList == null || enemyCharacterList.Count <= 0)
                return null;

            return enemyCharacterList[0].CurrentTile;
        }
        protected override void PromotionCommanderSkillCheck(PromotionLevelType firstPromotionLevel, PromotionLevelType secondPromotionLevel)
        {
        }
    }
}
