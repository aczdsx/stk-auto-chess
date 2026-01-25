using System;
using System.Collections.Generic;
using CookApps.AutoBattler;

namespace CookApps.BattleSystem
{
    [UseEffectCodeIds(CodeId)]
    public partial class EffectCodeChapterLandMine : EffectCodeGameBase
    {
        private const int CodeId = (int)EffectCodeNameType.CHAPTER_LANDMINE;
        Dictionary<InGameTile, InGameVfx> _chapterRuleTiles = new Dictionary<InGameTile, InGameVfx>();
        private float _effectCodeStat;
//  private enum TileRuleStatType
//     {
//         Tileidx = 0,
//         EffectStat_1 = 1,
//         EffectStat_2 = 2,
//         EffectStat_3 = 3,
//         End = 4,
//     }
        protected override void SetRuleTileByInfo(EffectCodeInfo codeInfo)
        {
            int tileID = codeInfo.GetCodeStatToInt(0);
            InGameTile inGameTile = InGameObjectManager.Instance.GetInGameTile(tileID);

            var vfx = InGameVfxManager.Instance.AddInGameVfx(InGameVfxNameType.fx_common_trap_explosion,
                inGameTile.View.CachedTr.position);
            _chapterRuleTiles.Add(inGameTile, vfx);
        }

        public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container,
            IEffectCodeSource source)
        {
            base.Initialize(codeInfo, container, source);
            _effectCodeStat = codeInfo.GetCodeStatToInt(1);
            _chapterRuleTiles.Clear();
            
            SetRuleTileByInfo(codeInfo);
        }

        public override void Merge(EffectCodeInfo codeInfo, IEffectCodeSource source)
        {
            base.Merge(codeInfo, source);
            _effectCodeStat = codeInfo.GetCodeStatToInt(1);

            SetRuleTileByInfo(codeInfo);
        }

        public override void OnTileCharacterEnter(InGameTile tile, CharacterController character)
        {
            if (!(InGameMainFlowManager.Instance.CurrentFlowState is StateCombatBase))
                return;

            if (_chapterRuleTiles.ContainsKey(tile))
            {
                InGameVfxManager.Instance.AddInGameVfx(InGameVfxNameType.fx_common_trap_ice_02,
                    character.GetCharacterView().CachedTr.position);

                Span<double> eccStats = stackalloc double[1];
                eccStats.Clear();
                eccStats[0] = _effectCodeStat;

                EffectCodeHelper.AddOrMergeEffectCode(EffectCodeNameType.CC_STUN, character, eccStats, source);

                _chapterRuleTiles[tile].Remove();
                _chapterRuleTiles.Remove(tile);
            }
        }
    }
}
