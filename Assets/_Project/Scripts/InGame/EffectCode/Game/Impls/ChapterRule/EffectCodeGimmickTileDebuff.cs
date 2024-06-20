using System;
using System.Collections.Generic;
using CookApps.AutoBattler;
using CookApps.Obfuscator;

namespace CookApps.BattleSystem
{
    [UseEffectCodeIds(CodeId)]
    public class EffectCodeGimmickTileDebuff : EffectCodeGameBase
    {
        private const int CodeId = (int)EffectCodeNameType.CHAPTER_FIRE;

        private ObfuscatorInt gimmickTileCount;
        List<InGameTile> gimmickTiles = new List<InGameTile>();

        public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container, IEffectCodeSource source)
        {
            base.Initialize(codeInfo, container, source);
            gimmickTileCount = codeInfo.GetCodeStatToInt(0);
            InGameMainFlowManager.OnFlowStateChanged += OnStateChanged;
        }

        public override void Merge(EffectCodeInfo codeInfo, IEffectCodeSource source)
        {
            base.Merge(codeInfo, source);
            gimmickTileCount = codeInfo.GetCodeStatToInt(0);
        }

        public override void OnPreRemoved()
        {
            base.OnPreRemoved();
            InGameMainFlowManager.OnFlowStateChanged -= OnStateChanged;
        }

        private void OnStateChanged(StateBase flowState)
        {
            if (flowState is FlowStateStageReady)
            {
                gimmickTiles.Clear();
                var tiles = InGameObjectManager.Instance.GetAllInGameTiles();
                // random gimmick tiles
                while (gimmickTiles.Count < gimmickTileCount)
                {
                    var tile = tiles[InGameRandomManager.GetUniversalRandomValue(0, tiles.Length)];
                    if (gimmickTiles.Contains(tile))
                    {
                        continue;
                    }
                    gimmickTiles.Add(tile);

                    InGameVfxManager.Instance.AddInGameVfx(InGameVfxNameType.fx_common_chapter1,
                        tile.View.CachedTr.position);
                }
            }
        }

        public override void OnTileCharacterEnter(InGameTile tile, CharacterController character)
        {
            if (tile == null || character == null)
            {
                return;
            }

            if (gimmickTiles.Contains(tile))
            {
                var someCodeId = (int)EffectCodeNameType.DEBUFF_FIRE;
                Span<double> debuffStats = stackalloc double[3];
                debuffStats.Clear();
                debuffStats[0] = codeId;
                debuffStats[1] = 1.0f;

                var debuff = new EffectCodeInfo(someCodeId, 0, debuffStats);
                character.GetEffectCodeContainer().AddOrMergeEffectCode(debuff, null);
            }
        }

        public override void OnTileCharacterExit(InGameTile tile, CharacterController character)
        {
            if (tile == null || character == null)
            {
                return;
            }

            if (gimmickTiles.Contains(tile))
            {
                var someCodeId = 0;
                character.GetEffectCodeContainer().RemoveEffectCode(someCodeId);
            }
        }
    }
}
