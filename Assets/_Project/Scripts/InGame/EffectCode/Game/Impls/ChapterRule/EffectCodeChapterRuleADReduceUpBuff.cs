using System;
using System.Collections.Generic;
using CookApps.AutoBattler;
using CookApps.Obfuscator;

namespace CookApps.BattleSystem
{
    [UseEffectCodeIds(CodeId)]
    public partial class EffectCodeChapterRuleADReduceUpBuff : EffectCodeGameBase
    {
        private float _defUpRate;
        private float _maxRemainBuffTime;

        private const EffectCodeNameType BuffEffectCodeID = EffectCodeNameType.BUFF_AD_REDUCE_UP;
        // private const EffectCodeNameType BuffEffectCodeID = (EffectCodeNameType)2000000023;
        private const int CodeId = 1210000002;

        private List<InGameTile> _chapterRuleTiles = new();
        private List<CharacterController> _characterControllers = new();

        protected override void SetRuleTileByInfo(EffectCodeInfo codeInfo)
        {
            _defUpRate = codeInfo.GetCodeStatToInt(1) * 0.01f;
            _maxRemainBuffTime = codeInfo.GetCodeStatToFloat(2);
            _chapterRuleTiles.Clear();
            _characterControllers.Clear();

            var tileID = codeInfo.GetCodeStatToInt(0);
            var inGameTile = InGameObjectManager.Instance.GetInGameTile(tileID);
            _chapterRuleTiles.Add(inGameTile);

            InGameVfxManager.Instance.AddInGameVfx(InGameVfxNameType.fx_common_bufftrap_defense, inGameTile.View.CachedTr.position);

            if (inGameTile.CheckValidTile(AllianceType.Enemy, true) ||
                inGameTile.CheckValidTile(AllianceType.Player, true))
            {
                OnTileCharacterEnter(inGameTile, inGameTile.OccupiedCharacter);
            }

        }

        public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container,
            IEffectCodeSource source)
        {
            base.Initialize(codeInfo, container, source);

            SetRuleTileByInfo(codeInfo);
            // EffectCharacterByRules();
        }

        public override void Merge(EffectCodeInfo codeInfo, IEffectCodeSource source)
        {
            base.Merge(codeInfo, source);

            SetRuleTileByInfo(codeInfo);
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

                    EffectCodeHelper.AddOrMergeEffectCode(BuffEffectCodeID, character, eccStats, source);

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
                    {
                        if (!(InGameMainFlowManager.Instance.CurrentFlowState is FlowStateStageCombat))
                        {
                            character.GetEffectCodeContainer().RemoveEffectCode((long)BuffEffectCodeID);
                        }
                        else
                        {
                            Span<double> eccStats = stackalloc double[3];
                            eccStats.Clear();
                            eccStats[0] = CodeId;
                            eccStats[1] = _maxRemainBuffTime;
                            eccStats[2] = _defUpRate;

                            EffectCodeHelper.AddOrMergeEffectCode(BuffEffectCodeID, character, eccStats, source);
                        }
                    }
                    _characterControllers.Remove(character);
                }
        }
    }
}