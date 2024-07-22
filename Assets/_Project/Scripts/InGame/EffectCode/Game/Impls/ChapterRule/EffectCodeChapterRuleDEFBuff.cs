using System;
using System.Collections.Generic;
using CookApps.AutoBattler;
using CookApps.Obfuscator;

namespace CookApps.BattleSystem
{
    [UseEffectCodeIds(CodeId)]
    public class EffectCodeChapterRuleDEFBuff : EffectCodeGameBase
    {
        private float _defUpRate;
        private const long BuffEffectCodeID = (long)EffectCodeNameType.BUFF_DEF_PERCENT_UP;
        private const int CodeId = (int)EffectCodeNameType.RULE_DEF;

        private List<InGameTile> _chapterRuleTiles = new();
        private List<CharacterController> _characterControllers = new();

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
            _defUpRate = codeInfo.GetCodeStatToInt(0) * 0.01f;
            _chapterRuleTiles.Clear();
            SetRuleTileByInfo(codeInfo, InGameVfxNameType.fx_common_bufftrap_defense);
            // EffectCharacterByRules();
        }

        public override void Merge(EffectCodeInfo codeInfo, IEffectCodeSource source)
        {
            base.Merge(codeInfo, source);
            _defUpRate = codeInfo.GetCodeStatToInt(0) * 0.01f;
            _chapterRuleTiles.Clear();
            SetRuleTileByInfo(codeInfo, InGameVfxNameType.fx_common_bufftrap_defense);
            // EffectCharacterByRules();
        }

        public override void OnTileCharacterEnter(InGameTile tile, CharacterController character)
        {
            if (_chapterRuleTiles.Exists(l => l.View.ID == tile.View.ID))
                if (character.AllianceType != AllianceType.Wall)
                {
                    Span<double> eccStats = stackalloc double[3];
                    eccStats.Clear();
                    eccStats[0] = CodeId;
                    eccStats[1] = 99999f;
                    eccStats[2] = _defUpRate;


                    var effectCodeID = BuffEffectCodeID;
                    var effectCodeInfo = new EffectCodeInfo(effectCodeID, 0, eccStats);

                    character.GetEffectCodeContainer().AddOrMergeEffectCode(effectCodeInfo, source);

                    _characterControllers.Add(character);
                }
        }

        // private void EffectCharacterByRules()
        // {
        //     for (var i = 1; i < codeInfo.StatsLength; i++)
        //     {
        //         var tileID = codeInfo.GetCodeStatToInt(i);
        //         var inGameTile = InGameObjectManager.Instance.GetInGameTile(tileID);
        //         if (inGameTile.OccupiedCharacter != null &&
        //             inGameTile.OccupiedCharacter.AllianceType == AllianceType.Player)
        //             TryApplyRule(inGameTile.OccupiedCharacter);
        //     }
        // }

        public override void OnTileCharacterExit(InGameTile tile, CharacterController character)
        {
            if (_chapterRuleTiles.Exists(l => l == tile))
                if (character.AllianceType != AllianceType.Wall)
                {
                    if (_characterControllers.Exists(c => c.CharacterUId == character.CharacterUId))
                        character.GetEffectCodeContainer().RemoveEffectCode(BuffEffectCodeID);
                    _characterControllers.Remove(character);
                }
        }
    }
}