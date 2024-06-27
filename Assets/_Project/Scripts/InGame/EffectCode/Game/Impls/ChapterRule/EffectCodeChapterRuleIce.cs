using System;
using System.Collections.Generic;
using CookApps.AutoBattler;
using CookApps.Obfuscator;

namespace CookApps.BattleSystem
{
    [UseEffectCodeIds(CodeId)]
    public class EffectCodeChapterRuleIce : EffectCodeGameBase
    {
        private const int CodeId = (int) EffectCodeNameType.CHAPTER_ICE;
        List<InGameTile> _chapterRuleTiles = new List<InGameTile>();
        private float _effectCodeStat;
        private float elapsedTime = 0f;

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
                _chapterRuleTiles.Add(inGameTile);

                InGameVfxManager.Instance.AddInGameVfx(InGameVfxNameType.fx_common_trap_ice_01,
                    inGameTile.View.CachedTr.position);
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
                _chapterRuleTiles.Add(inGameTile);

                InGameVfxManager.Instance.AddInGameVfx(InGameVfxNameType.fx_common_trap_ice_01,
                    inGameTile.View.CachedTr.position);
            }
        }

        public override void OnTileCharacterEnter(InGameTile tile, CharacterController character)
        {
        }

        public override void OnTileCharacterExit(InGameTile tile, CharacterController character)
        {
            if (!(InGameMainFlowManager.Instance.CurrentFlowState is FlowStateStageCombat))
                return;

            bool isIceTile = _chapterRuleTiles.Exists(l => l == tile);
            if (isIceTile)
            {
                Debug.LogColor("[TEST] OnTileCharacterEnter", "blue");
                if (tile == null || character == null)
                {
                    return;
                }

                InGameVfxManager.Instance.AddInGameVfx(InGameVfxNameType.fx_common_trap_ice_02,
                    character.GetCharacterView().CachedTr.position);

                Span<double> eccStats = stackalloc double[1];
                eccStats.Clear();
                eccStats[0] = _effectCodeStat;

                long effectCodeID = (long)EffectCodeNameType.STUN;
                var effectCodeInfo = new EffectCodeInfo(effectCodeID, 0, eccStats);
                character.GetEffectCodeContainer().AddOrMergeEffectCode(effectCodeInfo, null);
            }
        }
    }
}
