using System;
using System.Collections.Generic;
using CookApps.AutoBattler;
using CookApps.Obfuscator;
using Cysharp.Threading.Tasks;
using Unity.Mathematics;

namespace CookApps.BattleSystem
{
    public abstract class EffectCodeCommanderSkillBase : EffectCodeCharacterBase
    {
        public override EffectCodeType Type => EffectCodeType.CommanderSkill;
        protected ObfuscatorInt _tileID;
        protected SkillCommander _specTargetCommanderSkill;
        protected static Dictionary<int2, bool> _reuseableTileDic = new Dictionary<int2, bool>(); //index, occupied

        protected virtual void SkillAction()
        {

        }
        protected virtual async UniTaskVoid SkillActionAsync()
        {

        }

        /// <summary>
        /// 해당 스킬에 대한 데이터를 받아 각 커멘더스킬마다 정의한 추천 타일을 반환합니다.
        /// 커멘더 스킬은 level 별로 다르게 반환되어야 할 필요도 있습니다.
        /// Auto Skill 에서 사용되는 메소드로 너무 연산량이 많을 필요는 없습니다.
        /// </summary>
        /// <param name="specCommanderSkillData"></param>
        /// <returns></returns>
        public abstract InGameTile GetRecommendedTile(SkillCommander specCommanderSkillData);


        /// <summary>
        /// 커멘더 스킬 이펙트 코드 정보를 생성합니다.
        /// 유저의 커멘드스킬 레벨과
        /// 5단계, 10단계에 어떤 승급을 선택하였는지 정보를 받아 이펙트 코드 정보를 생성합니다.
        /// </summary>
        /// <param name="specCommanderSkillData"></param>
        /// <param name="TargetTileView"></param>
        /// <returns></returns>
        public static EffectCodeInfo GenerateEffectCodeInfo(SkillCommander specCommanderSkillData, InGameTileView TargetTileView)
        {
            Span<double> stats = stackalloc double[4];
            stats.Clear();
            stats[0] = TargetTileView.ID;
            stats[1] = specCommanderSkillData.level;
            //5단계 승급 여부
            stats[2] = (double)specCommanderSkillData.promotion_level_type;
            //10단계 승급 
            stats[3] = (double)specCommanderSkillData.promotion_level_type;

            var effectCodeInfo = new EffectCodeInfo(specCommanderSkillData.commander_skill_id, 0, stats);
            return effectCodeInfo;
        }

        protected abstract void PromotionCommanderSkillCheck(PromotionLevelType firstPromotionLevel, PromotionLevelType secondPromotionLevel);

        protected InGameTile GetOptimalTileRangeTypePlus(List<CharacterController> searchCharacterList, int rangeSize)
        {

            _reuseableTileDic.Clear();
            int maxCount = 0;
            InGameTile maxCountTile = null;

            foreach (var character in searchCharacterList)
            {
                _reuseableTileDic.Add(character.CurrentTile.Int2Index, true);//해당 타일은 캐릭터가 있는 타일.
            }

            foreach (var character in searchCharacterList)
            {
                //플러스 범위 안의 타일을 가져온다.
                var inRangeTileList = InGameObjectManager.Instance.InGameGrid.GetTileListByShapePlusInRange(character.CurrentTile, rangeSize / 2);
                if (inRangeTileList == null || inRangeTileList.Count <= 0)
                {
                    continue;
                }
                int occupiedCount = 0;

                foreach (var tile in inRangeTileList)
                {
                    //해당타일위에 캐릭터가 있으면 ++
                    if (_reuseableTileDic.ContainsKey(tile.Int2Index))
                    {
                        ++occupiedCount;
                    }
                }

                if (occupiedCount > maxCount)
                {
                    maxCount = occupiedCount;
                    maxCountTile = character.CurrentTile;
                }
            }
            return maxCountTile;
        }
        protected InGameTile GetOptimalTileRangeTypeSquare(List<CharacterController> searchCharacterList, int rangeSize)
        {
            _reuseableTileDic.Clear();
            int maxCount = 0;
            InGameTile maxCountTile = null;


            foreach (var character in searchCharacterList)
            {
                _reuseableTileDic.Add(character.CurrentTile.Int2Index, true);//해당 타일은 캐릭터가 있는 타일.
            }

            foreach (var character in searchCharacterList)
            {
                //플러스 범위 안의 타일을 가져온다.
                var inRangeTileList = InGameObjectManager.Instance.InGameGrid.GetTileListByShapeSquare(character.CurrentTile, rangeSize / 2);
                if (inRangeTileList == null || inRangeTileList.Count <= 0)
                {
                    continue;
                }
                int occupiedCount = 0;

                foreach (var tile in inRangeTileList)
                {
                    //해당타일위에 캐릭터가 있으면 ++
                    if (_reuseableTileDic.ContainsKey(tile.Int2Index))
                    {
                        ++occupiedCount;
                    }
                }

                if (occupiedCount > maxCount)
                {
                    maxCount = occupiedCount;
                    maxCountTile = character.CurrentTile;
                }
            }

            return maxCountTile;
        }

    }

}
