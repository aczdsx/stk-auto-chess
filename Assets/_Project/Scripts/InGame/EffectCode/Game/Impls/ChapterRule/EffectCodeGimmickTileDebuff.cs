using System;
using System.Collections.Generic;
using CookApps.AutoBattler;
using CookApps.Obfuscator;

namespace CookApps.BattleSystem
{
    [UseEffectCodeIds(CodeId)]
    public class EffectCodeGimmickTileDebuff : EffectCodeGameBase
    {
        private const int CodeId = (int) EffectCodeNameType.CHAPTER_FIRE;

        List<InGameTile> gimmickTiles = new List<InGameTile>();

        public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container,
            IEffectCodeSource source)
        {
            base.Initialize(codeInfo, container, source);
            for (int i = 0; i < codeInfo.StatsLength; i++)
            {
                int tileID = codeInfo.GetCodeStatToInt(i);
                gimmickTiles.Add(InGameObjectManager.Instance.GetInGameTile(tileID));
            }
        }

        public override void Merge(EffectCodeInfo codeInfo, IEffectCodeSource source)
        {
            base.Merge(codeInfo, source);
            for (int i = 0; i < codeInfo.StatsLength; i++)
            {
                int tileID = codeInfo.GetCodeStatToInt(i);
                gimmickTiles.Add(InGameObjectManager.Instance.GetInGameTile(tileID));
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
                var someCodeId = (int) EffectCodeNameType.DEBUFF_FIRE;
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
