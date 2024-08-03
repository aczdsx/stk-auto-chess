using System;
using System.Collections.Generic;
using CookApps.AutoBattler;
using CookApps.Obfuscator;

namespace CookApps.BattleSystem
{
    [UseEffectCodeIds(CodeId)]
    public class EffectCodeChapterSandStorm : EffectCodeGameBase
    {
        private const int CodeId = (int) EffectCodeNameType.CHAPTER_SANDSTORM;
        Dictionary<InGameTile, InGameVfx> _chapterRuleTiles = new Dictionary<InGameTile, InGameVfx>();
        private float _effectCodeStat;

        public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container,
            IEffectCodeSource source)
        {
            base.Initialize(codeInfo, container, source);
            _effectCodeStat = codeInfo.GetCodeStatToInt(0);
            _chapterRuleTiles.Clear();
            for (int i = 1; i < codeInfo.StatsLength; i++)
            {
                int tileID = codeInfo.GetCodeStatToInt(i);
                InGameTile inGameTile = InGameObjectManager.Instance.GetInGameTile(tileID);

                var vfx = InGameVfxManager.Instance.AddInGameVfx(InGameVfxNameType.fx_common_trap_airborne,
                    inGameTile.View.CachedTr.position);
                _chapterRuleTiles.Add(inGameTile, vfx);
            }
        }

        public override void Merge(EffectCodeInfo codeInfo, IEffectCodeSource source)
        {
            base.Merge(codeInfo, source);
            _effectCodeStat = codeInfo.GetCodeStatToInt(0);
            for (int i = 0; i < codeInfo.StatsLength; i++)
            {
                int tileID = codeInfo.GetCodeStatToInt(i);
                InGameTile inGameTile = InGameObjectManager.Instance.GetInGameTile(tileID);

                var vfx = InGameVfxManager.Instance.AddInGameVfx(InGameVfxNameType.fx_common_trap_airborne,
                    inGameTile.View.CachedTr.position);
                _chapterRuleTiles.Add(inGameTile, vfx);
            }
        }

        public override void OnTileCharacterEnter(InGameTile tile, CharacterController character)
        {
            if (!(InGameMainFlowManager.Instance.CurrentFlowState is StateCombatBase))
                return;
            
            if (_chapterRuleTiles.ContainsKey(tile))
            {
                if (tile.OccupiedCharacter != null)
                {
                    {
                        Span<double> eccStats = stackalloc double[3];
                        eccStats.Clear();
                        eccStats[0] = _effectCodeStat;
                        eccStats[1] = 3.0f;
                        eccStats[2] = tile.View.ID;
                        EffectCodeHelper.AddOrMergeEffectCode(EffectCodeNameType.AIRBORNE, tile.OccupiedCharacter, eccStats, source);
                    }
                

                    {
                        Span<double> eccStats = stackalloc double[3];
                        eccStats.Clear();
                        eccStats[0] = codeId;
                        eccStats[1] = _effectCodeStat;
                        eccStats[2] = 0;
                    
                        EffectCodeHelper.AddOrMergeEffectCode(EffectCodeNameType.DEBUFF_AIRBORNE, tile.OccupiedCharacter, eccStats, source);
                    }
                }
                
                _chapterRuleTiles[tile].Remove();
                _chapterRuleTiles.Remove(tile);
            }
        }
    }
}
