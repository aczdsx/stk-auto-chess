using System;
using System.Collections.Generic;
using CookApps.AutoBattler;
using LitMotion;
using UnityEngine;

namespace CookApps.BattleSystem
{
    [UseEffectCodeIds(CodeId)]
    public partial class EffectCodeChapterRuleIce : EffectCodeGameBase
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

        private const int CodeId = (int)EffectCodeNameType.CHAPTER_ICE;
        List<InGameTile> _chapterRuleTiles = new List<InGameTile>();
        List<CharacterInfo> _characterList = new List<CharacterInfo>();
        private float _effectCodeStat;
        private float _durationTime = 7.0f;

        private MotionHandle _moveHandle;
        protected override void SetRuleTileByInfo(EffectCodeInfo codeInfo)
        {
            int tileID = codeInfo.GetCodeStatToInt(0);
            InGameTile inGameTile = InGameObjectManager.Instance.GetInGameTile(tileID);
            _chapterRuleTiles.Add(inGameTile);

            InGameVfxManager.Instance.AddInGameVfx(InGameVfxNameType.fx_common_trap_ice_01,
                inGameTile.View.CachedTr.position);
        }

        public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container,
            IEffectCodeSource source)
        {
            base.Initialize(codeInfo, container, source);
            _effectCodeStat = codeInfo.GetCodeStatToInt(1);
            _chapterRuleTiles.Clear();
            _characterList.Clear();

            SetRuleTileByInfo(codeInfo);
        }

        public override void Merge(EffectCodeInfo codeInfo, IEffectCodeSource source)
        {
            base.Merge(codeInfo, source);
            _effectCodeStat = codeInfo.GetCodeStatToInt(1);
            _chapterRuleTiles.Clear();
            _characterList.Clear();

            SetRuleTileByInfo(codeInfo);
        }

        public override void OnTileCharacterEnter(InGameTile tile, CharacterController character)
        {
            if (_chapterRuleTiles.Exists(l => l == tile))
            {
                _characterList.Add(new CharacterInfo(character, _durationTime));
            }
        }

        public override void OnTileCharacterExit(InGameTile tile, CharacterController character)
        {
            if (_chapterRuleTiles.Exists(l => l == tile))
            {
                _characterList.RemoveAll(c => c.Controller != null && c.Controller.CharacterUId == character.CharacterUId);
            }
        }

        public override void OnUpdate(float dt)
        {
            if (!(InGameMainFlowManager.Instance.CurrentFlowState is StateCombatBase))
                return;

            foreach (var characterInfo in _characterList)
            {
                if (characterInfo.Controller != null && characterInfo.Controller.GetCharacterView() != null)
                {
                    characterInfo.Value += dt;

                    if (characterInfo.Value >= _durationTime)
                    {
                        characterInfo.Value = 0;

                        Span<double> eccStats = stackalloc double[1];
                        eccStats.Clear();
                        eccStats[0] = _effectCodeStat;

                        EffectCodeHelper.AddOrMergeEffectCode(EffectCodeNameType.CC_STUN, characterInfo.Controller, eccStats, source);

                        var moveDuration = SpecOptionCache.DefaultMoveDuration / characterInfo.Controller.GetCharacterStat().MoveSpeed;

                        _moveHandle.TryCancel();
                        _moveHandle = LMotion.Create(
                            characterInfo.Controller.Position3D,
                            characterInfo.Controller.CurrentTile.View.Position,
                            moveDuration)
                            .WithEase(Ease.Linear)
                            .WithOnComplete(() =>
                            {
                                if (characterInfo.Controller != null)
                                {
                                    InGameVfxManager.Instance.AddInGameVfx(InGameVfxNameType.fx_common_trap_ice_02,
                                        characterInfo.Controller.GetCharacterView().CachedTr.position);
                                    SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_hit_ice);
                                }
                            })
                            .Bind(value =>
                            {
                                if (characterInfo.Controller != null)
                                {
                                    characterInfo.Controller.Position3D = value;
                                    characterInfo.Controller.GetCharacterView().CachedTr.localPosition = value;
                                }
                            });
                    }
                }
            }
        }
    }
}
