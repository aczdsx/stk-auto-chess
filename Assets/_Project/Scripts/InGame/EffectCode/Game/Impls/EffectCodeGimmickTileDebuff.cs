using System;
using System.Collections.Generic;
using CookApps.Obfuscator;

namespace CookApps.BattleSystem
{
    public class EffectCodeGimmickTileDebuff : EffectCodeGameBase
    {

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
                var someCodeId = 0;
                var debuff = new EffectCodeInfo(someCodeId, 0, 4, 0, 0, 0, 0);
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
