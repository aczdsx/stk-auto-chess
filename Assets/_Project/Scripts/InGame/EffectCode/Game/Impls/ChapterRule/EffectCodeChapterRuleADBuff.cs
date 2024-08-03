using System;
using System.Collections.Generic;
using CookApps.AutoBattler;
using CookApps.Obfuscator;
using Cysharp.Threading.Tasks;
using UnityEngine.TextCore.Text;

namespace CookApps.BattleSystem
{
    [UseEffectCodeIds(CodeId)]
    public class EffectCodeChapterRuleAD : EffectCodeGameBase
    {
        private float _atkUpRate;
        private const int CodeId = (int)EffectCodeNameType.RULE_AD;

        private List<InGameTile> _chapterRuleTiles = new();
        private List<CharacterController> _characterControllers  = new();

        private void SetRuleTileByInfo(EffectCodeInfo codeInfo, InGameVfxNameType vnt)
        {
            for (var i = 1; i < codeInfo.StatsLength; i++)
            {
                var tileID = codeInfo.GetCodeStatToInt(i);
                var inGameTile = InGameObjectManager.Instance.GetInGameTile(tileID);
                _chapterRuleTiles.Add(inGameTile);

                InGameVfxManager.Instance.AddInGameVfx(vnt, inGameTile.View.CachedTr.position);
            }
        }

        public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container,
            IEffectCodeSource source)
        {
            base.Initialize(codeInfo, container, source);
            _atkUpRate = codeInfo.GetCodeStatToFloat(0) * 0.01f;
            _chapterRuleTiles.Clear();
            
            SetRuleTileByInfo(codeInfo, InGameVfxNameType.fx_common_bufftrap_ad);
        }

        public override void Merge(EffectCodeInfo codeInfo, IEffectCodeSource source)
        {
            base.Merge(codeInfo, source);
            _atkUpRate = codeInfo.GetCodeStatToFloat(0) * 0.01f;
            _chapterRuleTiles.Clear();
            SetRuleTileByInfo(codeInfo, InGameVfxNameType.fx_common_bufftrap_ad);
        }

        public override void OnTileCharacterEnter(InGameTile tile, CharacterController character)
        {
            if (_chapterRuleTiles.Exists(l => l.View.ID == tile.View.ID))
            {
                if (character.AllianceType != AllianceType.Wall)
                {
                    Span<double> eccStats = stackalloc double[3];
                    eccStats.Clear();
                    eccStats[0] = CodeId;
                    eccStats[1] = 99999f;
                    eccStats[2] = _atkUpRate;
                    
                    EffectCodeHelper.AddOrMergeEffectCode(EffectCodeNameType.BUFF_AD_PERCENT_UP, character, eccStats, source);

                    _characterControllers.Add(character);
                }
            }
        }

        public override void OnTileCharacterExit(InGameTile tile, CharacterController character)
        {
            if (_chapterRuleTiles.Exists(l => l == tile))
            {
                if (character.AllianceType != AllianceType.Wall)
                {
                    if(_characterControllers.Exists(c => c.CharacterUId == character.CharacterUId))
                    {
                        character.GetEffectCodeContainer().RemoveEffectCode((long)EffectCodeNameType.BUFF_AD_PERCENT_UP);
                    }
                    _characterControllers.Remove(character);
                }
            }
        }
    }
}