using System;
using CookApps.AutoBattler;
using CookApps.Obfuscator;

namespace CookApps.BattleSystem
{
    [UseEffectCodeIds(CodeId)]
    public partial class EffectCodeTileFire : EffectCodeTileBase
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

            Span<double> eccStats = stackalloc double[4];
            eccStats.Clear();
            eccStats[0] = codeId;
            eccStats[1] = _ownerUID;
            eccStats[2] = _damageRate;
            eccStats[3] = _duration;
            
            EffectCodeHelper.AddOrMergeEffectCode(EffectCodeNameType.DEBUFF_FIRE, character, eccStats, source);
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
