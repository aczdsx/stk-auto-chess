using System;
using System.Collections.Generic;
using System.Linq;
using CookApps.AutoBattler;
using CookApps.Obfuscator;
using Cysharp.Threading.Tasks;

//배치판 위의 아군과 적, 지형 지물 요소의 위치를 모두 랜덤하게 바꾼다.
namespace CookApps.BattleSystem
{
    [UseEffectCodeIds(CodeId)]
    public class EffectCodeCommanderSkill300005 : EffectCodeCommanderSkillBase
    {
        private const int CodeId = (int)EffectCodeNameType.COMMANDER_SKILL_TELEPORT;
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

            PromotionCommanderSkillCheck((PromotionLevelType)codeInfo.GetCodeStatToInt(2), (PromotionLevelType)codeInfo.GetCodeStatToInt(3));
            SkillActionAsync().Forget();
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

            PromotionCommanderSkillCheck((PromotionLevelType)codeInfo.GetCodeStatToInt(2), (PromotionLevelType)codeInfo.GetCodeStatToInt(3));

            SkillActionAsync().Forget();
        }

        protected override async UniTaskVoid SkillActionAsync()
        {
            var inGameTile = InGameObjectManager.Instance.GetInGameTile(_tileID);
            var character = inGameTile.OccupiedCharacter;

            InGameVfxManager.Instance.AddInGameVfx(InGameVfxNameType.fx_common_commander_skill_06_01,
                inGameTile.View.CachedTr.position);

            await UniTask.Delay(TimeSpan.FromSeconds(0.3));

            if (character != null)
            {
                var randomTile = InGameObjectManager.Instance.InGameGrid.GetRandomEmptyTile();
                character.ChangeOccupiedTile(randomTile);
                character.Position3D = randomTile.View.Position;
                character.GetCharacterView().CachedTr.localPosition = randomTile.View.Position;

                InGameVfxManager.Instance.AddInGameVfx(InGameVfxNameType.fx_common_commander_skill_06_02,
                    randomTile.View.CachedTr.position);
            }
        }
        public override InGameTile GetRecommendedTile(SpecCommanderSkill specCommanderSkillData)
        {
            InGameObjectManager inGameObjectManagerInstance = InGameObjectManager.Instance;
            var playerList = inGameObjectManagerInstance.GetCharacterListSortedByHpRate(AllianceType.Player, true);
            if (playerList == null || playerList.Count <= 0)
                return null;

            return playerList[0].CurrentTile;
        }
        protected override void PromotionCommanderSkillCheck(PromotionLevelType firstPromotionLevel, PromotionLevelType secondPromotionLevel)
        {
        }
    }
}
