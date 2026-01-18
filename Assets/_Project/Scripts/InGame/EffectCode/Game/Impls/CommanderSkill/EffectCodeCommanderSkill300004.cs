using System.Linq;
using CookApps.AutoBattler;
using CookApps.Obfuscator;

// 3*3 범위 내에 위치한 적의 체력 아군 평균 공격력의 {0}%만큼 회복시킨다.
namespace CookApps.BattleSystem
{
    [UseEffectCodeIds(CodeId)]
    public class EffectCodeCommanderSkill300004 : EffectCodeCommanderSkillBase
    {
        private const int CodeId = 300004; // (int)EffectCodeNameType.COMMANDER_SKILL_LIFEHEAL;
        private ObfuscatorFloat _healRate;

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

            _healRate = _specTargetCommanderSkill.base_rate;
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

            _healRate = _specTargetCommanderSkill.base_rate;
            PromotionCommanderSkillCheck((PromotionLevelType)codeInfo.GetCodeStatToInt(2), (PromotionLevelType)codeInfo.GetCodeStatToInt(3));
            SkillAction();
        }

        protected override void SkillAction()
        {
            var inGameTile = InGameObjectManager.Instance.GetInGameTile(_tileID);
            var tileList = InGameObjectManager.Instance.InGameGrid.GetTileListByShapeSquare(inGameTile,
            _specTargetCommanderSkill.commander_range_size / 2);
            double damageAmount = 0;

            var playerCharacterList = InGameObjectManager.Instance.GetCharacterList(AllianceType.Player);
            if (playerCharacterList != null && playerCharacterList.Count > 0)
            {
                var strongestCharacter = playerCharacterList.OrderByDescending(c => c.AD).First();
                damageAmount = strongestCharacter.AD * _healRate;
            }

            foreach (var tile in tileList)
            {
                InGameVfxManager.Instance.AddInGameVfx(InGameVfxNameType.fx_common_commander_skill_02,
                    tile.View.CachedTr.position);

                if (tile.CheckValidTile(AllianceType.Player, true))
                {
                    CharacterController.DamageInfo damageInfo = new CharacterController.DamageInfo();
                    damageInfo.damageAmount = damageAmount;

                    tile.OccupiedCharacter.GetHealed(damageInfo.damageAmount, null, codeId);
                }
            }
        }
        public override InGameTile GetRecommendedTile(SkillCommander specCommanderSkillData)
        {
            InGameObjectManager inGameObjectManagerInstance = InGameObjectManager.Instance;
            var playerCharacterList = inGameObjectManagerInstance.GetCharacterList(AllianceType.Player);
            if (playerCharacterList == null || playerCharacterList.Count <= 0)
                return null;

            return base.GetOptimalTileRangeTypeSquare(playerCharacterList, specCommanderSkillData.commander_range_size);
        }
        protected override void PromotionCommanderSkillCheck(PromotionLevelType firstPromotionLevel, PromotionLevelType secondPromotionLevel)
        {
        }
    }
}
