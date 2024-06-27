using System;
using System.Collections.Generic;
using CookApps.AutoBattler;
using CookApps.Obfuscator;

namespace CookApps.BattleSystem
{
    [UseEffectCodeIds(CodeId)]
    public class EffectCodeChapterRuleIce : EffectCodeGameBase
    {
        public class CharacterInfo
        {
            public CharacterController Controller { get; set; }
            public float Value { get; set; }

            public CharacterInfo(CharacterController controller, float value)
            {
                Controller = controller;
                Value = value;
            }
        }

        private const int CodeId = (int) EffectCodeNameType.CHAPTER_ICE;
        List<InGameTile> _chapterRuleTiles = new List<InGameTile>();
        List<CharacterInfo> _characterList = new List<CharacterInfo>();
        private float _effectCodeStat;
        private float _durationTime = 7.0f;

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
            if (_chapterRuleTiles.Exists(l => l == tile))
            {
                _characterList.Add(new CharacterInfo(character, _durationTime - 0.8f));
            }
        }

        public override void OnTileCharacterExit(InGameTile tile, CharacterController character)
        {
            if (_chapterRuleTiles.Exists(l => l == tile))
            {
                _characterList.RemoveAll(c =>
                    c.Controller != null && c.Controller.CharacterUId == character.CharacterUId);
            }
        }

        public override void OnUpdate(float dt)
        {
            if (!(InGameMainFlowManager.Instance.CurrentFlowState is FlowStateStageCombat))
                return;

            foreach (var characterInfo in _characterList)
            {
                if (characterInfo.Controller != null && characterInfo.Controller.GetCharacterView() != null)
                {
                    characterInfo.Value += dt;

                    if (characterInfo.Value >= _durationTime)
                    {
                        InGameVfxManager.Instance.AddInGameVfx(InGameVfxNameType.fx_common_trap_ice_02,
                            characterInfo.Controller.GetCharacterView().CachedTr.position);

                        Span<double> eccStats = stackalloc double[1];
                        eccStats.Clear();
                        eccStats[0] = _effectCodeStat;

                        long effectCodeID = (long)EffectCodeNameType.STUN;
                        var effectCodeInfo = new EffectCodeInfo(effectCodeID, 0, eccStats);
                        characterInfo.Controller.GetEffectCodeContainer().AddOrMergeEffectCode(effectCodeInfo, null);

                        characterInfo.Controller.Position3D = characterInfo.Controller.CurrentTile.View.CachedTr.position;

                        characterInfo.Value -= _durationTime;
                        SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_hit_ice);
                    }
                }
            }
        }
    }
}
