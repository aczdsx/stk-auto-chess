using System;
using System.Collections.Generic;
using CookApps.AutoBattler;
using CookApps.Obfuscator;

namespace CookApps.BattleSystem
{
    [UseEffectCodeIds(CodeId)]
    public partial class EffectCodeChapterRuleAP : EffectCodeGameBase
    {
        private float _abilUpRate;
        private float _maxRemainBuffTime;
        private const EffectCodeNameType BuffEffectCodeID = EffectCodeNameType.BUFF_AP_PERCENT_UP;
        private const int CodeId = (int)EffectCodeNameType.RULE_AP;

        private List<InGameTile> _chapterRuleTiles = new();
        private List<CharacterController> _characterControllers = new();

        protected override void SetRuleTileByInfo(EffectCodeInfo codeInfo)
        {
            _abilUpRate = codeInfo.GetCodeStatToFloat(1) * 0.01f;
            _maxRemainBuffTime = codeInfo.GetCodeStatToFloat(2);
            _chapterRuleTiles.Clear();
            _characterControllers.Clear();

            var tileID = codeInfo.GetCodeStatToInt(0);
            var inGameTile = InGameObjectManager.Instance.GetInGameTile(tileID);
            _chapterRuleTiles.Add(inGameTile);
            InGameVfxManager.Instance.AddInGameVfx(InGameVfxNameType.fx_common_bufftrap_ap, inGameTile.View.CachedTr.position);

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
        }

        public override void Merge(EffectCodeInfo codeInfo, IEffectCodeSource source)
        {
            base.Merge(codeInfo, source);

            SetRuleTileByInfo(codeInfo);
        }

        public override void OnTileCharacterEnter(InGameTile tile, CharacterController character)
        {

            if (_chapterRuleTiles.Exists(l => l.View.ID == tile.View.ID))
            {
                // Key는 유저 Value 이펙트 인포로 적용 해제를 레퍼런스.
                // 추가하고 실행

                if (_chapterRuleTiles.Exists(l => l.View.ID == tile.View.ID))
                {
                    Span<double> eccStats = stackalloc double[3];
                    eccStats.Clear();
                    eccStats[0] = CodeId;
                    eccStats[1] = 99999f;
                    eccStats[2] = _abilUpRate;

                    EffectCodeHelper.AddOrMergeEffectCode(EffectCodeNameType.BUFF_AP_PERCENT_UP, character, eccStats, source);

                    _characterControllers.Add(character);
                }
            }
        }

        public override void OnTileCharacterExit(InGameTile tile, CharacterController character)
        {

            // 하지만, Combat이전에 버프를 받던지 아니던지 해야한다.
            // if (!(InGameMainFlowManager.Instance.CurrentFlowState is FlowStateStageCombat)) return;

            if (_chapterRuleTiles.Exists(l => l == tile))
            {
                // 딕셔너리에서 캐릭터 검색
                // 이펙트 코드 리무브 하기
                if (character.AllianceType != AllianceType.Wall)
                {
                    if (_characterControllers.Exists(c => c.CharacterUId == character.CharacterUId))
                    {
                        if (!(InGameMainFlowManager.Instance.CurrentFlowState is FlowStateStageCombat))
                        {
                            character.GetEffectCodeContainer().RemoveEffectCode((long)EffectCodeNameType.BUFF_AP_PERCENT_UP);
                        }
                        else
                        {
                            Span<double> eccStats = stackalloc double[3];
                            eccStats.Clear();
                            eccStats[0] = CodeId;
                            eccStats[1] = _maxRemainBuffTime;
                            eccStats[2] = _abilUpRate;

                            EffectCodeHelper.AddOrMergeEffectCode(EffectCodeNameType.BUFF_AP_PERCENT_UP, character, eccStats, source);
                        }
                    }
                    _characterControllers.Remove(character);
                }
            }
        }
    }
}
