using System;
using CookApps.AutoBattler;
using CookApps.BattleSystem;
using CookApps.Obfuscator;

namespace CookApps.BattleSystem
{
    [UseEffectCodeIds(CodeId)]
    public class EffectCodeTileBurn : EffectCodeTileBase
    {
        private const int CodeId = (int)EffectCodeNameType.TILE_BURN;
        private ObfuscatorInt _ownerUID;
        private ObfuscatorFloat _damageRate;
        private ObfuscatorFloat _duration;
        public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container, IEffectCodeSource source)
        {
            base.Initialize(codeInfo, container, source);
            _ownerUID = codeInfo.GetCodeStatToInt(0);
            _damageRate = codeInfo.GetCodeStatToFloat(1);
            _duration = codeInfo.GetCodeStatToFloat(2);
        }

        public override void Merge(EffectCodeInfo codeInfo, IEffectCodeSource source)
        {
            base.Merge(codeInfo, source);
            _ownerUID = codeInfo.GetCodeStatToInt(0);
            _damageRate = codeInfo.GetCodeStatToFloat(1);
            _duration = codeInfo.GetCodeStatToFloat(2);
        }

        public override void OnTileCharacterEnter(InGameTile tile, CharacterController character)
        {
            if (tile == null || character == null)
            {
                return;
            }

            var someCodeId = (int)EffectCodeNameType.DEBUFF_FIRE;
            Span<double> debuffStats = stackalloc double[3];
            debuffStats.Clear();
            debuffStats[0] = codeId;
            debuffStats[1] = _ownerUID;
            debuffStats[2] = _damageRate;
            debuffStats[3] = _duration;

            var debuff = new EffectCodeInfo(someCodeId, 0, debuffStats);
            character.GetEffectCodeContainer().AddOrMergeEffectCode(debuff, null);
        }

        public override void OnTileCharacterExit(InGameTile tile, CharacterController character)
        {
            if (tile == null || character == null)
            {
                return;
            }

            var someCodeId = (int)EffectCodeNameType.DEBUFF_FIRE;
            character.GetEffectCodeContainer().RemoveEffectCode(someCodeId);
        }
    }
}
